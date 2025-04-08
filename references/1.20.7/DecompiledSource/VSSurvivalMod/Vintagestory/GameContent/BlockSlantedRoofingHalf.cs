using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSlantedRoofingHalf : Block
{
	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		string[] array = Code.Path.Split('-');
		BlockFacing facing = BlockFacing.FromCode(array[^2]);
		switch (array[0])
		{
		case "slantedroofinghalfleft":
			if (facing.Axis != axis)
			{
				return new AssetLocation(Code.Path.Replace("left", "right"));
			}
			return new AssetLocation(CodeWithVariant("horizontalorientation", facing.Opposite.Code).Path.Replace("left", "right"));
		case "slantedroofinghalfright":
			if (facing.Axis != axis)
			{
				return new AssetLocation(Code.Path.Replace("right", "left"));
			}
			return new AssetLocation(CodeWithVariant("horizontalorientation", facing.Opposite.Code).Path.Replace("right", "left"));
		case "slantedroofingcornerinner":
		case "slantedroofingcornerouter":
			if (facing.Axis != axis)
			{
				return CodeWithVariant("horizontalorientation", BlockFacing.HORIZONTALS[(facing.Index + 3) % 4].Code);
			}
			return CodeWithVariant("horizontalorientation", BlockFacing.HORIZONTALS[(facing.Index + 1) % 4].Code);
		default:
			if (facing.Axis != axis)
			{
				return CodeWithVariant("horizontalorientation", facing.Opposite.Code);
			}
			return Code;
		}
	}
}
