using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class BlockBehaviorWrenchOrientable : BlockBehavior
{
	public static Dictionary<string, SortedSet<AssetLocation>> VariantsByType = new Dictionary<string, SortedSet<AssetLocation>>();

	public string BaseCode;

	private bool hideInteractionHelpInSurvival;

	private static List<ItemStack> wrenchItems = new List<ItemStack>();

	public BlockBehaviorWrenchOrientable(Block block)
		: base(block)
	{
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
	{
		if (hideInteractionHelpInSurvival && forPlayer != null && forPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival)
		{
			return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling);
		}
		handling = EnumHandling.PassThrough;
		if (wrenchItems.Count == 0)
		{
			Item[] array = world.SearchItems(new AssetLocation("wrench-*"));
			foreach (Item item in array)
			{
				wrenchItems.Add(new ItemStack(item));
			}
		}
		bool notProtected = true;
		if (world.Claims != null && world is IClientWorldAccessor clientWorld)
		{
			IClientPlayer player = clientWorld.Player;
			if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Survival && world.Claims.TestAccess(clientWorld.Player, selection.Position, EnumBlockAccessFlags.BuildOrBreak) != 0)
			{
				notProtected = false;
			}
		}
		if (wrenchItems.Count > 0 && notProtected)
		{
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "Rotate",
					Itemstacks = wrenchItems.ToArray(),
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return new WorldInteraction[0];
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		hideInteractionHelpInSurvival = properties["hideInteractionHelpInSurvival"].AsBool();
		BaseCode = properties["baseCode"].AsString();
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (BaseCode != null)
		{
			if (!VariantsByType.TryGetValue(BaseCode, out var vars))
			{
				vars = (VariantsByType[BaseCode] = new SortedSet<AssetLocation>());
			}
			vars.Add(block.Code);
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		wrenchItems.Clear();
		VariantsByType.Clear();
	}
}
