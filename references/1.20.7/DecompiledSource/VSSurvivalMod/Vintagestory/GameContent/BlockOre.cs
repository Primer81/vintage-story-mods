using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockOre : Block
{
	public string Grade
	{
		get
		{
			string part = Variant["grade"];
			switch (part)
			{
			case "poor":
			case "medium":
			case "rich":
			case "bountiful":
				return part;
			default:
				return null;
			}
		}
	}

	public string MotherRock => Variant["rock"];

	public string OreName => Variant["type"];

	public string InfoText
	{
		get
		{
			StringBuilder dsc = new StringBuilder();
			if (Grade != null)
			{
				dsc.AppendLine(Lang.Get("ore-grade-" + Grade));
			}
			dsc.AppendLine(Lang.Get("ore-in-rock", Lang.Get("ore-" + OreName), Lang.Get("rock-" + MotherRock)));
			return dsc.ToString();
		}
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		dropQuantityMultiplier *= byPlayer?.Entity.Stats.GetBlended("oreDropRate") ?? 1f;
		EnumHandling handled = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int j = 0; j < blockBehaviors.Length; j++)
		{
			blockBehaviors[j].OnBlockBroken(world, pos, byPlayer, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (handled == EnumHandling.PreventDefault)
		{
			return;
		}
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] drops = GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
			if (drops != null)
			{
				for (int i = 0; i < drops.Length; i++)
				{
					world.SpawnItemEntity(drops[i], pos);
				}
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		SpawnBlockBrokenParticles(pos);
		world.BlockAccessor.SetBlock(0, pos);
		if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			CollectibleObject coll = byPlayer?.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			if (OreName == "flint" && (coll == null || coll.ToolTier == 0))
			{
				world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("rock-" + MotherRock)).BlockId, pos);
			}
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine(InfoText);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		return InfoText + "\n" + ((OreName == "flint") ? (Lang.Get("Break with bare hands to extract flint") + "\n") : "") + base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
	{
		EnumHandling handled = EnumHandling.PassThrough;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		for (int k = 0; k < blockBehaviors.Length; k++)
		{
			blockBehaviors[k].OnBlockExploded(world, pos, explosionCenter, blastType, ref handled);
			if (handled == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
		if (handled == EnumHandling.PreventDefault)
		{
			return;
		}
		world.BulkBlockAccessor.SetBlock(0, pos);
		double dropChancce = ExplosionDropChance(world, pos, blastType);
		if (world.Rand.NextDouble() < dropChancce)
		{
			ItemStack[] drops = GetDrops(world, pos, null);
			if (drops == null)
			{
				return;
			}
			for (int i = 0; i < drops.Length; i++)
			{
				if (!SplitDropStacks)
				{
					continue;
				}
				for (int j = 0; j < drops[i].StackSize; j++)
				{
					ItemStack stack = drops[i].Clone();
					if (!stack.Collectible.Code.Path.Contains("crystal"))
					{
						stack.StackSize = 1;
						world.SpawnItemEntity(stack, pos);
					}
				}
			}
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
		}
	}
}
