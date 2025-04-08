using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorHandbookTextAndExtraInfo : CollectibleBehavior
{
	protected const int TinyPadding = 2;

	protected const int TinyIndent = 2;

	protected const int MarginBottom = 3;

	protected const int SmallPadding = 7;

	protected const int MediumPadding = 14;

	public ExtraHandbookSection[] ExtraHandBookSections;

	private ICoreAPI Api;

	public CollectibleBehaviorHandbookTextAndExtraInfo(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		Api = api;
		JsonObject obj = collObj.Attributes?["handbook"]?["extraSections"];
		if (obj != null && obj.Exists)
		{
			ExtraHandBookSections = obj?.AsObject<ExtraHandbookSection[]>();
		}
	}

	public virtual RichTextComponentBase[] GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		ItemStack stack = inSlot.Itemstack;
		List<RichTextComponentBase> components = new List<RichTextComponentBase>();
		addGeneralInfo(inSlot, capi, stack, components, out var marginTop, out var marginBottom);
		List<ItemStack> breakBlocks = new List<ItemStack>();
		foreach (ItemStack blockStack in allStacks)
		{
			if (blockStack.Block == null)
			{
				continue;
			}
			BlockDropItemStack[] droppedStacks = blockStack.Block.GetDropsForHandbook(blockStack, capi.World.Player);
			if (droppedStacks == null)
			{
				continue;
			}
			for (int i = 0; i < droppedStacks.Length; i++)
			{
				_ = droppedStacks[i];
				if (droppedStacks[i].ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					breakBlocks.Add(blockStack);
				}
			}
		}
		addDropsInfo(capi, openDetailPageFor, stack, components, marginTop, breakBlocks);
		bool haveText = addObtainedThroughInfo(capi, openDetailPageFor, stack, components, marginTop, breakBlocks);
		haveText = addFoundInInfo(capi, openDetailPageFor, stack, components, marginTop, haveText);
		haveText = addAlloyForInfo(capi, openDetailPageFor, stack, components, marginTop, haveText);
		haveText = addAlloyedFromInfo(capi, openDetailPageFor, stack, components, marginTop, haveText);
		haveText = addProcessesIntoInfo(inSlot, capi, openDetailPageFor, stack, components, marginTop, marginBottom, haveText);
		haveText = addIngredientForInfo(capi, openDetailPageFor, stack, components, marginTop, haveText);
		haveText = addCreatedByInfo(capi, allStacks, openDetailPageFor, stack, components, marginTop, haveText);
		addExtraSections(capi, stack, components, marginTop);
		addStorableInfo(capi, stack, components, marginTop);
		if (collObj is ICustomHandbookPageContent chp)
		{
			chp.OnHandbookPageComposed(components, inSlot, capi, allStacks, openDetailPageFor);
		}
		return components.ToArray();
	}

	protected void addGeneralInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack stack, List<RichTextComponentBase> components, out float marginTop, out float marginBottom)
	{
		components.Add(new ItemstackTextComponent(capi, stack, 100.0, 10.0));
		components.AddRange(VtmlUtil.Richtextify(capi, stack.GetName() + "\n", CairoFont.WhiteSmallishText()));
		CairoFont font = CairoFont.WhiteDetailText();
		if (capi.Settings.Bool["extendedDebugInfo"])
		{
			font.Color[3] = 0.5;
			components.AddRange(VtmlUtil.Richtextify(capi, "Page code:" + GuiHandbookItemStackPage.PageCodeForStack(stack) + "\n", font));
		}
		components.AddRange(VtmlUtil.Richtextify(capi, stack.GetDescription(capi.World, inSlot), CairoFont.WhiteSmallText()));
		marginTop = 7f;
		marginBottom = 3f;
	}

	protected void addDropsInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> breakBlocks)
	{
		if (stack.Class != 0)
		{
			return;
		}
		BlockDropItemStack[] blockdropStacks = stack.Block.GetDropsForHandbook(stack, capi.World.Player);
		List<ItemStack[]> breakBlocksWithToolStacks = new List<ItemStack[]>();
		List<EnumTool?> tools = new List<EnumTool?>();
		List<ItemStack> dropsStacks = new List<ItemStack>();
		if (blockdropStacks != null)
		{
			BlockDropItemStack[] array = blockdropStacks;
			foreach (BlockDropItemStack val in array)
			{
				dropsStacks.Add(val.ResolvedItemstack);
				object obj;
				if (val.Tool.HasValue)
				{
					ICoreClientAPI api = capi;
					EnumTool? tool2 = val.Tool;
					obj = ObjectCacheUtil.GetOrCreate(api, "blockhelp-collect-withtool-" + tool2.ToString(), delegate
					{
						List<ItemStack> list = new List<ItemStack>();
						foreach (CollectibleObject current in capi.World.Collectibles)
						{
							if (current.Tool == val.Tool)
							{
								list.Add(new ItemStack(current));
							}
						}
						return list.ToArray();
					});
				}
				else
				{
					obj = null;
				}
				ItemStack[] toolStacks2 = (ItemStack[])obj;
				tools.Add(val.Tool);
				breakBlocksWithToolStacks.Add(toolStacks2);
			}
		}
		if (dropsStacks == null || dropsStacks.Count <= 0 || (dropsStacks.Count == 1 && breakBlocks.Count == 1 && breakBlocks[0].Equals(capi.World, dropsStacks[0], GlobalConstants.IgnoredStackAttributes)))
		{
			return;
		}
		bool haveText = components.Count > 0;
		AddHeading(components, capi, "Drops when broken", ref haveText);
		components.Add(new ClearFloatTextComponent(capi, 2f));
		int i = 0;
		while (dropsStacks.Count > 0)
		{
			ItemStack dstack = dropsStacks[0];
			EnumTool? tool = tools[i];
			ItemStack[] toolStacks = breakBlocksWithToolStacks[i++];
			dropsStacks.RemoveAt(0);
			if (dstack == null)
			{
				continue;
			}
			SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, dropsStacks, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			if (toolStacks != null)
			{
				comp.ExtraTooltipText = "\n\n<font color=\"orange\">" + Lang.Get("break-requires-tool-" + tool.ToString().ToLowerInvariant()) + "</font>";
			}
			components.Add(comp);
			if (toolStacks != null)
			{
				comp = new SlideshowItemstackTextComponent(capi, toolStacks, 24.0, EnumFloat.Left, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				comp.renderOffset.X = 0f - (float)GuiElement.scaled(17.0);
				comp.renderOffset.Z = 100f;
				comp.ShowTooltip = false;
				components.Add(comp);
			}
		}
		components.Add(new ClearFloatTextComponent(capi, 2f));
	}

	protected bool addObtainedThroughInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> breakBlocks)
	{
		List<string> killCreatures = new List<string>();
		List<string> harvestCreatures = new List<string>();
		HashSet<string> harvestCreatureCodes = new HashSet<string>();
		foreach (EntityProperties val in capi.World.EntityTypes)
		{
			if (val.Drops == null)
			{
				continue;
			}
			for (int i = 0; i < val.Drops.Length; i++)
			{
				if (val.Drops[i].ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					killCreatures.Add(Lang.Get(val.Code.Domain + ":item-creature-" + val.Code.Path));
				}
			}
			BlockDropItemStack[] harvestableDrops = val.Attributes?["harvestableDrops"]?.AsArray<BlockDropItemStack>();
			if (harvestableDrops == null)
			{
				continue;
			}
			BlockDropItemStack[] array = harvestableDrops;
			foreach (BlockDropItemStack hstack in array)
			{
				hstack.Resolve(Api.World, "handbook info", new AssetLocation());
				if (hstack.ResolvedItemstack != null && hstack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					string code = val.Code.Domain + ":item-creature-" + val.Code.Path;
					JsonObject attributes = val.Attributes;
					if (attributes != null && (attributes["handbook"]["groupcode"]?.Exists).GetValueOrDefault())
					{
						code = val.Attributes?["handbook"]["groupcode"].AsString();
					}
					if (!harvestCreatureCodes.Contains(code))
					{
						harvestCreatures.Add(Lang.Get(code));
						harvestCreatureCodes.Add(code);
					}
					break;
				}
			}
		}
		bool haveText = components.Count > 0;
		if (killCreatures.Count > 0)
		{
			AddHeading(components, capi, "Obtained by killing", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			RichTextComponent comp3 = new RichTextComponent(capi, string.Join(", ", killCreatures) + "\n", CairoFont.WhiteSmallText());
			comp3.PaddingLeft = 2.0;
			components.Add(comp3);
		}
		if (harvestCreatures.Count > 0)
		{
			AddHeading(components, capi, "handbook-obtainedby-killing-harvesting", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			RichTextComponent comp2 = new RichTextComponent(capi, string.Join(", ", harvestCreatures) + "\n", CairoFont.WhiteSmallText());
			comp2.PaddingLeft = 2.0;
			components.Add(comp2);
		}
		if (breakBlocks.Count > 0)
		{
			AddHeading(components, capi, "Obtained by breaking", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			while (breakBlocks.Count > 0)
			{
				ItemStack dstack = breakBlocks[0];
				breakBlocks.RemoveAt(0);
				if (dstack != null)
				{
					SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, breakBlocks, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					components.Add(comp);
				}
			}
			components.Add(new ClearFloatTextComponent(capi, 2f));
		}
		return haveText;
	}

	protected bool addFoundInInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, bool haveText)
	{
		string customFoundIn = stack.Collectible.Attributes?["handbook"]?["foundIn"]?.AsString();
		if (customFoundIn != null)
		{
			AddHeading(components, capi, "Found in", ref haveText);
			RichTextComponent comp2 = new RichTextComponent(capi, Lang.Get(customFoundIn), CairoFont.WhiteSmallText());
			comp2.PaddingLeft = 2.0;
			components.Add(comp2);
		}
		JsonObject attributes = collObj.Attributes;
		if (attributes != null && attributes["hostRockFor"].Exists)
		{
			AddHeading(components, capi, "Host rock for", ref haveText);
			int[] blockids2 = collObj.Attributes?["hostRockFor"].AsArray<int>();
			OrderedDictionary<string, List<ItemStack>> blocks2 = new OrderedDictionary<string, List<ItemStack>>();
			for (int j = 0; j < blockids2.Length; j++)
			{
				Block block2 = capi.World.Blocks[blockids2[j]];
				string key2 = block2.Code.ToString();
				JsonObject attributes2 = block2.Attributes;
				if (attributes2 != null && attributes2["handbook"]["groupBy"].Exists)
				{
					key2 = block2.Attributes["handbook"]["groupBy"].AsArray<string>()[0];
				}
				if (!blocks2.ContainsKey(key2))
				{
					blocks2[key2] = new List<ItemStack>();
				}
				blocks2[key2].Add(new ItemStack(block2));
			}
			int firstPadding2 = 2;
			foreach (KeyValuePair<string, List<ItemStack>> item in blocks2)
			{
				SlideshowItemstackTextComponent comp3 = new SlideshowItemstackTextComponent(capi, item.Value.ToArray(), 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				comp3.PaddingLeft = firstPadding2;
				firstPadding2 = 0;
				components.Add(comp3);
			}
		}
		JsonObject attributes3 = collObj.Attributes;
		if (attributes3 != null && attributes3["hostRock"].Exists)
		{
			AddHeading(components, capi, "Occurs in host rock", ref haveText);
			ushort[] blockids = collObj.Attributes?["hostRock"].AsArray<ushort>();
			OrderedDictionary<string, List<ItemStack>> blocks = new OrderedDictionary<string, List<ItemStack>>();
			for (int i = 0; i < blockids.Length; i++)
			{
				Block block = capi.World.Blocks[blockids[i]];
				string key = block.Code.ToString();
				JsonObject attributes4 = block.Attributes;
				if (attributes4 != null && attributes4["handbook"]["groupBy"].Exists)
				{
					key = block.Attributes["handbook"]["groupBy"].AsArray<string>()[0];
				}
				if (!blocks.ContainsKey(key))
				{
					blocks[key] = new List<ItemStack>();
				}
				blocks[key].Add(new ItemStack(block));
			}
			int firstPadding = 2;
			foreach (KeyValuePair<string, List<ItemStack>> item2 in blocks)
			{
				SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, item2.Value.ToArray(), 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				comp.PaddingLeft = firstPadding;
				firstPadding = 0;
				components.Add(comp);
			}
		}
		return haveText;
	}

	protected static bool addAlloyForInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, bool haveText)
	{
		Dictionary<AssetLocation, ItemStack> alloyables = new Dictionary<AssetLocation, ItemStack>();
		foreach (AlloyRecipe val in capi.GetMetalAlloys())
		{
			MetalAlloyIngredient[] ingredients = val.Ingredients;
			for (int i = 0; i < ingredients.Length; i++)
			{
				if (ingredients[i].ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
				{
					alloyables[val.Output.ResolvedItemstack.Collectible.Code] = val.Output.ResolvedItemstack;
				}
			}
		}
		if (alloyables.Count > 0)
		{
			AddHeading(components, capi, "Alloy for", ref haveText);
			int firstPadding = 2;
			foreach (KeyValuePair<AssetLocation, ItemStack> item in alloyables)
			{
				ItemstackTextComponent comp = new ItemstackTextComponent(capi, item.Value, 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				comp.PaddingLeft = firstPadding;
				firstPadding = 0;
				components.Add(comp);
			}
			haveText = true;
		}
		return haveText;
	}

	protected bool addAlloyedFromInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, bool haveText)
	{
		Dictionary<AssetLocation, MetalAlloyIngredient[]> alloyableFrom = new Dictionary<AssetLocation, MetalAlloyIngredient[]>();
		foreach (AlloyRecipe val in capi.GetMetalAlloys())
		{
			if (val.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				List<MetalAlloyIngredient> ingreds = new List<MetalAlloyIngredient>();
				MetalAlloyIngredient[] ingredients = val.Ingredients;
				foreach (MetalAlloyIngredient ing in ingredients)
				{
					ingreds.Add(ing);
				}
				alloyableFrom[val.Output.ResolvedItemstack.Collectible.Code] = ingreds.ToArray();
			}
		}
		if (alloyableFrom.Count > 0)
		{
			AddHeading(components, capi, "Alloyed from", ref haveText);
			int firstPadding = 2;
			foreach (KeyValuePair<AssetLocation, MetalAlloyIngredient[]> item in alloyableFrom)
			{
				MetalAlloyIngredient[] ingredients = item.Value;
				foreach (MetalAlloyIngredient ingred in ingredients)
				{
					string ratio = " " + Lang.Get("alloy-ratio-from-to", (int)(ingred.MinRatio * 100f), (int)(ingred.MaxRatio * 100f));
					components.Add(new RichTextComponent(capi, ratio, CairoFont.WhiteSmallText()));
					ItemstackComponentBase comp = new ItemstackTextComponent(capi, ingred.ResolvedItemstack, 30.0, 5.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					comp.offY = GuiElement.scaled(7.0);
					comp.PaddingLeft = firstPadding;
					firstPadding = 0;
					components.Add(comp);
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
		}
		return haveText;
	}

	protected bool addProcessesIntoInfo(ItemSlot inSlot, ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, float marginBottom, bool haveText)
	{
		BakingProperties bp = collObj.Attributes?["bakingProperties"]?.AsObject<BakingProperties>();
		if (bp != null && bp.ResultCode != null)
		{
			Item item = capi.World.GetItem(new AssetLocation(bp.ResultCode));
			if (item != null)
			{
				AddHeading(components, capi, "smeltdesc-bake-title", ref haveText);
				ItemstackTextComponent cmp6 = new ItemstackTextComponent(capi, new ItemStack(item), 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				cmp6.ShowStacksize = true;
				cmp6.PaddingLeft = 2.0;
				components.Add(cmp6);
				components.Add(new ClearFloatTextComponent(capi, marginBottom));
			}
		}
		else if (collObj.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null && !collObj.CombustibleProps.SmeltedStack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
		{
			string smelttype = collObj.CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
			AddHeading(components, capi, "game:smeltdesc-" + smelttype + "-title", ref haveText);
			ItemstackTextComponent cmp5 = new ItemstackTextComponent(capi, collObj.CombustibleProps.SmeltedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			cmp5.ShowStacksize = true;
			cmp5.PaddingLeft = 2.0;
			components.Add(cmp5);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		JsonObject attributes = collObj.Attributes;
		if (attributes != null && attributes["beehivekiln"].Exists)
		{
			Dictionary<string, JsonItemStack> dictionary = collObj.Attributes["beehivekiln"].AsObject<Dictionary<string, JsonItemStack>>();
			components.Add(new ClearFloatTextComponent(capi, 7f));
			components.Add(new RichTextComponent(capi, Lang.Get("game:smeltdesc-beehivekiln-title") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
			foreach (var (doorOpen, firesIntoStack) in dictionary)
			{
				if (firesIntoStack != null && firesIntoStack.Resolve(capi.World, "beehivekiln-burn"))
				{
					components.Add(new ItemstackTextComponent(capi, firesIntoStack.ResolvedItemstack.Clone(), 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					}));
					components.Add(new RichTextComponent(capi, Lang.Get("smeltdesc-beehivekiln-opendoors", doorOpen), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold))
					{
						VerticalAlign = EnumVerticalAlign.Middle
					});
					components.Add(new ItemstackTextComponent(capi, new ItemStack(capi.World.GetBlock("cokeovendoor-closed-north")), 40.0, 0.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					}));
					components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText())
					{
						VerticalAlign = EnumVerticalAlign.Middle
					});
				}
			}
		}
		if (collObj.CrushingProps?.CrushedStack?.ResolvedItemstack != null && !collObj.CrushingProps.CrushedStack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
		{
			AddHeading(components, capi, "pulverizesdesc-title", ref haveText);
			ItemstackTextComponent cmp4 = new ItemstackTextComponent(capi, collObj.CrushingProps.CrushedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			cmp4.ShowStacksize = true;
			cmp4.PaddingLeft = 2.0;
			components.Add(cmp4);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		if (collObj.GrindingProps?.GroundStack?.ResolvedItemstack != null && !collObj.GrindingProps.GroundStack.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
		{
			AddHeading(components, capi, "Grinds into", ref haveText);
			ItemstackTextComponent cmp3 = new ItemstackTextComponent(capi, collObj.GrindingProps.GroundStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			cmp3.ShowStacksize = true;
			cmp3.PaddingLeft = 2.0;
			components.Add(cmp3);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		JuiceableProperties jprops = getjuiceableProps(inSlot.Itemstack);
		if (jprops != null)
		{
			AddHeading(components, capi, "Juices into", ref haveText);
			ItemStack jstack = jprops.LiquidStack.ResolvedItemstack.Clone();
			if (jprops.LitresPerItem.HasValue)
			{
				jstack.StackSize = (int)(100f * jprops.LitresPerItem).Value;
			}
			ItemstackTextComponent cmp2 = new ItemstackTextComponent(capi, jstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			cmp2.ShowStacksize = jprops.LitresPerItem.HasValue;
			cmp2.PaddingLeft = 2.0;
			components.Add(cmp2);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		DistillationProps dprops = getDistillationProps(inSlot.Itemstack);
		if (dprops != null)
		{
			AddHeading(components, capi, "One liter distills into", ref haveText);
			ItemStack dstack = dprops.DistilledStack?.ResolvedItemstack.Clone();
			if (dprops.Ratio != 0f)
			{
				dstack.StackSize = (int)((float)(100 * inSlot.Itemstack.StackSize) * dprops.Ratio);
			}
			ItemstackTextComponent cmp = new ItemstackTextComponent(capi, dstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
			{
				openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
			});
			cmp.ShowStacksize = dprops.Ratio != 0f;
			cmp.PaddingLeft = 2.0;
			components.Add(cmp);
			components.Add(new ClearFloatTextComponent(capi, marginBottom));
		}
		TransitionableProperties[] props = collObj.GetTransitionableProperties(capi.World, stack, null);
		if (props != null)
		{
			haveText = true;
			ClearFloatTextComponent verticalSpace = new ClearFloatTextComponent(capi, 14f);
			bool addedItemStack = false;
			TransitionableProperties[] array = props;
			foreach (TransitionableProperties prop in array)
			{
				switch (prop.Type)
				{
				case EnumTransitionType.Cure:
				{
					components.Add(verticalSpace);
					addedItemStack = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours, cures into", prop.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
					ItemstackTextComponent compc = new ItemstackTextComponent(capi, prop.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					compc.PaddingLeft = 2.0;
					components.Add(compc);
					break;
				}
				case EnumTransitionType.Ripen:
				{
					components.Add(verticalSpace);
					addedItemStack = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours of open storage, ripens into", prop.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
					ItemstackTextComponent compr = new ItemstackTextComponent(capi, prop.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					compr.PaddingLeft = 2.0;
					components.Add(compr);
					break;
				}
				case EnumTransitionType.Dry:
				{
					components.Add(verticalSpace);
					addedItemStack = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours of open storage, dries into", prop.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
					ItemstackTextComponent compd = new ItemstackTextComponent(capi, prop.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					compd.PaddingLeft = 2.0;
					components.Add(compd);
					break;
				}
				case EnumTransitionType.Melt:
				{
					components.Add(verticalSpace);
					addedItemStack = true;
					components.Add(new RichTextComponent(capi, Lang.Get("After {0} hours of open storage, melts into", prop.TransitionHours.avg) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
					ItemstackTextComponent compm = new ItemstackTextComponent(capi, prop.TransitionedStack.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					compm.PaddingLeft = 2.0;
					components.Add(compm);
					break;
				}
				}
			}
			if (addedItemStack)
			{
				components.Add(new ClearFloatTextComponent(capi, marginBottom));
			}
		}
		return haveText;
	}

	protected bool addIngredientForInfo(ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, bool haveText)
	{
		ItemStack maxstack = stack.Clone();
		maxstack.StackSize = maxstack.Collectible.MaxStackSize * 10;
		List<ItemStack> recipestacks = new List<ItemStack>();
		foreach (GridRecipe recval in capi.World.GridRecipes)
		{
			GridRecipeIngredient[] resolvedIngredients = recval.resolvedIngredients;
			foreach (GridRecipeIngredient val3 in resolvedIngredients)
			{
				CraftingRecipeIngredient ingred = val3;
				if (ingred == null || !ingred.SatisfiesAsIngredient(maxstack) || recipestacks.Any((ItemStack s) => s.Equals(capi.World, recval.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
				{
					continue;
				}
				DummySlot outSlot = new DummySlot();
				DummySlot[] inSlots = new DummySlot[recval.Width * recval.Height];
				for (int x = 0; x < recval.Width; x++)
				{
					for (int y = 0; y < recval.Height; y++)
					{
						GridRecipeIngredient elementInGrid = recval.GetElementInGrid(y, x, recval.resolvedIngredients, recval.Width);
						ItemStack ingredStack = elementInGrid?.ResolvedItemstack?.Clone();
						if (elementInGrid == val3)
						{
							ingredStack = maxstack;
						}
						inSlots[y * recval.Width + x] = new DummySlot(ingredStack);
					}
				}
				GridRecipe gridRecipe = recval;
				ItemSlot[] inputSlots = inSlots;
				gridRecipe.GenerateOutputStack(inputSlots, outSlot);
				recipestacks.Add(outSlot.Itemstack);
			}
		}
		foreach (SmithingRecipe val4 in capi.GetSmithingRecipes())
		{
			if (val4.Ingredient.SatisfiesAsIngredient(maxstack) && !recipestacks.Any((ItemStack s) => s.Equals(capi.World, val4.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
			{
				recipestacks.Add(val4.Output.ResolvedItemstack);
			}
		}
		foreach (ClayFormingRecipe val2 in capi.GetClayformingRecipes())
		{
			if (val2.Ingredient.SatisfiesAsIngredient(maxstack) && !recipestacks.Any((ItemStack s) => s.Equals(capi.World, val2.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
			{
				recipestacks.Add(val2.Output.ResolvedItemstack);
			}
		}
		foreach (KnappingRecipe val in capi.GetKnappingRecipes())
		{
			if (val.Ingredient.SatisfiesAsIngredient(maxstack) && !recipestacks.Any((ItemStack s) => s.Equals(capi.World, val.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
			{
				recipestacks.Add(val.Output.ResolvedItemstack);
			}
		}
		foreach (BarrelRecipe recipe in capi.GetBarrelRecipes())
		{
			BarrelRecipeIngredient[] ingredients = recipe.Ingredients;
			for (int i = 0; i < ingredients.Length; i++)
			{
				if (ingredients[i].SatisfiesAsIngredient(maxstack) && !recipestacks.Any((ItemStack s) => s.Equals(capi.World, recipe.Output.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
				{
					recipestacks.Add(recipe.Output.ResolvedItemstack);
				}
			}
		}
		foreach (CookingRecipe recipe2 in capi.GetCookingRecipes())
		{
			if (recipe2.CooksInto?.ResolvedItemstack == null)
			{
				continue;
			}
			CookingRecipeIngredient[] ingredients2 = recipe2.Ingredients;
			for (int i = 0; i < ingredients2.Length; i++)
			{
				if (ingredients2[i].GetMatchingStack(stack) != null)
				{
					recipestacks.Add(recipe2.CooksInto.ResolvedItemstack);
				}
			}
		}
		if (recipestacks.Count > 0)
		{
			AddHeading(components, capi, "Ingredient for", ref haveText);
			components.Add(new ClearFloatTextComponent(capi, 2f));
			while (recipestacks.Count > 0)
			{
				ItemStack dstack = recipestacks[0];
				recipestacks.RemoveAt(0);
				if (dstack != null)
				{
					SlideshowItemstackTextComponent comp = new SlideshowItemstackTextComponent(capi, dstack, recipestacks, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					components.Add(comp);
				}
			}
			components.Add(new ClearFloatTextComponent(capi, 3f));
		}
		return haveText;
	}

	protected bool addCreatedByInfo(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, bool haveText)
	{
		bool smithable = false;
		bool knappable = false;
		bool clayformable = false;
		foreach (SmithingRecipe smithingRecipe in capi.GetSmithingRecipes())
		{
			if (smithingRecipe.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				smithable = true;
				break;
			}
		}
		foreach (KnappingRecipe knappingRecipe in capi.GetKnappingRecipes())
		{
			if (knappingRecipe.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				knappable = true;
				break;
			}
		}
		foreach (ClayFormingRecipe clayformingRecipe in capi.GetClayformingRecipes())
		{
			if (clayformingRecipe.Output.ResolvedItemstack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				clayformable = true;
				break;
			}
		}
		List<GridRecipe> grecipes = new List<GridRecipe>();
		foreach (GridRecipe val3 in capi.World.GridRecipes)
		{
			if (val3.ShowInCreatedBy)
			{
				ItemStack resolvedItemstack = val3.Output.ResolvedItemstack;
				if (resolvedItemstack != null && resolvedItemstack.Satisfies(stack))
				{
					grecipes.Add(val3);
				}
			}
		}
		List<CookingRecipe> cookrecipes = new List<CookingRecipe>();
		foreach (CookingRecipe val4 in capi.GetCookingRecipes())
		{
			if ((val4.CooksInto?.ResolvedItemstack?.Satisfies(stack)).GetValueOrDefault())
			{
				cookrecipes.Add(val4);
			}
		}
		List<ItemStack> bakables = new List<ItemStack>();
		List<ItemStack> grindables = new List<ItemStack>();
		List<ItemStack> crushables = new List<ItemStack>();
		List<ItemStack> curables = new List<ItemStack>();
		List<ItemStack> ripenables = new List<ItemStack>();
		List<ItemStack> dryables = new List<ItemStack>();
		List<ItemStack> meltables = new List<ItemStack>();
		List<ItemStack> juiceables = new List<ItemStack>();
		List<ItemStack> distillables = new List<ItemStack>();
		foreach (ItemStack val2 in allStacks)
		{
			ItemStack smeltedStack = val2.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
			if (smeltedStack != null && smeltedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !bakables.Any((ItemStack s) => s.Equals(capi.World, smeltedStack, GlobalConstants.IgnoredStackAttributes)))
			{
				bakables.Add(val2);
			}
			ItemStack groundStack = val2.Collectible.GrindingProps?.GroundStack.ResolvedItemstack;
			if (groundStack != null && groundStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !grindables.Any((ItemStack s) => s.Equals(capi.World, groundStack, GlobalConstants.IgnoredStackAttributes)))
			{
				grindables.Add(val2);
			}
			ItemStack crushedStack = val2.Collectible.CrushingProps?.CrushedStack.ResolvedItemstack;
			if (crushedStack != null && crushedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !crushables.Any((ItemStack s) => s.Equals(capi.World, crushedStack, GlobalConstants.IgnoredStackAttributes)))
			{
				crushables.Add(val2);
			}
			JsonObject itemAttributes = val2.ItemAttributes;
			if (itemAttributes != null && itemAttributes["juiceableProperties"].Exists)
			{
				ItemStack juicedStack = getjuiceableProps(val2)?.LiquidStack?.ResolvedItemstack;
				if (juicedStack != null && juicedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !juiceables.Any((ItemStack s) => s.Equals(capi.World, val2, GlobalConstants.IgnoredStackAttributes)))
				{
					juiceables.Add(val2);
				}
			}
			JsonObject itemAttributes2 = val2.ItemAttributes;
			if (itemAttributes2 != null && itemAttributes2["distillationProps"].Exists)
			{
				ItemStack distilledStack = getDistillationProps(val2)?.DistilledStack?.ResolvedItemstack;
				if (distilledStack != null && distilledStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !distillables.Any((ItemStack s) => s.Equals(capi.World, val2, GlobalConstants.IgnoredStackAttributes)))
				{
					distillables.Add(val2);
				}
			}
			TransitionableProperties[] oprops = val2.Collectible.GetTransitionableProperties(capi.World, val2, null);
			if (oprops == null)
			{
				continue;
			}
			TransitionableProperties[] array = oprops;
			foreach (TransitionableProperties prop in array)
			{
				ItemStack transitionedStack = prop.TransitionedStack?.ResolvedItemstack;
				switch (prop.Type)
				{
				case EnumTransitionType.Cure:
					if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !curables.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
					{
						curables.Add(val2);
					}
					break;
				case EnumTransitionType.Ripen:
					if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !curables.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
					{
						ripenables.Add(val2);
					}
					break;
				case EnumTransitionType.Dry:
					if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !curables.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
					{
						dryables.Add(val2);
					}
					break;
				case EnumTransitionType.Melt:
					if (transitionedStack != null && transitionedStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes) && !curables.Any((ItemStack s) => s.Equals(capi.World, transitionedStack, GlobalConstants.IgnoredStackAttributes)))
					{
						meltables.Add(val2);
					}
					break;
				}
			}
		}
		List<RichTextComponentBase> barrelRecipestext = BuildBarrelRecipesText(capi, stack, openDetailPageFor);
		string customCreatedBy = stack.Collectible.Attributes?["handbook"]?["createdBy"]?.AsString();
		string bakingInitialIngredient = collObj.Attributes?["bakingProperties"]?.AsObject<BakingProperties>()?.InitialCode;
		if (grecipes.Count > 0 || cookrecipes.Count > 0 || smithable || knappable || clayformable || customCreatedBy != null || bakables.Count > 0 || barrelRecipestext.Count > 0 || grindables.Count > 0 || curables.Count > 0 || ripenables.Count > 0 || dryables.Count > 0 || meltables.Count > 0 || crushables.Count > 0 || bakingInitialIngredient != null || juiceables.Count > 0 || distillables.Count > 0)
		{
			AddHeading(components, capi, "Created by", ref haveText);
			ClearFloatTextComponent verticalSpaceSmall = new ClearFloatTextComponent(capi, 7f);
			ClearFloatTextComponent verticalSpace = new ClearFloatTextComponent(capi, 3f);
			if (smithable)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Smithing", "craftinginfo-smithing");
			}
			if (knappable)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Knapping", "craftinginfo-knapping");
			}
			if (clayformable)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Clay forming", "craftinginfo-clayforming");
			}
			if (customCreatedBy != null)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				RichTextComponent comp13 = new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText());
				comp13.PaddingLeft = 2.0;
				components.Add(comp13);
				components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(customCreatedBy) + "\n", CairoFont.WhiteSmallText()));
			}
			if (grindables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Grinding", null);
				int firstPadding9 = 2;
				while (grindables.Count > 0)
				{
					ItemStack dstack9 = grindables[0];
					grindables.RemoveAt(0);
					if (dstack9 != null)
					{
						SlideshowItemstackTextComponent comp12 = new SlideshowItemstackTextComponent(capi, dstack9, grindables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp12.ShowStackSize = true;
						comp12.PaddingLeft = firstPadding9;
						firstPadding9 = 0;
						components.Add(comp12);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (crushables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Crushing", null);
				int firstPadding8 = 2;
				while (crushables.Count > 0)
				{
					ItemStack dstack8 = crushables[0];
					crushables.RemoveAt(0);
					if (dstack8 != null)
					{
						SlideshowItemstackTextComponent comp11 = new SlideshowItemstackTextComponent(capi, dstack8, crushables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp11.ShowStackSize = true;
						comp11.PaddingLeft = firstPadding8;
						firstPadding8 = 0;
						components.Add(comp11);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (curables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Curing", null);
				int firstPadding7 = 2;
				while (curables.Count > 0)
				{
					ItemStack dstack7 = curables[0];
					curables.RemoveAt(0);
					if (dstack7 != null)
					{
						SlideshowItemstackTextComponent comp10 = new SlideshowItemstackTextComponent(capi, dstack7, curables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp10.PaddingLeft = firstPadding7;
						firstPadding7 = 0;
						components.Add(comp10);
					}
				}
			}
			if (ripenables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Ripening", null);
				int firstPadding6 = 2;
				while (ripenables.Count > 0)
				{
					ItemStack dstack6 = ripenables[0];
					ripenables.RemoveAt(0);
					if (dstack6 != null)
					{
						SlideshowItemstackTextComponent comp9 = new SlideshowItemstackTextComponent(capi, dstack6, ripenables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp9.PaddingLeft = firstPadding6;
						firstPadding6 = 0;
						components.Add(comp9);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (dryables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Drying", null);
				int firstPadding5 = 2;
				while (dryables.Count > 0)
				{
					ItemStack dstack5 = dryables[0];
					dryables.RemoveAt(0);
					if (dstack5 != null)
					{
						SlideshowItemstackTextComponent comp8 = new SlideshowItemstackTextComponent(capi, dstack5, dryables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp8.PaddingLeft = firstPadding5;
						firstPadding5 = 0;
						components.Add(comp8);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (meltables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Melting", null);
				int firstPadding4 = 2;
				while (meltables.Count > 0)
				{
					ItemStack dstack4 = meltables[0];
					meltables.RemoveAt(0);
					if (dstack4 != null)
					{
						SlideshowItemstackTextComponent comp7 = new SlideshowItemstackTextComponent(capi, dstack4, meltables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp7.PaddingLeft = firstPadding4;
						firstPadding4 = 0;
						components.Add(comp7);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (bakables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Cooking/Smelting/Baking", null);
				int firstPadding3 = 2;
				while (bakables.Count > 0)
				{
					ItemStack dstack3 = bakables[0];
					bakables.RemoveAt(0);
					if (dstack3 != null)
					{
						SlideshowItemstackTextComponent comp6 = new SlideshowItemstackTextComponent(capi, dstack3, bakables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp6.PaddingLeft = firstPadding3;
						firstPadding3 = 0;
						components.Add(comp6);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (juiceables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Juicing", null);
				int firstPadding2 = 2;
				while (juiceables.Count > 0)
				{
					ItemStack dstack2 = juiceables[0];
					juiceables.RemoveAt(0);
					if (dstack2 != null)
					{
						SlideshowItemstackTextComponent comp5 = new SlideshowItemstackTextComponent(capi, dstack2, juiceables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp5.PaddingLeft = firstPadding2;
						firstPadding2 = 0;
						components.Add(comp5);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (distillables.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Distillation", null);
				int firstPadding = 2;
				while (distillables.Count > 0)
				{
					ItemStack dstack = distillables[0];
					distillables.RemoveAt(0);
					if (dstack != null)
					{
						SlideshowItemstackTextComponent comp4 = new SlideshowItemstackTextComponent(capi, dstack, distillables, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
						{
							openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
						});
						comp4.PaddingLeft = firstPadding;
						firstPadding = 0;
						components.Add(comp4);
					}
				}
				components.Add(new RichTextComponent(capi, "\n", CairoFont.WhiteSmallText()));
			}
			if (bakingInitialIngredient != null)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Baking (in oven)", null);
				CollectibleObject cobj = capi.World.GetItem(new AssetLocation(bakingInitialIngredient));
				if (cobj == null)
				{
					cobj = capi.World.GetBlock(new AssetLocation(bakingInitialIngredient));
				}
				ItemstackTextComponent comp3 = new ItemstackTextComponent(capi, new ItemStack(cobj), 40.0, 10.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				comp3.PaddingLeft = 2.0;
				components.Add(comp3);
			}
			if (cookrecipes.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Cooking (in pot)", null);
				foreach (CookingRecipe rec2 in cookrecipes)
				{
					int firstIndent = 2;
					for (int j = 0; j < rec2.Ingredients.Length; j++)
					{
						CookingRecipeIngredient ingred = rec2.Ingredients[j];
						if (j > 0)
						{
							RichTextComponent cmp2 = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
							cmp2.VerticalAlign = EnumVerticalAlign.Middle;
							components.Add(cmp2);
						}
						ItemStack[] stacks = ingred.ValidStacks.Select(delegate(CookingRecipeStack vs)
						{
							ItemStack itemStack = vs.ResolvedItemstack.Clone();
							JsonObject attributes = itemStack.Collectible.Attributes;
							if (attributes != null && attributes["waterTightContainerProps"].Exists)
							{
								WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(itemStack);
								itemStack.StackSize = (int)(containableProps.ItemsPerLitre * ingred.PortionSizeLitres);
							}
							else
							{
								itemStack.StackSize = 1;
							}
							return itemStack;
						}).ToArray();
						for (int l = 0; l < ingred.MinQuantity; l++)
						{
							if (l > 0)
							{
								RichTextComponent cmp = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
								cmp.VerticalAlign = EnumVerticalAlign.Middle;
								components.Add(cmp);
							}
							SlideshowItemstackTextComponent comp2 = new SlideshowItemstackTextComponent(capi, stacks, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
							{
								openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
							});
							comp2.ShowStackSize = true;
							comp2.PaddingLeft = firstIndent;
							components.Add(comp2);
							firstIndent = 0;
						}
					}
					RichTextComponent eqcomp = new RichTextComponent(capi, " = ", CairoFont.WhiteMediumText());
					eqcomp.VerticalAlign = EnumVerticalAlign.Middle;
					components.Add(eqcomp);
					ItemstackTextComponent ocmp = new ItemstackTextComponent(capi, rec2.CooksInto.ResolvedItemstack, 40.0, 10.0, EnumFloat.Inline);
					ocmp.ShowStacksize = true;
					components.Add(ocmp);
					components.Add(new ClearFloatTextComponent(capi, 10f));
				}
			}
			if (grecipes.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Crafting", null);
				OrderedDictionary<int, List<GridRecipe>> grouped = new OrderedDictionary<int, List<GridRecipe>>();
				ItemStack[] outputStacks = new ItemStack[grecipes.Count];
				int i = 0;
				foreach (GridRecipe recipe in grecipes)
				{
					if (!grouped.TryGetValue(recipe.RecipeGroup, out var list))
					{
						list = (grouped[recipe.RecipeGroup] = new List<GridRecipe>());
					}
					if (recipe.CopyAttributesFrom != null && recipe.Ingredients.ContainsKey(recipe.CopyAttributesFrom))
					{
						GridRecipe rec = recipe.Clone();
						CraftingRecipeIngredient ingred2 = rec.Ingredients[recipe.CopyAttributesFrom];
						ITreeAttribute cattr = stack.Attributes.Clone();
						cattr.MergeTree(ingred2.ResolvedItemstack.Attributes);
						ingred2.Attributes = new JsonObject(JToken.Parse(cattr.ToJsonToken()));
						rec.ResolveIngredients(capi.World);
						rec.Output.ResolvedItemstack.Attributes.MergeTree(stack.Attributes);
						list.Add(rec);
						outputStacks[i++] = rec.Output.ResolvedItemstack;
					}
					else
					{
						list.Add(recipe);
						outputStacks[i++] = recipe.Output.ResolvedItemstack;
					}
				}
				int k = 0;
				foreach (KeyValuePair<int, List<GridRecipe>> val in grouped)
				{
					if (k++ % 2 == 0)
					{
						components.Add(verticalSpaceSmall);
					}
					SlideshowGridRecipeTextComponent comp = new SlideshowGridRecipeTextComponent(capi, val.Value.ToArray(), 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					}, allStacks);
					comp.VerticalAlign = EnumVerticalAlign.Top;
					comp.PaddingRight = 8.0;
					comp.UnscaledMarginTop = 8.0;
					comp.PaddingLeft = 4 + (1 - k % 2) * 20;
					components.Add(comp);
					RichTextComponent ecomp = new RichTextComponent(capi, "=", CairoFont.WhiteMediumText());
					ecomp.VerticalAlign = EnumVerticalAlign.FixedOffset;
					ecomp.UnscaledMarginTop = 51.0;
					ecomp.PaddingRight = 5.0;
					SlideshowItemstackTextComponent ocomp = new SlideshowItemstackTextComponent(capi, outputStacks, 40.0, EnumFloat.Inline, delegate(ItemStack cs)
					{
						openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
					});
					ocomp.overrideCurrentItemStack = () => comp.CurrentVisibleRecipe.Recipe.Output.ResolvedItemstack;
					ocomp.VerticalAlign = EnumVerticalAlign.FixedOffset;
					ocomp.UnscaledMarginTop = 50.0;
					ocomp.ShowStackSize = true;
					components.Add(ecomp);
					components.Add(ocomp);
				}
				components.Add(new ClearFloatTextComponent(capi, 3f));
			}
			if (barrelRecipestext.Count > 0)
			{
				components.Add(verticalSpace);
				verticalSpace = verticalSpaceSmall;
				AddSubHeading(components, capi, openDetailPageFor, "Mixing (in Barrel)", null);
				components.AddRange(barrelRecipestext);
			}
		}
		return haveText;
	}

	protected static void AddHeading(List<RichTextComponentBase> components, ICoreClientAPI capi, string heading, ref bool haveText)
	{
		if (haveText)
		{
			components.Add(new ClearFloatTextComponent(capi, 14f));
		}
		haveText = true;
		RichTextComponent headc = new RichTextComponent(capi, Lang.Get(heading) + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold));
		components.Add(headc);
	}

	protected void AddSubHeading(List<RichTextComponentBase> components, ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor, string subheading, string detailpage)
	{
		if (detailpage == null)
		{
			RichTextComponent bullet = new RichTextComponent(capi, "• " + Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText());
			bullet.PaddingLeft = 2.0;
			components.Add(bullet);
			return;
		}
		RichTextComponent bullet2 = new RichTextComponent(capi, "• ", CairoFont.WhiteSmallText());
		bullet2.PaddingLeft = 2.0;
		components.Add(bullet2);
		components.Add(new LinkTextComponent(capi, Lang.Get(subheading) + "\n", CairoFont.WhiteSmallText(), delegate
		{
			openDetailPageFor(detailpage);
		}));
	}

	protected void addExtraSections(ICoreClientAPI capi, ItemStack stack, List<RichTextComponentBase> components, float marginTop)
	{
		if (ExtraHandBookSections != null)
		{
			bool haveText = true;
			for (int i = 0; i < ExtraHandBookSections.Length; i++)
			{
				ExtraHandbookSection extraSection = ExtraHandBookSections[i];
				AddHeading(components, capi, extraSection.Title, ref haveText);
				components.Add(new ClearFloatTextComponent(capi, 2f));
				RichTextComponent spacer2 = new RichTextComponent(capi, "", CairoFont.WhiteSmallText());
				spacer2.PaddingLeft = 2.0;
				components.Add(spacer2);
				if (extraSection.TextParts != null)
				{
					components.AddRange(VtmlUtil.Richtextify(capi, string.Join(", ", extraSection.TextParts) + "\n", CairoFont.WhiteSmallText()));
				}
				else
				{
					components.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(extraSection.Text) + "\n", CairoFont.WhiteSmallText()));
				}
			}
		}
		string text = collObj.Code.Domain + ":" + stack.Class.Name();
		string code = collObj.Code.ToShortString();
		string langExtraSectionTitle = Lang.GetMatchingIfExists(text + "-handbooktitle-" + code);
		string langExtraSectionText = Lang.GetMatchingIfExists(text + "-handbooktext-" + code);
		if (langExtraSectionTitle != null || langExtraSectionText != null)
		{
			components.Add(new ClearFloatTextComponent(capi, 14f));
			if (langExtraSectionTitle != null)
			{
				components.Add(new RichTextComponent(capi, langExtraSectionTitle + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
				components.Add(new ClearFloatTextComponent(capi, 2f));
			}
			if (langExtraSectionText != null)
			{
				RichTextComponent spacer = new RichTextComponent(capi, "", CairoFont.WhiteSmallText());
				spacer.PaddingLeft = 2.0;
				components.Add(spacer);
				components.AddRange(VtmlUtil.Richtextify(capi, langExtraSectionText + "\n", CairoFont.WhiteSmallText()));
			}
		}
	}

	protected List<RichTextComponentBase> BuildBarrelRecipesText(ICoreClientAPI capi, ItemStack stack, ActionConsumable<string> openDetailPageFor)
	{
		List<RichTextComponentBase> barrelRecipesTexts = new List<RichTextComponentBase>();
		List<BarrelRecipe> barrelRecipes = capi.GetBarrelRecipes();
		if (barrelRecipes.Count == 0)
		{
			return barrelRecipesTexts;
		}
		Dictionary<string, List<BarrelRecipe>> brecipesbyCode = new Dictionary<string, List<BarrelRecipe>>();
		foreach (BarrelRecipe recipe in barrelRecipes)
		{
			ItemStack mixdStack = recipe.Output.ResolvedItemstack;
			if (mixdStack != null && mixdStack.Equals(capi.World, stack, GlobalConstants.IgnoredStackAttributes))
			{
				if (!brecipesbyCode.TryGetValue(recipe.Code, out var tmp))
				{
					tmp = (brecipesbyCode[recipe.Code] = new List<BarrelRecipe>());
				}
				tmp.Add(recipe);
			}
		}
		foreach (List<BarrelRecipe> recipes in brecipesbyCode.Values)
		{
			int ingredientsLen = recipes[0].Ingredients.Length;
			ItemStack[][] ingstacks = new ItemStack[ingredientsLen][];
			ItemStack[] outstacks = new ItemStack[recipes.Count];
			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Ingredients.Length != ingredientsLen)
				{
					throw new Exception("Barrel recipe with same name but different ingredient count! Sorry, this is not supported right now. Please make sure you choose different barrel recipe names if you have different ingredient counts.");
				}
				for (int k = 0; k < ingredientsLen; k++)
				{
					if (j == 0)
					{
						ingstacks[k] = new ItemStack[recipes.Count];
					}
					ingstacks[k][j] = recipes[j].Ingredients[k].ResolvedItemstack;
				}
				outstacks[j] = recipes[j].Output.ResolvedItemstack;
			}
			int firstIndent = 2;
			for (int i = 0; i < ingredientsLen; i++)
			{
				if (i > 0)
				{
					RichTextComponent cmp = new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText());
					cmp.VerticalAlign = EnumVerticalAlign.Middle;
					barrelRecipesTexts.Add(cmp);
				}
				SlideshowItemstackTextComponent scmp = new SlideshowItemstackTextComponent(capi, ingstacks[i], 40.0, EnumFloat.Inline, delegate(ItemStack cs)
				{
					openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs));
				});
				scmp.ShowStackSize = true;
				scmp.PaddingLeft = firstIndent;
				firstIndent = 0;
				barrelRecipesTexts.Add(scmp);
			}
			RichTextComponent eqcomp = new RichTextComponent(capi, " = ", CairoFont.WhiteMediumText());
			eqcomp.VerticalAlign = EnumVerticalAlign.Middle;
			barrelRecipesTexts.Add(eqcomp);
			SlideshowItemstackTextComponent ocmp = new SlideshowItemstackTextComponent(capi, outstacks, 40.0, EnumFloat.Inline);
			ocmp.ShowStackSize = true;
			barrelRecipesTexts.Add(ocmp);
			barrelRecipesTexts.Add(new ClearFloatTextComponent(capi, 10f));
		}
		return barrelRecipesTexts;
	}

	protected void addStorableInfo(ICoreClientAPI capi, ItemStack stack, List<RichTextComponentBase> components, float marginTop)
	{
		List<RichTextComponentBase> storableComps = new List<RichTextComponentBase>();
		JsonObject itemAttributes = stack.ItemAttributes;
		if (itemAttributes != null && itemAttributes.IsTrue("moldrackable"))
		{
			AddPaddingAndRichText(storableComps, capi, "handbook-storable-moldrack");
		}
		JsonObject itemAttributes2 = stack.ItemAttributes;
		if (itemAttributes2 != null && itemAttributes2.IsTrue("shelvable"))
		{
			AddPaddingAndRichText(storableComps, capi, "handbook-storable-shelves");
		}
		JsonObject itemAttributes3 = stack.ItemAttributes;
		if (itemAttributes3 != null && itemAttributes3.IsTrue("displaycaseable"))
		{
			AddPaddingAndRichText(storableComps, capi, "handbook-storable-displaycase");
		}
		if (!stack.Collectible.Tool.HasValue)
		{
			JsonObject itemAttributes4 = stack.ItemAttributes;
			if (itemAttributes4 == null || !itemAttributes4["rackable"].AsBool())
			{
				goto IL_00b6;
			}
		}
		AddPaddingAndRichText(storableComps, capi, "handbook-storable-toolrack");
		goto IL_00b6;
		IL_00b6:
		if (stack.Collectible.HasBehavior<CollectibleBehaviorGroundStorable>())
		{
			AddPaddingAndRichText(storableComps, capi, "handbook-storable-ground");
		}
		JsonObject itemAttributes5 = stack.ItemAttributes;
		if (itemAttributes5 != null && itemAttributes5["waterTightContainerProps"].Exists)
		{
			AddPaddingAndRichText(storableComps, capi, "handbook-storable-barrel");
		}
		if (storableComps.Count > 0)
		{
			bool haveText = components.Count > 0;
			AddHeading(components, capi, "Storable in/on", ref haveText);
			components.AddRange(storableComps);
		}
	}

	private void AddPaddingAndRichText(List<RichTextComponentBase> storableComps, ICoreClientAPI capi, string text)
	{
		storableComps.Add(new ClearFloatTextComponent(capi, 2f));
		RichTextComponent spacer = new RichTextComponent(capi, "", CairoFont.WhiteSmallText());
		spacer.PaddingLeft = 2.0;
		storableComps.Add(spacer);
		storableComps.AddRange(VtmlUtil.Richtextify(capi, Lang.Get(text), CairoFont.WhiteSmallText()));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		JsonObject attributes = collObj.Attributes;
		if (attributes != null && (attributes["pigment"]?["color"].Exists).GetValueOrDefault())
		{
			dsc.AppendLine(Lang.Get("Pigment: {0}", Lang.Get(collObj.Attributes["pigment"]["name"].AsString())));
		}
		JsonObject obj = collObj.Attributes?["fertilizerProps"];
		if (obj != null && obj.Exists)
		{
			FertilizerProps fprops = obj.AsObject<FertilizerProps>();
			if (fprops != null)
			{
				dsc.AppendLine(Lang.Get("Fertilizer: {0}% N, {1}% P, {2}% K", fprops.N, fprops.P, fprops.K));
			}
		}
		JuiceableProperties jprops = getjuiceableProps(inSlot.Itemstack);
		if (jprops != null)
		{
			float litres = (jprops.LitresPerItem.HasValue ? (jprops.LitresPerItem.Value * (float)inSlot.Itemstack.StackSize) : ((float)inSlot.Itemstack.Attributes.GetDecimal("juiceableLitresLeft")));
			if ((double)litres > 0.01)
			{
				dsc.AppendLine(Lang.Get("collectibleinfo-juicingproperties", litres, jprops.LiquidStack.ResolvedItemstack.GetName()));
			}
		}
	}

	public JuiceableProperties getjuiceableProps(ItemStack stack)
	{
		JuiceableProperties obj = stack?.ItemAttributes?["juiceableProperties"]?.AsObject<JuiceableProperties>(null, stack.Collectible.Code.Domain);
		obj?.LiquidStack?.Resolve(Api.World, "juiceable properties liquidstack");
		if (obj != null)
		{
			JsonItemStack pressedStack = obj.PressedStack;
			if (pressedStack != null)
			{
				pressedStack.Resolve(Api.World, "juiceable properties pressedstack");
				return obj;
			}
			return obj;
		}
		return obj;
	}

	public DistillationProps getDistillationProps(ItemStack stack)
	{
		DistillationProps obj = stack?.ItemAttributes?["distillationProps"]?.AsObject<DistillationProps>(null, stack.Collectible.Code.Domain);
		if (obj != null)
		{
			JsonItemStack distilledStack = obj.DistilledStack;
			if (distilledStack != null)
			{
				distilledStack.Resolve(Api.World, "distillation props distilled stack");
				return obj;
			}
			return obj;
		}
		return obj;
	}
}
