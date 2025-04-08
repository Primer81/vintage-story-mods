using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Common.Database;

public class SQLiteDbConnectionv2 : SQLiteDBConnection, IGameDbConnection, IDisposable
{
	[CompilerGenerated]
	private sealed class _003CGetAllChunks_003Ed__9 : IEnumerable<DbChunk>, IEnumerable, IEnumerator<DbChunk>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private DbChunk _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SQLiteDbConnectionv2 _003C_003E4__this;

		private string tablename;

		public string _003C_003E3__tablename;

		private SqliteCommand _003Ccmd_003E5__2;

		private SqliteDataReader _003Csqlite_datareader_003E5__3;

		DbChunk IEnumerator<DbChunk>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetAllChunks_003Ed__9(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if ((uint)(num - -4) <= 1u || num == 1)
			{
				try
				{
					if (num == -4 || num == 1)
					{
						try
						{
						}
						finally
						{
							_003C_003Em__Finally2();
						}
					}
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003Ccmd_003E5__2 = null;
			_003Csqlite_datareader_003E5__3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			bool result;
			try
			{
				int num = _003C_003E1__state;
				SQLiteDbConnectionv2 sQLiteDbConnectionv = _003C_003E4__this;
				switch (num)
				{
				default:
					result = false;
					goto end_IL_0000;
				case 0:
					_003C_003E1__state = -1;
					_003Ccmd_003E5__2 = sQLiteDbConnectionv.sqliteConn.CreateCommand();
					_003C_003E1__state = -3;
					_003Ccmd_003E5__2.CommandText = "SELECT position, data FROM " + tablename;
					_003Csqlite_datareader_003E5__3 = _003Ccmd_003E5__2.ExecuteReader();
					_003C_003E1__state = -4;
					break;
				case 1:
					_003C_003E1__state = -4;
					break;
				}
				if (_003Csqlite_datareader_003E5__3.Read())
				{
					object data = _003Csqlite_datareader_003E5__3["data"];
					ChunkPos pos = ChunkPos.FromChunkIndex_saveGamev2((ulong)(long)_003Csqlite_datareader_003E5__3["position"]);
					_003C_003E2__current = new DbChunk
					{
						Position = pos,
						Data = (data as byte[])
					};
					_003C_003E1__state = 1;
					result = true;
				}
				else
				{
					result = false;
					_003C_003Em__Finally2();
					_003C_003Em__Finally1();
				}
				end_IL_0000:;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
			return result;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003Ccmd_003E5__2 != null)
			{
				((IDisposable)_003Ccmd_003E5__2).Dispose();
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -3;
			if (_003Csqlite_datareader_003E5__3 != null)
			{
				((IDisposable)_003Csqlite_datareader_003E5__3).Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<DbChunk> IEnumerable<DbChunk>.GetEnumerator()
		{
			_003CGetAllChunks_003Ed__9 _003CGetAllChunks_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CGetAllChunks_003Ed__ = this;
			}
			else
			{
				_003CGetAllChunks_003Ed__ = new _003CGetAllChunks_003Ed__9(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CGetAllChunks_003Ed__.tablename = _003C_003E3__tablename;
			return _003CGetAllChunks_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<DbChunk>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CGetChunks_003Ed__15 : IEnumerable<byte[]>, IEnumerable, IEnumerator<byte[]>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private byte[] _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SQLiteDbConnectionv2 _003C_003E4__this;

		private IEnumerable<ChunkPos> chunkpositions;

		public IEnumerable<ChunkPos> _003C_003E3__chunkpositions;

		private object _003C_003E7__wrap1;

		private bool _003C_003E7__wrap2;

		private SqliteTransaction _003Ctransaction_003E5__4;

		private IEnumerator<ChunkPos> _003C_003E7__wrap4;

		byte[] IEnumerator<byte[]>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetChunks_003Ed__15(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if ((uint)(num - -5) <= 2u || num == 1)
			{
				try
				{
					if ((uint)(num - -5) <= 1u || num == 1)
					{
						try
						{
							if (num == -5 || num == 1)
							{
								try
								{
								}
								finally
								{
									_003C_003Em__Finally3();
								}
							}
						}
						finally
						{
							_003C_003Em__Finally2();
						}
					}
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003Ctransaction_003E5__4 = null;
			_003C_003E7__wrap4 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				SQLiteDbConnectionv2 sQLiteDbConnectionv = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = sQLiteDbConnectionv.transactionLock;
					_003C_003E7__wrap2 = false;
					_003C_003E1__state = -3;
					Monitor.Enter(_003C_003E7__wrap1, ref _003C_003E7__wrap2);
					_003Ctransaction_003E5__4 = sQLiteDbConnectionv.sqliteConn.BeginTransaction();
					_003C_003E1__state = -4;
					_003C_003E7__wrap4 = chunkpositions.GetEnumerator();
					_003C_003E1__state = -5;
					break;
				case 1:
					_003C_003E1__state = -5;
					break;
				}
				if (_003C_003E7__wrap4.MoveNext())
				{
					_003C_003E2__current = sQLiteDbConnectionv.GetChunk(_003C_003E7__wrap4.Current.ToChunkIndex(), "chunk");
					_003C_003E1__state = 1;
					return true;
				}
				_003C_003Em__Finally3();
				_003C_003E7__wrap4 = null;
				_003Ctransaction_003E5__4.Commit();
				_003C_003Em__Finally2();
				_003Ctransaction_003E5__4 = null;
				_003C_003Em__Finally1();
				_003C_003E7__wrap1 = null;
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap2)
			{
				Monitor.Exit(_003C_003E7__wrap1);
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -3;
			if (_003Ctransaction_003E5__4 != null)
			{
				((IDisposable)_003Ctransaction_003E5__4).Dispose();
			}
		}

		private void _003C_003Em__Finally3()
		{
			_003C_003E1__state = -4;
			if (_003C_003E7__wrap4 != null)
			{
				_003C_003E7__wrap4.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<byte[]> IEnumerable<byte[]>.GetEnumerator()
		{
			_003CGetChunks_003Ed__15 _003CGetChunks_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CGetChunks_003Ed__ = this;
			}
			else
			{
				_003CGetChunks_003Ed__ = new _003CGetChunks_003Ed__15(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CGetChunks_003Ed__.chunkpositions = _003C_003E3__chunkpositions;
			return _003CGetChunks_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<byte[]>)this).GetEnumerator();
		}
	}

	private SqliteCommand setChunksCmd;

	private SqliteCommand setMapChunksCmd;

	public override string DBTypeCode => "savegame database";

	public SQLiteDbConnectionv2(ILogger logger)
		: base(logger)
	{
		base.logger = logger;
	}

	public override void OnOpened()
	{
		setChunksCmd = sqliteConn.CreateCommand();
		setChunksCmd.CommandText = "INSERT OR REPLACE INTO chunk (position, data) VALUES (@position,@data)";
		setChunksCmd.Parameters.Add(CreateParameter("position", DbType.UInt64, 0, setChunksCmd));
		setChunksCmd.Parameters.Add(CreateParameter("data", DbType.Object, null, setChunksCmd));
		setChunksCmd.Prepare();
		setMapChunksCmd = sqliteConn.CreateCommand();
		setMapChunksCmd.CommandText = "INSERT OR REPLACE INTO mapchunk (position, data) VALUES (@position,@data)";
		setMapChunksCmd.Parameters.Add(CreateParameter("position", DbType.UInt64, 0, setMapChunksCmd));
		setMapChunksCmd.Parameters.Add(CreateParameter("data", DbType.Object, null, setMapChunksCmd));
		setMapChunksCmd.Prepare();
	}

	public void UpgradeToWriteAccess()
	{
		CreateTablesIfNotExists(sqliteConn);
	}

	public bool IntegrityCheck()
	{
		if (!DoIntegrityCheck(sqliteConn))
		{
			string msg = "Database integrity check failed. Attempt basic repair procedure (via VACUUM), this might take minutes to hours depending on the size of the save game...";
			logger.Notification(msg);
			logger.StoryEvent(msg);
			try
			{
				using (SqliteCommand sqliteCommand = sqliteConn.CreateCommand())
				{
					sqliteCommand.CommandText = "PRAGMA writable_schema=ON;";
					sqliteCommand.ExecuteNonQuery();
				}
				using SqliteCommand cmd = sqliteConn.CreateCommand();
				cmd.CommandText = "VACUUM;";
				cmd.ExecuteNonQuery();
			}
			catch
			{
				logger.StoryEvent("Unable to repair :(");
				logger.Notification("Unable to repair :(\nRecommend any of the solutions posted here: https://wiki.vintagestory.at/index.php/Repairing_a_corrupt_savegame_or_worldmap\nWill exit now");
				throw new Exception("Database integrity bad");
			}
			if (!DoIntegrityCheck(sqliteConn, logResults: false))
			{
				logger.StoryEvent("Unable to repair :(");
				logger.Notification("Database integrity still bad :(\nRecommend any of the solutions posted here: https://wiki.vintagestory.at/index.php/Repairing_a_corrupt_savegame_or_worldmap\nWill exit now");
				throw new Exception("Database integrity bad");
			}
			logger.Notification("Database integrity check now okay, yay!");
		}
		return true;
	}

	public int QuantityChunks()
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT count(*) FROM chunk";
		return System.Convert.ToInt32(cmd.ExecuteScalar());
	}

	[IteratorStateMachine(typeof(_003CGetAllChunks_003Ed__9))]
	public IEnumerable<DbChunk> GetAllChunks(string tablename)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetAllChunks_003Ed__9(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__tablename = tablename
		};
	}

	public IEnumerable<DbChunk> GetAllChunks()
	{
		return GetAllChunks("chunk");
	}

	public IEnumerable<DbChunk> GetAllMapChunks()
	{
		return GetAllChunks("mapchunk");
	}

	public IEnumerable<DbChunk> GetAllMapRegions()
	{
		return GetAllChunks("mapregion");
	}

	public byte[] GetPlayerData(string playeruid)
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT data FROM playerdata WHERE playeruid=@playeruid";
		cmd.Parameters.Add(CreateParameter("playeruid", DbType.String, playeruid, cmd));
		using SqliteDataReader dataReader = cmd.ExecuteReader();
		if (dataReader.Read())
		{
			return dataReader["data"] as byte[];
		}
		return null;
	}

	public void SetPlayerData(string playeruid, byte[] data)
	{
		if (data == null)
		{
			using (DbCommand deleteCmd = sqliteConn.CreateCommand())
			{
				deleteCmd.CommandText = "DELETE FROM playerdata WHERE playeruid=@playeruid";
				deleteCmd.Parameters.Add(CreateParameter("playeruid", DbType.String, playeruid, deleteCmd));
				deleteCmd.ExecuteNonQuery();
				return;
			}
		}
		if (GetPlayerData(playeruid) == null)
		{
			using (DbCommand insertCmd = sqliteConn.CreateCommand())
			{
				insertCmd.CommandText = "INSERT INTO playerdata (playeruid, data) VALUES (@playeruid,@data)";
				insertCmd.Parameters.Add(CreateParameter("playeruid", DbType.String, playeruid, insertCmd));
				insertCmd.Parameters.Add(CreateParameter("data", DbType.Object, data, insertCmd));
				insertCmd.ExecuteNonQuery();
				return;
			}
		}
		using DbCommand updateCmd = sqliteConn.CreateCommand();
		updateCmd.CommandText = "UPDATE playerdata set data=@data where playeruid=@playeruid";
		updateCmd.Parameters.Add(CreateParameter("data", DbType.Object, data, updateCmd));
		updateCmd.Parameters.Add(CreateParameter("playeruid", DbType.String, playeruid, updateCmd));
		updateCmd.ExecuteNonQuery();
	}

	[IteratorStateMachine(typeof(_003CGetChunks_003Ed__15))]
	public IEnumerable<byte[]> GetChunks(IEnumerable<ChunkPos> chunkpositions)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetChunks_003Ed__15(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__chunkpositions = chunkpositions
		};
	}

	public byte[] GetChunk(ulong position)
	{
		return GetChunk(position, "chunk");
	}

	public byte[] GetMapChunk(ulong position)
	{
		return GetChunk(position, "mapchunk");
	}

	public byte[] GetMapRegion(ulong position)
	{
		return GetChunk(position, "mapregion");
	}

	public bool ChunkExists(ulong position)
	{
		return ChunkExists(position, "chunk");
	}

	public bool MapChunkExists(ulong position)
	{
		return ChunkExists(position, "mapchunk");
	}

	public bool MapRegionExists(ulong position)
	{
		return ChunkExists(position, "mapregion");
	}

	public bool ChunkExists(ulong position, string tablename)
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT position FROM " + tablename + " WHERE position=@position";
		cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, position, cmd));
		using SqliteDataReader dataReader = cmd.ExecuteReader();
		return dataReader.HasRows;
	}

	public byte[] GetChunk(ulong position, string tablename)
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT data FROM " + tablename + " WHERE position=@position";
		cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, position, cmd));
		using SqliteDataReader dataReader = cmd.ExecuteReader();
		if (dataReader.Read())
		{
			return dataReader["data"] as byte[];
		}
		return null;
	}

	public void DeleteChunks(IEnumerable<ChunkPos> chunkpositions)
	{
		DeleteChunks(chunkpositions, "chunk");
	}

	public void DeleteMapChunks(IEnumerable<ChunkPos> mapchunkpositions)
	{
		DeleteChunks(mapchunkpositions, "mapchunk");
	}

	public void DeleteMapRegions(IEnumerable<ChunkPos> mapchunkregions)
	{
		DeleteChunks(mapchunkregions, "mapregion");
	}

	public void DeleteChunks(IEnumerable<ChunkPos> chunkpositions, string tablename)
	{
		lock (transactionLock)
		{
			using SqliteTransaction transaction = sqliteConn.BeginTransaction();
			foreach (ChunkPos chunkposition in chunkpositions)
			{
				DeleteChunk(chunkposition.ToChunkIndex(), tablename);
			}
			transaction.Commit();
		}
	}

	public void DeleteChunk(ulong position, string tablename)
	{
		using DbCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "DELETE FROM " + tablename + " WHERE position=@position";
		cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, position, cmd));
		cmd.ExecuteNonQuery();
	}

	public void SetChunks(IEnumerable<DbChunk> chunks)
	{
		lock (transactionLock)
		{
			using SqliteTransaction transaction = sqliteConn.BeginTransaction();
			setChunksCmd.Transaction = transaction;
			foreach (DbChunk c in chunks)
			{
				setChunksCmd.Parameters["position"].Value = c.Position.ToChunkIndex();
				setChunksCmd.Parameters["data"].Value = c.Data;
				setChunksCmd.ExecuteNonQuery();
			}
			transaction.Commit();
		}
	}

	public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
	{
		lock (transactionLock)
		{
			using SqliteTransaction transaction = sqliteConn.BeginTransaction();
			setMapChunksCmd.Transaction = transaction;
			foreach (DbChunk c in mapchunks)
			{
				c.Position.Y = 0;
				setMapChunksCmd.Parameters["position"].Value = c.Position.ToChunkIndex();
				setMapChunksCmd.Parameters["data"].Value = c.Data;
				setMapChunksCmd.ExecuteNonQuery();
			}
			transaction.Commit();
		}
	}

	public void SetMapRegions(IEnumerable<DbChunk> mapregions)
	{
		lock (transactionLock)
		{
			using SqliteTransaction transaction = sqliteConn.BeginTransaction();
			foreach (DbChunk c in mapregions)
			{
				c.Position.Y = 0;
				InsertChunk(c.Position.ToChunkIndex(), c.Data, "mapregion");
			}
			transaction.Commit();
		}
	}

	private void InsertChunk(ulong position, byte[] data, string tablename)
	{
		using DbCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "INSERT OR REPLACE INTO " + tablename + " (position, data) VALUES (@position,@data)";
		cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, position, cmd));
		cmd.Parameters.Add(CreateParameter("data", DbType.Object, data, cmd));
		cmd.ExecuteNonQuery();
	}

	public byte[] GetGameData()
	{
		try
		{
			using SqliteCommand cmd = sqliteConn.CreateCommand();
			cmd.CommandText = "SELECT data FROM gamedata LIMIT 1";
			using SqliteDataReader sqlite_datareader = cmd.ExecuteReader();
			if (sqlite_datareader.Read())
			{
				return sqlite_datareader["data"] as byte[];
			}
			return null;
		}
		catch (Exception e)
		{
			logger.Warning("Exception thrown on GetGlobalData: " + e.Message);
			return null;
		}
	}

	public void StoreGameData(byte[] data)
	{
		lock (transactionLock)
		{
			using SqliteTransaction transaction = sqliteConn.BeginTransaction();
			using (DbCommand cmd = sqliteConn.CreateCommand())
			{
				cmd.CommandText = "INSERT OR REPLACE INTO gamedata (savegameid, data) VALUES (@savegameid,@data)";
				cmd.Parameters.Add(CreateParameter("savegameid", DbType.UInt64, 1, cmd));
				cmd.Parameters.Add(CreateParameter("data", DbType.Object, data, cmd));
				cmd.ExecuteNonQuery();
			}
			transaction.Commit();
		}
	}

	public bool QuickCorrectSaveGameVersionTest()
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'gamedata';";
		return cmd.ExecuteScalar() != null;
	}

	protected override void CreateTablesIfNotExists(SqliteConnection sqliteConn)
	{
		using (SqliteCommand sqliteCommand = sqliteConn.CreateCommand())
		{
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS chunk (position integer PRIMARY KEY, data BLOB);";
			sqliteCommand.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand2 = sqliteConn.CreateCommand())
		{
			sqliteCommand2.CommandText = "CREATE TABLE IF NOT EXISTS mapchunk (position integer PRIMARY KEY, data BLOB);";
			sqliteCommand2.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand3 = sqliteConn.CreateCommand())
		{
			sqliteCommand3.CommandText = "CREATE TABLE IF NOT EXISTS mapregion (position integer PRIMARY KEY, data BLOB);";
			sqliteCommand3.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand4 = sqliteConn.CreateCommand())
		{
			sqliteCommand4.CommandText = "CREATE TABLE IF NOT EXISTS gamedata (savegameid integer PRIMARY KEY, data BLOB);";
			sqliteCommand4.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand5 = sqliteConn.CreateCommand())
		{
			sqliteCommand5.CommandText = "CREATE TABLE IF NOT EXISTS playerdata (playerid integer PRIMARY KEY AUTOINCREMENT, playeruid TEXT, data BLOB);";
			sqliteCommand5.ExecuteNonQuery();
		}
		using SqliteCommand sqlite_cmd = sqliteConn.CreateCommand();
		sqlite_cmd.CommandText = "CREATE index IF NOT EXISTS index_playeruid on playerdata(playeruid);";
		sqlite_cmd.ExecuteNonQuery();
	}

	public void CreateBackup(string backupFilename)
	{
		if (databaseFileName == backupFilename)
		{
			logger.Error("Cannot overwrite current running database. Chose another destination.");
			return;
		}
		if (File.Exists(backupFilename))
		{
			logger.Error("File " + backupFilename + " exists. Overwriting file.");
		}
		SqliteConnection sqliteBckConn = new SqliteConnection(new DbConnectionStringBuilder
		{
			{
				"Data Source",
				Path.Combine(GamePaths.Backups, backupFilename)
			},
			{ "Pooling", "false" }
		}.ToString());
		sqliteBckConn.Open();
		using (SqliteCommand configCmd = sqliteBckConn.CreateCommand())
		{
			configCmd.CommandText = "PRAGMA journal_mode=Off;";
			configCmd.ExecuteNonQuery();
		}
		sqliteConn.BackupDatabase(sqliteBckConn, sqliteBckConn.Database, sqliteConn.Database);
		sqliteBckConn.Close();
		sqliteBckConn.Dispose();
	}

	public override void Close()
	{
		setChunksCmd?.Dispose();
		setMapChunksCmd?.Dispose();
		base.Close();
	}

	public override void Dispose()
	{
		setChunksCmd?.Dispose();
		setMapChunksCmd?.Dispose();
		base.Dispose();
	}

	bool IGameDbConnection.get_IsReadOnly()
	{
		return base.IsReadOnly;
	}

	bool IGameDbConnection.OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
	{
		return OpenOrCreate(filename, ref errorMessage, requireWriteAccess, corruptionProtection, doIntegrityCheck);
	}

	void IGameDbConnection.Vacuum()
	{
		Vacuum();
	}
}
