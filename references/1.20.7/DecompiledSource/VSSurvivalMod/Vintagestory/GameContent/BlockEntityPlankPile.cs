using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

internal class BlockEntityPlankPile : BlockEntityItemPile, ITexPositionSource
{
	private Block tmpBlock;

	private AssetLocation tmpWood;

	private ITexPositionSource tmpTextureSource;

	internal AssetLocation soundLocation = new AssetLocation("sounds/block/planks");

	private Dictionary<AssetLocation, MeshData[]> meshesByType => ObjectCacheUtil.GetOrCreate(Api, "plankpile-meshes", () => GenMeshes());

	public override AssetLocation SoundLocation => soundLocation;

	public override string BlockCode => "plankpile";

	public override int MaxStackSize => 48;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[tmpWood.Path];

	private Dictionary<AssetLocation, MeshData[]> GenMeshes()
	{
		Dictionary<AssetLocation, MeshData[]> meshesByType = new Dictionary<AssetLocation, MeshData[]>();
		tmpBlock = Api.World.BlockAccessor.GetBlock(Pos);
		tmpTextureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(tmpBlock);
		Shape shape = Shape.TryGet(Api, "shapes/block/wood/plankpile.json");
		MetalProperty woodtpyes = Api.Assets.TryGet("worldproperties/block/wood.json").ToObject<MetalProperty>();
		woodtpyes.Variants = woodtpyes.Variants.Append(new MetalPropertyVariant
		{
			Code = new AssetLocation("aged")
		});
		for (int i = 0; i < woodtpyes.Variants.Length; i++)
		{
			ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
			MeshData[] meshes = new MeshData[49];
			tmpWood = woodtpyes.Variants[i].Code;
			for (int j = 0; j <= 48; j++)
			{
				mesher.TesselateShape("PlankPile", shape, out meshes[j], this, null, 0, 0, 0, j);
			}
			meshesByType[tmpWood] = meshes;
		}
		tmpTextureSource = null;
		tmpWood = null;
		tmpBlock = null;
		return meshesByType;
	}

	public BlockEntityPlankPile()
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
			string woodtype = inventory[0].Itemstack.Collectible.LastCodePart();
			int index = Math.Min(48, inventory[0].StackSize);
			meshdata.AddMeshData(meshesByType[new AssetLocation(woodtype)][index]);
		}
		return true;
	}
}
