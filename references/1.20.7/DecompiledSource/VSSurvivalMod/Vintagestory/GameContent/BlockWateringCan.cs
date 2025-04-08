using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockWateringCan : Block
{
	public float CapacitySeconds = 32f;

	public static SimpleParticleProperties WaterParticles;

	private ILoadedSound pouringLoop;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		WaterParticles = new SimpleParticleProperties(1f, 1f, -1, new Vec3d(), new Vec3d(), new Vec3f(-1.5f, 0f, -1.5f), new Vec3f(1.5f, 3f, 1.5f), 1f, 1f, 0.33f, 0.75f);
		WaterParticles.AddPos = new Vec3d(0.0625, 0.125, 0.0625);
		WaterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.7f);
		WaterParticles.ClimateColorMap = "climateWaterTint";
		WaterParticles.AddQuantity = 1f;
		CapacitySeconds = Attributes?["capacitySeconds"].AsFloat(32f) ?? 32f;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		slot.Itemstack.TempAttributes.SetFloat("secondsUsed", 0f);
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position, 2);
		if (block.LiquidCode == "water")
		{
			BlockPos pos = blockSel.Position;
			SetRemainingWateringSeconds(slot.Itemstack, CapacitySeconds);
			slot.Itemstack.TempAttributes.SetInt("refilled", 1);
			slot.MarkDirty();
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/water"), pos, 0.35, byPlayer);
			handHandling = EnumHandHandling.PreventDefault;
			return;
		}
		BlockBucket bucket = byEntity.World.BlockAccessor.GetBlock(blockSel.Position) as BlockBucket;
		Block contentBlock = bucket?.GetContent(blockSel.Position)?.Block;
		if (bucket != null && contentBlock?.LiquidCode == "water")
		{
			WaterTightContainableProps liquidProps = contentBlock.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>(null, block.Code.Domain);
			int quantityItems = (int)(5f / liquidProps.ItemsPerLitre);
			bucket.GetCurrentLitres(blockSel.Position);
			BlockPos pos2 = blockSel.Position;
			ItemStack takenWater = bucket.TryTakeContent(blockSel.Position, quantityItems);
			SetRemainingWateringSeconds(slot.Itemstack, CapacitySeconds * (float)takenWater.StackSize * liquidProps.ItemsPerLitre);
			slot.Itemstack.TempAttributes.SetInt("refilled", 1);
			slot.MarkDirty();
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/water"), pos2, 0.35, byPlayer);
			handHandling = EnumHandHandling.PreventDefault;
			return;
		}
		slot.Itemstack.TempAttributes.SetInt("refilled", 0);
		if (GetRemainingWateringSeconds(slot.Itemstack) <= 0f)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			byEntity.World.RegisterCallback(After350ms, 350);
		}
		handHandling = EnumHandHandling.PreventDefault;
	}

	private void After350ms(float dt)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		IClientPlayer plr = capi.World.Player;
		EntityPlayer plrentity = plr.Entity;
		if (plrentity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
		{
			capi.World.PlaySoundAt(new AssetLocation("sounds/effect/watering"), plrentity, plr);
		}
		if (plrentity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
		{
			if (pouringLoop != null)
			{
				pouringLoop.FadeIn(0.3f, null);
				return;
			}
			pouringLoop = capi.World.LoadSound(new SoundParams
			{
				DisposeOnFinish = false,
				Location = new AssetLocation("sounds/effect/watering-loop.ogg"),
				Position = new Vec3f(),
				RelativePosition = true,
				ShouldLoop = true,
				Range = 16f,
				Volume = 0.2f,
				Pitch = 0.5f
			});
			pouringLoop.Start();
			pouringLoop.FadeIn(0.15f, null);
		}
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		if (entityItem.FeetInLiquid)
		{
			SetRemainingWateringSeconds(entityItem.Itemstack, CapacitySeconds);
		}
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityWateringCan becan)
		{
			ItemStack stack = new ItemStack(this);
			SetRemainingWateringSeconds(stack, becan.SecondsWateringLeft);
			return stack;
		}
		return base.OnPickBlock(world, pos);
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null)
		{
			return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		if (slot.Itemstack.TempAttributes.GetInt("refilled") > 0)
		{
			return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		float prevsecondsused = slot.Itemstack.TempAttributes.GetFloat("secondsUsed");
		slot.Itemstack.TempAttributes.SetFloat("secondsUsed", secondsUsed);
		float remainingwater = GetRemainingWateringSeconds(slot.Itemstack);
		SetRemainingWateringSeconds(slot.Itemstack, remainingwater -= secondsUsed - prevsecondsused);
		if (remainingwater <= 0f)
		{
			return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
		IWorldAccessor world = byEntity.World;
		BlockPos targetPos = blockSel.Position;
		if (api.World.Side == EnumAppSide.Server)
		{
			(world.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face))?.GetBehavior<BEBehaviorBurning>())?.KillFire(consumeFuel: false);
			(world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorBurning>())?.KillFire(consumeFuel: false);
			GetBEBehavior<BEBehaviorTemperatureSensitive>(blockSel.Position)?.OnWatered(secondsUsed - prevsecondsused);
			for (int dx = -2; dx < 2; dx++)
			{
				for (int dy = -2; dy < 2; dy++)
				{
					for (int dz = -2; dz < 2; dz++)
					{
						int x = (int)(blockSel.HitPosition.X * 16.0) + dx;
						int y = (int)(blockSel.HitPosition.Y * 16.0) + dy;
						int z = (int)(blockSel.HitPosition.Z * 16.0) + dz;
						if (x >= 0 && x <= 15 && y >= 0 && y <= 15 && z >= 0 && z <= 15)
						{
							DecorBits decorPosition = new DecorBits(blockSel.Face, x, 15 - y, z);
							if (world.BlockAccessor.GetDecor(blockSel.Position, decorPosition)?.FirstCodePart() == "caveart")
							{
								world.BlockAccessor.BreakDecor(blockSel.Position, blockSel.Face, decorPosition);
							}
						}
					}
				}
			}
		}
		Block block = world.BlockAccessor.GetBlock(blockSel.Position);
		bool notOnSolidblock = false;
		if (block.CollisionBoxes == null || block.CollisionBoxes.Length == 0)
		{
			block = world.BlockAccessor.GetBlock(blockSel.Position, 2);
			if ((block.CollisionBoxes == null || block.CollisionBoxes.Length == 0) && !block.IsLiquid())
			{
				notOnSolidblock = true;
				targetPos = targetPos.DownCopy();
			}
		}
		if (world.BlockAccessor.GetBlockEntity(targetPos) is BlockEntityFarmland be)
		{
			be.WaterFarmland(secondsUsed - prevsecondsused);
		}
		float speed = 3f;
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (secondsUsed > 1f / speed)
		{
			Vec3d pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
			if (notOnSolidblock)
			{
				pos.Y = (double)(int)pos.Y + 0.05;
			}
			WaterParticles.MinPos = pos.Add(-0.0625, 0.0625, -0.0625);
			byEntity.World.SpawnParticles(WaterParticles, byPlayer);
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		pouringLoop?.Stop();
		pouringLoop?.Dispose();
		pouringLoop = null;
		slot.MarkDirty();
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWateringCan bect)
		{
			BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
			double dz = byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
			float num2 = (float)Math.Atan2(y, dz);
			float deg22dot5rad = (float)Math.PI / 8f;
			float roundRad = (float)(int)Math.Round(num2 / deg22dot5rad) * deg22dot5rad;
			bect.MeshAngle = roundRad;
		}
		return num;
	}

	public float GetRemainingWateringSeconds(ItemStack stack)
	{
		return stack.Attributes.GetFloat("wateringSeconds");
	}

	public void SetRemainingWateringSeconds(ItemStack stack, float seconds)
	{
		stack.Attributes.SetFloat("wateringSeconds", seconds);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine();
		double perc = Math.Round(100f * GetRemainingWateringSeconds(inSlot.Itemstack) / CapacitySeconds);
		string colorn = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)GameMath.Clamp(perc, 0.0, 99.0)]);
		if (perc < 1.0)
		{
			dsc.AppendLine(string.Format("<font color=\"{0}\">" + Lang.Get("Empty") + "</font>", colorn));
			return;
		}
		dsc.AppendLine(string.Format("<font color=\"{0}\">" + Lang.Get("{0}% full", perc) + "</font>", colorn));
	}
}
