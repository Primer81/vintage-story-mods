#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementScrollbar : GuiElementControl
{
    public static int DefaultScrollbarWidth = 20;

    public static int DeafultScrollbarPadding = 2;

    protected Action<float> onNewScrollbarValue;

    public bool mouseDownOnScrollbarHandle;

    public int mouseDownStartY;

    protected float visibleHeight;

    protected float totalHeight;

    protected float currentHandlePosition;

    protected float currentHandleHeight;

    public float zOffset;

    protected LoadedTexture handleTexture;

    public override bool Focusable => true;

    //
    // Summary:
    //     Moving 1 pixel of the scrollbar moves the content by ScrollConversionFactor of
    //     pixels
    public float ScrollConversionFactor
    {
        get
        {
            if (Bounds.InnerHeight - (double)currentHandleHeight <= 0.0)
            {
                return 1f;
            }

            float num = (float)(Bounds.InnerHeight - (double)currentHandleHeight);
            return (totalHeight - visibleHeight) / num;
        }
    }

    //
    // Summary:
    //     The current Y position of the inner element
    public float CurrentYPosition
    {
        get
        {
            return currentHandlePosition * ScrollConversionFactor;
        }
        set
        {
            currentHandlePosition = value / ScrollConversionFactor;
        }
    }

    //
    // Summary:
    //     Creates a new Scrollbar.
    //
    // Parameters:
    //   capi:
    //     The client API.
    //
    //   onNewScrollbarValue:
    //     The event that fires when the scrollbar is changed.
    //
    //   bounds:
    //     The bounds of the scrollbar.
    public GuiElementScrollbar(ICoreClientAPI capi, Action<float> onNewScrollbarValue, ElementBounds bounds)
        : base(capi, bounds)
    {
        handleTexture = new LoadedTexture(capi);
        this.onNewScrollbarValue = onNewScrollbarValue;
    }

    public override void ComposeElements(Context ctxStatic, ImageSurface surface)
    {
        Bounds.CalcWorldBounds();
        ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
        ElementRoundRectangle(ctxStatic, Bounds);
        ctxStatic.Fill();
        EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true);
        RecomposeHandle();
    }

    public virtual void RecomposeHandle()
    {
        //IL_0023: Unknown result type (might be due to invalid IL or missing references)
        //IL_0029: Expected O, but got Unknown
        Bounds.CalcWorldBounds();
        int num = (int)Bounds.InnerWidth;
        int num2 = (int)currentHandleHeight;
        ImageSurface val = new ImageSurface((Format)0, num, num2);
        Context val2 = genContext(val);
        GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, 1.0);
        val2.SetSourceRGBA(GuiStyle.DialogHighlightColor);
        val2.FillPreserve();
        val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
        val2.Fill();
        EmbossRoundRectangleElement(val2, 0.0, 0.0, num, num2, inverse: false, 2, 1);
        generateTexture(val, ref handleTexture);
        val2.Dispose();
        ((Surface)val).Dispose();
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        api.Render.Render2DTexturePremultipliedAlpha(handleTexture.TextureId, (int)(Bounds.renderX + Bounds.absPaddingX), (int)(Bounds.renderY + Bounds.absPaddingY + (double)currentHandlePosition), (int)Bounds.InnerWidth, (int)currentHandleHeight, 200f + zOffset);
    }

    //
    // Summary:
    //     Sets the height of the scrollbar.
    //
    // Parameters:
    //   visibleHeight:
    //     The visible height of the scrollbar
    //
    //   totalHeight:
    //     The total height of the scrollbar.
    public void SetHeights(float visibleHeight, float totalHeight)
    {
        this.visibleHeight = visibleHeight;
        SetNewTotalHeight(totalHeight);
    }

    //
    // Summary:
    //     Sets the total height, recalculating things for the new height.
    //
    // Parameters:
    //   totalHeight:
    //     The total height of the scrollbar.
    public void SetNewTotalHeight(float totalHeight)
    {
        this.totalHeight = totalHeight;
        float num = GameMath.Clamp(visibleHeight / totalHeight, 0f, 1f);
        currentHandleHeight = Math.Max(10f, (float)((double)num * Bounds.InnerHeight));
        currentHandlePosition = (float)Math.Min(currentHandlePosition, Bounds.InnerHeight - (double)currentHandleHeight);
        TriggerChanged();
        RecomposeHandle();
    }

    public void SetScrollbarPosition(int pos)
    {
        currentHandlePosition = pos;
        onNewScrollbarValue(0f);
    }

    public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
    {
        if (!(Bounds.InnerHeight <= (double)currentHandleHeight + 0.001))
        {
            float num = currentHandlePosition - (float)GuiElement.scaled(102.0) * args.deltaPrecise / ScrollConversionFactor;
            double max = Bounds.InnerHeight - (double)currentHandleHeight;
            currentHandlePosition = (float)GameMath.Clamp(num, 0.0, max);
            TriggerChanged();
            args.SetHandled();
        }
    }

    public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
    {
        if (!(Bounds.InnerHeight <= (double)currentHandleHeight + 0.001) && Bounds.PointInside(args.X, args.Y))
        {
            mouseDownOnScrollbarHandle = true;
            mouseDownStartY = GameMath.Max(0, args.Y - (int)Bounds.renderY, 0);
            if ((float)mouseDownStartY > currentHandleHeight)
            {
                mouseDownStartY = (int)currentHandleHeight / 2;
            }

            UpdateHandlePositionAbs(args.Y - (int)Bounds.renderY - mouseDownStartY);
            args.Handled = true;
        }
    }

    public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
    {
        mouseDownOnScrollbarHandle = false;
    }

    public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
    {
        if (mouseDownOnScrollbarHandle)
        {
            UpdateHandlePositionAbs(args.Y - (int)Bounds.renderY - mouseDownStartY);
        }
    }

    public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
    {
        if (hasFocus && (args.KeyCode == 46 || args.KeyCode == 45))
        {
            float num = ((args.KeyCode == 46) ? (-0.5f) : 0.5f);
            float num2 = currentHandlePosition - (float)GuiElement.scaled(102.0) * num / ScrollConversionFactor;
            double max = Bounds.InnerHeight - (double)currentHandleHeight;
            currentHandlePosition = (float)GameMath.Clamp(num2, 0.0, max);
            TriggerChanged();
        }
    }

    private void UpdateHandlePositionAbs(float y)
    {
        double max = Bounds.InnerHeight - (double)currentHandleHeight;
        currentHandlePosition = (float)GameMath.Clamp(y, 0.0, max);
        TriggerChanged();
    }

    //
    // Summary:
    //     Triggers the change to the new value of the scrollbar.
    public void TriggerChanged()
    {
        onNewScrollbarValue(CurrentYPosition);
    }

    //
    // Summary:
    //     Puts the scrollblock to the very bottom of the scrollbar.
    public void ScrollToBottom()
    {
        float num = 1f;
        if (totalHeight < visibleHeight)
        {
            currentHandlePosition = 0f;
            num = 0f;
        }
        else
        {
            currentHandlePosition = (float)(Bounds.InnerHeight - (double)currentHandleHeight);
        }

        float obj = num * (totalHeight - visibleHeight);
        onNewScrollbarValue(obj);
    }

    public void EnsureVisible(double posX, double posY)
    {
        double num = CurrentYPosition;
        double num2 = (double)CurrentYPosition + Bounds.InnerHeight;
        if (posY < num)
        {
            float num3 = (float)(num - posY) / ScrollConversionFactor;
            currentHandlePosition = Math.Max(0f, currentHandlePosition - num3);
            TriggerChanged();
        }
        else if (posY > num2)
        {
            float num4 = (float)(posY - num2) / ScrollConversionFactor;
            currentHandlePosition = (float)Math.Min(Bounds.InnerHeight - (double)currentHandleHeight, currentHandlePosition + num4);
            TriggerChanged();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        handleTexture.Dispose();
    }
}
#if false // Decompilation log
'168' items in cache
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
Could not find by name: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
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
Could not find by name: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
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
Could not find by name: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Could not find by name: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
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
Could not find by name: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
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
