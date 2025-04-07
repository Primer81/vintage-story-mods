#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Cairo;

namespace Vintagestory.API.Client;

public class TextTextureUtil
{
    private TextBackground defaultBackground = new TextBackground();

    private ICoreClientAPI capi;

    //
    // Summary:
    //     Text Texture Util constructor.
    //
    // Parameters:
    //   capi:
    //     The Client API.
    public TextTextureUtil(ICoreClientAPI capi)
    {
        this.capi = capi;
    }

    //
    // Summary:
    //     Takes a string of text and applies a texture to it.
    //
    // Parameters:
    //   text:
    //     The text to texture.
    //
    //   font:
    //     The font of the text.
    //
    //   width:
    //     The width of the text.
    //
    //   height:
    //     The height of the text.
    //
    //   background:
    //     The background of the text. (default: none/null)
    //
    //   orientation:
    //     The orientation of the text. (default: left)
    //
    //   demulAlpha:
    //
    // Returns:
    //     The texturized text.
    public LoadedTexture GenTextTexture(string text, CairoFont font, int width, int height, TextBackground background = null, EnumTextOrientation orientation = EnumTextOrientation.Left, bool demulAlpha = false)
    {
        LoadedTexture loadedTexture = new LoadedTexture(capi);
        GenOrUpdateTextTexture(text, font, width, height, ref loadedTexture, background, orientation);
        return loadedTexture;
    }

    //
    // Summary:
    //     Takes a texture and applies some text to it.
    //
    // Parameters:
    //   text:
    //     The text to texture.
    //
    //   font:
    //     The font of the text.
    //
    //   width:
    //     The width of the text.
    //
    //   height:
    //     The height of the text.
    //
    //   loadedTexture:
    //     The texture to be loaded on to.
    //
    //   background:
    //     The background of the text. (default: none/null)
    //
    //   orientation:
    //     The orientation of the text. (default: left)
    //
    //   demulAlpha:
    public void GenOrUpdateTextTexture(string text, CairoFont font, int width, int height, ref LoadedTexture loadedTexture, TextBackground background = null, EnumTextOrientation orientation = EnumTextOrientation.Left, bool demulAlpha = false)
    {
        if (background == null)
        {
            background = defaultBackground;
        }

        ElementBounds bounds = new ElementBounds().WithFixedSize(width, height);
        ImageSurface imageSurface = new ImageSurface(Format.Argb32, width, height);
        Context context = new Context(imageSurface);
        GuiElementTextBase guiElementTextBase = new GuiElementTextBase(capi, text, font, bounds);
        context.SetSourceRGBA(background.FillColor);
        GuiElement.RoundRectangle(context, 0.0, 0.0, width, height, background.Radius);
        if (background.BorderWidth > 0.0)
        {
            context.FillPreserve();
            context.Operator = Operator.Atop;
            context.LineWidth = background.BorderWidth;
            context.SetSourceRGBA(background.BorderColor);
            context.Stroke();
            context.Operator = Operator.Over;
        }
        else
        {
            context.Fill();
        }

        guiElementTextBase.textUtil.AutobreakAndDrawMultilineTextAt(context, font, text, background.HorPadding, background.VerPadding, width, orientation);
        if (demulAlpha)
        {
            imageSurface.DemulAlpha();
        }

        capi.Gui.LoadOrUpdateCairoTexture(imageSurface, linearMag: false, ref loadedTexture);
        imageSurface.Dispose();
        context.Dispose();
    }

    //
    // Summary:
    //     Takes a string of text and applies a texture to it.
    //
    // Parameters:
    //   text:
    //     The text to texture.
    //
    //   font:
    //     The font of the text.
    //
    //   width:
    //     The width of the text.
    //
    //   height:
    //     The height of the text.
    //
    //   background:
    //     The background of the text. (default: none/null)
    //
    // Returns:
    //     The texturized text.
    public LoadedTexture GenTextTexture(string text, CairoFont font, int width, int height, TextBackground background = null)
    {
        if (background == null)
        {
            background = defaultBackground;
        }

        ImageSurface imageSurface = new ImageSurface(Format.Argb32, width, height);
        Context context = new Context(imageSurface);
        if (background?.FillColor != null)
        {
            context.SetSourceRGBA(background.FillColor);
            GuiElement.RoundRectangle(context, 0.0, 0.0, width, height, background.Radius);
            context.Fill();
        }

        if (background != null && background.Shade)
        {
            context.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.4, GuiStyle.DialogStrongBgColor[1] * 1.4, GuiStyle.DialogStrongBgColor[2] * 1.4, 1.0);
            context.LineWidth = 5.0;
            GuiElement.RoundRectangle(context, 0.0, 0.0, width, height, background.Radius);
            context.StrokePreserve();
            imageSurface.BlurFull(6.2);
            context.SetSourceRGBA(new double[4]
            {
                0.17647058823529413,
                7.0 / 51.0,
                11.0 / 85.0,
                1.0
            });
            context.LineWidth = background.BorderWidth;
            context.Stroke();
        }

        if (background?.BorderColor != null)
        {
            context.SetSourceRGBA(background.BorderColor);
            GuiElement.RoundRectangle(context, 0.0, 0.0, width, height, background.Radius);
            context.LineWidth = background.BorderWidth;
            context.Stroke();
        }

        font.SetupContext(context);
        double height2 = font.GetFontExtents().Height;
        string[] array = text.Split('\n');
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = array[i].TrimEnd();
            context.MoveTo(background.HorPadding, (double)background.VerPadding + context.FontExtents.Ascent + (double)i * height2);
            if (font.StrokeWidth > 0.0)
            {
                context.TextPath(array[i]);
                context.LineWidth = font.StrokeWidth;
                context.SetSourceRGBA(font.StrokeColor);
                context.StrokePreserve();
                context.SetSourceRGBA(font.Color);
                context.Fill();
            }
            else
            {
                context.ShowText(array[i]);
                if (font.RenderTwice)
                {
                    context.ShowText(array[i]);
                }
            }
        }

        int textureId = capi.Gui.LoadCairoTexture(imageSurface, linearMag: true);
        imageSurface.Dispose();
        context.Dispose();
        return new LoadedTexture(capi)
        {
            TextureId = textureId,
            Width = width,
            Height = height
        };
    }

    //
    // Summary:
    //     Takes a string of text and applies a texture to it.
    //
    // Parameters:
    //   text:
    //     The text to texture.
    //
    //   font:
    //     The font of the text.
    //
    //   background:
    //     The background of the text. (default: none/null)
    //
    // Returns:
    //     The texturized text.
    public LoadedTexture GenTextTexture(string text, CairoFont font, TextBackground background = null)
    {
        LoadedTexture loadedTexture = new LoadedTexture(capi);
        GenOrUpdateTextTexture(text, font, ref loadedTexture, background);
        return loadedTexture;
    }

    //
    // Summary:
    //     Takes a texture and applies some text to it.
    //
    // Parameters:
    //   text:
    //     The text to texture.
    //
    //   font:
    //     The font of the text.
    //
    //   loadedTexture:
    //     The texture to be loaded on to.
    //
    //   background:
    //     The background of the text. (default: none/null)
    public void GenOrUpdateTextTexture(string text, CairoFont font, ref LoadedTexture loadedTexture, TextBackground background = null)
    {
        if (background == null)
        {
            background = defaultBackground.Clone();
            if (font.StrokeWidth > 0.0)
            {
                background.Padding = (int)Math.Ceiling(font.StrokeWidth);
            }
        }

        ElementBounds elementBounds = new ElementBounds();
        font.AutoBoxSize(text, elementBounds);
        int width = (int)Math.Ceiling(GuiElement.scaled(elementBounds.fixedWidth + 1.0 + (double)(2 * background.HorPadding)));
        int height = (int)Math.Ceiling(GuiElement.scaled(elementBounds.fixedHeight + 1.0 + (double)(2 * background.VerPadding)));
        GenOrUpdateTextTexture(text, font, width, height, ref loadedTexture, background);
    }

    //
    // Summary:
    //     Generates an unscaled text texture.
    //
    // Parameters:
    //   text:
    //     The text to texture.
    //
    //   font:
    //     The font of the text.
    //
    //   background:
    //     The background of the text (Default: none/null)
    //
    // Returns:
    //     The loaded unscaled texture.
    public LoadedTexture GenUnscaledTextTexture(string text, CairoFont font, TextBackground background = null)
    {
        if (background == null)
        {
            background = defaultBackground;
        }

        double num = 0.0;
        string[] array = text.Split('\n');
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = array[i].TrimEnd();
            num = Math.Max(font.GetTextExtents(array[i]).Width, num);
        }

        FontExtents fontExtents = font.GetFontExtents();
        int width = (int)num + 1 + 2 * background.HorPadding;
        int height = (int)fontExtents.Height * array.Length + 1 + 2 * background.VerPadding;
        return GenTextTexture(text, font, width, height, background);
    }

    //
    // Summary:
    //     Takes a string of text and applies a texture to it.
    //
    // Parameters:
    //   text:
    //     The text to texture.
    //
    //   font:
    //     The font of the text.
    //
    //   maxWidth:
    //     The maximum width of the text.
    //
    //   background:
    //     The background of the text. (default: none/null)
    //
    //   orientation:
    //     The orientation of the text. (default: left)
    //
    // Returns:
    //     The texturized text.
    public LoadedTexture GenTextTexture(string text, CairoFont font, int maxWidth, TextBackground background = null, EnumTextOrientation orientation = EnumTextOrientation.Left)
    {
        if (background == null)
        {
            background = defaultBackground;
        }

        double val = 0.0;
        string[] array = text.Split('\n');
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = array[i].TrimEnd();
            val = Math.Max(font.GetTextExtents(array[i]).Width, val);
        }

        int num = (int)Math.Min(maxWidth, val) + 2 * background.HorPadding;
        double num2 = new TextDrawUtil().GetMultilineTextHeight(font, text, num) + (double)(2 * background.VerPadding);
        return GenTextTexture(text, font, num, (int)num2 + 1, background, orientation);
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
