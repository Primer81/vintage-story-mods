#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Cairo;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class GuiElement : IDisposable
{
    public static AssetLocation dirtTextureName = new AssetLocation("gui/backgrounds/soil.png");

    public static AssetLocation noisyMetalTextureName = new AssetLocation("gui/backgrounds/noisymetal.png");

    public static AssetLocation woodTextureName = new AssetLocation("gui/backgrounds/oak.png");

    public static AssetLocation stoneTextureName = new AssetLocation("gui/backgrounds/stone.png");

    public static AssetLocation waterTextureName = new AssetLocation("gui/backgrounds/water.png");

    public static AssetLocation paperTextureName = new AssetLocation("gui/backgrounds/signpaper.png");

    internal static Dictionary<AssetLocation, KeyValuePair<SurfacePattern, ImageSurface>> cachedPatterns = new Dictionary<AssetLocation, KeyValuePair<SurfacePattern, ImageSurface>>();

    internal string lastShownText = "";

    internal ImageSurface metalNail;

    //
    // Summary:
    //     The bounds of the element.
    public ElementBounds Bounds;

    //
    // Summary:
    //     The tab index of the element.
    public int TabIndex;

    //
    // Summary:
    //     Whether or not the element has focus.
    protected bool hasFocus;

    //
    // Summary:
    //     The Client API.
    protected ICoreClientAPI api;

    //
    // Summary:
    //     If the element is inside a clip or not.
    public virtual ElementBounds InsideClipBounds { get; set; }

    public bool RenderAsPremultipliedAlpha { get; set; } = true;


    //
    // Summary:
    //     Whether or not the element has focus or not.
    public bool HasFocus => hasFocus;

    //
    // Summary:
    //     0 = draw first, 1 = draw last. Only for interactive elements.
    public virtual double DrawOrder => 0.0;

    //
    // Summary:
    //     Whether or not the element can be focused.
    public virtual bool Focusable => false;

    //
    // Summary:
    //     The scale of the element.
    public virtual double Scale { get; set; } = 1.0;


    public virtual string MouseOverCursor { get; protected set; }

    //
    // Summary:
    //     The event fired when the element gains focus.
    public virtual void OnFocusGained()
    {
        hasFocus = true;
    }

    //
    // Summary:
    //     The event fired when the element looses focus.
    public virtual void OnFocusLost()
    {
        hasFocus = false;
    }

    //
    // Summary:
    //     Adds a new GUIElement to the GUI.
    //
    // Parameters:
    //   capi:
    //     The Client API
    //
    //   bounds:
    //     The bounds of the element.
    public GuiElement(ICoreClientAPI capi, ElementBounds bounds)
    {
        api = capi;
        Bounds = bounds;
    }

    //
    // Summary:
    //     Composes the elements.
    //
    // Parameters:
    //   ctxStatic:
    //     The context of the components.
    //
    //   surface:
    //     The surface of the GUI.
    public virtual void ComposeElements(Context ctxStatic, ImageSurface surface)
    {
    }

    //
    // Summary:
    //     Renders the element as an interactive element.
    //
    // Parameters:
    //   deltaTime:
    //     The change in time.
    public virtual void RenderInteractiveElements(float deltaTime)
    {
    }

    //
    // Summary:
    //     The post render of the interactive element.
    //
    // Parameters:
    //   deltaTime:
    //     The change in time.
    public virtual void PostRenderInteractiveElements(float deltaTime)
    {
    }

    //
    // Summary:
    //     Renders the focus overlay.
    //
    // Parameters:
    //   deltaTime:
    //     The change in time.
    public void RenderFocusOverlay(float deltaTime)
    {
        ElementBounds elementBounds = Bounds;
        if (InsideClipBounds != null)
        {
            elementBounds = InsideClipBounds;
        }

        api.Render.RenderRectangle((int)elementBounds.renderX, (int)elementBounds.renderY, 800f, (int)elementBounds.OuterWidth, (int)elementBounds.OuterHeight, 1627389951);
    }

    //
    // Summary:
    //     Generates a texture with an ID.
    //
    // Parameters:
    //   surface:
    //     The image surface supplied.
    //
    //   textureId:
    //     The previous texture id.
    //
    //   linearMag:
    //     Whether or not the texture will have linear magnification.
    protected void generateTexture(ImageSurface surface, ref int textureId, bool linearMag = true)
    {
        int num = textureId;
        textureId = api.Gui.LoadCairoTexture(surface, linearMag);
        if (num > 0)
        {
            api.Render.GLDeleteTexture(num);
        }
    }

    //
    // Summary:
    //     Generates a new texture.
    //
    // Parameters:
    //   surface:
    //     The surface provided.
    //
    //   intoTexture:
    //     The texture to be loaded into.
    //
    //   linearMag:
    //     Whether or not the texture will have linear magnification.
    protected void generateTexture(ImageSurface surface, ref LoadedTexture intoTexture, bool linearMag = true)
    {
        api.Gui.LoadOrUpdateCairoTexture(surface, linearMag, ref intoTexture);
    }

    //
    // Summary:
    //     Changes the scale of given value by the GUIScale factor.
    //
    // Parameters:
    //   value:
    //
    // Returns:
    //     The scaled value based
    public static double scaled(double value)
    {
        return value * (double)RuntimeEnv.GUIScale;
    }

    //
    // Summary:
    //     Changes the scale of given value by the GUIScale factor
    //
    // Parameters:
    //   value:
    //
    // Returns:
    //     Scaled value type cast to int
    public static int scaledi(double value)
    {
        return (int)(value * (double)RuntimeEnv.GUIScale);
    }

    //
    // Summary:
    //     Generates context based off the image surface.
    //
    // Parameters:
    //   surface:
    //     The surface where the context is based.
    //
    // Returns:
    //     The context based off the provided surface.
    protected Context genContext(ImageSurface surface)
    {
        Context context = new Context(surface);
        context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
        context.Paint();
        context.Antialias = Antialias.Best;
        return context;
    }

    //
    // Summary:
    //     Gets a surface pattern based off the bitmap.
    //
    // Parameters:
    //   bitmap:
    //     The provided bitmap.
    //
    // Returns:
    //     The resulting surface pattern.
    [Obsolete("Use getPattern(BitmapExternal bitmap) for easier update to .NET7.0")]
    public static SurfacePattern getPattern(SKBitmap bitmap)
    {
        return new SurfacePattern(getImageSurfaceFromAsset(bitmap))
        {
            Extend = Extend.Repeat
        };
    }

    //
    // Summary:
    //     Gets a surface pattern based off the bitmap.
    //
    // Parameters:
    //   bitmap:
    //     The provided bitmap.
    //
    // Returns:
    //     The resulting surface pattern.
    [Obsolete("Use getPattern(BitmapExternal bitmap) for easier update to .NET7.0")]
    public static SurfacePattern getPattern(BitmapExternal bitmap)
    {
        return new SurfacePattern(getImageSurfaceFromAsset(bitmap))
        {
            Extend = Extend.Repeat
        };
    }

    //
    // Summary:
    //     Gets an image surface based off the bitmap.
    //
    // Parameters:
    //   bitmap:
    //     The provided bitmap.
    //
    // Returns:
    //     The image surface built from the bitmap.
    [Obsolete("Use getImageSurfaceFromAsset(BitmapExternal bitmap) for easier update to .NET7.0")]
    public unsafe static ImageSurface getImageSurfaceFromAsset(SKBitmap bitmap)
    {
        ImageSurface imageSurface = new ImageSurface(Format.Argb32, bitmap.Width, bitmap.Height);
        uint* ptr = (uint*)((IntPtr)imageSurface.DataPtr).ToPointer();
        uint* ptr2 = (uint*)((IntPtr)(nint)bitmap.GetPixels()).ToPointer();
        int num = bitmap.Width * bitmap.Height;
        for (int i = 0; i < num; i++)
        {
            ptr[i] = ptr2[i];
        }

        imageSurface.MarkDirty();
        return imageSurface;
    }

    //
    // Summary:
    //     Gets an image surface based off the bitmap.
    //
    // Parameters:
    //   bitmap:
    //     The provided bitmap.
    //
    // Returns:
    //     The image surface built from the bitmap.
    public unsafe static ImageSurface getImageSurfaceFromAsset(BitmapExternal bitmap)
    {
        ImageSurface imageSurface = new ImageSurface(Format.Argb32, bitmap.Width, bitmap.Height);
        uint* ptr = (uint*)((IntPtr)imageSurface.DataPtr).ToPointer();
        uint* ptr2 = (uint*)((IntPtr)bitmap.PixelsPtrAndLock).ToPointer();
        int num = bitmap.Width * bitmap.Height;
        for (int i = 0; i < num; i++)
        {
            ptr[i] = ptr2[i];
        }

        imageSurface.MarkDirty();
        return imageSurface;
    }

    //
    // Summary:
    //     Gets an image surface based off the bitmap.
    //
    // Parameters:
    //   bitmap:
    //     The provided bitmap.
    //
    //   width:
    //     The width requested.
    //
    //   height:
    //     The height requested.
    //
    // Returns:
    //     The image surface built from the bitmap and data.
    [Obsolete("Use getImageSurfaceFromAsset(BitmapExternal bitmap, int width, int height) for easier update to .NET7.0")]
    public static ImageSurface getImageSurfaceFromAsset(SKBitmap bitmap, int width, int height)
    {
        ImageSurface imageSurface = new ImageSurface(Format.Argb32, width, height);
        imageSurface.Image(bitmap, 0, 0, width, height);
        return imageSurface;
    }

    //
    // Summary:
    //     Gets an image surface based off the bitmap.
    //
    // Parameters:
    //   bitmap:
    //     The provided bitmap.
    //
    //   width:
    //     The width requested.
    //
    //   height:
    //     The height requested.
    //
    // Returns:
    //     The image surface built from the bitmap and data.
    public static ImageSurface getImageSurfaceFromAsset(BitmapExternal bitmap, int width, int height)
    {
        ImageSurface imageSurface = new ImageSurface(Format.Argb32, width, height);
        Vintagestory.API.Common.SurfaceDrawImage.Image(imageSurface, bitmap, 0, 0, width, height);
        return imageSurface;
    }

    public virtual void BeforeCalcBounds()
    {
    }

    //
    // Summary:
    //     Gets a surface pattern from a named file.
    //
    // Parameters:
    //   capi:
    //     The Client API
    //
    //   textureLoc:
    //     The name of the file.
    //
    //   doCache:
    //     Do we cache the file?
    //
    //   mulAlpha:
    //
    //   scale:
    //
    // Returns:
    //     The resulting surface pattern.
    public static SurfacePattern getPattern(ICoreClientAPI capi, AssetLocation textureLoc, bool doCache = true, int mulAlpha = 255, float scale = 1f)
    {
        AssetLocation key = textureLoc.Clone().WithPathPrefix(scale + "-").WithPathPrefix(mulAlpha + "@");
        if (cachedPatterns.ContainsKey(key) && cachedPatterns[key].Key.HandleValid)
        {
            return cachedPatterns[key].Key;
        }

        ImageSurface imageSurfaceFromAsset = getImageSurfaceFromAsset(capi, textureLoc, mulAlpha);
        SurfacePattern surfacePattern = new SurfacePattern(imageSurfaceFromAsset);
        surfacePattern.Extend = Extend.Repeat;
        surfacePattern.Filter = Filter.Nearest;
        if (doCache)
        {
            cachedPatterns[key] = new KeyValuePair<SurfacePattern, ImageSurface>(surfacePattern, imageSurfaceFromAsset);
        }

        Matrix matrix = new Matrix();
        matrix.Scale(scale / RuntimeEnv.GUIScale, scale / RuntimeEnv.GUIScale);
        surfacePattern.Matrix = matrix;
        return surfacePattern;
    }

    //
    // Summary:
    //     Fetches an image surface from a named file.
    //
    // Parameters:
    //   capi:
    //     The Client API
    //
    //   textureLoc:
    //     The name of the text file.
    //
    //   mulAlpha:
    public unsafe static ImageSurface getImageSurfaceFromAsset(ICoreClientAPI capi, AssetLocation textureLoc, int mulAlpha = 255)
    {
        byte[] data = capi.Assets.Get(textureLoc.Clone().WithPathPrefixOnce("textures/")).Data;
        BitmapExternal bitmapExternal = capi.Render.BitmapCreateFromPng(data);
        if (mulAlpha != 255)
        {
            bitmapExternal.MulAlpha(mulAlpha);
        }

        ImageSurface imageSurface = new ImageSurface(Format.Argb32, bitmapExternal.Width, bitmapExternal.Height);
        uint* ptr = (uint*)((IntPtr)imageSurface.DataPtr).ToPointer();
        uint* ptr2 = (uint*)((IntPtr)bitmapExternal.PixelsPtrAndLock).ToPointer();
        int num = bitmapExternal.Width * bitmapExternal.Height;
        for (int i = 0; i < num; i++)
        {
            ptr[i] = ptr2[i];
        }

        imageSurface.MarkDirty();
        bitmapExternal.Dispose();
        return imageSurface;
    }

    //
    // Summary:
    //     Fills an area with a pattern.
    //
    // Parameters:
    //   capi:
    //     The Client API
    //
    //   ctx:
    //     The context of the fill.
    //
    //   textureLoc:
    //     The name of the texture file.
    //
    //   nearestScalingFiler:
    //
    //   preserve:
    //     Whether or not to preserve the aspect ratio of the texture.
    //
    //   mulAlpha:
    //
    //   scale:
    //
    // Returns:
    //     The surface pattern filled with the given texture.
    public static SurfacePattern fillWithPattern(ICoreClientAPI capi, Context ctx, AssetLocation textureLoc, bool nearestScalingFiler = false, bool preserve = false, int mulAlpha = 255, float scale = 1f)
    {
        SurfacePattern pattern = getPattern(capi, textureLoc, doCache: true, mulAlpha, scale);
        if (nearestScalingFiler)
        {
            pattern.Filter = Filter.Nearest;
        }

        ctx.SetSource(pattern);
        if (preserve)
        {
            ctx.FillPreserve();
        }
        else
        {
            ctx.Fill();
        }

        return pattern;
    }

    //
    // Summary:
    //     Discards a pattern based off the the filename.
    //
    // Parameters:
    //   textureLoc:
    //     The pattern to discard.
    public static void DiscardPattern(AssetLocation textureLoc)
    {
        if (cachedPatterns.ContainsKey(textureLoc))
        {
            KeyValuePair<SurfacePattern, ImageSurface> keyValuePair = cachedPatterns[textureLoc];
            keyValuePair.Key.Dispose();
            keyValuePair.Value.Dispose();
            cachedPatterns.Remove(textureLoc);
        }
    }

    internal SurfacePattern paintWithPattern(ICoreClientAPI capi, Context ctx, AssetLocation textureLoc)
    {
        SurfacePattern pattern = getPattern(capi, textureLoc);
        ctx.SetSource(pattern);
        ctx.Paint();
        return pattern;
    }

    protected void Lamp(Context ctx, double x, double y, float[] color)
    {
        ctx.SetSourceRGBA(color[0], color[1], color[2], 1.0);
        RoundRectangle(ctx, x, y, scaled(10.0), scaled(10.0), GuiStyle.ElementBGRadius);
        ctx.Fill();
        EmbossRoundRectangleElement(ctx, x, y, scaled(10.0), scaled(10.0));
    }

    //
    // Summary:
    //     Makes a rectangle with the provided context and bounds.
    //
    // Parameters:
    //   ctx:
    //     The context for the rectangle.
    //
    //   bounds:
    //     The bounds of the rectangle.
    public static void Rectangle(Context ctx, ElementBounds bounds)
    {
        ctx.NewPath();
        ctx.LineTo(bounds.drawX, bounds.drawY);
        ctx.LineTo(bounds.drawX + bounds.OuterWidth, bounds.drawY);
        ctx.LineTo(bounds.drawX + bounds.OuterWidth, bounds.drawY + bounds.OuterHeight);
        ctx.LineTo(bounds.drawX, bounds.drawY + bounds.OuterHeight);
        ctx.ClosePath();
    }

    //
    // Summary:
    //     Makes a rectangle with specified parameters.
    //
    // Parameters:
    //   ctx:
    //     Context of the rectangle
    //
    //   x:
    //     The X position of the rectangle
    //
    //   y:
    //     The Y position of the rectangle
    //
    //   width:
    //     The width of the rectangle
    //
    //   height:
    //     The height of the rectangle.
    public static void Rectangle(Context ctx, double x, double y, double width, double height)
    {
        ctx.NewPath();
        ctx.LineTo(x, y);
        ctx.LineTo(x + width, y);
        ctx.LineTo(x + width, y + height);
        ctx.LineTo(x, y + height);
        ctx.ClosePath();
    }

    //
    // Summary:
    //     Creates a rounded rectangle.
    //
    // Parameters:
    //   ctx:
    //     The GUI context
    //
    //   bounds:
    //     The bounds of the rectangle.
    public void DialogRoundRectangle(Context ctx, ElementBounds bounds)
    {
        RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, GuiStyle.DialogBGRadius);
    }

    //
    // Summary:
    //     Creates a rounded rectangle element.
    //
    // Parameters:
    //   ctx:
    //     The context for the rectangle.
    //
    //   bounds:
    //     The bounds of the rectangle.
    //
    //   isBackground:
    //     Is the rectangle part of a background GUI object (Default: false)
    //
    //   radius:
    //     The radius of the corner of the rectangle (default: -1)
    public void ElementRoundRectangle(Context ctx, ElementBounds bounds, bool isBackground = false, double radius = -1.0)
    {
        if (radius == -1.0)
        {
            radius = GuiStyle.ElementBGRadius;
        }

        if (isBackground)
        {
            RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, radius);
        }
        else
        {
            RoundRectangle(ctx, bounds.drawX, bounds.drawY, bounds.InnerWidth, bounds.InnerHeight, radius);
        }
    }

    //
    // Summary:
    //     Creates a rounded rectangle
    //
    // Parameters:
    //   ctx:
    //     The context for the rectangle.
    //
    //   x:
    //     The X position of the rectangle
    //
    //   y:
    //     The Y position of the rectangle
    //
    //   width:
    //     The width of the rectangle
    //
    //   height:
    //     The height of the rectangle.
    //
    //   radius:
    //     The radius of the corner of the rectangle.
    public static void RoundRectangle(Context ctx, double x, double y, double width, double height, double radius)
    {
        double num = Math.PI / 180.0;
        ctx.Antialias = Antialias.Best;
        ctx.NewPath();
        ctx.Arc(x + width - radius, y + radius, radius, -90.0 * num, 0.0 * num);
        ctx.Arc(x + width - radius, y + height - radius, radius, 0.0 * num, 90.0 * num);
        ctx.Arc(x + radius, y + height - radius, radius, 90.0 * num, 180.0 * num);
        ctx.Arc(x + radius, y + radius, radius, 180.0 * num, 270.0 * num);
        ctx.ClosePath();
    }

    //
    // Summary:
    //     Shades a path with the given context.
    //
    // Parameters:
    //   ctx:
    //     The context of the shading.
    //
    //   thickness:
    //     The thickness of the line to shade.
    public void ShadePath(Context ctx, double thickness = 2.0)
    {
        ctx.Operator = Operator.Atop;
        ctx.SetSourceRGBA(GuiStyle.DialogBorderColor);
        ctx.LineWidth = thickness;
        ctx.Stroke();
        ctx.Operator = Operator.Over;
    }

    //
    // Summary:
    //     Adds an embossed rounded rectangle to the dialog.
    //
    // Parameters:
    //   ctx:
    //     The context of the rectangle.
    //
    //   x:
    //     The X position of the rectangle
    //
    //   y:
    //     The Y position of the rectangle
    //
    //   width:
    //     The width of the rectangle
    //
    //   height:
    //     The height of the rectangle.
    //
    //   inverse:
    //     Whether or not it goes in or out.
    public void EmbossRoundRectangleDialog(Context ctx, double x, double y, double width, double height, bool inverse = false)
    {
        EmbossRoundRectangle(ctx, x, y, width, height, GuiStyle.DialogBGRadius, 4, 0.5f, 0.5f, inverse, 0.25f);
    }

    //
    // Summary:
    //     Adds an embossed rounded rectangle to the dialog.
    //
    // Parameters:
    //   ctx:
    //     The context of the rectangle.
    //
    //   x:
    //     The X position of the rectangle
    //
    //   y:
    //     The Y position of the rectangle
    //
    //   width:
    //     The width of the rectangle
    //
    //   height:
    //     The height of the rectangle.
    //
    //   inverse:
    //     Whether or not it goes in or out.
    //
    //   depth:
    //     The depth of the emboss.
    //
    //   radius:
    //     The radius of the corner of the rectangle.
    public void EmbossRoundRectangleElement(Context ctx, double x, double y, double width, double height, bool inverse = false, int depth = 2, int radius = -1)
    {
        EmbossRoundRectangle(ctx, x, y, width, height, (radius == -1) ? GuiStyle.ElementBGRadius : ((double)radius), depth, 0.7f, 0.8f, inverse, 0.25f);
    }

    //
    // Summary:
    //     Adds an embossed rounded rectangle to the dialog.
    //
    // Parameters:
    //   ctx:
    //     The context of the rectangle.
    //
    //   bounds:
    //     The position and size of the rectangle.
    //
    //   inverse:
    //     Whether or not it goes in or out. (Default: false)
    //
    //   depth:
    //     The depth of the emboss. (Default: 2)
    //
    //   radius:
    //     The radius of the corner of the rectangle. (default: -1)
    public void EmbossRoundRectangleElement(Context ctx, ElementBounds bounds, bool inverse = false, int depth = 2, int radius = -1)
    {
        EmbossRoundRectangle(ctx, bounds.drawX, bounds.drawY, bounds.InnerWidth, bounds.InnerHeight, radius, depth, 0.7f, 0.8f, inverse, 0.25f);
    }

    //
    // Summary:
    //     Adds an embossed rounded rectangle to the dialog.
    //
    // Parameters:
    //   ctx:
    //     The context of the rectangle.
    //
    //   x:
    //     The X position of the rectangle
    //
    //   y:
    //     The Y position of the rectangle
    //
    //   width:
    //     The width of the rectangle
    //
    //   height:
    //     The height of the rectangle.
    //
    //   radius:
    //     The radius of the corner of the rectangle.
    //
    //   depth:
    //     The thickness of the emboss. (Default: 3)
    //
    //   intensity:
    //     The intensity of the emboss. (Default: 0.4f)
    //
    //   lightDarkBalance:
    //     How skewed is the light/dark balance (Default: 1)
    //
    //   inverse:
    //     Whether or not it goes in or out. (Default: false)
    //
    //   alphaOffset:
    //     The offset for the alpha part of the emboss. (Default: 0)
    protected void EmbossRoundRectangle(Context ctx, double x, double y, double width, double height, double radius, int depth = 3, float intensity = 0.4f, float lightDarkBalance = 1f, bool inverse = false, float alphaOffset = 0f)
    {
        double num = Math.PI / 180.0;
        int num2 = depth;
        int num3 = 0;
        ctx.Antialias = Antialias.Best;
        int num4 = 255;
        int num5 = 0;
        if (inverse)
        {
            num4 = 0;
            num5 = 255;
            lightDarkBalance = 2f - lightDarkBalance;
        }

        while (num2-- > 0)
        {
            ctx.NewPath();
            ctx.Arc(x + radius, y + height - radius, radius, 135.0 * num, 180.0 * num);
            ctx.Arc(x + radius, y + radius, radius, 180.0 * num, 270.0 * num);
            ctx.Arc(x + width - radius, y + radius, radius, -90.0 * num, -45.0 * num);
            float num6 = intensity * (float)(depth - num3) / (float)depth;
            double a = Math.Min(1f, lightDarkBalance * num6) - alphaOffset;
            ctx.SetSourceRGBA(num4, num4, num4, a);
            ctx.LineWidth = 1.0;
            ctx.Stroke();
            ctx.NewPath();
            ctx.Arc(x + width - radius, y + radius, radius, -45.0 * num, 0.0 * num);
            ctx.Arc(x + width - radius, y + height - radius, radius, 0.0 * num, 90.0 * num);
            ctx.Arc(x + radius, y + height - radius, radius, 90.0 * num, 135.0 * num);
            a = Math.Min(1f, (2f - lightDarkBalance) * num6) - alphaOffset;
            ctx.SetSourceRGBA(num5, num5, num5, a);
            ctx.LineWidth = 1.0;
            ctx.Stroke();
            num3++;
            x += 1.0;
            y += 1.0;
            width -= 2.0;
            height -= 2.0;
        }
    }

    public virtual void RenderBoundsDebug()
    {
        api.Render.RenderRectangle((int)Bounds.renderX, (int)Bounds.renderY, 500f, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight, OutlineColor());
    }

    //
    // Summary:
    //     The event fired when the mouse is down the element is around. Fires before OnMouseDownOnElement,
    //     however OnMouseDownOnElement is called within the base function.
    //
    // Parameters:
    //   api:
    //     The Client API
    //
    //   mouse:
    //     The mouse event args.
    public virtual void OnMouseDown(ICoreClientAPI api, MouseEvent mouse)
    {
        if (IsPositionInside(mouse.X, mouse.Y))
        {
            OnMouseDownOnElement(api, mouse);
        }
    }

    //
    // Summary:
    //     The event fired when the mouse is pressed while on the element. Called after
    //     OnMouseDown and tells the engine that the event is handled.
    //
    // Parameters:
    //   api:
    //     The Client API
    //
    //   args:
    //     The mouse event args.
    public virtual void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
    {
        args.Handled = true;
    }

    //
    // Summary:
    //     The event fired when the mouse is released on the element. Called after OnMouseUp.
    //
    //
    // Parameters:
    //   api:
    //     The Client API
    //
    //   args:
    //     The mouse event args.
    public virtual void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
    {
    }

    //
    // Summary:
    //     The event fired when the mouse is released.
    //
    // Parameters:
    //   api:
    //     The Client API.
    //
    //   args:
    //     The arguments for the mouse event.
    public virtual void OnMouseUp(ICoreClientAPI api, MouseEvent args)
    {
        if (IsPositionInside(args.X, args.Y))
        {
            OnMouseUpOnElement(api, args);
        }
    }

    public virtual bool OnMouseEnterSlot(ICoreClientAPI api, ItemSlot slot)
    {
        return false;
    }

    public virtual bool OnMouseLeaveSlot(ICoreClientAPI api, ItemSlot slot)
    {
        return false;
    }

    //
    // Summary:
    //     The event fired when the mouse is moved.
    //
    // Parameters:
    //   api:
    //     The Client API.
    //
    //   args:
    //     The mouse event arguments.
    public virtual void OnMouseMove(ICoreClientAPI api, MouseEvent args)
    {
    }

    //
    // Summary:
    //     The event fired when the mouse wheel is scrolled.
    //
    // Parameters:
    //   api:
    //     The Client API
    //
    //   args:
    //     The mouse wheel arguments.
    public virtual void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
    {
    }

    //
    // Summary:
    //     The event fired when a key is held down.
    //
    // Parameters:
    //   api:
    //     The client API
    //
    //   args:
    //     The key event arguments.
    public virtual void OnKeyDown(ICoreClientAPI api, KeyEvent args)
    {
    }

    //
    // Summary:
    //     The event fired when a key is held down.
    //
    // Parameters:
    //   api:
    //     The client API
    //
    //   args:
    //     The key event arguments.
    public virtual void OnKeyUp(ICoreClientAPI api, KeyEvent args)
    {
    }

    //
    // Summary:
    //     The event fired the moment a key is pressed.
    //
    // Parameters:
    //   api:
    //     The Client API.
    //
    //   args:
    //     The keyboard state when the key was pressed.
    public virtual void OnKeyPress(ICoreClientAPI api, KeyEvent args)
    {
    }

    //
    // Summary:
    //     Whether or not the point on screen is inside the Element's area.
    //
    // Parameters:
    //   posX:
    //     The X Position of the point.
    //
    //   posY:
    //     The Y Position of the point.
    public virtual bool IsPositionInside(int posX, int posY)
    {
        if (Bounds.PointInside(posX, posY))
        {
            if (InsideClipBounds != null)
            {
                return InsideClipBounds.PointInside(posX, posY);
            }

            return true;
        }

        return false;
    }

    //
    // Summary:
    //     The compressed version of the debug outline color as a single int value.
    public virtual int OutlineColor()
    {
        return -2130706433;
    }

    protected void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
    {
        if (RenderAsPremultipliedAlpha)
        {
            api.Render.Render2DTexturePremultipliedAlpha(textureid, posX, posY, width, height, z, color);
        }
        else
        {
            api.Render.Render2DTexture(textureid, posX, posY, width, height, z, color);
        }
    }

    protected void Render2DTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
    {
        if (RenderAsPremultipliedAlpha)
        {
            api.Render.Render2DTexturePremultipliedAlpha(textureid, posX, posY, width, height, z, color);
        }
        else
        {
            api.Render.Render2DTexture(textureid, (float)posX, (float)posY, (float)width, (float)height, z, color);
        }
    }

    protected void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
    {
        if (RenderAsPremultipliedAlpha)
        {
            api.Render.Render2DTexturePremultipliedAlpha(textureid, bounds, z, color);
        }
        else
        {
            api.Render.Render2DTexture(textureid, bounds, z, color);
        }
    }

    public virtual void Dispose()
    {
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
