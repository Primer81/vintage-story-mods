using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

internal class BlockEntityPlatePile : BlockEntityItemPile, ITexPositionSource
{
	private Block tmpBlock;

	private AssetLocation tmpMetal;

	private ITexPositionSource tmpTextureSource;

	internal AssetLocation soundLocation = new AssetLocation("sounds/block/plate");

	private Dictionary<AssetLocation, MeshData[]> meshesByType => ObjectCacheUtil.GetOrCreate(Api, "platepile-meshes", () => GenMeshes());

	public override AssetLocation SoundLocation => soundLocation;

	public override string BlockCode => "platepile";

	public override int MaxStackSize => 16;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[tmpMetal.Path];

	private Dictionary<AssetLocation, MeshData[]> GenMeshes()
	{
		Dictionary<AssetLocation, MeshData[]> meshesByType = new Dictionary<AssetLocation, MeshData[]>();
		tmpBlock = Api.World.BlockAccessor.GetBlock(Pos);
		tmpTextureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(tmpBlock);
		Shape shape = Shape.TryGet(Api, "shapes/block/metal/platepile.json");
		MetalProperty metals = Api.Assets.TryGet("worldproperties/block/metal.json").ToObject<MetalProperty>();
		for (int i = 0; i < metals.Variants.Length; i++)
		{
			ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
			MeshData[] meshes = new MeshData[17];
			tmpMetal = metals.Variants[i].Code;
			for (int j = 0; j <= 16; j++)
			{
				mesher.TesselateShape("platePile", shape, out meshes[j], this, null, 0, 0, 0, j);
			}
			meshesByType[tmpMetal] = meshes;
		}
		tmpTextureSource = null;
		tmpMetal = null;
		tmpBlock = null;
		return meshesByType;
	}

	public BlockEntityPlatePile()
	{
		inventory = new InventoryGeneric(1, BlockCode, null, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.ResolveBlocksOrItems();
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		lock (inventoryLock)
		{
			if (inventory[0].Itemstack == null)
			{
				return true;
			}
			string metal = inventory[0].Itemstack.Collectible.LastCodePart();
			int index = Math.Min(16, inventory[0].StackSize);
			meshdata.AddMeshData(meshesByType[new AssetLocation(metal)][index]);
		}
		return true;
	}
}
