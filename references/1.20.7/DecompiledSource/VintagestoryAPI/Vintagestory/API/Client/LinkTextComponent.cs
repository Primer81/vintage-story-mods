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

	/// <summary>
	/// Create a dummy link text component for use with triggering link protocols through code. Not usable for anything gui related (it'll crash if you try)
	/// </summary>
	/// <param name="href"></param>
	public LinkTextComponent(string href)
		: base(null, "", null)
	{
		Href = href;
	}

	/// <summary>
	/// A text component with an embedded link.
	/// </summary>
	/// <param name="api"></param>
	/// <param name="displayText">The text of the Text.</param>
	/// <param name="font"></param>
	/// <param name="onLinkClicked"></param>
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
		double rightMostX = 0.0;
		double bottomMostY = 0.0;
		for (int i = 0; i < Lines.Length; i++)
		{
			TextLine line = Lines[i];
			leftMostX = Math.Min(leftMostX, line.Bounds.X);
			topMostY = Math.Min(topMostY, line.Bounds.Y);
			rightMostX = Math.Max(rightMostX, line.Bounds.X + line.Bounds.Width);
			bottomMostY = Math.Max(bottomMostY, line.Bounds.Y + line.Bounds.Height);
		}
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)(rightMostX - leftMostX), (int)(bottomMostY - topMostY));
		Context ctx = new Context(surface);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Paint();
		ctx.Save();
		Matrix j = ctx.Matrix;
		j.Translate((int)(0.0 - leftMostX), (int)(0.0 - topMostY));
		ctx.Matrix = j;
		CairoFont normalFont = Font;
		ComposeFor(ctx, surface);
		api.Gui.LoadOrUpdateCairoTexture(surface, linearMag: false, ref normalText);
		ctx.Operator = Operator.Clear;
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Paint();
		ctx.Operator = Operator.Over;
		Font = Font.Clone();
		Font.Color[0] = Math.Min(1.0, Font.Color[0] * 1.2);
		Font.Color[1] = Math.Min(1.0, Font.Color[1] * 1.2);
		Font.Color[2] = Math.Min(1.0, Font.Color[2] * 1.2);
		ComposeFor(ctx, surface);
		Font = normalFont;
		ctx.Restore();
		api.Gui.LoadOrUpdateCairoTexture(surface, linearMag: false, ref hoverText);
		surface.Dispose();
		ctx.Dispose();
	}

	private void ComposeFor(Context ctx, ImageSurface surface)
	{
		textUtil.DrawMultilineText(ctx, Font, Lines);
		ctx.LineWidth = 1.0;
		ctx.SetSourceRGBA(Font.Color);
		for (int i = 0; i < Lines.Length; i++)
		{
			TextLine line = Lines[i];
			ctx.MoveTo(line.Bounds.X, line.Bounds.Y + line.Bounds.AscentOrHeight + 2.0);
			ctx.LineTo(line.Bounds.X + line.Bounds.Width, line.Bounds.Y + line.Bounds.AscentOrHeight + 2.0);
			ctx.Stroke();
		}
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		base.RenderInteractiveElements(deltaTime, renderX, renderY, renderZ);
		isHover = false;
		double offsetX = GetFontOrientOffsetX();
		if (clickable)
		{
			LineRectangled[] boundsPerLine = BoundsPerLine;
			for (int i = 0; i < boundsPerLine.Length; i++)
			{
				if (boundsPerLine[i].PointInside((double)api.Input.MouseX - renderX - offsetX, (double)api.Input.MouseY - renderY))
				{
					isHover = true;
					break;
				}
			}
		}
		api.Render.Render2DTexturePremultipliedAlpha(isHover ? hoverText.TextureId : normalText.TextureId, (int)(renderX + leftMostX + offsetX), (int)(renderY + topMostY), hoverText.Width, hoverText.Height, (float)renderZ + 50f);
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
		double offsetX = GetFontOrientOffsetX();
		wasMouseDown = false;
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside((double)args.X - offsetX, args.Y))
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
		double offsetX = GetFontOrientOffsetX();
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside((double)args.X - offsetX, args.Y))
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
		string[] parts = Href.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length != 0 && api.LinkProtocols != null && api.LinkProtocols.ContainsKey(parts[0]))
		{
			api.LinkProtocols[parts[0]](this);
		}
		else if (parts.Length != 0 && parts[0].StartsWithOrdinal("http"))
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
