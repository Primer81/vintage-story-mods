namespace Vintagestory.API.Client;

public enum EnumRenderStage
{
	/// <summary>
	/// Before any rendering has begun, use for setting up stuff during render
	/// </summary>
	Before,
	/// <summary>
	/// Opaque/Alpha tested rendering
	/// </summary>
	Opaque,
	/// <summary>
	/// Order independent transparency 
	/// </summary>
	OIT,
	/// <summary>
	/// To render the held item over water. If done in the opaque pass it would not render water behind it.
	/// </summary>
	AfterOIT,
	/// <summary>
	/// Shadow map
	/// </summary>
	ShadowFar,
	/// <summary>
	/// Shadow map done
	/// </summary>
	ShadowFarDone,
	/// <summary>
	/// Shadow map
	/// </summary>
	ShadowNear,
	/// <summary>
	/// Shadow map done
	/// </summary>
	ShadowNearDone,
	/// <summary>
	/// After all 3d geometry has rendered and post processing of the frame is complete
	/// </summary>
	AfterPostProcessing,
	/// <summary>
	/// Scene has been rendered onto the default frame buffer, but not yet rendered UIs
	/// </summary>
	AfterBlit,
	/// <summary>
	/// Ortho mode for rendering GUIs and everything 2D
	/// </summary>
	Ortho,
	/// <summary>
	/// The post processing passes are merged with all 3d geometry and the scene is color graded
	/// </summary>
	AfterFinalComposition,
	/// <summary>
	/// Scene is blitted onto the default frame buffer, buffers not yet swapped though so can still render to default FB
	/// </summary>
	Done
}
