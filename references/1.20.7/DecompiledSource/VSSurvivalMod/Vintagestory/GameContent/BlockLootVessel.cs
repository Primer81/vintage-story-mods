using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLootVessel : Block
{
	private LootList lootList;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		lootList = LootList.Create(Attributes["lootTries"].AsInt(), Attributes["lootList"].AsObject<LootItem[]>());
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		List<BlockDropItemStack> drops = new List<BlockDropItemStack>();
		foreach (LootItem val in lootList.lootItems)
		{
			for (int i = 0; i < val.codes.Length; i++)
			{
				ItemStack lstack = val.GetItemStack(api.World, i, 1);
				if (lstack == null)
				{
					continue;
				}
				BlockDropItemStack stack = new BlockDropItemStack(lstack);
				if (stack != null)
				{
					stack.Quantity.avg = val.chance / lootList.TotalChance / (float)val.codes.Length;
					if (drops.FirstOrDefault((BlockDropItemStack dstack) => dstack.ResolvedItemstack.Equals(api.World, stack.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)) == null)
					{
						drops.Add(stack);
					}
				}
			}
		}
		return drops.ToArray();
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		float selfdropRate = (byPlayer?.Entity.Stats.GetBlended("wholeVesselLootChance") ?? 0f) - 1f;
		if (api.World.Rand.NextDouble() < (double)selfdropRate)
		{
			return new ItemStack[1]
			{
				new ItemStack(this)
			};
		}
		if (lootList == null)
		{
			return new ItemStack[0];
		}
		return lootList.GenerateLoot(world, byPlayer);
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		EnumTool? tool = itemslot.Itemstack?.Collectible?.Tool;
		if (tool.GetValueOrDefault() == EnumTool.Hammer || tool.GetValueOrDefault() == EnumTool.Pickaxe || tool.GetValueOrDefault() == EnumTool.Shovel || tool.GetValueOrDefault() == EnumTool.Sword || tool.GetValueOrDefault() == EnumTool.Spear || tool.GetValueOrDefault() == EnumTool.Axe || tool.GetValueOrDefault() == EnumTool.Hoe)
		{
			if (counter % 5 == 0 || remainingResistance <= 0f)
			{
				double posx = (double)blockSel.Position.X + blockSel.HitPosition.X;
				double posy = (double)blockSel.Position.InternalY + blockSel.HitPosition.Y;
				double posz = (double)blockSel.Position.Z + blockSel.HitPosition.Z;
				player.Entity.World.PlaySoundAt((remainingResistance > 0f) ? Sounds.GetHitSound(player) : Sounds.GetBreakSound(player), posx, posy, posz, player, randomizePitch: true, 16f);
			}
			return remainingResistance - 0.05f;
		}
		return base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		SimpleParticleProperties props = new SimpleParticleProperties(15f, 22f, ColorUtil.ToRgba(150, 255, 255, 255), new Vec3d(pos.X, pos.Y, pos.Z), new Vec3d(pos.X + 1, pos.Y + 1, pos.Z + 1), new Vec3f(-0.2f, -0.1f, -0.2f), new Vec3f(0.2f, 0.2f, 0.2f), 1.5f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
		props.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -200f);
		props.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f);
		world.SpawnParticles(props);
		SimpleParticleProperties spiders = new SimpleParticleProperties(8f, 16f, ColorUtil.ToRgba(255, 30, 30, 30), new Vec3d(pos.X, pos.Y, pos.Z), new Vec3d(pos.X + 1, pos.Y + 1, pos.Z + 1), new Vec3f(-2f, -0.3f, -2f), new Vec3f(2f, 1f, 2f), 1f, 0.5f, 0.5f, 1.5f);
		world.SpawnParticles(spiders);
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
	}
}
