namespace PakReader.Parsers.Objects
{
    struct FObjectImport
    {
        // FObjectResource
        public FName ObjectName;
        public FPackageIndex OuterIndex;

        // FObjectImport
        public FName ClassPackage;
        public FName ClassName;
        //public bool bImportPackageHandled; unused for serialization
        //public bool bImportSearchedFor;
        //public bool bImportFailed;

        public FObjectImport(PackageReader reader)
        {
            ClassPackage = reader.ReadFName();
            ClassName = reader.ReadFName();
            OuterIndex = new FPackageIndex(reader);
            ObjectName = reader.ReadFName();
        }
    }
}
