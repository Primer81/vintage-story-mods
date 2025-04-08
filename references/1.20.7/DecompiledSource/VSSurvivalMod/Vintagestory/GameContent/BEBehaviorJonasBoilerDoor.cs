using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorJonasBoilerDoor : BlockEntityBehavior
{
	private ModSystemControlPoints modSys;

	private AnimationMetaData animData;

	private ControlPoint cp;

	private bool on;

	private float heatAccum;

	public BEBehaviorJonasBoilerDoor(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		api.Event.OnTestBlockAccess += Event_OnTestBlockAccess;
		if (api.Side == EnumAppSide.Server)
		{
			Blockentity.RegisterGameTickListener(checkFireServer, 1000, 12);
		}
		animData = properties["animData"].AsObject<AnimationMetaData>();
		AssetLocation controlpointcode = AssetLocation.Create(properties["controlpointcode"].ToString(), base.Block.Code.Domain);
		modSys = api.ModLoader.GetModSystem<ModSystemControlPoints>();
		cp = modSys[controlpointcode];
		cp.ControlData = animData;
		animData.AnimationSpeed = (on ? 1 : 0);
		cp.Trigger();
	}

	private EnumWorldAccessResponse Event_OnTestBlockAccess(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response)
	{
		BlockFacing facing = BlockFacing.FromCode(base.Block.Variant["side"]);
		BlockPos a = base.Pos.AddCopy(facing);
		BlockPos b = blockSel.Position.UpCopy();
		if ((a == b || a == blockSel.Position) && player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible is ItemCoal)
		{
			return EnumWorldAccessResponse.Granted;
		}
		return response;
	}

	private void checkFireServer(float dt)
	{
		BlockFacing facing = BlockFacing.FromCode(base.Block.Variant["side"]);
		if (Api.World.BlockAccessor.GetBlockEntity(base.Pos.AddCopy(facing)) is BlockEntityCoalPile { IsBurning: not false })
		{
			heatAccum = Math.Min(10f, heatAccum + dt);
		}
		else
		{
			heatAccum = Math.Max(0f, heatAccum - dt);
		}
		if (!on && heatAccum >= 9.9f)
		{
			on = true;
			animData.AnimationSpeed = (on ? 1 : 0);
			cp.Trigger();
			Blockentity.MarkDirty(redrawOnClient: true);
		}
		else if (on && heatAccum <= 0f)
		{
			on = false;
			animData.AnimationSpeed = (on ? 1 : 0);
			cp.Trigger();
			Blockentity.MarkDirty(redrawOnClient: true);
		}
	}

	internal void Interact(IPlayer byPlayer, BlockSelection blockSel)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		on = tree.GetBool("on");
		heatAccum = tree.GetFloat("heatAccum");
		if (Api != null && worldAccessForResolve.Side == EnumAppSide.Client)
		{
			animData.AnimationSpeed = (on ? 1 : 0);
			cp.Trigger();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("on", on);
		tree.SetFloat("heatAccum", heatAccum);
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine("animspeed: " + animData.AnimationSpeed);
		}
	}
}
