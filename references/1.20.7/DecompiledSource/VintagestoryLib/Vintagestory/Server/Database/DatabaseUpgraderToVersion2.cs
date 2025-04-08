using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Config;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server.Database;

public class DatabaseUpgraderToVersion2 : IDatabaseUpgrader
{
	public bool Upgrade(ServerMain server, string worldFilename)
	{
		GameDatabase dbOld = new GameDatabase(ServerMain.Logger);
		dbOld.OpenConnection(worldFilename, 1, corruptionProtection: true, doIntegrityCheck: false);
		ServerMain.Logger.Event("Old world file opened");
		GameDatabase dbNew = new GameDatabase(ServerMain.Logger);
		dbNew.OpenConnection(worldFilename + "v2", 2, corruptionProtection: true, doIntegrityCheck: false);
		ServerMain.Logger.Event("New world file created");
		ServerMain.Logger.Event("Migrating savegame");
		dbNew.StoreSaveGame(dbOld.GetSaveGame());
		ServerMain.Logger.Event("Migrating map regions");
		dbNew.SetMapRegions(dbOld.GetAllMapRegions());
		ServerMain.Logger.Event("Migrating map chunks");
		dbNew.SetMapChunks(dbOld.GetAllMapChunks());
		ServerMain.Logger.Event("Migrating chunks...");
		IEnumerable<DbChunk> allChunks = dbOld.GetAllChunks();
		List<DbChunk> newChunks = new List<DbChunk>();
		ICompression oldCompressor = new CompressionGzip();
		ICompression newCompressor = new CompressionDeflate();
		int i = 0;
		foreach (DbChunk dbchunk in allChunks)
		{
			Compression.compressor = oldCompressor;
			ServerChunk chunk = ServerChunk.FromBytes(dbchunk.Data, server.serverChunkDataPool, server);
			chunk.Unpack();
			Compression.compressor = newCompressor;
			chunk.TryPackAndCommit();
			dbchunk.Data = chunk.ToBytes();
			i++;
			newChunks.Add(dbchunk);
			if (newChunks.Count >= 100)
			{
				ServerMain.Logger.Event(i + " chunks migrated");
				dbNew.SetChunks(newChunks);
				newChunks.Clear();
			}
		}
		dbNew.SetChunks(newChunks);
		ServerMain.Logger.Event(i + " chunks migrated. Done!");
		dbOld.CloseConnection();
		dbNew.CloseConnection();
		ServerMain.Logger.Event("Moving away old world file");
		GamePaths.EnsurePathExists(GamePaths.OldSaves);
		FileInfo file = new FileInfo(worldFilename);
		string bkpWorldFilename = Path.Combine(GamePaths.OldSaves, file.Name);
		if (File.Exists(bkpWorldFilename))
		{
			File.Delete(bkpWorldFilename);
		}
		file.MoveTo(bkpWorldFilename);
		ServerMain.Logger.Event("Renaming new world file");
		file = new FileInfo(worldFilename + "v2");
		file.MoveTo(worldFilename);
		return true;
	}
}
