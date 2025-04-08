using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemSupplyChunkCommands : ServerSystem
{
	private string backupFileName;

	private ChunkServerThread chunkthread;

	private bool backupInProgress;

	public ServerSystemSupplyChunkCommands(ServerMain server, ChunkServerThread chunkthread)
		: base(server)
	{
		this.chunkthread = chunkthread;
		server.api.ChatCommands.GetOrCreate("chunk").BeginSub("cit").WithDescription("Chunk information from the supply chunks thread")
			.WithArgs(server.api.ChatCommands.Parsers.OptionalWord("perf"))
			.HandleWith(OnChunkInfoCmd)
			.EndSub()
			.BeginSub("printmap")
			.WithDescription("Export a png file of a map of loaded chunks. Marks call location with a yellow pixel")
			.HandleWith(OnChunkMap)
			.EndSub();
	}

	private TextCommandResult OnChunkMap(TextCommandCallingArgs args)
	{
		string filename = PrintServerChunkMap(new Vec2i(args.Caller.Pos.XInt / 32, args.Caller.Pos.ZInt / 32));
		return TextCommandResult.Success("map " + filename + " generated");
	}

	public override void OnBeginModsAndConfigReady()
	{
		base.OnBeginModsAndConfigReady();
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		IChatCommand dbcmd = server.api.ChatCommands.GetOrCreate("db").RequiresPrivilege(Privilege.controlserver).WithDesc("Save-game related commands");
		if (!server.Config.HostedMode)
		{
			dbcmd.BeginSub("backup").WithDesc("Creates a copy of the current save game in the Backups folder").WithArgs(parsers.OptionalWord("filename"))
				.HandleWith(onCmdGenBackup)
				.WithRootAlias("genbackup")
				.EndSub()
				.BeginSub("vacuum")
				.WithDesc("Repack save game to minimize its file size")
				.HandleWith(onCmdVacuum)
				.EndSub();
		}
		else
		{
			dbcmd.WithAdditionalInformation("(/db backup and /db vacuum sub-commands are not available for hosted servers, sorry)");
		}
		dbcmd.BeginSub("prune").WithDesc("Delete all unchanged or hardly changed chunks, with changes below a specified threshold. Chunks with claims can be protected.").WithAdditionalInformation("'Changes' refers to edits by players, counted separately in each 32x32 chunk in the world. The number of edits is the count of blocks of any kind placed or broken by any player in either Survival or Creative modes. Breaking grass or leaves is counted, harvesting berries or collecting sticks is not counted. Only player actions since game version 1.18.0 (April 2023) are counted. Chunks with land claims of any size, even a single block, can be protected using the 'keep' option. The 'keep' option will preserve all trader caravans and the Resonance Archives.\n\nPruned chunks are fully deleted and destroyed and, when next visited, will be regenerated with up-to-date worldgen from the current game version, including new vegetation and ruins. Bodies of water, general terrain shape and climate conditions will be unchanged or almost unchanged. Ore presence in each chunk will be similar as before, may be in slightly different positions.\n\nWithout the 'confirm' arg, does a dry-run only! If mods or worldconfig have changed since the world was first created, or if the map was first created in game version 1.17 or earlier, results of a prune may be unpredictable or chunk borders may become visible, a backup first is advisable. This command is irreversible, use with care!")
			.WithArgs(parsers.Int("threshold"), parsers.WordRange("choice whether to protect (keep) all chunks which have land claims", "keep", "drop"), parsers.OptionalWordRange("confirm flag", "confirm"))
			.HandleWith(onCmdPrune)
			.EndSub()
			.Validate();
	}

	private TextCommandResult onCmdVacuum(TextCommandCallingArgs args)
	{
		IServerPlayer logToPlayer = args.Caller.Player as IServerPlayer;
		processInBackground(chunkthread.gameDatabase.Vacuum, delegate
		{
			notifyIndirect(logToPlayer, Lang.Get("Vacuum complete!"));
		});
		return TextCommandResult.Success(Lang.Get("Vacuum started, this may take some time"));
	}

	private TextCommandResult onCmdPrune(TextCommandCallingArgs args)
	{
		IServerPlayer logToPlayer = args.Caller.Player as IServerPlayer;
		int threshold = (int)args[0];
		bool keepClaims = (string)args[1] == "keep";
		bool dryRun = (string)args[2] != "confirm";
		return prune(logToPlayer, threshold, dryRun, keepClaims);
	}

	private TextCommandResult prune(IServerPlayer logToPlayer, int threshold, bool dryRun, bool keepClaims)
	{
		int qBelowThreshold = 0;
		HashSet<Vec2i> toDelete = new HashSet<Vec2i>();
		HashSet<Vec2i> toKeep = new HashSet<Vec2i>();
		List<LandClaim> claims = server.SaveGameData.LandClaims;
		HorRectanglei rect = new HorRectanglei();
		int chunksize = server.api.worldapi.ChunkSize;
		processInBackground(delegate
		{
			foreach (DbChunk current in chunkthread.gameDatabase.GetAllChunks())
			{
				ServerChunk serverChunk = ServerChunk.FromBytes(current.Data, server.serverChunkDataPool, server);
				if (keepClaims)
				{
					bool flag = false;
					rect.X1 = current.Position.X * chunksize;
					rect.Z1 = current.Position.Z * chunksize;
					rect.X2 = current.Position.X * chunksize + chunksize;
					rect.Z2 = current.Position.Z * chunksize + chunksize;
					foreach (LandClaim current2 in claims)
					{
						if (current2 != null && current2.Intersects2d(rect))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						toKeep.Add(new Vec2i(current.Position.X, current.Position.Z));
						continue;
					}
				}
				int blocksRemoved = serverChunk.BlocksRemoved;
				int blocksPlaced = serverChunk.BlocksPlaced;
				if (blocksRemoved + blocksPlaced > threshold)
				{
					toKeep.Add(new Vec2i(current.Position.X, current.Position.Z));
				}
				else
				{
					qBelowThreshold++;
					toDelete.Add(new Vec2i(current.Position.X, current.Position.Z));
				}
			}
			foreach (Vec2i current3 in toKeep)
			{
				toDelete.Remove(current3);
			}
			server.EnqueueMainThreadTask(delegate
			{
				if (dryRun)
				{
					notifyIndirect(logToPlayer, Lang.Get("Dry run prune complete. With a {0} block edits threshold, {1} chunk columns can be removed, {2} chunk columns would be kept.", threshold, toDelete.Count, toKeep.Count));
				}
				else
				{
					int num = server.api.worldapi.RegionSize / chunksize;
					Cuboidi cuboidi = new Cuboidi();
					Dictionary<long, ServerMapRegion> dictionary = new Dictionary<long, ServerMapRegion>(10);
					Queue<long> queue = new Queue<long>(10);
					foreach (Vec2i current4 in toDelete)
					{
						int num2 = current4.X / num;
						int num3 = current4.Y / num;
						ServerMapRegion value = server.WorldMap.GetMapRegion(num2, num3) as ServerMapRegion;
						long num4 = server.WorldMap.MapRegionIndex2D(num2, num3);
						if (value == null && !dictionary.TryGetValue(num4, out value))
						{
							byte[] mapRegion = chunkthread.gameDatabase.GetMapRegion(num2, num3);
							if (mapRegion != null)
							{
								if (queue.Count >= 9)
								{
									long num5 = queue.Dequeue();
									ServerMapRegion serverMapRegion = dictionary[num5];
									DbChunk item = new DbChunk
									{
										Position = server.WorldMap.MapRegionPosFromIndex2D(num5),
										Data = serverMapRegion.ToBytes()
									};
									chunkthread.gameDatabase.SetMapRegions(new List<DbChunk> { item });
									dictionary.Remove(num5);
								}
								value = (dictionary[num4] = ServerMapRegion.FromBytes(mapRegion));
								queue.Enqueue(num4);
							}
						}
						List<GeneratedStructure> list = new List<GeneratedStructure>();
						cuboidi.X1 = current4.X * chunksize;
						cuboidi.Z1 = current4.Y * chunksize;
						cuboidi.X2 = current4.X * chunksize + chunksize;
						cuboidi.Z2 = current4.Y * chunksize + chunksize;
						if (value?.GeneratedStructures != null)
						{
							foreach (GeneratedStructure current5 in value.GeneratedStructures)
							{
								if (cuboidi.Contains(current5.Location.Start.X, current5.Location.Start.Z))
								{
									list.Add(current5);
								}
							}
							foreach (GeneratedStructure current6 in list)
							{
								value.GeneratedStructures.Remove(current6);
							}
						}
						server.api.WorldManager.DeleteChunkColumn(current4.X, current4.Y);
					}
					chunkthread.gameDatabase.SetMapRegions(dictionary.Select((KeyValuePair<long, ServerMapRegion> r) => new DbChunk
					{
						Position = server.WorldMap.MapRegionPosFromIndex2D(r.Key),
						Data = r.Value.ToBytes()
					}));
					notifyIndirect(logToPlayer, Lang.Get("Prune complete, {1} chunk columns were removed, {2} chunk columns were kept.", threshold, toDelete.Count, toKeep.Count));
				}
			});
		}, null);
		return TextCommandResult.Success(dryRun ? Lang.Get("Dry run prune started, this may take some time.") : Lang.Get("Prune started, this may take some time."));
	}

	private TextCommandResult onCmdGenBackup(TextCommandCallingArgs args)
	{
		if (server.Config.HostedMode)
		{
			return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode"));
		}
		backupFileName = (args.Parsers[0].IsMissing ? null : Path.GetFileName(args[0] as string));
		GenBackup(args.Caller.Player as IServerPlayer);
		return TextCommandResult.Success(Lang.Get("Ok, generating backup, this might take a while"));
	}

	private void GenBackup(IServerPlayer logToPlayer = null)
	{
		if (backupInProgress)
		{
			notifyIndirect(logToPlayer, Lang.Get("Can't run backup. A backup is already in progress"));
			return;
		}
		backupInProgress = true;
		if (backupFileName == null || backupFileName.Length == 0 || backupFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			string filename = Path.GetFileName(server.Config.WorldConfig.SaveFileLocation).Replace(".vcdbs", "");
			if (filename.Length == 0)
			{
				filename = "world";
			}
			backupFileName = filename + "-" + $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}" + ".vcdbs";
		}
		processInBackground(delegate
		{
			chunkthread.gameDatabase.CreateBackup(backupFileName);
		}, delegate
		{
			backupInProgress = false;
			string msg = Lang.Get("Backup complete!");
			notifyIndirect(logToPlayer, msg);
		});
	}

	private void processInBackground(Action backgroundProc, Action onDoneOnMainthread)
	{
		TyronThreadPool.QueueLongDurationTask(delegate
		{
			backgroundProc();
			server.EnqueueMainThreadTask(delegate
			{
				onDoneOnMainthread?.Invoke();
			});
		}, "supplychunkcommand");
	}

	private void notifyIndirect(IServerPlayer logToPlayer, string msg)
	{
		if (logToPlayer != null)
		{
			logToPlayer.SendMessage(server.IsDedicatedServer ? GlobalConstants.ServerInfoChatGroup : GlobalConstants.GeneralChatGroup, msg, EnumChatType.CommandSuccess, "backupdone");
		}
		else
		{
			ServerMain.Logger.Notification(msg);
		}
	}

	private TextCommandResult OnChunkInfoCmd(TextCommandCallingArgs args)
	{
		if ((string)args[0] == "perf")
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < 20; i++)
			{
				sb.AppendLine(ServerSystemSendChunks.performanceTest(server));
			}
			return TextCommandResult.Success(sb.ToString());
		}
		BlockPos asBlockPos = args.Caller.Pos.AsBlockPos;
		int chunkX = asBlockPos.X / 32;
		int chunkY = asBlockPos.Y / 32;
		int chunkZ = asBlockPos.Z / 32;
		long index2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		ServerChunk chunk = chunkthread.GetGeneratingChunk(chunkX, chunkY, chunkZ);
		ChunkColumnLoadRequest chunkReq = chunkthread.requestedChunkColumns.GetByIndex(index2d);
		if (chunkReq != null)
		{
			return TextCommandResult.Success($"Chunk in genQ: {chunk != null}, chunkReq in Q: {chunkReq != null}, currentPass: {chunkReq.CurrentIncompletePass}, untilPass: {chunkReq.GenerateUntilPass}");
		}
		return TextCommandResult.Success($"Chunk in genQ: {chunk != null}, chunkReq in Q: {chunkReq != null}");
	}

	public string PrintServerChunkMap(Vec2i markChunkPos = null)
	{
		ChunkPos minPos = new ChunkPos(int.MaxValue, 0, int.MaxValue, 0);
		ChunkPos maxPos = new ChunkPos(0, 0, 0, 0);
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (long index3d2 in server.loadedChunks.Keys)
			{
				ChunkPos vec2 = server.WorldMap.ChunkPosFromChunkIndex3D(index3d2);
				if (vec2.Dimension <= 0)
				{
					minPos.X = Math.Min(minPos.X, vec2.X);
					minPos.Z = Math.Min(minPos.Z, vec2.Z);
					maxPos.X = Math.Max(maxPos.X, vec2.X);
					maxPos.Z = Math.Max(maxPos.Z, vec2.Z);
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		if (minPos.X == int.MaxValue)
		{
			return "";
		}
		int num = maxPos.X - minPos.X;
		int sizeZ = maxPos.Z - minPos.Z;
		SKBitmap bmp = new SKBitmap(num + 1, sizeZ + 1);
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (long index3d in server.loadedChunks.Keys)
			{
				ChunkPos vec = server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
				if (vec.Dimension <= 0)
				{
					bmp.SetPixel(vec.X - minPos.X, vec.Z - minPos.Z, new SKColor(0, byte.MaxValue, 0, byte.MaxValue));
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		foreach (ChunkColumnLoadRequest req in chunkthread.requestedChunkColumns.Snapshot())
		{
			if (req != null && !req.Disposed)
			{
				if (req.Chunks == null)
				{
					bmp.SetPixel(req.chunkX, req.chunkZ, new SKColor(20, 20, 20, byte.MaxValue));
					continue;
				}
				int currentpass = req.CurrentIncompletePass_AsInt;
				SKColor c = new SKColor((byte)(5 + bmp.GetPixel(req.chunkX, req.chunkZ).Red), (byte)(currentpass * 30), (byte)(currentpass * 30), byte.MaxValue);
				bmp.SetPixel(req.chunkX - minPos.X, req.chunkZ - minPos.Z, c);
			}
		}
		int i = 0;
		while (File.Exists("serverchunks" + i + ".png"))
		{
			i++;
		}
		if (markChunkPos != null)
		{
			bmp.SetPixel(markChunkPos.X - minPos.X, markChunkPos.Y - minPos.Z, new SKColor(byte.MaxValue, 20, byte.MaxValue, byte.MaxValue));
		}
		bmp.Save("serverchunks" + i + ".png");
		return "serverchunks" + i + ".png";
	}
}
