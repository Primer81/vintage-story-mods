using System;
using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementHorizontalTabs : GuiElementTextBase
{
	private Action<int> handler;

	internal GuiTab[] tabs;

	private LoadedTexture baseTexture;

	private LoadedTexture[] hoverTextures;

	private LoadedTexture[] notifyTextures;

	private int[] tabWidths;

	private CairoFont selectedFont;

	public int activeElement;

	public double unscaledTabSpacing = 5.0;

	public double unscaledTabPadding = 4.0;

	public bool AlarmTabs;

	private float fontHeight;

	private CairoFont notifyFont;

	public bool[] TabHasAlarm { get; set; }

	public override bool Focusable => true;

	/// <summary>
	/// Creates a collection of horizontal tabs.
	/// </summary>
	/// <param name="capi">The client API</param>
	/// <param name="tabs">A collection of GUI tabs.</param>
	/// <param name="font">The font for the name of each tab.</param>
	/// <param name="selectedFont"></param>
	/// <param name="bounds">The bounds of each tab.</param>
	/// <param name="onTabClicked">The event fired whenever the tab is clicked.</param>
	public GuiElementHorizontalTabs(ICoreClientAPI capi, GuiTab[] tabs, CairoFont font, CairoFont selectedFont, ElementBounds bounds, Action<int> onTabClicked)
		: base(capi, "", font, bounds)
	{
		this.selectedFont = selectedFont;
		this.tabs = tabs;
		TabHasAlarm = new bool[tabs.Length];
		handler = onTabClicked;
		hoverTextures = new LoadedTexture[tabs.Length];
		for (int i = 0; i < tabs.Length; i++)
		{
			hoverTextures[i] = new LoadedTexture(capi);
		}
		tabWidths = new int[tabs.Length];
		baseTexture = new LoadedTexture(capi);
	}

	[Obsolete("Use TabHasAlarm[] property instead. Used by the chat window to mark a tab/chat as unread")]
	public void SetAlarmTab(int tabIndex)
	{
	}

	public void WithAlarmTabs(CairoFont notifyFont)
	{
		this.notifyFont = notifyFont;
		notifyTextures = new LoadedTexture[tabs.Length];
		for (int i = 0; i < tabs.Length; i++)
		{
			notifyTextures[i] = new LoadedTexture(api);
		}
		AlarmTabs = true;
		ComposeOverlays(isNotifyTabs: true);
	}

	public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth + 1, (int)Bounds.InnerHeight + 1);
		Context ctx = new Context(surface);
		Font.SetupContext(ctx);
		fontHeight = (float)Font.GetFontExtents().Height;
		double radius = GuiElement.scaled(1.0);
		double spacing = GuiElement.scaled(unscaledTabSpacing);
		double padding = GuiElement.scaled(unscaledTabPadding);
		double xpos = spacing;
		Font.Color[3] = 0.5;
		for (int i = 0; i < tabs.Length; i++)
		{
			tabWidths[i] = (int)(ctx.TextExtents(tabs[i].Name).Width + 2.0 * padding + 1.0);
			ctx.NewPath();
			ctx.MoveTo(xpos, Bounds.InnerHeight);
			ctx.LineTo(xpos, radius);
			ctx.Arc(xpos + radius, radius, radius, 3.1415927410125732, 4.71238899230957);
			ctx.Arc(xpos + (double)tabWidths[i] - radius, radius, radius, -1.5707963705062866, 0.0);
			ctx.LineTo(xpos + (double)tabWidths[i], Bounds.InnerHeight);
			ctx.ClosePath();
			double[] color = GuiStyle.DialogDefaultBgColor;
			ctx.SetSourceRGBA(color[0], color[1], color[2], color[3] * 0.75);
			ctx.FillPreserve();
			ShadePath(ctx);
			if (AlarmTabs)
			{
				notifyFont.SetupContext(ctx);
			}
			else
			{
				Font.SetupContext(ctx);
			}
			DrawTextLineAt(ctx, tabs[i].Name, xpos + padding, ((float)surface.Height - fontHeight) / 2f);
			xpos += (double)tabWidths[i] + spacing;
		}
		Font.Color[3] = 1.0;
		ComposeOverlays();
		generateTexture(surface, ref baseTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	private void ComposeOverlays(bool isNotifyTabs = false)
	{
		double radius = GuiElement.scaled(1.0);
		GuiElement.scaled(unscaledTabSpacing);
		double padding = GuiElement.scaled(unscaledTabPadding);
		for (int i = 0; i < tabs.Length; i++)
		{
			ImageSurface surface = new ImageSurface(Format.Argb32, tabWidths[i], (int)Bounds.InnerHeight + 1);
			Context ctx = genContext(surface);
			double degrees = Math.PI / 180.0;
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
			ctx.Paint();
			ctx.NewPath();
			ctx.MoveTo(0.0, Bounds.InnerHeight + 1.0);
			ctx.LineTo(0.0, radius);
			ctx.Arc(radius, radius, radius, 180.0 * degrees, 270.0 * degrees);
			ctx.Arc((double)tabWidths[i] - radius, radius, radius, -90.0 * degrees, 0.0 * degrees);
			ctx.LineTo(tabWidths[i], surface.Height);
			ctx.ClosePath();
			double[] color = GuiStyle.DialogDefaultBgColor;
			ctx.SetSourceRGBA(color[0], color[1], color[2], color[3] * 0.75);
			ctx.FillPreserve();
			ctx.SetSourceRGBA(color[0] * 1.6, color[1] * 1.6, color[2] * 1.6, 1.0);
			ctx.LineWidth = 3.5;
			ctx.StrokePreserve();
			surface.BlurPartial(5.2, 10);
			ctx.SetSourceRGBA(color[0], color[1], color[2], color[3] * 0.75);
			ctx.LineWidth = 1.0;
			ctx.StrokePreserve();
			ctx.NewPath();
			ctx.MoveTo(0.0, Bounds.InnerHeight);
			ctx.LineTo(0.0, radius);
			ctx.Arc(radius, radius, radius, 180.0 * degrees, 270.0 * degrees);
			ctx.Arc((double)tabWidths[i] - radius, radius, radius, -90.0 * degrees, 0.0 * degrees);
			ctx.LineTo(tabWidths[i], Bounds.InnerHeight);
			ShadePath(ctx);
			if (isNotifyTabs)
			{
				notifyFont.SetupContext(ctx);
			}
			else
			{
				selectedFont.SetupContext(ctx);
			}
			ctx.Operator = Operator.Clear;
			ctx.Rectangle(0.0, surface.Height - 1, surface.Width, 1.0);
			ctx.Fill();
			ctx.Operator = Operator.Over;
			DrawTextLineAt(ctx, tabs[i].Name, padding, ((float)surface.Height - fontHeight) / 2f);
			if (isNotifyTabs)
			{
				generateTexture(surface, ref notifyTextures[i]);
			}
			else
			{
				generateTexture(surface, ref hoverTextures[i]);
			}
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
		double xpos = spacing;
		for (int i = 0; i < tabs.Length; i++)
		{
			if (i == activeElement || ((double)mouseRelX > xpos && (double)mouseRelX < xpos + (double)tabWidths[i] && mouseRelY > 0 && (double)mouseRelY < Bounds.InnerHeight))
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTextures[i].TextureId, (int)(Bounds.renderX + xpos), (int)Bounds.renderY, tabWidths[i], (int)Bounds.InnerHeight + 1);
			}
			if (TabHasAlarm[i])
			{
				api.Render.Render2DTexturePremultipliedAlpha(notifyTextures[i].TextureId, (int)(Bounds.renderX + xpos), (int)Bounds.renderY, tabWidths[i], (int)Bounds.InnerHeight + 1);
			}
			xpos += (double)tabWidths[i] + spacing;
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus)
		{
			if (args.KeyCode == 48)
			{
				args.Handled = true;
				SetValue((activeElement + 1) % tabs.Length);
			}
			if (args.KeyCode == 47)
			{
				SetValue(GameMath.Mod(activeElement - 1, tabs.Length));
				args.Handled = true;
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		double spacing = GuiElement.scaled(unscaledTabSpacing);
		double xpos = spacing;
		int mouseRelX = api.Input.MouseX - (int)Bounds.absX;
		int mouseRelY = api.Input.MouseY - (int)Bounds.absY;
		for (int i = 0; i < tabs.Length; i++)
		{
			if ((double)mouseRelX > xpos && (double)mouseRelX < xpos + (double)tabWidths[i] && mouseRelY > 0 && (double)mouseRelY < Bounds.InnerHeight)
			{
				SetValue(i);
				break;
			}
			xpos += (double)tabWidths[i] + spacing;
		}
	}

	/// <summary>
	/// Sets the current tab to the given index.
	/// </summary>
	/// <param name="selectedIndex">The current index of the tab.</param>
	/// <param name="callhandler"></param>
	public void SetValue(int selectedIndex, bool callhandler = true)
	{
		if (callhandler)
		{
			handler(tabs[selectedIndex].DataInt);
			api.Gui.PlaySound("menubutton_wood");
		}
		activeElement = selectedIndex;
	}

	public override void Dispose()
	{
		base.Dispose();
		baseTexture?.Dispose();
		for (int i = 0; i < hoverTextures.Length; i++)
		{
			hoverTextures[i].Dispose();
			if (notifyTextures != null)
			{
				notifyTextures[i].Dispose();
			}
		}
	}
}
