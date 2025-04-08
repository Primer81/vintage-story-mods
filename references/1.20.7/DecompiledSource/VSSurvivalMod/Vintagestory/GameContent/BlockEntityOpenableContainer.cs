using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockEntityOpenableContainer : BlockEntityContainer
{
	protected GuiDialogBlockEntity invDialog;

	public HashSet<long> LidOpenEntityId;

	public virtual AssetLocation OpenSound { get; set; } = new AssetLocation("sounds/block/chestopen");


	public virtual AssetLocation CloseSound { get; set; } = new AssetLocation("sounds/block/chestclose");


	public abstract bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		LidOpenEntityId = new HashSet<long>();
		Inventory.LateInitialize(InventoryClassName + "-" + Pos, api);
		Inventory.ResolveBlocksOrItems();
		Inventory.OnInventoryOpened += OnInventoryOpened;
		Inventory.OnInventoryClosed += OnInventoryClosed;
		string os = base.Block.Attributes?["openSound"]?.AsString();
		string cs = base.Block.Attributes?["closeSound"]?.AsString();
		AssetLocation opensound = ((os == null) ? null : AssetLocation.Create(os, base.Block.Code.Domain));
		AssetLocation closesound = ((cs == null) ? null : AssetLocation.Create(cs, base.Block.Code.Domain));
		OpenSound = opensound ?? OpenSound;
		CloseSound = closesound ?? CloseSound;
	}

	private void OnInventoryOpened(IPlayer player)
	{
		LidOpenEntityId.Add(player.Entity.EntityId);
	}

	private void OnInventoryClosed(IPlayer player)
	{
		LidOpenEntityId.Remove(player.Entity.EntityId);
	}

	protected void toggleInventoryDialogClient(IPlayer byPlayer, CreateDialogDelegate onCreateDialog)
	{
		if (invDialog == null)
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			invDialog = onCreateDialog();
			invDialog.OnClosed += delegate
			{
				invDialog = null;
				capi.Network.SendBlockEntityPacket(Pos, 1001);
				capi.Network.SendPacketClient(Inventory.Close(byPlayer));
			};
			invDialog.TryOpen();
			capi.Network.SendPacketClient(Inventory.Open(byPlayer));
			capi.Network.SendBlockEntityPacket(Pos, 1000);
		}
		else
		{
			invDialog.TryClose();
		}
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			player.InventoryManager?.CloseInventory(Inventory);
			data = SerializerUtil.Serialize(new OpenContainerLidPacket(player.Entity.EntityId, opened: false));
			((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 5001, data, (IServerPlayer)player);
		}
		if (!Api.World.Claims.TryAccess(player, Pos, EnumBlockAccessFlags.Use))
		{
			Api.World.Logger.Audit("Player {0} sent an inventory packet to openable container at {1} but has no claim access. Rejected.", player.PlayerName, Pos);
		}
		else if (packetid < 1000)
		{
			Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		else if (packetid == 1000)
		{
			player.InventoryManager?.OpenInventory(Inventory);
			data = SerializerUtil.Serialize(new OpenContainerLidPacket(player.Entity.EntityId, opened: true));
			((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 5001, data, (IServerPlayer)player);
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
		if (packetid == 5000)
		{
			if (invDialog != null)
			{
				GuiDialogBlockEntity guiDialogBlockEntity = invDialog;
				if (guiDialogBlockEntity != null && guiDialogBlockEntity.IsOpened())
				{
					invDialog.TryClose();
				}
				invDialog?.Dispose();
				invDialog = null;
				return;
			}
			BlockEntityContainerOpen blockContainer = BlockEntityContainerOpen.FromBytes(data);
			Inventory.FromTreeAttributes(blockContainer.Tree);
			Inventory.ResolveBlocksOrItems();
			invDialog = new GuiDialogBlockEntityInventory(blockContainer.DialogTitle, Inventory, Pos, blockContainer.Columns, Api as ICoreClientAPI);
			Block block = Api.World.BlockAccessor.GetBlock(Pos);
			string os = block.Attributes?["openSound"]?.AsString();
			string cs = block.Attributes?["closeSound"]?.AsString();
			AssetLocation opensound = ((os == null) ? null : AssetLocation.Create(os, block.Code.Domain));
			AssetLocation closesound = ((cs == null) ? null : AssetLocation.Create(cs, block.Code.Domain));
			invDialog.OpenSound = opensound ?? OpenSound;
			invDialog.CloseSound = closesound ?? CloseSound;
			invDialog.TryOpen();
		}
		if (packetid == 5001)
		{
			OpenContainerLidPacket containerPacket = SerializerUtil.Deserialize<OpenContainerLidPacket>(data);
			if (this is BlockEntityGenericTypedContainer genericContainer)
			{
				if (containerPacket.Opened)
				{
					LidOpenEntityId.Add(containerPacket.EntityId);
					genericContainer.OpenLid();
				}
				else
				{
					LidOpenEntityId.Remove(containerPacket.EntityId);
					if (LidOpenEntityId.Count == 0)
					{
						genericContainer.CloseLid();
					}
				}
			}
		}
		if (packetid == 1001)
		{
			clientWorld.Player.InventoryManager.CloseInventory(Inventory);
			GuiDialogBlockEntity guiDialogBlockEntity2 = invDialog;
			if (guiDialogBlockEntity2 != null && guiDialogBlockEntity2.IsOpened())
			{
				invDialog?.TryClose();
			}
			invDialog?.Dispose();
			invDialog = null;
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		Dispose();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		Dispose();
	}

	public virtual void Dispose()
	{
		GuiDialogBlockEntity guiDialogBlockEntity = invDialog;
		if (guiDialogBlockEntity != null && guiDialogBlockEntity.IsOpened())
		{
			invDialog?.TryClose();
		}
		invDialog?.Dispose();
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
	}

	public override void DropContents(Vec3d atPos)
	{
		Inventory.DropAll(atPos);
	}
}
