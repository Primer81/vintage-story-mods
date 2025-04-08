using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BEBehaviorGiveItemPerPlayer : BlockEntityBehavior
{
	public Dictionary<string, double> retrievedTotalDaysByPlayerUid = new Dictionary<string, double>();

	private double resetDays;

	private bool selfRetrieved;

	public BEBehaviorGiveItemPerPlayer(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		resetDays = base.Block.Attributes?["resetAfterDays"].AsDouble(-1.0) ?? (-1.0);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		ITreeAttribute rtree = tree.GetTreeAttribute("retrievedTotalDaysByPlayerUid");
		if (rtree != null)
		{
			foreach (KeyValuePair<string, IAttribute> val in rtree)
			{
				retrievedTotalDaysByPlayerUid[val.Key] = (val.Value as DoubleAttribute).value;
			}
		}
		if (Api is ICoreClientAPI capi)
		{
			selfRetrieved = false;
			if (retrievedTotalDaysByPlayerUid.TryGetValue(capi.World.Player.PlayerUID, out var recievedTotalDays))
			{
				selfRetrieved = resetDays < 0.0 || Api.World.Calendar.TotalDays - recievedTotalDays < resetDays;
			}
		}
		base.FromTreeAttributes(tree, worldAccessForResolve);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		TreeAttribute rtree = new TreeAttribute();
		foreach (KeyValuePair<string, double> val in retrievedTotalDaysByPlayerUid)
		{
			rtree.SetDouble(val.Key, val.Value);
		}
		tree["retrievedTotalDaysByPlayerUid"] = rtree;
		base.ToTreeAttributes(tree);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (selfRetrieved)
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			CompositeShape cshape = base.Block.Attributes["lootedShape"].AsObject<CompositeShape>();
			if (cshape != null)
			{
				ITexPositionSource texSource = capi.Tesselator.GetTextureSource(base.Block);
				capi.Tesselator.TesselateShape("lootedShape", cshape.Base, cshape, out var meshdata, texSource, 0, 0, 0);
				mesher.AddMeshData(meshdata);
				return true;
			}
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	public void OnInteract(IPlayer byPlayer)
	{
		if (byPlayer == null || Api.Side != EnumAppSide.Server || (retrievedTotalDaysByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var recievedTotalDays) && (resetDays < 0.0 || Api.World.Calendar.TotalDays - recievedTotalDays < resetDays)))
		{
			return;
		}
		retrievedTotalDaysByPlayerUid[byPlayer.PlayerUID] = Api.World.Calendar.TotalDays;
		JsonItemStack jstack = base.Block.Attributes["giveItem"].AsObject<JsonItemStack>();
		if (jstack == null)
		{
			Api.Logger.Warning(string.Concat("Block code ", base.Block.Code, " attribute giveItem has GiveItemPerPlayer behavior but no giveItem defined"));
		}
		else if (jstack.Resolve(Api.World, string.Concat("Block code ", base.Block.Code, " attribute giveItem")))
		{
			if (!byPlayer.InventoryManager.TryGiveItemstack(jstack.ResolvedItemstack))
			{
				Api.World.SpawnItemEntity(jstack.ResolvedItemstack, base.Pos);
			}
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}
}
