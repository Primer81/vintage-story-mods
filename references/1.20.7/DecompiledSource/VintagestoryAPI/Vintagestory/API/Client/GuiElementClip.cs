using Cairo;

namespace Vintagestory.API.Client;

internal class GuiElementClip : GuiElement
{
	private bool clip;

	/// <summary>
	/// Adds a clipped area to the GUI.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="clip">Do we clip?</param>
	/// <param name="bounds">The bounds of the element.</param>
	public GuiElementClip(ICoreClientAPI capi, bool clip, ElementBounds bounds)
		: base(capi, bounds)
	{
		this.clip = clip;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (clip)
		{
			api.Render.PushScissor(Bounds);
		}
		else
		{
			api.Render.PopScissor();
		}
	}

	public override int OutlineColor()
	{
		return -65536;
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent mouse)
	{
	}
}
