namespace Vintagestory.API.MathTools;

/// <summary>
/// Specifies types of per-pixel color blending.
/// </summary>
[DocumentAsJson]
public enum EnumColorBlendMode
{
	Normal,
	Darken,
	Lighten,
	Multiply,
	Screen,
	ColorDodge,
	ColorBurn,
	Overlay,
	OverlayCutout
}
