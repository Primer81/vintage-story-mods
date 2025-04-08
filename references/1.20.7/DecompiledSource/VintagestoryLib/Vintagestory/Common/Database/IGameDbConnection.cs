using System;
using System.Collections.Generic;

namespace Vintagestory.Common.Database;

public interface IGameDbConnection : IDisposable
{
	bool IsReadOnly { get; }

	bool OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck);

	void CreateBackup(string backupFilename);

	void Vacuum();

	IEnumerable<DbChunk> GetAllChunks();

	IEnumerable<DbChunk> GetAllMapChunks();

	IEnumerable<DbChunk> GetAllMapRegions();

	byte[] GetPlayerData(string playeruid);

	void SetPlayerData(string playeruid, byte[] data);

	bool ChunkExists(ulong position);

	bool MapChunkExists(ulong position);

	bool MapRegionExists(ulong position);

	byte[] GetChunk(ulong coord);

	byte[] GetMapChunk(ulong coord);

	byte[] GetMapRegion(ulong coord);

	void SetChunks(IEnumerable<DbChunk> chunks);

	void SetMapChunks(IEnumerable<DbChunk> mapchunks);

	void SetMapRegions(IEnumerable<DbChunk> mapregions);

	void DeleteChunks(IEnumerable<ChunkPos> chunkpositions);

	void DeleteMapChunks(IEnumerable<ChunkPos> chunkpositions);

	void DeleteMapRegions(IEnumerable<ChunkPos> chunkpositions);

	byte[] GetGameData();

	void StoreGameData(byte[] data);

	bool QuickCorrectSaveGameVersionTest();

	void UpgradeToWriteAccess();

	bool IntegrityCheck();
}
