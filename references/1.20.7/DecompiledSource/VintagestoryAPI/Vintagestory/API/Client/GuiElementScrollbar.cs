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

	/// <summary>
	/// Moving 1 pixel of the scrollbar moves the content by ScrollConversionFactor of pixels
	/// </summary>
	public float ScrollConversionFactor
	{
		get
		{
			if (Bounds.InnerHeight - (double)currentHandleHeight <= 0.0)
			{
				return 1f;
			}
			float scrollbarMovableArea = (float)(Bounds.InnerHeight - (double)currentHandleHeight);
			return (totalHeight - visibleHeight) / scrollbarMovableArea;
		}
	}

	/// <summary>
	/// The current Y position of the inner element
	/// </summary>
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

	/// <summary>
	/// Creates a new Scrollbar.
	/// </summary>
	/// <param name="capi">The client API.</param>
	/// <param name="onNewScrollbarValue">The event that fires when the scrollbar is changed.</param>
	/// <param name="bounds">The bounds of the scrollbar.</param>
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
		Bounds.CalcWorldBounds();
		int w = (int)Bounds.InnerWidth;
		int h = (int)currentHandleHeight;
		ImageSurface surface = new ImageSurface(Format.Argb32, w, h);
		Context ctx = genContext(surface);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, w, h, 1.0);
		ctx.SetSourceRGBA(GuiStyle.DialogHighlightColor);
		ctx.FillPreserve();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, 0.0, 0.0, w, h, inverse: false, 2, 1);
		generateTexture(surface, ref handleTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(handleTexture.TextureId, (int)(Bounds.renderX + Bounds.absPaddingX), (int)(Bounds.renderY + Bounds.absPaddingY + (double)currentHandlePosition), (int)Bounds.InnerWidth, (int)currentHandleHeight, 200f + zOffset);
	}

	/// <summary>
	/// Sets the height of the scrollbar.
	/// </summary>
	/// <param name="visibleHeight">The visible height of the scrollbar</param>
	/// <param name="totalHeight">The total height of the scrollbar.</param>
	public void SetHeights(float visibleHeight, float totalHeight)
	{
		this.visibleHeight = visibleHeight;
		SetNewTotalHeight(totalHeight);
	}

	/// <summary>
	/// Sets the total height, recalculating things for the new height.
	/// </summary>
	/// <param name="totalHeight">The total height of the scrollbar.</param>
	public void SetNewTotalHeight(float totalHeight)
	{
		this.totalHeight = totalHeight;
		float heightDiffFactor = GameMath.Clamp(visibleHeight / totalHeight, 0f, 1f);
		currentHandleHeight = Math.Max(10f, (float)((double)heightDiffFactor * Bounds.InnerHeight));
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
			float y = currentHandlePosition - (float)GuiElement.scaled(102.0) * args.deltaPrecise / ScrollConversionFactor;
			double scrollbarMoveableHeight = Bounds.InnerHeight - (double)currentHandleHeight;
			currentHandlePosition = (float)GameMath.Clamp(y, 0.0, scrollbarMoveableHeight);
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
			float movey = ((args.KeyCode == 46) ? (-0.5f) : 0.5f);
			float y = currentHandlePosition - (float)GuiElement.scaled(102.0) * movey / ScrollConversionFactor;
			double scrollbarMoveableHeight = Bounds.InnerHeight - (double)currentHandleHeight;
			currentHandlePosition = (float)GameMath.Clamp(y, 0.0, scrollbarMoveableHeight);
			TriggerChanged();
		}
	}

	private void UpdateHandlePositionAbs(float y)
	{
		double scrollbarMoveableHeight = Bounds.InnerHeight - (double)currentHandleHeight;
		currentHandlePosition = (float)GameMath.Clamp(y, 0.0, scrollbarMoveableHeight);
		TriggerChanged();
	}

	/// <summary>
	/// Triggers the change to the new value of the scrollbar.
	/// </summary>
	public void TriggerChanged()
	{
		onNewScrollbarValue(CurrentYPosition);
	}

	/// <summary>
	/// Puts the scrollblock to the very bottom of the scrollbar.
	/// </summary>
	public void ScrollToBottom()
	{
		float currentPositionRelative = 1f;
		if (totalHeight < visibleHeight)
		{
			currentHandlePosition = 0f;
			currentPositionRelative = 0f;
		}
		else
		{
			currentHandlePosition = (float)(Bounds.InnerHeight - (double)currentHandleHeight);
		}
		float currentPositionAbs = currentPositionRelative * (totalHeight - visibleHeight);
		onNewScrollbarValue(currentPositionAbs);
	}

	public void EnsureVisible(double posX, double posY)
	{
		double startY = CurrentYPosition;
		double endY = (double)CurrentYPosition + Bounds.InnerHeight;
		if (posY < startY)
		{
			float diff = (float)(startY - posY) / ScrollConversionFactor;
			currentHandlePosition = Math.Max(0f, currentHandlePosition - diff);
			TriggerChanged();
		}
		else if (posY > endY)
		{
			float diff2 = (float)(posY - endY) / ScrollConversionFactor;
			currentHandlePosition = (float)Math.Min(Bounds.InnerHeight - (double)currentHandleHeight, currentHandlePosition + diff2);
			TriggerChanged();
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		handleTexture.Dispose();
	}
}
