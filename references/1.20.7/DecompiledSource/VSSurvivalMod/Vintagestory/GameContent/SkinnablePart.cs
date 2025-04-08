using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class SkinnablePart
{
	public bool Colbreak;

	public bool UseDropDown;

	public string Code;

	public EnumSkinnableType Type;

	public string[] DisableElements;

	public CompositeShape ShapeTemplate;

	public SkinnablePartVariant[] Variants;

	public Vec2i TextureRenderTo;

	public string TextureTarget;

	public AssetLocation TextureTemplate;

	public Dictionary<string, SkinnablePartVariant> VariantsByCode;
}
