#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class GuiElementEditableTextBase : GuiElementTextBase
{
    public delegate bool OnTryTextChangeDelegate(List<string> lines);

    internal float[] caretColor = new float[4] { 1f, 1f, 1f, 1f };

    internal bool hideCharacters;

    internal bool multilineMode;

    internal int maxlines = 99999;

    internal double caretX;

    internal double caretY;

    internal double topPadding;

    internal double leftPadding = 3.0;

    internal double rightSpacing;

    internal double bottomSpacing;

    internal LoadedTexture caretTexture;

    internal LoadedTexture textTexture;

    public Action<int, int> OnCaretPositionChanged;

    public Action<string> OnTextChanged;

    public OnTryTextChangeDelegate OnTryTextChangeText;

    public Action<double, double> OnCursorMoved;

    internal Action OnFocused;

    internal Action OnLostFocus;

    //
    // Summary:
    //     Called when a keyboard key was pressed, received and handled
    public Action OnKeyPressed;

    internal long caretBlinkMilliseconds;

    internal bool caretDisplayed;

    internal double caretHeight;

    internal double renderLeftOffset;

    internal Vec2i textSize = new Vec2i();

    protected List<string> lines;

    //
    // Summary:
    //     Contains the same as Lines, but may momentarily have different values when an
    //     edit is being made
    protected List<string> linesStaging;

    public bool WordWrap = true;

    protected int pcaretPosLine;

    protected int pcaretPosInLine;

    public int TextLengthWithoutLineBreaks
    {
        get
        {
            int num = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                num += lines[i].Length;
            }

            return num;
        }
    }

    public int CaretPosWithoutLineBreaks
    {
        get
        {
            int num = 0;
            for (int i = 0; i < CaretPosLine; i++)
            {
                num += lines[i].Length;
            }

            return num + CaretPosInLine;
        }
        set
        {
            int num = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                int length = lines[i].Length;
                if (num + length > value)
                {
                    SetCaretPos(value - num, i);
                    return;
                }

                num += length;
            }

            if (!multilineMode)
            {
                SetCaretPos(num);
            }
            else
            {
                SetCaretPos(num, lines.Count);
            }
        }
    }

    public int CaretPosLine
    {
        get
        {
            return pcaretPosLine;
        }
        set
        {
            pcaretPosLine = value;
        }
    }

    public int CaretPosInLine
    {
        get
        {
            return pcaretPosInLine;
        }
        set
        {
            if (value > lines[CaretPosLine].Length)
            {
                throw new IndexOutOfRangeException("Caret @" + value + ", cannot beyond current line length of " + pcaretPosInLine);
            }

            pcaretPosInLine = value;
        }
    }

    public override bool Focusable => true;

    public List<string> GetLines()
    {
        return new List<string>(lines);
    }

    //
    // Summary:
    //     Initializes the text component.
    //
    // Parameters:
    //   capi:
    //     The Client API
    //
    //   font:
    //     The font of the text.
    //
    //   bounds:
    //     The bounds of the component.
    public GuiElementEditableTextBase(ICoreClientAPI capi, CairoFont font, ElementBounds bounds)
        : base(capi, "", font, bounds)
    {
        caretTexture = new LoadedTexture(capi);
        textTexture = new LoadedTexture(capi);
        lines = new List<string> { "" };
        linesStaging = new List<string> { "" };
    }

    public override void OnFocusGained()
    {
        base.OnFocusGained();
        SetCaretPos(TextLengthWithoutLineBreaks);
        OnFocused?.Invoke();
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        OnLostFocus?.Invoke();
    }

    //
    // Summary:
    //     Sets the position of the cursor at a given point.
    //
    // Parameters:
    //   x:
    //     X position of the cursor.
    //
    //   y:
    //     Y position of the cursor.
    public void SetCaretPos(double x, double y)
    {
        CaretPosLine = 0;
        ImageSurface imageSurface = new ImageSurface(Format.Argb32, 1, 1);
        Context context = genContext(imageSurface);
        Font.SetupContext(context);
        if (multilineMode)
        {
            double num = y / context.FontExtents.Height;
            if (num > (double)lines.Count)
            {
                CaretPosLine = lines.Count - 1;
                CaretPosInLine = lines[CaretPosLine].Length;
                context.Dispose();
                imageSurface.Dispose();
                return;
            }

            CaretPosLine = Math.Max(0, (int)num);
        }

        string text = lines[CaretPosLine].TrimEnd('\r', '\n');
        CaretPosInLine = text.Length;
        for (int i = 0; i < text.Length; i++)
        {
            double xAdvance = context.TextExtents(text.Substring(0, i + 1)).XAdvance;
            if (x - xAdvance <= 0.0)
            {
                CaretPosInLine = i;
                break;
            }
        }

        context.Dispose();
        imageSurface.Dispose();
        SetCaretPos(CaretPosInLine, CaretPosLine);
    }

    //
    // Summary:
    //     Sets the position of the cursor to a specific character.
    //
    // Parameters:
    //   posInLine:
    //     The position in the line.
    //
    //   posLine:
    //     The line of the text.
    public void SetCaretPos(int posInLine, int posLine = 0)
    {
        caretBlinkMilliseconds = api.ElapsedMilliseconds;
        caretDisplayed = true;
        CaretPosLine = GameMath.Clamp(posLine, 0, lines.Count - 1);
        CaretPosInLine = GameMath.Clamp(posInLine, 0, lines[CaretPosLine].TrimEnd('\r', '\n').Length);
        if (multilineMode)
        {
            caretX = Font.GetTextExtents(lines[CaretPosLine].Substring(0, CaretPosInLine)).XAdvance;
            caretY = Font.GetFontExtents().Height * (double)CaretPosLine;
        }
        else
        {
            string text = lines[0];
            if (hideCharacters)
            {
                text = new StringBuilder(lines[0]).Insert(0, "•", text.Length).ToString();
            }

            caretX = Font.GetTextExtents(text.Substring(0, CaretPosInLine)).XAdvance;
            caretY = 0.0;
        }

        OnCursorMoved?.Invoke(caretX, caretY);
        renderLeftOffset = Math.Max(0.0, caretX - Bounds.InnerWidth + rightSpacing);
        OnCaretPositionChanged?.Invoke(posLine, posInLine);
    }

    //
    // Summary:
    //     Sets a numerical value to the text, appending it to the end of the text.
    //
    // Parameters:
    //   value:
    //     The value to add to the text.
    public void SetValue(float value)
    {
        SetValue(value.ToString(GlobalConstants.DefaultCultureInfo));
    }

    //
    // Summary:
    //     Sets a numerical value to the text, appending it to the end of the text.
    //
    // Parameters:
    //   value:
    //     The value to add to the text.
    public void SetValue(double value)
    {
        SetValue(value.ToString(GlobalConstants.DefaultCultureInfo));
    }

    //
    // Summary:
    //     Sets given text, sets the cursor to the end of the text
    //
    // Parameters:
    //   text:
    //
    //   setCaretPosToEnd:
    public void SetValue(string text, bool setCaretPosToEnd = true)
    {
        LoadValue(Lineize(text));
        if (setCaretPosToEnd)
        {
            int length = lines[lines.Count - 1].Length;
            SetCaretPos(length, lines.Count - 1);
        }
    }

    //
    // Summary:
    //     Sets given texts, leaves cursor position unchanged
    //
    // Parameters:
    //   newLines:
    public void LoadValue(List<string> newLines)
    {
        OnTryTextChangeDelegate onTryTextChangeText = OnTryTextChangeText;
        if ((onTryTextChangeText != null && !onTryTextChangeText(newLines)) || (newLines.Count > maxlines && newLines.Count >= lines.Count))
        {
            linesStaging = new List<string>(lines);
            return;
        }

        lines = new List<string>(newLines);
        linesStaging = new List<string>(lines);
        TextChanged();
    }

    public List<string> Lineize(string text)
    {
        if (text == null)
        {
            text = "";
        }

        List<string> list = new List<string>();
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (multilineMode)
        {
            double boxWidth = Bounds.InnerWidth - 2.0 * Bounds.absPaddingX;
            if (!WordWrap)
            {
                boxWidth = 999999.0;
            }

            TextLine[] array = textUtil.Lineize(Font, text, boxWidth, EnumLinebreakBehavior.Default, keepLinebreakChar: true);
            foreach (TextLine textLine in array)
            {
                list.Add(textLine.Text);
            }

            if (list.Count == 0)
            {
                list.Add("");
            }
        }
        else
        {
            list.Add(text);
        }

        return list;
    }

    internal virtual void TextChanged()
    {
        OnTextChanged?.Invoke(string.Join("", lines));
        RecomposeText();
    }

    internal virtual void RecomposeText()
    {
        Bounds.CalcWorldBounds();
        string text = null;
        if (multilineMode)
        {
            textSize.X = (int)(Bounds.OuterWidth - rightSpacing);
            textSize.Y = (int)(Bounds.OuterHeight - bottomSpacing);
        }
        else
        {
            text = lines[0];
            if (hideCharacters)
            {
                text = new StringBuilder(text.Length).Insert(0, "•", text.Length).ToString();
            }

            textSize.X = (int)Math.Max(Bounds.InnerWidth - rightSpacing, Font.GetTextExtents(text).Width);
            textSize.Y = (int)(Bounds.InnerHeight - bottomSpacing);
        }

        ImageSurface imageSurface = new ImageSurface(Format.Argb32, textSize.X, textSize.Y);
        Context context = genContext(imageSurface);
        Font.SetupContext(context);
        double height = context.FontExtents.Height;
        if (multilineMode)
        {
            double boxWidth = Bounds.InnerWidth - 2.0 * Bounds.absPaddingX - rightSpacing;
            TextLine[] array = new TextLine[lines.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new TextLine
                {
                    Text = lines[i].Replace("\r\n", "").Replace("\n", ""),
                    Bounds = new LineRectangled(0.0, (double)i * height, Bounds.InnerWidth, height)
                };
            }

            textUtil.DrawMultilineTextAt(context, Font, array, Bounds.absPaddingX + leftPadding, Bounds.absPaddingY, boxWidth);
        }
        else
        {
            topPadding = Math.Max(0.0, Bounds.OuterHeight - bottomSpacing - context.FontExtents.Height) / 2.0;
            textUtil.DrawTextLine(context, Font, text, Bounds.absPaddingX + leftPadding, Bounds.absPaddingY + topPadding);
        }

        generateTexture(imageSurface, ref textTexture);
        context.Dispose();
        imageSurface.Dispose();
        if (caretTexture.TextureId == 0)
        {
            caretHeight = height;
            imageSurface = new ImageSurface(Format.Argb32, 3, (int)height);
            context = genContext(imageSurface);
            Font.SetupContext(context);
            context.SetSourceRGBA(caretColor[0], caretColor[1], caretColor[2], caretColor[3]);
            context.LineWidth = 1.0;
            context.NewPath();
            context.MoveTo(2.0, 0.0);
            context.LineTo(2.0, height);
            context.ClosePath();
            context.Stroke();
            generateTexture(imageSurface, ref caretTexture.TextureId);
            context.Dispose();
            imageSurface.Dispose();
        }
    }

    public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
    {
        base.OnMouseDownOnElement(api, args);
        SetCaretPos((double)args.X - Bounds.absX, (double)args.Y - Bounds.absY);
    }

    public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
    {
        if (!base.HasFocus)
        {
            return;
        }

        bool handled = multilineMode || args.KeyCode != 52;
        if (args.KeyCode == 53 && CaretPosWithoutLineBreaks > 0)
        {
            OnKeyBackSpace();
        }

        if (args.KeyCode == 55 && CaretPosWithoutLineBreaks < TextLengthWithoutLineBreaks)
        {
            OnKeyDelete();
        }

        if (args.KeyCode == 59)
        {
            if (args.CtrlPressed)
            {
                SetCaretPos(lines[lines.Count - 1].TrimEnd('\r', '\n').Length, lines.Count - 1);
            }
            else
            {
                SetCaretPos(lines[CaretPosLine].TrimEnd('\r', '\n').Length, CaretPosLine);
            }

            api.Gui.PlaySound("tick");
        }

        if (args.KeyCode == 58)
        {
            if (args.CtrlPressed)
            {
                SetCaretPos(0);
            }
            else
            {
                SetCaretPos(0, CaretPosLine);
            }

            api.Gui.PlaySound("tick");
        }

        if (args.KeyCode == 47)
        {
            MoveCursor(-1, args.CtrlPressed);
        }

        if (args.KeyCode == 48)
        {
            MoveCursor(1, args.CtrlPressed);
        }

        if (args.KeyCode == 104 && (args.CtrlPressed || args.CommandPressed))
        {
            string clipboardText = api.Forms.GetClipboardText();
            clipboardText = clipboardText.Replace("\ufeff", "");
            string text = string.Join("", lines);
            int num = CaretPosInLine;
            for (int i = 0; i < CaretPosLine; i++)
            {
                num += lines[i].Length;
            }

            SetValue(text.Substring(0, num) + clipboardText + text.Substring(num, text.Length - num));
            api.Gui.PlaySound("tick");
        }

        if (args.KeyCode == 46 && CaretPosLine < lines.Count - 1)
        {
            SetCaretPos(CaretPosInLine, CaretPosLine + 1);
            api.Gui.PlaySound("tick");
        }

        if (args.KeyCode == 45 && CaretPosLine > 0)
        {
            SetCaretPos(CaretPosInLine, CaretPosLine - 1);
            api.Gui.PlaySound("tick");
        }

        if (args.KeyCode == 49 || args.KeyCode == 82)
        {
            if (multilineMode)
            {
                OnKeyEnter();
            }
            else
            {
                handled = false;
            }
        }

        if (args.KeyCode == 50)
        {
            handled = false;
        }

        args.Handled = handled;
    }

    public override string GetText()
    {
        return string.Join("", lines);
    }

    private void OnKeyEnter()
    {
        if (lines.Count < maxlines)
        {
            string text = linesStaging[CaretPosLine].Substring(0, CaretPosInLine);
            string item = linesStaging[CaretPosLine].Substring(CaretPosInLine);
            linesStaging[CaretPosLine] = text + "\n";
            linesStaging.Insert(CaretPosLine + 1, item);
            OnTryTextChangeDelegate onTryTextChangeText = OnTryTextChangeText;
            if (onTryTextChangeText == null || onTryTextChangeText(linesStaging))
            {
                lines = new List<string>(linesStaging);
                TextChanged();
                SetCaretPos(0, CaretPosLine + 1);
                api.Gui.PlaySound("tick");
            }
        }
    }

    private void OnKeyDelete()
    {
        string text = GetText();
        int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
        if (text.Length != caretPosWithoutLineBreaks)
        {
            text = text.Substring(0, caretPosWithoutLineBreaks) + text.Substring(caretPosWithoutLineBreaks + 1, text.Length - caretPosWithoutLineBreaks - 1);
            LoadValue(Lineize(text));
            api.Gui.PlaySound("tick");
        }
    }

    private void OnKeyBackSpace()
    {
        int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
        if (caretPosWithoutLineBreaks != 0)
        {
            string text = GetText();
            text = text.Substring(0, caretPosWithoutLineBreaks - 1) + text.Substring(caretPosWithoutLineBreaks, text.Length - caretPosWithoutLineBreaks);
            int caretPosWithoutLineBreaks2 = CaretPosWithoutLineBreaks;
            LoadValue(Lineize(text));
            if (caretPosWithoutLineBreaks2 > 0)
            {
                CaretPosWithoutLineBreaks = caretPosWithoutLineBreaks2 - 1;
            }

            api.Gui.PlaySound("tick");
        }
    }

    public override void OnKeyPress(ICoreClientAPI api, KeyEvent args)
    {
        if (!base.HasFocus)
        {
            return;
        }

        ReadOnlySpan<char> readOnlySpan = lines[CaretPosLine].Substring(0, CaretPosInLine);
        char reference = args.KeyChar;
        string text = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference), lines[CaretPosLine].Substring(CaretPosInLine, lines[CaretPosLine].Length - CaretPosInLine));
        double num = Bounds.InnerWidth - 2.0 * Bounds.absPaddingX - rightSpacing;
        linesStaging[CaretPosLine] = text;
        if (multilineMode && Font.GetTextExtents(text.TrimEnd('\r', '\n')).Width >= num)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                stringBuilder.Append((i == CaretPosLine) ? text : lines[i]);
            }

            linesStaging = Lineize(stringBuilder.ToString());
            if (lines.Count >= maxlines && linesStaging.Count >= maxlines)
            {
                return;
            }
        }

        int caretPosWithoutLineBreaks = CaretPosWithoutLineBreaks;
        LoadValue(linesStaging);
        CaretPosWithoutLineBreaks = caretPosWithoutLineBreaks + 1;
        args.Handled = true;
        api.Gui.PlaySound("tick");
        OnKeyPressed?.Invoke();
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        if (base.HasFocus)
        {
            if (api.ElapsedMilliseconds - caretBlinkMilliseconds > 900)
            {
                caretBlinkMilliseconds = api.ElapsedMilliseconds;
                caretDisplayed = !caretDisplayed;
            }

            if (caretDisplayed && caretX - renderLeftOffset < Bounds.InnerWidth)
            {
                api.Render.Render2DTexturePremultipliedAlpha(caretTexture.TextureId, Bounds.renderX + caretX + GuiElement.scaled(1.5) - renderLeftOffset, Bounds.renderY + caretY + topPadding, 2.0, caretHeight);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        caretTexture.Dispose();
        textTexture.Dispose();
    }

    //
    // Summary:
    //     Moves the cursor forward and backward by an amount.
    //
    // Parameters:
    //   dir:
    //     The direction to move the cursor.
    //
    //   wholeWord:
    //     Whether or not we skip entire words moving it.
    public void MoveCursor(int dir, bool wholeWord = false)
    {
        bool flag = false;
        bool flag2 = ((CaretPosInLine > 0 || CaretPosLine > 0) && dir < 0) || ((CaretPosInLine < lines[CaretPosLine].Length || CaretPosLine < lines.Count - 1) && dir > 0);
        int num = CaretPosInLine;
        int num2 = CaretPosLine;
        while (!flag)
        {
            num += dir;
            if (num < 0)
            {
                if (num2 <= 0)
                {
                    break;
                }

                num2--;
                num = lines[num2].TrimEnd('\r', '\n').Length;
            }

            if (num > lines[num2].TrimEnd('\r', '\n').Length)
            {
                if (num2 >= lines.Count - 1)
                {
                    break;
                }

                num = 0;
                num2++;
            }

            flag = !wholeWord || (num > 0 && lines[num2][num - 1] == ' ');
        }

        if (flag2)
        {
            SetCaretPos(num, num2);
            api.Gui.PlaySound("tick");
        }
    }

    //
    // Summary:
    //     Sets the number of lines in the Text Area.
    //
    // Parameters:
    //   maxlines:
    //     The maximum number of lines.
    public void SetMaxLines(int maxlines)
    {
        this.maxlines = maxlines;
    }

    public void SetMaxHeight(int maxheight)
    {
        maxlines = (int)Math.Floor((double)maxheight / Font.GetFontExtents().Height);
    }
}
#if false // Decompilation log
'180' items in cache
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
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
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
