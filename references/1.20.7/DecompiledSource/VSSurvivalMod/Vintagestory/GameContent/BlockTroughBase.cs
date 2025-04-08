using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockTroughBase : Block
{
	public ContentConfig[] contentConfigs;

	public WorldInteraction[] placeInteractionHelp;

	public BlockPos RootOffset = new BlockPos();

	protected string[] unsuitableEntityCodesBeginsWith = new string[0];

	protected string[] unsuitableEntityCodesExact;

	protected string unsuitableEntityFirstLetters = "";

	public void init()
	{
		CanStep = false;
		contentConfigs = ObjectCacheUtil.GetOrCreate(api, "troughContentConfigs-" + Code, delegate
		{
			ContentConfig[] array2 = Attributes?["contentConfig"]?.AsObject<ContentConfig[]>();
			if (array2 == null)
			{
				return (ContentConfig[])null;
			}
			ContentConfig[] array3 = array2;
			foreach (ContentConfig contentConfig in array3)
			{
				if (!contentConfig.Content.Code.Path.Contains('*'))
				{
					contentConfig.Content.Resolve(api.World, "troughcontentconfig");
				}
			}
			return array2;
		});
		List<ItemStack> allowedstacks = new List<ItemStack>();
		ContentConfig[] array = contentConfigs;
		foreach (ContentConfig val in array)
		{
			if (val.Content.Code.Path.Contains('*'))
			{
				if (val.Content.Type == EnumItemClass.Block)
				{
					allowedstacks.AddRange(from block in api.World.SearchBlocks(val.Content.Code)
						select new ItemStack(block, val.QuantityPerFillLevel));
				}
				else
				{
					allowedstacks.AddRange(from item in api.World.SearchItems(val.Content.Code)
						select new ItemStack(item, val.QuantityPerFillLevel));
				}
			}
			else if (val.Content.ResolvedItemstack != null)
			{
				ItemStack stack = val.Content.ResolvedItemstack.Clone();
				stack.StackSize = val.QuantityPerFillLevel;
				allowedstacks.Add(stack);
			}
		}
		placeInteractionHelp = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-trough-addfeed",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = allowedstacks.ToArray(),
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					if (!(api.World.BlockAccessor.GetBlockEntity(bs.Position + RootOffset) is BlockEntityTrough { IsFull: false } blockEntityTrough))
					{
						return (ItemStack[])null;
					}
					ItemStack[] nonEmptyContentStacks = blockEntityTrough.GetNonEmptyContentStacks();
					return (nonEmptyContentStacks != null && nonEmptyContentStacks.Length != 0) ? nonEmptyContentStacks : wi.Itemstacks;
				}
			}
		};
		string[] codes = Attributes?["unsuitableFor"].AsArray(new string[0]);
		if (codes.Length != 0)
		{
			AiTaskBaseTargetable.InitializeTargetCodes(codes, ref unsuitableEntityCodesExact, ref unsuitableEntityCodesBeginsWith, ref unsuitableEntityFirstLetters);
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return placeInteractionHelp.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public virtual bool UnsuitableForEntity(string testPath)
	{
		if (unsuitableEntityFirstLetters.IndexOf(testPath[0]) < 0)
		{
			return false;
		}
		for (int j = 0; j < unsuitableEntityCodesExact.Length; j++)
		{
			if (testPath == unsuitableEntityCodesExact[j])
			{
				return true;
			}
		}
		for (int i = 0; i < unsuitableEntityCodesBeginsWith.Length; i++)
		{
			if (testPath.StartsWithFast(unsuitableEntityCodesBeginsWith[i]))
			{
				return true;
			}
		}
		return false;
	}
}
