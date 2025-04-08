using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemBlockReinforcement : ModSystem
{
	private ICoreAPI api;

	private IServerNetworkChannel serverChannel;

	private Dictionary<string, ReinforcedPrivilegeGrants> privGrantsByOwningPlayerUid = new Dictionary<string, ReinforcedPrivilegeGrants>();

	private Dictionary<int, ReinforcedPrivilegeGrantsGroup> privGrantsByOwningGroupUid = new Dictionary<int, ReinforcedPrivilegeGrantsGroup>();

	public bool reasonableReinforcements = true;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.RegisterItemClass("ItemPlumbAndSquare", typeof(ItemPlumbAndSquare));
		api.RegisterBlockBehaviorClass("Reinforcable", typeof(BlockBehaviorReinforcable));
	}

	public override void AssetsFinalize(ICoreAPI api)
	{
		if (api.Side == EnumAppSide.Server)
		{
			addReinforcementBehavior();
		}
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		api.Network.RegisterChannel("blockreinforcement").RegisterMessageType(typeof(ChunkReinforcementData)).RegisterMessageType(typeof(PrivGrantsData))
			.SetMessageHandler<ChunkReinforcementData>(onChunkData)
			.SetMessageHandler<PrivGrantsData>(onPrivData);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
		serverChannel = api.Network.RegisterChannel("blockreinforcement").RegisterMessageType(typeof(ChunkReinforcementData)).RegisterMessageType(typeof(PrivGrantsData));
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.Create("bre").WithDescription("Player owned Block reinforcement privilege management").RequiresPrivilege(Privilege.chat)
			.BeginSubCommand("grant")
			.WithDescription("Grant a player access to your block reinforcements")
			.WithArgs(parsers.Word("playername"), parsers.WordRange("flag", "all", "use"))
			.HandleWith(OnCmdGrant)
			.EndSubCommand()
			.BeginSubCommand("revoke")
			.WithDescription("Revoke access for a player to your block reinforcements")
			.WithArgs(parsers.Word("playername"))
			.HandleWith(OnCmdRevoke)
			.EndSubCommand()
			.BeginSubCommand("grantgroup")
			.WithDescription("Grant a group access to your block reinforcements")
			.WithArgs(parsers.Word("groupname"), parsers.WordRange("flag", "all", "use"))
			.HandleWith(OnCmdGrantGroup)
			.EndSubCommand()
			.BeginSubCommand("revokegroup")
			.WithDescription("Revoke access for a group to your block reinforcements")
			.WithArgs(parsers.Word("groupname"))
			.HandleWith(OnCmdRevokeGroup)
			.EndSubCommand();
		api.ChatCommands.Create("gbre").WithDescription("Group owned Block reinforcement privilege management").RequiresPrivilege(Privilege.chat)
			.BeginSubCommand("grant")
			.WithDescription("Grant a player access to your groups block reinforcements. Use default as group name to change the access type for members")
			.WithArgs(parsers.Word("playername"), parsers.WordRange("flag", "all", "use"))
			.HandleWith(OnCmdGroupGrant)
			.EndSubCommand()
			.BeginSubCommand("revoke")
			.WithDescription("Revoke a player access to your groups block reinforcements. Use default as group name to revoke the access type for goup members")
			.WithArgs(parsers.Word("playername"))
			.HandleWith(OnCmdGroupRevoke)
			.EndSubCommand()
			.BeginSubCommand("grantgroup")
			.WithDescription("Grant an other group access to your groups block reinforcements")
			.WithArgs(parsers.Word("groupname"), parsers.WordRange("flag", "all", "use"))
			.HandleWith(OnCmdGroupGrantGroup)
			.EndSubCommand()
			.BeginSubCommand("revokegroup")
			.WithDescription("Revoke an others groups access to your groups block reinforcements")
			.WithArgs(parsers.Word("groupname"))
			.HandleWith(OnCmdGroupRevokeGroup)
			.EndSubCommand();
		api.Permissions.RegisterPrivilege("denybreakreinforced", "Deny the ability to break reinforced blocks", adminAutoGrant: false);
	}

	private TextCommandResult OnCmdGroupRevokeGroup(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int groupId = args.Caller.FromChatGroupId;
		string groupName = args.Parsers[0].GetValue() as string;
		PlayerGroupMembership group = player.GetGroup(groupId);
		if (group == null)
		{
			return TextCommandResult.Success("Type this command inside the group chat tab that you are a owner of");
		}
		if (group.Level != EnumPlayerGroupMemberShip.Owner)
		{
			return TextCommandResult.Success("Must be owner of the group to change access flags");
		}
		if (!privGrantsByOwningGroupUid.TryGetValue(groupId, out var groupGrants))
		{
			groupGrants = (privGrantsByOwningGroupUid[groupId] = new ReinforcedPrivilegeGrantsGroup());
		}
		return GrantRevokeGroupOwned2Group(player, groupId, args.Command.Name, groupName, "none", EnumBlockAccessFlags.None, groupGrants);
	}

	private TextCommandResult OnCmdGroupGrantGroup(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int groupId = args.Caller.FromChatGroupId;
		string groupName = args.Parsers[0].GetValue() as string;
		string flagString = args.Parsers[1].GetValue() as string;
		PlayerGroupMembership group = player.GetGroup(groupId);
		if (group == null)
		{
			return TextCommandResult.Success("Type this command inside the group chat tab that you are a owner of");
		}
		if (group.Level != EnumPlayerGroupMemberShip.Owner)
		{
			return TextCommandResult.Success("Must be owner of the group to change access flags");
		}
		EnumBlockAccessFlags flags = GetFlags(flagString);
		if (!privGrantsByOwningGroupUid.TryGetValue(groupId, out var groupGrants))
		{
			groupGrants = (privGrantsByOwningGroupUid[groupId] = new ReinforcedPrivilegeGrantsGroup());
		}
		return GrantRevokeGroupOwned2Group(player, groupId, args.Command.Name, groupName, flagString, flags, groupGrants);
	}

	private static EnumBlockAccessFlags GetFlags(string flagString)
	{
		EnumBlockAccessFlags flags = EnumBlockAccessFlags.None;
		if (flagString != null)
		{
			if (flagString.ToLowerInvariant() == "use")
			{
				flags = EnumBlockAccessFlags.Use;
			}
			if (flagString.ToLowerInvariant() == "all")
			{
				flags = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use;
			}
		}
		return flags;
	}

	private TextCommandResult OnCmdGroupRevoke(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int groupId = args.Caller.FromChatGroupId;
		string playerName = args.Parsers[0].GetValue() as string;
		PlayerGroupMembership group = player.GetGroup(groupId);
		if (group == null)
		{
			return TextCommandResult.Success("Type this command inside the group chat tab that you are a owner of");
		}
		if (group.Level != EnumPlayerGroupMemberShip.Owner)
		{
			return TextCommandResult.Success("Must be owner of the group to change access flags");
		}
		if (!privGrantsByOwningGroupUid.TryGetValue(groupId, out var groupGrants))
		{
			groupGrants = (privGrantsByOwningGroupUid[groupId] = new ReinforcedPrivilegeGrantsGroup());
		}
		if (playerName == "default")
		{
			groupGrants.DefaultGrants = EnumBlockAccessFlags.None;
			SyncPrivData();
			return TextCommandResult.Success("All access revoked for group members");
		}
		GrantRevokeGroupOwned2Player(player, groupId, args.Command.Name, playerName, "none", EnumBlockAccessFlags.None, groupGrants);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGroupGrant(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int groupId = args.Caller.FromChatGroupId;
		string playerName = args.Parsers[0].GetValue() as string;
		string flagString = args.Parsers[1].GetValue() as string;
		PlayerGroupMembership group = player.GetGroup(groupId);
		if (group == null)
		{
			return TextCommandResult.Success("Type this command inside the group chat tab that you are a owner of");
		}
		if (group.Level != EnumPlayerGroupMemberShip.Owner)
		{
			return TextCommandResult.Success("Must be owner of the group to change access flags");
		}
		EnumBlockAccessFlags flags = GetFlags(flagString);
		if (!privGrantsByOwningGroupUid.TryGetValue(groupId, out var groupGrants))
		{
			groupGrants = (privGrantsByOwningGroupUid[groupId] = new ReinforcedPrivilegeGrantsGroup());
		}
		if (playerName == "default")
		{
			groupGrants.DefaultGrants = flags;
			SyncPrivData();
			return TextCommandResult.Success("Default access for group members set to " + flagString);
		}
		GrantRevokeGroupOwned2Player(player, groupId, args.Command.Name, playerName, flagString, flags, groupGrants);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGrantGroup(TextCommandCallingArgs args)
	{
		string groupName = args.Parsers[0].GetValue() as string;
		EnumBlockAccessFlags flags = GetFlags(args.Parsers[1].GetValue() as string);
		return SetGroupPrivilege(args.Caller.Player as IServerPlayer, groupName, flags);
	}

	private TextCommandResult OnCmdRevokeGroup(TextCommandCallingArgs args)
	{
		string groupName = args.Parsers[0].GetValue() as string;
		return SetGroupPrivilege(args.Caller.Player as IServerPlayer, groupName, EnumBlockAccessFlags.None);
	}

	private TextCommandResult OnCmdGrant(TextCommandCallingArgs args)
	{
		string playerName = args.Parsers[0].GetValue() as string;
		string flagString = args.Parsers[1].GetValue() as string;
		ICoreServerAPI sapi = api as ICoreServerAPI;
		EnumBlockAccessFlags flags = GetFlags(flagString);
		IServerPlayerData plrData = null;
		plrData = sapi.PlayerData.GetPlayerDataByLastKnownName(playerName);
		if (plrData == null)
		{
			return TextCommandResult.Success("No player with such name found or never connected to this server");
		}
		return SetPlayerPrivilege(args.Caller.Player as IServerPlayer, plrData.PlayerUID, flags);
	}

	private TextCommandResult OnCmdRevoke(TextCommandCallingArgs args)
	{
		string playerName = args.Parsers[0].GetValue() as string;
		IServerPlayerData plrData = (api as ICoreServerAPI).PlayerData.GetPlayerDataByLastKnownName(playerName);
		if (plrData == null)
		{
			return TextCommandResult.Success("No player with such name found or never connected to this server");
		}
		return SetPlayerPrivilege(args.Caller.Player as IServerPlayer, plrData.PlayerUID, EnumBlockAccessFlags.None);
	}

	protected void GrantRevokeGroupOwned2Player(IServerPlayer player, int groupId, string command, string playername, string flagString, EnumBlockAccessFlags flags, ReinforcedPrivilegeGrantsGroup groupGrants)
	{
		(api as ICoreServerAPI).PlayerData.ResolvePlayerName(playername, delegate(EnumServerResponse result, string playeruid)
		{
			switch (result)
			{
			case EnumServerResponse.Good:
				if (command == "grant")
				{
					groupGrants.PlayerGrants[playeruid] = flags;
					player.SendMessage(groupId, flagString + " access set for player " + playername, EnumChatType.CommandError);
					SyncPrivData();
				}
				else if (groupGrants.PlayerGrants.Remove(playeruid))
				{
					player.SendMessage(groupId, "All access revoked for player " + playername, EnumChatType.CommandError);
					SyncPrivData();
				}
				else
				{
					player.SendMessage(groupId, "This player has no access. No action taken.", EnumChatType.CommandError);
				}
				break;
			case EnumServerResponse.Offline:
				player.SendMessage(groupId, Lang.Get("Player with name '{0}' is not online and auth server is offline. Cannot check if this player exists. Try again later.", playername), EnumChatType.CommandError);
				break;
			default:
				player.SendMessage(groupId, Lang.Get("No player with name '{0}' exists", playername), EnumChatType.CommandError);
				break;
			}
		});
	}

	protected TextCommandResult GrantRevokeGroupOwned2Group(IServerPlayer player, int groupId, string command, string groupname, string flagString, EnumBlockAccessFlags flags, ReinforcedPrivilegeGrantsGroup groupGrants)
	{
		PlayerGroup group = (api as ICoreServerAPI).Groups.GetPlayerGroupByName(groupname);
		if (group == null)
		{
			return TextCommandResult.Success(Lang.Get("No group with name '{0}' exists", groupname));
		}
		string msg;
		if (command == "grant")
		{
			groupGrants.GroupGrants[group.Uid] = flags;
			msg = flagString + " access set for group " + groupname;
			SyncPrivData();
		}
		else if (groupGrants.GroupGrants.Remove(group.Uid))
		{
			msg = "All access revoked for group " + groupname;
		}
		else
		{
			msg = "This group has no access. No action taken.";
			SyncPrivData();
		}
		return TextCommandResult.Success(msg);
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		serverChannel?.SendPacket(new PrivGrantsData
		{
			privGrantsByOwningPlayerUid = privGrantsByOwningPlayerUid,
			privGrantsByOwningGroupUid = privGrantsByOwningGroupUid
		}, byPlayer);
	}

	private void onChunkData(ChunkReinforcementData msg)
	{
		api.World.BlockAccessor.GetChunk(msg.chunkX, msg.chunkY, msg.chunkZ)?.SetModdata("reinforcements", msg.Data);
	}

	private void onPrivData(PrivGrantsData networkMessage)
	{
		privGrantsByOwningPlayerUid = networkMessage.privGrantsByOwningPlayerUid;
		privGrantsByOwningGroupUid = networkMessage.privGrantsByOwningGroupUid;
	}

	private void SyncPrivData()
	{
		serverChannel?.BroadcastPacket(new PrivGrantsData
		{
			privGrantsByOwningPlayerUid = privGrantsByOwningPlayerUid,
			privGrantsByOwningGroupUid = privGrantsByOwningGroupUid
		});
	}

	private void Event_GameWorldSave()
	{
		(api as ICoreServerAPI).WorldManager.SaveGame.StoreData("blockreinforcementprivileges", SerializerUtil.Serialize(privGrantsByOwningPlayerUid));
		(api as ICoreServerAPI).WorldManager.SaveGame.StoreData("blockreinforcementprivilegesgroup", SerializerUtil.Serialize(privGrantsByOwningGroupUid));
	}

	private void Event_SaveGameLoaded()
	{
		byte[] data = (api as ICoreServerAPI).WorldManager.SaveGame.GetData("blockreinforcementprivileges");
		if (data != null)
		{
			try
			{
				privGrantsByOwningPlayerUid = SerializerUtil.Deserialize<Dictionary<string, ReinforcedPrivilegeGrants>>(data);
			}
			catch
			{
				api.World.Logger.Notification("Unable to load player->group privileges for the block reinforcement system. Exception thrown when trying to deserialize it. Will be discarded.");
			}
		}
		data = (api as ICoreServerAPI).WorldManager.SaveGame.GetData("blockreinforcementprivilegesgroup");
		if (data != null)
		{
			try
			{
				privGrantsByOwningGroupUid = SerializerUtil.Deserialize<Dictionary<int, ReinforcedPrivilegeGrantsGroup>>(data);
			}
			catch
			{
				api.World.Logger.Notification("Unable to load group->player privileges for the block reinforcement system. Exception thrown when trying to deserialize it. Will be discarded.");
			}
		}
	}

	private void addReinforcementBehavior()
	{
		foreach (Block block in api.World.Blocks)
		{
			if (!(block.Code == null) && block.Id != 0 && IsReinforcable(block))
			{
				block.BlockBehaviors = block.BlockBehaviors.Append(new BlockBehaviorReinforcable(block));
				block.CollectibleBehaviors = block.CollectibleBehaviors.Append(new BlockBehaviorReinforcable(block));
			}
		}
	}

	protected bool IsReinforcable(Block block)
	{
		if (reasonableReinforcements && (block.BlockMaterial == EnumBlockMaterial.Plant || block.BlockMaterial == EnumBlockMaterial.Liquid || block.BlockMaterial == EnumBlockMaterial.Snow || block.BlockMaterial == EnumBlockMaterial.Leaves || block.BlockMaterial == EnumBlockMaterial.Lava || block.BlockMaterial == EnumBlockMaterial.Sand || block.BlockMaterial == EnumBlockMaterial.Gravel) && (block.Attributes == null || !block.Attributes["reinforcable"].AsBool()))
		{
			return false;
		}
		if (block.Attributes == null || block.Attributes["reinforcable"].AsBool(defaultValue: true))
		{
			return true;
		}
		return false;
	}

	public ItemSlot FindResourceForReinforcing(IPlayer byPlayer)
	{
		ItemSlot foundSlot = null;
		byPlayer.Entity.WalkInventory(delegate(ItemSlot onSlot)
		{
			if (onSlot.Itemstack == null || onSlot.Itemstack.ItemAttributes == null)
			{
				return true;
			}
			if (onSlot is ItemSlotCreative)
			{
				return true;
			}
			if (!(onSlot.Inventory is InventoryBasePlayer))
			{
				return true;
			}
			if (new int?(onSlot.Itemstack.ItemAttributes["reinforcementStrength"].AsInt()) > 0)
			{
				foundSlot = onSlot;
				return false;
			}
			return true;
		});
		return foundSlot;
	}

	public bool TryRemoveReinforcement(BlockPos pos, IPlayer forPlayer, ref string errorCode)
	{
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		if (reinforcmentsOfChunk == null)
		{
			return false;
		}
		int index3d = toLocalIndex(pos);
		if (!reinforcmentsOfChunk.ContainsKey(index3d))
		{
			errorCode = "notreinforced";
			return false;
		}
		BlockReinforcement bre = reinforcmentsOfChunk[index3d];
		PlayerGroupMembership group = forPlayer.GetGroup(bre.GroupUid);
		if (bre.PlayerUID != forPlayer.PlayerUID && group == null && (GetAccessFlags(bre.PlayerUID, bre.GroupUid, forPlayer) & EnumBlockAccessFlags.BuildOrBreak) == 0)
		{
			errorCode = "notownblock";
			return false;
		}
		reinforcmentsOfChunk.Remove(index3d);
		SaveReinforcments(reinforcmentsOfChunk, pos);
		return true;
	}

	public bool IsReinforced(BlockPos pos)
	{
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		if (reinforcmentsOfChunk == null)
		{
			return false;
		}
		int index3d = toLocalIndex(pos);
		return reinforcmentsOfChunk.ContainsKey(index3d);
	}

	public bool IsLockedForInteract(BlockPos pos, IPlayer forPlayer)
	{
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		if (reinforcmentsOfChunk == null)
		{
			return false;
		}
		int index3d = toLocalIndex(pos);
		if (reinforcmentsOfChunk.TryGetValue(index3d, out var bre) && bre.Locked && bre.PlayerUID != forPlayer.PlayerUID && forPlayer.GetGroup(bre.GroupUid) == null)
		{
			if ((GetAccessFlags(bre.PlayerUID, bre.GroupUid, forPlayer) & EnumBlockAccessFlags.Use) > EnumBlockAccessFlags.None)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public EnumBlockAccessFlags GetAccessFlags(string owningPlayerUid, int owningGroupId, IPlayer forPlayer)
	{
		if (owningPlayerUid == forPlayer.PlayerUID)
		{
			return EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use;
		}
		PlayerGroupMembership group = forPlayer.GetGroup(owningGroupId);
		if (group != null)
		{
			return EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use;
		}
		EnumBlockAccessFlags flags = EnumBlockAccessFlags.None;
		if (owningPlayerUid != null && privGrantsByOwningPlayerUid.TryGetValue(owningPlayerUid, out var grants))
		{
			grants.PlayerGrants.TryGetValue(forPlayer.PlayerUID, out flags);
			foreach (KeyValuePair<int, EnumBlockAccessFlags> val2 in grants.GroupGrants)
			{
				if (forPlayer.GetGroup(val2.Key) != null)
				{
					flags |= val2.Value;
				}
			}
		}
		if (owningGroupId != 0 && privGrantsByOwningGroupUid.TryGetValue(owningGroupId, out var grantsgr))
		{
			if (group != null)
			{
				grantsgr.PlayerGrants.TryGetValue(forPlayer.PlayerUID, out flags);
				flags |= grantsgr.DefaultGrants;
			}
			foreach (KeyValuePair<int, EnumBlockAccessFlags> val in grantsgr.GroupGrants)
			{
				if (forPlayer.GetGroup(val.Key) != null)
				{
					flags |= val.Value;
				}
			}
		}
		return flags;
	}

	public bool TryLock(BlockPos pos, IPlayer byPlayer, string itemCode)
	{
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		if (reinforcmentsOfChunk == null)
		{
			return false;
		}
		int index3d = toLocalIndex(pos);
		if (reinforcmentsOfChunk.TryGetValue(index3d, out var bre))
		{
			PlayerGroupMembership membership = byPlayer.GetGroup(bre.GroupUid);
			bool isAllowed = bre.PlayerUID == byPlayer.PlayerUID;
			if (membership != null)
			{
				isAllowed |= membership.Level == EnumPlayerGroupMemberShip.Owner;
				isAllowed |= membership.Level == EnumPlayerGroupMemberShip.Op;
			}
			if (!isAllowed || bre.Locked)
			{
				return false;
			}
			bre.Locked = true;
			bre.LockedByItemCode = itemCode;
			SaveReinforcments(reinforcmentsOfChunk, pos);
			return true;
		}
		reinforcmentsOfChunk[index3d] = new BlockReinforcement
		{
			PlayerUID = byPlayer.PlayerUID,
			LastPlayername = byPlayer.PlayerName,
			Strength = 0,
			Locked = true,
			LockedByItemCode = itemCode
		};
		SaveReinforcments(reinforcmentsOfChunk, pos);
		return true;
	}

	public BlockReinforcement GetReinforcment(BlockPos pos)
	{
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		if (reinforcmentsOfChunk == null)
		{
			return null;
		}
		int index3d = toLocalIndex(pos);
		if (!reinforcmentsOfChunk.ContainsKey(index3d))
		{
			return null;
		}
		return reinforcmentsOfChunk[index3d];
	}

	public void ConsumeStrength(BlockPos pos, int byAmount)
	{
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		if (reinforcmentsOfChunk == null)
		{
			return;
		}
		int index3d = toLocalIndex(pos);
		if (reinforcmentsOfChunk.ContainsKey(index3d))
		{
			reinforcmentsOfChunk[index3d].Strength -= byAmount;
			if (reinforcmentsOfChunk[index3d].Strength <= 0)
			{
				reinforcmentsOfChunk.Remove(index3d);
			}
			SaveReinforcments(reinforcmentsOfChunk, pos);
		}
	}

	public void ClearReinforcement(BlockPos pos)
	{
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		if (reinforcmentsOfChunk != null)
		{
			int index3d = toLocalIndex(pos);
			if (reinforcmentsOfChunk.ContainsKey(index3d) && reinforcmentsOfChunk.Remove(index3d))
			{
				SaveReinforcments(reinforcmentsOfChunk, pos);
			}
		}
	}

	public bool StrengthenBlock(BlockPos pos, IPlayer byPlayer, int strength, int forGroupUid = 0)
	{
		if (api.Side == EnumAppSide.Client)
		{
			return false;
		}
		if (!api.World.BlockAccessor.GetBlock(pos, 1).HasBehavior<BlockBehaviorReinforcable>())
		{
			return false;
		}
		Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = getOrCreateReinforcmentsAt(pos);
		int index3d = toLocalIndex(pos);
		if (reinforcmentsOfChunk.ContainsKey(index3d))
		{
			BlockReinforcement bre = reinforcmentsOfChunk[index3d];
			if (bre.Strength > 0)
			{
				return false;
			}
			bre.Strength = strength;
		}
		else
		{
			string grpname = null;
			if ((api as ICoreServerAPI).Groups.PlayerGroupsById.TryGetValue(forGroupUid, out var grp))
			{
				grpname = grp.Name;
			}
			reinforcmentsOfChunk[index3d] = new BlockReinforcement
			{
				PlayerUID = ((forGroupUid == 0) ? byPlayer.PlayerUID : null),
				GroupUid = forGroupUid,
				LastPlayername = byPlayer.PlayerName,
				LastGroupname = grpname,
				Strength = strength
			};
		}
		SaveReinforcments(reinforcmentsOfChunk, pos);
		return true;
	}

	private Dictionary<int, BlockReinforcement> getOrCreateReinforcmentsAt(BlockPos pos)
	{
		IWorldChunk chunk = api.World.BlockAccessor.GetChunkAtBlockPos(pos);
		if (chunk == null)
		{
			return null;
		}
		byte[] data = chunk.GetModdata("reinforcements");
		if (data != null)
		{
			try
			{
				return SerializerUtil.Deserialize<Dictionary<int, BlockReinforcement>>(data);
			}
			catch (Exception)
			{
				try
				{
					api.World.Logger.Warning("Failed reading block reinforcments at block position, maybe old format. Will attempt to convert.");
					Dictionary<int, BlockReinforcementOld> dictionary = SerializerUtil.Deserialize<Dictionary<int, BlockReinforcementOld>>(data);
					Dictionary<int, BlockReinforcement> reinforcmentsOfChunk = new Dictionary<int, BlockReinforcement>();
					foreach (KeyValuePair<int, BlockReinforcementOld> val in dictionary)
					{
						reinforcmentsOfChunk[val.Key] = val.Value.Update();
					}
					SaveReinforcments(reinforcmentsOfChunk, pos);
					api.World.Logger.Warning("Ok, converted");
				}
				catch (Exception e2)
				{
					api.World.Logger.VerboseDebug("Failed reading block reinforcments at block position {0}, will discard, sorry.", pos);
					api.World.Logger.VerboseDebug(LoggerBase.CleanStackTrace(e2.ToString()));
				}
				return new Dictionary<int, BlockReinforcement>();
			}
		}
		return new Dictionary<int, BlockReinforcement>();
	}

	private void SaveReinforcments(Dictionary<int, BlockReinforcement> reif, BlockPos pos)
	{
		int chunkX = pos.X / 32;
		int chunkY = pos.Y / 32;
		int chunkZ = pos.Z / 32;
		byte[] data = SerializerUtil.Serialize(reif);
		api.World.BlockAccessor.GetChunk(chunkX, chunkY, chunkZ).SetModdata("reinforcements", data);
		serverChannel?.BroadcastPacket(new ChunkReinforcementData
		{
			chunkX = chunkX,
			chunkY = chunkY,
			chunkZ = chunkZ,
			Data = data
		});
	}

	public TextCommandResult SetPlayerPrivilege(IServerPlayer owningPlayer, string forPlayerUid, EnumBlockAccessFlags access)
	{
		if (!privGrantsByOwningPlayerUid.TryGetValue(owningPlayer.PlayerUID, out var grants))
		{
			grants = new ReinforcedPrivilegeGrants();
			privGrantsByOwningPlayerUid[owningPlayer.PlayerUID] = grants;
		}
		string msg;
		if (access == EnumBlockAccessFlags.None)
		{
			msg = ((!grants.PlayerGrants.Remove(forPlayerUid)) ? Lang.Get("No action taken. Player does not have any privilege to your reinforced blocks.") : Lang.Get("Ok, privilege revoked from player."));
		}
		else
		{
			grants.PlayerGrants[forPlayerUid] = access;
			msg = Lang.Get("Ok, Privilege for player set.");
		}
		SyncPrivData();
		return TextCommandResult.Success(msg);
	}

	public TextCommandResult SetGroupPrivilege(IServerPlayer owningPlayer, string forGroupName, EnumBlockAccessFlags access)
	{
		if (!privGrantsByOwningPlayerUid.TryGetValue(owningPlayer.PlayerUID, out var grants))
		{
			grants = new ReinforcedPrivilegeGrants();
			privGrantsByOwningPlayerUid[owningPlayer.PlayerUID] = grants;
		}
		PlayerGroup group = (api as ICoreServerAPI).Groups.GetPlayerGroupByName(forGroupName);
		if (group == null)
		{
			return TextCommandResult.Success(Lang.Get("No such group found"));
		}
		string msg;
		if (access == EnumBlockAccessFlags.None)
		{
			msg = ((!grants.GroupGrants.Remove(group.Uid)) ? Lang.Get("No action taken. Group does not have any privilege to your reinforced blocks.") : Lang.Get("Ok, privilege revoked from group."));
		}
		else
		{
			grants.GroupGrants[group.Uid] = access;
			msg = Lang.Get("Ok, Privilege for group set.");
		}
		SyncPrivData();
		return TextCommandResult.Success(msg);
	}

	private int toLocalIndex(BlockPos pos)
	{
		return toLocalIndex(pos.X % 32, pos.Y % 32, pos.Z % 32);
	}

	private int toLocalIndex(int x, int y, int z)
	{
		return (y << 16) | (z << 8) | x;
	}

	private Vec3i fromLocalIndex(int index)
	{
		return new Vec3i(index & 0xFF, (index >> 16) & 0xFF, (index >> 8) & 0xFF);
	}
}
