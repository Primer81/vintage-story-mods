using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;
using Vintagestory.Server.Database;

namespace Vintagestory.Server;

internal class ServerSystemLoadAndSaveGame : ServerSystem, IChunkProviderThread
{
	private ChunkServerThread chunkthread;

	private object savingLock = new object();

	private BlockAccessorWorldGen blockAccessorWG;

	private BlockAccessorWorldGenUpdateHeightmap blockAccessorWGUpdateHeightMap;

	private Dictionary<long, ServerChunk> chunksCopy = new Dictionary<long, ServerChunk>();

	private bool ignoreSave;

	internal static PlayerSpawnPos SetDefaultSpawnOnce;

	public ServerSystemLoadAndSaveGame(ServerMain server, ChunkServerThread chunkthread)
		: base(server)
	{
		this.chunkthread = chunkthread;
		chunkthread.loadsavegame = this;
	}

	public override void OnSeparateThreadTick()
	{
		if (!chunkthread.runOffThreadSaveNow)
		{
			return;
		}
		lock (savingLock)
		{
			if (chunkthread.runOffThreadSaveNow)
			{
				int saved = SaveAllDirtyLoadedChunks(isSaveLater: true);
				ServerMain.Logger.Event("Offthread save of {0} chunks done.", saved);
				saved = SaveAllDirtyGeneratingChunks();
				ServerMain.Logger.Notification("Offthread save of {0} generating chunks done.", saved);
				int dirtyMapChunks = SaveAllDirtyMapChunks();
				ServerMain.Logger.Event("Offthread save of {0} map chunks done.", dirtyMapChunks);
				chunkthread.gameDatabase.StoreSaveGame(SerializerUtil.Serialize(server.SaveGameData));
				ServerMain.Logger.Event("Offthread save of savegame done.");
				chunkthread.runOffThreadSaveNow = false;
			}
		}
	}

	public override void OnFinalizeAssets()
	{
		server.SaveGameData.WillSave();
		chunkthread.gameDatabase.StoreSaveGame(SerializerUtil.Serialize(server.SaveGameData));
	}

	public override void OnBeginConfiguration()
	{
		chunkthread.gameDatabase = new GameDatabase(ServerMain.Logger);
		string errorMessage = null;
		bool existed = File.Exists(server.GetSaveFilename());
		bool isreadonly = false;
		int version;
		try
		{
			server.SaveGameData = chunkthread.gameDatabase.ProbeOpenConnection(server.GetSaveFilename(), corruptionProtection: true, out version, out errorMessage, out isreadonly);
		}
		catch (Exception e2)
		{
			ServerMain.Logger.Fatal("Unable to open or create savegame.");
			ServerMain.Logger.Fatal(e2);
			server.Stop("Failed opening savegame");
			return;
		}
		if (server.SaveGameData == null && existed && server.Config.RepairMode)
		{
			chunkthread.gameDatabase.CloseConnection();
			chunkthread.gameDatabase.OpenConnection(server.GetSaveFilename(), GameVersion.DatabaseVersion, out errorMessage, requireWriteAccess: true, server.Config.CorruptionProtection, server.Config.RepairMode);
			ServerMain.Logger.Fatal("Failed opening savegame data, possibly corrupted. We are in repair mode, so initializing new savegame data structure.", errorMessage);
			server.SaveGameData = SaveGame.CreateNew(server.Config);
			server.SaveGameData.WorldType = "standard";
			server.SaveGameData.PlayStyle = "surviveandbuild";
			server.SaveGameData.PlayStyleLangCode = "preset-surviveandbuild";
			version = GameVersion.DatabaseVersion;
		}
		else if (server.Config.RepairMode)
		{
			chunkthread.gameDatabase.IntegrityCheck();
		}
		if (server.SaveGameData == null && existed)
		{
			server.SaveGameData = null;
			ServerMain.Logger.Fatal("Failed opening savegame, possibly corrupted. Error Message: {0}. Will exit server now.", errorMessage);
			server.Stop("Failed opening savegame");
			return;
		}
		if (isreadonly)
		{
			server.SaveGameData = null;
			chunkthread.gameDatabase.CloseConnection();
			ServerMain.Logger.Fatal("Failed opening savegame, have no write access to it. Make sure no other server is accessing it. Will exit server now.");
			server.Stop("Failed opening savegame, it is readonly");
			return;
		}
		try
		{
			FileInfo f = new FileInfo(chunkthread.gameDatabase.DatabaseFilename);
			long freeSpaceBytes = ServerMain.xPlatInterface.GetFreeDiskSpace(f.DirectoryName);
			if (freeSpaceBytes >= 0 && freeSpaceBytes < 1048576 * server.Config.DieBelowDiskSpaceMb)
			{
				string messsage = $"Disk space is below {server.Config.DieBelowDiskSpaceMb} megabytes ({freeSpaceBytes / 1024 / 1024} mb left). A full harddisk can heavily corrupt a savegame. Please free up more disk space or adjust the threshold in the serverconfig.json (or set to -1 to disable this check). Will kill server now...";
				ServerMain.Logger.Fatal(messsage);
				throw new Exception(messsage);
			}
		}
		catch (ArgumentException)
		{
			ServerMain.Logger.Warning("Exception thrown when trying to check for available disk space. Please manually verify that your hard disk won't run full to avoid savegame corruption");
		}
		if (version != GameVersion.DatabaseVersion)
		{
			chunkthread.gameDatabase.CloseConnection();
			ServerMain.Logger.Event("Old savegame database version detected, will upgrade now...");
			DatabaseUpgrader upgrader = new DatabaseUpgrader(server, server.GetSaveFilename(), version, GameVersion.DatabaseVersion);
			try
			{
				upgrader.PerformUpgrade();
				chunkthread.gameDatabase.OpenConnection(server.GetSaveFilename(), corruptionProtection: true, doIntegrityCheck: true);
				server.SaveGameData = null;
			}
			catch (Exception e)
			{
				ServerMain.Logger.Event("Failed upgrading old savegame, giving up, sorry.");
				throw new InvalidDataException("Failed upgrading savegame {0}", e);
			}
		}
		chunkthread.gameDatabase.UpgradeToWriteAccess();
		server.ModEventManager.OnWorldgenStartup += OnWorldgenStartup;
		LoadSaveGame();
	}

	public override void OnBeginModsAndConfigReady()
	{
		if (server.SaveGameData.IsNewWorld)
		{
			server.ModEventManager.TriggerSaveGameCreated();
		}
		server.EventManager.TriggerSaveGameLoaded();
		server.WorldMap.chunkIlluminatorWorldGen.chunkProvider = chunkthread;
		chunkthread.worldgenBlockAccessor = GetBlockAccessor(updateHeightmap: false);
		foreach (WorldGenThreadDelegate item in server.ModEventManager.WorldgenBlockAccessor)
		{
			item(this);
		}
	}

	public void OnWorldgenStartup()
	{
		chunkthread.loadsavechunks.InitWorldgenAndSpawnChunks();
	}

	public override void OnBeginRunGame()
	{
		server.EventManager.OnGameWorldBeingSaved += OnWorldBeingSaved;
	}

	public override void OnBeginShutdown()
	{
		if (server.Saving)
		{
			ServerMain.Logger.Error("Server was saving and a shutdown has begun? Waiting 10 secs before doing save-on-shutdown");
			Thread.Sleep(10000);
		}
		server.Saving = true;
		if (server.SaveGameData != null)
		{
			server.SaveGameData.TotalSecondsPlayed += (int)(server.ElapsedMilliseconds / 1000);
			server.EventManager.TriggerGameWorldBeingSaved();
		}
		server.Saving = false;
	}

	public override void OnSeperateThreadShutDown()
	{
		chunkthread.gameDatabase.Dispose();
	}

	public void OnWorldBeingSaved()
	{
		bool saveLater = server.RunPhase != EnumServerRunPhase.Shutdown;
		if (ignoreSave)
		{
			return;
		}
		try
		{
			FileInfo file = new FileInfo(chunkthread.gameDatabase.DatabaseFilename);
			long freeSpaceBytes = ServerMain.xPlatInterface.GetFreeDiskSpace(file.DirectoryName);
			long maxSpaceInBytes = 1048576 * server.Config.DieBelowDiskSpaceMb;
			if (freeSpaceBytes >= 0)
			{
				if (freeSpaceBytes >= maxSpaceInBytes && freeSpaceBytes < maxSpaceInBytes * 2)
				{
					ServerMain.Logger.Warning("Disk space is getting close to configured server shutdown level. Please free up more disk space or adjust the threshold in the serverconfig.json.");
				}
				else if (freeSpaceBytes < maxSpaceInBytes)
				{
					string messsage = $"Disk space is below {server.Config.DieBelowDiskSpaceMb} megabytes ({freeSpaceBytes / 1024 / 1024} mb left). A full harddisk can heavily corrupt a savegame. Please free up more disk space or adjust the threshold in the serverconfig.json (or set to -1 to disable this check). Will kill server now...";
					ServerMain.Logger.Fatal(messsage);
					ignoreSave = true;
					server.Stop("Out of disk space");
					return;
				}
			}
		}
		catch (ArgumentException)
		{
			ServerMain.Logger.Warning("Exception thrown when trying to check for available disk space. Please manually verify that your hard disk won't run full to avoid savegame corruption");
		}
		if (saveLater && chunkthread.runOffThreadSaveNow)
		{
			ServerMain.Logger.Fatal("Already saving, will ignore save this time");
			return;
		}
		lock (savingLock)
		{
			SaveGameWorld(saveLater);
		}
	}

	private void LoadSaveGame()
	{
		string saveFileName = server.GetSaveFilename();
		ServerMain.Logger.Notification("Loading savegame");
		if (!File.Exists(saveFileName))
		{
			ServerMain.Logger.Notification("No savegame file found, creating new one");
		}
		if (server.SaveGameData == null)
		{
			server.SaveGameData = chunkthread.gameDatabase.GetSaveGame();
		}
		if (server.SaveGameData == null)
		{
			server.SaveGameData = SaveGame.CreateNew(server.Config);
			server.SaveGameData.WillSave();
			chunkthread.gameDatabase.StoreSaveGame(server.SaveGameData);
			server.EventManager.TriggerSaveGameCreated();
			ServerMain.Logger.Notification("Create new save game data. Playstyle: {0}", server.SaveGameData.PlayStyle);
			if (!server.Standalone)
			{
				ServerMain.Logger.Notification("Default spawn was set in serverconfig, resetting for safety.");
				server.Config.DefaultSpawn = null;
				server.ConfigNeedsSaving = true;
			}
		}
		else
		{
			if (server.PlayerDataManager.WorldDataByUID == null)
			{
				server.PlayerDataManager.WorldDataByUID = new Dictionary<string, ServerWorldPlayerData>();
			}
			server.SaveGameData.Init(server);
			if (server.SaveGameData.PlayerDataByUID != null)
			{
				ServerMain.Logger.Notification("Transferring player data to new db table...");
				foreach (KeyValuePair<string, ServerWorldPlayerData> val in server.SaveGameData.PlayerDataByUID)
				{
					server.PlayerDataManager.WorldDataByUID[val.Key] = val.Value;
					val.Value.Init(server);
				}
				server.SaveGameData.PlayerDataByUID = null;
			}
			if (SetDefaultSpawnOnce != null)
			{
				server.SaveGameData.DefaultSpawn = SetDefaultSpawnOnce;
				SetDefaultSpawnOnce = null;
			}
			server.SaveGameData.IsNewWorld = false;
			ServerMain.Logger.Notification("Loaded existing save game data. Playstyle: {0}, Playstyle Lang code: {1}, WorldType: {1}", server.SaveGameData.PlayStyle, server.SaveGameData.PlayStyleLangCode, server.SaveGameData.WorldType);
		}
		server.WorldMap.Init(server.SaveGameData.MapSizeX, server.SaveGameData.MapSizeY, server.SaveGameData.MapSizeZ);
		chunkthread.requestedChunkColumns = new ConcurrentIndexedFifoQueue<ChunkColumnLoadRequest>(MagicNum.RequestChunkColumnsQueueSize, Math.Max(1, Math.Min(6, MagicNum.MaxWorldgenThreads)) + 1);
		chunkthread.peekingChunkColumns = new IndexedFifoQueue<ChunkColumnLoadRequest>(MagicNum.RequestChunkColumnsQueueSize / 5);
		ServerMain.Logger.Notification("Savegame {0} loaded", saveFileName);
		ServerMain.Logger.Notification("World size = {0} {1} {2}", server.SaveGameData.MapSizeX, server.SaveGameData.MapSizeY, server.SaveGameData.MapSizeZ);
	}

	private void SaveGameWorld(bool saveLater = false)
	{
		if (!saveLater)
		{
			chunkthread.runOffThreadSaveNow = false;
		}
		if (ServerMain.FrameProfiler == null)
		{
			ServerMain.FrameProfiler = new FrameProfilerUtil(delegate(string text)
			{
				ServerMain.Logger.Notification(text);
			});
			ServerMain.FrameProfiler.Begin(null);
		}
		ServerMain.FrameProfiler.Mark("savegameworld-begin");
		ServerMain.Logger.Event("Mods and systems notified, now saving everything...");
		ServerMain.Logger.StoryEvent(Lang.Get("It pauses."));
		server.SaveGameData.WillSave();
		if (saveLater)
		{
			ServerMain.Logger.Event("Will do offthread savegamedata saving...");
		}
		ServerMain.FrameProfiler.Mark("savegameworld-mid-1");
		ServerMain.Logger.StoryEvent(Lang.Get("One last gaze..."));
		foreach (ServerWorldPlayerData plrdata in server.PlayerDataManager.WorldDataByUID.Values)
		{
			plrdata.BeforeSerialization();
			chunkthread.gameDatabase.SetPlayerData(plrdata.PlayerUID, SerializerUtil.Serialize(plrdata));
		}
		ServerMain.FrameProfiler.Mark("savegameworld-mid-2");
		ServerMain.Logger.Event("Saved player world data...");
		int dirtyMapRegions = SaveAllDirtyMapRegions();
		ServerMain.FrameProfiler.Mark("savegameworld-mid-3");
		ServerMain.Logger.Event("Saved map regions...");
		ServerMain.Logger.StoryEvent(Lang.Get("...then all goes quiet"));
		int dirtyMapChunks = 0;
		if (!saveLater)
		{
			dirtyMapChunks = SaveAllDirtyMapChunks();
			ServerMain.FrameProfiler.Mark("savegameworld-mid-4");
		}
		ServerMain.Logger.Event("Saved map chunks...");
		ServerMain.Logger.StoryEvent(Lang.Get("The waters recede..."));
		ServerMain.FrameProfiler.Mark("savegameworld-mid-5");
		int dirtyChunks = 0;
		if (saveLater)
		{
			PopulateChunksCopy();
			chunkthread.runOffThreadSaveNow = true;
		}
		else
		{
			dirtyChunks = SaveAllDirtyLoadedChunks(isSaveLater: false);
			ServerMain.Logger.Event("Saved loaded chunks...");
			ServerMain.Logger.StoryEvent(Lang.Get("The mountains fade..."));
			ServerMain.Logger.StoryEvent(Lang.Get("The dark settles in."));
			dirtyChunks += SaveAllDirtyGeneratingChunks();
			ServerMain.Logger.Event("Saved generating chunks...");
			chunkthread.gameDatabase.StoreSaveGame(server.SaveGameData);
			ServerMain.Logger.Event("Saved savegamedata..." + server.SaveGameData.HighestChunkdataVersion);
		}
		ServerMain.Logger.Event("World saved! Saved {0} chunks, {1} mapchunks, {2} mapregions.", dirtyChunks, dirtyMapChunks, dirtyMapRegions);
		ServerMain.Logger.StoryEvent(Lang.Get("It sighs..."));
		ServerMain.FrameProfiler.Mark("savegameworld-end");
	}

	private int SaveAllDirtyMapRegions()
	{
		int dirty = 0;
		List<DbChunk> dirtyMapRegions = new List<DbChunk>();
		foreach (KeyValuePair<long, ServerMapRegion> val in server.loadedMapRegions)
		{
			if (val.Value.DirtyForSaving)
			{
				val.Value.DirtyForSaving = false;
				dirty++;
				dirtyMapRegions.Add(new DbChunk
				{
					Position = server.WorldMap.MapRegionPosFromIndex2D(val.Key),
					Data = val.Value.ToBytes()
				});
			}
		}
		chunkthread.gameDatabase.SetMapRegions(dirtyMapRegions);
		return dirty;
	}

	private int SaveAllDirtyMapChunks()
	{
		int dirty = 0;
		List<DbChunk> dirtyMapChunks = new List<DbChunk>();
		using FastMemoryStream reusableStream = new FastMemoryStream();
		foreach (KeyValuePair<long, ServerMapChunk> val in server.loadedMapChunks)
		{
			if (val.Value.DirtyForSaving)
			{
				val.Value.DirtyForSaving = false;
				ChunkPos pos = server.WorldMap.ChunkPosFromChunkIndex2D(val.Key);
				dirty++;
				dirtyMapChunks.Add(new DbChunk
				{
					Position = pos,
					Data = val.Value.ToBytes(reusableStream)
				});
				if (dirtyMapChunks.Count > 200)
				{
					chunkthread.gameDatabase.SetMapChunks(dirtyMapChunks);
					dirtyMapChunks.Clear();
				}
			}
		}
		chunkthread.gameDatabase.SetMapChunks(dirtyMapChunks);
		return dirty;
	}

	internal int SaveAllDirtyLoadedChunks(bool isSaveLater)
	{
		int dirty = 0;
		List<DbChunk> dirtyChunks = new List<DbChunk>();
		if (!isSaveLater)
		{
			PopulateChunksCopy();
		}
		using FastMemoryStream reusableStream = new FastMemoryStream();
		foreach (KeyValuePair<long, ServerChunk> val in chunksCopy)
		{
			if (val.Value.DirtyForSaving)
			{
				val.Value.DirtyForSaving = false;
				ChunkPos vec = server.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
				dirtyChunks.Add(new DbChunk
				{
					Position = vec,
					Data = val.Value.ToBytes(reusableStream)
				});
				dirty++;
				if (dirtyChunks.Count > 300)
				{
					chunkthread.gameDatabase.SetChunks(dirtyChunks);
					dirtyChunks.Clear();
				}
				if (dirty > 0 && dirty % 300 == 0)
				{
					ServerMain.Logger.Event("Saved {0} chunks...", dirty);
				}
			}
		}
		chunkthread.gameDatabase.SetChunks(dirtyChunks);
		if (dirty > 0)
		{
			server.SaveGameData.UpdateChunkdataVersion();
		}
		return dirty;
	}

	private void PopulateChunksCopy()
	{
		chunksCopy.Clear();
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (KeyValuePair<long, ServerChunk> val in server.loadedChunks)
			{
				chunksCopy[val.Key] = val.Value;
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
	}

	internal int SaveAllDirtyGeneratingChunks()
	{
		int dirty = 0;
		List<DbChunk> dirtyChunks = new List<DbChunk>();
		using FastMemoryStream reusableStream = new FastMemoryStream();
		if (chunkthread.requestedChunkColumns.Count > 0)
		{
			foreach (ChunkColumnLoadRequest request in chunkthread.requestedChunkColumns.Snapshot())
			{
				if (request.Chunks == null || request.Disposed || request.CurrentIncompletePass <= EnumWorldGenPass.Terrain)
				{
					continue;
				}
				request.generatingLock.AcquireReadLock();
				try
				{
					for (int y = 0; y < request.Chunks.Length; y++)
					{
						if (request.Chunks[y].DirtyForSaving)
						{
							request.Chunks[y].DirtyForSaving = false;
							dirtyChunks.Add(new DbChunk
							{
								Position = new ChunkPos(request.chunkX, y, request.chunkZ, 0),
								Data = request.Chunks[y].ToBytes(reusableStream)
							});
							dirty++;
							if (dirty > 0 && dirty % 300 == 0)
							{
								ServerMain.Logger.Event("Saved {0} generating chunks...", dirty);
								ServerMain.Logger.StoryEvent("...");
							}
						}
					}
				}
				finally
				{
					request.generatingLock.ReleaseReadLock();
				}
				if (dirtyChunks.Count > 300)
				{
					chunkthread.gameDatabase.SetChunks(dirtyChunks);
					dirtyChunks.Clear();
				}
			}
			chunkthread.gameDatabase.SetChunks(dirtyChunks);
		}
		if (dirty > 0)
		{
			server.SaveGameData.UpdateChunkdataVersion();
		}
		return dirty;
	}

	public IWorldGenBlockAccessor GetBlockAccessor(bool updateHeightmap)
	{
		if (updateHeightmap)
		{
			if (blockAccessorWGUpdateHeightMap == null)
			{
				blockAccessorWGUpdateHeightMap = new BlockAccessorWorldGenUpdateHeightmap(server, chunkthread);
			}
			return blockAccessorWGUpdateHeightMap;
		}
		if (blockAccessorWG == null)
		{
			blockAccessorWG = new BlockAccessorWorldGen(server, chunkthread);
		}
		return blockAccessorWG;
	}
}
