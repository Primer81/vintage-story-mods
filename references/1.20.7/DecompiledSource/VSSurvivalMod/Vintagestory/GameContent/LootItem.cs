using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class LootItem
{
	public AssetLocation[] codes;

	public EnumItemClass type;

	public float chance;

	public float minQuantity;

	public float maxQuantity;

	public static LootItem Item(float chance, float minQuantity, float maxQuantity, params string[] codes)
	{
		return new LootItem
		{
			codes = AssetLocation.toLocations(codes),
			type = EnumItemClass.Item,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public static LootItem Item(float chance, float minQuantity, float maxQuantity, params AssetLocation[] codes)
	{
		return new LootItem
		{
			codes = codes,
			type = EnumItemClass.Item,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public static LootItem Block(float chance, float minQuantity, float maxQuantity, params string[] codes)
	{
		return new LootItem
		{
			codes = AssetLocation.toLocations(codes),
			type = EnumItemClass.Block,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public static LootItem Block(float chance, float minQuantity, float maxQuantity, params AssetLocation[] codes)
	{
		return new LootItem
		{
			codes = codes,
			type = EnumItemClass.Block,
			chance = chance,
			minQuantity = minQuantity,
			maxQuantity = maxQuantity
		};
	}

	public ItemStack GetItemStack(IWorldAccessor world, int variant, int quantity)
	{
		ItemStack stack = null;
		AssetLocation code = codes[variant % codes.Length];
		if (type == EnumItemClass.Block)
		{
			Block block = world.GetBlock(code);
			if (block != null)
			{
				stack = new ItemStack(block, quantity);
			}
			else
			{
				world.Logger.Warning("BlockLootVessel: Failed resolving block code {0}", code);
			}
		}
		else
		{
			Item item = world.GetItem(code);
			if (item != null)
			{
				stack = new ItemStack(item, quantity);
			}
			else
			{
				world.Logger.Warning("BlockLootVessel: Failed resolving item code {0}", code);
			}
		}
		return stack;
	}

	public int GetDropQuantity(IWorldAccessor world, float dropQuantityMul)
	{
		float qfloat = dropQuantityMul * (minQuantity + (float)world.Rand.NextDouble() * (maxQuantity - minQuantity));
		return GameMath.RoundRandom(world.Rand, qfloat);
	}
}
