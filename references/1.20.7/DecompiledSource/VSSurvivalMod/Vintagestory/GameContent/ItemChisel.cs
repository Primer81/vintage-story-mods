using System;
using System.Linq;
using VSSurvivalMod.Systems.ChiselModes;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemChisel : Item
{
	public SkillItem[] ToolModes;

	private SkillItem addMatItem;

	public static bool AllowHalloweenEvent = true;

	public bool carvingTime
	{
		get
		{
			DateTime dateTime = DateTime.UtcNow;
			if (dateTime.Month != 10)
			{
				return dateTime.Month == 11;
			}
			return true;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		ToolModes = ObjectCacheUtil.GetOrCreate(api, "chiselToolModes", delegate
		{
			SkillItem[] array = new SkillItem[7]
			{
				new SkillItem
				{
					Code = new AssetLocation("1size"),
					Name = Lang.Get("1x1x1"),
					Data = new OneByChiselMode()
				},
				new SkillItem
				{
					Code = new AssetLocation("2size"),
					Name = Lang.Get("2x2x2"),
					Data = new TwoByChiselMode()
				},
				new SkillItem
				{
					Code = new AssetLocation("4size"),
					Name = Lang.Get("4x4x4"),
					Data = new FourByChiselMode()
				},
				new SkillItem
				{
					Code = new AssetLocation("8size"),
					Name = Lang.Get("8x8x8"),
					Data = new EightByChiselModeData()
				},
				new SkillItem
				{
					Code = new AssetLocation("rotate"),
					Name = Lang.Get("Rotate"),
					Data = new RotateChiselMode()
				},
				new SkillItem
				{
					Code = new AssetLocation("flip"),
					Name = Lang.Get("Flip"),
					Data = new FlipChiselMode()
				},
				new SkillItem
				{
					Code = new AssetLocation("rename"),
					Name = Lang.Get("Set name"),
					Data = new RenameChiselMode()
				}
			};
			ICoreClientAPI capi = api as ICoreClientAPI;
			if (capi != null)
			{
				array = array.Select(delegate(SkillItem i)
				{
					ChiselMode chiselMode = (ChiselMode)i.Data;
					return i.WithIcon(capi, chiselMode.DrawAction(capi));
				}).ToArray();
			}
			return array;
		});
		addMatItem = new SkillItem
		{
			Name = Lang.Get("chisel-addmat"),
			Code = new AssetLocation("addmat"),
			Enabled = false
		};
		if (api is ICoreClientAPI clientApi)
		{
			addMatItem = addMatItem.WithIcon(clientApi, "plus");
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		int i = 0;
		while (ToolModes != null && i < ToolModes.Length)
		{
			ToolModes[i]?.Dispose();
			i++;
		}
		addMatItem?.Dispose();
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		ICoreClientAPI obj = api as ICoreClientAPI;
		if (obj != null && obj.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			return null;
		}
		return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
	}

	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		ICoreClientAPI obj = api as ICoreClientAPI;
		if (obj != null && obj.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			return null;
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		ItemSlot leftHandItemSlot = byEntity.LeftHandItemSlot;
		if ((leftHandItemSlot == null || (leftHandItemSlot.Itemstack?.Collectible?.Tool).GetValueOrDefault() != EnumTool.Hammer) && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
			handling = EnumHandHandling.PreventDefaultAction;
		}
		else if (!(blockSel?.Position == null))
		{
			BlockPos pos = blockSel.Position;
			Block block = byEntity.World.BlockAccessor.GetBlock(pos);
			ModSystemBlockReinforcement modSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
			if (modSystem != null && modSystem.IsReinforced(pos))
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			}
			else if (!byEntity.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.BuildOrBreak))
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			}
			else if (!IsChiselingAllowedFor(api, pos, block, byPlayer))
			{
				base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
			}
			else if (blockSel == null)
			{
				base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
			}
			else if (block is BlockChisel)
			{
				OnBlockInteract(byEntity.World, byPlayer, blockSel, isBreak: true, ref handling);
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling == EnumHandHandling.PreventDefault)
		{
			return;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (blockSel?.Position == null)
		{
			return;
		}
		BlockPos pos = blockSel.Position;
		Block block = byEntity.World.BlockAccessor.GetBlock(pos);
		ItemSlot leftHandItemSlot = byEntity.LeftHandItemSlot;
		if ((leftHandItemSlot == null || (leftHandItemSlot.Itemstack?.Collectible?.Tool).GetValueOrDefault() != EnumTool.Hammer) && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
			handling = EnumHandHandling.PreventDefaultAction;
			return;
		}
		ModSystemBlockReinforcement modSystem = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
		if (modSystem != null && modSystem.IsReinforced(pos))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return;
		}
		if (!byEntity.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.BuildOrBreak))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
			return;
		}
		if (block is BlockGroundStorage)
		{
			ItemSlot neslot = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityGroundStorage).Inventory.FirstNonEmptySlot;
			if (neslot != null && neslot.Itemstack.Block != null && IsChiselingAllowedFor(api, pos, neslot.Itemstack.Block, byPlayer))
			{
				block = neslot.Itemstack.Block;
			}
			if (block.Code.Path == "pumpkin-fruit-4" && (!carvingTime || !AllowHalloweenEvent))
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
				api.World.BlockAccessor.MarkBlockDirty(pos);
				return;
			}
		}
		if (!IsChiselingAllowedFor(api, pos, block, byPlayer))
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			return;
		}
		if (block.Resistance > 100f)
		{
			if (api.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "tootoughtochisel", Lang.Get("This material is too strong to chisel"));
			}
			return;
		}
		if (blockSel == null)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			return;
		}
		if (block is BlockChisel)
		{
			OnBlockInteract(byEntity.World, byPlayer, blockSel, isBreak: false, ref handling);
			return;
		}
		Block chiseledblock = byEntity.World.GetBlock(new AssetLocation("chiseledblock"));
		byEntity.World.BlockAccessor.SetBlock(chiseledblock.BlockId, blockSel.Position);
		if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChisel be)
		{
			be.WasPlaced(block, null);
			if (carvingTime && block.Code.Path == "pumpkin-fruit-4")
			{
				be.AddMaterial(api.World.GetBlock(new AssetLocation("creativeglow-35")));
			}
			handling = EnumHandHandling.PreventDefaultAction;
		}
	}

	public static bool IsChiselingAllowedFor(ICoreAPI api, BlockPos pos, Block block, IPlayer player)
	{
		if (block is BlockMicroBlock)
		{
			if (block is BlockChisel)
			{
				return true;
			}
			return false;
		}
		return IsValidChiselingMaterial(api, pos, block, player);
	}

	public static bool IsValidChiselingMaterial(ICoreAPI api, BlockPos pos, Block block, IPlayer player)
	{
		if (block is BlockChisel)
		{
			return false;
		}
		string mode = api.World.Config.GetString("microblockChiseling");
		if (mode == "off")
		{
			return false;
		}
		IConditionalChiselable icc = block as IConditionalChiselable;
		if ((icc != null || (icc = block.BlockBehaviors.FirstOrDefault((BlockBehavior bh) => bh is IConditionalChiselable) as IConditionalChiselable) != null) && ((icc != null && !icc.CanChisel(api.World, pos, player, out var errorCode)) || (icc != null && !icc.CanChisel(api.World, pos, player, out errorCode))))
		{
			(api as ICoreClientAPI)?.TriggerIngameError(icc, errorCode, Lang.Get(errorCode));
			return false;
		}
		bool canChiselSet = block.Attributes?["canChisel"].Exists ?? false;
		bool canChisel = block.Attributes?["canChisel"].AsBool() ?? false;
		if (canChisel)
		{
			return true;
		}
		if (canChiselSet && !canChisel)
		{
			return false;
		}
		if (block.DrawType != EnumDrawType.Cube && block.Shape?.Base.Path != "block/basic/cube")
		{
			return false;
		}
		if (block.HasBehavior<BlockBehaviorDecor>())
		{
			return false;
		}
		if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			return true;
		}
		if (mode == "stonewood")
		{
			if (block.Code.Path.Contains("mudbrick"))
			{
				return true;
			}
			if (block.BlockMaterial != EnumBlockMaterial.Wood && block.BlockMaterial != EnumBlockMaterial.Stone && block.BlockMaterial != EnumBlockMaterial.Ore)
			{
				return block.BlockMaterial == EnumBlockMaterial.Ceramic;
			}
			return true;
		}
		return true;
	}

	public void OnBlockInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, bool isBreak, ref EnumHandHandling handling)
	{
		if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
		}
		else if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChisel bec)
		{
			int materialId = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetInt("materialId", -1);
			if (materialId >= 0)
			{
				bec.SetNowMaterialId(materialId);
			}
			bec.OnBlockInteract(byPlayer, blockSel, isBreak);
			handling = EnumHandHandling.PreventDefaultAction;
		}
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		if (blockSel == null)
		{
			return null;
		}
		if (forPlayer.Entity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChisel be)
		{
			if (be.BlockIds.Length <= 1)
			{
				addMatItem.Linebreak = true;
				return ToolModes.Append(addMatItem);
			}
			SkillItem[] mats = new SkillItem[be.BlockIds.Length + 1];
			for (int i = 0; i < be.BlockIds.Length; i++)
			{
				Block block = api.World.GetBlock(be.BlockIds[i]);
				ItemSlot dummySlot = new DummySlot();
				dummySlot.Itemstack = new ItemStack(block);
				mats[i] = new SkillItem
				{
					Code = block.Code,
					Data = be.BlockIds[i],
					Linebreak = (i % 7 == 0),
					Name = block.GetHeldItemName(dummySlot.Itemstack),
					RenderHandler = delegate(AssetLocation code, float dt, double atPosX, double atPosY)
					{
						float num = (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
						(api as ICoreClientAPI).Render.RenderItemstackToGui(dummySlot, atPosX + (double)(num / 2f), atPosY + (double)(num / 2f), 50.0, num / 2f, -1, shading: true, rotate: false, showStackSize: false);
					}
				};
			}
			mats[^1] = addMatItem;
			addMatItem.Linebreak = (mats.Length - 1) % 7 == 0;
			return ToolModes.Append(mats);
		}
		return null;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		return slot.Itemstack.Attributes.GetInt("toolMode");
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
	{
		if (blockSel == null)
		{
			return;
		}
		BlockPos pos = blockSel.Position;
		ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;
		if (!mouseslot.Empty && mouseslot.Itemstack.Block != null && !(mouseslot.Itemstack.Block is BlockChisel))
		{
			BlockEntityChisel be = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
			if (!IsValidChiselingMaterial(api, pos, mouseslot.Itemstack.Block, byPlayer))
			{
				return;
			}
			if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				be.AddMaterial(mouseslot.Itemstack.Block, out var isFull);
				if (!isFull)
				{
					mouseslot.TakeOut(1);
					mouseslot.MarkDirty();
				}
			}
			else
			{
				be.AddMaterial(mouseslot.Itemstack.Block, out var _, compareToPickBlock: false);
			}
			be.MarkDirty();
			api.Event.PushEvent("keepopentoolmodedlg");
		}
		else if (toolMode > ToolModes.Length - 1)
		{
			int matNum = toolMode - ToolModes.Length;
			if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChisel be2 && be2.BlockIds.Length > matNum)
			{
				slot.Itemstack.Attributes.SetInt("materialId", be2.BlockIds[matNum]);
				slot.MarkDirty();
			}
		}
		else
		{
			slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
		}
	}
}
