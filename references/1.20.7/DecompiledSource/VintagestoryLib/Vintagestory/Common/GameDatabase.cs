using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common.Database;

namespace Vintagestory.Common;

public class GameDatabase : IDisposable
{
	private IGameDbConnection conn;

	private ILogger logger;

	private string databaseFilename;

	public string DatabaseFilename => databaseFilename;

	public GameDatabase(ILogger logger)
	{
		this.logger = logger;
	}

	public bool OpenConnection(string databaseFilename, int databaseVersion, bool corruptionProtection, bool doIntegrityCheck)
	{
		string errorMessage;
		return OpenConnection(databaseFilename, databaseVersion, out errorMessage, requireWriteAccess: true, corruptionProtection, doIntegrityCheck);
	}

	public bool OpenConnection(string databaseFilename, out string errorMessage, bool corruptionProtection, bool doIntegrityCheck)
	{
		return OpenConnection(databaseFilename, GameVersion.DatabaseVersion, out errorMessage, requireWriteAccess: true, corruptionProtection, doIntegrityCheck);
	}

	public bool OpenConnection(string databaseFilename, bool corruptionProtection, bool doIntegrityCheck)
	{
		string errorMessage;
		return OpenConnection(databaseFilename, GameVersion.DatabaseVersion, out errorMessage, requireWriteAccess: true, corruptionProtection, doIntegrityCheck);
	}

	public bool OpenConnection(string databaseFilename, int databaseVersion, out string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
	{
		this.databaseFilename = databaseFilename;
		errorMessage = null;
		if (conn != null)
		{
			conn.Dispose();
		}
		switch (databaseVersion)
		{
		case 1:
			conn = new SQLiteDbConnectionv1(logger);
			break;
		case 2:
			conn = new SQLiteDbConnectionv2(logger);
			break;
		}
		return conn.OpenOrCreate(databaseFilename, ref errorMessage, requireWriteAccess, corruptionProtection, doIntegrityCheck);
	}

	public void UpgradeToWriteAccess()
	{
		conn.UpgradeToWriteAccess();
	}

	public bool IntegrityCheck()
	{
		return conn.IntegrityCheck();
	}

	public SaveGame ProbeOpenConnection(string databaseFilename, bool corruptionProtection, out int foundVersion, out bool isReadonly, bool requireWrite = true)
	{
		string errorMessage;
		return ProbeOpenConnection(databaseFilename, corruptionProtection, out foundVersion, out errorMessage, out isReadonly, requireWrite);
	}

	public SaveGame ProbeOpenConnection(string databaseFilename, bool corruptionProtection, out int foundVersion, out string errorMessage, out bool isReadonly, bool requireWrite = true)
	{
		int version = GameVersion.DatabaseVersion;
		errorMessage = null;
		if (!File.Exists(databaseFilename))
		{
			OpenConnection(databaseFilename, version, out errorMessage, requireWrite, corruptionProtection, doIntegrityCheck: false);
			isReadonly = conn.IsReadOnly;
			foundVersion = version;
			return null;
		}
		foundVersion = 0;
		while (version > 0)
		{
			foundVersion = version;
			if (!OpenConnection(databaseFilename, version, out errorMessage, requireWrite, corruptionProtection, doIntegrityCheck: false))
			{
				isReadonly = conn.IsReadOnly;
				return null;
			}
			if (!conn.QuickCorrectSaveGameVersionTest())
			{
				version--;
				continue;
			}
			SaveGame savegame = GetSaveGame();
			if (savegame != null)
			{
				isReadonly = conn.IsReadOnly;
				return savegame;
			}
			version--;
		}
		isReadonly = false;
		return null;
	}

	public IEnumerable<DbChunk> GetAllChunks()
	{
		return conn.GetAllChunks();
	}

	public IEnumerable<DbChunk> GetAllMapChunks()
	{
		return conn.GetAllMapChunks();
	}

	public IEnumerable<DbChunk> GetAllMapRegions()
	{
		return conn.GetAllMapRegions();
	}

	public void Vacuum()
	{
		conn.Vacuum();
	}

	public bool ChunkExists(int x, int y, int z)
	{
		return conn.ChunkExists(ChunkPos.ToChunkIndex(x, y, z, 0));
	}

	public bool MapChunkExists(int x, int z)
	{
		return conn.MapChunkExists(ChunkPos.ToChunkIndex(x, 0, z));
	}

	public bool MapRegionExists(int x, int z)
	{
		return conn.MapRegionExists(ChunkPos.ToChunkIndex(x, 0, z));
	}

	public byte[] GetChunk(int x, int y, int z)
	{
		return conn.GetChunk(ChunkPos.ToChunkIndex(x, y, z, 0));
	}

	public byte[] GetChunk(int x, int y, int z, int dimension)
	{
		return conn.GetChunk(ChunkPos.ToChunkIndex(x, y, z, dimension));
	}

	public byte[] GetMapChunk(int x, int z)
	{
		return conn.GetMapChunk(ChunkPos.ToChunkIndex(x, 0, z));
	}

	public byte[] GetMapRegion(int x, int z)
	{
		return conn.GetMapRegion(ChunkPos.ToChunkIndex(x, 0, z));
	}

	public void SetChunks(IEnumerable<DbChunk> chunks)
	{
		conn.SetChunks(chunks);
	}

	public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
	{
		conn.SetMapChunks(mapchunks);
	}

	public void SetMapRegions(IEnumerable<DbChunk> mapregions)
	{
		conn.SetMapRegions(mapregions);
	}

	public void DeleteChunks(IEnumerable<ChunkPos> coords)
	{
		conn.DeleteChunks(coords);
	}

	public void DeleteMapChunks(IEnumerable<ChunkPos> coords)
	{
		conn.DeleteMapChunks(coords);
	}

	public void DeleteMapRegions(IEnumerable<ChunkPos> coords)
	{
		conn.DeleteMapRegions(coords);
	}

	public byte[] GetPlayerData(string playeruid)
	{
		return conn.GetPlayerData(playeruid);
	}

	public void SetPlayerData(string playeruid, byte[] data)
	{
		conn.SetPlayerData(playeruid, data);
	}

	public void Dispose()
	{
		if (conn != null)
		{
			conn.Dispose();
		}
		conn = null;
	}

	public void CreateBackup(string backupFilename)
	{
		conn.CreateBackup(backupFilename);
	}

	public SaveGame GetSaveGame()
	{
		byte[] gamedata = conn.GetGameData();
		SaveGame savegame = null;
		if (gamedata != null)
		{
			try
			{
				savegame = Serializer.Deserialize<SaveGame>(new MemoryStream(gamedata));
			}
			catch (Exception e)
			{
				logger.Warning("Exception thrown on GetSaveGame: " + e.Message);
				return null;
			}
		}
		return savegame;
	}

	public void StoreSaveGame(SaveGame savegame)
	{
		using MemoryStream ms = new MemoryStream();
		Serializer.Serialize((Stream)ms, savegame);
		conn.StoreGameData(ms.ToArray());
	}

	public void StoreSaveGame(byte[] data)
	{
		conn.StoreGameData(data);
	}

	internal void CloseConnection()
	{
		Dispose();
	}

	public static bool HaveWriteAccessFolder(string folderPath)
	{
		try
		{
			string path = Path.Combine(folderPath, "temp.txt");
			File.Create(path).Close();
			File.Delete(path);
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
	}

	public static bool HaveWriteAccessFile(FileInfo file)
	{
		FileStream stream = null;
		try
		{
			stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			stream?.Close();
		}
		return true;
	}
}
