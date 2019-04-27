using System;
using System.IO;
using SkiaSharp;
using static PakReader.AssetReader;

namespace PakReader
{
    class ImageExporter
    {
        public readonly int Width;
        public readonly int Height;

        public readonly byte[] Data;

        public ImageExporter(string pixel_format, FTexture2DMipMap inp)
        {
            Width = inp.size_x;
            Height = inp.size_y;
            Data = inp.data.data;
        }

        public static SKImage GetImage(FTexture2DMipMap inp, string pixel_format)
        {
            byte[] decoded;
            SKColorType color;
            switch (pixel_format)
            {
                case "PF_DXT5":
                    decoded = DDSImage.DecompressDXT5(inp.data.data, DDSImage.PixelFormat.DXT5, (uint)inp.size_x, (uint)inp.size_y, (uint)inp.size_z);
                    color = SKColorType.Rgba8888;
                    break;
                case "PF_DXT1":
                    decoded = DDSImage.DecompressDXT1(inp.data.data, DDSImage.PixelFormat.DXT1, (uint)inp.size_x, (uint)inp.size_y, (uint)inp.size_z);
                    color = SKColorType.Rgba8888;
                    break;
                case "PF_B8G8R8A8":
                    decoded = inp.data.data;
                    color = SKColorType.Bgra8888;
                    break;
                case "PF_BC5":
                default:
                    throw new IOException("Unknown image type: " + pixel_format);
            }
            var info = new SKImageInfo(inp.size_x, inp.size_y, color, SKAlphaType.Unpremul);
            using (SKBitmap bitmap = new SKBitmap(info))
            {
                unsafe
                {
                    fixed (byte* p = decoded)
                    {
                        bitmap.SetPixels(new IntPtr(p));
                    }
                }

                return SKImage.FromBitmap(bitmap);/*
                using (var b = bitmap.Resize(new SKImageInfo(256, 256), SKBitmapResizeMethod.Lanczos3))
                {
                    var img = SKImage.FromBitmap(b);
                    //string filename = RandomString(8) + ".png";
                    //File.WriteAllBytes(filename, img.Encode().ToArray());
                    //Console.WriteLine(filename);
                }*/
            }
        }

        static Random r = new Random();
        static string RandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[r.Next(chars.Length)];
            }

            return new string(stringChars);
        }

        /*void DecodeBC5(byte[] inp, uint width, uint height)
        {
            byte[] ret = new byte[width * height * 3];
            BinaryReader reader = new BinaryReader(new MemoryStream(inp));
            for (int y_block = 0; y_block < height / 4; y_block++)
            {
                for (int x_block = 0; x_block < width / 4; x_block++)
                {
                    var r_bytes = DecodeBC3Block(reader);
                    var g_bytes = DecodeBC3Block(reader);
                }
            }
        }

        byte[] DecodeBC3Block(BinaryReader reader)
        {
            float ref0 = reader.ReadByte();
            float ref1 = reader.ReadByte();

            float[] ref_sl = new float[8];
            ref_sl[0] = ref0;
            ref_sl[1] = ref1;

            if (ref0 > ref1)
            {
                ref_sl[2] = (6 * ref0 + 1 * ref1) / 7;
                ref_sl[3] = (5 * ref0 + 2 * ref1) / 7;
                ref_sl[4] = (4 * ref0 + 3 * ref1) / 7;
                ref_sl[5] = (3 * ref0 + 4 * ref1) / 7;
                ref_sl[6] = (2 * ref0 + 5 * ref1) / 7;
                ref_sl[7] = (1 * ref0 + 6 * ref1) / 7;
            }
            else
            {
                ref_sl[2] = (4 * ref0 + 1 * ref1) / 5;
                ref_sl[3] = (3 * ref0 + 2 * ref1) / 5;
                ref_sl[4] = (2 * ref0 + 3 * ref1) / 5;
                ref_sl[5] = (1 * ref0 + 4 * ref1) / 5;
                ref_sl[6] = 0;
                ref_sl[7] = 255;
            }

            byte index_block1 = reader.ReadBytes(3);
        }

        void GetBC3Indices(byte[] buf_block)
        {
            byte[] buf_test = new byte[]
            {
                buf_block[2], buf_block[1], buf_block[0]
            };

            var bits = new BitArray(buf_test);

            byte[] indices = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                indicies[i] = bits.Get()
            }
        }*/
    }
}
