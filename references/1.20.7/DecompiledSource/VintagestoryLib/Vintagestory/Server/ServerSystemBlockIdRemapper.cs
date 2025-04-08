using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerSystemBlockIdRemapper : ServerSystem
{
	public ServerSystemBlockIdRemapper(ServerMain server)
		: base(server)
	{
		server.ModEventManager.AssetsFirstLoaded += RemapBlocks;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("bir").RequiresPrivilege(Privilege.controlserver).WithDescription("Block id remapper info and fixing tool")
			.BeginSubCommand("list")
			.WithDescription("list")
			.HandleWith(OnCmdList)
			.EndSubCommand()
			.BeginSubCommand("getcode")
			.WithDescription("getcode")
			.WithArgs(parsers.Int("blockId"))
			.HandleWith(OnCmdGetcode)
			.EndSubCommand()
			.BeginSubCommand("getid")
			.WithDescription("getid")
			.WithArgs(parsers.Word("domainAndPath"))
			.HandleWith(OnCmdGetid)
			.EndSubCommand()
			.BeginSubCommand("map")
			.WithDescription("map")
			.RequiresPlayer()
			.WithArgs(parsers.Word("new_block"), parsers.Word("old_block"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdMap)
			.EndSubCommand()
			.BeginSubCommand("remap")
			.WithAlias("remapq")
			.WithDescription("map")
			.RequiresPlayer()
			.WithArgs(parsers.Word("new_block"), parsers.Word("old_block"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdReMap)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdReMap(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> storedBlockCodesById = LoadStoredBlockCodesById();
		bool quiet = args.SubCmdCode == "remapq";
		string newBlock = args[0] as string;
		string oldBlock = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (int.TryParse(newBlock, out var newBlockId) && int.TryParse(oldBlock, out var oldBlockId))
		{
			MapById(storedBlockCodesById, newBlockId, oldBlockId, player, args.Caller.FromChatGroupId, remap: true, force, quiet);
			return TextCommandResult.Success();
		}
		MapByCode(storedBlockCodesById, new AssetLocation(newBlock), new AssetLocation(oldBlock), player, args.Caller.FromChatGroupId, remap: true, force, quiet);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMap(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> storedBlockCodesById = LoadStoredBlockCodesById();
		string newBlock = args[0] as string;
		string oldBlock = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (int.TryParse(newBlock, out var newBlockId) && int.TryParse(oldBlock, out var oldBlockId))
		{
			MapById(storedBlockCodesById, newBlockId, oldBlockId, player, args.Caller.FromChatGroupId, remap: false, force, quiet: false);
			return TextCommandResult.Success();
		}
		MapByCode(storedBlockCodesById, new AssetLocation(newBlock), new AssetLocation(oldBlock), player, args.Caller.FromChatGroupId, remap: false, force, quiet: false);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGetid(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> storedBlockCodesById = LoadStoredBlockCodesById();
		string domainAndPath = args[0] as string;
		if (!storedBlockCodesById.ContainsValue(new AssetLocation(domainAndPath)))
		{
			return TextCommandResult.Success("No mapping for blockcode " + domainAndPath + " found");
		}
		return TextCommandResult.Success("Blockcode " + domainAndPath + " is currently mapped to " + storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(new AssetLocation(domainAndPath))).Key);
	}

	private TextCommandResult OnCmdGetcode(TextCommandCallingArgs args)
	{
		int blockId = (int)args[0];
		Dictionary<int, AssetLocation> storedBlockCodesById = LoadStoredBlockCodesById();
		if (!storedBlockCodesById.ContainsKey(blockId))
		{
			return TextCommandResult.Success("No mapping for blockid " + blockId + " found");
		}
		return TextCommandResult.Success("Blockid " + blockId + " is currently mapped to " + storedBlockCodesById[blockId]);
	}

	private TextCommandResult OnCmdList(TextCommandCallingArgs args)
	{
		Dictionary<int, AssetLocation> dictionary = LoadStoredBlockCodesById();
		ServerMain.Logger.Notification("Current block id mapping (issued by /bir list command)");
		foreach (KeyValuePair<int, AssetLocation> val in dictionary)
		{
			ServerMain.Logger.Notification("  " + val.Key + ": " + val.Value);
		}
		return TextCommandResult.Success("Full mapping printed to console and main log file");
	}

	private void MapById(Dictionary<int, AssetLocation> storedBlockCodesById, int newBlockId, int oldBlockId, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
	{
		if (!force && storedBlockCodesById.TryGetValue(oldBlockId, out var oldBlockCode))
		{
			player.SendMessage(groupId, string.Concat("newblockid ", oldBlockId.ToString(), " is already mapped to ", oldBlockCode, ", type '/bir ", remap ? "remap" : "map", " ", newBlockId.ToString(), " ", oldBlockId.ToString(), " force' to overwrite"), EnumChatType.CommandError);
			return;
		}
		AssetLocation newCode = (storedBlockCodesById[oldBlockId] = storedBlockCodesById[newBlockId]);
		if (remap)
		{
			storedBlockCodesById.Remove(newBlockId);
		}
		if (!quiet)
		{
			string type = (remap ? "remapped" : "mapped");
			player.SendMessage(groupId, string.Concat(newCode, " is now ", type, " to id ", oldBlockId.ToString()), EnumChatType.CommandSuccess);
		}
		StoreBlockCodesById(storedBlockCodesById);
	}

	private void MapByCode(Dictionary<int, AssetLocation> storedBlockCodesById, AssetLocation newCode, AssetLocation oldCode, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
	{
		if (!storedBlockCodesById.ContainsValue(newCode))
		{
			player.SendMessage(groupId, string.Concat("No mapping for blockcode ", newCode, " found"), EnumChatType.CommandError);
			return;
		}
		if (!storedBlockCodesById.ContainsValue(oldCode))
		{
			player.SendMessage(groupId, string.Concat("No mapping for blockcode ", oldCode, " found"), EnumChatType.CommandError);
			return;
		}
		if (!force)
		{
			player.SendMessage(groupId, string.Concat("Both block codes found. Type '/bir ", remap ? "remap" : "map", " ", newCode, " ", oldCode, " force' to make the remap permanent."), EnumChatType.CommandError);
			return;
		}
		int newBlockId = storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(newCode)).Key;
		int oldBlockId = storedBlockCodesById.FirstOrDefault((KeyValuePair<int, AssetLocation> x) => x.Value.Equals(oldCode)).Key;
		storedBlockCodesById[oldBlockId] = newCode;
		if (remap)
		{
			storedBlockCodesById.Remove(newBlockId);
		}
		if (!quiet)
		{
			string type = (remap ? "remapped" : "mapped");
			player.SendMessage(groupId, string.Concat(newCode, " is now ", type, " to id ", oldBlockId.ToString()), EnumChatType.CommandSuccess);
		}
		StoreBlockCodesById(storedBlockCodesById);
	}

	private void RemapBlocks()
	{
		ServerMain.Logger.Event("Remapping blocks and items...");
		ServerMain.Logger.VerboseDebug("BlockID Remapper: Begin");
		Dictionary<AssetLocation, int> currentBlockIdsByCode = new Dictionary<AssetLocation, int>();
		Dictionary<int, AssetLocation> newBlockCodesById = new Dictionary<int, AssetLocation>();
		Dictionary<int, AssetLocation> missingBlockCodesById = new Dictionary<int, AssetLocation>();
		Dictionary<int, int> remappedBlockIds = new Dictionary<int, int>();
		for (int k = 0; k < server.Blocks.Count; k++)
		{
			Block block7 = server.Blocks[k];
			if (block7 != null && !(block7.Code == null))
			{
				currentBlockIdsByCode[block7.Code] = block7.BlockId;
			}
		}
		Dictionary<int, AssetLocation> storedBlockCodesById = LoadStoredBlockCodesById();
		if (storedBlockCodesById == null)
		{
			storedBlockCodesById = new Dictionary<int, AssetLocation>();
		}
		if (server.Config.RepairMode)
		{
			int maxBlockId = 0;
			Dictionary<string, int> countByDomain = new Dictionary<string, int>();
			server.api.Logger.Notification("Stored blocks by mod domain:");
			foreach (KeyValuePair<int, AssetLocation> val6 in storedBlockCodesById)
			{
				AssetLocation code3 = val6.Value;
				countByDomain.TryGetValue(code3.Domain, out var cnt);
				countByDomain[code3.Domain] = cnt + 1;
				maxBlockId = Math.Max(maxBlockId, val6.Key);
			}
			foreach (KeyValuePair<string, int> val5 in countByDomain)
			{
				ServerMain.Logger.Notification("{0}: {1}", val5.Key, val5.Value);
			}
		}
		int maxBlockID = 0;
		foreach (KeyValuePair<int, AssetLocation> val4 in storedBlockCodesById)
		{
			AssetLocation code2 = val4.Value;
			int oldBlockId2 = val4.Key;
			maxBlockID = Math.Max(oldBlockId2, maxBlockID);
			if (!currentBlockIdsByCode.TryGetValue(code2, out var newBlockId2))
			{
				missingBlockCodesById.Add(oldBlockId2, code2);
			}
			else if (newBlockId2 != oldBlockId2)
			{
				remappedBlockIds[newBlockId2] = oldBlockId2;
			}
		}
		for (int j = 0; j < server.Blocks.Count; j++)
		{
			Block block6 = server.Blocks[j];
			if (block6 != null)
			{
				maxBlockID = Math.Max(block6.BlockId, maxBlockID);
			}
		}
		server.nextFreeBlockId = maxBlockID + 1;
		ServerMain.Logger.VerboseDebug("Max BlockID is " + maxBlockID);
		bool isNewWorld = storedBlockCodesById.Count == 0;
		HashSet<AssetLocation> storedBlockCodes = new HashSet<AssetLocation>(storedBlockCodesById.Values);
		foreach (KeyValuePair<AssetLocation, int> val3 in currentBlockIdsByCode)
		{
			AssetLocation code = val3.Key;
			if (!storedBlockCodes.Contains(code))
			{
				newBlockCodesById[val3.Value] = code;
			}
		}
		ServerMain.Logger.VerboseDebug("Found {0} blocks requiring remapping and {1} new blocks", remappedBlockIds.Count, newBlockCodesById.Count);
		StringBuilder codes = new StringBuilder();
		List<Block> newBlocks = new List<Block>();
		foreach (KeyValuePair<int, AssetLocation> item in newBlockCodesById)
		{
			int curblockId = item.Key;
			Block block5 = server.Blocks[curblockId];
			newBlocks.Add(block5);
			if (!isNewWorld)
			{
				server.Blocks[curblockId] = new Block
				{
					BlockId = curblockId
				};
			}
		}
		List<Block> blocksToRemap = new List<Block>();
		int maxNewId = 0;
		foreach (KeyValuePair<int, int> val2 in remappedBlockIds)
		{
			if (codes.Length > 0)
			{
				codes.Append(", ");
			}
			int oldBlockId = val2.Key;
			int newBlockId = val2.Value;
			maxNewId = Math.Max(maxNewId, newBlockId);
			Block block4 = server.Blocks[oldBlockId];
			block4.BlockId = newBlockId;
			blocksToRemap.Add(block4);
			server.Blocks[oldBlockId] = new Block
			{
				BlockId = oldBlockId,
				IsMissing = true
			};
			codes.Append(oldBlockId + "=>" + newBlockId);
		}
		(server.Blocks as BlockList).PreAlloc(maxNewId);
		foreach (Block block3 in blocksToRemap)
		{
			server.RemapBlock(block3);
		}
		if (!isNewWorld)
		{
			int newBlocksCount = 0;
			foreach (Block block2 in newBlocks)
			{
				if (block2.BlockId != 0)
				{
					server.BlocksByCode.Remove(block2.Code);
					server.RegisterBlock(block2);
					newBlocksCount++;
				}
			}
			ServerMain.Logger.VerboseDebug("Remapped {0} new blockids", newBlocksCount);
		}
		maxBlockID = 0;
		for (int i = 0; i < server.Blocks.Count; i++)
		{
			if (server.Blocks[i] != null)
			{
				maxBlockID = Math.Max(server.Blocks[i].BlockId, maxBlockID);
			}
		}
		server.nextFreeBlockId = maxBlockID + 1;
		if (codes.Length > 0)
		{
			ServerMain.Logger.VerboseDebug("Remapped {0} existing blockids", remappedBlockIds.Count);
		}
		ServerMain.Logger.Debug("Found {0} missing blocks", missingBlockCodesById.Count);
		codes = new StringBuilder();
		FastSmallDictionary<string, CompositeTexture> unknownTex = new FastSmallDictionary<string, CompositeTexture>("all", new CompositeTexture(new AssetLocation("unknown")));
		foreach (KeyValuePair<int, AssetLocation> val in missingBlockCodesById)
		{
			if (codes.Length > 0)
			{
				codes.Append(", ");
			}
			server.FillMissingBlock(val.Key, new Block
			{
				Textures = unknownTex,
				Code = val.Value,
				DrawType = EnumDrawType.Cube,
				MatterState = EnumMatterState.Solid,
				IsMissing = true,
				Replaceable = 1
			});
			codes.Append(val.Value.ToShortString());
		}
		if (codes.Length > 0)
		{
			ServerMain.Logger.Debug("Added unknown block for {0} blocks", missingBlockCodesById.Count);
			ServerMain.Logger.Debug(codes.ToString());
		}
		foreach (Block block in newBlocks)
		{
			storedBlockCodesById[block.BlockId] = block.Code;
		}
		if (newBlocks.Count > 0)
		{
			ServerMain.Logger.Debug("Added {0} new blocks to the mapping", newBlocks.Count);
		}
		StoreBlockCodesById(storedBlockCodesById);
	}

	public Dictionary<int, AssetLocation> LoadStoredBlockCodesById()
	{
		Dictionary<int, AssetLocation> blocks = new Dictionary<int, AssetLocation>();
		try
		{
			byte[] data = server.api.WorldManager.SaveGame.GetData("BlockIDs");
			if (data != null)
			{
				Dictionary<int, string> dictionary = Serializer.Deserialize<Dictionary<int, string>>(new MemoryStream(data));
				blocks = new Dictionary<int, AssetLocation>();
				foreach (KeyValuePair<int, string> entry in dictionary)
				{
					blocks.Add(entry.Key, new AssetLocation(entry.Value));
				}
				ServerMain.Logger.VerboseDebug(blocks.Count + " block IDs loaded from savegame.");
			}
			else
			{
				ServerMain.Logger.Debug("Block IDs not found in savegame.");
			}
		}
		catch
		{
			throw new Exception("Error at loading blocks!");
		}
		return blocks;
	}

	public void StoreBlockCodesById(Dictionary<int, AssetLocation> storedBlockCodesById)
	{
		int maxId = 0;
		MemoryStream ms = new MemoryStream();
		Dictionary<int, string> blocksOld = new Dictionary<int, string>();
		foreach (KeyValuePair<int, AssetLocation> entry in storedBlockCodesById)
		{
			maxId = Math.Max(maxId, entry.Key);
			blocksOld.Add(entry.Key, entry.Value.ToShortString());
		}
		Serializer.Serialize((Stream)ms, blocksOld);
		server.api.WorldManager.SaveGame.StoreData("BlockIDs", ms.ToArray());
		ServerMain.Logger.Debug("Block IDs have been written to savegame. Saved max BlockID was " + maxId);
	}
}
