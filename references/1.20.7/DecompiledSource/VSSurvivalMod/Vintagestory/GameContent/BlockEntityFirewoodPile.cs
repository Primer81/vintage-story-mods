using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityFirewoodPile : BlockEntityItemPile, IBlockEntityItemPile
{
	internal AssetLocation soundLocation = new AssetLocation("sounds/block/planks");

	public override AssetLocation SoundLocation => soundLocation;

	public override string BlockCode => "firewoodpile";

	public override int DefaultTakeQuantity => 2;

	public override int BulkTakeQuantity => 8;

	public override int MaxStackSize => 32;

	private MeshData[] meshes => ObjectCacheUtil.GetOrCreate(Api, "firewoodpile-meshes", delegate
	{
		MeshData[] array = new MeshData[17];
		Block block = Api.World.BlockAccessor.GetBlock(Pos);
		Shape shape = Shape.TryGet(Api, "shapes/block/wood/firewoodpile.json");
		ITesselatorAPI tesselator = ((ICoreClientAPI)Api).Tesselator;
		for (int i = 0; i <= 16; i++)
		{
			tesselator.TesselateShape(block, shape, out array[i], null, i);
		}
		return array;
	});

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RandomizeSoundPitch = true;
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		lock (inventoryLock)
		{
			int index = Math.Min(16, (int)Math.Ceiling((double)inventory[0].StackSize / 2.0));
			meshdata.AddMeshData(meshes[index]);
		}
		return true;
	}
}
