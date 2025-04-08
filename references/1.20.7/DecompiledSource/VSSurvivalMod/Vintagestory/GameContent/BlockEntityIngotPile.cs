using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityIngotPile : BlockEntityItemPile, ITexPositionSource
{
	private Block tmpBlock;

	private string tmpMetalCode;

	private ITexPositionSource tmpTextureSource;

	private ICoreClientAPI capi;

	internal AssetLocation soundLocation = new AssetLocation("sounds/block/ingot");

	private Dictionary<string, MeshData[]> meshesByType
	{
		get
		{
			object value = null;
			Api.ObjectCache.TryGetValue("ingotpile-meshes", out value);
			return (Dictionary<string, MeshData[]>)value;
		}
		set
		{
			Api.ObjectCache["ingotpile-meshes"] = value;
		}
	}

	public override AssetLocation SoundLocation => soundLocation;

	public override string BlockCode => "ingotpile";

	public override int MaxStackSize => 64;

	public TextureAtlasPosition this[string textureCode] => tmpTextureSource[tmpMetalCode];

	public string MetalType => inventory?[0]?.Itemstack?.Collectible?.LastCodePart();

	internal void EnsureMeshExists()
	{
		if (meshesByType == null)
		{
			meshesByType = new Dictionary<string, MeshData[]>();
		}
		if (MetalType == null || meshesByType.ContainsKey(MetalType) || Api.Side != EnumAppSide.Client)
		{
			return;
		}
		tmpBlock = Api.World.BlockAccessor.GetBlock(Pos);
		tmpTextureSource = ((ICoreClientAPI)Api).Tesselator.GetTextureSource(tmpBlock);
		Shape shape = ObjectCacheUtil.GetOrCreate(Api, "ingotpileshape", () => Shape.TryGet(Api, "shapes/block/metal/ingotpile.json"));
		if (shape == null)
		{
			return;
		}
		foreach (string textureCode in base.Block.Textures.Keys)
		{
			ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
			MeshData[] meshes = new MeshData[65];
			tmpMetalCode = textureCode;
			for (int i = 0; i <= 64; i++)
			{
				MeshData mesh = meshes[i];
				mesher.TesselateShape("ingotPile", shape, out mesh, this, null, 0, 0, 0, i);
			}
			meshesByType[tmpMetalCode] = meshes;
		}
		tmpTextureSource = null;
		tmpMetalCode = null;
		tmpBlock = null;
	}

	public override bool TryPutItem(IPlayer player)
	{
		return base.TryPutItem(player);
	}

	public BlockEntityIngotPile()
	{
		inventory = new InventoryGeneric(1, BlockCode, null, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		inventory.ResolveBlocksOrItems();
		capi = api as ICoreClientAPI;
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
			EnsureMeshExists();
			MeshData[] mesh = null;
			if (MetalType != null && meshesByType.TryGetValue(MetalType, out mesh))
			{
				meshdata.AddMeshData(mesh[inventory[0].StackSize]);
			}
		}
		return true;
	}
}
