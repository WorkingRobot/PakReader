using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers
{
    public class PackageReader
    {
        public string PackageFilename;
        public BinaryReader Loader;
        public Objects.FPackageFileSummary PackageFileSummary;
        internal FName[] NameMap;
        public long PackageFileSize;

        public PackageReader(BinaryReader uasset, BinaryReader uexp)
        {
            Loader = uasset;
            PackageFileSummary = new Objects.FPackageFileSummary(Loader);

            SerializeNameMap();
            var ImportMap = SerializeImportMap();
            var ExportMap = SerializeExportMap();
            foreach(var Export in ExportMap)
            {
                
                //if (Export.bIsAsset)
                {
                    // We need to get the class name from the import/export maps
                    FName ObjectClassName;
                    if (Export.ClassIndex.IsNull)
                        ObjectClassName = ReadFName(); // check if this is true, I don't know if Fortnite ever uses this
                    else if (Export.ClassIndex.IsExport)
                        ObjectClassName = ExportMap[Export.ClassIndex.AsExport].ObjectName;
                    else if (Export.ClassIndex.IsImport)
                        ObjectClassName = ImportMap[Export.ClassIndex.AsImport].ObjectName;
                    else
                        throw new FileLoadException("Can't get class name"); // Shouldn't reach this unless the laws of math have bent to MagmaReef's will

                    
                    Console.WriteLine($"Loading {ObjectClassName.Name}: {Export.bIsAsset}");
                }
            }
            return;
        }

        void SerializeNameMap()
        {
            if (PackageFileSummary.NameCount > 0)
            {
                Loader.BaseStream.Position = PackageFileSummary.NameOffset;

                NameMap = new FName[PackageFileSummary.NameCount];
                for (int NameMapIdx = 0; NameMapIdx < PackageFileSummary.NameCount; ++NameMapIdx)
                {
                    // Read the name entry from the file.
                    NameMap[NameMapIdx] = new FName(Loader);
                }
            }
        }

        FObjectImport[] SerializeImportMap()
        {
            if (PackageFileSummary.ImportCount > 0)
            {
                Loader.BaseStream.Position = PackageFileSummary.ImportOffset;

                var OutImportMap = new FObjectImport[PackageFileSummary.ImportCount];
                for (int ImportMapIdx = 0; ImportMapIdx < PackageFileSummary.ImportCount; ++ImportMapIdx)
                {
                    OutImportMap[ImportMapIdx] = new FObjectImport(this);
                }
                return OutImportMap;
            }
            return Array.Empty<FObjectImport>();
        }

        FObjectExport[] SerializeExportMap()
        {
            if (PackageFileSummary.ExportCount > 0)
            {
                Loader.BaseStream.Position = PackageFileSummary.ExportOffset;

                var OutExportMap = new FObjectExport[PackageFileSummary.ExportCount];
                for (int ExportMapIdx = 0; ExportMapIdx < PackageFileSummary.ExportCount; ++ExportMapIdx)
                {
                    OutExportMap[ExportMapIdx] = new FObjectExport(this);
                }
                return OutExportMap;
            }
            return Array.Empty<FObjectExport>();
        }

        public FName ReadFName()
        {
            var NameIndex = Loader.ReadInt32();
            var Number = Loader.ReadInt32();

            // https://github.com/EpicGames/UnrealEngine/blob/bf95c2cbc703123e08ab54e3ceccdd47e48d224a/Engine/Source/Runtime/CoreUObject/Public/UObject/LinkerLoad.h#L821
            // Has some more complicated stuff related to name map pools etc. that seems unnecessary atm
            if (NameIndex >= 0 && NameIndex < NameMap.Length)
            {
                return NameMap[NameIndex];
            }
            throw new FileLoadException($"Bad Name Index {NameIndex}");
        }


        public static implicit operator BinaryReader(PackageReader reader) => reader.Loader;

        public byte ReadByte() => Loader.ReadByte();
        public byte[] ReadBytes(int count) => Loader.ReadBytes(count);
        public string ReadFString() => Loader.ReadFString();

        public short ReadInt16() => Loader.ReadInt16();
        public ushort ReadUInt16() => Loader.ReadUInt16();
        public int ReadInt32() => Loader.ReadInt32();
        public uint ReadUInt32() => Loader.ReadUInt32();
        public long ReadInt64() => Loader.ReadInt64();
        public ulong ReadUInt64() => Loader.ReadUInt64();

        public long Position { get => Loader.BaseStream.Position; set => Loader.BaseStream.Position = value; }
    }
}
