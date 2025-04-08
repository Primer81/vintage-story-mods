using System;
using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementVerticalTabs : GuiElementTextBase
{
	protected Action<int, GuiTab> handler;

	protected GuiTab[] tabs;

	protected LoadedTexture baseTexture;

	protected LoadedTexture[] hoverTextures;

	protected int[] tabWidths;

	protected CairoFont selectedFont;

	protected double unscaledTabSpacing = 5.0;

	protected double unscaledTabHeight = 25.0;

	protected double unscaledTabPadding = 3.0;

	protected double tabHeight;

	protected double textOffsetY;

	public int ActiveElement;

	public bool Right;

	/// <summary>
	/// If true, more than one tab can be active
	/// </summary>
	public bool ToggleTabs;

	public override bool Focusable => true;

	/// <summary>
	/// Creates a new vertical tab group.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="tabs">The collection of individual tabs.</param>
	/// <param name="font">The font for the group of them all.</param>
	/// <param name="selectedFont"></param>
	/// <param name="bounds">The bounds of the tabs.</param>
	/// <param name="onTabClicked">The event fired when the tab is clicked.</param>
	public GuiElementVerticalTabs(ICoreClientAPI capi, GuiTab[] tabs, CairoFont font, CairoFont selectedFont, ElementBounds bounds, Action<int, GuiTab> onTabClicked)
		: base(capi, "", font, bounds)
	{
		this.selectedFont = selectedFont;
		this.tabs = tabs;
		handler = onTabClicked;
		hoverTextures = new LoadedTexture[tabs.Length];
		for (int i = 0; i < tabs.Length; i++)
		{
			hoverTextures[i] = new LoadedTexture(capi);
		}
		baseTexture = new LoadedTexture(capi);
		tabWidths = new int[tabs.Length];
		if (tabs.Length != 0)
		{
			tabs[0].Active = true;
		}
	}

	public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Bounds.CalcWorldBounds();
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth + 1, (int)Bounds.InnerHeight + 1);
		Context ctx = new Context(surface);
		double radius = GuiElement.scaled(1.0);
		double spacing = GuiElement.scaled(unscaledTabSpacing);
		double padding = GuiElement.scaled(unscaledTabPadding);
		tabHeight = GuiElement.scaled(unscaledTabHeight);
		double ypos = 0.0;
		Font.Color[3] = 0.85;
		Font.SetupContext(ctx);
		textOffsetY = (tabHeight + 1.0 - Font.GetFontExtents().Height) / 2.0;
		double maxWidth = 0.0;
		for (int j = 0; j < tabs.Length; j++)
		{
			maxWidth = Math.Max((int)(ctx.TextExtents(tabs[j].Name).Width + 1.0 + 2.0 * padding), maxWidth);
		}
		for (int i = 0; i < tabs.Length; i++)
		{
			tabWidths[i] = (int)maxWidth + 1;
			double xpos;
			if (Right)
			{
				xpos = 1.0;
				ypos += tabs[i].PaddingTop;
				ctx.NewPath();
				ctx.MoveTo(xpos, ypos + tabHeight);
				ctx.LineTo(xpos, ypos);
				ctx.LineTo(xpos + (double)tabWidths[i] + radius, ypos);
				ctx.ArcNegative(xpos + (double)tabWidths[i], ypos + radius, radius, 4.71238899230957, 3.1415927410125732);
				ctx.ArcNegative(xpos + (double)tabWidths[i], ypos - radius + tabHeight, radius, 3.1415927410125732, 1.5707963705062866);
			}
			else
			{
				xpos = (int)Bounds.InnerWidth + 1;
				ypos += tabs[i].PaddingTop;
				ctx.NewPath();
				ctx.MoveTo(xpos, ypos + tabHeight);
				ctx.LineTo(xpos, ypos);
				ctx.LineTo(xpos - (double)tabWidths[i] + radius, ypos);
				ctx.ArcNegative(xpos - (double)tabWidths[i], ypos + radius, radius, 4.71238899230957, 3.1415927410125732);
				ctx.ArcNegative(xpos - (double)tabWidths[i], ypos - radius + tabHeight, radius, 3.1415927410125732, 1.5707963705062866);
			}
			ctx.ClosePath();
			double[] color = GuiStyle.DialogDefaultBgColor;
			ctx.SetSourceRGBA(color[0], color[1], color[2], color[3]);
			ctx.FillPreserve();
			ShadePath(ctx);
			Font.SetupContext(ctx);
			DrawTextLineAt(ctx, tabs[i].Name, xpos - (double)((!Right) ? tabWidths[i] : 0) + padding, ypos + textOffsetY);
			ypos += tabHeight + spacing;
		}
		Font.Color[3] = 1.0;
		ComposeOverlays();
		generateTexture(surface, ref baseTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	private void ComposeOverlays()
	{
		double radius = GuiElement.scaled(1.0);
		double padding = GuiElement.scaled(unscaledTabPadding);
		for (int i = 0; i < tabs.Length; i++)
		{
			ImageSurface surface = new ImageSurface(Format.Argb32, tabWidths[i] + 1, (int)tabHeight + 1);
			Context ctx = genContext(surface);
			double width = tabWidths[i] + 1;
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
			ctx.Paint();
			ctx.NewPath();
			ctx.MoveTo(width, tabHeight + 1.0);
			ctx.LineTo(width, 0.0);
			ctx.LineTo(radius, 0.0);
			ctx.ArcNegative(0.0, radius, radius, 4.71238899230957, 3.1415927410125732);
			ctx.ArcNegative(0.0, tabHeight - radius, radius, 3.1415927410125732, 1.5707963705062866);
			ctx.ClosePath();
			double[] color = GuiStyle.DialogDefaultBgColor;
			ctx.SetSourceRGBA(color[0], color[1], color[2], color[3]);
			ctx.Fill();
			ctx.NewPath();
			if (Right)
			{
				ctx.LineTo(1.0, 1.0);
				ctx.LineTo(width, 1.0);
				ctx.LineTo(width, 1.0 + tabHeight - 1.0);
				ctx.LineTo(1.0, tabHeight - 1.0);
			}
			else
			{
				ctx.LineTo(1.0 + width, 1.0);
				ctx.LineTo(1.0, 1.0);
				ctx.LineTo(1.0, tabHeight - 1.0);
				ctx.LineTo(1.0 + width, 1.0 + tabHeight - 1.0);
			}
			float strokeWidth = 2f;
			ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.6, GuiStyle.DialogStrongBgColor[1] * 1.6, GuiStyle.DialogStrongBgColor[2] * 1.6, 1.0);
			ctx.LineWidth = (double)strokeWidth * 1.75;
			ctx.StrokePreserve();
			surface.BlurPartial(8.0, 16);
			ctx.SetSourceRGBA(new double[4]
			{
				0.17647058823529413,
				7.0 / 51.0,
				11.0 / 85.0,
				1.0
			});
			ctx.LineWidth = strokeWidth;
			ctx.Stroke();
			selectedFont.SetupContext(ctx);
			DrawTextLineAt(ctx, tabs[i].Name, padding + 2.0, textOffsetY);
			generateTexture(surface, ref hoverTextures[i]);
			ctx.Dispose();
			surface.Dispose();
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexture(baseTexture.TextureId, (int)Bounds.renderX, (int)Bounds.renderY, (int)Bounds.InnerWidth + 1, (int)Bounds.InnerHeight + 1);
		double spacing = GuiElement.scaled(unscaledTabSpacing);
		int mouseRelX = api.Input.MouseX - (int)Bounds.absX;
		int mouseRelY = api.Input.MouseY - (int)Bounds.absY;
		double xposend = (int)Bounds.InnerWidth;
		double ypos = 0.0;
		for (int i = 0; i < tabs.Length; i++)
		{
			GuiTab tab = tabs[i];
			ypos += tab.PaddingTop;
			if (Right)
			{
				if (tab.Active || (mouseRelX >= 0 && (double)mouseRelX < xposend && (double)mouseRelY > ypos && (double)mouseRelY < ypos + tabHeight))
				{
					api.Render.Render2DTexturePremultipliedAlpha(hoverTextures[i].TextureId, (int)Bounds.renderX, (int)(Bounds.renderY + ypos), tabWidths[i] + 1, (int)tabHeight + 1);
				}
			}
			else if (tab.Active || ((double)mouseRelX > xposend - (double)tabWidths[i] - 3.0 && (double)mouseRelX < xposend && (double)mouseRelY > ypos && (double)mouseRelY < ypos + tabHeight))
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTextures[i].TextureId, (int)(Bounds.renderX + xposend - (double)tabWidths[i] - 1.0), (int)(Bounds.renderY + ypos), tabWidths[i] + 1, (int)tabHeight + 1);
			}
			ypos += tabHeight + spacing;
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus)
		{
			if (args.KeyCode == 46)
			{
				args.Handled = true;
				SetValue((ActiveElement + 1) % tabs.Length);
			}
			if (args.KeyCode == 45)
			{
				SetValue(GameMath.Mod(ActiveElement - 1, tabs.Length));
				args.Handled = true;
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		double spacing = GuiElement.scaled(unscaledTabSpacing);
		double xposend = Bounds.InnerWidth + 1.0;
		double ypos = 0.0;
		int mouseRelX = api.Input.MouseX - (int)Bounds.absX;
		int mouseRelY = api.Input.MouseY - (int)Bounds.absY;
		for (int i = 0; i < tabs.Length; i++)
		{
			ypos += tabs[i].PaddingTop;
			bool num = (double)mouseRelX > xposend - (double)tabWidths[i] - 3.0 && (double)mouseRelX < xposend;
			bool iny = (double)mouseRelY > ypos && (double)mouseRelY < ypos + tabHeight + spacing;
			if (num && iny)
			{
				SetValue(i);
				args.Handled = true;
				break;
			}
			ypos += tabHeight + spacing;
		}
	}

	/// <summary>
	/// Switches to a different tab.
	/// </summary>
	/// <param name="index">The tab to switch to.</param>
	public void SetValue(int index)
	{
		api.Gui.PlaySound("menubutton_wood");
		if (!ToggleTabs)
		{
			GuiTab[] array = tabs;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Active = false;
			}
		}
		tabs[index].Active = !tabs[index].Active;
		handler(index, tabs[index]);
		ActiveElement = index;
	}

	/// <summary>
	/// Switches to a different tab.
	/// </summary>
	/// <param name="index">The tab to switch to.</param>
	/// <param name="triggerHandler">Whether or not the handler triggers.</param>
	public void SetValue(int index, bool triggerHandler)
	{
		if (!ToggleTabs)
		{
			GuiTab[] array = tabs;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Active = false;
			}
		}
		tabs[index].Active = !tabs[index].Active;
		if (triggerHandler)
		{
			handler(index, tabs[index]);
			api.Gui.PlaySound("menubutton_wood");
		}
		ActiveElement = index;
	}

	public override void Dispose()
	{
		base.Dispose();
		for (int i = 0; i < hoverTextures.Length; i++)
		{
			hoverTextures[i].Dispose();
		}
		baseTexture.Dispose();
	}
}
