using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemPlumbAndSquare : Item
{
	private WorldInteraction[] interactions;

	private List<LoadedTexture> symbols;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "plumbAndSquareInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				JsonObject attributes = current.Attributes;
				if (attributes != null && attributes["reinforcementStrength"].AsInt() > 0)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-reinforceblock",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				},
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-removereinforcement",
					MouseButton = EnumMouseButton.Left,
					Itemstacks = list.ToArray()
				}
			};
		});
		symbols = new List<LoadedTexture>();
		symbols.Add(GenTexture(1, 1));
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (!(api is ICoreClientAPI) || symbols == null)
		{
			return;
		}
		foreach (LoadedTexture symbol in symbols)
		{
			symbol.Dispose();
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling == EnumHandHandling.PreventDefault)
		{
			return;
		}
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			handling = EnumHandHandling.PreventDefaultAction;
		}
		else
		{
			if (blockSel == null || !((byEntity as EntityPlayer).Player is IServerPlayer byPlayer) || !byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				return;
			}
			ModSystemBlockReinforcement bre = byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
			ItemSlot resSlot = bre.FindResourceForReinforcing(byPlayer);
			if (resSlot == null)
			{
				return;
			}
			int strength = resSlot.Itemstack.ItemAttributes["reinforcementStrength"].AsInt();
			int toolMode = slot.Itemstack.Attributes.GetInt("toolMode");
			int groupUid = 0;
			PlayerGroupMembership[] groups = byPlayer.GetGroups();
			if (toolMode > 0 && toolMode - 1 < groups.Length)
			{
				groupUid = groups[toolMode - 1].GroupUid;
			}
			if (!api.World.BlockAccessor.GetBlock(blockSel.Position).HasBehavior<BlockBehaviorReinforcable>())
			{
				byPlayer.SendIngameError("notreinforcable", "This block can not be reinforced!");
				return;
			}
			if (!((groupUid > 0) ? bre.StrengthenBlock(blockSel.Position, byPlayer, strength, groupUid) : bre.StrengthenBlock(blockSel.Position, byPlayer, strength)))
			{
				byPlayer.SendIngameError("alreadyreinforced", "Cannot reinforce block, it's already reinforced!");
				return;
			}
			resSlot.TakeOut(1);
			resSlot.MarkDirty();
			BlockPos pos = blockSel.Position;
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/reinforce"), pos, 0.0);
			handling = EnumHandHandling.PreventDefaultAction;
			if (byEntity.World.Side == EnumAppSide.Client)
			{
				((byEntity as EntityPlayer)?.Player as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			}
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			handling = EnumHandHandling.PreventDefaultAction;
		}
		else
		{
			if (blockSel == null)
			{
				return;
			}
			ModSystemBlockReinforcement modBre = byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
			if (!((byEntity as EntityPlayer).Player is IServerPlayer player))
			{
				return;
			}
			BlockReinforcement bre = modBre.GetReinforcment(blockSel.Position);
			string errorCode = "";
			if (!modBre.TryRemoveReinforcement(blockSel.Position, player, ref errorCode))
			{
				if (errorCode == "notownblock")
				{
					player.SendIngameError("cantremove", "Cannot remove reinforcement. This block does not belong to you");
				}
				else
				{
					player.SendIngameError("cantremove", "Cannot remove reinforcement. It's not reinforced");
				}
				return;
			}
			if (bre.Locked)
			{
				ItemStack stack = new ItemStack(byEntity.World.GetItem(new AssetLocation(bre.LockedByItemCode)));
				if (!player.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
				{
					byEntity.World.SpawnItemEntity(stack, byEntity.ServerPos.XYZ);
				}
			}
			BlockPos pos = blockSel.Position;
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/reinforce"), pos, 0.0);
			handling = EnumHandHandling.PreventDefaultAction;
		}
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return Math.Min(1 + byPlayer.GetGroups().Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		PlayerGroupMembership[] groups = forPlayer.GetGroups();
		SkillItem[] modes = new SkillItem[1 + groups.Length];
		ICoreClientAPI capi = api as ICoreClientAPI;
		int seed = 1;
		LoadedTexture texture = FetchOrCreateTexture(seed);
		modes[0] = new SkillItem
		{
			Code = new AssetLocation("self"),
			Name = Lang.Get("Reinforce for yourself")
		}.WithIcon(capi, texture);
		for (int i = 0; i < groups.Length; i++)
		{
			texture = FetchOrCreateTexture(++seed);
			modes[i + 1] = new SkillItem
			{
				Code = new AssetLocation("group"),
				Name = Lang.Get("Reinforce for group " + groups[i].GroupName)
			}.WithIcon(capi, texture);
		}
		return modes;
	}

	private LoadedTexture FetchOrCreateTexture(int seed)
	{
		if (symbols.Count >= seed)
		{
			return symbols[seed - 1];
		}
		LoadedTexture newTexture = GenTexture(seed, seed);
		symbols.Add(newTexture);
		return newTexture;
	}

	private LoadedTexture GenTexture(int seed, int addLines)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		return capi.Gui.Icons.GenTexture(48, 48, delegate(Context ctx, ImageSurface surface)
		{
			capi.Gui.Icons.DrawRandomSymbol(ctx, 0.0, 0.0, 48.0, GuiStyle.MacroIconColor, 2.0, seed, addLines);
		});
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
