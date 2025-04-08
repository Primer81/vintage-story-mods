using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// An expanded, atlas-friendly version of a CompositeTexture
/// </summary>
public class BakedCompositeTexture
{
	/// <summary>
	/// Unique identifier for this texture
	/// </summary>
	public int TextureSubId;

	/// <summary>
	/// The Base name and Overlay concatenated (if there was any defined)
	/// </summary>
	public AssetLocation BakedName;

	/// <summary>
	/// The base name and overlays as array
	/// </summary>
	public AssetLocation[] TextureFilenames;

	/// <summary>
	/// If non-null also contains BakedName
	/// </summary>
	public BakedCompositeTexture[] BakedVariants;

	/// <summary>
	/// If non-null also contains BakedName
	/// </summary>
	public BakedCompositeTexture[] BakedTiles;

	public int TilesWidth;
}
