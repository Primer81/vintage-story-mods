using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class LootList
{
	public float Tries;

	public List<LootItem> lootItems = new List<LootItem>();

	public float TotalChance;

	public ItemStack[] GenerateLoot(IWorldAccessor world, IPlayer forPlayer)
	{
		List<ItemStack> stacks = new List<ItemStack>();
		int variant = world.Rand.Next();
		float curtries = Tries;
		float dropRate = forPlayer?.Entity.Stats.GetBlended("vesselContentsDropRate") ?? 1f;
		while (curtries >= 1f || (double)curtries > world.Rand.NextDouble())
		{
			lootItems.Shuffle(world.Rand);
			double choice = world.Rand.NextDouble() * (double)TotalChance;
			foreach (LootItem lootItem in lootItems)
			{
				choice -= (double)lootItem.chance;
				if (choice <= 0.0)
				{
					int quantity = lootItem.GetDropQuantity(world, dropRate);
					ItemStack stack = lootItem.GetItemStack(world, variant, quantity);
					if (stack != null)
					{
						stacks.Add(stack);
					}
					break;
				}
			}
			curtries -= 1f;
		}
		return stacks.ToArray();
	}

	public static LootList Create(float tries, params LootItem[] lootItems)
	{
		LootList list = new LootList();
		list.Tries = tries;
		list.lootItems.AddRange(lootItems);
		for (int i = 0; i < lootItems.Length; i++)
		{
			list.TotalChance += lootItems[i].chance;
		}
		return list;
	}
}
