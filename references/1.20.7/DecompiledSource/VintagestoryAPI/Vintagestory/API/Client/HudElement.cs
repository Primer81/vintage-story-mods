namespace Vintagestory.API.Client;

/// <summary>
/// Base class for Hud Elements.
/// </summary>
public abstract class HudElement : GuiDialog
{
	public override EnumDialogType DialogType => EnumDialogType.HUD;

	public override string ToggleKeyCombinationCode => null;

	public override bool PrefersUngrabbedMouse => false;

	/// <summary>
	/// Creates a new Hud Element.
	/// </summary>
	/// <param name="capi">The Client API</param>
	public HudElement(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public override void OnRenderGUI(float deltaTime)
	{
		capi.Render.GlPushMatrix();
		capi.Render.GlTranslate(0f, 0f, -150f);
		base.OnRenderGUI(deltaTime);
		capi.Render.GlPopMatrix();
	}
}
