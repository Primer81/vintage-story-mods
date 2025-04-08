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

	/// <summary>
	/// The bounds of the element.
	/// </summary>
	public ElementBounds Bounds;

	/// <summary>
	/// The tab index of the element.
	/// </summary>
	public int TabIndex;

	/// <summary>
	/// Whether or not the element has focus.
	/// </summary>
	protected bool hasFocus;

	/// <summary>
	/// The Client API.
	/// </summary>
	protected ICoreClientAPI api;

	/// <summary>
	/// If the element is inside a clip or not.
	/// </summary>
	public virtual ElementBounds InsideClipBounds { get; set; }

	public bool RenderAsPremultipliedAlpha { get; set; } = true;


	/// <summary>
	/// Whether or not the element has focus or not.
	/// </summary>
	public bool HasFocus => hasFocus;

	/// <summary>
	/// 0 = draw first, 1 = draw last. Only for interactive elements.
	/// </summary>
	public virtual double DrawOrder => 0.0;

	/// <summary>
	/// Whether or not the element can be focused.
	/// </summary>
	public virtual bool Focusable => false;

	/// <summary>
	/// The scale of the element.
	/// </summary>
	public virtual double Scale { get; set; } = 1.0;


	public virtual string MouseOverCursor { get; protected set; }

	/// <summary>
	/// The event fired when the element gains focus.
	/// </summary>
	public virtual void OnFocusGained()
	{
		hasFocus = true;
	}

	/// <summary>
	/// The event fired when the element looses focus.
	/// </summary>
	public virtual void OnFocusLost()
	{
		hasFocus = false;
	}

	/// <summary>
	/// Adds a new GUIElement to the GUI.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="bounds">The bounds of the element.</param>
	public GuiElement(ICoreClientAPI capi, ElementBounds bounds)
	{
		api = capi;
		Bounds = bounds;
	}

	/// <summary>
	/// Composes the elements.
	/// </summary>
	/// <param name="ctxStatic">The context of the components.</param>
	/// <param name="surface">The surface of the GUI.</param>
	public virtual void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
	}

	/// <summary>
	/// Renders the element as an interactive element.
	/// </summary>
	/// <param name="deltaTime">The change in time.</param>
	public virtual void RenderInteractiveElements(float deltaTime)
	{
	}

	/// <summary>
	/// The post render of the interactive element.
	/// </summary>
	/// <param name="deltaTime">The change in time.</param>
	public virtual void PostRenderInteractiveElements(float deltaTime)
	{
	}

	/// <summary>
	/// Renders the focus overlay.
	/// </summary>
	/// <param name="deltaTime">The change in time.</param>
	public void RenderFocusOverlay(float deltaTime)
	{
		ElementBounds bounds = Bounds;
		if (InsideClipBounds != null)
		{
			bounds = InsideClipBounds;
		}
		api.Render.RenderRectangle((int)bounds.renderX, (int)bounds.renderY, 800f, (int)bounds.OuterWidth, (int)bounds.OuterHeight, 1627389951);
	}

	/// <summary>
	/// Generates a texture with an ID.
	/// </summary>
	/// <param name="surface">The image surface supplied.</param>
	/// <param name="textureId">The previous texture id.</param>
	/// <param name="linearMag">Whether or not the texture will have linear magnification.</param>
	protected void generateTexture(ImageSurface surface, ref int textureId, bool linearMag = true)
	{
		int prevTexId = textureId;
		textureId = api.Gui.LoadCairoTexture(surface, linearMag);
		if (prevTexId > 0)
		{
			api.Render.GLDeleteTexture(prevTexId);
		}
	}

	/// <summary>
	/// Generates a new texture.
	/// </summary>
	/// <param name="surface">The surface provided.</param>
	/// <param name="intoTexture">The texture to be loaded into.</param>
	/// <param name="linearMag">Whether or not the texture will have linear magnification.</param>
	protected void generateTexture(ImageSurface surface, ref LoadedTexture intoTexture, bool linearMag = true)
	{
		api.Gui.LoadOrUpdateCairoTexture(surface, linearMag, ref intoTexture);
	}

	/// <summary>
	/// Changes the scale of given value by the GUIScale factor.
	/// </summary>
	/// <param name="value"></param>
	/// <returns>The scaled value based</returns>
	public static double scaled(double value)
	{
		return value * (double)RuntimeEnv.GUIScale;
	}

	/// <summary>
	/// Changes the scale of given value by the GUIScale factor
	/// </summary>
	/// <param name="value"></param>
	/// <returns>Scaled value type cast to int</returns>
	public static int scaledi(double value)
	{
		return (int)(value * (double)RuntimeEnv.GUIScale);
	}

	/// <summary>
	/// Generates context based off the image surface.
	/// </summary>
	/// <param name="surface">The surface where the context is based.</param>
	/// <returns>The context based off the provided surface.</returns>
	protected Context genContext(ImageSurface surface)
	{
		Context context = new Context(surface);
		context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		context.Paint();
		context.Antialias = Antialias.Best;
		return context;
	}

	/// <summary>
	/// Gets a surface pattern based off the bitmap.
	/// </summary>
	/// <param name="bitmap">The provided bitmap.</param>
	/// <returns>The resulting surface pattern.</returns>
	[Obsolete("Use getPattern(BitmapExternal bitmap) for easier update to .NET7.0")]
	public static SurfacePattern getPattern(SKBitmap bitmap)
	{
		return new SurfacePattern(getImageSurfaceFromAsset(bitmap))
		{
			Extend = Extend.Repeat
		};
	}

	/// <summary>
	/// Gets a surface pattern based off the bitmap.
	/// </summary>
	/// <param name="bitmap">The provided bitmap.</param>
	/// <returns>The resulting surface pattern.</returns>
	[Obsolete("Use getPattern(BitmapExternal bitmap) for easier update to .NET7.0")]
	public static SurfacePattern getPattern(BitmapExternal bitmap)
	{
		return new SurfacePattern(getImageSurfaceFromAsset(bitmap))
		{
			Extend = Extend.Repeat
		};
	}

	/// <summary>
	/// Gets an image surface based off the bitmap.
	/// </summary>
	/// <param name="bitmap">The provided bitmap.</param>
	/// <returns>The image surface built from the bitmap.</returns>
	[Obsolete("Use getImageSurfaceFromAsset(BitmapExternal bitmap) for easier update to .NET7.0")]
	public unsafe static ImageSurface getImageSurfaceFromAsset(SKBitmap bitmap)
	{
		ImageSurface imageSurface = new ImageSurface(Format.Argb32, bitmap.Width, bitmap.Height);
		uint* destPixels = (uint*)((IntPtr)imageSurface.DataPtr).ToPointer();
		uint* sourcePixels = (uint*)((IntPtr)bitmap.GetPixels()).ToPointer();
		int size = bitmap.Width * bitmap.Height;
		for (int i = 0; i < size; i++)
		{
			destPixels[i] = sourcePixels[i];
		}
		imageSurface.MarkDirty();
		return imageSurface;
	}

	/// <summary>
	/// Gets an image surface based off the bitmap.
	/// </summary>
	/// <param name="bitmap">The provided bitmap.</param>
	/// <returns>The image surface built from the bitmap.</returns>
	public unsafe static ImageSurface getImageSurfaceFromAsset(BitmapExternal bitmap)
	{
		ImageSurface imageSurface = new ImageSurface(Format.Argb32, bitmap.Width, bitmap.Height);
		uint* destPixels = (uint*)((IntPtr)imageSurface.DataPtr).ToPointer();
		uint* sourcePixels = (uint*)((IntPtr)bitmap.PixelsPtrAndLock).ToPointer();
		int size = bitmap.Width * bitmap.Height;
		for (int i = 0; i < size; i++)
		{
			destPixels[i] = sourcePixels[i];
		}
		imageSurface.MarkDirty();
		return imageSurface;
	}

	/// <summary>
	/// Gets an image surface based off the bitmap.
	/// </summary>
	/// <param name="bitmap">The provided bitmap.</param>
	/// <param name="width">The width requested.</param>
	/// <param name="height">The height requested.</param>
	/// <returns>The image surface built from the bitmap and data.</returns>
	[Obsolete("Use getImageSurfaceFromAsset(BitmapExternal bitmap, int width, int height) for easier update to .NET7.0")]
	public static ImageSurface getImageSurfaceFromAsset(SKBitmap bitmap, int width, int height)
	{
		ImageSurface imageSurface = new ImageSurface(Format.Argb32, width, height);
		imageSurface.Image(bitmap, 0, 0, width, height);
		return imageSurface;
	}

	/// <summary>
	/// Gets an image surface based off the bitmap.
	/// </summary>
	/// <param name="bitmap">The provided bitmap.</param>
	/// <param name="width">The width requested.</param>
	/// <param name="height">The height requested.</param>
	/// <returns>The image surface built from the bitmap and data.</returns>
	public static ImageSurface getImageSurfaceFromAsset(BitmapExternal bitmap, int width, int height)
	{
		ImageSurface imageSurface = new ImageSurface(Format.Argb32, width, height);
		imageSurface.Image(bitmap, 0, 0, width, height);
		return imageSurface;
	}

	public virtual void BeforeCalcBounds()
	{
	}

	/// <summary>
	/// Gets a surface pattern from a named file.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="textureLoc">The name of the file.</param>
	/// <param name="doCache">Do we cache the file?</param>
	/// <param name="mulAlpha"></param>
	/// <param name="scale"></param>
	/// <returns>The resulting surface pattern.</returns>
	public static SurfacePattern getPattern(ICoreClientAPI capi, AssetLocation textureLoc, bool doCache = true, int mulAlpha = 255, float scale = 1f)
	{
		AssetLocation cacheKey = textureLoc.Clone().WithPathPrefix(scale + "-").WithPathPrefix(mulAlpha + "@");
		if (cachedPatterns.ContainsKey(cacheKey) && cachedPatterns[cacheKey].Key.HandleValid)
		{
			return cachedPatterns[cacheKey].Key;
		}
		ImageSurface patternSurface = getImageSurfaceFromAsset(capi, textureLoc, mulAlpha);
		SurfacePattern pattern = new SurfacePattern(patternSurface);
		pattern.Extend = Extend.Repeat;
		pattern.Filter = Filter.Nearest;
		if (doCache)
		{
			cachedPatterns[cacheKey] = new KeyValuePair<SurfacePattern, ImageSurface>(pattern, patternSurface);
		}
		Matrix i = new Matrix();
		i.Scale(scale / RuntimeEnv.GUIScale, scale / RuntimeEnv.GUIScale);
		pattern.Matrix = i;
		return pattern;
	}

	/// <summary>
	/// Fetches an image surface from a named file.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="textureLoc">The name of the text file.</param>
	/// <param name="mulAlpha"></param>
	/// <returns></returns>
	public unsafe static ImageSurface getImageSurfaceFromAsset(ICoreClientAPI capi, AssetLocation textureLoc, int mulAlpha = 255)
	{
		byte[] data = capi.Assets.Get(textureLoc.Clone().WithPathPrefixOnce("textures/")).Data;
		BitmapExternal bitmapExternal = capi.Render.BitmapCreateFromPng(data);
		if (mulAlpha != 255)
		{
			bitmapExternal.MulAlpha(mulAlpha);
		}
		ImageSurface imageSurface = new ImageSurface(Format.Argb32, bitmapExternal.Width, bitmapExternal.Height);
		uint* destPixels = (uint*)((IntPtr)imageSurface.DataPtr).ToPointer();
		uint* sourcePixels = (uint*)((IntPtr)bitmapExternal.PixelsPtrAndLock).ToPointer();
		int size = bitmapExternal.Width * bitmapExternal.Height;
		for (int i = 0; i < size; i++)
		{
			destPixels[i] = sourcePixels[i];
		}
		imageSurface.MarkDirty();
		bitmapExternal.Dispose();
		return imageSurface;
	}

	/// <summary>
	/// Fills an area with a pattern.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="ctx">The context of the fill.</param>
	/// <param name="textureLoc">The name of the texture file.</param>
	/// <param name="nearestScalingFiler"></param>
	/// <param name="preserve">Whether or not to preserve the aspect ratio of the texture.</param>
	/// <param name="mulAlpha"></param>
	/// <param name="scale"></param>
	/// <returns>The surface pattern filled with the given texture.</returns>
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

	/// <summary>
	/// Discards a pattern based off the the filename.
	/// </summary>
	/// <param name="textureLoc">The pattern to discard.</param>
	public static void DiscardPattern(AssetLocation textureLoc)
	{
		if (cachedPatterns.ContainsKey(textureLoc))
		{
			KeyValuePair<SurfacePattern, ImageSurface> val = cachedPatterns[textureLoc];
			val.Key.Dispose();
			val.Value.Dispose();
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

	/// <summary>
	/// Makes a rectangle with the provided context and bounds.
	/// </summary>
	/// <param name="ctx">The context for the rectangle.</param>
	/// <param name="bounds">The bounds of the rectangle.</param>
	public static void Rectangle(Context ctx, ElementBounds bounds)
	{
		ctx.NewPath();
		ctx.LineTo(bounds.drawX, bounds.drawY);
		ctx.LineTo(bounds.drawX + bounds.OuterWidth, bounds.drawY);
		ctx.LineTo(bounds.drawX + bounds.OuterWidth, bounds.drawY + bounds.OuterHeight);
		ctx.LineTo(bounds.drawX, bounds.drawY + bounds.OuterHeight);
		ctx.ClosePath();
	}

	/// <summary>
	/// Makes a rectangle with specified parameters.
	/// </summary>
	/// <param name="ctx">Context of the rectangle</param>
	/// <param name="x">The X position of the rectangle</param>
	/// <param name="y">The Y position of the rectangle</param>
	/// <param name="width">The width of the rectangle</param>
	/// <param name="height">The height of the rectangle.</param>
	public static void Rectangle(Context ctx, double x, double y, double width, double height)
	{
		ctx.NewPath();
		ctx.LineTo(x, y);
		ctx.LineTo(x + width, y);
		ctx.LineTo(x + width, y + height);
		ctx.LineTo(x, y + height);
		ctx.ClosePath();
	}

	/// <summary>
	/// Creates a rounded rectangle.
	/// </summary>
	/// <param name="ctx">The GUI context</param>
	/// <param name="bounds">The bounds of the rectangle.</param>
	public void DialogRoundRectangle(Context ctx, ElementBounds bounds)
	{
		RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, GuiStyle.DialogBGRadius);
	}

	/// <summary>
	/// Creates a rounded rectangle element.
	/// </summary>
	/// <param name="ctx">The context for the rectangle.</param>
	/// <param name="bounds">The bounds of the rectangle.</param>
	/// <param name="isBackground">Is the rectangle part of a background GUI object (Default: false)</param>
	/// <param name="radius">The radius of the corner of the rectangle (default: -1)</param>
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

	/// <summary>
	/// Creates a rounded rectangle
	/// </summary>
	/// <param name="ctx">The context for the rectangle.</param>
	/// <param name="x">The X position of the rectangle</param>
	/// <param name="y">The Y position of the rectangle</param>
	/// <param name="width">The width of the rectangle</param>
	/// <param name="height">The height of the rectangle.</param>
	/// <param name="radius">The radius of the corner of the rectangle.</param>
	public static void RoundRectangle(Context ctx, double x, double y, double width, double height, double radius)
	{
		double degrees = Math.PI / 180.0;
		ctx.Antialias = Antialias.Best;
		ctx.NewPath();
		ctx.Arc(x + width - radius, y + radius, radius, -90.0 * degrees, 0.0 * degrees);
		ctx.Arc(x + width - radius, y + height - radius, radius, 0.0 * degrees, 90.0 * degrees);
		ctx.Arc(x + radius, y + height - radius, radius, 90.0 * degrees, 180.0 * degrees);
		ctx.Arc(x + radius, y + radius, radius, 180.0 * degrees, 270.0 * degrees);
		ctx.ClosePath();
	}

	/// <summary>
	/// Shades a path with the given context.
	/// </summary>
	/// <param name="ctx">The context of the shading.</param>
	/// <param name="thickness">The thickness of the line to shade.</param>
	public void ShadePath(Context ctx, double thickness = 2.0)
	{
		ctx.Operator = Operator.Atop;
		ctx.SetSourceRGBA(GuiStyle.DialogBorderColor);
		ctx.LineWidth = thickness;
		ctx.Stroke();
		ctx.Operator = Operator.Over;
	}

	/// <summary>
	/// Adds an embossed rounded rectangle to the dialog.
	/// </summary>
	/// <param name="ctx">The context of the rectangle.</param>
	/// <param name="x">The X position of the rectangle</param>
	/// <param name="y">The Y position of the rectangle</param>
	/// <param name="width">The width of the rectangle</param>
	/// <param name="height">The height of the rectangle.</param>
	/// <param name="inverse">Whether or not it goes in or out.</param>
	public void EmbossRoundRectangleDialog(Context ctx, double x, double y, double width, double height, bool inverse = false)
	{
		EmbossRoundRectangle(ctx, x, y, width, height, GuiStyle.DialogBGRadius, 4, 0.5f, 0.5f, inverse, 0.25f);
	}

	/// <summary>
	/// Adds an embossed rounded rectangle to the dialog.
	/// </summary>
	/// <param name="ctx">The context of the rectangle.</param>
	/// <param name="x">The X position of the rectangle</param>
	/// <param name="y">The Y position of the rectangle</param>
	/// <param name="width">The width of the rectangle</param>
	/// <param name="height">The height of the rectangle.</param>
	/// <param name="inverse">Whether or not it goes in or out.</param>
	/// <param name="depth">The depth of the emboss.</param>
	/// <param name="radius">The radius of the corner of the rectangle.</param>
	public void EmbossRoundRectangleElement(Context ctx, double x, double y, double width, double height, bool inverse = false, int depth = 2, int radius = -1)
	{
		EmbossRoundRectangle(ctx, x, y, width, height, (radius == -1) ? GuiStyle.ElementBGRadius : ((double)radius), depth, 0.7f, 0.8f, inverse, 0.25f);
	}

	/// <summary>
	/// Adds an embossed rounded rectangle to the dialog.
	/// </summary>
	/// <param name="ctx">The context of the rectangle.</param>
	/// <param name="bounds">The position and size of the rectangle.</param>
	/// <param name="inverse">Whether or not it goes in or out. (Default: false)</param>
	/// <param name="depth">The depth of the emboss. (Default: 2)</param>
	/// <param name="radius">The radius of the corner of the rectangle. (default: -1)</param>
	public void EmbossRoundRectangleElement(Context ctx, ElementBounds bounds, bool inverse = false, int depth = 2, int radius = -1)
	{
		EmbossRoundRectangle(ctx, bounds.drawX, bounds.drawY, bounds.InnerWidth, bounds.InnerHeight, radius, depth, 0.7f, 0.8f, inverse, 0.25f);
	}

	/// <summary>
	/// Adds an embossed rounded rectangle to the dialog.
	/// </summary>
	/// <param name="ctx">The context of the rectangle.</param>
	/// <param name="x">The X position of the rectangle</param>
	/// <param name="y">The Y position of the rectangle</param>
	/// <param name="width">The width of the rectangle</param>
	/// <param name="height">The height of the rectangle.</param>
	/// <param name="radius">The radius of the corner of the rectangle.</param>
	/// <param name="depth">The thickness of the emboss. (Default: 3)</param>
	/// <param name="intensity">The intensity of the emboss. (Default: 0.4f)</param>
	/// <param name="lightDarkBalance">How skewed is the light/dark balance (Default: 1)</param>
	/// <param name="inverse">Whether or not it goes in or out. (Default: false)</param>
	/// <param name="alphaOffset">The offset for the alpha part of the emboss. (Default: 0)</param>
	protected void EmbossRoundRectangle(Context ctx, double x, double y, double width, double height, double radius, int depth = 3, float intensity = 0.4f, float lightDarkBalance = 1f, bool inverse = false, float alphaOffset = 0f)
	{
		double degrees = Math.PI / 180.0;
		int i = depth;
		int linewidth = 0;
		ctx.Antialias = Antialias.Best;
		int light = 255;
		int dark = 0;
		if (inverse)
		{
			light = 0;
			dark = 255;
			lightDarkBalance = 2f - lightDarkBalance;
		}
		while (i-- > 0)
		{
			ctx.NewPath();
			ctx.Arc(x + radius, y + height - radius, radius, 135.0 * degrees, 180.0 * degrees);
			ctx.Arc(x + radius, y + radius, radius, 180.0 * degrees, 270.0 * degrees);
			ctx.Arc(x + width - radius, y + radius, radius, -90.0 * degrees, -45.0 * degrees);
			float fac = intensity * (float)(depth - linewidth) / (float)depth;
			double alpha = Math.Min(1f, lightDarkBalance * fac) - alphaOffset;
			ctx.SetSourceRGBA(light, light, light, alpha);
			ctx.LineWidth = 1.0;
			ctx.Stroke();
			ctx.NewPath();
			ctx.Arc(x + width - radius, y + radius, radius, -45.0 * degrees, 0.0 * degrees);
			ctx.Arc(x + width - radius, y + height - radius, radius, 0.0 * degrees, 90.0 * degrees);
			ctx.Arc(x + radius, y + height - radius, radius, 90.0 * degrees, 135.0 * degrees);
			alpha = Math.Min(1f, (2f - lightDarkBalance) * fac) - alphaOffset;
			ctx.SetSourceRGBA(dark, dark, dark, alpha);
			ctx.LineWidth = 1.0;
			ctx.Stroke();
			linewidth++;
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

	/// <summary>
	/// The event fired when the mouse is down the element is around.  Fires before OnMouseDownOnElement, however OnMouseDownOnElement is called within the base function.
	/// </summary>
	/// <param name="api">The Client API</param>
	/// <param name="mouse">The mouse event args.</param>
	public virtual void OnMouseDown(ICoreClientAPI api, MouseEvent mouse)
	{
		if (IsPositionInside(mouse.X, mouse.Y))
		{
			OnMouseDownOnElement(api, mouse);
		}
	}

	/// <summary>
	/// The event fired when the mouse is pressed while on the element. Called after OnMouseDown and tells the engine that the event is handled.
	/// </summary>
	/// <param name="api">The Client API</param>
	/// <param name="args">The mouse event args.</param>
	public virtual void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		args.Handled = true;
	}

	/// <summary>
	/// The event fired when the mouse is released on the element.  Called after OnMouseUp.  
	/// </summary>
	/// <param name="api">The Client API</param>
	/// <param name="args">The mouse event args.</param>
	public virtual void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	/// <summary>
	/// The event fired when the mouse is released.  
	/// </summary>
	/// <param name="api">The Client API.</param>
	/// <param name="args">The arguments for the mouse event.</param>
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

	/// <summary>
	/// The event fired when the mouse is moved.
	/// </summary>
	/// <param name="api">The Client API.</param>
	/// <param name="args">The mouse event arguments.</param>
	public virtual void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
	}

	/// <summary>
	/// The event fired when the mouse wheel is scrolled.
	/// </summary>
	/// <param name="api">The Client API</param>
	/// <param name="args">The mouse wheel arguments.</param>
	public virtual void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
	}

	/// <summary>
	/// The event fired when a key is held down.
	/// </summary>
	/// <param name="api">The client API</param>
	/// <param name="args">The key event arguments.</param>
	public virtual void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
	}

	/// <summary>
	/// The event fired when a key is held down.
	/// </summary>
	/// <param name="api">The client API</param>
	/// <param name="args">The key event arguments.</param>
	public virtual void OnKeyUp(ICoreClientAPI api, KeyEvent args)
	{
	}

	/// <summary>
	/// The event fired the moment a key is pressed.
	/// </summary>
	/// <param name="api">The Client API.</param>
	/// <param name="args">The keyboard state when the key was pressed.</param>
	public virtual void OnKeyPress(ICoreClientAPI api, KeyEvent args)
	{
	}

	/// <summary>
	/// Whether or not the point on screen is inside the Element's area.
	/// </summary>
	/// <param name="posX">The X Position of the point.</param>
	/// <param name="posY">The Y Position of the point.</param>
	/// <returns></returns>
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

	/// <summary>
	/// The compressed version of the debug outline color as a single int value.
	/// </summary>
	/// <returns></returns>
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
