using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class CmdLand
{
	public delegate TextCommandResult ClaimInProgressHandlerDelegate(TextCommandCallingArgs args, ClaimInProgress claimp);

	private ServerMain server;

	private Dictionary<IPlayer, ClaimInProgress> TempClaims = new Dictionary<IPlayer, ClaimInProgress>();

	private int claimedColor = ColorUtil.ToRgba(64, 100, 255, 100);

	private int claimingColor = ColorUtil.ToRgba(64, 148, 210, 246);

	public CmdLand(ServerMain server)
	{
		CmdLand cmdLand = this;
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		chatCommands.GetOrCreate("land").RequiresPrivilege(Privilege.chat).RequiresPlayer()
			.WithDesc("Manage land rights")
			.WithPreCondition((TextCommandCallingArgs args) => (!server.SaveGameData.WorldConfiguration.GetBool("allowLandClaiming", defaultValue: true)) ? TextCommandResult.Error(Lang.Get("Land claiming has been disabled by world configuration")) : TextCommandResult.Success())
			.BeginSub("free")
			.WithArgs(parsers.Int("claim id"), parsers.OptionalBool("confirm", "confirm"))
			.HandleWith((TextCommandCallingArgs args) => cmdLand.freeLand(args.Caller.Player as IServerPlayer, (int)args[0], (bool)args[1]))
			.WithDesc("Remove a land claim of yours")
			.EndSub()
			.BeginSub("adminfree")
			.RequiresPrivilege(Privilege.commandplayer)
			.WithArgs(parsers.PlayerUids("player name"))
			.WithDesc("Delete all claims of selected player(s)")
			.HandleWith(freeLandAdmin)
			.EndSub()
			.BeginSub("adminfreehere")
			.RequiresPrivilege(Privilege.commandplayer)
			.WithDesc("Remove a land claim at the calling position")
			.HandleWith(freeLandAdminHere)
			.EndSub()
			.BeginSub("list")
			.WithDesc("List your claimed lands or retrieve information about a claim")
			.WithArgs(parsers.OptionalInt("land claim index"))
			.HandleWith((TextCommandCallingArgs args) => cmdLand.landList(args.Caller.Player as IServerPlayer, args.Parsers[0].IsMissing ? null : ((int?)args[0])))
			.EndSub()
			.BeginSub("info")
			.WithDesc("Land rights information at your location")
			.HandleWith((TextCommandCallingArgs args) => cmdLand.landInfo(args.Caller.Player as IServerPlayer))
			.EndSub()
			.BeginSub("claim")
			.RequiresPrivilege(Privilege.claimland)
			.WithDesc("Add, Remove or Modify your claims")
			.BeginSub("load")
			.WithDesc("Load an existing claim")
			.WithArgs(parsers.Int("claim id"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				IServerPlayer serverPlayer2 = args.Caller.Player as IServerPlayer;
				List<LandClaim> playerClaims = GetPlayerClaims(server, serverPlayer2.PlayerUID);
				int num = (int)args[0];
				if (num < 0 || num >= playerClaims.Count)
				{
					return TextCommandResult.Error(Lang.Get("Incorrect claimid, you only have {0} claims", playerClaims.Count));
				}
				cmdLand.TempClaims[serverPlayer2] = new ClaimInProgress
				{
					Claim = playerClaims[num].Clone(),
					IsNew = false,
					OriginalClaim = playerClaims[num]
				};
				cmdLand.ResendHighlights(serverPlayer2, cmdLand.TempClaims[serverPlayer2].Claim);
				return TextCommandResult.Success(Lang.Get("Ok, claim loaded, you can now modify it", serverPlayer2.Role.LandClaimMaxAreas));
			})
			.EndSub()
			.BeginSub("new")
			.WithDesc("Create a new claim")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
				if (GetPlayerClaims(server, serverPlayer.PlayerUID).Count >= serverPlayer.Role.LandClaimMaxAreas + serverPlayer.ServerData.ExtraLandClaimAreas)
				{
					return TextCommandResult.Error(Lang.Get("Sorry you can't have more than {0} separate claims", serverPlayer.Role.LandClaimMaxAreas));
				}
				ClaimInProgress claimInProgress = new ClaimInProgress
				{
					Claim = LandClaim.CreateClaim(serverPlayer, serverPlayer.Role.PrivilegeLevel),
					IsNew = true
				};
				cmdLand.TempClaims[serverPlayer] = claimInProgress;
				claimInProgress.Start = serverPlayer.Entity.Pos.XYZ.AsBlockPos;
				cmdLand.ResendHighlights(serverPlayer, claimInProgress.Claim);
				return TextCommandResult.Success(Lang.Get("Ok new claim initiated, use /land claim start, then /land claim end to mark an area, you can use /land claim grow [up|north|east|...] [size] to grow/shrink the selection, if you messed up use /land claim cancel, then finally /land claim add to add that area. You can add multiple areas as long as they are adjacent. Once all is ready, use /land claim save [text] to save the claim"));
			})
			.EndSub()
			.BeginSub("grant")
			.WithDesc("Grant a player access to your claim")
			.WithArgs(parsers.PlayerUids("for player"), parsers.WordRange("permission type", "use", "all"))
			.HandleWith((TextCommandCallingArgs ccargs) => CmdPlayer.Each(ccargs, cmdLand.handleGrant))
			.EndSub()
			.BeginSub("revoke")
			.WithDesc("Revoke a player access on your claim")
			.WithArgs(parsers.PlayerUids("for player"))
			.HandleWith((TextCommandCallingArgs ccargs) => CmdPlayer.Each(ccargs, cmdLand.handleRevoke))
			.EndSub()
			.BeginSub("grantgroup")
			.WithDesc("Grant a group access to your claim")
			.WithArgs(parsers.Word("group name"), parsers.WordRange("permission type", "use", "all"))
			.HandleWith(handleGrantGroup)
			.EndSub()
			.BeginSub("revokegroup")
			.WithDesc("Revoke a group access on your claim")
			.WithArgs(parsers.Word("group name"))
			.HandleWith(handleRevokeGroup)
			.EndSub()
			.BeginSub("grow")
			.WithDesc("Grow area in one of 6 directions (up/down/north/east/south/west)")
			.WithArgs(parsers.WordRange("direction", "up", "down", "north", "east", "south", "west"), parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromCode((string)args[0]), (int)args[1])))
			.EndSub()
			.BeginSubs("gu", "gd", "gn", "ge", "gs", "gw")
			.WithDesc("Grow area in one of 6 directions (gu/gd/gn/ge/gs/gw)")
			.WithArgs(parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromFirstLetter(cargs.SubCmdCode[1]), (int)args[0])))
			.EndSub()
			.BeginSub("shrink")
			.WithDesc("Shrink area in one of 6 directions (up/down/north/east/south/west)")
			.WithArgs(parsers.WordRange("direction", "up", "down", "north", "east", "south", "west"), parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromCode((string)args[0]), -(int)args[1])))
			.EndSub()
			.BeginSubs("su", "sd", "sn", "se", "ss", "sw")
			.WithDesc("Shrink area in one of 6 directions (su/sd/sn/se/ss/sw)")
			.WithArgs(parsers.OptionalInt("amount", 1))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, (TextCommandCallingArgs args, ClaimInProgress claimp) => cmdLand.GrowSelection(args.Caller.Player, claimp, BlockFacing.FromFirstLetter(cargs.SubCmdCode[1]), -(int)args[0])))
			.EndSub()
			.BeginSub("start")
			.WithDesc("Set a start position for an area")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.Start = (args[0] as Vec3d).AsBlockPos;
				cmdLand.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
				return TextCommandResult.Success(Lang.Get("Ok, Land claim start position {0} set", claimp.Start.ToLocalPosition(server.api)));
			}))
			.EndSub()
			.BeginSub("end")
			.WithDesc("Set a end position for an area")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.End = (args[0] as Vec3d).AsBlockPos;
				cmdLand.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
				return TextCommandResult.Success(Lang.Get("Ok, Land claim end position {0} set", claimp.End.ToLocalPosition(server.api)));
			}))
			.EndSub()
			.BeginSub("add")
			.WithDesc("Add current area to the claim")
			.HandleWith(addCurrentArea)
			.EndSub()
			.BeginSub("allowuseeveryone")
			.WithDesc("Grant use privilege to all players")
			.WithArgs(parsers.Bool("on/off"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.Claim.AllowUseEveryone = (bool)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, allow use everyone is now {0}", claimp.Claim.AllowUseEveryone ? "on" : "off"));
			}))
			.EndSub()
			.BeginSub("plevel")
			.WithDesc("Set protection level on your current claim")
			.WithArgs(parsers.Int("protection level"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				claimp.Claim.ProtectionLevel = (int)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, protection level set to {0}", claimp.Claim.ProtectionLevel));
			}))
			.EndSub()
			.BeginSub("fullheight")
			.WithDesc("Expand claim to cover the entire map height")
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				if (claimp.Start == null || claimp.End == null)
				{
					return TextCommandResult.Error(Lang.Get("Define start and end position first"));
				}
				claimp.Start.Y = 0;
				claimp.End.Y = server.WorldMap.MapSizeY;
				cmdLand.ResendHighlights(args.Caller.Player, claimp.Claim, claimp.Start, claimp.End);
				return TextCommandResult.Success(Lang.Get("Ok, extended land claim to cover full world height"));
			}))
			.EndSub()
			.BeginSub("save")
			.WithDesc("Save your currently edited claim")
			.WithArgs(parsers.All("description"))
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				if (claimp.Claim.Areas.Count == 0)
				{
					return TextCommandResult.Error(Lang.Get("Cannot save an empty claim. Did you forget to type /land claim add?"));
				}
				claimp.Claim.Description = (string)args[0];
				if (claimp.IsNew)
				{
					server.WorldMap.Add(claimp.Claim);
				}
				else
				{
					server.WorldMap.UpdateClaim(claimp.OriginalClaim, claimp.Claim);
				}
				IPlayer player2 = args.Caller.Player;
				cmdLand.TempClaims[player2] = null;
				cmdLand.ResendHighlights(player2, null);
				return TextCommandResult.Success("Ok, Land claim saved on your name");
			}))
			.EndSub()
			.BeginSub("cancel")
			.WithDesc("Discard changes on currently edited claim")
			.HandleWith((TextCommandCallingArgs cargs) => cmdLand.acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
			{
				IPlayer player = args.Caller.Player;
				if (!cmdLand.TempClaims.ContainsKey(player))
				{
					return TextCommandResult.Error("No current land claim changes active");
				}
				cmdLand.TempClaims[player] = null;
				cmdLand.ResendHighlights(player, null);
				return TextCommandResult.Success("Ok, Land claim changes cancelled");
			}))
			.EndSub()
			.EndSub()
			.Validate();
	}

	private TextCommandResult handleRevokeGroup(TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			string text = (string)args[0];
			PlayerGroup playerGroupByName = server.PlayerDataManager.GetPlayerGroupByName(text);
			if (playerGroupByName != null && claimp.Claim.PermittedPlayerGroupIds.ContainsKey(playerGroupByName.Uid))
			{
				claimp.Claim.PermittedPlayerGroupIds.Remove(playerGroupByName.Uid);
				return TextCommandResult.Success("Ok, revoked access to group " + text);
			}
			return TextCommandResult.Error("No such group has access to your claim");
		});
	}

	private TextCommandResult handleGrantGroup(TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			string text = (string)args[0];
			EnumBlockAccessFlags value = EnumBlockAccessFlags.Use;
			if ((string)args[1] == "all")
			{
				value = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use;
			}
			PlayerGroup playerGroupByName = server.PlayerDataManager.GetPlayerGroupByName(text);
			if (playerGroupByName != null)
			{
				claimp.Claim.PermittedPlayerGroupIds[playerGroupByName.Uid] = value;
				return TextCommandResult.Success("Ok, granted access to group " + text);
			}
			return TextCommandResult.Error("No such group found");
		});
	}

	private TextCommandResult handleGrant(PlayerUidName forPlayer, TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			EnumBlockAccessFlags value = EnumBlockAccessFlags.Use;
			if ((string)args[1] == "all")
			{
				value = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use;
			}
			claimp.Claim.PermittedPlayerUids[forPlayer.Uid] = value;
			claimp.Claim.PermittedPlayerLastKnownPlayerName[forPlayer.Uid] = forPlayer.Name;
			return TextCommandResult.Success(Lang.Get("Ok, player {0} granted {1} access to your claim.", forPlayer.Name, args[1]));
		});
	}

	private TextCommandResult handleRevoke(PlayerUidName forPlayer, TextCommandCallingArgs cargs)
	{
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			if (claimp.Claim.PermittedPlayerUids.ContainsKey(forPlayer.Uid))
			{
				claimp.Claim.PermittedPlayerUids.Remove(forPlayer.Uid);
				return TextCommandResult.Success(Lang.Get("Ok, revoked access to player {0}.", forPlayer.Name));
			}
			return TextCommandResult.Success(Lang.Get("Player {0} had no access to your claim.", forPlayer.Name));
		});
	}

	private TextCommandResult acquireClaimInProgress(TextCommandCallingArgs cargs, ClaimInProgressHandlerDelegate handler)
	{
		if (TempClaims.TryGetValue(cargs.Caller.Player, out var claimp) && claimp != null)
		{
			return handler(cargs, claimp);
		}
		return TextCommandResult.Success(Lang.Get("No current or incomplete claim, type '/land claim new' to prepare a new one or '/land claim load [id]' to modify an existing one. The id can be retrieved from /land list"));
	}

	private TextCommandResult landInfo(IServerPlayer player)
	{
		List<LandClaim> claims = server.SaveGameData.LandClaims;
		List<string> claimTexts = new List<string>();
		bool haveBuildAccess = false;
		bool haveUseAccess = false;
		bool inPartionedClaim = false;
		bool inListClaim = false;
		foreach (LandClaim claim2 in claims)
		{
			if (claim2.PositionInside(player.Entity.ServerPos.XYZ))
			{
				claimTexts.Add(claim2.LastKnownOwnerName);
				inListClaim = true;
				if (claim2.TestPlayerAccess(player, EnumBlockAccessFlags.BuildOrBreak) != 0)
				{
					haveBuildAccess = true;
				}
				if (claim2.TestPlayerAccess(player, EnumBlockAccessFlags.Use) != 0)
				{
					haveUseAccess = true;
				}
				break;
			}
		}
		long regionindex = server.WorldMap.MapRegionIndex2D(player.Entity.ServerPos.XInt / server.WorldMap.RegionSize, player.Entity.ServerPos.ZInt / server.WorldMap.RegionSize);
		if (server.WorldMap.LandClaimByRegion.ContainsKey(regionindex))
		{
			foreach (LandClaim item in server.WorldMap.LandClaimByRegion[regionindex])
			{
				if (item.PositionInside(player.Entity.ServerPos.XYZ))
				{
					inPartionedClaim = true;
					break;
				}
			}
		}
		if (inPartionedClaim != inListClaim)
		{
			return TextCommandResult.Error($"Incorrect state. Spatially partitioned claim list not consistent with full claim list. Please contact the game developer. A server restart may temporarily fix the issue. (in partition: {inPartionedClaim}, in listclaim: {inListClaim})");
		}
		string privilegeInfo = "";
		if (player.HasPrivilege(Privilege.readlists))
		{
			foreach (LandClaim claim in claims)
			{
				if (!claim.PositionInside(player.Entity.ServerPos.XYZ))
				{
					continue;
				}
				int protLevel = claim.ProtectionLevel;
				StringBuilder plrsReadable = new StringBuilder();
				foreach (KeyValuePair<string, EnumBlockAccessFlags> val2 in claim.PermittedPlayerUids)
				{
					if (plrsReadable.Length > 0)
					{
						plrsReadable.Append(", ");
					}
					ServerPlayerData plrdata = server.GetServerPlayerData(val2.Key);
					if (plrdata != null)
					{
						plrsReadable.Append($"{plrdata.LastKnownPlayername} can {val2.Value}");
					}
					else
					{
						plrsReadable.Append($"{val2.Key} can {val2.Value}");
					}
				}
				if (plrsReadable.Length == 0)
				{
					plrsReadable.Append("None.");
				}
				StringBuilder groupsReadable = new StringBuilder();
				foreach (KeyValuePair<int, EnumBlockAccessFlags> val in claim.PermittedPlayerGroupIds)
				{
					if (groupsReadable.Length > 0)
					{
						groupsReadable.Append(", ");
					}
					server.PlayerDataManager.PlayerGroupsById.TryGetValue(val.Key, out var group);
					if (group != null)
					{
						groupsReadable.Append($"{group.Name} can {val.Value}");
					}
					else
					{
						groupsReadable.Append($"{val.Key} can {val.Value}");
					}
				}
				if (groupsReadable.Length == 0)
				{
					groupsReadable.Append("None.");
				}
				privilegeInfo = "\n" + $"Protection level: {protLevel}, Granted Players: {plrsReadable.ToString()}, Granted Groups: {groupsReadable.ToString()}";
			}
		}
		if (claimTexts.Count > 0)
		{
			string useText = Lang.Get("You don't have access to it.");
			if (haveBuildAccess && haveUseAccess)
			{
				useText = Lang.Get("You have build and use access.");
			}
			else
			{
				if (haveBuildAccess)
				{
					useText = Lang.Get("You have build access.");
				}
				if (haveUseAccess)
				{
					useText = Lang.Get("You have use access.");
				}
			}
			return TextCommandResult.Success(Lang.Get("These lands are claimed by {0}. {1}", string.Join(", ", claimTexts), useText) + privilegeInfo);
		}
		return TextCommandResult.Success(Lang.Get("These lands are not claimed by anybody") + privilegeInfo);
	}

	private TextCommandResult freeLand(IServerPlayer player, int claimid, bool confirm)
	{
		List<LandClaim> landClaims = server.SaveGameData.LandClaims;
		List<LandClaim> ownclaims = new List<LandClaim>();
		foreach (LandClaim claim in landClaims)
		{
			if (claim.OwnedByPlayerUid == player.PlayerUID)
			{
				ownclaims.Add(claim);
			}
		}
		if (claimid < 0 || claimid >= ownclaims.Count)
		{
			return TextCommandResult.Error(Lang.Get("Claim number too wrong, you only have {0} claims", ownclaims.Count));
		}
		LandClaim todeleteclaim = ownclaims[claimid];
		if (!confirm)
		{
			return TextCommandResult.Success(Lang.Get("command-deleteclaim-confirmation", todeleteclaim.Description, todeleteclaim.SizeXYZ, claimid));
		}
		server.WorldMap.Remove(todeleteclaim);
		return TextCommandResult.Success(Lang.Get("Ok, claim removed"));
	}

	private TextCommandResult freeLandAdmin(TextCommandCallingArgs args)
	{
		PlayerUidName[] obj = (PlayerUidName[])args[0];
		List<LandClaim> allclaims = server.SaveGameData.LandClaims;
		int qremoved = 0;
		List<string> playernames = new List<string>();
		PlayerUidName[] array = obj;
		foreach (PlayerUidName player in array)
		{
			playernames.Add(player.Name);
			foreach (LandClaim claim in new List<LandClaim>(allclaims))
			{
				if (claim.OwnedByPlayerUid == player.Uid && server.WorldMap.Remove(claim))
				{
					qremoved++;
				}
			}
		}
		return TextCommandResult.Success(Lang.Get("Ok, {0} claims removed from {1}", qremoved, string.Join(", ", playernames)));
	}

	private TextCommandResult freeLandAdminHere(TextCommandCallingArgs args)
	{
		Vec3d srcPos = args.Caller.Pos;
		long regionindex = server.WorldMap.MapRegionIndex2D(srcPos.XInt / server.WorldMap.RegionSize, srcPos.ZInt / server.WorldMap.RegionSize);
		if (server.WorldMap.LandClaimByRegion.ContainsKey(regionindex))
		{
			foreach (LandClaim claim in server.WorldMap.LandClaimByRegion[regionindex])
			{
				if (claim.PositionInside(srcPos))
				{
					server.WorldMap.Remove(claim);
					return TextCommandResult.Success(Lang.Get("Ok, Removed claim from {0}", claim.LastKnownOwnerName));
				}
			}
		}
		return TextCommandResult.Error(Lang.Get("No claim found at this position"), "nonefound");
	}

	private TextCommandResult landList(IServerPlayer player, int? index)
	{
		List<LandClaim> claims = server.SaveGameData.LandClaims;
		if (index.HasValue)
		{
			LandClaim ownClaim = null;
			int j = 0;
			int claimId = index.Value;
			foreach (LandClaim claim in claims)
			{
				if (!(claim.OwnedByPlayerUid != player.PlayerUID))
				{
					if (claimId == j)
					{
						ownClaim = claim;
						break;
					}
					j++;
				}
			}
			if (ownClaim == null)
			{
				return TextCommandResult.Error("No such claim");
			}
			BlockPos center = ownClaim.Center;
			center = center.Copy().Sub(server.DefaultSpawnPosition.XYZ.AsBlockPos);
			string claimInfo = Lang.Get("{0} ({1}m³ at {2})", ownClaim.Description, ownClaim.SizeXYZ, center);
			StringBuilder extPrivs = new StringBuilder();
			if (ownClaim.PermittedPlayerUids.Count > 0)
			{
				foreach (KeyValuePair<string, EnumBlockAccessFlags> val2 in ownClaim.PermittedPlayerUids)
				{
					string playeruid = val2.Key;
					string playername = null;
					if (!ownClaim.PermittedPlayerLastKnownPlayerName.TryGetValue(playeruid, out playername))
					{
						playername = playeruid;
					}
					bool build2 = (val2.Value & EnumBlockAccessFlags.BuildOrBreak) > EnumBlockAccessFlags.None;
					bool use2 = (val2.Value & EnumBlockAccessFlags.Use) > EnumBlockAccessFlags.None;
					string privs2 = ((build2 && use2) ? Lang.Get("Player {0} can build/break and use blocks", playername) : (build2 ? Lang.Get("Player {0} can build/break but not use blocks", playername) : Lang.Get("Player {0} can use but not build/break blocks", playername)));
					extPrivs.AppendLine(privs2);
				}
			}
			if (ownClaim.PermittedPlayerGroupIds.Count > 0)
			{
				Dictionary<int, PlayerGroup> allGroups = server.PlayerDataManager.PlayerGroupsById;
				foreach (KeyValuePair<int, EnumBlockAccessFlags> val in ownClaim.PermittedPlayerGroupIds)
				{
					int groupid = val.Key;
					PlayerGroup group = null;
					if (allGroups.TryGetValue(groupid, out group))
					{
						bool build = (val.Value & EnumBlockAccessFlags.BuildOrBreak) > EnumBlockAccessFlags.None;
						bool use = (val.Value & EnumBlockAccessFlags.Use) > EnumBlockAccessFlags.None;
						string privs = ((build && use) ? Lang.Get("Group {0} can build/break and use blocks", group.Name) : (build ? Lang.Get("Group {0} can build/break but not use blocks", group.Name) : Lang.Get("Group {0} can use but not build/break blocks", group.Name)));
						extPrivs.AppendLine(privs);
					}
				}
			}
			return TextCommandResult.Success(claimInfo + "\r\n" + ((extPrivs.Length == 0) ? Lang.Get("No other players/groups have access to this claim") : extPrivs.ToString()));
		}
		List<string> playerOwnedTexts = new List<string>();
		List<string> groupOwnedTexts = new List<string>();
		int i = 0;
		PlayerGroupMembership[] groups = player.Groups;
		bool allowCoordinateHud = server.api.World.Config.GetBool("allowCoordinateHud", defaultValue: true);
		foreach (LandClaim claim2 in claims)
		{
			BlockPos center2 = claim2.Center;
			center2 = center2.Copy().Sub(server.DefaultSpawnPosition.XYZ.AsBlockPos);
			if (claim2.OwnedByPlayerUid == player.PlayerUID)
			{
				if (allowCoordinateHud)
				{
					playerOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³ at {3})", i, claim2.Description, claim2.SizeXYZ, center2));
				}
				else
				{
					playerOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³)", i, claim2.Description, claim2.SizeXYZ));
				}
				i++;
			}
			if (groups.Any((PlayerGroupMembership g) => g.GroupName.Equals(claim2.OwnedByPlayerGroupUid)))
			{
				if (allowCoordinateHud)
				{
					groupOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³ at {3}) (group owned)", i, claim2.Description, claim2.SizeXYZ, center2));
				}
				else
				{
					groupOwnedTexts.Add(Lang.Get("{0}: {1} ({2}m³) (group owned)", i, claim2.Description, claim2.SizeXYZ));
				}
			}
		}
		return TextCommandResult.Success(Lang.Get("land-claim-list", string.Join("\n", playerOwnedTexts)));
	}

	private TextCommandResult addCurrentArea(TextCommandCallingArgs cargs)
	{
		List<LandClaim> allclaims = server.SaveGameData.LandClaims;
		IServerPlayer fromPlayer = cargs.Caller.Player as IServerPlayer;
		List<LandClaim> ownclaims = GetPlayerClaims(server, cargs.Caller.Player.PlayerUID);
		return acquireClaimInProgress(cargs, delegate(TextCommandCallingArgs args, ClaimInProgress claimp)
		{
			if (claimp.Start == null || claimp.End == null)
			{
				return TextCommandResult.Error(Lang.Get("Start or End not marked"));
			}
			Cuboidi cuboidi = new Cuboidi(claimp.Start, claimp.End);
			if (cuboidi.SizeX < fromPlayer.Role.LandClaimMinSize.X || cuboidi.SizeY < fromPlayer.Role.LandClaimMinSize.Y || cuboidi.SizeZ < fromPlayer.Role.LandClaimMinSize.Z)
			{
				return TextCommandResult.Error(Lang.Get("Cannot add area. Your marked area has a size of {0}x{1}x{2} which is to small, needs to be at least {3}x{4}x{5}", cuboidi.SizeX, cuboidi.SizeY, cuboidi.SizeZ, fromPlayer.Role.LandClaimMinSize.X, fromPlayer.Role.LandClaimMinSize.Y, fromPlayer.Role.LandClaimMinSize.Z));
			}
			int num = cuboidi.SizeXYZ;
			foreach (LandClaim current in ownclaims)
			{
				num += current.SizeXYZ;
			}
			if (num > (long)fromPlayer.Role.LandClaimAllowance + (long)fromPlayer.ServerData.ExtraLandClaimAllowance)
			{
				return TextCommandResult.Error(Lang.Get("Cannot add area. Adding this area of size {0}m³ would bring your total claim size up to {1}m³, but your max allowance is {2}m³", cuboidi.SizeXYZ, num, fromPlayer.Role.LandClaimAllowance));
			}
			for (int i = 0; i < allclaims.Count; i++)
			{
				if (allclaims[i].Intersects(cuboidi))
				{
					return TextCommandResult.Error(Lang.Get("Cannot add area. This area overlaps with with another claim by {0}. Please correct your start/end position", allclaims[i].LastKnownOwnerName));
				}
			}
			EnumClaimError enumClaimError = claimp.Claim.AddArea(cuboidi);
			if (enumClaimError != 0)
			{
				return TextCommandResult.Error((enumClaimError == EnumClaimError.Overlapping) ? Lang.Get("Cannot add area. This area overlaps with your other claims. Please correct your start/end position") : Lang.Get("Cannot add area. This area is not adjacent to other claims. Please correct your start/end position"));
			}
			claimp.Start = null;
			claimp.End = null;
			ResendHighlights(fromPlayer, claimp.Claim, claimp.Start, claimp.End);
			return TextCommandResult.Success(Lang.Get("Ok, Land claim area added"));
		});
	}

	private TextCommandResult GrowSelection(IPlayer plr, ClaimInProgress claimp, BlockFacing facing, int size)
	{
		if (claimp.Start == null || claimp.End == null)
		{
			return TextCommandResult.Error(Lang.Get("Define start and end position first"));
		}
		if (facing == BlockFacing.UP)
		{
			if (claimp.Start.Y < claimp.End.Y)
			{
				claimp.End.Y += size;
			}
			else
			{
				claimp.Start.Y += size;
			}
		}
		if (facing == BlockFacing.DOWN)
		{
			if (claimp.Start.Y < claimp.End.Y)
			{
				claimp.Start.Y -= size;
			}
			else
			{
				claimp.End.Y -= size;
			}
		}
		if (facing == BlockFacing.NORTH)
		{
			if (claimp.Start.Z < claimp.End.Z)
			{
				claimp.Start.Z -= size;
			}
			else
			{
				claimp.End.Z -= size;
			}
		}
		if (facing == BlockFacing.EAST)
		{
			if (claimp.Start.X > claimp.End.X)
			{
				claimp.Start.X += size;
			}
			else
			{
				claimp.End.X += size;
			}
		}
		if (facing == BlockFacing.WEST)
		{
			if (claimp.Start.X < claimp.End.X)
			{
				claimp.Start.X -= size;
			}
			else
			{
				claimp.End.X -= size;
			}
		}
		if (facing == BlockFacing.SOUTH)
		{
			if (claimp.Start.Z > claimp.End.Z)
			{
				claimp.Start.Z += size;
			}
			else
			{
				claimp.End.Z += size;
			}
		}
		ResendHighlights(plr, claimp.Claim, claimp.Start, claimp.End);
		return TextCommandResult.Success(Lang.Get("Ok, area extended {0} by {1} blocks", facing, size));
	}

	private void ResendHighlights(IPlayer toPlayer, LandClaim claim)
	{
		ResendHighlights(toPlayer, claim, null, null);
	}

	private void ResendHighlights(IPlayer toPlayer, LandClaim claim, BlockPos claimingStartPos, BlockPos claimingEndPos)
	{
		List<BlockPos> startEnds = new List<BlockPos>();
		List<int> colors = new List<int>();
		if (claim != null)
		{
			foreach (Cuboidi area in claim.Areas)
			{
				startEnds.Add(area.Start.ToBlockPos());
				startEnds.Add(area.End.ToBlockPos());
				colors.Add(claimedColor);
			}
		}
		if (claimingStartPos != null && claimingEndPos != null)
		{
			startEnds.Add(claimingStartPos);
			startEnds.Add(claimingEndPos);
			colors.Add(claimingColor);
		}
		server.api.World.HighlightBlocks(toPlayer, 3, startEnds, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
	}

	public static List<LandClaim> GetPlayerClaims(ServerMain server, string playerUid)
	{
		List<LandClaim> landClaims = server.SaveGameData.LandClaims;
		List<LandClaim> ownclaims = new List<LandClaim>();
		foreach (LandClaim claim in landClaims)
		{
			if (claim.OwnedByPlayerUid == playerUid)
			{
				ownclaims.Add(claim);
			}
		}
		return ownclaims;
	}
}
