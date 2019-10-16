namespace PakReader.Parsers.Objects
{
    struct FObjectExport
    {
        // FObjectResource
        public FName ObjectName;
        public FPackageIndex OuterIndex;

        // FObjectExport
        public FPackageIndex ClassIndex;
        //public FPackageIndex ThisIndex; unused for serialization
        public FPackageIndex SuperIndex;
        public FPackageIndex TemplateIndex;
        public EObjectFlags ObjectFlags;
        public long SerialSize;
        public long SerialOffset;
        //public long ScriptSerializationStartOffset;
        //public long ScriptSerializationEndOffset;
        //public UObject Object;
        //public int HashNext;
        public bool bForcedExport;
        public bool bNotForClient;
        public bool bNotForServer;
        public bool bNotAlwaysLoadedForEditorGame;
        public bool bIsAsset;
        //public bool bExportLoadFailed;
        //public EDynamicType DynamicType;
        //public bool bWasFiltered;
        public FGuid PackageGuid;
        public uint PackageFlags;
        public int FirstExportDependency;
        public int SerializationBeforeSerializationDependencies;
        public int CreateBeforeSerializationDependencies;
        public int SerializationBeforeCreateDependencies;
        public int CreateBeforeCreateDependencies;

        public FObjectExport(PackageReader reader)
        {
            ClassIndex = new FPackageIndex(reader);
            SuperIndex = new FPackageIndex(reader);

            // only serialize when file version is past VER_UE4_TemplateIndex_IN_COOKED_EXPORTS
            TemplateIndex = new FPackageIndex(reader);

            OuterIndex = new FPackageIndex(reader);
            ObjectName = reader.ReadFName();

            ObjectFlags = (EObjectFlags)reader.ReadUInt32() & EObjectFlags.RF_Load;

            // only serialize when file version is past VER_UE4_64BIT_EXPORTMAP_SERIALSIZES
            SerialSize = reader.ReadInt64();
            SerialOffset = reader.ReadInt64();

            bForcedExport = reader.ReadInt32() != 0;
            bNotForClient = reader.ReadInt32() != 0;
            bNotForServer = reader.ReadInt32() != 0;

            PackageGuid = new FGuid(reader);
            PackageFlags = reader.ReadUInt32();

            // only serialize when file version is past VER_UE4_LOAD_FOR_EDITOR_GAME
            bNotAlwaysLoadedForEditorGame = reader.ReadInt32() != 0;

            // only serialize when file version is past VER_UE4_COOKED_ASSETS_IN_EDITOR_SUPPORT
            bIsAsset = reader.ReadInt32() != 0;

            // only serialize when file version is past VER_UE4_PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS
            FirstExportDependency = reader.ReadInt32();
            SerializationBeforeSerializationDependencies = reader.ReadInt32();
            CreateBeforeSerializationDependencies = reader.ReadInt32();
            SerializationBeforeCreateDependencies = reader.ReadInt32() ;
            CreateBeforeCreateDependencies = reader.ReadInt32();
        }

        public enum EDynamicType : byte
        {
            NotDynamicExport,
		    DynamicType,
		    ClassDefaultObject,
	    };
    }
}
