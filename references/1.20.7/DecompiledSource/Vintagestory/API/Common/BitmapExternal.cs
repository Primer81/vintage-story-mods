#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class BitmapExternal : BitmapRef
{
    public SKBitmap bmp;

    public override int Height => bmp.Height;

    public override int Width => bmp.Width;

    public override int[] Pixels => Array.ConvertAll(bmp.Pixels, (SKColor p) => (int)(uint)p);

    public nint PixelsPtrAndLock => bmp.GetPixels();

    public BitmapExternal()
    {
    }

    public BitmapExternal(int width, int height)
    {
        bmp = new SKBitmap(width, height);
    }

    public BitmapExternal(MemoryStream ms, ILogger logger, AssetLocation loc = null)
    {
        try
        {
            SKBitmap sKBitmap = SKBitmap.Decode(ms);
            bmp = new SKBitmap(sKBitmap.Width, sKBitmap.Height, SKColorType.Bgra8888, sKBitmap.Info.AlphaType);
            using SKCanvas sKCanvas = new SKCanvas(bmp);
            sKCanvas.DrawBitmap(sKBitmap, 0f, 0f);
        }
        catch (Exception e)
        {
            if (loc != null)
            {
                logger.Error("Failed loading bitmap from png file {0}. Will default to an empty 1x1 bitmap.", loc);
                logger.Error(e);
            }
            else
            {
                logger.Error("Failed loading bitmap. Will default to an empty 1x1 bitmap.");
                logger.Error(e);
            }

            bmp = new SKBitmap(1, 1);
            bmp.SetPixel(0, 0, SKColors.Orange);
        }
    }

    public BitmapExternal(string filePath)
    {
        try
        {
            SKBitmap sKBitmap = SKBitmap.Decode(filePath);
            bmp = new SKBitmap(sKBitmap.Width, sKBitmap.Height, SKColorType.Bgra8888, sKBitmap.Info.AlphaType);
            using SKCanvas sKCanvas = new SKCanvas(bmp);
            sKCanvas.DrawBitmap(sKBitmap, 0f, 0f);
        }
        catch (Exception)
        {
            bmp = new SKBitmap(1, 1);
            bmp.SetPixel(0, 0, SKColors.Orange);
        }
    }

    public BitmapExternal(Stream stream)
    {
        try
        {
            SKBitmap sKBitmap = SKBitmap.Decode(stream);
            bmp = new SKBitmap(sKBitmap.Width, sKBitmap.Height, SKColorType.Bgra8888, sKBitmap.Info.AlphaType);
            using SKCanvas sKCanvas = new SKCanvas(bmp);
            sKCanvas.DrawBitmap(sKBitmap, 0f, 0f);
        }
        catch (Exception)
        {
            bmp = new SKBitmap(1, 1);
            bmp.SetPixel(0, 0, SKColors.Orange);
        }
    }

    //
    // Summary:
    //     Create a BitmapExternal from a byte array
    //
    // Parameters:
    //   data:
    //
    //   dataLength:
    //
    //   logger:
    public BitmapExternal(byte[] data, int dataLength, ILogger logger)
    {
        try
        {
            if (RuntimeEnv.OS == OS.Mac)
            {
                SKBitmap sKBitmap = SKBitmap.Decode(new ReadOnlySpan<byte>(data, 0, dataLength));
                bmp = new SKBitmap(sKBitmap.Width, sKBitmap.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                using SKCanvas sKCanvas = new SKCanvas(bmp);
                sKCanvas.DrawBitmap(sKBitmap, 0f, 0f);
                return;
            }

            bmp = Decode(new ReadOnlySpan<byte>(data, 0, dataLength));
        }
        catch (Exception e)
        {
            logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
            logger.Error(e);
            bmp = new SKBitmap(1, 1);
            bmp.SetPixel(0, 0, SKColors.Orange);
        }
    }

    public unsafe static SKBitmap Decode(ReadOnlySpan<byte> buffer)
    {
        fixed (byte* address = buffer)
        {
            using SKData data = SKData.Create((nint)address, buffer.Length);
            using SKCodec sKCodec = SKCodec.Create(data);
            SKImageInfo info = sKCodec.Info;
            info.AlphaType = SKAlphaType.Unpremul;
            return SKBitmap.Decode(sKCodec, info);
        }
    }

    public override void Dispose()
    {
        bmp.Dispose();
    }

    public override void Save(string filename)
    {
        bmp.Save(filename);
    }

    //
    // Summary:
    //     Retrives the ARGB value from given coordinate
    //
    // Parameters:
    //   x:
    //
    //   y:
    public override SKColor GetPixel(int x, int y)
    {
        return bmp.GetPixel(x, y);
    }

    //
    // Summary:
    //     Retrives the ARGB value from given coordinate using normalized coordinates (0..1)
    //
    //
    // Parameters:
    //   x:
    //
    //   y:
    public override SKColor GetPixelRel(float x, float y)
    {
        return bmp.GetPixel((int)Math.Min(bmp.Width - 1, x * (float)bmp.Width), (int)Math.Min(bmp.Height - 1, y * (float)bmp.Height));
    }

    public unsafe override void MulAlpha(int alpha = 255)
    {
        int num = Width * Height;
        float num2 = (float)alpha / 255f;
        byte* ptr = (byte*)((IntPtr)bmp.GetPixels()).ToPointer();
        for (int i = 0; i < num; i++)
        {
            int num3 = ptr[3];
            *ptr = (byte)((float)(int)(*ptr) * num2);
            ptr[1] = (byte)((float)(int)ptr[1] * num2);
            ptr[2] = (byte)((float)(int)ptr[2] * num2);
            ptr[3] = (byte)((float)num3 * num2);
            ptr += 4;
        }
    }

    public override int[] GetPixelsTransformed(int rot = 0, int mulAlpha = 255)
    {
        int[] array = new int[Width * Height];
        int width = bmp.Width;
        int height = bmp.Height;
        FastBitmap fastBitmap = new FastBitmap();
        fastBitmap.bmp = bmp;
        int stride = fastBitmap.Stride;
        switch (rot)
        {
            case 0:
                {
                    for (int num4 = 0; num4 < height; num4++)
                    {
                        fastBitmap.GetPixelRow(width, num4 * stride, array, num4 * width);
                    }

                    break;
                }
            case 90:
                {
                    for (int k = 0; k < width; k++)
                    {
                        int num2 = k * width;
                        for (int l = 0; l < height; l++)
                        {
                            array[l + num2] = fastBitmap.GetPixel(width - k - 1, l * stride);
                        }
                    }

                    break;
                }
            case 180:
                {
                    for (int m = 0; m < height; m++)
                    {
                        int num3 = m * width;
                        int y = (height - m - 1) * stride;
                        for (int n = 0; n < width; n++)
                        {
                            array[n + num3] = fastBitmap.GetPixel(width - n - 1, y);
                        }
                    }

                    break;
                }
            case 270:
                {
                    for (int i = 0; i < width; i++)
                    {
                        int num = i * width;
                        for (int j = 0; j < height; j++)
                        {
                            array[j + num] = fastBitmap.GetPixel(i, (height - j - 1) * stride);
                        }
                    }

                    break;
                }
        }

        if (mulAlpha != 255)
        {
            float num5 = (float)mulAlpha / 255f;
            int num6 = 16777215;
            for (int num7 = 0; num7 < array.Length; num7++)
            {
                int num8 = array[num7];
                uint num9 = (uint)num8 >> 24;
                num8 &= num6;
                array[num7] = num8 | ((int)((float)num9 * num5) << 24);
            }
        }

        return array;
    }
}
#if false // Decompilation log
'181' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
