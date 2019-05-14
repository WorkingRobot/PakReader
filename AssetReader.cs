using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static PakReader.AssetReader;

namespace PakReader
{
    public class AssetReader
    {
        public readonly ExportObject[] Exports;

        public AssetReader(string path, bool ubulk = false) : this(path + ".uasset", path + ".uexp", ubulk ? path + ".ubulk" : null) { }

        public AssetReader(string assetPath, string expPath, string bulkPath) : this(File.OpenRead(assetPath), File.OpenRead(expPath), bulkPath == null ? null : File.OpenRead(bulkPath)) { }

        public AssetReader(Stream asset, Stream exp, Stream bulk = null)
        {
            BinaryReader reader = new BinaryReader(asset);
            var summary = new AssetSummary(reader);

            reader.BaseStream.Seek(summary.name_offset, SeekOrigin.Begin);
            FNameEntrySerialized[] name_map = new FNameEntrySerialized[summary.name_count];
            for (int i = 0; i < summary.name_count; i++)
            {
                name_map[i] = new FNameEntrySerialized(reader);
            }

            reader.BaseStream.Seek(summary.import_offset, SeekOrigin.Begin);
            FObjectImport[] import_map = new FObjectImport[summary.import_count];
            for (int i = 0; i < summary.import_count; i++)
            {
                import_map[i] = new FObjectImport(reader, name_map, import_map);
            }

            reader.BaseStream.Seek(summary.export_offset, SeekOrigin.Begin);
            FObjectExport[] export_map = new FObjectExport[summary.export_count];
            for (int i = 0; i < summary.export_count; i++)
            {
                export_map[i] = new FObjectExport(reader, name_map, import_map);
            }

            long export_size = export_map.Sum(v => v.serial_size);

            reader = new BinaryReader(exp);

            var bulkReader = bulk == null ? null : new BinaryReader(bulk);

            int asset_length = summary.total_header_size;

            Exports = new ExportObject[summary.export_count];

            int ind = 0;
            foreach (FObjectExport v in export_map)
            {
                string export_type = v.class_index.import;
                long position = v.serial_offset - asset.Length;
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                switch (export_type)
                {
                    case "Texture2D":
                        Exports[ind] = new Texture2D(reader, name_map, import_map, asset_length, export_size, bulkReader);
                        break;
                    case "DataTable":
                        throw new NotImplementedException("Not implemented data table exporting");
                    case "SkeletalMesh":
                        throw new NotImplementedException("Not implemented mesh exporting");
                    case "AnimSequence":
                        throw new NotImplementedException("Not implemented animation exporting");
                    case "Skeleton":
                        throw new NotImplementedException("Not implemented skeleton exporting");
                    case "CurveTable":
                        throw new NotImplementedException("Not implemented curve table exporting");
                    default:
                        Exports[ind] = new UObject(reader, name_map, import_map, export_type, true);
                        break;
                }
                long valid_pos = position + v.serial_size;
                if (reader.BaseStream.Position != valid_pos)
                {
                    Console.WriteLine($"Did not read {export_type} correctly. Current Position: {reader.BaseStream.Position}, Bytes Remaining: {valid_pos - reader.BaseStream.Position}");
                    reader.BaseStream.Seek(valid_pos, SeekOrigin.Begin);
                    //throw new IOException($"Did not read {export_type} correctly. Current Position: {reader.BaseStream.Position}, Bytes Remaining: {valid_pos - reader.BaseStream.Position}");
                }
                ind++;
            }
            ind = 0;

        }

        internal static string read_string(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            //Console.WriteLine("Reading string of length " + length);
            if (length > 65536 || length < -65536)
            {
                throw new IOException($"String length too large ({length}), likely a read error.");
            }
            if (length < 0)
            {
                length *= -1;
                ushort[] data = new ushort[length];
                for (int i = 0; i < length; i++)
                {
                    data[i] = reader.ReadUInt16();
                }
                unsafe
                {
                    fixed (ushort* dataPtr = &data[0])
                        return new string((char*)dataPtr, 0, data.Length);
                }
            }
            else
            {
                byte[] bytes = reader.ReadBytes(length);
                if (bytes.Length == 0) return string.Empty;
                return Encoding.UTF8.GetString(bytes).Substring(0, length - 1);
            }
        }

        public static T[] read_tarray<T>(BinaryReader reader, Func<BinaryReader, T> getter)
        {
            int length = reader.ReadInt32();
            T[] container = new T[length];
            for (int i = 0; i < length; i++)
            {
                container[i] = getter(reader);
            }
            return container;
        }

        internal static string read_fname(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            //long index_pos = reader.BaseStream.Position;
            int name_index = reader.ReadInt32();
            reader.ReadInt32();
            return name_map[name_index].data;
        }

        static object tag_data_overrides(string name)
        {
            switch (name)
            {
                case "BindingIdToReferences":
                    return ("Guid", "LevelSequenceBindingReferenceArray");
                case "Tracks":
                    return ("MovieSceneTrackIdentifier", "MovieSceneEvaluationTrack");
                case "SubTemplateSerialNumbers":
                    return ("MovieSceneSequenceID", "UInt32Property");
                case "SubSequences":
                    return ("MovieSceneSequenceID", "MovieSceneSubSequenceData");
                case "Hierarchy":
                    return ("MovieSceneSequenceID", "MovieSceneSequenceHierarchyNode");
                case "TrackSignatureToTrackIdentifier":
                    return ("Guid", "MovieSceneTrackIdentifier");
                case "SubSectionRanges":
                    return ("Guid", "MovieSceneFrameRange");
                default:
                    return default;
            }
        }

        internal static FPropertyTag read_property_tag(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, bool read_data)
        {
            string name = read_fname(reader, name_map);
            if (name == "None")
            {
                return default;
            }

            //Console.WriteLine("pos " + reader.BaseStream.Position);
            string property_type = read_fname(reader, name_map).Trim();
            int size = reader.ReadInt32();
            int array_index = reader.ReadInt32();

            object tag_data;
            switch (property_type)
            {
                case "StructProperty":
                    tag_data = (read_fname(reader, name_map), new FGuid(reader));
                    break;
                case "BoolProperty":
                    tag_data = reader.ReadByte() != 0;
                    break;
                case "EnumProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                case "ByteProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                case "ArrayProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                case "MapProperty":
                    tag_data = (read_fname(reader, name_map), read_fname(reader, name_map));
                    break;
                case "SetProperty":
                    tag_data = read_fname(reader, name_map);
                    break;
                default:
                    tag_data = null;
                    break;
            }

            if (property_type == "MapProperty")
            {
                tag_data = tag_data_overrides(name) ?? tag_data;
            }

            bool has_property_guid = reader.ReadByte() != 0;
            FGuid property_guid = has_property_guid ? new FGuid(reader) : default;

            long pos = reader.BaseStream.Position;
            var tag = read_data ? new_property_tag_type(reader, name_map, import_map, property_type, tag_data) : default;
            if ((int)tag.type == 100)
            {
                return default;
            }

            if (read_data)
            {
                reader.BaseStream.Seek(pos + size, SeekOrigin.Begin);
            }
            if (read_data && pos + size != reader.BaseStream.Position)
            {
                throw new IOException($"Could not read entire property: {name} ({property_type})");
            }

            return new FPropertyTag
            {
                array_index = array_index,
                name = name,
                property_guid = property_guid,
                property_type = property_type,
                size = size,
                tag = tag.type,
                tag_data = tag.data
            };
        }

        internal static (FPropertyTagType type, object data) new_property_tag_type(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string property_type, object tag_data)
        {
            switch (property_type)
            {
                case "BoolProperty":
                    return (FPropertyTagType.BoolProperty, (bool)tag_data);
                case "StructProperty":
                    if (tag_data is UScriptStruct)
                    {
                        return (FPropertyTagType.StructProperty, tag_data);
                    }
                    return (FPropertyTagType.StructProperty, new UScriptStruct(reader, name_map, import_map, ((ValueTuple<string, FGuid>)tag_data).Item1));
                case "ObjectProperty":
                    return (FPropertyTagType.ObjectProperty, new FPackageIndex(reader, import_map));
                case "InterfaceProperty":
                    return (FPropertyTagType.InterfaceProperty, new UInterfaceProperty(reader));
                case "FloatProperty":
                    return (FPropertyTagType.FloatProperty, reader.ReadSingle());
                case "TextProperty":
                    return (FPropertyTagType.TextProperty, new FText(reader));
                case "StrProperty":
                    return (FPropertyTagType.StrProperty, read_string(reader));
                case "NameProperty":
                    return (FPropertyTagType.NameProperty, read_fname(reader, name_map));
                case "IntProperty":
                    return (FPropertyTagType.IntProperty, reader.ReadInt32());
                case "UInt16Property":
                    return (FPropertyTagType.UInt16Property, reader.ReadUInt16());
                case "UInt32Property":
                    return (FPropertyTagType.UInt32Property, reader.ReadUInt32());
                case "UInt64Property":
                    return (FPropertyTagType.UInt64Property, reader.ReadUInt64());
                case "ArrayProperty":
                    return (FPropertyTagType.ArrayProperty, new UScriptArray(reader, (string)tag_data, name_map, import_map));
                case "MapProperty":
                    (string key_type, string value_type) = (ValueTuple<string, string>)tag_data;
                    return (FPropertyTagType.MapProperty, new UScriptMap(reader, name_map, import_map, key_type, value_type));
                case "ByteProperty":
                    return (FPropertyTagType.ByteProperty, (string)tag_data == "None" ? (object)reader.ReadByte() : read_fname(reader, name_map));
                case "EnumProperty":
                    return (FPropertyTagType.EnumProperty, (string)tag_data == "None" ? null : read_fname(reader, name_map));
                case "SoftObjectProperty":
                    return (FPropertyTagType.SoftObjectProperty, new FSoftObjectPath(reader, name_map));
                default:
                    return ((FPropertyTagType)100, null);
                    //throw new NotImplementedException($"Could not read property type: {property_type} at pos {reader.BaseStream.Position}");
            }
        }

        struct AssetSummary
        {
            internal AssetSummary(BinaryReader reader)
            {
                //Console.WriteLine("starting position: " + reader.BaseStream.Position);
                tag = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", tag: " + tag);
                legacy_file_version = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", legacy_file_version: " + legacy_file_version);
                legacy_ue3_version = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", legacy_ue3_version: " + legacy_ue3_version);
                file_version_u34 = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", file_version_u34: " + file_version_u34);
                file_version_licensee_ue4 = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", file_version_licensee_ue4: " + file_version_licensee_ue4);
                custom_version_container = read_tarray(reader, r => new FCustomVersion(reader));
                //Console.WriteLine(reader.BaseStream.Position + ", custom_version_container: " + custom_version_container.Length);
                total_header_size = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", total_header_size: " + total_header_size);
                folder_name = read_string(reader);
                //Console.WriteLine(reader.BaseStream.Position + ", folder_name: " + folder_name);
                package_flags = reader.ReadUInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", package_flags: " + package_flags);
                name_count = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", name_count: " + name_count);
                name_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", name_offset: " + name_offset);
                gatherable_text_data_count = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", gatherable_text_data_count: " + gatherable_text_data_count);
                gatherable_text_data_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", gatherable_text_data_offset: " + gatherable_text_data_offset);
                export_count = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", export_count: " + export_count);
                export_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", export_offset: " + export_offset);
                import_count = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", import_count: " + import_count);
                import_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", import_offset: " + import_offset);
                depends_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", depends_offset: " + depends_offset);
                string_asset_references_count = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", string_asset_references_count: " + string_asset_references_count);
                string_asset_references_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", string_asset_references_offset: " + string_asset_references_offset);
                searchable_names_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", searchable_names_offset: " + searchable_names_offset);
                thumbnail_table_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", thumbnail_table_offset: " + thumbnail_table_offset);
                guid = new FGuid(reader);
                //Console.WriteLine(reader.BaseStream.Position + ", guid: " + guid.D);
                generations = read_tarray(reader, r => new FGenerationInfo(reader));
                //Console.WriteLine(reader.BaseStream.Position + ", generations: " + generations);
                saved_by_engine_version = new FEngineVersion(reader);
                //Console.WriteLine(reader.BaseStream.Position + ", saved_by_engine_version: " + saved_by_engine_version);
                compatible_with_engine_version = new FEngineVersion(reader);
                //Console.WriteLine(reader.BaseStream.Position + ", compatible_with_engine_version: " + compatible_with_engine_version);
                compression_flags = reader.ReadUInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", compression_flags: " + compression_flags);
                compressed_chunks = read_tarray(reader, r => new FCompressedChunk(reader));
                //Console.WriteLine(reader.BaseStream.Position + ", compressed_chunks: " + compressed_chunks.Length);
                package_source = reader.ReadUInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", package_source: " + package_source);
                additional_packages_to_cook = read_tarray(reader, r => read_string(r));
                //Console.WriteLine(reader.BaseStream.Position + ", additional_packages_to_cook: " + additional_packages_to_cook);
                asset_registry_data_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", asset_registry_data_offset: " + asset_registry_data_offset);
                buld_data_start_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", buld_data_start_offset: " + buld_data_start_offset);
                world_tile_info_data_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", world_tile_info_data_offset: " + world_tile_info_data_offset);
                chunk_ids = read_tarray(reader, r => r.ReadInt32());
                //Console.WriteLine(reader.BaseStream.Position + ", chunk_ids: " + chunk_ids);
                preload_dependency_count = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", preload_dependency_count: " + preload_dependency_count);
                preload_dependency_offset = reader.ReadInt32();
                //Console.WriteLine(reader.BaseStream.Position + ", preload_dependency_offset: " + preload_dependency_offset);
                var pos = reader.BaseStream.Position;
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                //Console.WriteLine(ToHex(reader.ReadBytes((int)pos)));
                //Console.WriteLine("ending position: " + reader.BaseStream.Position);
            }

            public int tag;
            public int legacy_file_version;
            public int legacy_ue3_version;
            public int file_version_u34;
            public int file_version_licensee_ue4;
            public FCustomVersion[] custom_version_container;
            public int total_header_size;
            public string folder_name;
            public uint package_flags;
            public int name_count;
            public int name_offset;
            public int gatherable_text_data_count;
            public int gatherable_text_data_offset;
            public int export_count;
            public int export_offset;
            public int import_count;
            public int import_offset;
            public int depends_offset;
            public int string_asset_references_count;
            public int string_asset_references_offset;
            public int searchable_names_offset;
            public int thumbnail_table_offset;
            public FGuid guid;
            public FGenerationInfo[] generations;
            public FEngineVersion saved_by_engine_version;
            public FEngineVersion compatible_with_engine_version;
            public uint compression_flags;
            public FCompressedChunk[] compressed_chunks;
            public uint package_source;
            public string[] additional_packages_to_cook;
            public int asset_registry_data_offset;
            public int buld_data_start_offset;
            public int world_tile_info_data_offset;
            public int[] chunk_ids;
            public int preload_dependency_count;
            public int preload_dependency_offset;
        }

        public struct FCustomVersion
        {
            public FGuid key;
            public int version;

            internal FCustomVersion(BinaryReader reader)
            {
                key = new FGuid(reader);
                version = reader.ReadInt32();
            }
        }

        public struct FGenerationInfo
        {
            public int export_count;
            public int name_count;

            internal FGenerationInfo(BinaryReader reader)
            {
                export_count = reader.ReadInt32();
                name_count = reader.ReadInt32();
            }
        }

        public struct FEngineVersion
        {
            public ushort major;
            public ushort minor;
            public ushort patch;
            public uint changelist;
            public string branch;

            internal FEngineVersion(BinaryReader reader)
            {
                major = reader.ReadUInt16();
                minor = reader.ReadUInt16();
                patch = reader.ReadUInt16();
                changelist = reader.ReadUInt32();
                branch = read_string(reader);
            }
        }

        public struct FCompressedChunk
        {
            public int uncompressed_offset;
            public int uncompressed_size;
            public int compressed_offset;
            public int compressed_size;

            internal FCompressedChunk(BinaryReader reader)
            {
                uncompressed_offset = reader.ReadInt32();
                uncompressed_size = reader.ReadInt32();
                compressed_offset = reader.ReadInt32();
                compressed_size = reader.ReadInt32();
            }
        }

        internal struct FNameEntrySerialized
        {
            public string data;
            public ushort non_case_preserving_hash;
            public ushort case_preserving_hash;

            internal FNameEntrySerialized(BinaryReader reader)
            {
                data = read_string(reader);
                non_case_preserving_hash = reader.ReadUInt16();
                case_preserving_hash = reader.ReadUInt16();
            }
        }

        internal struct FStripDataFlags
        {
            byte global_strip_flags;
            byte class_strip_flags;

            internal FStripDataFlags(BinaryReader reader)
            {
                global_strip_flags = reader.ReadByte();
                class_strip_flags = reader.ReadByte();
            }
        }

        public struct FTexturePlatformData
        {
            public int size_x;
            public int size_y;
            public int num_slices;
            public string pixel_format;
            public int first_mip;
            public FTexture2DMipMap[] mips;

            internal FTexturePlatformData(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
            {
                size_x = reader.ReadInt32();
                size_y = reader.ReadInt32();
                num_slices = reader.ReadInt32();
                pixel_format = read_string(reader);
                first_mip = reader.ReadInt32();
                mips = new FTexture2DMipMap[reader.ReadUInt32()];
                for(int i = 0; i < mips.Length; i++)
                {
                    mips[i] = new FTexture2DMipMap(reader, ubulk, bulk_offset);
                }
            }
        }

        public struct FTexture2DMipMap
        {
            public FByteBulkData data;
            public int size_x;
            public int size_y;
            public int size_z;

            internal FTexture2DMipMap(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
            {
                int cooked = reader.ReadInt32();
                data = new FByteBulkData(reader, ubulk, bulk_offset);
                size_x = reader.ReadInt32();
                size_y = reader.ReadInt32();
                size_z = reader.ReadInt32();
                if (cooked != 1)
                {
                    read_string(reader);
                }
            }
        }

        public struct FByteBulkData
        {
            public FByteBulkDataHeader header;
            public byte[] data;

            internal FByteBulkData(BinaryReader reader, BinaryReader ubulk, long bulk_offset)
            {
                header = new FByteBulkDataHeader(reader);

                data = null;
                if ((header.bulk_data_flags & 0x0040) != 0)
                {
                    data = reader.ReadBytes(header.element_count);
                }
                if ((header.bulk_data_flags & 0x0100) != 0)
                {
                    if (ubulk == null)
                    {
                        throw new IOException("No ubulk specified for texture");
                    }
                    // Archive seems "kind of" appended.
                    ubulk.BaseStream.Seek(header.offset_in_file + bulk_offset, SeekOrigin.Begin);
                    data = ubulk.ReadBytes(header.element_count);
                }

                if (data == null)
                {
                    throw new IOException("Could not read texture");
                }
            }
        }

        public struct FByteBulkDataHeader
        {
            public int bulk_data_flags;
            public int element_count;
            public int size_on_disk;
            public long offset_in_file;

            internal FByteBulkDataHeader(BinaryReader reader)
            {
                bulk_data_flags = reader.ReadInt32();
                element_count = reader.ReadInt32();
                size_on_disk = reader.ReadInt32();
                offset_in_file = reader.ReadInt64();
            }
        }

        internal struct FObjectImport
        {
            public string class_package;
            public string class_name;
            public FPackageIndex outer_index;
            public string object_name;

            public FObjectImport(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                class_package = read_fname(reader, name_map);
                class_name = read_fname(reader, name_map);
                outer_index = new FPackageIndex(reader, import_map);
                object_name = read_fname(reader, name_map);
            }
        }

        struct FObjectExport
        {
            public FPackageIndex class_index;
            public FPackageIndex super_index;
            public FPackageIndex template_index;
            public FPackageIndex outer_index;
            public string object_name;
            public uint save;
            public long serial_size;
            public long serial_offset;
            public bool forced_export;
            public bool not_for_client;
            public bool not_for_server;
            public FGuid package_guid;
            public uint package_flags;
            public bool not_always_loaded_for_editor_game;
            public bool is_asset;
            public int first_export_dependency;
            public bool serialization_before_serialization_dependencies;
            public bool create_before_serialization_dependencies;
            public bool serialization_before_create_dependencies;
            public bool create_before_create_dependencies;

            internal FObjectExport(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                class_index = new FPackageIndex(reader, import_map);
                super_index = new FPackageIndex(reader, import_map);
                template_index = new FPackageIndex(reader, import_map);
                outer_index = new FPackageIndex(reader, import_map);
                object_name = read_fname(reader, name_map);
                save = reader.ReadUInt32();
                serial_size = reader.ReadInt64();
                serial_offset = reader.ReadInt64();
                forced_export = reader.ReadInt32() != 0;
                not_for_client = reader.ReadInt32() != 0;
                not_for_server = reader.ReadInt32() != 0;
                package_guid = new FGuid(reader);
                package_flags = reader.ReadUInt32();
                not_always_loaded_for_editor_game = reader.ReadInt32() != 0;
                is_asset = reader.ReadInt32() != 0;
                first_export_dependency = reader.ReadInt32();
                serialization_before_serialization_dependencies = reader.ReadInt32() != 0;
                create_before_serialization_dependencies = reader.ReadInt32() != 0;
                serialization_before_create_dependencies = reader.ReadInt32() != 0;
                create_before_create_dependencies = reader.ReadInt32() != 0;
            }
        }

        internal struct FLevelSequenceObjectReferenceMap
        {
            public FLevelSequenceLegacyObjectReference[] map_data;

            internal FLevelSequenceObjectReferenceMap(BinaryReader reader)
            {
                int element_count = reader.ReadInt32();
                map_data = new FLevelSequenceLegacyObjectReference[element_count];
                for (int i = 0; i < element_count; i++)
                {
                    map_data[i] = new FLevelSequenceLegacyObjectReference(reader);
                }
            }
        }

        internal struct FLevelSequenceLegacyObjectReference
        {
            public FGuid key_guid;
            public FGuid object_id;
            public string object_path;

            internal FLevelSequenceLegacyObjectReference(BinaryReader reader)
            {
                key_guid = new FGuid(reader);
                object_id = new FGuid(reader);
                object_path = read_string(reader);
            }
        }

        internal struct FSectionEvaluationDataTree
        {
            public TMovieSceneEvaluationTree<FStructFallback> tree;

            internal FSectionEvaluationDataTree(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                tree = new TMovieSceneEvaluationTree<FStructFallback>(reader, name_map, import_map);
            }
        }

        internal struct TMovieSceneEvaluationTree<T>
        {
            public FMovieSceneEvaluationTree base_tree;
            public TEvaluationTreeEntryContainer<T> data;

            internal TMovieSceneEvaluationTree(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
            {
                base_tree = new FMovieSceneEvaluationTree(reader);
                data = new TEvaluationTreeEntryContainer<T>(reader);
            }
        }

        internal struct FMovieSceneEvaluationTree
        {
            public FMovieSceneEvaluationTreeNode root_node;
            public TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode> child_nodes;

            internal FMovieSceneEvaluationTree(BinaryReader reader)
            {
                root_node = new FMovieSceneEvaluationTreeNode(reader);
                child_nodes = new TEvaluationTreeEntryContainer<FMovieSceneEvaluationTreeNode>(reader);
            }
        }

        internal struct FMovieSceneEvaluationTreeNode
        {
            internal FMovieSceneEvaluationTreeNode(BinaryReader reader)
            {
                // holy shit this goes on forever
                throw new NotImplementedException("Not implemented yet.");
            }
        }

        internal struct TEvaluationTreeEntryContainer<T>
        {
            public FEntry[] entries;
            public T[] items;

            internal TEvaluationTreeEntryContainer(BinaryReader reader)
            {
                entries = read_tarray(reader, r => new FEntry(r));
                items = null;
                throw new NotImplementedException("Not implemented yet.");
            }
        }

        internal struct FEntry
        {
            public int start_index;
            public int size;
            public int capacity;

            internal FEntry(BinaryReader reader)
            {
                start_index = reader.ReadInt32();
                size = reader.ReadInt32();
                capacity = reader.ReadInt32();
            }
        }

        internal struct UInterfaceProperty
        {
            public uint interface_number;

            internal UInterfaceProperty(BinaryReader reader)
            {
                interface_number = reader.ReadUInt32();
            }
        }

        static (FPropertyTagType type, object data) read_map_value(BinaryReader reader, string inner_type, string struct_type, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            switch (inner_type)
            {
                case "BoolProperty":
                    return (FPropertyTagType.BoolProperty, reader.ReadByte() != 1);
                case "EnumProperty":
                    return (FPropertyTagType.EnumProperty, read_fname(reader, name_map));
                case "UInt32Property":
                    return (FPropertyTagType.UInt32Property, reader.ReadUInt32());
                case "StructProperty":
                    return (FPropertyTagType.StructProperty, new UScriptStruct(reader, name_map, import_map, struct_type));
                case "NameProperty":
                    return (FPropertyTagType.NameProperty, read_fname(reader, name_map));
                default:
                    return (FPropertyTagType.StructProperty, new UScriptStruct(reader, name_map, import_map, inner_type));
            }
        }

        internal struct UScriptMap
        {
            public ((FPropertyTagType type, object data), (FPropertyTagType type, object data))[] map_data;

            public UScriptMap(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string key_type, string value_type)
            {
                int num_keys_to_remove = reader.ReadInt32();
                if (num_keys_to_remove != 0)
                {
                    throw new NotSupportedException($"Could not read MapProperty with types: {key_type} {value_type}");
                }

                int num = reader.ReadInt32();
                map_data = new ValueTuple<(FPropertyTagType type, object data), (FPropertyTagType type, object data)>[num];
                for (int i = 0; i < num; i++)
                {
                    map_data[i] = (read_map_value(reader, key_type, "StructProperty", name_map, import_map), read_map_value(reader, value_type, "StructProperty", name_map, import_map));
                }
            }
        }
    }

    public enum FPropertyTagType
    {
        BoolProperty,
        StructProperty,
        ObjectProperty,
        InterfaceProperty,
        FloatProperty,
        TextProperty,
        StrProperty,
        NameProperty,
        IntProperty,
        UInt16Property,
        UInt32Property,
        UInt64Property,
        ArrayProperty,
        MapProperty,
        ByteProperty,
        EnumProperty,
        SoftObjectProperty,
    }

    public struct FPropertyTag
    {
        public string name;
        [JsonIgnore]
        public string property_type;
        public object tag_data;
        [JsonIgnore]
        public int size;
        [JsonIgnore]
        public int array_index;
        [JsonIgnore]
        public FGuid property_guid;
        [JsonIgnore]
        public FPropertyTagType tag;

        public bool Equals(FPropertyTag b)
        {
            return name == b.name &&
                property_type == b.property_type &&
                size == b.size &&
                array_index == b.array_index &&
                tag == b.tag &&
                tag_data == b.tag_data;
        }
    }

    public struct UScriptStruct
    {
        public string struct_name;
        public object struct_type;

        internal UScriptStruct(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string struct_name)
        {
            this.struct_name = struct_name;
            switch (struct_name)
            {
                case "Vector2D":
                    struct_type = new FVector2D(reader);
                    break;
                case "LinearColor":
                    struct_type = new FLinearColor(reader);
                    break;
                case "Color":
                    struct_type = new FColor(reader);
                    break;
                case "GameplayTagContainer":
                    struct_type = new FGameplayTagContainer(reader, name_map);
                    break;
                case "IntPoint":
                    struct_type = new FIntPoint(reader);
                    break;
                case "Guid":
                    struct_type = new FGuid(reader);
                    break;
                case "Quat":
                    struct_type = new FQuat(reader);
                    break;
                case "Vector":
                    struct_type = new FVector(reader);
                    break;
                case "Rotator":
                    struct_type = new FRotator(reader);
                    break;
                case "SoftObjectPath":
                    struct_type = new FSoftObjectPath(reader, name_map);
                    break;
                case "LevelSequenceObjectReferenceMap":
                    struct_type = new FLevelSequenceObjectReferenceMap(reader);
                    break;
                case "FrameNumber":
                    struct_type = reader.ReadSingle();
                    break;/*
                    case "SectionEvaluationDataTree":
                        struct_type = new FSectionEvaluationDataTree(reader, name_map, import_map);
                        break;
                    case "MovieSceneTrackIdentifier":
                        struct_type = reader.ReadSingle();
                        break;
                    case "MovieSceneSegment":
                        struct_type = new FMovieSceneSegment(reader, name_map, import_map);
                        break;
                    case "MovieSceneEvalTemplatePtr":
                        struct_type = new InlineUStruct(reader, name_map, import_map);
                        break;
                    case "MovieSceneTrackImplementationPtr":
                        struct_type = new InlineUStruct(reader, name_map, import_map);
                        break;
                    case "MovieSceneSequenceInstanceDataPtr":
                        struct_type = new InlineUStruct(reader, name_map, import_map);
                        break;
                    case "MovieSceneFrameRange":
                        struct_type = new FMovieSceneFrameRange(reader, name_map, import_map);
                        break;
                    case "MovieSceneSegmentIdentifier":
                        struct_type = reader.ReadSingle();
                        break;
                    case "MovieSceneSequenceID":
                        struct_type = reader.ReadSingle();
                        break;
                    case "MovieSceneEvaluationKey":
                        struct_type = new FMovieSceneEvaluationKey(reader, name_map, import_map);
                        break;*/
                default:
                    struct_type = new FStructFallback(reader, name_map, import_map);
                    break;
            }
        }
    }

    public struct FStructFallback
    {
        public FPropertyTag[] properties;

        internal FStructFallback(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            var properties_ = new List<FPropertyTag>();
            int i = 0;
            while (true)
            {
                var tag = read_property_tag(reader, name_map, import_map, true);
                if (tag.Equals(default))
                {
                    break;
                }

                properties_.Add(tag);
                i++;
            }
            properties = properties_.ToArray();
        }
    }

    public struct FVector2D
    {
        public float x;
        public float y;

        internal FVector2D(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
        }
    }

    public struct FLinearColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        internal FLinearColor(BinaryReader reader)
        {
            r = reader.ReadSingle();
            g = reader.ReadSingle();
            b = reader.ReadSingle();
            a = reader.ReadSingle();
        }
    }

    public struct FColor
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        internal FColor(BinaryReader reader)
        {
            r = reader.ReadByte();
            g = reader.ReadByte();
            b = reader.ReadByte();
            a = reader.ReadByte();
        }
    }

    public struct FGameplayTagContainer
    {
        public string[] gameplay_tags;

        internal FGameplayTagContainer(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            uint length = reader.ReadUInt32();
            gameplay_tags = new string[length];

            for (int i = 0; i < length; i++)
            {
                gameplay_tags[i] = read_fname(reader, name_map);
            }
        }
    }

    public struct FIntPoint
    {
        public uint x;
        public uint y;

        internal FIntPoint(BinaryReader reader)
        {
            x = reader.ReadUInt32();
            y = reader.ReadUInt32();
        }
    }

    public struct FQuat
    {
        public float x;
        public float y;
        public float z;
        public float w;

        internal FQuat(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            w = reader.ReadSingle();
        }
    }

    public struct FVector
    {
        public float x;
        public float y;
        public float z;

        internal FVector(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
        }
    }

    public struct FRotator
    {
        public float pitch;
        public float yaw;
        public float roll;

        internal FRotator(BinaryReader reader)
        {
            pitch = reader.ReadSingle();
            yaw = reader.ReadSingle();
            roll = reader.ReadSingle();
        }
    }

    public struct FSoftObjectPath
    {
        public string asset_path_name;
        public string sub_path_string;

        internal FSoftObjectPath(BinaryReader reader, FNameEntrySerialized[] name_map)
        {
            asset_path_name = read_fname(reader, name_map);
            sub_path_string = read_string(reader);
        }
    }

    public struct FLevelSequenceObjectReferenceMap
    {
        public FLevelSequenceLegacyObjectReference[] map_data;

        internal FLevelSequenceObjectReferenceMap(BinaryReader reader)
        {
            int element_count = reader.ReadInt32();
            map_data = new FLevelSequenceLegacyObjectReference[element_count];
            for (int i = 0; i < element_count; i++)
            {
                map_data[i] = new FLevelSequenceLegacyObjectReference(reader);
            }
        }
    }

    public struct FLevelSequenceLegacyObjectReference
    {
        public FGuid key_guid;
        public FGuid object_id;
        public string object_path;

        internal FLevelSequenceLegacyObjectReference(BinaryReader reader)
        {
            key_guid = new FGuid(reader);
            object_id = new FGuid(reader);
            object_path = read_string(reader);
        }
    }

    public abstract class ExportObject { }

    public sealed class Texture2D : ExportObject, IDisposable
    {
        public UObject base_object;
        public uint cooked;
        internal FTexturePlatformData[] textures;

        internal Texture2D(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, int asset_file_size, long export_size, BinaryReader ubulk)
        {
            var uobj = new UObject(reader, name_map, import_map, "Texture2D", true); // unsure if read zero is true or false

            new FStripDataFlags(reader); // no idea
            new FStripDataFlags(reader); // why are there two

            List<FTexturePlatformData> texs = new List<FTexturePlatformData>();
            uint cooked = reader.ReadUInt32();
            if (cooked == 1)
            {
                string pixel_format = read_fname(reader, name_map);
                while (pixel_format != "None")
                {
                    long skipOffset = reader.ReadInt64();
                    var texture = new FTexturePlatformData(reader, ubulk, export_size + asset_file_size);
                    if (reader.BaseStream.Position + asset_file_size != skipOffset)
                    {
                        throw new IOException("Texture read incorrectly");
                    }
                    texs.Add(texture);
                    pixel_format = read_fname(reader, name_map);
                }
            }

            textures = texs.ToArray();
        }

        public SKImage GetImage() => ImageExporter.GetImage(textures[0].mips[0], textures[0].pixel_format);

        public void Dispose()
        {
            textures = null;
        }
    }

    public struct FPackageIndex
    {
        [JsonIgnore]
        public int index;
        public string import;

        internal FPackageIndex(BinaryReader reader, FObjectImport[] import_map)
        {
            index = reader.ReadInt32();
            var import = get_package(index, import_map);
            if (import.Equals(default))
            {
                this.import = index.ToString();
            }
            else
            {
                this.import = import.object_name;
            }
        }

        static FObjectImport get_package(int index, FObjectImport[] import_map)
        {
            if (index < 0) index *= -1;
            index -= 1;
            if (index < 0 || index >= import_map.Length)
            {
                return default;
            }
            return import_map[index];
        }
    }

    public class UObject : ExportObject
    {
        public string export_type;
        public FPropertyTag[] properties;

        internal UObject(BinaryReader reader, FNameEntrySerialized[] name_map, FObjectImport[] import_map, string export_type, bool read_zero)
        {
            this.export_type = export_type;
            var properties_ = new List<FPropertyTag>();
            while (true)
            {
                var tag = read_property_tag(reader, name_map, import_map, true);
                if (tag.Equals(default))
                {
                    break;
                }
                properties_.Add(tag);
            }

            if (read_zero)
            {
                reader.ReadUInt32();
            }

            properties = properties_.ToArray();
        }
    }

    public struct FText
    {
        public uint flags;
        public byte history_type;
        public string @namespace;
        public string key;
        public string source_string;

        internal FText(BinaryReader reader)
        {
            flags = reader.ReadUInt32();
            history_type = reader.ReadByte();

            if (history_type == 255)
            {
                @namespace = "";
                key = "";
                source_string = "";
            }
            else if (history_type == 0)
            {
                @namespace = read_string(reader);
                key = read_string(reader);
                source_string = read_string(reader);
            }
            else
            {
                throw new NotImplementedException($"Could not read history type: {history_type}");
            }
        }
    }
    public struct UScriptArray
    {
        public FPropertyTag tag;
        public object[] data;

        internal UScriptArray(BinaryReader reader, string inner_type, FNameEntrySerialized[] name_map, FObjectImport[] import_map)
        {
            uint element_count = reader.ReadUInt32();
            tag = default;

            if (inner_type == "StructProperty" || inner_type == "ArrayProperty")
            {
                tag = read_property_tag(reader, name_map, import_map, true);
                if (tag.Equals(default))
                {
                    throw new IOException("Could not read file");
                }
            }
            object inner_tag_data = tag.Equals(default) ? null : tag.tag_data;

            data = new object[element_count];
            for (int i = 0; i < element_count; i++)
            {
                if (inner_type == "BoolProperty")
                {
                    data[i] = reader.ReadByte() != 0;
                }
                else if (inner_type == "ByteProperty")
                {
                    data[i] = reader.ReadByte();
                }
                else
                {
                    var tag = new_property_tag_type(reader, name_map, import_map, inner_type, inner_tag_data);
                    if ((int)tag.type != 100)
                    {
                        data[i] = tag.data;
                    }
                }
            }
        }
    }
}
