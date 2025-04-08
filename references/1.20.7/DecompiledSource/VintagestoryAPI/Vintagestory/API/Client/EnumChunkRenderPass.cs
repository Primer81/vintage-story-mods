namespace Vintagestory.API.Client;

/// <summary>
/// The various render passes available for rendering blocks
/// </summary>
[DocumentAsJson]
public enum EnumChunkRenderPass
{
	/// <summary>
	/// Backfaced culled, no alpha testing, alpha discard
	/// </summary>
	Opaque,
	/// <summary>
	/// Backfaced not culled, no alpha blended but alpha discard
	/// </summary>
	OpaqueNoCull,
	/// <summary>
	/// Backfaced not culled, alpha blended and alpha discard
	/// </summary>
	BlendNoCull,
	/// <summary>
	/// Uses a special rendering system called Weighted Blended Order Independent Transparency for half transparent blocks
	/// </summary>
	Transparent,
	/// <summary>
	/// Used for animated liquids
	/// </summary>
	Liquid,
	/// <summary>
	/// Special render pass for top soil only in order to have climated tinted grass half transparently overlaid over an opaque block
	/// </summary>
	TopSoil,
	/// <summary>
	/// Special render pass for meta blocks
	/// </summary>
	Meta
}
