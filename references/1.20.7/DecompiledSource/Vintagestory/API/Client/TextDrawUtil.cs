#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class TextDrawUtil
{
    private int caretPos;

    private bool gotLinebreak;

    private bool gotSpace;

    public TextLine[] Lineize(Context ctx, string text, double boxwidth, double lineHeightMultiplier = 1.0, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default, bool keepLinebreakChar = false)
    {
        return Lineize(ctx, text, linebreak, new TextFlowPath[1]
        {
            new TextFlowPath(boxwidth)
        }, 0.0, 0.0, lineHeightMultiplier, keepLinebreakChar);
    }

    public int GetQuantityTextLines(CairoFont font, string text, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default)
    {
        return GetQuantityTextLines(font, text, linebreak, new TextFlowPath[1]
        {
            new TextFlowPath(boxWidth)
        });
    }

    public double GetMultilineTextHeight(CairoFont font, string text, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default)
    {
        return (double)GetQuantityTextLines(font, text, boxWidth, linebreak) * GetLineHeight(font);
    }

    public TextLine[] Lineize(CairoFont font, string fulltext, double boxWidth, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.Default, bool keepLinebreakChar = false)
    {
        return Lineize(font, fulltext, linebreak, new TextFlowPath[1]
        {
            new TextFlowPath(boxWidth)
        }, 0.0, 0.0, keepLinebreakChar);
    }

    //
    // Summary:
    //     Use Matrix transformation to move the draw position
    //
    // Parameters:
    //   ctx:
    //     The context of the text.
    //
    //   font:
    //     The font of the text.
    //
    //   text:
    //     The text itself.
    //
    //   boxWidth:
    //     The width of the box containing the text.
    //
    //   orientation:
    //     The orientation of the text.
    public void AutobreakAndDrawMultilineText(Context ctx, CairoFont font, string text, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
    {
        AutobreakAndDrawMultilineText(ctx, font, text, 0.0, 0.0, new TextFlowPath[1]
        {
            new TextFlowPath(boxWidth)
        }, orientation);
    }

    //
    // Summary:
    //     Draws the text with matrix transformations.
    //
    // Parameters:
    //   ctx:
    //     The context of the text.
    //
    //   font:
    //     The font of the text.
    //
    //   text:
    //     The text itself.
    //
    //   posX:
    //     The X position of the text.
    //
    //   posY:
    //     The Y position of the text.
    //
    //   boxWidth:
    //     The width of the box containing the text.
    //
    //   orientation:
    //     The orientation of the text.
    //
    // Returns:
    //     The new height of the text.
    public double AutobreakAndDrawMultilineTextAt(Context ctx, CairoFont font, string text, double posX, double posY, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
    {
        ctx.Save();
        Matrix matrix = ctx.Matrix;
        matrix.Translate((int)posX, (int)posY);
        ctx.Matrix = matrix;
        double result = AutobreakAndDrawMultilineText(ctx, font, text, 0.0, 0.0, new TextFlowPath[1]
        {
            new TextFlowPath(boxWidth)
        }, orientation);
        ctx.Restore();
        return result;
    }

    //
    // Summary:
    //     Draws the text with pre-set breaks.
    //
    // Parameters:
    //   ctx:
    //     The context of the text.
    //
    //   font:
    //     The font of the text.
    //
    //   lines:
    //     The lines of text.
    //
    //   posX:
    //     The X position of the text.
    //
    //   posY:
    //     The Y position of the text.
    //
    //   boxWidth:
    //     The width of the box containing the text.
    //
    //   orientation:
    //     The orientation of the text.
    public void DrawMultilineTextAt(Context ctx, CairoFont font, TextLine[] lines, double posX, double posY, double boxWidth, EnumTextOrientation orientation = EnumTextOrientation.Left)
    {
        ctx.Save();
        Matrix matrix = ctx.Matrix;
        matrix.Translate(posX, posY);
        ctx.Matrix = matrix;
        font.SetupContext(ctx);
        DrawMultilineText(ctx, font, lines, orientation);
        ctx.Restore();
    }

    //
    // Summary:
    //     Gets the height of the font to calculate the height of the line.
    //
    // Parameters:
    //   font:
    //     The font to calculate from.
    //
    // Returns:
    //     The height of the line.
    public double GetLineHeight(CairoFont font)
    {
        return font.GetFontExtents().Height * font.LineHeightMultiplier;
    }

    //
    // Summary:
    //     Gets the number of lines of text.
    //
    // Parameters:
    //   font:
    //     The font of the text.
    //
    //   text:
    //     The text itself.
    //
    //   linebreak:
    //
    //   flowPath:
    //     The path for the text.
    //
    //   lineY:
    //     The height of the line
    //
    // Returns:
    //     The number of lines.
    public int GetQuantityTextLines(CairoFont font, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double lineY = 0.0)
    {
        if (text == null || text.Length == 0)
        {
            return 0;
        }

        ImageSurface imageSurface = new ImageSurface(Format.Argb32, 1, 1);
        Context context = new Context(imageSurface);
        font.SetupContext(context);
        int result = Lineize(context, text, linebreak, flowPath, 0.0, lineY, font.LineHeightMultiplier).Length;
        context.Dispose();
        imageSurface.Dispose();
        return result;
    }

    //
    // Summary:
    //     Get the final height of the text.
    //
    // Parameters:
    //   font:
    //     The font of the text.
    //
    //   text:
    //     The text itself.
    //
    //   linebreak:
    //
    //   flowPath:
    //     The path for the text.
    //
    //   lineY:
    //     The height of the line
    //
    // Returns:
    //     The final height of the text.
    public double GetMultilineTextHeight(CairoFont font, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double lineY = 0.0)
    {
        return (double)GetQuantityTextLines(font, text, linebreak, flowPath, lineY) * GetLineHeight(font);
    }

    //
    // Summary:
    //     Turns the supplied text into line of text constrained by supplied flow path and
    //     starting at supplied start coordinates
    //
    // Parameters:
    //   font:
    //     The font of the text.
    //
    //   fulltext:
    //     The text of the lines.
    //
    //   linebreak:
    //
    //   flowPath:
    //     The flow direction of text.
    //
    //   startOffsetX:
    //     The offset start position for X
    //
    //   startY:
    //     The offset start position for Y
    //
    //   keepLinebreakChar:
    //
    // Returns:
    //     The text broken up into lines.
    public TextLine[] Lineize(CairoFont font, string fulltext, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double startOffsetX = 0.0, double startY = 0.0, bool keepLinebreakChar = false)
    {
        if (fulltext == null || fulltext.Length == 0)
        {
            return new TextLine[0];
        }

        ImageSurface imageSurface = new ImageSurface(Format.Argb32, 1, 1);
        Context context = new Context(imageSurface);
        font.SetupContext(context);
        TextLine[] result = Lineize(context, fulltext, linebreak, flowPath, startOffsetX, startY, font.LineHeightMultiplier, keepLinebreakChar);
        context.Dispose();
        imageSurface.Dispose();
        return result;
    }

    //
    // Summary:
    //     Turns the supplied text into line of text constrained by supplied flow path and
    //     starting at supplied start coordinates
    //
    // Parameters:
    //   ctx:
    //     Contexts of the GUI.
    //
    //   text:
    //     The text to be split
    //
    //   linebreak:
    //
    //   flowPath:
    //     Sets the general flow of text.
    //
    //   startOffsetX:
    //     The offset start position for X
    //
    //   startY:
    //     The offset start position for Y
    //
    //   lineHeightMultiplier:
    //
    //   keepLinebreakChar:
    //
    // Returns:
    //     The text broken up into lines.
    public TextLine[] Lineize(Context ctx, string text, EnumLinebreakBehavior linebreak, TextFlowPath[] flowPath, double startOffsetX = 0.0, double startY = 0.0, double lineHeightMultiplier = 1.0, bool keepLinebreakChar = false)
    {
        if (text == null || text.Length == 0)
        {
            return new TextLine[0];
        }

        if (linebreak == EnumLinebreakBehavior.Default)
        {
            linebreak = Lang.AvailableLanguages[Lang.CurrentLocale].LineBreakBehavior;
        }

        StringBuilder stringBuilder = new StringBuilder();
        List<TextLine> list = new List<TextLine>();
        caretPos = 0;
        double num = ctx.FontExtents.Height * lineHeightMultiplier;
        double num2 = startOffsetX;
        double num3 = startY;
        string text2;
        TextFlowPath textFlowPath;
        double num4;
        while ((text2 = getNextWord(text, linebreak)) != null)
        {
            string text3 = ((gotLinebreak || caretPos >= text.Length || !gotSpace) ? "" : " ");
            double width = ctx.TextExtents(stringBuilder?.ToString() + text2 + text3).Width;
            textFlowPath = GetCurrentFlowPathSection(flowPath, num3);
            if (textFlowPath == null)
            {
                Console.WriteLine("Flow path underflow. Something in the text flow system is incorrectly programmed.");
                textFlowPath = new TextFlowPath(500.0);
            }

            num4 = textFlowPath.X2 - textFlowPath.X1 - num2;
            if (width >= num4)
            {
                if (text2.Length > 0 && stringBuilder.Length == 0 && startOffsetX == 0.0)
                {
                    int num5 = 500;
                    while (text2.Length > 0 && width >= num4 && num5-- > 0)
                    {
                        text2 = text2.Substring(0, text2.Length - 1);
                        width = ctx.TextExtents(stringBuilder?.ToString() + text2 + text3).Width;
                        caretPos--;
                    }

                    stringBuilder.Append(text2);
                    text2 = "";
                }

                string text4 = stringBuilder.ToString();
                double width2 = ctx.TextExtents(text4).Width;
                list.Add(new TextLine
                {
                    Text = text4,
                    Bounds = new LineRectangled(textFlowPath.X1 + num2, num3, width2, num)
                    {
                        Ascent = ctx.FontExtents.Ascent
                    },
                    LeftSpace = 0.0,
                    RightSpace = num4 - width2
                });
                stringBuilder.Clear();
                num3 += num;
                num2 = 0.0;
                if (gotLinebreak)
                {
                    textFlowPath = GetCurrentFlowPathSection(flowPath, num3);
                }
            }

            stringBuilder.Append(text2);
            if (gotSpace)
            {
                stringBuilder.Append(" ");
            }

            if (textFlowPath == null)
            {
                textFlowPath = new TextFlowPath();
            }

            if (gotLinebreak)
            {
                if (keepLinebreakChar)
                {
                    stringBuilder.Append("\n");
                }

                string text5 = stringBuilder.ToString();
                double width3 = ctx.TextExtents(text5).Width;
                list.Add(new TextLine
                {
                    Text = text5,
                    Bounds = new LineRectangled(textFlowPath.X1 + num2, num3, width3, num)
                    {
                        Ascent = ctx.FontExtents.Ascent
                    },
                    LeftSpace = 0.0,
                    NextOffsetX = num2,
                    RightSpace = num4 - width3
                });
                stringBuilder.Clear();
                num3 += num;
                num2 = 0.0;
            }
        }

        textFlowPath = GetCurrentFlowPathSection(flowPath, num3);
        if (textFlowPath == null)
        {
            textFlowPath = new TextFlowPath();
        }

        num4 = textFlowPath.X2 - textFlowPath.X1 - num2;
        string text6 = stringBuilder.ToString();
        double width4 = ctx.TextExtents(text6).Width;
        list.Add(new TextLine
        {
            Text = text6,
            Bounds = new LineRectangled(textFlowPath.X1 + num2, num3, width4, num)
            {
                Ascent = ctx.FontExtents.Ascent
            },
            LeftSpace = 0.0,
            NextOffsetX = num2,
            RightSpace = num4 - width4
        });
        return list.ToArray();
    }

    private TextFlowPath GetCurrentFlowPathSection(TextFlowPath[] flowPath, double posY)
    {
        for (int i = 0; i < flowPath.Length; i++)
        {
            if (flowPath[i].Y1 <= posY && flowPath[i].Y2 >= posY)
            {
                return flowPath[i];
            }
        }

        return null;
    }

    private string getNextWord(string fulltext, EnumLinebreakBehavior linebreakBh)
    {
        if (caretPos >= fulltext.Length)
        {
            return null;
        }

        StringBuilder stringBuilder = new StringBuilder();
        gotLinebreak = false;
        gotSpace = false;
        bool flag = linebreakBh != EnumLinebreakBehavior.None;
        while (caretPos < fulltext.Length)
        {
            char c = fulltext[caretPos];
            caretPos++;
            if (c == ' ' && flag)
            {
                gotSpace = true;
                break;
            }

            switch (c)
            {
                case '\t':
                    if (stringBuilder.Length > 0)
                    {
                        caretPos--;
                        break;
                    }

                    return "  ";
                case '\r':
                    gotLinebreak = true;
                    if (caretPos <= fulltext.Length - 1 && fulltext[caretPos] == '\n')
                    {
                        caretPos++;
                    }

                    break;
                case '\n':
                    gotLinebreak = true;
                    break;
                default:
                    goto IL_00cf;
            }

            break;
        IL_00cf:
            stringBuilder.Append(c);
            if (linebreakBh == EnumLinebreakBehavior.AfterCharacter)
            {
                break;
            }
        }

        return stringBuilder.ToString();
    }

    public double AutobreakAndDrawMultilineText(Context ctx, CairoFont font, string text, double lineX, double lineY, TextFlowPath[] flowPath, EnumTextOrientation orientation = EnumTextOrientation.Left, EnumLinebreakBehavior linebreak = EnumLinebreakBehavior.AfterWord)
    {
        TextLine[] array = Lineize(font, text, linebreak, flowPath, lineX, lineY);
        DrawMultilineText(ctx, font, array, orientation);
        if (array.Length == 0)
        {
            return 0.0;
        }

        return array[^1].Bounds.Y + array[^1].Bounds.Height;
    }

    //
    // Summary:
    //     lineX is set to 0 after the second line, lineY is advanced by line height for
    //     each line
    //
    // Parameters:
    //   ctx:
    //     The context of the text.
    //
    //   lines:
    //     The preformatted lines of the text.
    //
    //   font:
    //     The font of the text
    //
    //   orientation:
    //     The orientation of text (Default: Left)
    public void DrawMultilineText(Context ctx, CairoFont font, TextLine[] lines, EnumTextOrientation orientation = EnumTextOrientation.Left)
    {
        font.SetupContext(ctx);
        double num = 0.0;
        foreach (TextLine textLine in lines)
        {
            if (textLine.Text.Length != 0)
            {
                if (orientation == EnumTextOrientation.Center)
                {
                    num = (textLine.LeftSpace + textLine.RightSpace) / 2.0;
                }

                if (orientation == EnumTextOrientation.Right)
                {
                    num = textLine.LeftSpace + textLine.RightSpace;
                }

                DrawTextLine(ctx, font, textLine.Text, num + textLine.Bounds.X, textLine.Bounds.Y);
            }
        }
    }

    //
    // Summary:
    //     Draws a line of text on the screen.
    //
    // Parameters:
    //   ctx:
    //     The context of the text.
    //
    //   font:
    //     The font of the text.
    //
    //   text:
    //     The text to draw.
    //
    //   offsetX:
    //     The X offset for the text start position. (Default: 0)
    //
    //   offsetY:
    //     The Y offset for the text start position. (Default: 0)
    //
    //   textPathMode:
    //     Whether or not to use TextPathMode.
    public void DrawTextLine(Context ctx, CairoFont font, string text, double offsetX = 0.0, double offsetY = 0.0, bool textPathMode = false)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        ctx.MoveTo((int)offsetX, (int)(offsetY + ctx.FontExtents.Ascent));
        if (textPathMode)
        {
            ctx.TextPath(text);
        }
        else if (font.StrokeWidth > 0.0)
        {
            ctx.TextPath(text);
            ctx.LineWidth = font.StrokeWidth;
            ctx.SetSourceRGBA(font.StrokeColor);
            ctx.StrokePreserve();
            ctx.SetSourceRGBA(font.Color);
            ctx.Fill();
        }
        else
        {
            ctx.ShowText(text);
            if (font.RenderTwice)
            {
                ctx.ShowText(text);
            }
        }
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
