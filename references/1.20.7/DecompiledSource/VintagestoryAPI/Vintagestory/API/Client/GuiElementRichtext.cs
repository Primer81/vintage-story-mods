using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

public class GuiElementRichtext : GuiElement
{
	public static bool DebugLogging;

	protected TextFlowPath[] flowPath;

	public RichTextComponentBase[] Components;

	public float zPos = 50f;

	public LoadedTexture richtTextTexture;

	public bool Debug;

	public Vec4f RenderColor;

	private ImageSurface surface;

	private Context ctx;

	public bool HalfComposed;

	public int MaxHeight { get; set; } = int.MaxValue;


	public double MaxLineWidth
	{
		get
		{
			if (flowPath == null)
			{
				return 0.0;
			}
			double x = 0.0;
			for (int i = 0; i < Components.Length; i++)
			{
				RichTextComponentBase cmp = Components[i];
				for (int j = 0; j < cmp.BoundsPerLine.Length; j++)
				{
					x = Math.Max(x, cmp.BoundsPerLine[j].X + cmp.BoundsPerLine[j].Width);
				}
			}
			return x;
		}
	}

	public double TotalHeight
	{
		get
		{
			if (flowPath == null)
			{
				return 0.0;
			}
			double y = 0.0;
			for (int i = 0; i < Components.Length; i++)
			{
				RichTextComponentBase cmp = Components[i];
				for (int j = 0; j < cmp.BoundsPerLine.Length; j++)
				{
					y = Math.Max(y, cmp.BoundsPerLine[j].Y + cmp.BoundsPerLine[j].Height);
				}
			}
			return y;
		}
	}

	public GuiElementRichtext(ICoreClientAPI capi, RichTextComponentBase[] components, ElementBounds bounds)
		: base(capi, bounds)
	{
		Components = components;
		richtTextTexture = new LoadedTexture(capi);
	}

	public override void BeforeCalcBounds()
	{
		CalcHeightAndPositions();
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Compose();
	}

	public void Compose(bool genTextureLater = false)
	{
		ElementBounds rtbounds = Bounds.CopyOnlySize();
		rtbounds.fixedPaddingX = 0.0;
		rtbounds.fixedPaddingY = 0.0;
		Bounds.CalcWorldBounds();
		int wdt = (int)Bounds.InnerWidth;
		int hgt = (int)Bounds.InnerHeight;
		if (richtTextTexture.TextureId != 0)
		{
			wdt = Math.Max(1, Math.Max(wdt, richtTextTexture.Width));
			hgt = Math.Max(1, Math.Max(hgt, richtTextTexture.Height));
		}
		surface = new ImageSurface(Format.Argb32, wdt, Math.Min(MaxHeight, hgt));
		ctx = new Context(surface);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Paint();
		if (!genTextureLater)
		{
			ComposeFor(rtbounds, ctx, surface);
			generateTexture(surface, ref richtTextTexture);
			ctx.Dispose();
			surface.Dispose();
			ctx = null;
			surface = null;
		}
		else
		{
			HalfComposed = true;
		}
	}

	public void genTexture()
	{
		generateTexture(surface, ref richtTextTexture);
		ctx.Dispose();
		surface.Dispose();
		ctx = null;
		surface = null;
		HalfComposed = false;
	}

	public void CalcHeightAndPositions()
	{
		Bounds.CalcWorldBounds();
		if (DebugLogging)
		{
			api.Logger.VerboseDebug("GuiElementRichtext: before bounds: {0}/{1}  w/h = {2},{3}", Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
		}
		double posX = 0.0;
		double posY = 0.0;
		List<int> currentLine = new List<int>();
		List<TextFlowPath> flowPathList = new List<TextFlowPath>();
		flowPathList.Add(new TextFlowPath(Bounds.InnerWidth));
		double lineHeight = 0.0;
		double ascentHeight = 0.0;
		RichTextComponentBase comp = null;
		for (int i = 0; i < Components.Length; i++)
		{
			comp = Components[i];
			double nextPosX;
			EnumCalcBoundsResult calcBoundResult = comp.CalcBounds(flowPathList.ToArray(), lineHeight, posX, posY, out nextPosX);
			if (comp.Float == EnumFloat.Inline)
			{
				posX = nextPosX;
			}
			if (DebugLogging)
			{
				api.Logger.VerboseDebug("GuiElementRichtext, add comp {0}, posY={1}, lineHeight={2}", i, posY, lineHeight);
				if (comp.BoundsPerLine.Length != 0)
				{
					api.Logger.VerboseDebug("GuiElementRichtext, Comp bounds 0 w/h: {0}/{1}", comp.BoundsPerLine[0].Width, comp.BoundsPerLine[0].Height);
				}
			}
			if (comp.Float == EnumFloat.None)
			{
				posX = 0.0;
				posY += Math.Max(lineHeight, comp.BoundsPerLine[0].Height) + ((calcBoundResult != 0) ? GuiElement.scaled(comp.UnscaledMarginTop) : 0.0);
				posY = Math.Ceiling(posY);
				handleVerticalAlignment(currentLine, ascentHeight);
				lineHeight = 0.0;
				ascentHeight = 0.0;
				currentLine.Clear();
				continue;
			}
			if (calcBoundResult != 0)
			{
				adjustLineTextAlignment(currentLine);
				lineHeight = Math.Max(lineHeight, comp.BoundsPerLine[0].Height);
				ascentHeight = Math.Max(ascentHeight, comp.BoundsPerLine[0].AscentOrHeight);
				handleVerticalAlignment(currentLine, ascentHeight);
				if (calcBoundResult == EnumCalcBoundsResult.Multiline)
				{
					if (comp.VerticalAlign == EnumVerticalAlign.Bottom)
					{
						LineRectangled[] boundsPerLine = comp.BoundsPerLine;
						foreach (LineRectangled obj in boundsPerLine)
						{
							obj.Y = Math.Ceiling(obj.Y + ascentHeight - comp.BoundsPerLine[0].AscentOrHeight);
						}
					}
					if (comp.VerticalAlign == EnumVerticalAlign.Middle)
					{
						LineRectangled[] boundsPerLine = comp.BoundsPerLine;
						foreach (LineRectangled obj2 in boundsPerLine)
						{
							obj2.Y = Math.Ceiling(obj2.Y + ascentHeight / 2.0 - comp.BoundsPerLine[0].AscentOrHeight / 2.0);
						}
					}
					if (comp.VerticalAlign == EnumVerticalAlign.FixedOffset)
					{
						LineRectangled[] boundsPerLine = comp.BoundsPerLine;
						foreach (LineRectangled obj3 in boundsPerLine)
						{
							obj3.Y = Math.Ceiling(obj3.Y + comp.UnscaledMarginTop);
						}
					}
				}
				currentLine.Clear();
				currentLine.Add(i);
				posY += lineHeight;
				for (int j = 1; j < comp.BoundsPerLine.Length - 1; j++)
				{
					posY += comp.BoundsPerLine[j].Height;
				}
				posY += GuiElement.scaled(comp.UnscaledMarginTop);
				posY = Math.Ceiling(posY);
				LineRectangled lastBound = comp.BoundsPerLine[comp.BoundsPerLine.Length - 1];
				if (lastBound.Width > 0.0)
				{
					lineHeight = lastBound.Height;
					ascentHeight = lastBound.AscentOrHeight;
				}
				else
				{
					lineHeight = 0.0;
					ascentHeight = 0.0;
				}
			}
			else if (comp.Float == EnumFloat.Inline && comp.BoundsPerLine.Length != 0)
			{
				lineHeight = Math.Max(comp.BoundsPerLine[0].Height, lineHeight);
				ascentHeight = Math.Max(comp.BoundsPerLine[0].AscentOrHeight, ascentHeight);
				currentLine.Add(i);
			}
			if (comp.Float != EnumFloat.Inline)
			{
				ConstrainTextFlowPath(flowPathList, posY, comp);
			}
		}
		if (DebugLogging)
		{
			api.Logger.VerboseDebug("GuiElementRichtext: after loop. posY = {0}", posY);
		}
		if (comp != null && posX > 0.0 && comp.BoundsPerLine.Length != 0)
		{
			posY += lineHeight;
		}
		Bounds.fixedHeight = (posY + 1.0) / (double)RuntimeEnv.GUIScale;
		adjustLineTextAlignment(currentLine);
		double maxHeight = 0.0;
		foreach (int index2 in currentLine)
		{
			RichTextComponentBase lineComp2 = Components[index2];
			Rectangled lastLineBounds2 = lineComp2.BoundsPerLine[lineComp2.BoundsPerLine.Length - 1];
			maxHeight = Math.Max(maxHeight, lastLineBounds2.Height);
		}
		foreach (int index in currentLine)
		{
			RichTextComponentBase lineComp = Components[index];
			Rectangled lastLineBounds = lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1];
			if (lineComp.VerticalAlign == EnumVerticalAlign.Bottom)
			{
				lastLineBounds.Y = Math.Ceiling(lastLineBounds.Y + ascentHeight - lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1].AscentOrHeight);
			}
			else if (lineComp.VerticalAlign == EnumVerticalAlign.Middle)
			{
				lastLineBounds.Y += (maxHeight - lastLineBounds.Height) / 2.0;
			}
			else if (lineComp.VerticalAlign == EnumVerticalAlign.FixedOffset)
			{
				lastLineBounds.Y += lineComp.UnscaledMarginTop;
			}
		}
		flowPath = flowPathList.ToArray();
		if (DebugLogging)
		{
			api.Logger.VerboseDebug("GuiElementRichtext: after bounds: {0}/{1}  w/h = {2},{3}", Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			api.Logger.VerboseDebug("GuiElementRichtext: posY = {0}", posY);
			api.Logger.VerboseDebug("GuiElementRichtext: framewidth/height: {0}/{1}", api.Render.FrameWidth, api.Render.FrameHeight);
		}
	}

	private void handleVerticalAlignment(List<int> currentLine, double ascentHeight)
	{
		foreach (int index in currentLine)
		{
			RichTextComponentBase lineComp = Components[index];
			LineRectangled lastLineBounds = lineComp.BoundsPerLine[lineComp.BoundsPerLine.Length - 1];
			if (lineComp.VerticalAlign == EnumVerticalAlign.Bottom)
			{
				lastLineBounds.Y = Math.Ceiling(lastLineBounds.Y + ascentHeight - lastLineBounds.AscentOrHeight);
			}
			else if (lineComp.VerticalAlign == EnumVerticalAlign.Middle)
			{
				lastLineBounds.Y = Math.Ceiling(lastLineBounds.Y + ascentHeight / 2.0 - lastLineBounds.AscentOrHeight / 2.0);
			}
			else if (lineComp.VerticalAlign == EnumVerticalAlign.FixedOffset)
			{
				lastLineBounds.Y += lineComp.UnscaledMarginTop;
			}
		}
	}

	private void adjustLineTextAlignment(List<int> currentLine)
	{
		if (currentLine.Count == 0)
		{
			return;
		}
		int lastIndex = currentLine[currentLine.Count - 1];
		RichTextComponent obj = Components[lastIndex] as RichTextComponent;
		double rightSpace = ((obj == null) ? null : obj.Lines[0]?.RightSpace).GetValueOrDefault();
		EnumTextOrientation orient = ((Components[lastIndex] as RichTextComponent)?.Font?.Orientation).GetValueOrDefault();
		foreach (int index in currentLine)
		{
			RichTextComponentBase comp = Components[index];
			if (orient == EnumTextOrientation.Center && comp is RichTextComponent rtcb)
			{
				rtcb.Lines[(lastIndex != index) ? (rtcb.Lines.Length - 1) : 0].RightSpace = rightSpace;
			}
		}
	}

	private void ConstrainTextFlowPath(List<TextFlowPath> flowPath, double posY, RichTextComponentBase comp)
	{
		Rectangled rect = comp.BoundsPerLine[0];
		EnumFloat @float = comp.Float;
		double x1 = ((@float == EnumFloat.Left) ? rect.Width : 0.0);
		double x2 = ((@float == EnumFloat.Right) ? (Bounds.InnerWidth - rect.Width) : Bounds.InnerWidth);
		double remainingHeight = rect.Height;
		for (int i = 0; i < flowPath.Count; i++)
		{
			TextFlowPath tfp = flowPath[i];
			if (tfp.Y2 <= posY)
			{
				continue;
			}
			double hereX1 = Math.Max(x1, tfp.X1);
			double hereX2 = Math.Min(x2, tfp.X2);
			if (tfp.Y2 > posY + rect.Height)
			{
				if (!(x1 <= tfp.X1) || !(x2 >= tfp.X2))
				{
					if (i == 0)
					{
						flowPath[i] = new TextFlowPath(hereX1, posY, hereX2, posY + rect.Height);
						flowPath.Insert(i + 1, new TextFlowPath(tfp.X1, posY + rect.Height, tfp.X2, tfp.Y2));
					}
					else
					{
						flowPath[i] = new TextFlowPath(tfp.X1, tfp.Y1, tfp.X2, posY);
						flowPath.Insert(i + 1, new TextFlowPath(tfp.X1, posY + rect.Height, tfp.X2, tfp.Y2));
						flowPath.Insert(i, new TextFlowPath(hereX1, posY, hereX2, posY + rect.Height));
					}
					remainingHeight = 0.0;
					break;
				}
			}
			else
			{
				flowPath[i].X1 = hereX1;
				flowPath[i].X2 = hereX2;
				remainingHeight -= tfp.Y2 - posY;
			}
		}
		if (remainingHeight > 0.0)
		{
			flowPath.Add(new TextFlowPath(x1, posY, x2, posY + remainingHeight));
		}
	}

	public virtual void ComposeFor(ElementBounds bounds, Context ctx, ImageSurface surface)
	{
		bounds.CalcWorldBounds();
		ctx.Save();
		Matrix j = ctx.Matrix;
		j.Translate(bounds.drawX, bounds.drawY);
		ctx.Matrix = j;
		for (int i = 0; i < Components.Length; i++)
		{
			Components[i].ComposeElements(ctx, surface);
			if (Debug)
			{
				ctx.LineWidth = 1.0;
				if (Components[i] is ClearFloatTextComponent)
				{
					ctx.SetSourceRGBA(0.0, 0.0, 1.0, 0.5);
				}
				else
				{
					ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
				}
				LineRectangled[] boundsPerLine = Components[i].BoundsPerLine;
				foreach (LineRectangled val in boundsPerLine)
				{
					ctx.Rectangle(val.X, val.Y, val.Width, val.Height);
					ctx.Stroke();
				}
			}
		}
		ctx.Restore();
	}

	/// <summary>
	/// Recomposes the element for lines.
	/// </summary>
	public void RecomposeText()
	{
		CalcHeightAndPositions();
		Bounds.CalcWorldBounds();
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Math.Min(MaxHeight, Bounds.OuterHeight));
		Context ctx = genContext(surface);
		ComposeFor(Bounds.CopyOnlySize(), ctx, surface);
		generateTexture(surface, ref richtTextTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		Render2DTexture(richtTextTexture.TextureId, (int)Bounds.renderX, (int)Bounds.renderY, richtTextTexture.Width, richtTextTexture.Height, zPos, RenderColor);
		MouseOverCursor = null;
		for (int i = 0; i < Components.Length; i++)
		{
			RichTextComponentBase comp = Components[i];
			comp.RenderColor = RenderColor;
			comp.RenderInteractiveElements(deltaTime, Bounds.renderX, Bounds.renderY, zPos);
			if (comp.UseMouseOverCursor(Bounds))
			{
				MouseOverCursor = comp.MouseOverCursor;
			}
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		MouseEvent relArgs = new MouseEvent((int)((double)args.X - Bounds.absX), (int)((double)args.Y - Bounds.absY), args.Button, 0);
		for (int i = 0; i < Components.Length; i++)
		{
			Components[i].OnMouseMove(relArgs);
			if (relArgs.Handled)
			{
				break;
			}
		}
		args.Handled = relArgs.Handled;
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		MouseEvent relArgs = new MouseEvent((int)((double)args.X - Bounds.absX), (int)((double)args.Y - Bounds.absY), args.Button, 0);
		for (int i = 0; i < Components.Length; i++)
		{
			Components[i].OnMouseDown(relArgs);
			if (relArgs.Handled)
			{
				break;
			}
		}
		args.Handled = relArgs.Handled;
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		MouseEvent relArgs = new MouseEvent((int)((double)args.X - Bounds.absX), (int)((double)args.Y - Bounds.absY), args.Button, 0);
		for (int i = 0; i < Components.Length; i++)
		{
			Components[i].OnMouseUp(relArgs);
			if (relArgs.Handled)
			{
				break;
			}
		}
		args.Handled |= relArgs.Handled;
		base.OnMouseUp(api, args);
	}

	public void SetNewText(string vtmlCode, CairoFont baseFont, Action<LinkTextComponent> didClickLink = null)
	{
		SetNewTextWithoutRecompose(vtmlCode, baseFont, didClickLink, recalcBounds: true);
		RecomposeText();
	}

	public void SetNewText(RichTextComponentBase[] comps)
	{
		Components = comps;
		RecomposeText();
	}

	[Obsolete("Use AppendText(RichTextComponentBase[] comps) instead")]
	public void AppendText(RichTextComponent[] comps)
	{
		Components = Components.Append(comps);
		RecomposeText();
	}

	public void AppendText(RichTextComponentBase[] comps)
	{
		Components = Components.Append(comps);
		RecomposeText();
	}

	public void SetNewTextWithoutRecompose(string vtmlCode, CairoFont baseFont, Action<LinkTextComponent> didClickLink = null, bool recalcBounds = false)
	{
		if (Components != null)
		{
			RichTextComponentBase[] components = Components;
			for (int i = 0; i < components.Length; i++)
			{
				components[i]?.Dispose();
			}
		}
		Components = VtmlUtil.Richtextify(api, vtmlCode, baseFont, didClickLink);
		if (recalcBounds)
		{
			CalcHeightAndPositions();
			Bounds.CalcWorldBounds();
		}
	}

	public void RecomposeInto(ImageSurface surface, Context ctx)
	{
		ComposeFor(Bounds.CopyOnlySize(), ctx, surface);
		generateTexture(surface, ref richtTextTexture);
	}

	public override void Dispose()
	{
		base.Dispose();
		richtTextTexture?.Dispose();
		RichTextComponentBase[] components = Components;
		for (int i = 0; i < components.Length; i++)
		{
			components[i].Dispose();
		}
	}
}
