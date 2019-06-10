using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static PakReader.AssetReader;

namespace PakReader
{/*
    class MeshExporter
    {
        public static ExportMesh(BinaryWriter writer, USkeletalMesh mesh, FSkeletalMeshRenderData lod)
        {
            VChunkHeader BoneHdr, InfHdr;

            int i, j;
            CVertexShare Share;

            Share.Prepare(lod.ver)
        }
    }

    struct UsableMesh
    {
        public ExportObject OriginalMesh;
        public FBox BoundingBox;
        public FSphere BoundingSphere;
        public CVec3 MeshOrigin;
        public CVec3 MeshScale;
        public FRotator RotOrigin;
        public CSkelMeshBone[] RefSkeleton;
        public CSkelMeshLod[] Lods;
        public CSkelMeshSocket[] Sockets;

        public UsableMesh(USkeletalMesh mesh)
        {
            // convert bounds
            BoundingSphere.R = mesh.imported_bounds.sphere_radius / 2;
            BoundingBox.Min = mesh.imported_bounds.origin - mesh.imported_bounds.box_extend;
            BoundingBox.Max = mesh.imported_bounds.origin + mesh.imported_bounds.box_extend;

            // MeshScale, MeshOrigin, RotOrigin are removed in UE4
            //!! NOTE: MeshScale is integrated into RefSkeleton.RefBonePose[0].Scale3D.
            //!! Perhaps rotation/translation are integrated too!
            MeshOrigin = new CVec3 { v = new float[] { 0, 0, 0 } };
            RotOrigin = new FRotator { pitch = 0, roll = 0, yaw = 0 };
            MeshScale = new CVec3 { v = new float[] { 1, 1, 1 } };

            // convert LODs
            Lods = new CSkelMeshLod[mesh.lod_models.Length];
            for(int i = 0; i < Lods.Length; i++)
            {
                var SrcLod = mesh.lod_models[i];
                if (SrcLod.Indices.Indices16.Length == 0 && SrcLod.Indices.Indices32.Length == 0)
                {
                    // No indicies in this lod
                    continue;
                }

                int NumTexCoords = SrcLod.text
            }
        }
    }

    struct FBox
    {
        public FVector Min, Max;
        public byte IsValid;
    }

    struct FSphere
    {
        public float X;
        public float Y;
        public float Z;
        public float R;
    }

    struct VChunkHeader
    {
        public string ChunkID; // text identifier, 20 length
        public int TypeFlag; // version: 1999801 or 2003321
        public int DataSize; // sizeof(type)
        public int DataCount; // number of array elements
    }

    struct CVec3
    {
        public float[] v; // 3 length

        public static CVec3 operator -(CVec3 a, CVec3 b)
        {
            return new CVec3
            {
                v = new float[]
                {
                    a.v[0] - b.v[0],
                    a.v[1] - b.v[1],
                    a.v[2] - b.v[2]
                }
            };
        }

        public static CVec3 operator +(CVec3 a, CVec3 b)
        {
            return new CVec3
            {
                v = new float[]
                {
                    a.v[0] + b.v[0],
                    a.v[1] + b.v[1],
                    a.v[2] + b.v[2]
                }
            };
        }
    }

    struct CVec4
    {
        public float[] v; // 4 length
    }

    struct CQuat
    {
        public float x, y, z, w;
    }

    struct CSkelMeshBone
    {
        public string Name;
        public int ParentIndex;
        public CVec3 Position;
        public CQuat Orientation;
    }

    struct CSkelMeshLod
    {
        // generic properties
        public int NumTexCoords;
        public bool HasNormals;
        public bool HasTangents;

        // geometry
        public CMeshSection[] Sections;
        public int NumVerts;
        public CMeshUVFloat[] ExtraUV; // 7 length
        public CIndexBuffer Indices;

        // impl
        public CSkelMeshVertex[] Verts;
    }

    struct CSkelMeshSocket
    {
        public string Name;
        public string Bone;
        public CCoords Transform;
    }

    struct CCoords
    {
        public CVec3 origin;
        public CVec3 axis;
    }

    struct CMeshSection
    {
        public UUnrealMaterial Material;
        public int FirstIndex;
        public int NumFaces;
    }

    class UUnrealMaterial : ExportObject // dummy obj atm
    {

    }

    struct CMeshUVFloat
    {
        public float U, V;
    }

    struct CIndexBuffer
    {
        public ushort[] Indices16;
        public uint[] Indices32;
    }

    struct CSkelMeshVertex
    {
        public CVec4 Position;
        public CPackedNormal Normal;
        public CPackedNormal Tangent;
        public CMeshUVFloat UV;

        public uint PackedWeights;
        public short[] Bone; // 4 length
    }

    struct CVertexShare
    {
        public FVector[] Points;
        public FPackedNormal[] Normals;
        public uint[] ExtraInfos;
        public int[] WedgeToVert;
        public int[] VertToWedge;
        public int WedgeIndex;

        // hashing
        public FVector Mins, Maxs;
        public FVector Extents;
        public int[] Hash; // 16384 length
        public int[] HashNext;

    }*/
}
