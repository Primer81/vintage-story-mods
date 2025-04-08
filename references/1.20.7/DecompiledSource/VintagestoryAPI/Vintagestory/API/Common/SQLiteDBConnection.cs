using System;
using System.Data;
using System.Data.Common;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Vintagestory.API.Common;

public class SQLiteDBConnection : IDisposable
{
	protected SqliteConnection sqliteConn;

	protected string databaseFileName;

	protected ILogger logger;

	public object transactionLock = new object();

	public virtual string DBTypeCode => "database";

	public bool IsReadOnly { get; protected set; }

	public SQLiteDBConnection(ILogger logger)
	{
		this.logger = logger;
	}

	public bool OpenOrCreate(string filename, ref string errorMessage, bool requireWriteAccess, bool corruptionProtection, bool doIntegrityCheck)
	{
		databaseFileName = filename;
		if (!File.Exists(databaseFileName))
		{
			if (!HaveWriteAccessFolder(Path.GetDirectoryName(filename)))
			{
				throw new IOException(string.Format("Cannot create {2} file {0}, the folder {1} does not seem to be writable!", filename, Path.GetDirectoryName(filename), DBTypeCode));
			}
		}
		else if (!HaveWriteAccessFile(new FileInfo(filename)))
		{
			if (requireWriteAccess)
			{
				throw new IOException(string.Format("Cannot open {1} file {0}, it seems to be not writable!", filename, DBTypeCode));
			}
			IsReadOnly = true;
		}
		try
		{
			DbConnectionStringBuilder conf = new DbConnectionStringBuilder
			{
				{ "Data Source", databaseFileName },
				{ "Pooling", "false" }
			};
			if (!requireWriteAccess)
			{
				conf.Add("Mode", "ReadOnly");
			}
			sqliteConn = new SqliteConnection(conf.ToString());
			sqliteConn.Open();
			using (SqliteCommand cmd = sqliteConn.CreateCommand())
			{
				if (corruptionProtection)
				{
					cmd.CommandText = "PRAGMA journal_mode=WAL;PRAGMA synchronous=Normal;";
				}
				else
				{
					cmd.CommandText = "PRAGMA journal_mode=Off;";
				}
				cmd.ExecuteNonQuery();
			}
			if (requireWriteAccess)
			{
				CreateTablesIfNotExists(sqliteConn);
			}
			if (doIntegrityCheck && !DoIntegrityCheck(sqliteConn))
			{
				logger.Error(errorMessage = "Database is possibly corrupted.");
			}
		}
		catch (Exception e)
		{
			logger.Error(errorMessage = "Failed opening savegame.");
			logger.Error(e);
			return false;
		}
		OnOpened();
		return true;
	}

	public virtual void OnOpened()
	{
	}

	public virtual void Close()
	{
		sqliteConn?.Close();
		sqliteConn?.Dispose();
	}

	public virtual void Dispose()
	{
		Close();
	}

	protected virtual void CreateTablesIfNotExists(SqliteConnection sqliteConn)
	{
	}

	public void Vacuum()
	{
		using SqliteCommand command = sqliteConn.CreateCommand();
		command.CommandText = "VACUUM;";
		command.ExecuteNonQuery();
	}

	public bool DoIntegrityCheck(SqliteConnection sqliteConn, bool logResults = true)
	{
		bool okay = false;
		using (SqliteCommand command = sqliteConn.CreateCommand())
		{
			command.CommandText = "PRAGMA integrity_check";
			using SqliteDataReader sqlite_datareader = command.ExecuteReader();
			if (logResults)
			{
				logger.Notification($"Database: {sqliteConn.DataSource}. Running SQLite integrity check:");
			}
			while (sqlite_datareader.Read())
			{
				logger.Notification("Integrity check " + sqlite_datareader[0].ToString());
				if (sqlite_datareader[0].ToString() == "ok")
				{
					okay = true;
					break;
				}
			}
		}
		return okay;
	}

	public static bool HaveWriteAccessFolder(string folderPath)
	{
		if (!Directory.Exists(folderPath))
		{
			throw new DirectoryNotFoundException(folderPath);
		}
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

	protected DbParameter CreateParameter(string parameterName, DbType dbType, object value, DbCommand command)
	{
		DbParameter dbParameter = command.CreateParameter();
		dbParameter.ParameterName = parameterName;
		dbParameter.DbType = dbType;
		dbParameter.Value = value;
		return dbParameter;
	}
}
