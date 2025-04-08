using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class MapDB : SQLiteDBConnection
{
	private SqliteCommand setMapPieceCmd;

	private SqliteCommand getMapPieceCmd;

	public override string DBTypeCode => "worldmap database";

	public MapDB(ILogger logger)
		: base(logger)
	{
	}

	public override void OnOpened()
	{
		base.OnOpened();
		setMapPieceCmd = sqliteConn.CreateCommand();
		setMapPieceCmd.CommandText = "INSERT OR REPLACE INTO mappiece (position, data) VALUES (@pos, @data)";
		setMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
		setMapPieceCmd.Parameters.Add("@data", SqliteType.Blob);
		setMapPieceCmd.Prepare();
		getMapPieceCmd = sqliteConn.CreateCommand();
		getMapPieceCmd.CommandText = "SELECT data FROM mappiece WHERE position=@pos";
		getMapPieceCmd.Parameters.Add("@pos", SqliteType.Integer, 1);
		getMapPieceCmd.Prepare();
	}

	protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
	{
		using (SqliteCommand sqliteCommand = sqliteConn.CreateCommand())
		{
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS mappiece (position integer PRIMARY KEY, data BLOB);";
			sqliteCommand.ExecuteNonQuery();
		}
		using SqliteCommand sqlite_cmd = sqliteConn.CreateCommand();
		sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS blockidmapping (id integer PRIMARY KEY, data BLOB);";
		sqlite_cmd.ExecuteNonQuery();
	}

	public void Purge()
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "delete FROM mappiece";
		cmd.ExecuteNonQuery();
	}

	public MapPieceDB[] GetMapPieces(List<Vec2i> chunkCoords)
	{
		MapPieceDB[] pieces = new MapPieceDB[chunkCoords.Count];
		for (int i = 0; i < chunkCoords.Count; i++)
		{
			getMapPieceCmd.Parameters["@pos"].Value = chunkCoords[i].ToChunkIndex();
			using SqliteDataReader sqlite_datareader = getMapPieceCmd.ExecuteReader();
			while (sqlite_datareader.Read())
			{
				object data = sqlite_datareader["data"];
				if (data == null)
				{
					return null;
				}
				pieces[i] = SerializerUtil.Deserialize<MapPieceDB>(data as byte[]);
			}
		}
		return pieces;
	}

	public MapPieceDB GetMapPiece(Vec2i chunkCoord)
	{
		getMapPieceCmd.Parameters["@pos"].Value = chunkCoord.ToChunkIndex();
		using SqliteDataReader sqlite_datareader = getMapPieceCmd.ExecuteReader();
		if (sqlite_datareader.Read())
		{
			object data = sqlite_datareader["data"];
			if (data == null)
			{
				return null;
			}
			return SerializerUtil.Deserialize<MapPieceDB>(data as byte[]);
		}
		return null;
	}

	public void SetMapPieces(Dictionary<Vec2i, MapPieceDB> pieces)
	{
		using SqliteTransaction transaction = sqliteConn.BeginTransaction();
		setMapPieceCmd.Transaction = transaction;
		foreach (KeyValuePair<Vec2i, MapPieceDB> val in pieces)
		{
			setMapPieceCmd.Parameters["@pos"].Value = val.Key.ToChunkIndex();
			setMapPieceCmd.Parameters["@data"].Value = SerializerUtil.Serialize(val.Value);
			setMapPieceCmd.ExecuteNonQuery();
		}
		transaction.Commit();
	}

	public MapBlockIdMappingDB GetMapBlockIdMappingDB()
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT data FROM blockidmapping WHERE id=1";
		using SqliteDataReader sqlite_datareader = cmd.ExecuteReader();
		if (sqlite_datareader.Read())
		{
			object data = sqlite_datareader["data"];
			return (data == null) ? null : SerializerUtil.Deserialize<MapBlockIdMappingDB>(data as byte[]);
		}
		return null;
	}

	public void SetMapBlockIdMappingDB(MapBlockIdMappingDB mapping)
	{
		using SqliteTransaction transaction = sqliteConn.BeginTransaction();
		using (DbCommand cmd = sqliteConn.CreateCommand())
		{
			cmd.Transaction = transaction;
			byte[] data = SerializerUtil.Serialize(mapping);
			cmd.CommandText = "INSERT OR REPLACE INTO mappiece (position, data) VALUES (@position,@data)";
			cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, 1, cmd));
			cmd.Parameters.Add(CreateParameter("data", DbType.Object, data, cmd));
			cmd.ExecuteNonQuery();
		}
		transaction.Commit();
	}

	public override void Close()
	{
		setMapPieceCmd?.Dispose();
		getMapPieceCmd?.Dispose();
		base.Close();
	}

	public override void Dispose()
	{
		setMapPieceCmd?.Dispose();
		getMapPieceCmd?.Dispose();
		base.Dispose();
	}
}
