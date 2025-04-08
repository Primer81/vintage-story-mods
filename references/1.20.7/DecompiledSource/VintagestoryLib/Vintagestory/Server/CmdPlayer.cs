using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server;

public class CmdPlayer : ServerSystem
{
	public delegate TextCommandResult PlayerEachDelegate(PlayerUidName targetPlayer, TextCommandCallingArgs args);

	private bool ConfigNeedsSaving
	{
		get
		{
			return server.ConfigNeedsSaving;
		}
		set
		{
			server.ConfigNeedsSaving = value;
		}
	}

	private ServerConfig Config => server.Config;

	public CmdPlayer(ServerMain server)
		: base(server)
	{
		CmdPlayer cmdPlayer = this;
		IChatCommandApi cmdapi = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		string[] gameModes = new string[10] { "0", "1", "2", "3", "4", "creative", "survival", "spectator", "guest", "abbreviated game mode names are valid as well" };
		cmdapi.GetOrCreate("mystats").WithDescription("shows players stats").RequiresPrivilege(Privilege.chat)
			.RequiresPlayer()
			.HandleWith(OnCmdMyStats)
			.Validate();
		cmdapi.GetOrCreate("whitelist").WithDesc("Whitelist control").RequiresPrivilege(Privilege.whitelist)
			.BeginSub("add")
			.WithDesc("Add a player to the whitelist")
			.WithArgs(parsers.PlayerUids("player"), parsers.OptionalAll("optional reason"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, delegate(PlayerUidName targetPlayer, TextCommandCallingArgs args)
			{
				if (server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid))
				{
					return TextCommandResult.Error("Player is already whitelisted");
				}
				string reason = (string)args[1];
				server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
				string byPlayername = ((args.Caller.Player != null) ? args.Caller.Player.PlayerName : args.Caller.Type.ToString());
				DateTime dateTime = DateTime.Now.AddYears(50);
				server.PlayerDataManager.WhitelistPlayer(targetPlayer.Name, targetPlayer.Uid, byPlayername, reason, dateTime);
				return TextCommandResult.Success(Lang.Get("Player is now whitelisted until {0}", dateTime));
			}))
			.EndSub()
			.BeginSub("remove")
			.WithDesc("Remove a player from the whitelist")
			.WithArgs(parsers.PlayerUids("player"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, delegate(PlayerUidName targetPlayer, TextCommandCallingArgs args)
			{
				if (!server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid))
				{
					return TextCommandResult.Error("Player is not whitelisted");
				}
				server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
				server.PlayerDataManager.UnWhitelistPlayer(targetPlayer.Name, targetPlayer.Uid);
				return TextCommandResult.Success(Lang.Get("Player is now removed from the whitelist"));
			}))
			.EndSub()
			.BeginSub("on")
			.WithDesc("Enable whitelist system. Only whitelisted players can join")
			.HandleWith(delegate
			{
				if (server.Config.WhitelistMode == EnumWhitelistMode.On)
				{
					return TextCommandResult.Error(Lang.Get("Whitelist was already enabled"));
				}
				server.Config.WhitelistMode = EnumWhitelistMode.On;
				return TextCommandResult.Success(Lang.Get("Whitelist now enabled"));
			})
			.EndSub()
			.BeginSub("off")
			.HandleWith(delegate
			{
				if (server.Config.WhitelistMode == EnumWhitelistMode.Off)
				{
					return TextCommandResult.Error(Lang.Get("Whitelist was already disabled"));
				}
				server.Config.WhitelistMode = EnumWhitelistMode.Off;
				return TextCommandResult.Success(Lang.Get("Whitelist now disabled"));
			})
			.WithDesc("Disable whitelist system. All players can join")
			.EndSub()
			.Validate();
		cmdapi.GetOrCreate("player").WithDesc("Player control").WithArgs(parsers.PlayerUids("player"))
			.RequiresPrivilege(Privilege.chat)
			.BeginSub("movespeed")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Set a player's move speed")
			.WithArgs(parsers.Float("movespeed"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.setMovespeed))
			.EndSub()
			.BeginSub("whitelist")
			.RequiresPrivilege(Privilege.whitelist)
			.WithDesc("Add/remove player to/from the whitelist")
			.WithArgs(parsers.OptionalBool("add/remove", "add"), parsers.OptionalAll("optional reason"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.addRemoveWhitelist))
			.EndSub()
			.BeginSub("privilege")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Player privilege control")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.listPrivilege))
			.BeginSub("grant")
			.WithDesc("Grant a privilege to a player")
			.WithArgs(parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.grantPrivilege))
			.EndSub()
			.BeginSub("revoke")
			.WithArgs(parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")))
			.WithDesc("Revoke a privilege from a player")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.revokePrivilege))
			.EndSub()
			.BeginSub("deny")
			.WithArgs(parsers.Privilege("privilege_name"))
			.WithDesc("Deny a privilege to a player that was ordinarily granted from a role")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.denyPrivilege))
			.EndSub()
			.BeginSub("removedeny")
			.WithArgs(parsers.Privilege("privilege_name"))
			.WithDesc("Remove a previous privilege denial from a player")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.removeDenyPrivilege))
			.EndSub()
			.EndSub()
			.BeginSub("role")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Set or get a player role")
			.WithArgs(parsers.OptionalPlayerRole("role"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.GetSetRole))
			.EndSub()
			.BeginSub("stats")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Display player parameters")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.getStats))
			.EndSub()
			.BeginSub("entity")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Get/Set an attribute value on the player entity")
			.WithArgs(parsers.OptionalWord("attribute_name"), parsers.OptionalFloat("attribute value"))
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleEntity))
			.EndSub()
			.BeginSub("wipedata")
			.RequiresPrivilege(Privilege.controlserver)
			.WithDesc("Wipe the player data, such as the entire inventory, skin/class, etc.")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.WipePlayerData))
			.EndSub()
			.BeginSub("clearinv")
			.RequiresPrivilege(Privilege.controlserver)
			.WithDesc("Clear the player's entire inventory")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.WipePlayerInventory))
			.EndSub()
			.BeginSub("gamemode")
			.WithAlias("gm")
			.WithArgs(parsers.OptionalWordRange("mode", gameModes))
			.RequiresPrivilege(Privilege.gamemode)
			.WithDesc("Set (or discover) the player(s) game mode")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.getSetGameMode))
			.EndSub()
			.BeginSub("allowcharselonce")
			.WithAlias("acso")
			.RequiresPrivilege(Privilege.grantrevoke)
			.WithDesc("Allow changing character class and skin one more time")
			.WithAdditionalInformation("Allows the player to run the <code>.charsel</code> command client-side")
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleCharSel))
			.EndSub()
			.BeginSub("landclaimallowance")
			.WithAlias("lca")
			.WithArgs(parsers.OptionalInt("amount"))
			.WithDesc("Get/Set land claim allowance")
			.WithAdditionalInformation("Specifies the amount of land a player can claim, in m³")
			.RequiresPrivilege(Privilege.grantrevoke)
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleLandClaimAllowance))
			.EndSub()
			.BeginSub("landclaimmaxareas")
			.WithAlias("lcma")
			.WithArgs(parsers.OptionalInt("number"))
			.WithDesc("Get/Set land claim max areas")
			.WithAdditionalInformation("Specifies the maximum number of separate land areas a player can claim")
			.RequiresPrivilege(Privilege.grantrevoke)
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.handleLandClaimMaxAreas))
			.EndSub()
			.Validate();
		cmdapi.Create("op").WithDesc("Give a player admin status. Shorthand for /player &lt;playername&gt; role admin").WithArgs(parsers.PlayerUids("playername"))
			.RequiresPrivilege(Privilege.grantrevoke)
			.HandleWith((TextCommandCallingArgs args) => Each(args, cmdPlayer.opPlayer))
			.Validate();
		cmdapi.Create("self").WithDesc("Information about your player").RequiresPrivilege(Privilege.chat)
			.BeginSub("stats")
			.WithDesc("Full stats")
			.HandleWith((TextCommandCallingArgs args) => cmdPlayer.getStats(new PlayerUidName(args.Caller.Player?.PlayerUID, args.Caller.Player?.PlayerName), args))
			.EndSub()
			.BeginSub("privileges")
			.WithDesc("Your current privileges")
			.HandleWith((TextCommandCallingArgs args) => cmdPlayer.listPrivilege(new PlayerUidName(args.Caller.Player?.PlayerUID, args.Caller.Player?.PlayerName), args))
			.EndSub()
			.BeginSub("role")
			.WithDesc("Your current role")
			.HandleWith((TextCommandCallingArgs args) => cmdPlayer.GetSetRole(new PlayerUidName(args.Caller.Player?.PlayerUID, args.Caller.Player?.PlayerName), args))
			.EndSub()
			.BeginSub("gamemode")
			.WithDesc("Your current game mode")
			.HandleWith(handleGameMode)
			.EndSub()
			.BeginSub("clearinv")
			.RequiresPrivilege(Privilege.gamemode)
			.WithRootAlias("clearinv")
			.WithDesc("Empties your inventory")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				args.Caller.Player?.InventoryManager.DiscardAll();
				return TextCommandResult.Success();
			})
			.EndSub()
			.BeginSub("kill")
			.RequiresPrivilege(Privilege.selfkill)
			.WithRootAlias("kill")
			.WithDesc("Kill yourself")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				args.Caller.Entity.Die(EnumDespawnReason.Death, new DamageSource
				{
					Source = EnumDamageSource.Suicide
				});
				return TextCommandResult.Success();
			})
			.EndSub()
			.Validate();
		cmdapi.Create("gamemode").WithAlias("gm").WithDesc("Get/Set one players game mode. Omit playername arg to get/set your own game mode")
			.RequiresPrivilege(Privilege.chat)
			.WithArgs(parsers.Unparsed("playername"), parsers.Unparsed("mode", gameModes))
			.HandleWith(handleGameMode)
			.Validate();
		cmdapi.Create("role").RequiresPrivilege(Privilege.controlserver).WithDescription("Modify/See player role related data")
			.WithArgs(parsers.PlayerRole("rolename"))
			.BeginSub("landclaimallowance")
			.WithAlias("lca")
			.WithDescription("Get/Set land claim allowance m³")
			.WithArgs(parsers.OptionalInt("landClaimAllowance", -1))
			.HandleWith(OnLandclaimallowanceCmd)
			.EndSub()
			.BeginSub("landclaimminsize")
			.WithAlias("lcms")
			.WithDescription("Get/Set land claim minimum size")
			.WithArgs(parsers.OptionalVec3i("minSize"))
			.HandleWith(OnLandclaimminsizeCmd)
			.EndSub()
			.BeginSub("landclaimmaxareas")
			.WithAlias("lcma")
			.WithDescription("Get/Set land claim maximum areas")
			.WithArgs(parsers.OptionalInt("area", -1))
			.HandleWith(OnLandclaimmaxareasCmd)
			.EndSub()
			.BeginSub("privilege")
			.WithDescription("Show privileges for role")
			.HandleWith(OnPrivilegeCmd)
			.BeginSub("grant")
			.WithDescription("Grant a privilege")
			.WithArgs(parsers.Word("privilege_name", Privilege.AllCodes().Append("or custom defined privileges")))
			.HandleWith(OnGrantCmd)
			.EndSub()
			.BeginSub("revoke")
			.WithDescription("Revoke  a privilege")
			.WithArgs(parsers.Privilege("privilege_name"))
			.HandleWith(OnRevokeCmd)
			.EndSub()
			.EndSub()
			.BeginSub("spawnpoint")
			.WithDescription("Get/Set/Unset the default spawnpoint")
			.HandleWith(OnSpawnpointCmd)
			.BeginSub("set")
			.WithDescription("Set the default spawnpoint")
			.WithArgs(parsers.WorldPosition("pos"))
			.HandleWith(OnSpawnpointSetCmd)
			.EndSub()
			.BeginSub("unset")
			.WithDesc("Unset the default spawnpoint")
			.HandleWith(OnSpawnpointUnsetCmd)
			.EndSub()
			.EndSub()
			.Validate();
	}

	private TextCommandResult OnSpawnpointUnsetCmd(TextCommandCallingArgs args)
	{
		PlayerRole role = (PlayerRole)args.Parsers[0].GetValue();
		role.DefaultSpawn = null;
		ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} now unset.", role.Name, role.DefaultSpawn));
	}

	private TextCommandResult OnSpawnpointSetCmd(TextCommandCallingArgs args)
	{
		PlayerRole role = (PlayerRole)args.Parsers[0].GetValue();
		Vec3d pos = (Vec3d)args.Parsers[1].GetValue();
		role.DefaultSpawn = new PlayerSpawnPos
		{
			x = (int)pos.X,
			y = (int)pos.Y,
			z = (int)pos.Z
		};
		ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} now set to {1}", role.Name, role.DefaultSpawn));
	}

	private TextCommandResult OnSpawnpointCmd(TextCommandCallingArgs args)
	{
		PlayerRole role = (PlayerRole)args.Parsers[0].GetValue();
		if (role.DefaultSpawn == null)
		{
			return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} is not set.", role.Name));
		}
		return TextCommandResult.Success(Lang.Get("Spawnpoint for role {0} is at {1}", role.Name, role.DefaultSpawn));
	}

	private TextCommandResult OnRevokeCmd(TextCommandCallingArgs args)
	{
		IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
		string privname = (string)args.Parsers[1].GetValue();
		if (!role.Privileges.Contains(privname))
		{
			return TextCommandResult.Error(Lang.Get("Role does not have this privilege"));
		}
		role.RevokePrivilege(privname);
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Ok, privilege '{0}' now revoked", privname));
	}

	private TextCommandResult OnGrantCmd(TextCommandCallingArgs args)
	{
		IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
		string privname = (string)args.Parsers[1].GetValue();
		if (role.Privileges.Contains(privname))
		{
			return TextCommandResult.Error(Lang.Get("Role already has this privilege"));
		}
		role.GrantPrivilege(privname);
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Ok, privilege '{0}' now granted", privname));
	}

	private TextCommandResult OnPrivilegeCmd(TextCommandCallingArgs args)
	{
		IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
		return TextCommandResult.Success(Lang.Get("This role has following privileges: {0}", string.Join(", ", role.Privileges)));
	}

	private TextCommandResult OnLandclaimmaxareasCmd(TextCommandCallingArgs args)
	{
		IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
		int? area = (int?)args.Parsers[1].GetValue();
		if (!area.HasValue || area < 0)
		{
			return TextCommandResult.Success(Lang.Get("This role has a land claim max areas {0}", role.LandClaimMaxAreas));
		}
		role.LandClaimMaxAreas = area.Value;
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Land claim max areas now set to {0}", role.LandClaimMaxAreas));
	}

	private TextCommandResult OnLandclaimminsizeCmd(TextCommandCallingArgs args)
	{
		IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
		Vec3i minSize = (Vec3i)args.Parsers[1].GetValue();
		if (minSize == null)
		{
			return TextCommandResult.Success(Lang.Get("This role has a land claim min size of {0} blocks", role.LandClaimMinSize));
		}
		role.LandClaimMinSize = minSize;
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Land claim min size now set to {0} blocks", role.LandClaimMinSize));
	}

	private TextCommandResult OnLandclaimallowanceCmd(TextCommandCallingArgs args)
	{
		IPlayerRole role = (IPlayerRole)args.Parsers[0].GetValue();
		int? landClaimAllowance = (int?)args.Parsers[1].GetValue();
		if (!landClaimAllowance.HasValue || landClaimAllowance < 0)
		{
			return TextCommandResult.Success(Lang.Get("This role has a land claim allowance of {0}m³", role.LandClaimAllowance));
		}
		role.LandClaimAllowance = landClaimAllowance.Value;
		server.ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Land claim allowance now set to {0}m³", role.LandClaimAllowance));
	}

	private TextCommandResult OnCmdMyStats(TextCommandCallingArgs args)
	{
		return getStats(new PlayerUidName(args.Caller.Player.PlayerUID, args.Caller.Player.PlayerName), args);
	}

	private TextCommandResult WipePlayerInventory(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ConnectedClient client = server.GetClientByUID(targetPlayer.Uid);
		if (client != null)
		{
			foreach (KeyValuePair<string, InventoryBase> inventory in client.WorldData.inventories)
			{
				inventory.Value.Clear();
			}
			client.Player.BroadcastPlayerData(sendInventory: true);
			return TextCommandResult.Success("Inventory cleared.");
		}
		server.ClearPlayerInvs.Add(targetPlayer.Uid);
		return TextCommandResult.Success("Clear command queued. Inventory will be cleared next time the player connects, which must happen before the server restarts");
	}

	private TextCommandResult WipePlayerData(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (server.chunkThread.gameDatabase.GetPlayerData(targetPlayer.Uid) == null)
		{
			return TextCommandResult.Error("No data for this player found in savegame");
		}
		server.chunkThread.gameDatabase.SetPlayerData(targetPlayer.Uid, null);
		server.PlayerDataManager.PlayerDataByUid.Remove(targetPlayer.Uid);
		server.PlayerDataManager.WorldDataByUID.Remove(targetPlayer.Uid);
		server.PlayerDataManager.playerDataDirty = true;
		return TextCommandResult.Success("Ok, player data deleted");
	}

	private TextCommandResult handleGameMode(TextCommandCallingArgs args)
	{
		string targetPlayername = args.Caller.Player?.PlayerName;
		if (args.RawArgs.Length > 0 && (server.GetClientByPlayername(args.RawArgs.PeekWord()) != null || args.RawArgs.Length > 1))
		{
			targetPlayername = args.RawArgs.PopWord();
		}
		string gamemodestr = args.RawArgs.PopWord();
		ConnectedClient targetPlayer = server.GetClientByPlayername(targetPlayername);
		if (targetPlayer == null)
		{
			return TextCommandResult.Error(Lang.Get("No player with name '{0}' online", targetPlayername));
		}
		bool isSelf = args.Caller.Player?.PlayerUID == targetPlayer.Player.PlayerUID;
		if (gamemodestr == null)
		{
			if (isSelf)
			{
				return TextCommandResult.Success(Lang.Get("Your Current gamemode is {0}", targetPlayer.WorldData.GameMode));
			}
			return TextCommandResult.Success(Lang.Get("Current gamemode for {0} is {1}", targetPlayername, targetPlayer.WorldData.GameMode));
		}
		if (!isSelf && !args.Caller.HasPrivilege(Privilege.commandplayer))
		{
			return TextCommandResult.Error(Lang.Get("Insufficient Privileges to set another players game mode"));
		}
		if (isSelf && !args.Caller.HasPrivilege(Privilege.gamemode))
		{
			return TextCommandResult.Error(Lang.Get("Insufficient Privileges to set your game mode"));
		}
		return SetGameMode(args.Caller, new PlayerUidName(targetPlayer.SentPlayerUid, targetPlayer.PlayerName), gamemodestr);
	}

	private TextCommandResult handleEntity(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		string type = (string)args[1];
		float value = (float)args[2];
		if (!(server.PlayerByUid(targetPlayer.Uid) is IServerPlayer { Entity: var eplr }))
		{
			return TextCommandResult.Error(Lang.Get("Player must be online to set attributes"));
		}
		ITreeAttribute hungerTree = eplr.WatchedAttributes.GetTreeAttribute("hunger");
		ITreeAttribute healthTree = eplr.WatchedAttributes.GetTreeAttribute("health");
		ITreeAttribute oxyTree = eplr.WatchedAttributes.GetTreeAttribute("oxygen");
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Error(Lang.Get("Position: {0}, Satiety: {1}/{2}, Health: {3}/{4}", eplr.ServerPos.XYZ, hungerTree.GetFloat("currentsaturation"), hungerTree.TryGetFloat("maxsaturation"), healthTree.GetFloat("currenthealth"), healthTree.TryGetFloat("maxhealth")));
		}
		float? maxSaturation = hungerTree.TryGetFloat("maxsaturation");
		switch (type)
		{
		case "satiety":
			value = GameMath.Clamp(value, 0f, 1f);
			if (hungerTree != null)
			{
				float newval3 = value * maxSaturation.Value;
				hungerTree.SetFloat("currentsaturation", newval3);
				eplr.WatchedAttributes.MarkPathDirty("hunger");
				return TextCommandResult.Success("Satiety " + newval3 + " set.");
			}
			return TextCommandResult.Error("hunger attribute tree not found.");
		case "protein":
		case "fruit":
		case "dairy":
		case "grain":
		case "vegetable":
			value = GameMath.Clamp(value, 0f, 1f);
			if (hungerTree != null)
			{
				float newval2 = value * maxSaturation.Value;
				hungerTree.SetFloat(type + "Level", newval2);
				return TextCommandResult.Success(type + " level " + newval2 + " set.");
			}
			return TextCommandResult.Error("hunger attribute tree not found.");
		case "intox":
			eplr.WatchedAttributes.SetFloat("intoxication", value);
			return TextCommandResult.Success("Intoxication value " + value + " set.");
		case "temp":
			eplr.WatchedAttributes.GetTreeAttribute("bodyTemp").SetFloat("bodytemp", value);
			return TextCommandResult.Success("Body temp " + value + " set.");
		case "tempstab":
			value = GameMath.Clamp(value, 0f, 1f);
			eplr.WatchedAttributes.SetDouble("temporalStability", value);
			return TextCommandResult.Success("Stability " + value + " set.");
		case "health":
			value = GameMath.Clamp(value, 0f, 1f);
			if (healthTree != null)
			{
				float newval = value * healthTree.TryGetFloat("maxhealth").Value;
				healthTree.SetFloat("currenthealth", newval);
				eplr.WatchedAttributes.MarkPathDirty("health");
				return TextCommandResult.Success("Health " + newval + " set.");
			}
			return TextCommandResult.Error("health attribute tree not found.");
		case "maxhealth":
			value = GameMath.Clamp(value, 0f, 9999f);
			if (healthTree != null)
			{
				healthTree.SetFloat("basemaxhealth", value);
				healthTree.SetFloat("maxhealth", value);
				healthTree.SetFloat("currenthealth", value);
				eplr.WatchedAttributes.MarkPathDirty("health");
				return TextCommandResult.Success("Max Health " + value + " set.");
			}
			return TextCommandResult.Error("health attribute tree not found.");
		case "maxoxygen":
		case "maxoxy":
			value = GameMath.Clamp(value, 0f, 100000000f);
			if (oxyTree != null)
			{
				oxyTree.SetFloat("maxoxygen", value);
				oxyTree.SetFloat("currentoxygen", value);
				eplr.WatchedAttributes.MarkPathDirty("oxygen");
				return TextCommandResult.Success("Max Oxygen " + value + " set.");
			}
			return TextCommandResult.Error("Oxygen attribute tree not found.");
		default:
			return TextCommandResult.Success("Incorrect attribute name");
		}
	}

	private TextCommandResult handleLandClaimMaxAreas(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData plrdata = server.GetServerPlayerData(targetPlayer.Uid);
		if (plrdata == null)
		{
			return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once"));
		}
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success(Lang.Get("This player has a land claim extra max areas setting of {0}", plrdata.ExtraLandClaimAreas));
		}
		plrdata.ExtraLandClaimAreas = (int)args[1];
		return TextCommandResult.Success(Lang.Get("Land claim extra max areas now set to {0}", plrdata.ExtraLandClaimAreas));
	}

	private TextCommandResult handleLandClaimAllowance(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData plrdata = server.GetServerPlayerData(targetPlayer.Uid);
		if (plrdata == null)
		{
			return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once"));
		}
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success(Lang.Get("This player has a land claim extra allowance of {0}m³", plrdata.ExtraLandClaimAllowance));
		}
		plrdata.ExtraLandClaimAllowance = (int)args[1];
		return TextCommandResult.Success(Lang.Get("Land claim extra allowance now set to {0}m³", plrdata.ExtraLandClaimAllowance));
	}

	private TextCommandResult handleCharSel(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		IWorldPlayerData plrdata = server.GetWorldPlayerData(targetPlayer.Uid);
		if (plrdata == null)
		{
			return TextCommandResult.Error(Lang.Get("Only works for players that have connected to your server at least once"));
		}
		if (!plrdata.EntityPlayer.WatchedAttributes.GetBool("allowcharselonce"))
		{
			plrdata.EntityPlayer.WatchedAttributes.SetBool("allowcharselonce", value: true);
			return TextCommandResult.Success(Lang.Get("Ok, player can now run .charsel to change skin and character class once"));
		}
		return TextCommandResult.Error(Lang.Get("Player can already run .charsel to change skin and character class"));
	}

	private TextCommandResult getSetGameMode(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (args.Parsers[1].IsMissing)
		{
			if (!server.PlayerDataManager.WorldDataByUID.TryGetValue(targetPlayer.Uid, out var targetPlayerWorldData))
			{
				return TextCommandResult.Error(Lang.Get("Player never connected to this server. Must at least connect once to set game mode"));
			}
			return TextCommandResult.Success(Lang.Get("Player has game mode {0}", targetPlayerWorldData.GameMode));
		}
		return SetGameMode(args.Caller, targetPlayer, (string)args[1]);
	}

	private TextCommandResult SetGameMode(Caller caller, PlayerUidName parsedTargetPlayer, string modestring)
	{
		EnumGameMode? mode = null;
		if (int.TryParse(modestring, out var modeint))
		{
			if (Enum.IsDefined(typeof(EnumGameMode), modeint))
			{
				mode = (EnumGameMode)modeint;
			}
		}
		else if (modestring.ToLowerInvariant().StartsWith('c'))
		{
			mode = EnumGameMode.Creative;
		}
		else if (modestring.ToLowerInvariant().StartsWithOrdinal("sp"))
		{
			mode = EnumGameMode.Spectator;
		}
		else if (modestring.ToLowerInvariant().StartsWith('s'))
		{
			mode = EnumGameMode.Survival;
		}
		else if (modestring.ToLowerInvariant().StartsWith('g'))
		{
			mode = EnumGameMode.Guest;
		}
		if (!mode.HasValue)
		{
			return TextCommandResult.Error(Lang.Get("Invalid game mode '{0}'", modestring));
		}
		if (!server.PlayerDataManager.WorldDataByUID.TryGetValue(parsedTargetPlayer.Uid, out var targetPlayerWorldData))
		{
			return TextCommandResult.Error(Lang.Get("Player never connected to this server. Must at least connect once to set game mode.", modestring));
		}
		EnumGameMode modeBefore = targetPlayerWorldData.GameMode;
		targetPlayerWorldData.GameMode = mode.Value;
		bool canFreeMove = mode.GetValueOrDefault() == EnumGameMode.Creative || mode.GetValueOrDefault() == EnumGameMode.Spectator;
		targetPlayerWorldData.FreeMove = (targetPlayerWorldData.FreeMove && canFreeMove) || mode.GetValueOrDefault() == EnumGameMode.Spectator;
		targetPlayerWorldData.NoClip = (targetPlayerWorldData.NoClip && canFreeMove) || mode.GetValueOrDefault() == EnumGameMode.Spectator;
		if (mode.GetValueOrDefault() == EnumGameMode.Survival || mode == EnumGameMode.Guest)
		{
			if (modeBefore == EnumGameMode.Creative)
			{
				targetPlayerWorldData.PreviousPickingRange = targetPlayerWorldData.PickingRange;
			}
			targetPlayerWorldData.PickingRange = GlobalConstants.DefaultPickingRange;
		}
		if (mode.GetValueOrDefault() == EnumGameMode.Creative && (modeBefore == EnumGameMode.Survival || modeBefore == EnumGameMode.Guest))
		{
			targetPlayerWorldData.PickingRange = targetPlayerWorldData.PreviousPickingRange;
		}
		ServerPlayer targetPlayer = server.GetConnectedClient(parsedTargetPlayer.Uid)?.Player;
		if (mode != modeBefore)
		{
			if (targetPlayer != null)
			{
				for (int i = 0; i < server.Systems.Length; i++)
				{
					server.Systems[i].OnPlayerSwitchGameMode(targetPlayer);
				}
			}
			if (mode == EnumGameMode.Guest || mode.GetValueOrDefault() == EnumGameMode.Survival)
			{
				targetPlayerWorldData.MoveSpeedMultiplier = 1f;
			}
			if (targetPlayer != null)
			{
				server.EventManager.TriggerPlayerChangeGamemode(targetPlayer);
			}
		}
		if (targetPlayer != null)
		{
			server.BroadcastPlayerData(targetPlayer, sendInventory: false);
			targetPlayer.Entity.UpdatePartitioning();
			if (targetPlayer.client.Socket is TcpNetConnection tcpSocket)
			{
				tcpSocket.SetLengthLimit(mode.GetValueOrDefault() == EnumGameMode.Creative);
			}
		}
		object obj;
		if (mode.HasValue)
		{
			EnumGameMode? enumGameMode = mode;
			obj = Lang.Get("gamemode-" + enumGameMode.ToString());
		}
		else
		{
			obj = "-";
		}
		string modeLocalized = (string)obj;
		if (targetPlayer == caller.Player)
		{
			ServerMain.Logger.Audit("{0} put himself into game mode {1}", caller.GetName(), modeLocalized);
			return TextCommandResult.Success(Lang.Get("Game mode {0} set.", modeLocalized));
		}
		targetPlayer?.SendMessage(GlobalConstants.CurrentChatGroup, Lang.Get("{0} has set your gamemode to {1}", caller.GetName(), modeLocalized), EnumChatType.Notification);
		ServerMain.Logger.Audit("{0} put {1} into game mode {2}", caller.GetName(), parsedTargetPlayer.Name, modeLocalized);
		return TextCommandResult.Success(Lang.Get("Game mode {0} set for player {1}.", modeLocalized, parsedTargetPlayer.Name));
	}

	private TextCommandResult getStats(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData plrData = server.GetServerPlayerData(targetPlayer.Uid);
		HashSet<string> privcodes = plrData.GetAllPrivilegeCodes(server.Config);
		StringBuilder stats = new StringBuilder();
		ConnectedClient client = server.GetClientByUID(targetPlayer.Uid);
		PlayerRole role = plrData.GetPlayerRole(server);
		stats.AppendLine(Lang.Get("{0} is currently {1}", plrData.LastKnownPlayername, (client == null) ? "offline" : "online"));
		stats.AppendLine(Lang.Get("Role: {0}", plrData.RoleCode));
		stats.AppendLine(Lang.Get("All Privilege codes: {0}", string.Join(", ", privcodes.ToArray())));
		stats.AppendLine(Lang.Get("Land claim allowance: {0}m³ + {1}m³", role.LandClaimAllowance, plrData.ExtraLandClaimAllowance));
		stats.AppendLine(Lang.Get("Land claim max areas: {0} + {1}", role.LandClaimMaxAreas, plrData.ExtraLandClaimAreas));
		List<LandClaim> claims = CmdLand.GetPlayerClaims(server, targetPlayer.Uid);
		int totalSize = 0;
		foreach (LandClaim claim in claims)
		{
			totalSize += claim.SizeXYZ;
		}
		stats.AppendLine(Lang.Get("Land claimed: {0}m³", totalSize));
		stats.AppendLine(Lang.Get("Amount of areas claimed: {0}", claims.Count));
		if (args.Caller.HasPrivilege(Privilege.grantrevoke) && client != null)
		{
			stats.AppendLine($"Fly suspicion count: {client.AuditFlySuspicion}");
			stats.AppendLine($"Tele/Speed suspicion count: {client.TotalTeleSupicions}");
		}
		return TextCommandResult.Success(stats.ToString());
	}

	private TextCommandResult GetSetRole(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (args.Parsers.Count == 0 || args.Parsers[1].IsMissing)
		{
			ServerPlayerData targetPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
			return TextCommandResult.Success("Player has role " + targetPlayerData.RoleCode);
		}
		PlayerRole role = (PlayerRole)args[1];
		return ChangeRole(args.Caller, targetPlayer, role.Code);
	}

	private TextCommandResult opPlayer(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		return ChangeRole(args.Caller, targetPlayer, "admin");
	}

	public TextCommandResult ChangeRole(Caller caller, PlayerUidName targetPlayer, string newRoleCode)
	{
		ServerPlayerData targetPlayerData = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		if (targetPlayerData == null)
		{
			return TextCommandResult.Error(Lang.Get("No player with this playername found"));
		}
		if (caller.Player?.PlayerUID == targetPlayerData.PlayerUID)
		{
			return TextCommandResult.Error(Lang.Get("Can't change your own group"));
		}
		PlayerRole newRole = null;
		foreach (KeyValuePair<string, PlayerRole> val in Config.RolesByCode)
		{
			if (val.Key.ToLowerInvariant() == newRoleCode.ToLowerInvariant())
			{
				newRole = val.Value;
				break;
			}
		}
		if (newRole == null)
		{
			return TextCommandResult.Error(Lang.Get("No group '{0}' found", newRoleCode));
		}
		string callerRole = caller.CallerRole;
		if (caller.Player != null)
		{
			callerRole = server.PlayerDataManager.GetPlayerDataByUid(caller.Player.PlayerUID).RoleCode;
		}
		Config.RolesByCode.TryGetValue(callerRole, out var issuingRole);
		if (newRole.IsSuperior(issuingRole) || (newRole.EqualLevel(issuingRole) && !caller.HasPrivilege(Privilege.root)))
		{
			return TextCommandResult.Error(Lang.Get("Can only set lower role level than your own"));
		}
		PlayerRole oldTargetRole = Config.RolesByCode[targetPlayerData.RoleCode];
		if (oldTargetRole.Code == newRole.Code)
		{
			return TextCommandResult.Error(Lang.Get("Player is already in group {0}", oldTargetRole.Code));
		}
		if (oldTargetRole.IsSuperior(issuingRole) || (oldTargetRole.EqualLevel(issuingRole) && !caller.HasPrivilege(Privilege.root)))
		{
			return TextCommandResult.Error(Lang.Get("Can't modify a players role with a superior role. Players current role is {0}", oldTargetRole.Code));
		}
		targetPlayerData.SetRole(newRole);
		server.PlayerDataManager.playerDataDirty = true;
		ServerMain.Logger.Audit($"{caller.GetName()} assigned {newRole.Name} the role {targetPlayer.Name}.");
		ConnectedClient client = server.GetClientByPlayername(targetPlayer.Name);
		if (client != null)
		{
			server.SendOwnPlayerData(client.Player, sendInventory: false, sendPrivileges: true);
			string msg = ((newRole.PrivilegeLevel > oldTargetRole.PrivilegeLevel) ? Lang.Get("You've been promoted to role {0}", newRole.Name) : Lang.Get("You've been demoted to role {0}", newRole.Name));
			server.SendMessage(client.Player, GlobalConstants.CurrentChatGroup, msg, EnumChatType.Notification);
			server.SendRoles(client.Player);
		}
		return TextCommandResult.Success(Lang.Get("Ok, role {0} assigned to {1}", newRole.Name, targetPlayer.Name));
	}

	private TextCommandResult removeDenyPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData plrdata = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string privilege = (string)args[1];
		string targetPlayerName = targetPlayer.Name;
		if (!plrdata.DeniedPrivileges.Contains(privilege))
		{
			return TextCommandResult.Error(Lang.Get("Player {0} did not have this privilege denied.", targetPlayerName));
		}
		plrdata.RemovePrivilegeDenial(privilege);
		string hisMsg = Lang.Get("{0} removed your Privilege denial for {1}", args.Caller.GetName(), privilege);
		ConnectedClient targetClient = server.GetConnectedClient(targetPlayer.Uid);
		if (targetClient != null)
		{
			server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification);
			server.SendOwnPlayerData(targetClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit($"{args.Caller.GetName()} no longer denied {targetPlayer.Name} the privilege {privilege}.");
		ServerMain.Logger.Event($"{args.Caller.GetName()} no longer denied {targetPlayer.Name} the privilege {privilege}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} is no longer denied from {1}", privilege, targetPlayerName));
	}

	private TextCommandResult denyPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData plrdata = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string privilege = (string)args[1];
		string targetPlayerName = targetPlayer.Name;
		if (plrdata.DeniedPrivileges.Contains(privilege))
		{
			return TextCommandResult.Error(Lang.Get("Player {0} already has this privilege denied.", targetPlayerName));
		}
		plrdata.DenyPrivilege(privilege);
		string hisMsg = Lang.Get("{0} has denied Privilege {1}", args.Caller.GetName(), privilege);
		ConnectedClient targetClient = server.GetConnectedClient(targetPlayer.Uid);
		if (targetClient != null)
		{
			server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification);
			server.SendOwnPlayerData(targetClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit($"{args.Caller.GetName()} denied {targetPlayer.Name} the privilege {privilege}.");
		ServerMain.Logger.Event($"{args.Caller.GetName()} denied {targetPlayer.Name} the privilege {privilege}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} has been denied from {1}", privilege, targetPlayerName));
	}

	private TextCommandResult revokePrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData plrdata = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string privilege = (string)args[1];
		string targetPlayerName = targetPlayer.Name;
		if (!plrdata.PermaPrivileges.Contains(privilege) && !plrdata.HasPrivilege(privilege, server.Config.RolesByCode))
		{
			return TextCommandResult.Error(Lang.Get("Player {0} does not have this privilege neither directly or by role", targetPlayerName));
		}
		plrdata.RevokePrivilege(privilege);
		string hisMsg = Lang.Get("{0} has revoked your Privilege {1}", args.Caller.GetName(), privilege);
		ConnectedClient targetClient = server.GetConnectedClient(targetPlayer.Uid);
		if (targetClient != null)
		{
			server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification);
			server.SendOwnPlayerData(targetClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit($"{args.Caller.GetName()} revoked {targetPlayer.Name} privilege {privilege}.");
		ServerMain.Logger.Event($"{args.Caller.GetName()} revoked {targetPlayer.Name} privilege {privilege}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} has been revoked from {1}", privilege, targetPlayerName));
	}

	private TextCommandResult grantPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		ServerPlayerData plrdata = server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string privilege = (string)args[1];
		string targetPlayerName = targetPlayer.Name;
		string denyRemoveMsg = "";
		if (plrdata.DeniedPrivileges.Contains(privilege))
		{
			denyRemoveMsg = Lang.Get("Privilege deny for '{0}' removed from player {1}", privilege, targetPlayerName);
			ServerMain.Logger.Audit("{0} removed the privilege deny for '{1}' from player {2}", args.Caller.GetName(), privilege, targetPlayerName);
		}
		if (plrdata.HasPrivilege(privilege, server.Config.RolesByCode))
		{
			if (denyRemoveMsg.Length == 0)
			{
				return TextCommandResult.Error(Lang.Get("Player {0} has this privilege already", targetPlayerName));
			}
			return TextCommandResult.Success(denyRemoveMsg);
		}
		plrdata.GrantPrivilege(privilege);
		ConnectedClient targetClient = server.GetConnectedClient(targetPlayer.Uid);
		if (targetClient != null)
		{
			string hisMsg = Lang.Get("{0} granted you the privilege {1}", args.Caller.GetName(), privilege);
			server.SendMessage(targetClient.Player, GlobalConstants.CurrentChatGroup, hisMsg, EnumChatType.Notification);
			server.SendOwnPlayerData(targetClient.Player, sendInventory: false, sendPrivileges: true);
		}
		ServerMain.Logger.Audit("Player {0} granted {1} the privilege {2}", args.Caller.GetName(), targetPlayerName, privilege);
		ServerMain.Logger.Event($"{args.Caller.GetName()} grants {targetPlayerName} the privilege {privilege}.");
		return TextCommandResult.Success(Lang.Get("Privilege {0} granted to {1}", privilege, targetPlayerName));
	}

	private TextCommandResult listPrivilege(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		bool self = targetPlayer.Uid == args.Caller.Player?.PlayerUID;
		if (!server.PlayerDataManager.PlayerDataByUid.ContainsKey(targetPlayer.Uid))
		{
			return TextCommandResult.Error(Lang.Get("This player is has never joined your server. He will have the privileges of the default role '{0}'.", server.Config.DefaultRoleCode));
		}
		ServerPlayerData serverPlayerData = server.PlayerDataManager.PlayerDataByUid[targetPlayer.Uid];
		HashSet<string> privcodes = serverPlayerData.GetAllPrivilegeCodes(server.Config);
		foreach (string priv in serverPlayerData.DeniedPrivileges)
		{
			privcodes.Remove(priv);
		}
		return TextCommandResult.Success(self ? Lang.Get("You have {0} privileges: {1}", privcodes.Count, privcodes.Implode()) : Lang.Get("{0} has {1} privileges: {2}", targetPlayer.Name, privcodes.Count, privcodes.Implode()));
	}

	private TextCommandResult addRemoveWhitelist(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		if (args.Parsers[1].IsMissing)
		{
			bool islisted = server.PlayerDataManager.WhitelistedPlayers.Any((PlayerEntry item) => item.PlayerUID == targetPlayer.Uid);
			return TextCommandResult.Success(Lang.Get("Player is currently {0}", islisted ? "whitelisted" : "not whitelisted"));
		}
		bool num = (bool)args[1];
		string reason = (string)args[2];
		server.PlayerDataManager.GetOrCreateServerPlayerData(targetPlayer.Uid, targetPlayer.Name);
		string issuer = ((args.Caller.Player != null) ? args.Caller.Player.PlayerName : args.Caller.Type.ToString());
		if (num)
		{
			DateTime untildate = DateTime.Now.AddYears(50);
			server.PlayerDataManager.WhitelistPlayer(targetPlayer.Name, targetPlayer.Uid, issuer, reason, untildate);
			return TextCommandResult.Success(Lang.Get("Player is now whitelisted until {0}", untildate));
		}
		if (server.PlayerDataManager.UnWhitelistPlayer(targetPlayer.Name, targetPlayer.Uid))
		{
			return TextCommandResult.Success(Lang.Get("Player is now removed from the whitelist"));
		}
		return TextCommandResult.Error(Lang.Get("Player is not whitelisted"));
	}

	private TextCommandResult setMovespeed(PlayerUidName targetPlayer, TextCommandCallingArgs args)
	{
		IWorldPlayerData plrdata = server.GetWorldPlayerData(targetPlayer.Uid);
		plrdata.MoveSpeedMultiplier = (float)args[1];
		if (server.PlayerByUid(plrdata.PlayerUID) is IServerPlayer plr)
		{
			plr.Entity.Controls.MovespeedMultiplier = plrdata.MoveSpeedMultiplier;
			server.broadCastModeChange(plr);
		}
		return TextCommandResult.Success("Ok, movespeed set to " + plrdata.MoveSpeedMultiplier);
	}

	public static TextCommandResult Each(TextCommandCallingArgs args, PlayerEachDelegate onPlayer)
	{
		PlayerUidName[] players = (PlayerUidName[])args.Parsers[0].GetValue();
		int successCnt = 0;
		LimitedList<TextCommandResult> results = new LimitedList<TextCommandResult>(10);
		if (players.Length == 0)
		{
			return TextCommandResult.Error(Lang.Get("No players found that match your selector"));
		}
		PlayerUidName[] array = players;
		foreach (PlayerUidName parsedplayer in array)
		{
			TextCommandResult result = onPlayer(parsedplayer, args);
			if (result.Status == EnumCommandStatus.Success)
			{
				successCnt++;
			}
			results.Add(result);
		}
		if (players.Length <= 10)
		{
			return TextCommandResult.Success(string.Join(", ", results.Select((TextCommandResult el) => el.StatusMessage)));
		}
		return TextCommandResult.Success(Lang.Get("Successfully executed commands on {0}/{1} players", successCnt, players.Length));
	}
}
