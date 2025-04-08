using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonConverter(typeof(DialogueComponentJsonConverter))]
public abstract class DialogueComponent
{
	public string Code;

	public string Owner;

	public string Sound;

	public string Type;

	public Dictionary<string, string> SetVariables;

	public string JumpTo;

	public string Trigger;

	public JsonObject TriggerData;

	protected DialogueController controller;

	protected GuiDialogueDialog dialog;

	public virtual void SetReferences(DialogueController controller, GuiDialogueDialog dialogue)
	{
		this.controller = controller;
		dialog = dialogue;
	}

	public abstract string Execute();

	protected void setVars()
	{
		if (Trigger != null)
		{
			controller.Trigger(controller.PlayerEntity, Trigger, TriggerData);
		}
		if (Sound != null)
		{
			controller.PlayerEntity.Api.World.PlaySoundAt(new AssetLocation(Sound).WithPathPrefixOnce("sounds/"), controller.NPCEntity, controller.PlayerEntity?.Player, randomizePitch: false, 16f);
		}
		if (SetVariables == null)
		{
			return;
		}
		foreach (KeyValuePair<string, string> setvar in SetVariables)
		{
			string[] parts = setvar.Key.Split('.');
			EnumActivityVariableScope scope = scopeFromString(parts[0]);
			controller.VarSys.SetVariable((scope == EnumActivityVariableScope.Player) ? controller.PlayerEntity : controller.NPCEntity, scope, parts[1], setvar.Value);
		}
	}

	protected bool IsConditionMet(string variable, string isValue, bool invertCheck)
	{
		switch (variable)
		{
		case "player.inventory":
		{
			JsonItemStack jstack2 = JsonItemStack.FromString(isValue);
			if (!jstack2.Resolve(controller.NPCEntity.World, Code + "dialogue talk component quest item"))
			{
				return false;
			}
			ItemStack wantStack2 = jstack2.ResolvedItemstack;
			ItemSlot slot2 = FindDesiredItem(controller.PlayerEntity, wantStack2);
			if (!invertCheck)
			{
				return slot2 != null;
			}
			return slot2 == null;
		}
		case "player.inventorywildcard":
		{
			ItemSlot slot = FindDesiredItem(controller.PlayerEntity, isValue);
			if (!invertCheck)
			{
				return slot != null;
			}
			return slot == null;
		}
		case "player.heldstack":
		{
			if (isValue == "damagedtool")
			{
				ItemSlot hotbarslot = controller.PlayerEntity.RightHandItemSlot;
				if (hotbarslot.Empty)
				{
					return false;
				}
				int d = hotbarslot.Itemstack.Collectible.GetRemainingDurability(hotbarslot.Itemstack);
				int max = hotbarslot.Itemstack.Collectible.GetMaxDurability(hotbarslot.Itemstack);
				if (hotbarslot.Itemstack.Collectible.Tool.HasValue)
				{
					return d < max;
				}
				return false;
			}
			if (isValue == "damagedarmor")
			{
				ItemSlot hotbarslot2 = controller.PlayerEntity.RightHandItemSlot;
				if (hotbarslot2.Empty)
				{
					return false;
				}
				int d2 = hotbarslot2.Itemstack.Collectible.GetRemainingDurability(hotbarslot2.Itemstack);
				int max2 = hotbarslot2.Itemstack.Collectible.GetMaxDurability(hotbarslot2.Itemstack);
				if (hotbarslot2.Itemstack.Collectible.FirstCodePart() == "armor")
				{
					return d2 < max2;
				}
				return false;
			}
			JsonItemStack jstack = JsonItemStack.FromString(isValue);
			if (!jstack.Resolve(controller.NPCEntity.World, Code + "dialogue talk component quest item"))
			{
				return false;
			}
			ItemStack wantStack = jstack.ResolvedItemstack;
			ItemSlot hotbarslot3 = controller.PlayerEntity.RightHandItemSlot;
			if (matches(controller.PlayerEntity, wantStack, hotbarslot3, getIgnoreAttrs()))
			{
				return true;
			}
			break;
		}
		}
		string[] parts = variable.Split(new char[1] { '.' }, 2);
		EnumActivityVariableScope scope = scopeFromString(parts[0]);
		string curValue = controller.VarSys.GetVariable(scope, parts[1], (scope == EnumActivityVariableScope.Player) ? controller.PlayerEntity : controller.NPCEntity);
		if (!invertCheck)
		{
			return curValue == isValue;
		}
		return curValue != isValue;
	}

	public static ItemSlot FindDesiredItem(EntityAgent eagent, ItemStack wantStack)
	{
		ItemSlot foundSlot = null;
		string[] ignoredAttrs = getIgnoreAttrs();
		eagent.WalkInventory(delegate(ItemSlot slot)
		{
			if (slot.Empty)
			{
				return true;
			}
			if (matches(eagent, wantStack, slot, ignoredAttrs))
			{
				foundSlot = slot;
				return false;
			}
			return true;
		});
		return foundSlot;
	}

	public static ItemSlot FindDesiredItem(EntityAgent eagent, AssetLocation wildcardcode)
	{
		ItemSlot foundSlot = null;
		eagent.WalkInventory(delegate(ItemSlot slot)
		{
			if (slot.Empty)
			{
				return true;
			}
			if (WildcardUtil.Match(wildcardcode, slot.Itemstack.Collectible.Code))
			{
				foundSlot = slot;
				return false;
			}
			return true;
		});
		return foundSlot;
	}

	private static string[] getIgnoreAttrs()
	{
		return GlobalConstants.IgnoredStackAttributes.Append("backpack").Append("condition").Append("durability")
			.Append("randomX")
			.Append("randomZ");
	}

	private static bool matches(EntityAgent eagent, ItemStack wantStack, ItemSlot slot, string[] ignoredAttrs)
	{
		ItemStack giveStack = slot.Itemstack;
		if ((wantStack.Equals(eagent.World, giveStack, ignoredAttrs) || giveStack.Satisfies(wantStack)) && giveStack.Collectible.IsReasonablyFresh(eagent.World, giveStack) && giveStack.StackSize >= wantStack.StackSize)
		{
			return true;
		}
		return false;
	}

	private static EnumActivityVariableScope scopeFromString(string name)
	{
		EnumActivityVariableScope scope = EnumActivityVariableScope.Global;
		if (name == "global")
		{
			scope = EnumActivityVariableScope.Global;
		}
		if (name == "player")
		{
			scope = EnumActivityVariableScope.Player;
		}
		if (name == "entity")
		{
			scope = EnumActivityVariableScope.Entity;
		}
		if (name == "group")
		{
			scope = EnumActivityVariableScope.Group;
		}
		return scope;
	}

	public virtual void Init(ref int uniqueIdCounter)
	{
	}
}
