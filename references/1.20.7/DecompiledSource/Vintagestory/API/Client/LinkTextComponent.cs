#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Cairo;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

public class LinkTextComponent : RichTextComponent
{
    private Action<LinkTextComponent> onLinkClicked;

    public string Href;

    private bool clickable = true;

    private LoadedTexture normalText;

    private LoadedTexture hoverText;

    private double leftMostX;

    private double topMostY;

    private bool isHover;

    private bool wasMouseDown;

    public bool Clickable
    {
        get
        {
            return clickable;
        }
        set
        {
            clickable = value;
            base.MouseOverCursor = (clickable ? "linkselect" : null);
        }
    }

    //
    // Summary:
    //     Create a dummy link text component for use with triggering link protocols through
    //     code. Not usable for anything gui related (it'll crash if you try)
    //
    // Parameters:
    //   href:
    public LinkTextComponent(string href)
        : base(null, "", null)
    {
        Href = href;
    }

    //
    // Summary:
    //     A text component with an embedded link.
    //
    // Parameters:
    //   api:
    //
    //   displayText:
    //     The text of the Text.
    //
    //   font:
    //
    //   onLinkClicked:
    public LinkTextComponent(ICoreClientAPI api, string displayText, CairoFont font, Action<LinkTextComponent> onLinkClicked)
        : base(api, displayText, font)
    {
        this.onLinkClicked = onLinkClicked;
        base.MouseOverCursor = "linkselect";
        Font = Font.Clone().WithColor(GuiStyle.ActiveButtonTextColor);
        hoverText = new LoadedTexture(api);
        normalText = new LoadedTexture(api);
    }

    public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
    {
        return base.CalcBounds(flowPath, currentLineHeight, offsetX, lineY, out nextOffsetX);
    }

    public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
    {
        leftMostX = 999999.0;
        topMostY = 999999.0;
        double num = 0.0;
        double num2 = 0.0;
        for (int i = 0; i < Lines.Length; i++)
        {
            TextLine textLine = Lines[i];
            leftMostX = Math.Min(leftMostX, textLine.Bounds.X);
            topMostY = Math.Min(topMostY, textLine.Bounds.Y);
            num = Math.Max(num, textLine.Bounds.X + textLine.Bounds.Width);
            num2 = Math.Max(num2, textLine.Bounds.Y + textLine.Bounds.Height);
        }

        ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)(num - leftMostX), (int)(num2 - topMostY));
        Context context = new Context(imageSurface);
        context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
        context.Paint();
        context.Save();
        Matrix matrix = context.Matrix;
        matrix.Translate((int)(0.0 - leftMostX), (int)(0.0 - topMostY));
        context.Matrix = matrix;
        CairoFont font = Font;
        ComposeFor(context, imageSurface);
        api.Gui.LoadOrUpdateCairoTexture(imageSurface, linearMag: false, ref normalText);
        context.Operator = Operator.Clear;
        context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
        context.Paint();
        context.Operator = Operator.Over;
        Font = Font.Clone();
        Font.Color[0] = Math.Min(1.0, Font.Color[0] * 1.2);
        Font.Color[1] = Math.Min(1.0, Font.Color[1] * 1.2);
        Font.Color[2] = Math.Min(1.0, Font.Color[2] * 1.2);
        ComposeFor(context, imageSurface);
        Font = font;
        context.Restore();
        api.Gui.LoadOrUpdateCairoTexture(imageSurface, linearMag: false, ref hoverText);
        imageSurface.Dispose();
        context.Dispose();
    }

    private void ComposeFor(Context ctx, ImageSurface surface)
    {
        textUtil.DrawMultilineText(ctx, Font, Lines);
        ctx.LineWidth = 1.0;
        ctx.SetSourceRGBA(Font.Color);
        for (int i = 0; i < Lines.Length; i++)
        {
            TextLine textLine = Lines[i];
            ctx.MoveTo(textLine.Bounds.X, textLine.Bounds.Y + textLine.Bounds.AscentOrHeight + 2.0);
            ctx.LineTo(textLine.Bounds.X + textLine.Bounds.Width, textLine.Bounds.Y + textLine.Bounds.AscentOrHeight + 2.0);
            ctx.Stroke();
        }
    }

    public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
    {
        base.RenderInteractiveElements(deltaTime, renderX, renderY, renderZ);
        isHover = false;
        double fontOrientOffsetX = GetFontOrientOffsetX();
        if (clickable)
        {
            LineRectangled[] boundsPerLine = BoundsPerLine;
            for (int i = 0; i < boundsPerLine.Length; i++)
            {
                if (boundsPerLine[i].PointInside((double)api.Input.MouseX - renderX - fontOrientOffsetX, (double)api.Input.MouseY - renderY))
                {
                    isHover = true;
                    break;
                }
            }
        }

        api.Render.Render2DTexturePremultipliedAlpha(isHover ? hoverText.TextureId : normalText.TextureId, (int)(renderX + leftMostX + fontOrientOffsetX), (int)(renderY + topMostY), hoverText.Width, hoverText.Height, (float)renderZ + 50f);
    }

    public override bool UseMouseOverCursor(ElementBounds richtextBounds)
    {
        return isHover;
    }

    public override void OnMouseDown(MouseEvent args)
    {
        if (!clickable)
        {
            return;
        }

        double fontOrientOffsetX = GetFontOrientOffsetX();
        wasMouseDown = false;
        LineRectangled[] boundsPerLine = BoundsPerLine;
        for (int i = 0; i < boundsPerLine.Length; i++)
        {
            if (boundsPerLine[i].PointInside((double)args.X - fontOrientOffsetX, args.Y))
            {
                wasMouseDown = true;
            }
        }
    }

    public override void OnMouseUp(MouseEvent args)
    {
        if (!clickable || !wasMouseDown)
        {
            return;
        }

        double fontOrientOffsetX = GetFontOrientOffsetX();
        LineRectangled[] boundsPerLine = BoundsPerLine;
        for (int i = 0; i < boundsPerLine.Length; i++)
        {
            if (boundsPerLine[i].PointInside((double)args.X - fontOrientOffsetX, args.Y))
            {
                args.Handled = true;
                Trigger();
            }
        }
    }

    public LinkTextComponent SetHref(string href)
    {
        Href = href;
        return this;
    }

    public void Trigger()
    {
        if (onLinkClicked == null)
        {
            if (Href != null)
            {
                HandleLink();
            }
        }
        else
        {
            onLinkClicked(this);
        }
    }

    public void HandleLink()
    {
        if (Href.StartsWithOrdinal("hotkey://"))
        {
            api.Input.GetHotKeyByCode(Href.Substring("hotkey://".Length))?.Handler?.Invoke(null);
            return;
        }

        string[] array = Href.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
        if (array.Length != 0 && api.LinkProtocols != null && api.LinkProtocols.ContainsKey(array[0]))
        {
            api.LinkProtocols[array[0]](this);
        }
        else if (array.Length != 0 && array[0].StartsWithOrdinal("http"))
        {
            api.Gui.OpenLink(Href);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        hoverText?.Dispose();
        normalText?.Dispose();
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
