using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BEBehaviorJonasHydraulicPump : BEBehaviorControlPointAnimatable, INetworkedLight
{
	private ControlPoint cp;

	private bool on;

	private string networkCode;

	public string oncommands;

	public string offcommands;

	private bool hasTempGear;

	private bool hasPumphead;

	private bool IsRepaired
	{
		get
		{
			if (hasTempGear)
			{
				return hasPumphead;
			}
			return false;
		}
	}

	private bool ReceivesPower
	{
		get
		{
			AnimationMetaData obj = animControlPoint?.ControlData as AnimationMetaData;
			if (obj == null)
			{
				return false;
			}
			return obj.AnimationSpeed > 0f;
		}
	}

	protected override Shape AnimationShape
	{
		get
		{
			AssetLocation shapePath = base.Block.ShapeInventory.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			return Shape.TryGet(Api, shapePath);
		}
	}

	public BEBehaviorJonasHydraulicPump(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		Api = api;
		registerToLightNetworkControlPoint();
		base.Initialize(api, properties);
		updatePumpingState();
	}

	public void setNetwork(string networkCode)
	{
		this.networkCode = networkCode;
		registerToLightNetworkControlPoint();
		Blockentity.MarkDirty(redrawOnClient: true);
		updatePumpingState();
	}

	private void registerToLightNetworkControlPoint()
	{
		if (networkCode != null)
		{
			modSys = Api.ModLoader.GetModSystem<ModSystemControlPoints>();
			AssetLocation controlpointcode = AssetLocation.Create(networkCode, base.Block.Code.Domain);
			cp = modSys[controlpointcode];
			on = IsRepaired && ReceivesPower;
		}
	}

	protected override void BEBehaviorControlPointAnimatable_Activate(ControlPoint cpoint)
	{
		if (IsRepaired)
		{
			base.BEBehaviorControlPointAnimatable_Activate(cpoint);
		}
		else
		{
			animControlPoint = cpoint;
		}
		on = IsRepaired && ReceivesPower;
		updatePumpingState();
	}

	internal void Interact(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (Api.Side != EnumAppSide.Server)
		{
			return;
		}
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
		{
			if (byPlayer.Entity.Controls.ShiftKey)
			{
				hasTempGear = false;
				hasPumphead = false;
			}
			else
			{
				on = !on;
				updatePumpingState();
			}
			Blockentity.MarkDirty(redrawOnClient: true);
		}
		else
		{
			if (IsRepaired)
			{
				return;
			}
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (slot.Itemstack?.Collectible.Code.Path == "largegear-temporal" && !hasTempGear)
			{
				if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
				{
					slot.TakeOut(1);
				}
				hasTempGear = true;
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/latch"), base.Pos, -0.5, null, randomizePitch: true, 16f);
			}
			if (slot.Itemstack?.Collectible.Code.Path == "jonasparts-pumphead" && !hasPumphead)
			{
				if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
				{
					slot.TakeOut(1);
				}
				hasPumphead = true;
				Api.World.PlaySoundAt(new AssetLocation("sounds/effect/latch"), base.Pos, -0.5, null, randomizePitch: true, 16f);
			}
			on = IsRepaired && ReceivesPower;
			updatePumpingState();
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}

	protected void updatePumpingState()
	{
		if (Api != null)
		{
			if (IsRepaired)
			{
				updateAnimationstate();
			}
			if (cp != null)
			{
				cp.ControlData = on;
				cp.Trigger();
			}
			Caller caller2 = new Caller();
			caller2.CallerPrivileges = new string[1] { "*" };
			caller2.Pos = base.Pos.ToVec3d();
			caller2.Type = EnumCallerType.Block;
			Caller caller = caller2;
			if (Blockentity is BlockEntityCommands becs)
			{
				becs.CallingPrivileges = caller.CallerPrivileges;
				becs.Execute(caller, on ? oncommands : offcommands);
			}
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		hasPumphead = tree.GetBool("hasPumpHead");
		hasTempGear = tree.GetBool("hasTempGear");
		on = tree.GetBool("on");
		networkCode = tree.GetString("networkCode");
		if (networkCode == "")
		{
			networkCode = null;
		}
		oncommands = tree.GetString("oncommands");
		offcommands = tree.GetString("offcommands");
		updatePumpingState();
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("hasTempGear", hasTempGear);
		tree.SetBool("hasPumpHead", hasPumphead);
		tree.SetBool("on", on);
		tree.SetString("networkCode", networkCode);
		tree.SetString("oncommands", oncommands);
		tree.SetString("offcommands", offcommands);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine("network code: " + networkCode);
			dsc.AppendLine(on ? "On" : "Off");
			dsc.AppendLine("oncommand:" + oncommands);
			dsc.AppendLine("offcommand:" + offcommands);
		}
		if (!ReceivesPower)
		{
			dsc.AppendLine(Lang.Get("No power."));
		}
		if (!hasTempGear)
		{
			dsc.AppendLine(Lang.Get("Missing large temporal gear."));
		}
		if (!hasPumphead)
		{
			dsc.AppendLine(Lang.Get("Missing pump head."));
		}
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (animUtil.activeAnimationsByAnimCode.Count == 0)
		{
			if (hasPumphead)
			{
				mesher.AddMeshData(genMesh(new AssetLocation("shapes/block/machine/jonas/pumphead1-flywheel.json")));
			}
			if (hasTempGear)
			{
				mesher.AddMeshData(genMesh(new AssetLocation("shapes/block/machine/jonas/pumphead1-gear.json")));
			}
			return false;
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}

	private MeshData genMesh(AssetLocation assetLocation)
	{
		return ObjectCacheUtil.GetOrCreate(Api, "hydrpumpmesh-" + assetLocation.Path + "-" + base.Block.Shape.rotateY, delegate
		{
			Shape shape = Api.Assets.TryGet(assetLocation).ToObject<Shape>();
			(Api as ICoreClientAPI).Tesselator.TesselateShape(base.Block, shape, out var modeldata, base.Block.Shape.RotateXYZCopy);
			return modeldata;
		});
	}
}
