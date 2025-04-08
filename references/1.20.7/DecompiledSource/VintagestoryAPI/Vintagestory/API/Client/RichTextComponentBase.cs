using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class RichTextComponentBase
{
	protected ICoreClientAPI api;

	public string MouseOverCursor { get; protected set; }

	/// <summary>
	/// The width/height boundaries of this text component per line
	/// </summary>
	public virtual LineRectangled[] BoundsPerLine { get; protected set; }

	/// <summary>
	/// This will be the Y-Advance into the next line. Unscaled value. Also used as the offset with EnumVerticalAlign.FixedOffset
	/// </summary>
	public virtual double UnscaledMarginTop { get; set; }

	/// <summary>
	/// Padding that is used when a richtextcomponent came before and needs some left spacing to it. Unscaled value
	/// </summary>
	public virtual double PaddingRight { get; set; }

	/// <summary>
	/// Unscaled value
	/// </summary>
	public virtual double PaddingLeft { get; set; }

	/// <summary>
	/// When left or right, then this element can span over multiple text lines
	/// </summary>
	public virtual EnumFloat Float { get; set; } = EnumFloat.Inline;


	public virtual Vec4f RenderColor { get; set; }

	public virtual EnumVerticalAlign VerticalAlign { get; set; } = EnumVerticalAlign.Bottom;


	public RichTextComponentBase(ICoreClientAPI api)
	{
		this.api = api;
	}

	/// <summary>
	/// Composes the element.
	/// </summary>
	/// <param name="ctx"></param>
	/// <param name="surface"></param>
	public virtual void ComposeElements(Context ctx, ImageSurface surface)
	{
	}

	/// <summary>
	/// Renders the text component.
	/// </summary>
	/// <param name="deltaTime"></param>
	/// <param name="renderX"></param>
	/// <param name="renderY"></param>
	/// <param name="renderZ"></param>
	public virtual void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
	}

	/// <summary>
	/// Initializes the size and stuff. Return true if you had to enter the next line
	/// </summary>
	/// <param name="flowPath"></param>
	/// <param name="currentLineHeight"></param>
	/// <param name="offsetX"></param>
	/// <param name="lineY"></param>
	/// <param name="nextOffsetX"></param>
	/// <returns>A</returns>
	public virtual EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		nextOffsetX = offsetX;
		return EnumCalcBoundsResult.Continue;
	}

	protected virtual TextFlowPath GetCurrentFlowPathSection(TextFlowPath[] flowPath, double posY)
	{
		for (int i = 0; i < flowPath.Length; i++)
		{
			if (flowPath[i].Y1 <= posY && flowPath[i].Y2 >= posY)
			{
				return flowPath[i];
			}
		}
		return null;
	}

	public virtual void OnMouseMove(MouseEvent args)
	{
	}

	public virtual void OnMouseDown(MouseEvent args)
	{
	}

	public virtual void OnMouseUp(MouseEvent args)
	{
	}

	public virtual void Dispose()
	{
	}

	public virtual bool UseMouseOverCursor(ElementBounds richtextBounds)
	{
		int relx = (int)((double)api.Input.MouseX - richtextBounds.absX);
		int rely = (int)((double)api.Input.MouseY - richtextBounds.absY);
		for (int i = 0; i < BoundsPerLine.Length; i++)
		{
			if (BoundsPerLine[i].PointInside(relx, rely))
			{
				return true;
			}
		}
		return false;
	}
}
