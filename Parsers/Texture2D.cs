using System.Collections.Generic;
using System.IO;
using PakReader.Parsers.Objects;
using SkiaSharp;

namespace PakReader.Parsers
{
    public sealed class Texture2D : UObject
    {
        public FTexturePlatformData[] PlatformDatas { get; }

        SKImage image;
        public SKImage Image {
            get
            {
                if (image == null)
                {
                    var mip = PlatformDatas[0].Mips[0];
                    image = TextureDecoder.DecodeImage(mip.BulkData.Data, mip.SizeX, mip.SizeY, mip.SizeZ, PlatformDatas[0].PixelFormat);
                }
                return image;
            }
        }

        internal Texture2D(PackageReader reader, Stream ubulk, int bulkOffset) : base(reader)
        {
            new FStripDataFlags(reader); // and I quote, "still no idea"
            new FStripDataFlags(reader); // "why there are two" :)

            if (reader.ReadInt32() != 0) // bIsCooked
            {
                var data = new List<FTexturePlatformData>(1); // Probably gonna be only one texture anyway
                var PixelFormatName = reader.ReadFName();
                while (!PixelFormatName.IsNone)
                {
                    long SkipOffset = reader.ReadInt64();
                    data.Add(new FTexturePlatformData(reader, ubulk, bulkOffset));
                    PixelFormatName = reader.ReadFName();
                }
                PlatformDatas = data.ToArray();
            }
        }
    }
}
