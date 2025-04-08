using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityGuiConfigurableCommands : BlockEntityCommands
{
	protected GuiDialogBlockEntity clientDialog;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side != EnumAppSide.Server)
		{
			return;
		}
		JsonObject jsonObject = base.Block.Attributes["runOnInitialize"];
		if (jsonObject == null || !jsonObject.AsBool())
		{
			return;
		}
		RegisterDelayedCallback(delegate
		{
			try
			{
				Caller caller = new Caller
				{
					CallerPrivileges = new string[1] { "*" },
					Pos = Pos.ToVec3d(),
					Type = EnumCallerType.Block
				};
				OnInteract(caller);
			}
			catch (Exception e)
			{
				Api.Logger.Warning("Exception thrown when trying to call commands on init with block:");
				Api.Logger.Warning(e);
			}
		}, 2000);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (byItemStack != null)
		{
			JsonObject jsonObject = base.Block.Attributes["runOnInitialize"];
			if (jsonObject == null || !jsonObject.AsBool())
			{
				Commands = byItemStack?.Attributes.GetString("commands") ?? "";
				CallingPrivileges = (byItemStack?.Attributes["callingPrivileges"] as StringArrayAttribute)?.value;
			}
		}
	}

	public virtual bool OnInteract(Caller caller)
	{
		if (caller.Player != null && caller.Player.Entity.Controls.ShiftKey)
		{
			if (Api.Side == EnumAppSide.Client && caller.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && caller.Player.HasPrivilege("controlserver"))
			{
				if (clientDialog != null)
				{
					clientDialog.TryClose();
					clientDialog.Dispose();
					clientDialog = null;
					return true;
				}
				clientDialog = new GuiDialogBlockEntityCommand(Pos, Commands, Silent, Api as ICoreClientAPI, "Command editor");
				clientDialog.TryOpen();
				clientDialog.OnClosed += delegate
				{
					clientDialog?.Dispose();
					clientDialog = null;
				};
			}
			else
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "noprivilege", "Can only be edited in creative mode and with controlserver privlege");
			}
			return false;
		}
		Execute(caller, Commands);
		return true;
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(fromPlayer, packetid, data);
		if (packetid == 12 && CanEditCommandblocks(fromPlayer))
		{
			CallingPrivileges = ((!(fromPlayer as IServerPlayer).Role.AutoGrant) ? fromPlayer.Privileges : new string[1] { "*" });
			UpdateFromPacket(data);
		}
	}

	public static bool CanEditCommandblocks(IPlayer player)
	{
		if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			if (!player.HasPrivilege("controlserver"))
			{
				return player.Entity.World.Config.GetBool("allowCreativeModeCommandBlocks");
			}
			return true;
		}
		return false;
	}

	protected virtual void UpdateFromPacket(byte[] data)
	{
		BlockEntityCommandPacket packet = SerializerUtil.Deserialize<BlockEntityCommandPacket>(data);
		Commands = packet.Commands;
		Silent = packet.Silent;
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		clientDialog?.TryClose();
		clientDialog?.Dispose();
		clientDialog = null;
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		clientDialog?.TryClose();
		clientDialog?.Dispose();
		clientDialog = null;
	}
}
