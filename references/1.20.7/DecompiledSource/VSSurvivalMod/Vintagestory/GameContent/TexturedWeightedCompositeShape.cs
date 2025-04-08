using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class TexturedWeightedCompositeShape : CompositeShape
{
	public float Weight = 1f;

	public Dictionary<string, AssetLocation> Textures;

	public Dictionary<string, AssetLocation> OverrideTextures;

	public string[] DisableElements { get; set; }
}
