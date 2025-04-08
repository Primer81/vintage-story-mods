using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityPeatPile : BlockEntityItemPile, ITexPositionSource
{
	private Block tmpBlock;

	private ITexPositionSource tmpTextureSource;

	internal AssetLocation soundLocation = new AssetLocation("sounds/block/dirt");

	private MeshData[] meshes => ObjectCacheUtil.GetOrCreate(Api, "peatpile-meshes", () => GenMeshes());

	public override AssetLocation SoundLocation => soundLocation;

	public override string BlockCode => "peatpile";

	public override int MaxStackSize => 32;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[textureCode];

	private MeshData[] GenMeshes()
	{
		tmpBlock = Api.World.BlockAccessor.GetBlock(Pos);
		tmpTextureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(tmpBlock);
		Shape shape = Shape.TryGet(Api, "shapes/block/peatpile.json");
		ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
		MeshData[] meshes = new MeshData[33];
		for (int i = 0; i <= 32; i++)
		{
			mesher.TesselateShape("peatPile", shape, out meshes[i], this, null, 0, 0, 0, i);
		}
		tmpTextureSource = null;
		tmpBlock = null;
		return meshes;
	}

	public override bool TryPutItem(IPlayer player)
	{
		return base.TryPutItem(player);
	}

	public BlockEntityPeatPile()
	{
		inventory = new InventoryGeneric(1, BlockCode, null, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.ResolveBlocksOrItems();
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		lock (inventoryLock)
		{
			if (inventory[0].Itemstack == null)
			{
				return true;
			}
			meshdata.AddMeshData(meshes[inventory[0].StackSize]);
		}
		return true;
	}
}
