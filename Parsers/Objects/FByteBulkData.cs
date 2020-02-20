using System.IO;

namespace PakReader.Parsers.Objects
{
    public readonly struct FByteBulkData
    {
        // Memory saving, we don't need this
        //uint BulkDataFlags;
        //long ElementCount;
        //long BulkDataOffsetInFile;
        //long BulkDataSizeOnDisk;

        public readonly byte[] Data;

        internal FByteBulkData(BinaryReader reader, Stream ubulk, int bulkOffset)
        {
            var BulkDataFlags = reader.ReadUInt32();

            bool LongBits = (BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_Size64Bit) != 0;

            var ElementCount = LongBits ? reader.ReadInt64() : reader.ReadInt32();
            var BulkDataSizeOnDisk = LongBits ? reader.ReadInt64() : reader.ReadInt32();
            var BulkDataOffsetInFile = reader.ReadInt64();

            Data = null;
            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_ForceInlinePayload) != 0)
            {
                Data = reader.ReadBytes((int)ElementCount);
            }
            
            if ((BulkDataFlags & (uint)EBulkDataFlags.BULKDATA_PayloadInSeperateFile) != 0)
            {
                ubulk.Position = BulkDataOffsetInFile + bulkOffset;
                Data = new byte[ElementCount];
                ubulk.Read(Data, 0, (int)ElementCount);
            }
        }
    }
}
