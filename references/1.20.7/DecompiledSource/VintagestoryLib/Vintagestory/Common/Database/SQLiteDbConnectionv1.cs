using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common.Database;

public class SQLiteDbConnectionv1 : IGameDbConnection, IDisposable
{
	[CompilerGenerated]
	private sealed class _003CGetAllChunks_003Ed__14 : IEnumerable<DbChunk>, IEnumerable, IEnumerator<DbChunk>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private DbChunk _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SQLiteDbConnectionv1 _003C_003E4__this;

		private SqliteCommand _003Ccmd_003E5__2;

		private SqliteDataReader _003Creader_003E5__3;

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
		public _003CGetAllChunks_003Ed__14(int _003C_003E1__state)
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
			_003Creader_003E5__3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			bool result;
			try
			{
				int num = _003C_003E1__state;
				SQLiteDbConnectionv1 sQLiteDbConnectionv = _003C_003E4__this;
				switch (num)
				{
				default:
					result = false;
					goto end_IL_0000;
				case 0:
					_003C_003E1__state = -1;
					_003Ccmd_003E5__2 = sQLiteDbConnectionv.sqliteConn.CreateCommand();
					_003C_003E1__state = -3;
					_003Ccmd_003E5__2.CommandText = "SELECT position, data FROM chunks";
					_003Creader_003E5__3 = _003Ccmd_003E5__2.ExecuteReader();
					_003C_003E1__state = -4;
					break;
				case 1:
					_003C_003E1__state = -4;
					break;
				}
				while (true)
				{
					if (_003Creader_003E5__3.Read())
					{
						object value = _003Creader_003E5__3["position"];
						object data = _003Creader_003E5__3["data"];
						ulong posUlong = System.Convert.ToUInt64(value);
						Vec3i pos = FromMapPos(posUlong);
						if (pos.Y != MapChunkYCoord && pos.Y != MapRegionYCoord && posUlong != long.MaxValue)
						{
							_003C_003E2__current = new DbChunk
							{
								Position = new ChunkPos(pos),
								Data = (data as byte[])
							};
							_003C_003E1__state = 1;
							result = true;
							break;
						}
						continue;
					}
					result = false;
					_003C_003Em__Finally2();
					_003C_003Em__Finally1();
					break;
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
			if (_003Creader_003E5__3 != null)
			{
				((IDisposable)_003Creader_003E5__3).Dispose();
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
			_003CGetAllChunks_003Ed__14 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CGetAllChunks_003Ed__14(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<DbChunk>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CGetAllMapChunks_003Ed__15 : IEnumerable<DbChunk>, IEnumerable, IEnumerator<DbChunk>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private DbChunk _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SQLiteDbConnectionv1 _003C_003E4__this;

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
		public _003CGetAllMapChunks_003Ed__15(int _003C_003E1__state)
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
				SQLiteDbConnectionv1 sQLiteDbConnectionv = _003C_003E4__this;
				switch (num)
				{
				default:
					result = false;
					goto end_IL_0000;
				case 0:
					_003C_003E1__state = -1;
					_003Ccmd_003E5__2 = sQLiteDbConnectionv.sqliteConn.CreateCommand();
					_003C_003E1__state = -3;
					_003Ccmd_003E5__2.CommandText = "SELECT position, data FROM chunks";
					_003Csqlite_datareader_003E5__3 = _003Ccmd_003E5__2.ExecuteReader();
					_003C_003E1__state = -4;
					break;
				case 1:
					_003C_003E1__state = -4;
					break;
				}
				while (true)
				{
					if (_003Csqlite_datareader_003E5__3.Read())
					{
						object value = _003Csqlite_datareader_003E5__3["position"];
						object data = _003Csqlite_datareader_003E5__3["data"];
						ulong posUlong = System.Convert.ToUInt64(value);
						Vec3i pos = FromMapPos(posUlong);
						if (pos.Y == MapChunkYCoord && posUlong != long.MaxValue)
						{
							_003C_003E2__current = new DbChunk
							{
								Position = new ChunkPos(pos),
								Data = (data as byte[])
							};
							_003C_003E1__state = 1;
							result = true;
							break;
						}
						continue;
					}
					result = false;
					_003C_003Em__Finally2();
					_003C_003Em__Finally1();
					break;
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
			_003CGetAllMapChunks_003Ed__15 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CGetAllMapChunks_003Ed__15(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<DbChunk>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CGetAllMapRegions_003Ed__16 : IEnumerable<DbChunk>, IEnumerable, IEnumerator<DbChunk>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private DbChunk _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SQLiteDbConnectionv1 _003C_003E4__this;

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
		public _003CGetAllMapRegions_003Ed__16(int _003C_003E1__state)
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
				SQLiteDbConnectionv1 sQLiteDbConnectionv = _003C_003E4__this;
				switch (num)
				{
				default:
					result = false;
					goto end_IL_0000;
				case 0:
					_003C_003E1__state = -1;
					_003Ccmd_003E5__2 = sQLiteDbConnectionv.sqliteConn.CreateCommand();
					_003C_003E1__state = -3;
					_003Ccmd_003E5__2.CommandText = "SELECT position, data FROM chunks";
					_003Csqlite_datareader_003E5__3 = _003Ccmd_003E5__2.ExecuteReader();
					_003C_003E1__state = -4;
					break;
				case 1:
					_003C_003E1__state = -4;
					break;
				}
				while (true)
				{
					if (_003Csqlite_datareader_003E5__3.Read())
					{
						object value = _003Csqlite_datareader_003E5__3["position"];
						object data = _003Csqlite_datareader_003E5__3["data"];
						ulong posUlong = System.Convert.ToUInt64(value);
						Vec3i pos = FromMapPos(posUlong);
						if (pos.Y == MapRegionYCoord && posUlong != long.MaxValue)
						{
							_003C_003E2__current = new DbChunk
							{
								Position = new ChunkPos(pos),
								Data = (data as byte[])
							};
							_003C_003E1__state = 1;
							result = true;
							break;
						}
						continue;
					}
					result = false;
					_003C_003Em__Finally2();
					_003C_003Em__Finally1();
					break;
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
			_003CGetAllMapRegions_003Ed__16 result;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				result = this;
			}
			else
			{
				result = new _003CGetAllMapRegions_003Ed__16(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<DbChunk>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class _003CGetChunks_003Ed__13 : IEnumerable<byte[]>, IEnumerable, IEnumerator<byte[]>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private byte[] _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public SQLiteDbConnectionv1 _003C_003E4__this;

		private IEnumerable<Vec3i> chunkpositions;

		public IEnumerable<Vec3i> _003C_003E3__chunkpositions;

		private SqliteTransaction _003Ctransaction_003E5__2;

		private IEnumerator<Vec3i> _003C_003E7__wrap2;

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
		public _003CGetChunks_003Ed__13(int _003C_003E1__state)
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
			_003Ctransaction_003E5__2 = null;
			_003C_003E7__wrap2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			bool result;
			try
			{
				int num = _003C_003E1__state;
				SQLiteDbConnectionv1 sQLiteDbConnectionv = _003C_003E4__this;
				switch (num)
				{
				default:
					result = false;
					goto end_IL_0000;
				case 0:
					_003C_003E1__state = -1;
					_003Ctransaction_003E5__2 = sQLiteDbConnectionv.sqliteConn.BeginTransaction();
					_003C_003E1__state = -3;
					_003C_003E7__wrap2 = chunkpositions.GetEnumerator();
					_003C_003E1__state = -4;
					break;
				case 1:
					_003C_003E1__state = -4;
					break;
				}
				if (_003C_003E7__wrap2.MoveNext())
				{
					Vec3i xyz = _003C_003E7__wrap2.Current;
					ulong pos = ToMapPos(xyz.X, xyz.Y, xyz.Z);
					_003C_003E2__current = sQLiteDbConnectionv.GetChunk(pos);
					_003C_003E1__state = 1;
					result = true;
				}
				else
				{
					_003C_003Em__Finally2();
					_003C_003E7__wrap2 = null;
					_003Ctransaction_003E5__2.Commit();
					result = false;
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
			if (_003Ctransaction_003E5__2 != null)
			{
				((IDisposable)_003Ctransaction_003E5__2).Dispose();
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -3;
			if (_003C_003E7__wrap2 != null)
			{
				_003C_003E7__wrap2.Dispose();
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
			_003CGetChunks_003Ed__13 _003CGetChunks_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CGetChunks_003Ed__ = this;
			}
			else
			{
				_003CGetChunks_003Ed__ = new _003CGetChunks_003Ed__13(0)
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

	private SqliteConnection sqliteConn;

	private string databaseFileName;

	public ILogger logger = new NullLogger();

	private static int MapChunkYCoord = 99998;

	private static int MapRegionYCoord = 99999;

	private static ulong pow20minus1 = 1048575uL;

	public bool IsReadOnly => false;

	public SQLiteDbConnectionv1(ILogger logger)
	{
		this.logger = logger;
	}

	public bool OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
	{
		try
		{
			databaseFileName = filename;
			bool newdatabase = !File.Exists(databaseFileName);
			DbConnectionStringBuilder conf = new DbConnectionStringBuilder
			{
				{ "Data Source", databaseFileName },
				{ "Pooling", "false" }
			};
			sqliteConn = new SqliteConnection(conf.ToString());
			sqliteConn.Open();
			using (SqliteCommand cmd = sqliteConn.CreateCommand())
			{
				cmd.CommandText = "PRAGMA journal_mode=Off;";
				cmd.ExecuteNonQuery();
			}
			if (newdatabase)
			{
				CreateTables(sqliteConn);
			}
			if (doIntegrityCheck && !integrityCheck(sqliteConn))
			{
				logger.Error("Database is possibly corrupted.");
			}
		}
		catch (Exception e)
		{
			logger.Error(errorMessage = "Failed opening savegame.");
			logger.Error(e);
			return false;
		}
		return true;
	}

	public void Close()
	{
		sqliteConn.Close();
		sqliteConn.Dispose();
	}

	public void Dispose()
	{
		Close();
	}

	private void CreateTables(SqliteConnection sqliteConn)
	{
		using SqliteCommand sqlite_cmd = sqliteConn.CreateCommand();
		sqlite_cmd.CommandText = "CREATE TABLE chunks (position integer PRIMARY KEY, data BLOB);";
		sqlite_cmd.ExecuteNonQuery();
	}

	public void CreateBackup(string backupFilename)
	{
		if (databaseFileName == backupFilename)
		{
			logger.Warning("Cannot overwrite current running database. Chose another destination.");
			return;
		}
		if (File.Exists(backupFilename))
		{
			logger.Notification("File " + backupFilename + " exists. Overwriting file.");
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

	private bool integrityCheck(SqliteConnection sqliteConn)
	{
		bool okay = false;
		using SqliteCommand command = sqliteConn.CreateCommand();
		command.CommandText = "PRAGMA integrity_check";
		using SqliteDataReader sqlite_datareader = command.ExecuteReader();
		logger.Notification($"Database: {sqliteConn.DataSource}. Running SQLite integrity check...");
		while (sqlite_datareader.Read())
		{
			logger.Notification("Integrity check " + sqlite_datareader[0].ToString());
			if (sqlite_datareader[0].ToString() == "ok")
			{
				okay = true;
				break;
			}
		}
		return okay;
	}

	public int QuantityChunks()
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT count(*) FROM chunks";
		return System.Convert.ToInt32(cmd.ExecuteScalar());
	}

	[IteratorStateMachine(typeof(_003CGetChunks_003Ed__13))]
	public IEnumerable<byte[]> GetChunks(IEnumerable<Vec3i> chunkpositions)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetChunks_003Ed__13(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__chunkpositions = chunkpositions
		};
	}

	[IteratorStateMachine(typeof(_003CGetAllChunks_003Ed__14))]
	public IEnumerable<DbChunk> GetAllChunks()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetAllChunks_003Ed__14(-2)
		{
			_003C_003E4__this = this
		};
	}

	[IteratorStateMachine(typeof(_003CGetAllMapChunks_003Ed__15))]
	public IEnumerable<DbChunk> GetAllMapChunks()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetAllMapChunks_003Ed__15(-2)
		{
			_003C_003E4__this = this
		};
	}

	[IteratorStateMachine(typeof(_003CGetAllMapRegions_003Ed__16))]
	public IEnumerable<DbChunk> GetAllMapRegions()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetAllMapRegions_003Ed__16(-2)
		{
			_003C_003E4__this = this
		};
	}

	public byte[] GetMapChunk(ulong position)
	{
		Vec3i vec = FromMapPos(position);
		position = ToMapPos(vec.X, MapChunkYCoord, vec.Z);
		return GetChunk(position);
	}

	public byte[] GetMapRegion(ulong position)
	{
		Vec3i vec = FromMapPos(position);
		position = ToMapPos(vec.X, MapRegionYCoord, vec.Z);
		return GetChunk(position);
	}

	public byte[] GetChunk(ulong position)
	{
		using SqliteCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "SELECT data FROM chunks WHERE position=@position";
		cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, position, cmd));
		using SqliteDataReader dataReader = cmd.ExecuteReader();
		if (dataReader.Read())
		{
			return dataReader["data"] as byte[];
		}
		return null;
	}

	public void DeleteMapChunks(IEnumerable<ChunkPos> coords)
	{
		using SqliteTransaction transaction = sqliteConn.BeginTransaction();
		foreach (ChunkPos vec in coords)
		{
			DeleteChunk(ToMapPos(vec.X, MapChunkYCoord, vec.Z));
		}
		transaction.Commit();
	}

	public void DeleteMapRegions(IEnumerable<ChunkPos> coords)
	{
		using SqliteTransaction transaction = sqliteConn.BeginTransaction();
		foreach (ChunkPos vec in coords)
		{
			DeleteChunk(ToMapPos(vec.X, MapRegionYCoord, vec.Z));
		}
		transaction.Commit();
	}

	public void DeleteChunks(IEnumerable<ChunkPos> coords)
	{
		using SqliteTransaction transaction = sqliteConn.BeginTransaction();
		foreach (ChunkPos vec in coords)
		{
			DeleteChunk(ToMapPos(vec.X, vec.Y, vec.Z));
		}
		transaction.Commit();
	}

	public void DeleteChunk(ulong position)
	{
		using DbCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "DELETE FROM chunks WHERE position=@position";
		cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, position, cmd));
		cmd.ExecuteNonQuery();
	}

	public void SetChunks(IEnumerable<DbChunk> chunks)
	{
		using SqliteTransaction transaction = sqliteConn.BeginTransaction();
		foreach (DbChunk c in chunks)
		{
			ulong pos = ToMapPos(c.Position.X, c.Position.Y, c.Position.Z);
			InsertChunk(pos, c.Data);
		}
		transaction.Commit();
	}

	public void SetMapChunks(IEnumerable<DbChunk> mapchunks)
	{
		foreach (DbChunk mapchunk in mapchunks)
		{
			mapchunk.Position.Y = MapChunkYCoord;
		}
		SetChunks(mapchunks);
	}

	public void SetMapRegions(IEnumerable<DbChunk> mapregions)
	{
		foreach (DbChunk mapregion in mapregions)
		{
			mapregion.Position.Y = MapRegionYCoord;
		}
		SetChunks(mapregions);
	}

	private void InsertChunk(ulong position, byte[] data)
	{
		using DbCommand cmd = sqliteConn.CreateCommand();
		cmd.CommandText = "INSERT OR REPLACE INTO chunks (position, data) VALUES (@position,@data)";
		cmd.Parameters.Add(CreateParameter("position", DbType.UInt64, position, cmd));
		cmd.Parameters.Add(CreateParameter("data", DbType.Object, data, cmd));
		cmd.ExecuteNonQuery();
	}

	private DbParameter CreateParameter(string parameterName, DbType dbType, object value, DbCommand command)
	{
		DbParameter dbParameter = command.CreateParameter();
		dbParameter.ParameterName = parameterName;
		dbParameter.DbType = dbType;
		dbParameter.Value = value;
		return dbParameter;
	}

	public byte[] GetGameData()
	{
		try
		{
			return GetChunk(9223372036854775807uL);
		}
		catch (Exception e)
		{
			logger.Warning("Exception thrown on GetGlobalData: " + e.Message);
			return null;
		}
	}

	public void StoreGameData(byte[] data)
	{
		using SqliteTransaction transaction = sqliteConn.BeginTransaction();
		InsertChunk(9223372036854775807uL, data);
		transaction.Commit();
	}

	public bool QuickCorrectSaveGameVersionTest()
	{
		return true;
	}

	public static Vec3i FromMapPos(ulong v)
	{
		uint z = (uint)(v & pow20minus1);
		v >>= 20;
		uint y = (uint)(v & pow20minus1);
		v >>= 20;
		return new Vec3i((int)(v & pow20minus1), (int)y, (int)z);
	}

	public static ulong ToMapPos(int x, int y, int z)
	{
		return (ulong)(((long)x << 40) | ((long)y << 20) | (uint)z);
	}

	public byte[] GetPlayerData(string playeruid)
	{
		throw new NotImplementedException();
	}

	public void SetPlayerData(string playeruid, byte[] data)
	{
		throw new NotImplementedException();
	}

	public void UpgradeToWriteAccess()
	{
	}

	public bool IntegrityCheck()
	{
		throw new NotImplementedException();
	}

	public bool ChunkExists(ulong position)
	{
		throw new NotImplementedException();
	}

	public bool MapChunkExists(ulong position)
	{
		throw new NotImplementedException();
	}

	public bool MapRegionExists(ulong position)
	{
		throw new NotImplementedException();
	}

	public void Vacuum()
	{
		using SqliteCommand command = sqliteConn.CreateCommand();
		command.CommandText = "VACUUM;";
		command.ExecuteNonQuery();
	}
}
