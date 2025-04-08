using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class SkinnablePartVariant
{
	public string Code;

	public CompositeShape Shape;

	public AssetLocation Texture;

	public AssetLocation Sound;

	public int Color;

	public AppliedSkinnablePartVariant AppliedCopy(string partCode)
	{
		return new AppliedSkinnablePartVariant
		{
			Code = Code,
			Shape = Shape,
			Texture = Texture,
			Color = Color,
			PartCode = partCode
		};
	}
}
