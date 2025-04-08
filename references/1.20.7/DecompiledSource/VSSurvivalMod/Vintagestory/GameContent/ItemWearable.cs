using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemWearable : ItemWearableAttachment
{
	public StatModifiers StatModifers;

	public ProtectionModifiers ProtectionModifiers;

	public AssetLocation[] FootStepSounds;

	public EnumCharacterDressType DressType { get; private set; }

	public bool IsArmor
	{
		get
		{
			if (DressType != EnumCharacterDressType.ArmorBody && DressType != EnumCharacterDressType.ArmorHead)
			{
				return DressType == EnumCharacterDressType.ArmorLegs;
			}
			return true;
		}
	}

	public override string GetMeshCacheKey(ItemStack itemstack)
	{
		return "wearableModelRef-" + itemstack.Collectible.Code.ToString();
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Enum.TryParse<EnumCharacterDressType>(Attributes["clothescategory"].AsString(), ignoreCase: true, out var dt);
		DressType = dt;
		JsonObject jsonObj = Attributes?["footStepSound"];
		if (jsonObj != null && jsonObj.Exists)
		{
			string soundloc = jsonObj.AsString();
			if (soundloc != null)
			{
				AssetLocation loc = AssetLocation.Create(soundloc, Code.Domain).WithPathPrefixOnce("sounds/");
				if (soundloc.EndsWith('*'))
				{
					loc.Path = loc.Path.TrimEnd('*');
					FootStepSounds = api.Assets.GetLocations(loc.Path, loc.Domain).ToArray();
				}
				else
				{
					FootStepSounds = new AssetLocation[1] { loc };
				}
			}
		}
		jsonObj = Attributes?["statModifiers"];
		if (jsonObj != null && jsonObj.Exists)
		{
			try
			{
				StatModifers = jsonObj.AsObject<StatModifiers>();
			}
			catch (Exception e3)
			{
				api.World.Logger.Error("Failed loading statModifiers for item/block {0}. Will ignore.", Code);
				api.World.Logger.Error(e3);
				StatModifers = null;
			}
		}
		ProtectionModifiers defMods = null;
		jsonObj = Attributes?["defaultProtLoss"];
		if (jsonObj != null && jsonObj.Exists)
		{
			try
			{
				defMods = jsonObj.AsObject<ProtectionModifiers>();
			}
			catch (Exception e2)
			{
				api.World.Logger.Error("Failed loading defaultProtLoss for item/block {0}. Will ignore.", Code);
				api.World.Logger.Error(e2);
			}
		}
		jsonObj = Attributes?["protectionModifiers"];
		if (jsonObj != null && jsonObj.Exists)
		{
			try
			{
				ProtectionModifiers = jsonObj.AsObject<ProtectionModifiers>();
			}
			catch (Exception e)
			{
				api.World.Logger.Error("Failed loading protectionModifiers for item/block {0}. Will ignore.", Code);
				api.World.Logger.Error(e);
				ProtectionModifiers = null;
			}
		}
		if (ProtectionModifiers != null && ProtectionModifiers.PerTierFlatDamageReductionLoss == null)
		{
			ProtectionModifiers.PerTierFlatDamageReductionLoss = defMods?.PerTierFlatDamageReductionLoss;
		}
		if (ProtectionModifiers != null && ProtectionModifiers.PerTierRelativeProtectionLoss == null)
		{
			ProtectionModifiers.PerTierRelativeProtectionLoss = defMods?.PerTierRelativeProtectionLoss;
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		Dictionary<string, MultiTextureMeshRef> armorMeshrefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "armorMeshRefs");
		if (armorMeshrefs == null)
		{
			return;
		}
		foreach (MultiTextureMeshRef value in armorMeshrefs.Values)
		{
			value?.Dispose();
		}
		api.ObjectCache.Remove("armorMeshRefs");
	}

	public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe recipe, ItemSlot dummyslot, double x, double y, double z, double size)
	{
		bool num = recipe.Name.Path.Contains("repair");
		int prevDura = 0;
		if (num)
		{
			prevDura = dummyslot.Itemstack.Collectible.GetRemainingDurability(dummyslot.Itemstack);
			dummyslot.Itemstack.Attributes.SetInt("durability", 0);
		}
		base.OnHandbookRecipeRender(capi, recipe, dummyslot, x, y, z, size);
		if (num)
		{
			dummyslot.Itemstack.Attributes.SetInt("durability", prevDura);
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (byPlayer != null)
		{
			IInventory inv = byPlayer.InventoryManager.GetOwnInventory("character");
			if (inv != null && DressType != EnumCharacterDressType.Unknown && inv[(int)DressType].TryFlipWith(slot))
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string descTextToAppend = "";
		string descText = base.GetItemDescText();
		if (descText.Length > 1)
		{
			int descIndex = dsc.ToString().IndexOfOrdinal(descText);
			if (descIndex >= 0)
			{
				if (descIndex > 0)
				{
					descIndex--;
				}
				else
				{
					descTextToAppend = "\n";
				}
				descTextToAppend += dsc.ToString(descIndex, dsc.Length - descIndex);
				dsc.Remove(descIndex, dsc.Length - descIndex);
			}
		}
		if ((api as ICoreClientAPI).Settings.Bool["extendedDebugInfo"])
		{
			if (DressType == EnumCharacterDressType.Unknown)
			{
				dsc.AppendLine(Lang.Get("Cloth Category: Unknown"));
			}
			else
			{
				dsc.AppendLine(Lang.Get("Cloth Category: {0}", Lang.Get("clothcategory-" + inSlot.Itemstack.ItemAttributes["clothescategory"].AsString())));
			}
		}
		if (ProtectionModifiers != null)
		{
			if (ProtectionModifiers.FlatDamageReduction != 0f)
			{
				dsc.AppendLine(Lang.Get("Flat damage reduction: {0} hp", ProtectionModifiers.FlatDamageReduction));
			}
			if (ProtectionModifiers.RelativeProtection != 0f)
			{
				dsc.AppendLine(Lang.Get("Percent protection: {0}%", (int)(100f * ProtectionModifiers.RelativeProtection)));
			}
			dsc.AppendLine(Lang.Get("Protection tier: {0}", ProtectionModifiers.ProtectionTier));
		}
		if (StatModifers != null)
		{
			if (ProtectionModifiers != null)
			{
				dsc.AppendLine();
			}
			if (StatModifers.healingeffectivness != 0f)
			{
				dsc.AppendLine(Lang.Get("Healing effectivness: {0}%", (int)(100f * StatModifers.healingeffectivness)));
			}
			if (StatModifers.hungerrate != 0f)
			{
				dsc.AppendLine(Lang.Get("Hunger rate: {1}{0}%", (int)(100f * StatModifers.hungerrate), (StatModifers.hungerrate > 0f) ? "+" : ""));
			}
			if (StatModifers.rangedWeaponsAcc != 0f)
			{
				dsc.AppendLine(Lang.Get("Ranged Weapon Accuracy: {1}{0}%", (int)(100f * StatModifers.rangedWeaponsAcc), (StatModifers.rangedWeaponsAcc > 0f) ? "+" : ""));
			}
			if (StatModifers.rangedWeaponsSpeed != 0f)
			{
				dsc.AppendLine(Lang.Get("Ranged Weapon Charge Time: {1}{0}%", -(int)(100f * StatModifers.rangedWeaponsSpeed), (0f - StatModifers.rangedWeaponsSpeed > 0f) ? "+" : ""));
			}
			if (StatModifers.walkSpeed != 0f)
			{
				dsc.AppendLine(Lang.Get("Walk speed: {1}{0}%", (int)(100f * StatModifers.walkSpeed), (StatModifers.walkSpeed > 0f) ? "+" : ""));
			}
		}
		ProtectionModifiers protectionModifiers = ProtectionModifiers;
		if (protectionModifiers != null && protectionModifiers.HighDamageTierResistant)
		{
			dsc.AppendLine("<font color=\"#86aad0\">" + Lang.Get("High damage tier resistant") + "</font> " + Lang.Get("When damaged by a higher tier attack, the loss of protection is only half as much."));
		}
		JsonObject itemAttributes = inSlot.Itemstack.ItemAttributes;
		if (itemAttributes != null && itemAttributes["warmth"].Exists)
		{
			JsonObject itemAttributes2 = inSlot.Itemstack.ItemAttributes;
			if (itemAttributes2 == null || itemAttributes2["warmth"].AsFloat() != 0f)
			{
				if (!(inSlot is ItemSlotCreative))
				{
					ensureConditionExists(inSlot);
					float condition = inSlot.Itemstack.Attributes.GetFloat("condition", 1f);
					string condStr = (((double)condition > 0.5) ? Lang.Get("clothingcondition-good", (int)(condition * 100f)) : (((double)condition > 0.4) ? Lang.Get("clothingcondition-worn", (int)(condition * 100f)) : (((double)condition > 0.3) ? Lang.Get("clothingcondition-heavilyworn", (int)(condition * 100f)) : (((double)condition > 0.2) ? Lang.Get("clothingcondition-tattered", (int)(condition * 100f)) : ((!((double)condition > 0.1)) ? Lang.Get("clothingcondition-terrible", (int)(condition * 100f)) : Lang.Get("clothingcondition-heavilytattered", (int)(condition * 100f)))))));
					dsc.Append(Lang.Get("Condition:") + " ");
					float warmth = GetWarmth(inSlot);
					string color = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, condition * 200f)]);
					if ((double)warmth < 0.05)
					{
						dsc.AppendLine("<font color=\"" + color + "\">" + condStr + "</font>, <font color=\"#ff8484\">" + Lang.Get("+{0:0.#}°C", warmth) + "</font>");
					}
					else
					{
						dsc.AppendLine("<font color=\"" + color + "\">" + condStr + "</font>, <font color=\"#84ff84\">" + Lang.Get("+{0:0.#}°C", warmth) + "</font>");
					}
				}
				float maxWarmth = inSlot.Itemstack.ItemAttributes?["warmth"].AsFloat() ?? 0f;
				dsc.AppendLine();
				dsc.AppendLine(Lang.Get("clothing-maxwarmth", maxWarmth));
			}
		}
		dsc.Append(descTextToAppend);
	}

	public float GetWarmth(ItemSlot inslot)
	{
		ensureConditionExists(inslot);
		float maxWarmth = inslot.Itemstack.ItemAttributes?["warmth"].AsFloat() ?? 0f;
		float condition = inslot.Itemstack.Attributes.GetFloat("condition", 1f);
		return Math.Min(maxWarmth, condition * 2f * maxWarmth);
	}

	public void ChangeCondition(ItemSlot slot, float changeVal)
	{
		if (changeVal != 0f)
		{
			ensureConditionExists(slot);
			slot.Itemstack.Attributes.SetFloat("condition", GameMath.Clamp(slot.Itemstack.Attributes.GetFloat("condition", 1f) + changeVal, 0f, 1f));
			slot.MarkDirty();
		}
	}

	public override bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
	{
		return !itemstack.Attributes.HasAttribute("condition");
	}

	private void ensureConditionExists(ItemSlot slot, bool markdirty = true)
	{
		if (slot is DummySlot || slot.Itemstack.Attributes.HasAttribute("condition") || api.Side != EnumAppSide.Server)
		{
			return;
		}
		JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
		if (itemAttributes == null || !itemAttributes["warmth"].Exists)
		{
			return;
		}
		JsonObject itemAttributes2 = slot.Itemstack.ItemAttributes;
		if (itemAttributes2 == null || itemAttributes2["warmth"].AsFloat() != 0f)
		{
			if (slot is ItemSlotTrade)
			{
				slot.Itemstack.Attributes.SetFloat("condition", (float)api.World.Rand.NextDouble() * 0.25f + 0.75f);
			}
			else
			{
				slot.Itemstack.Attributes.SetFloat("condition", (float)api.World.Rand.NextDouble() * 0.4f);
			}
			if (markdirty)
			{
				slot.MarkDirty();
			}
		}
	}

	public override void OnCreatedByCrafting(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		base.OnCreatedByCrafting(inSlots, outputSlot, byRecipe);
		if (!(outputSlot is DummySlot))
		{
			ensureConditionExists(outputSlot);
			outputSlot.Itemstack.Attributes.SetFloat("condition", 1f);
			if (byRecipe.Name.Path.Contains("repair"))
			{
				CalculateRepairValue(inSlots, outputSlot, out var repairValue, out var _);
				int curDur = outputSlot.Itemstack.Collectible.GetRemainingDurability(outputSlot.Itemstack);
				int maxDur = GetMaxDurability(outputSlot.Itemstack);
				outputSlot.Itemstack.Attributes.SetInt("durability", Math.Min(maxDur, (int)((float)curDur + (float)maxDur * repairValue)));
			}
		}
	}

	public override bool ConsumeCraftingIngredients(ItemSlot[] inSlots, ItemSlot outputSlot, GridRecipe recipe)
	{
		if (recipe.Name.Path.Contains("repair"))
		{
			CalculateRepairValue(inSlots, outputSlot, out var _, out var matCostPerMatType);
			foreach (ItemSlot islot in inSlots)
			{
				if (!islot.Empty)
				{
					if (islot.Itemstack.Collectible == this)
					{
						islot.Itemstack = null;
					}
					else
					{
						islot.TakeOut(matCostPerMatType);
					}
				}
			}
			return true;
		}
		return false;
	}

	public void CalculateRepairValue(ItemSlot[] inSlots, ItemSlot outputSlot, out float repairValue, out int matCostPerMatType)
	{
		int origMatCount = GetOrigMatCount(inSlots, outputSlot);
		ItemSlot armorSlot = inSlots.FirstOrDefault((ItemSlot slot) => slot.Itemstack?.Collectible is ItemWearable);
		int curDur = outputSlot.Itemstack.Collectible.GetRemainingDurability(armorSlot.Itemstack);
		int maxDur = GetMaxDurability(outputSlot.Itemstack);
		float repairDurabilityPerItem = 2f / (float)origMatCount * (float)maxDur;
		int fullRepairMatCount = (int)Math.Max(1.0, Math.Round((float)(maxDur - curDur) / repairDurabilityPerItem));
		int minMatStackSize = GetInputRepairCount(inSlots);
		int matTypeCount = GetRepairMatTypeCount(inSlots);
		int availableRepairMatCount = Math.Min(fullRepairMatCount, minMatStackSize * matTypeCount);
		matCostPerMatType = Math.Min(fullRepairMatCount, minMatStackSize);
		repairValue = (float)availableRepairMatCount / (float)origMatCount * 2f;
	}

	private int GetRepairMatTypeCount(ItemSlot[] slots)
	{
		List<ItemStack> stackTypes = new List<ItemStack>();
		foreach (ItemSlot slot in slots)
		{
			if (slot.Empty)
			{
				continue;
			}
			bool found = false;
			if (slot.Itemstack.Collectible is ItemWearable)
			{
				continue;
			}
			foreach (ItemStack stack in stackTypes)
			{
				if (slot.Itemstack.Satisfies(stack))
				{
					found = true;
					break;
				}
			}
			if (!found)
			{
				stackTypes.Add(slot.Itemstack);
			}
		}
		return stackTypes.Count;
	}

	public int GetInputRepairCount(ItemSlot[] inputSlots)
	{
		OrderedDictionary<int, int> matcounts = new OrderedDictionary<int, int>();
		foreach (ItemSlot slot in inputSlots)
		{
			if (!slot.Empty && !(slot.Itemstack.Collectible is ItemWearable))
			{
				int hash = slot.Itemstack.GetHashCode();
				int cnt = 0;
				matcounts.TryGetValue(hash, out cnt);
				matcounts[hash] = cnt + slot.StackSize;
			}
		}
		return matcounts.Values.Min();
	}

	public int GetOrigMatCount(ItemSlot[] inputSlots, ItemSlot outputSlot)
	{
		ItemStack stack = outputSlot.Itemstack;
		_ = inputSlots.FirstOrDefault((ItemSlot slot) => !slot.Empty && slot.Itemstack.Collectible != this).Itemstack;
		int origMatCount = 0;
		foreach (GridRecipe recipe in api.World.GridRecipes)
		{
			ItemStack resolvedItemstack = recipe.Output.ResolvedItemstack;
			if (resolvedItemstack == null || !resolvedItemstack.Satisfies(stack) || recipe.Name.Path.Contains("repair"))
			{
				continue;
			}
			GridRecipeIngredient[] resolvedIngredients = recipe.resolvedIngredients;
			foreach (GridRecipeIngredient ingred in resolvedIngredients)
			{
				if (ingred == null)
				{
					continue;
				}
				JsonObject recipeAttributes = ingred.RecipeAttributes;
				if (recipeAttributes != null && recipeAttributes["repairMat"].Exists)
				{
					JsonItemStack jstack = ingred.RecipeAttributes["repairMat"].AsObject<JsonItemStack>();
					jstack.Resolve(api.World, $"recipe '{recipe.Name}' repair mat");
					if (jstack.ResolvedItemstack != null)
					{
						origMatCount += jstack.ResolvedItemstack.StackSize;
					}
				}
				else
				{
					origMatCount += ingred.Quantity;
				}
			}
			break;
		}
		return origMatCount;
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		ensureConditionExists(inslot);
		return base.UpdateAndGetTransitionStates(world, inslot);
	}

	public override TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
	{
		if (type != 0)
		{
			ensureConditionExists(inslot);
		}
		return base.UpdateAndGetTransitionState(world, inslot, type);
	}

	public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
	{
		if (Variant["construction"] == "improvised")
		{
			base.DamageItem(world, byEntity, itemslot, amount);
			return;
		}
		float amountf = amount;
		if (byEntity is EntityPlayer && (DressType == EnumCharacterDressType.ArmorHead || DressType == EnumCharacterDressType.ArmorBody || DressType == EnumCharacterDressType.ArmorLegs))
		{
			amountf *= byEntity.Stats.GetBlended("armorDurabilityLoss");
		}
		amount = GameMath.RoundRandom(world.Rand, amountf);
		int leftDurability = itemslot.Itemstack.Attributes.GetInt("durability", GetMaxDurability(itemslot.Itemstack));
		if (leftDurability > 0 && leftDurability - amount < 0)
		{
			world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.SidedPos.X, byEntity.SidedPos.InternalY, byEntity.SidedPos.Z, (byEntity as EntityPlayer)?.Player);
		}
		itemslot.Itemstack.Attributes.SetInt("durability", Math.Max(0, leftDurability - amount));
		itemslot.MarkDirty();
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-dress",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}

	public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
	{
		if (priority == EnumMergePriority.DirectMerge)
		{
			JsonObject itemAttributes = sinkStack.ItemAttributes;
			if (itemAttributes != null && itemAttributes["warmth"].Exists)
			{
				JsonObject itemAttributes2 = sinkStack.ItemAttributes;
				if (itemAttributes2 == null || itemAttributes2["warmth"].AsFloat() != 0f)
				{
					if ((sourceStack?.ItemAttributes?["clothingRepairStrength"].AsFloat()).GetValueOrDefault() > 0f)
					{
						if (sinkStack.Attributes.GetFloat("condition") < 1f)
						{
							return 1;
						}
						return 0;
					}
					goto IL_00c7;
				}
			}
			return base.GetMergableQuantity(sinkStack, sourceStack, priority);
		}
		goto IL_00c7;
		IL_00c7:
		return base.GetMergableQuantity(sinkStack, sourceStack, priority);
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		if (op.CurrentPriority == EnumMergePriority.DirectMerge)
		{
			float repstr = op.SourceSlot.Itemstack.ItemAttributes?["clothingRepairStrength"].AsFloat() ?? 0f;
			if (repstr > 0f && op.SinkSlot.Itemstack.Attributes.GetFloat("condition") < 1f)
			{
				ChangeCondition(op.SinkSlot, repstr);
				op.MovedQuantity = 1;
				op.SourceSlot.TakeOut(1);
				return;
			}
		}
		base.TryMergeStacks(op);
	}
}
