#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

//
// Summary:
//     Represent a font with sizing and styling for use in rendering text
public class CairoFont : FontConfig, IDisposable
{
    private static ImageSurface surface;

    //
    // Summary:
    //     The static Context for all Cairo Fonts.
    public static Context FontMeasuringContext;

    //
    // Summary:
    //     Whether or not the font is rendered twice.
    public bool RenderTwice;

    public double LineHeightMultiplier = 1.0;

    private FontOptions CairoFontOptions;

    public FontSlant Slant;

    public EnumTextOrientation Orientation;

    static CairoFont()
    {
        surface = new ImageSurface(Format.Argb32, 1, 1);
        FontMeasuringContext = new Context(surface);
    }

    //
    // Summary:
    //     Creates an empty CairoFont instance.
    public CairoFont()
    {
    }

    //
    // Summary:
    //     Creates a pre-populated CairoFont instance.
    //
    // Parameters:
    //   config:
    //     The configuration for the CairoFont
    public CairoFont(FontConfig config)
    {
        UnscaledFontsize = config.UnscaledFontsize;
        Fontname = config.Fontname;
        FontWeight = config.FontWeight;
        Color = config.Color;
        StrokeColor = config.StrokeColor;
        StrokeWidth = config.StrokeWidth;
    }

    //
    // Summary:
    //     Creates a CairoFont object.
    //
    // Parameters:
    //   unscaledFontSize:
    //     The size of the font before scaling is applied.
    //
    //   fontName:
    //     The name of the font.
    public CairoFont(double unscaledFontSize, string fontName)
    {
        UnscaledFontsize = unscaledFontSize;
        Fontname = fontName;
    }

    public CairoFont WithLineHeightMultiplier(double lineHeightMul)
    {
        LineHeightMultiplier = lineHeightMul;
        return this;
    }

    public CairoFont WithStroke(double[] color, double width)
    {
        StrokeColor = color;
        StrokeWidth = width;
        return this;
    }

    //
    // Summary:
    //     Creates a CairoFont object
    //
    // Parameters:
    //   unscaledFontSize:
    //     The size of the font before scaling is applied.
    //
    //   fontName:
    //     The name of the font.
    //
    //   color:
    //     The color of the font.
    //
    //   strokeColor:
    //     The color for the stroke of the font. (Default: Null)
    public CairoFont(double unscaledFontSize, string fontName, double[] color, double[] strokeColor = null)
    {
        UnscaledFontsize = unscaledFontSize;
        Fontname = fontName;
        Color = color;
        StrokeColor = strokeColor;
        if (StrokeColor != null)
        {
            StrokeWidth = 1.0;
        }
    }

    //
    // Summary:
    //     Adjust font size so that it fits given bounds
    //
    // Parameters:
    //   text:
    //     The text of the object.
    //
    //   bounds:
    //     The bounds of the element where the font is displayed.
    //
    //   onlyShrink:
    public void AutoFontSize(string text, ElementBounds bounds, bool onlyShrink = true)
    {
        double unscaledFontsize = UnscaledFontsize;
        UnscaledFontsize = 50.0;
        UnscaledFontsize *= (bounds.InnerWidth - 1.0) / GetTextExtents(text).Width;
        if (onlyShrink)
        {
            UnscaledFontsize = Math.Min(UnscaledFontsize, unscaledFontsize);
        }
    }

    //
    // Summary:
    //     Adjust the bounds so that it fits given text in one line
    //
    // Parameters:
    //   text:
    //     The text to adjust
    //
    //   bounds:
    //     The bounds to adjust the text to.
    //
    //   onlyGrow:
    //     If true, the box will not be made smaller
    public void AutoBoxSize(string text, ElementBounds bounds, bool onlyGrow = false)
    {
        double num = 0.0;
        double num2 = 0.0;
        FontExtents fontExtents = GetFontExtents();
        if (text.Contains('\n'))
        {
            string[] array = text.Split('\n');
            for (int i = 0; i < array.Length && (array[i].Length != 0 || i != array.Length - 1); i++)
            {
                num = Math.Max(num, GetTextExtents(array[i]).Width);
                num2 += fontExtents.Height;
            }
        }
        else
        {
            num = GetTextExtents(text).Width;
            num2 = fontExtents.Height;
        }

        if (text.Length == 0)
        {
            num = 0.0;
            num2 = 0.0;
        }

        if (onlyGrow)
        {
            bounds.fixedWidth = Math.Max(bounds.fixedWidth, num / (double)RuntimeEnv.GUIScale + 1.0);
            bounds.fixedHeight = Math.Max(bounds.fixedHeight, num2 / (double)RuntimeEnv.GUIScale);
        }
        else
        {
            bounds.fixedWidth = Math.Max(1.0, num / (double)RuntimeEnv.GUIScale + 1.0);
            bounds.fixedHeight = Math.Max(1.0, num2 / (double)RuntimeEnv.GUIScale);
        }
    }

    //
    // Summary:
    //     Sets the color of the CairoFont.
    //
    // Parameters:
    //   color:
    //     The color to set.
    public CairoFont WithColor(double[] color)
    {
        Color = (double[])color.Clone();
        return this;
    }

    //
    // Summary:
    //     Adds a weight to the font.
    //
    // Parameters:
    //   weight:
    //     The weight of the font.
    public CairoFont WithWeight(FontWeight weight)
    {
        FontWeight = weight;
        return this;
    }

    //
    // Summary:
    //     Sets the font to render twice.
    public CairoFont WithRenderTwice()
    {
        RenderTwice = true;
        return this;
    }

    public CairoFont WithSlant(FontSlant slant)
    {
        Slant = slant;
        return this;
    }

    public CairoFont WithFont(string fontname)
    {
        Fontname = fontname;
        return this;
    }

    //
    // Summary:
    //     Sets up the context. Must be executed in the main thread, as it is not thread
    //     safe.
    //
    // Parameters:
    //   ctx:
    //     The context to set up the CairoFont with.
    public void SetupContext(Context ctx)
    {
        ctx.SetFontSize(GuiElement.scaled(UnscaledFontsize));
        ctx.SelectFontFace(Fontname, Slant, FontWeight);
        CairoFontOptions = new FontOptions();
        CairoFontOptions.Antialias = Antialias.Subpixel;
        ctx.FontOptions = CairoFontOptions;
        if (Color != null)
        {
            if (Color.Length == 3)
            {
                ctx.SetSourceRGB(Color[0], Color[1], Color[2]);
            }

            if (Color.Length == 4)
            {
                ctx.SetSourceRGBA(Color[0], Color[1], Color[2], Color[3]);
            }
        }
    }

    //
    // Summary:
    //     Gets the font's extents.
    //
    // Returns:
    //     The FontExtents for this particular font.
    public FontExtents GetFontExtents()
    {
        SetupContext(FontMeasuringContext);
        return FontMeasuringContext.FontExtents;
    }

    //
    // Summary:
    //     Gets the extents of the text.
    //
    // Parameters:
    //   text:
    //     The text to extend.
    //
    // Returns:
    //     The Text extends for this font with this text.
    public TextExtents GetTextExtents(string text)
    {
        SetupContext(FontMeasuringContext);
        return FontMeasuringContext.TextExtents(text);
    }

    //
    // Summary:
    //     Clone function. Creates a duplicate of this Cairofont.
    //
    // Returns:
    //     The duplicate font.
    public CairoFont Clone()
    {
        CairoFont cairoFont = (CairoFont)MemberwiseClone();
        cairoFont.Color = new double[Color.Length];
        Array.Copy(Color, cairoFont.Color, Color.Length);
        return cairoFont;
    }

    //
    // Summary:
    //     Sets the base size of the CairoFont.
    //
    // Parameters:
    //   fontSize:
    //     The new font size
    public CairoFont WithFontSize(float fontSize)
    {
        UnscaledFontsize = fontSize;
        return this;
    }

    public static CairoFont SmallButtonText(EnumButtonStyle style = EnumButtonStyle.Normal)
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.ButtonTextColor.Clone(),
            FontWeight = ((style != EnumButtonStyle.Small) ? FontWeight.Bold : FontWeight.Normal),
            Orientation = EnumTextOrientation.Center,
            Fontname = GuiStyle.StandardFontName,
            UnscaledFontsize = GuiStyle.SmallFontSize
        };
    }

    //
    // Summary:
    //     Creates a Button Text preset.
    //
    // Returns:
    //     The button text preset.
    public static CairoFont ButtonText()
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.ButtonTextColor.Clone(),
            FontWeight = FontWeight.Bold,
            Orientation = EnumTextOrientation.Center,
            Fontname = GuiStyle.DecorativeFontName,
            UnscaledFontsize = 24.0
        };
    }

    //
    // Summary:
    //     Creates a text preset for when the button is pressed.
    //
    // Returns:
    //     The text preset for a pressed button.
    public static CairoFont ButtonPressedText()
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.ActiveButtonTextColor.Clone(),
            FontWeight = FontWeight.Bold,
            Fontname = GuiStyle.DecorativeFontName,
            Orientation = EnumTextOrientation.Center,
            UnscaledFontsize = 24.0
        };
    }

    public CairoFont WithOrientation(EnumTextOrientation orientation)
    {
        Orientation = orientation;
        return this;
    }

    //
    // Summary:
    //     Creates a text preset for text input fields.
    //
    // Returns:
    //     The text field input preset.
    public static CairoFont TextInput()
    {
        CairoFont cairoFont = new CairoFont();
        cairoFont.Color = new double[4] { 1.0, 1.0, 1.0, 0.9 };
        cairoFont.Fontname = GuiStyle.StandardFontName;
        cairoFont.UnscaledFontsize = 18.0;
        return cairoFont;
    }

    //
    // Summary:
    //     Creates a text oreset for smaller text input fields.
    //
    // Returns:
    //     The smaller text input preset.
    public static CairoFont SmallTextInput()
    {
        CairoFont cairoFont = new CairoFont();
        cairoFont.Color = new double[4] { 0.0, 0.0, 0.0, 0.9 };
        cairoFont.Fontname = GuiStyle.StandardFontName;
        cairoFont.UnscaledFontsize = GuiStyle.SmallFontSize;
        return cairoFont;
    }

    //
    // Summary:
    //     Creates a white text for medium dialog.
    //
    // Returns:
    //     The white text for medium dialog.
    public static CairoFont WhiteMediumText()
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
            Fontname = GuiStyle.StandardFontName,
            UnscaledFontsize = GuiStyle.NormalFontSize
        };
    }

    //
    // Summary:
    //     Creates a white text for smallish dialogs.
    //
    // Returns:
    //     The white text for small dialogs.
    public static CairoFont WhiteSmallishText()
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
            Fontname = GuiStyle.StandardFontName,
            UnscaledFontsize = GuiStyle.SmallishFontSize
        };
    }

    //
    // Summary:
    //     Creates a white text for smallish dialogs, using the specified base font
    //
    // Parameters:
    //   baseFont:
    public static CairoFont WhiteSmallishText(string baseFont)
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
            Fontname = baseFont,
            UnscaledFontsize = GuiStyle.SmallishFontSize
        };
    }

    //
    // Summary:
    //     Creates a white text for small dialogs.
    //
    // Returns:
    //     The white text for small dialogs
    public static CairoFont WhiteSmallText()
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
            Fontname = GuiStyle.StandardFontName,
            UnscaledFontsize = GuiStyle.SmallFontSize
        };
    }

    //
    // Summary:
    //     Creates a white text for details.
    //
    // Returns:
    //     A white text for details.
    public static CairoFont WhiteDetailText()
    {
        return new CairoFont
        {
            Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
            Fontname = GuiStyle.StandardFontName,
            UnscaledFontsize = GuiStyle.DetailFontSize
        };
    }

    public void Dispose()
    {
        CairoFontOptions.Dispose();
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
