using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class CmdDebug
{
	private class Eproprs
	{
		public EntityProperties Props;

		public SKColor Color;
	}

	private Thread MainThread;

	private ServerMain server;

	public CmdDebug(ServerMain server)
	{
		CmdDebug cmdDebug = this;
		MainThread = Thread.CurrentThread;
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		_ = server.api;
		chatCommands.GetOrCreate("debug").WithDesc("Debug and Developer utilities").RequiresPrivilege(Privilege.controlserver)
			.BeginSub("blockcodes")
			.WithDesc("Print codes of all loaded blocks to the server log file")
			.HandleWith(printBlockCodes)
			.EndSub()
			.BeginSub("itemcodes")
			.WithDesc("Print codes of all loaded items to the server log file")
			.HandleWith(printItemCodes)
			.EndSub()
			.BeginSub("blockstats")
			.WithDesc("Generates counds amount of block ids used, grouped by first block code part, prints it to the server log file")
			.HandleWith(printBlockStats)
			.EndSub()
			.BeginSub("helddurability")
			.WithAlias("helddura")
			.WithDesc("Set held item durability")
			.WithArgs(parsers.Int("durability"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				ItemSlot activeHandItemSlot2 = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
				if (activeHandItemSlot2.Itemstack == null)
				{
					return TextCommandResult.Error("Nothing in active hands");
				}
				getSetItemStackAttr(activeHandItemSlot2, "durability", "int", ((int)args[0]).ToString() ?? "");
				return TextCommandResult.Success((int)args[0] + " durability set.");
			})
			.EndSub()
			.BeginSub("heldtemperature")
			.WithAlias("heldtemp")
			.WithDesc("Set held item temperature")
			.WithArgs(parsers.Int("temperature in °C"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				ItemSlot activeHandItemSlot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
				ItemStack itemstack = activeHandItemSlot.Itemstack;
				if (itemstack == null)
				{
					return TextCommandResult.Error("Nothing in active hands");
				}
				int num = (int)args[0];
				itemstack.Collectible.SetTemperature(server, itemstack, num);
				activeHandItemSlot.MarkDirty();
				return TextCommandResult.Success(num + " °C set.");
			})
			.EndSub()
			.BeginSub("heldcoattr")
			.WithDesc("Get/Set collectible attributes of the currently held item")
			.WithArgs(parsers.Word("key"), parsers.OptionalAll("value"))
			.HandleWith(getSetCollectibleAttr)
			.EndSub()
			.BeginSub("heldstattr")
			.WithDesc("Get/Set itemstack attributes of the currently held item")
			.WithArgs(parsers.Word("key"), parsers.OptionalWordRange("type", "int", "bool", "string", "tree", "double", "float"), parsers.OptionalAll("value"))
			.HandleWith(getSetItemstackAttr)
			.EndSub()
			.BeginSub("netbench")
			.WithDesc("Toggle network benchmarking mode")
			.HandleWith(toggleNetworkBenchmarking)
			.EndSub()
			.BeginSub("rebuildlandclaimpartitions")
			.WithDesc("Rebuild land claim partitions")
			.HandleWith(delegate
			{
				server.WorldMap.RebuildLandClaimPartitions();
				return TextCommandResult.Success("Partitioned land claim index rebuilt");
			})
			.EndSub()
			.BeginSub("privileges")
			.WithDesc("Toggle privileges debug mode")
			.WithArgs(parsers.OptionalBool("on"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				if (args.Parsers[0].IsMissing)
				{
					return TextCommandResult.Success("Privilege debugging is currently " + (server.DebugPrivileges ? "on" : "off"));
				}
				server.DebugPrivileges = (bool)args[0];
				return TextCommandResult.Success("Privilege debugging now " + (server.DebugPrivileges ? "on" : "off"));
			})
			.EndSub()
			.BeginSub("cloh")
			.WithDesc("Compact the large object heap")
			.HandleWith(delegate
			{
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect();
				return TextCommandResult.Success("Ok, compacted large object heap");
			})
			.EndSub()
			.BeginSub("logticks")
			.WithDesc("Toggle slow tick profiler")
			.WithArgs(parsers.Int("millisecond threshold"), parsers.OptionalBool("include offthreads"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				ServerMain.FrameProfiler.PrintSlowTicks = !ServerMain.FrameProfiler.PrintSlowTicks;
				ServerMain.FrameProfiler.Enabled = ServerMain.FrameProfiler.PrintSlowTicks;
				ServerMain.FrameProfiler.PrintSlowTicksThreshold = (int)args[0];
				if ((!args.Parsers[1].IsMissing && (bool)args[1]) || !ServerMain.FrameProfiler.Enabled)
				{
					FrameProfilerUtil.PrintSlowTicks_Offthreads = ServerMain.FrameProfiler.PrintSlowTicks;
					FrameProfilerUtil.PrintSlowTicksThreshold_Offthreads = ServerMain.FrameProfiler.PrintSlowTicksThreshold;
					FrameProfilerUtil.offThreadProfiles = new ConcurrentQueue<string>();
				}
				ServerMain.FrameProfiler.Begin(null);
				return TextCommandResult.Success("Server Tick Profiling now " + (ServerMain.FrameProfiler.PrintSlowTicks ? ("on, threshold " + ServerMain.FrameProfiler.PrintSlowTicksThreshold + " ms") : "off"));
			})
			.EndSub()
			.BeginSub("octagonpoints")
			.WithDesc("Exports a map of chunks that ought to be sent to the client as a png image")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				cmdDebug.PrintOctagonPoints(args.Caller.Player.WorldData.DesiredViewDistance);
				return TextCommandResult.Success("Printed octagon points");
			})
			.EndSub()
			.BeginSub("tickposition")
			.WithDesc("Print current server tick position (for debugging a frozen server)")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(server.TickPosition.ToString() ?? ""))
			.EndSub()
			.BeginSub("mainthreadstate")
			.WithDesc("Print current main thread state")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(cmdDebug.MainThread.ThreadState.ToString()))
			.EndSub()
			.BeginSub("threadpoolstate")
			.WithDesc("Print current thread pool state")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(TyronThreadPool.Inst.ListAllRunningTasks() + "\n" + TyronThreadPool.Inst.ListAllThreads()))
			.EndSub()
			.BeginSub("tickhandlers")
			.WithDesc("Counts amount of game tick listeners grouped by listener type")
			.HandleWith(countTickHandlers)
			.BeginSub("dump")
			.WithDesc("Export full list of all listeners to the log file")
			.WithArgs(parsers.Word("Listener type"))
			.HandleWith(exportTickHandlers)
			.EndSub()
			.EndSub()
			.BeginSub("chunk")
			.WithDesc("Chunk debug utilities")
			.BeginSub("queue")
			.WithAlias("q")
			.WithDesc("Amount of generating chunks in queue")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success($"Currently {server.chunkThread.requestedChunkColumns.Count} chunks in generation queue"))
			.EndSub()
			.BeginSub("stats")
			.WithDesc("Statics on currently loaded chunks")
			.HandleWith(getChunkStats)
			.EndSub()
			.BeginSub("printmap")
			.WithDesc("Exports a map of loaded chunk as a png image")
			.HandleWith(delegate
			{
				server.WorldMap.PrintChunkMap(new Vec2i(server.MapSize.X / 2 / 32, server.MapSize.Z / 2 / 32));
				return TextCommandResult.Success("Printed chunk map");
			})
			.EndSub()
			.BeginSub("here")
			.WithDesc("Information about the chunk at the callers position")
			.HandleWith(getHereChunkInfo)
			.EndSub()
			.BeginSub("resend")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.WithDesc("Resend a chunk to all players")
			.HandleWith(resendChunk)
			.EndSub()
			.BeginSub("relight")
			.WithArgs(parsers.OptionalWorldPosition("position"))
			.WithDesc("Relight a chunk for all players")
			.HandleWith(relightChunk)
			.EndSub()
			.EndSub()
			.BeginSub("sendchunks")
			.WithDescription("Allows toggling of the normal chunk generation/sending operations to all clients.")
			.WithAdditionalInformation("Force loaded chunks are not affected by this switch.")
			.WithArgs(parsers.Bool("state"))
			.HandleWith(toggleSendChunks)
			.EndSub()
			.BeginSub("expclang")
			.WithDescription("Export a list of missing block and item translations, with suggestions")
			.HandleWith(handleExpCLang)
			.EndSub()
			.BeginSub("blu")
			.WithDesc("Place every block type in the game")
			.HandleWith(handleBlu)
			.EndSub()
			.BeginSub("dumpanimstate")
			.WithDesc("Dump animation state into log file")
			.WithArgs(parsers.Entities("target entity"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => cmdDebug.handleDumpAnimState(e, args)))
			.EndSub()
			.BeginSub("dumprecipes")
			.WithDesc("Dump grid recipes into log file")
			.HandleWith(delegate
			{
				foreach (GridRecipe current in server.GridRecipes)
				{
					bool flag = false;
					foreach (KeyValuePair<string, CraftingRecipeIngredient> current2 in current.Ingredients)
					{
						if (current2.Value.ResolvedItemstack?.TempAttributes != null && current2.Value.ResolvedItemstack.TempAttributes.Count > 0)
						{
							if (!flag)
							{
								ServerMain.Logger.VerboseDebug(current.Name);
							}
							flag = true;
							ServerMain.Logger.VerboseDebug(current2.Key + ": " + current2.Value.ToString() + "/" + current2.Value.ResolvedItemstack.TempAttributes);
						}
					}
				}
				return TextCommandResult.Success();
			})
			.EndSub()
			.BeginSubCommand("spawnheatmap")
			.WithDescription("spawnheatmap")
			.HandleWith(OnCmdSpawnHeatmap)
			.WithArgs(parsers.WordRange("x-axis", "temp", "rain", "forest", "elevation"), parsers.WordRange("y-axis", "temp", "rain", "forest", "elevation"), parsers.OptionalWord("entity type"), parsers.OptionalBool("negate entity type filter"))
			.EndSubCommand()
			.Validate();
	}

	private TextCommandResult handleDumpAnimState(Entity e, TextCommandCallingArgs args)
	{
		ServerMain.Logger.Notification(e.AnimManager?.Animator?.DumpCurrentState());
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdSpawnHeatmap(TextCommandCallingArgs args)
	{
		byte[] data = server.AssetManager.TryGet("textures/environment/planttint.png").Data;
		BitmapExternal bmp = new BitmapExternal(data, data.Length, ServerMain.Logger);
		List<Eproprs> eprops = new List<Eproprs>();
		string xaxis = args[0] as string;
		string yaxis = args[1] as string;
		AssetLocation etype = (args.Parsers[2].IsMissing ? null : new AssetLocation(args[2] as string));
		bool negate = (bool)args[3];
		Random rand = new Random(0);
		foreach (EntityProperties props in server.EntityTypes)
		{
			RuntimeSpawnConditions spawnconds = props.Server?.SpawnConditions?.Runtime;
			if (spawnconds == null || props.Code.Path.Contains("drifter") || props.Code.Path.Contains("butter"))
			{
				continue;
			}
			if (etype != null)
			{
				bool match = !WildcardUtil.Match(etype, props.Code);
				if ((match && !negate) || (!match && negate))
				{
					continue;
				}
			}
			if (spawnconds.MaxQuantity > 0 || (spawnconds.MaxQuantityByGroup != null && spawnconds.MaxQuantityByGroup.MaxQuantity > 0))
			{
				SKColor skcol = ((props.Color != null && !(props.Color == "")) ? new SKColor(((uint)ColorUtil.Hex2Int(props.Color) & 0xFFFFFFu) | 0x60000000u) : new SKColor(((uint)(int)rand.NextInt64() & 0xFFFFFFu) | 0x60000000u));
				eprops.Add(new Eproprs
				{
					Props = props,
					Color = skcol
				});
				ServerMain.Logger.Notification(props.Code.ToString());
			}
		}
		for (int x = 0; x < 256; x++)
		{
			for (int y = 0; y < 256; y++)
			{
				for (int i = 0; i < eprops.Count; i++)
				{
					Eproprs eprop = eprops[i];
					RuntimeSpawnConditions spc = eprop.Props.Server.SpawnConditions.Runtime;
					float tempx = GameMath.Clamp(((float)x - 0f) / 4.25f - 20f, -20f, 40f);
					float tempy = GameMath.Clamp(((float)y - 0f) / 4.25f - 20f, -20f, 40f);
					float relx = (float)x / 255f;
					float rely = (float)y / 255f;
					bool xAxisOk = false;
					bool yAxisOk = false;
					switch (xaxis)
					{
					case "temp":
						xAxisOk = tempx >= spc.MinTemp && tempx <= spc.MaxTemp;
						break;
					case "rain":
						xAxisOk = rely >= spc.MinRain && rely <= spc.MaxRain;
						break;
					case "forest":
						xAxisOk = relx >= spc.MinForest && relx <= spc.MaxForest;
						break;
					case "elevation":
						xAxisOk = relx >= spc.MinY - 1f && relx <= spc.MaxY - 1f;
						break;
					}
					switch (yaxis)
					{
					case "temp":
						yAxisOk = tempy >= spc.MinTemp && tempy <= spc.MaxTemp;
						break;
					case "rain":
						yAxisOk = rely >= spc.MinRain && rely <= spc.MaxRain;
						break;
					case "forest":
						yAxisOk = rely >= spc.MinForest && rely <= spc.MaxForest;
						break;
					case "elevation":
						yAxisOk = rely >= spc.MinY - 1f && rely <= spc.MaxY - 1f;
						break;
					}
					if (xAxisOk && yAxisOk)
					{
						int outcol = ColorUtil.ColorOverlay(bmp.bmp.GetPixel(4 + x, 4 + y).ToInt(), eprop.Color.ToInt(), (float)(int)eprop.Color.Alpha / 255f);
						SKColor overlaidcol = new SKColor((uint)outcol);
						bmp.bmp.SetPixel(4 + x, 4 + y, overlaidcol);
					}
				}
			}
		}
		bmp.Save("spawnheatmap.png");
		if (!(etype == null))
		{
			return TextCommandResult.Success("ok, spawnheatmap.png generated for " + etype.Path + ". Also printed matching entities to server-main.log");
		}
		return TextCommandResult.Success("ok, spawnheatmap.png generated. Also printed matching entities to server-main.log");
	}

	private TextCommandResult handleBlu(TextCommandCallingArgs args)
	{
		BlockLineup(args.Caller.Pos.AsBlockPos, args.RawArgs);
		return TextCommandResult.Success("Block lineup created");
	}

	private void BlockLineup(BlockPos pos, CmdArgs args)
	{
		IList<Block> blocks = server.World.Blocks;
		bool all = args.PopWord() == "all";
		List<Block> existingBlocks = new List<Block>();
		for (int j = 0; j < blocks.Count; j++)
		{
			Block block = blocks[j];
			if (block != null && !(block.Code == null))
			{
				if (all)
				{
					existingBlocks.Add(block);
				}
				else if (block.CreativeInventoryTabs != null && block.CreativeInventoryTabs.Length != 0)
				{
					existingBlocks.Add(block);
				}
			}
		}
		int width = (int)Math.Sqrt(existingBlocks.Count);
		for (int i = 0; i < existingBlocks.Count; i++)
		{
			server.World.BlockAccessor.SetBlock(existingBlocks[i].BlockId, pos.AddCopy(i / width, 0, i % width));
		}
	}

	private TextCommandResult printBlockCodes(TextCommandCallingArgs args)
	{
		Dictionary<string, int> blockcodes = new Dictionary<string, int>();
		foreach (Block block in server.Blocks)
		{
			if (!(block.Code == null))
			{
				string key = block.Code.ToShortString();
				blockcodes.TryGetValue(key, out var cnt);
				cnt = (blockcodes[key] = cnt + 1);
			}
		}
		List<KeyValuePair<string, int>> list = blockcodes.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList();
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<string, int> item in list)
		{
			sb.AppendLine(item.Key);
		}
		ServerMain.Logger.Notification(sb.ToString());
		return TextCommandResult.Success("Block codes written to log file.");
	}

	private TextCommandResult printItemCodes(TextCommandCallingArgs args)
	{
		Dictionary<string, int> itemcodes = new Dictionary<string, int>();
		foreach (Item item in server.Items)
		{
			if (!(item.Code == null))
			{
				string key = item.Code.ToShortString();
				itemcodes.TryGetValue(key, out var cnt);
				cnt = (itemcodes[key] = cnt + 1);
			}
		}
		List<KeyValuePair<string, int>> list = itemcodes.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList();
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<string, int> item2 in list)
		{
			sb.AppendLine(item2.Key);
		}
		ServerMain.Logger.Notification(sb.ToString());
		return TextCommandResult.Success("Item codes written to log file.");
	}

	private TextCommandResult printBlockStats(TextCommandCallingArgs args)
	{
		Dictionary<string, int> blockcodes = new Dictionary<string, int>();
		foreach (Block block in server.Blocks)
		{
			if (!(block.Code == null))
			{
				string key = block.Code.Domain + ":" + block.FirstCodePart();
				blockcodes.TryGetValue(key, out var cnt);
				cnt = (blockcodes[key] = cnt + 1);
			}
		}
		List<KeyValuePair<string, int>> list = blockcodes.OrderByDescending((KeyValuePair<string, int> p) => p.Value).ToList();
		StringBuilder sb = new StringBuilder();
		foreach (KeyValuePair<string, int> val in list)
		{
			sb.AppendLine(val.Key + ": " + val.Value);
		}
		ServerMain.Logger.Notification(sb.ToString());
		return TextCommandResult.Success("Block ids summary written to log file.");
	}

	private TextCommandResult getSetCollectibleAttr(TextCommandCallingArgs args)
	{
		ItemSlot slot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
		ItemStack stack = slot.Itemstack;
		if (stack == null)
		{
			return TextCommandResult.Success("Nothing in right hands");
		}
		string key = (string)args[0];
		string value = (string)args[1];
		JToken jtoken = stack.Collectible.Attributes.Token;
		if (key == null)
		{
			return TextCommandResult.Error("Syntax: /debug heldcoattr key value");
		}
		if (value == null)
		{
			return TextCommandResult.Success(Lang.Get("Collectible Attribute {0} has value {1}.", key, jtoken[key]));
		}
		jtoken[key] = JToken.Parse(value);
		slot.MarkDirty();
		return TextCommandResult.Success(Lang.Get("Collectible Attribute {0} set to {1}.", key, value));
	}

	private TextCommandResult getSetItemstackAttr(TextCommandCallingArgs args)
	{
		ItemSlot slot = (args.Caller.Entity as EntityAgent).ActiveHandItemSlot;
		if (slot.Itemstack == null)
		{
			return TextCommandResult.Error("Nothing in active hands");
		}
		string key = (string)args[0];
		string type = (string)args[1];
		string value = (string)args[2];
		return getSetItemStackAttr(slot, key, type, value);
	}

	private static TextCommandResult getSetItemStackAttr(ItemSlot slot, string key, string type, string value)
	{
		ItemStack stack = slot.Itemstack;
		if (type == null)
		{
			if (stack.Attributes.HasAttribute(key))
			{
				IAttribute attr = stack.Attributes[key];
				Type attrtype = TreeAttribute.AttributeIdMapping[attr.GetAttributeId()];
				return TextCommandResult.Success(Lang.Get("Attribute {0} is of type {1} and has value {2}", attrtype, attr.ToString()));
			}
			return TextCommandResult.Error(Lang.Get("Attribute {0} does not exist"));
		}
		switch (type)
		{
		case "int":
			stack.Attributes.SetInt(key, value.ToInt());
			break;
		case "bool":
			stack.Attributes.SetBool(key, value.ToBool());
			break;
		case "string":
			stack.Attributes.SetString(key, value);
			break;
		case "tree":
			stack.Attributes[key] = new JsonObject(JObject.Parse(value)).ToAttribute();
			break;
		case "double":
			stack.Attributes.SetDouble(key, value.ToDouble());
			break;
		case "float":
			stack.Attributes.SetFloat(key, value.ToFloat());
			break;
		default:
			return TextCommandResult.Error("Invalid type");
		}
		slot.MarkDirty();
		return TextCommandResult.Success($"Stack Attribute {key}={stack.Attributes[key].ToString()} set.");
	}

	private TextCommandResult toggleNetworkBenchmarking(TextCommandCallingArgs args)
	{
		if (server.doNetBenchmark)
		{
			server.doNetBenchmark = false;
			StringBuilder str = new StringBuilder();
			foreach (KeyValuePair<int, int> val2 in server.packetBenchmarkBytes)
			{
				SystemNetworkProcess.ServerPacketNames.TryGetValue(val2.Key, out var packetName3);
				int q2 = server.packetBenchmark[val2.Key];
				str.AppendLine(q2 + "x " + packetName3 + ": " + ((val2.Value > 9999) ? (((float)val2.Value / 1024f).ToString("#.#") + "kb") : (val2.Value + "b")));
			}
			str.AppendLine("-----");
			foreach (KeyValuePair<string, int> val3 in server.packetBenchmarkBlockEntitiesBytes)
			{
				string packetName2 = val3.Key;
				str.AppendLine("BE " + packetName2 + ": " + ((val3.Value > 9999) ? (((float)val3.Value / 1024f).ToString("#.#") + "kb") : (val3.Value + "b")));
			}
			str.AppendLine("-----");
			foreach (KeyValuePair<int, int> val in server.udpPacketBenchmarkBytes)
			{
				string packetName = val.Key.ToString();
				int q = server.udpPacketBenchmark[val.Key];
				str.AppendLine(q + "x " + packetName + ": " + ((val.Value > 9999) ? (((float)val.Value / 1024f).ToString("#.#") + "kb") : (val.Value + "b")));
			}
			return TextCommandResult.Success(str.ToString());
		}
		server.doNetBenchmark = true;
		server.packetBenchmark.Clear();
		server.packetBenchmarkBytes.Clear();
		server.packetBenchmarkBlockEntitiesBytes.Clear();
		server.udpPacketBenchmark.Clear();
		server.udpPacketBenchmarkBytes.Clear();
		return TextCommandResult.Success("Benchmarking started. Stop it after a while to get results.");
	}

	private TextCommandResult toggleSendChunks(TextCommandCallingArgs args)
	{
		server.SendChunks = (bool)args[0];
		return TextCommandResult.Success("Sending chunks is now " + (server.SendChunks ? "on" : "off"));
	}

	private TextCommandResult handleExpCLang(TextCommandCallingArgs args)
	{
		if (server.Config.HostedMode)
		{
			return TextCommandResult.Error("Can't access this feature, server is in hosted mode");
		}
		List<string> lines = new List<string>();
		for (int j = 0; j < server.Blocks.Count; j++)
		{
			Block block = server.Blocks[j];
			if (block != null && !(block.Code == null) && block.CreativeInventoryTabs != null && block.CreativeInventoryTabs.Length != 0 && block.GetHeldItemName(new ItemStack(block)) == block.Code?.Domain + ":block-" + block.Code?.Path)
			{
				string domain2 = block.Code.ShortDomain();
				if (domain2.Length > 0)
				{
					domain2 += ":";
				}
				lines.Add("\t\"" + domain2 + "block-" + block.Code.Path + "\": \"" + Lang.GetNamePlaceHolder(block.Code) + "\",");
			}
		}
		for (int i = 0; i < server.Items.Count; i++)
		{
			Item item = server.Items[i];
			if (item != null && !(item.Code == null) && item.CreativeInventoryTabs != null && item.CreativeInventoryTabs.Length != 0 && item.GetHeldItemName(new ItemStack(item)) == item.Code?.Domain + ":item-" + item.Code?.Path)
			{
				string domain = item.Code.ShortDomain();
				if (domain.Length > 0)
				{
					domain += ":";
				}
				lines.Add("\t\"" + domain + "item-" + item.Code.Path + "\": \"" + Lang.GetNamePlaceHolder(item.Code) + "\",");
			}
		}
		TreeAttribute tree = new TreeAttribute();
		server.api.eventapi.PushEvent("expclang", tree);
		foreach (KeyValuePair<string, IAttribute> item2 in tree)
		{
			string line = (item2.Value as StringAttribute)?.value;
			if (line != null)
			{
				lines.Add(line);
			}
		}
		lines.Sort();
		string outfilepath = "collectiblelang.json";
		using (TextWriter textWriter = new StreamWriter(outfilepath))
		{
			textWriter.Write(string.Join("\r\n", lines));
			textWriter.Close();
		}
		return TextCommandResult.Success("Ok, Missing translations exported to " + outfilepath);
	}

	private TextCommandResult getChunkStats(TextCommandCallingArgs args)
	{
		int total = 0;
		int packed = 0;
		int cntData = 0;
		int cntEmpty = 0;
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (KeyValuePair<long, ServerChunk> val in server.loadedChunks)
			{
				total++;
				if (val.Value.IsPacked())
				{
					packed++;
				}
				if (val.Value.Empty)
				{
					cntEmpty++;
				}
				else
				{
					cntData++;
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		ChunkDataPool pool = server.serverChunkDataPool;
		return TextCommandResult.Success(string.Format("{0} Total chunks ({1} with data and {2} empty)\n{3} of which are packed\nFree pool objects {0}", total, cntData, cntEmpty, packed, pool.CountFree()));
	}

	private TextCommandResult getHereChunkInfo(TextCommandCallingArgs args)
	{
		int chunkX = (int)args.Caller.Pos.X / 32;
		int chunkY = (int)args.Caller.Pos.Y / 32;
		int chunkZ = (int)args.Caller.Pos.Z / 32;
		long index3d = server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ);
		long index2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		ConnectedClient client = server.GetClientByUID(args.Caller.Player.PlayerUID);
		bool loaded = server.WorldMap.GetServerChunk(index3d) != null;
		bool didRequest = server.ChunkColumnRequested.ContainsKey(index2d);
		bool didSend = client.DidSendChunk(index3d);
		bool inRequestQueue = server.requestedChunkColumns.Contains(index2d);
		IServerChunk chunk = server.WorldMap.GetChunk(chunkX, chunkY, chunkZ) as IServerChunk;
		return TextCommandResult.Success(string.Format("Loaded: {0}, DidRequest: {1}, DidSend: {2}, InRequestQueue: {3}, your current chunk sent radius: {4}{5}, Player placed blocks: {6}, Player removed blocks: {7}", loaded, didRequest, didSend, inRequestQueue, client.CurrentChunkSentRadius, loaded ? (", " + string.Format("Gameversioncreated: {0} , WorldGenVersion: {1}", chunk.GameVersionCreated ?? "1.10 or earlier", ((ServerMapChunk)chunk.MapChunk).WorldGenVersion)) : "", chunk.BlocksPlaced, chunk.BlocksRemoved));
	}

	private TextCommandResult resendChunk(TextCommandCallingArgs args)
	{
		Vec3d obj = args[0] as Vec3d;
		int chunkX = (int)obj.X / 32;
		int chunkY = (int)obj.Y / 32;
		int chunkZ = (int)obj.Z / 32;
		server.BroadcastChunk(chunkX, chunkY, chunkZ, onlyIfInRange: false);
		return TextCommandResult.Success("Ok, chunk now resent");
	}

	private TextCommandResult relightChunk(TextCommandCallingArgs args)
	{
		Vec3d obj = args[0] as Vec3d;
		int chunksize = 32;
		int chunkX = (int)obj.X / chunksize;
		int chunkY = (int)obj.Y / chunksize;
		int chunkZ = (int)obj.Z / chunksize;
		BlockPos minPos = new BlockPos(chunkX * chunksize, chunkY * chunksize, chunkZ * chunksize);
		BlockPos maxPos = new BlockPos((chunkX + 1) * chunksize - 1, (chunkY + 1) * chunksize - 1, (chunkZ + 1) * chunksize - 1);
		server.api.WorldManager.FullRelight(minPos, maxPos);
		return TextCommandResult.Success("Ok, chunk now relit");
	}

	private TextCommandResult exportTickHandlers(TextCommandCallingArgs args)
	{
		string type = (string)args[0];
		server.EventManager.defragLists();
		switch (type)
		{
		case "gtblock":
			dumpList(server.EventManager.GameTickListenersBlock);
			break;
		case "gtentity":
			dumpList(server.EventManager.GameTickListenersEntity);
			break;
		case "dcblock":
			dumpList(server.EventManager.DelayedCallbacksBlock);
			break;
		case "sdcblock":
			dumpList(server.EventManager.SingleDelayedCallbacksBlock.Values);
			break;
		case "dcentity":
			dumpList(server.EventManager.DelayedCallbacksEntity);
			break;
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult countTickHandlers(TextCommandCallingArgs args)
	{
		server.EventManager.defragLists();
		return TextCommandResult.Success(Lang.Get("GameTickListenersBlock={0}, GameTickListenersEntity={1}, DelayedCallbacksBlock={2}, DelayedCallbacksEntity={3}, SingleDelayedCallbacksBlock={4}", server.EventManager.GameTickListenersBlock.Count, server.EventManager.GameTickListenersEntity.Count, server.EventManager.DelayedCallbacksBlock.Count, server.EventManager.DelayedCallbacksEntity.Count, server.EventManager.SingleDelayedCallbacksBlock.Count));
	}

	private void PrintOctagonPoints(int viewDistance)
	{
		int desiredRadius = (int)Math.Ceiling((float)viewDistance / (float)MagicNum.ServerChunkSize);
		for (int r = 1; r < desiredRadius; r++)
		{
			Vec2i[] octagonPoints = ShapeUtil.GetOctagonPoints(desiredRadius / 2 + 25, desiredRadius / 2 + 25, r);
			SKBitmap bmp = new SKBitmap(desiredRadius + 50, desiredRadius + 50);
			Vec2i[] array = octagonPoints;
			foreach (Vec2i point in array)
			{
				bmp.SetPixel(point.X, point.Y, new SKColor(byte.MaxValue, 0, 0, byte.MaxValue));
			}
			bmp.Save("octapoints" + r + ".png");
		}
	}

	private void dumpList<T>(ICollection<T> list)
	{
		StringBuilder sb = new StringBuilder();
		foreach (T item in list)
		{
			if (item is GameTickListener gtl)
			{
				sb.AppendLine(gtl.Origin().ToString() + ":" + gtl.Handler.Method.ToString());
			}
			if (item is DelayedCallback dc)
			{
				sb.AppendLine(dc.Handler.Target.ToString() + ":" + dc.Handler.Method.ToString());
			}
			if (item is GameTickListenerBlock gtblock)
			{
				sb.AppendLine(gtblock.Handler.Target.ToString() + ":" + gtblock.Handler.Method.ToString());
			}
			if (item is DelayedCallbackBlock dcblock)
			{
				sb.AppendLine(dcblock.Handler.Target.ToString() + ":" + dcblock.Handler.Method.ToString());
			}
		}
		ServerMain.Logger.VerboseDebug(sb.ToString());
	}

	private StackTrace GetStackTrace(Thread targetThread)
	{
		StackTrace stackTrace = null;
		ManualResetEventSlim ready = new ManualResetEventSlim();
		new Thread((ThreadStart)delegate
		{
			ready.Set();
			Thread.Sleep(200);
			try
			{
				targetThread.Resume();
			}
			catch
			{
			}
		}).Start();
		ready.Wait();
		targetThread.Suspend();
		try
		{
		}
		finally
		{
			try
			{
				targetThread.Resume();
			}
			catch
			{
				stackTrace = null;
			}
		}
		return stackTrace;
	}
}
