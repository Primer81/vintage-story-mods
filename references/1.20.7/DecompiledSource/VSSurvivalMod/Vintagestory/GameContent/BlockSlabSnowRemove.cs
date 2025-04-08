using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSlabSnowRemove : Block, ITexPositionSource
{
	private MeshData groundSnowLessMesh;

	private MeshData groundSnowedMesh;

	private bool testGroundSnowRemoval;

	private bool testGroundSnowAdd;

	private BlockFacing rot;

	private ICoreClientAPI capi;

	private AssetLocation snowLoc = new AssetLocation("block/liquid/snow/normal1");

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => capi.BlockTextureAtlas[snowLoc] ?? capi.BlockTextureAtlas.UnknownTexturePosition;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
		rot = BlockFacing.FromCode(Variant["rot"]);
		testGroundSnowRemoval = Variant["cover"] == "snow" && (Variant["rot"] == "north" || Variant["rot"] == "east" || Variant["rot"] == "south" || Variant["rot"] == "west");
		testGroundSnowAdd = Variant["cover"] == "free" && (Variant["rot"] == "north" || Variant["rot"] == "east" || Variant["rot"] == "south" || Variant["rot"] == "west");
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (testGroundSnowRemoval && !SolidBlockBelow(chunkExtBlocks, extIndex3d, pos))
		{
			if (groundSnowLessMesh == null)
			{
				groundSnowLessMesh = sourceMesh.Clone();
				groundSnowLessMesh.RemoveVertices(24);
				groundSnowLessMesh.XyzFacesCount -= 6;
			}
			sourceMesh = groundSnowLessMesh;
		}
		if (!testGroundSnowAdd || chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[rot.Opposite.Index]].BlockMaterial != EnumBlockMaterial.Snow || !SolidBlockBelow(chunkExtBlocks, extIndex3d, pos))
		{
			return;
		}
		if (groundSnowedMesh == null)
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(api, "shapes/block/basic/slab/snow-" + Variant["rot"] + ".json");
			(api as ICoreClientAPI).Tesselator.TesselateShape("slab snow cover", shape, out groundSnowedMesh, this, null, 0, 0, 0);
			for (int i = 0; i < groundSnowedMesh.RenderPassCount; i++)
			{
				groundSnowedMesh.RenderPassesAndExtraBits[i] = 0;
			}
			groundSnowedMesh.AddMeshData(sourceMesh);
		}
		sourceMesh = groundSnowedMesh;
	}

	private bool SolidBlockBelow(Block[] chunkExtBlocks, int extIndex3d, BlockPos pos)
	{
		if (chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[5]].SideSolid[BlockFacing.UP.Index])
		{
			return true;
		}
		return api.World.BlockAccessor.GetBlock(pos.DownCopy(), 2).SideSolid[BlockFacing.UP.Index];
	}

	public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		ClimateCondition conds = capi.World.Player.Entity.selfClimateCond;
		if (FirstCodePart() == "glassslab" && conds != null && conds.Rainfall > 0.1f && conds.Temperature > 3f && (world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y || world.BlockAccessor.GetDistanceToRainFall(pos, 3) <= 2))
		{
			return conds.Rainfall;
		}
		return 0f;
	}
}
