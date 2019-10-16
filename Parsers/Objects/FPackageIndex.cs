using System.IO;

namespace PakReader.Parsers.Objects
{
    /**
     * Wrapper for index into a ULnker's ImportMap or ExportMap.
     * Values greater than zero indicate that this is an index into the ExportMap.  The
     * actual array index will be (FPackageIndex - 1).
     *
     * Values less than zero indicate that this is an index into the ImportMap. The actual
     * array index will be (-FPackageIndex - 1)
     */
    public struct FPackageIndex
    {
        public int Index;

        public FPackageIndex(BinaryReader reader)
        {
            Index = reader.ReadInt32();
        }

        public bool IsNull => Index == 0;
        public bool IsImport => Index < 0;
        public bool IsExport => Index > 0;

        // Original names were ToImport and ToExport but I prefer "As" to "To" for properties
        public int AsImport => -Index - 1;
        public int AsExport => Index - 1;
    }
}
