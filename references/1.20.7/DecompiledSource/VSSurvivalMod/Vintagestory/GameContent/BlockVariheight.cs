using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockVariheight : Block
{
	private int height;

	public override void OnLoaded(ICoreAPI api)
	{
		int.TryParse(Code.EndVariant(), out height);
		base.OnLoaded(api);
	}

	public override bool ShouldMergeFace(int tileSide, Block nBlock, int intraChunkIndex3d)
	{
		if (tileSide == 4)
		{
			return false;
		}
		if (nBlock.SideOpaque[TileSideEnum.GetOpposite(tileSide)])
		{
			return true;
		}
		if (tileSide == 5)
		{
			return false;
		}
		if (nBlock is BlockVariheight bvh)
		{
			return bvh.height >= height;
		}
		return false;
	}
}
