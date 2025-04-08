using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent.Mechanics;

namespace Vintagestory.GameContent;

public class BlockEntityItemFlow : BlockEntityOpenableContainer
{
	internal InventoryGeneric inventory;

	public BlockFacing[] PullFaces = new BlockFacing[0];

	public BlockFacing[] PushFaces = new BlockFacing[0];

	public BlockFacing[] AcceptFromFaces = new BlockFacing[0];

	public string inventoryClassName = "hopper";

	public string ItemFlowObjectLangCode = "hopper-contents";

	public int QuantitySlots = 4;

	protected float itemFlowRate = 1f;

	public BlockFacing LastReceivedFromDir;

	public int MaxHorizontalTravel = 3;

	private int checkRateMs;

	private float itemFlowAccum;

	private static AssetLocation hopperOpen = new AssetLocation("sounds/block/hopperopen");

	private static AssetLocation hopperTumble = new AssetLocation("sounds/block/hoppertumble");

	public override AssetLocation OpenSound => hopperOpen;

	public override AssetLocation CloseSound => null;

	public virtual float ItemFlowRate => itemFlowRate;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => inventoryClassName;

	private void InitInventory()
	{
		parseBlockProperties();
		if (inventory == null)
		{
			inventory = new InventoryGeneric(QuantitySlots, null, null);
			inventory.OnInventoryClosed += OnInvClosed;
			inventory.OnInventoryOpened += OnInvOpened;
			inventory.SlotModified += OnSlotModifid;
			inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
			inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
		}
	}

	private void parseBlockProperties()
	{
		if (base.Block?.Attributes == null)
		{
			return;
		}
		if (base.Block.Attributes["pullFaces"].Exists)
		{
			string[] faces3 = base.Block.Attributes["pullFaces"].AsArray<string>();
			PullFaces = new BlockFacing[faces3.Length];
			for (int k = 0; k < faces3.Length; k++)
			{
				PullFaces[k] = BlockFacing.FromCode(faces3[k]);
			}
		}
		if (base.Block.Attributes["pushFaces"].Exists)
		{
			string[] faces2 = base.Block.Attributes["pushFaces"].AsArray<string>();
			PushFaces = new BlockFacing[faces2.Length];
			for (int j = 0; j < faces2.Length; j++)
			{
				PushFaces[j] = BlockFacing.FromCode(faces2[j]);
			}
		}
		if (base.Block.Attributes["acceptFromFaces"].Exists)
		{
			string[] faces = base.Block.Attributes["acceptFromFaces"].AsArray<string>();
			AcceptFromFaces = new BlockFacing[faces.Length];
			for (int i = 0; i < faces.Length; i++)
			{
				AcceptFromFaces[i] = BlockFacing.FromCode(faces[i]);
			}
		}
		itemFlowRate = base.Block.Attributes["item-flowrate"].AsFloat(itemFlowRate);
		checkRateMs = base.Block.Attributes["item-checkrateMs"].AsInt(200);
		inventoryClassName = base.Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
		ItemFlowObjectLangCode = base.Block.Attributes["itemFlowObjectLangCode"].AsString(ItemFlowObjectLangCode);
		QuantitySlots = base.Block.Attributes["quantitySlots"].AsInt(QuantitySlots);
	}

	private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		PushFaces.Contains(atBlockFace);
		return null;
	}

	private ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		if (PullFaces.Contains(atBlockFace) || AcceptFromFaces.Contains(atBlockFace))
		{
			return inventory[0];
		}
		return null;
	}

	private void OnSlotModifid(int slot)
	{
		Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
	}

	protected virtual void OnInvOpened(IPlayer player)
	{
		inventory.PutLocked = false;
	}

	protected virtual void OnInvClosed(IPlayer player)
	{
		invDialog?.Dispose();
		invDialog = null;
	}

	public override void Initialize(ICoreAPI api)
	{
		InitInventory();
		base.Initialize(api);
		if (api is ICoreServerAPI)
		{
			RegisterDelayedCallback(delegate
			{
				RegisterGameTickListener(MoveItem, checkRateMs);
			}, 10 + api.World.Rand.Next(200));
		}
	}

	public void MoveItem(float dt)
	{
		itemFlowAccum = Math.Min(itemFlowAccum + ItemFlowRate, Math.Max(1f, ItemFlowRate * 2f));
		if (itemFlowAccum < 1f)
		{
			return;
		}
		if (PushFaces != null && PushFaces.Length != 0 && !inventory.Empty)
		{
			ItemStack itemstack = inventory.First((ItemSlot slot) => !slot.Empty).Itemstack;
			BlockFacing outputFace = PushFaces[Api.World.Rand.Next(PushFaces.Length)];
			int dir = itemstack.Attributes.GetInt("chuteDir", -1);
			BlockFacing desiredDir = ((dir >= 0 && PushFaces.Contains(BlockFacing.ALLFACES[dir])) ? BlockFacing.ALLFACES[dir] : null);
			if (desiredDir != null)
			{
				if (Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.AddCopy(desiredDir)) == null)
				{
					return;
				}
				if (!TrySpitOut(desiredDir) && !TryPushInto(desiredDir) && !TrySpitOut(outputFace) && outputFace != desiredDir.Opposite && !TryPushInto(outputFace) && PullFaces.Length != 0)
				{
					BlockFacing pullFace = PullFaces[Api.World.Rand.Next(PullFaces.Length)];
					if (pullFace.IsHorizontal && !TryPushInto(pullFace))
					{
						TrySpitOut(pullFace);
					}
				}
			}
			else
			{
				if (Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.AddCopy(outputFace)) == null)
				{
					return;
				}
				if (!TrySpitOut(outputFace) && !TryPushInto(outputFace) && PullFaces != null && PullFaces.Length != 0)
				{
					BlockFacing pullFace2 = PullFaces[Api.World.Rand.Next(PullFaces.Length)];
					if (pullFace2.IsHorizontal && !TryPushInto(pullFace2))
					{
						TrySpitOut(pullFace2);
					}
				}
			}
		}
		if (PullFaces != null && PullFaces.Length != 0 && inventory.Empty)
		{
			BlockFacing inputFace = PullFaces[Api.World.Rand.Next(PullFaces.Length)];
			TryPullFrom(inputFace);
		}
	}

	private void TryPullFrom(BlockFacing inputFace)
	{
		BlockPos InputPosition = Pos.AddCopy(inputFace);
		BlockEntityContainer beContainer = Api.World.BlockAccessor.GetBlock(InputPosition).GetBlockEntity<BlockEntityContainer>(InputPosition);
		if (beContainer == null)
		{
			return;
		}
		if (beContainer.Block is BlockChute chute)
		{
			string[] array = chute.Attributes["pushFaces"].AsArray<string>();
			if (array != null && array.Contains(inputFace.Opposite.Code))
			{
				return;
			}
		}
		ItemSlot sourceSlot = beContainer.Inventory.GetAutoPullFromSlot(inputFace.Opposite);
		ItemSlot targetSlot = ((sourceSlot == null) ? null : inventory.GetBestSuitedSlot(sourceSlot).slot);
		BlockEntityItemFlow beFlow = beContainer as BlockEntityItemFlow;
		if (sourceSlot == null || targetSlot == null || (beFlow != null && !targetSlot.Empty))
		{
			return;
		}
		ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.DirectMerge, (int)itemFlowAccum);
		int horTravelled = sourceSlot.Itemstack.Attributes.GetInt("chuteQHTravelled");
		if (horTravelled >= MaxHorizontalTravel)
		{
			return;
		}
		int qmoved = sourceSlot.TryPutInto(targetSlot, ref op);
		if (qmoved > 0)
		{
			if (beFlow != null)
			{
				targetSlot.Itemstack.Attributes.SetInt("chuteQHTravelled", inputFace.IsHorizontal ? (horTravelled + 1) : 0);
				targetSlot.Itemstack.Attributes.SetInt("chuteDir", inputFace.Opposite.Index);
			}
			else
			{
				targetSlot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
				targetSlot.Itemstack.Attributes.RemoveAttribute("chuteDir");
			}
			sourceSlot.MarkDirty();
			targetSlot.MarkDirty();
			MarkDirty();
			beFlow?.MarkDirty();
		}
		if (qmoved > 0 && Api.World.Rand.NextDouble() < 0.2)
		{
			Api.World.PlaySoundAt(hopperTumble, Pos, 0.0, null, randomizePitch: true, 8f, 0.5f);
			itemFlowAccum -= qmoved;
		}
	}

	private bool TryPushInto(BlockFacing outputFace)
	{
		BlockPos OutputPosition = Pos.AddCopy(outputFace);
		BlockEntityContainer beContainer = Api.World.BlockAccessor.GetBlock(OutputPosition).GetBlockEntity<BlockEntityContainer>(OutputPosition);
		if (beContainer != null)
		{
			ItemSlot sourceSlot = inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty);
			if ((sourceSlot?.Itemstack?.StackSize).GetValueOrDefault() == 0)
			{
				return false;
			}
			int horTravelled = sourceSlot.Itemstack.Attributes.GetInt("chuteQHTravelled");
			int chuteDir = sourceSlot.Itemstack.Attributes.GetInt("chuteDir");
			if (outputFace.IsHorizontal && horTravelled >= MaxHorizontalTravel)
			{
				return false;
			}
			sourceSlot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
			sourceSlot.Itemstack.Attributes.RemoveAttribute("chuteDir");
			ItemSlot targetSlot = beContainer.Inventory.GetAutoPushIntoSlot(outputFace.Opposite, sourceSlot);
			BlockEntityItemFlow beFlow = beContainer as BlockEntityItemFlow;
			if (targetSlot != null && (beFlow == null || targetSlot.Empty))
			{
				int quantity = (int)itemFlowAccum;
				ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.DirectMerge, quantity);
				int qmoved = sourceSlot.TryPutInto(targetSlot, ref op);
				if (qmoved > 0)
				{
					if (Api.World.Rand.NextDouble() < 0.2)
					{
						Api.World.PlaySoundAt(hopperTumble, Pos, 0.0, null, randomizePitch: true, 8f, 0.5f);
					}
					if (beFlow != null)
					{
						targetSlot.Itemstack.Attributes.SetInt("chuteQHTravelled", outputFace.IsHorizontal ? (horTravelled + 1) : 0);
						if (beFlow is BlockEntityArchimedesScrew)
						{
							targetSlot.Itemstack.Attributes.SetInt("chuteDir", BlockFacing.UP.Index);
						}
						else
						{
							targetSlot.Itemstack.Attributes.SetInt("chuteDir", outputFace.Index);
						}
					}
					else
					{
						targetSlot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
						targetSlot.Itemstack.Attributes.RemoveAttribute("chuteDir");
					}
					sourceSlot.MarkDirty();
					targetSlot.MarkDirty();
					MarkDirty();
					beFlow?.MarkDirty();
					itemFlowAccum -= qmoved;
					return true;
				}
				sourceSlot.Itemstack.Attributes.SetInt("chuteDir", chuteDir);
			}
		}
		return false;
	}

	private bool TrySpitOut(BlockFacing outputFace)
	{
		if (Api.World.BlockAccessor.GetBlock(Pos.AddCopy(outputFace)).Replaceable >= 6000)
		{
			ItemSlot? itemSlot = inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty);
			ItemStack stack = itemSlot.TakeOut((int)itemFlowAccum);
			itemFlowAccum -= stack.StackSize;
			stack.Attributes.RemoveAttribute("chuteQHTravelled");
			stack.Attributes.RemoveAttribute("chuteDir");
			float velox = outputFace.Normalf.X / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 0.05f) * (float)Math.Sign(outputFace.Normalf.X);
			float veloy = outputFace.Normalf.Y / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 0.05f) * (float)Math.Sign(outputFace.Normalf.Y);
			float veloz = outputFace.Normalf.Z / 10f + ((float)Api.World.Rand.NextDouble() / 20f - 0.05f) * (float)Math.Sign(outputFace.Normalf.Z);
			Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5 + (double)(outputFace.Normalf.X / 2f), 0.5 + (double)(outputFace.Normalf.Y / 2f), 0.5 + (double)(outputFace.Normalf.Z / 2f)), new Vec3d(velox, veloy, veloz));
			itemSlot.MarkDirty();
			MarkDirty();
			return true;
		}
		return false;
	}

	public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (Api.World is IServerWorldAccessor)
		{
			byte[] data = BlockEntityContainerOpen.ToBytes("BlockEntityItemFlowDialog", Lang.Get(ItemFlowObjectLangCode), 4, inventory);
			((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos, 5000, data);
			byPlayer.InventoryManager.OpenInventory(inventory);
		}
		return true;
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		base.OnReceivedServerPacket(packetid, data);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		InitInventory();
		int index = tree.GetInt("lastReceivedFromDir");
		if (index < 0)
		{
			LastReceivedFromDir = null;
		}
		else
		{
			LastReceivedFromDir = BlockFacing.ALLFACES[index];
		}
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("lastReceivedFromDir", LastReceivedFromDir?.Index ?? (-1));
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		if (base.Block is BlockChute)
		{
			foreach (BlockEntityBehavior behavior in Behaviors)
			{
				behavior.GetBlockInfo(forPlayer, sb);
			}
			sb.AppendLine(Lang.Get("Transporting: {0}", inventory[0].Empty ? Lang.Get("nothing") : (inventory[0].StackSize + "x " + inventory[0].GetStackName())));
			sb.AppendLine("\u00a0                                                           \u00a0");
		}
		else
		{
			base.GetBlockInfo(forPlayer, sb);
			sb.AppendLine(Lang.Get("Contents: {0}", inventory[0].Empty ? Lang.Get("Empty") : (inventory[0].StackSize + "x " + inventory[0].GetStackName())));
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (Api.World is IServerWorldAccessor)
		{
			DropContents();
		}
		base.OnBlockBroken(byPlayer);
	}

	private void DropContents()
	{
		Vec3d epos = Pos.ToVec3d().Add(0.5, 0.5, 0.5);
		foreach (ItemSlot slot in inventory)
		{
			if (slot.Itemstack != null)
			{
				slot.Itemstack.Attributes.RemoveAttribute("chuteQHTravelled");
				slot.Itemstack.Attributes.RemoveAttribute("chuteDir");
				Api.World.SpawnItemEntity(slot.Itemstack, epos);
				slot.Itemstack = null;
				slot.MarkDirty();
			}
		}
	}

	public override void OnBlockRemoved()
	{
		if (Api.World is IServerWorldAccessor)
		{
			DropContents();
		}
		base.OnBlockRemoved();
	}

	public override void OnExchanged(Block block)
	{
		base.OnExchanged(block);
		parseBlockProperties();
	}
}
