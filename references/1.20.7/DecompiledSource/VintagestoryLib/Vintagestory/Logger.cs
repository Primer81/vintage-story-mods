#define TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory;

public abstract class Logger : LoggerBase, IDisposable
{
	[CompilerGenerated]
	private sealed class _003CReadLines_003Ed__16 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private string path;

		public string _003C_003E3__path;

		private FileStream _003Cfs_003E5__2;

		private StreamReader _003Csr_003E5__3;

		string IEnumerator<string>.Current
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
		public _003CReadLines_003Ed__16(int _003C_003E1__state)
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
			_003Cfs_003E5__2 = null;
			_003Csr_003E5__3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				switch (_003C_003E1__state)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003Cfs_003E5__2 = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
					_003C_003E1__state = -3;
					_003Csr_003E5__3 = new StreamReader(_003Cfs_003E5__2, Encoding.UTF8);
					_003C_003E1__state = -4;
					break;
				case 1:
					_003C_003E1__state = -4;
					break;
				}
				string line;
				if ((line = _003Csr_003E5__3.ReadLine()) != null)
				{
					_003C_003E2__current = line;
					_003C_003E1__state = 1;
					return true;
				}
				_003C_003Em__Finally2();
				_003Csr_003E5__3 = null;
				_003C_003Em__Finally1();
				_003Cfs_003E5__2 = null;
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
			if (_003Cfs_003E5__2 != null)
			{
				((IDisposable)_003Cfs_003E5__2).Dispose();
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -3;
			if (_003Csr_003E5__3 != null)
			{
				((IDisposable)_003Csr_003E5__3).Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			_003CReadLines_003Ed__16 _003CReadLines_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CReadLines_003Ed__ = this;
			}
			else
			{
				_003CReadLines_003Ed__ = new _003CReadLines_003Ed__16(0);
			}
			_003CReadLines_003Ed__.path = _003C_003E3__path;
			return _003CReadLines_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private string program = "";

	protected Dictionary<string, DisposableWriter> fileWriters = new Dictionary<string, DisposableWriter>();

	protected Dictionary<string, uint> LinesWritten = new Dictionary<string, uint>();

	protected Dictionary<string, int> LogfileSplitNumbers = new Dictionary<string, int>();

	protected bool exceptionPrinted;

	protected bool disposed;

	protected bool canUseColor = true;

	private static string[] logTypeToExcludeFromArchive = new string[2] { "crash", "worldgen" };

	public const string ArchiveFolderDateFormat = "yyyy-MM-dd_HH_mm_ss";

	public const string LogDateFormat = "d.M.yyyy HH:mm:ss";

	public const string LogDateFormatVerbose = "d.M.yyyy HH:mm:ss.fff";

	private static bool _logsRotated;

	public static uint LogFileSplitAfterLine = 500000u;

	public Logger(string program, bool clearOldFiles, int archiveLogFileCount, int archiveLogFileMaxSize)
	{
		this.program = program;
		if (clearOldFiles && !_logsRotated)
		{
			ArchiveLogFiles(archiveLogFileCount, archiveLogFileMaxSize);
		}
		foreach (EnumLogType logType in Enum.GetValues(typeof(EnumLogType)))
		{
			string logFileName = getLogFile(logType);
			if (logFileName != null && !fileWriters.ContainsKey(logFileName))
			{
				try
				{
					fileWriters.Add(logFileName, new DisposableWriter(logFileName, logType != EnumLogType.Worldgen && clearOldFiles));
					LinesWritten.Add(logFileName, 0u);
					LogfileSplitNumbers.Add(logFileName, 2);
				}
				catch (Exception e)
				{
					Error("Cannot open logfile {0} for writing ", logFileName);
					Error(e);
				}
			}
		}
		Notification("{0} logger started.", program);
		Notification("Game Version: {0}", GameVersion.LongGameVersion);
	}

	private static void ArchiveLogFiles(int archiveLogFileCount, int archiveLogFileMaxSize)
	{
		_logsRotated = true;
		List<FileInfo> logsToMove = (from f in Directory.GetFiles(GamePaths.Logs)
			where !logTypeToExcludeFromArchive.Any(f.Contains)
			select f into folder
			select new FileInfo(folder) into file
			orderby file.LastWriteTime descending
			select file).ToList();
		if (logsToMove.Count <= 0)
		{
			return;
		}
		string sessionTimeStamp = GetSessionTimeStamp(logsToMove.First().FullName);
		string archiveFolder = Path.Combine(GamePaths.Logs, "Archive");
		Directory.CreateDirectory(archiveFolder);
		string sessionArchiveFolder = Path.Combine(GamePaths.Logs, "Archive", sessionTimeStamp);
		Directory.CreateDirectory(sessionArchiveFolder);
		bool delete = logsToMove.Sum((FileInfo file) => file.Length / 1024 / 1024) > archiveLogFileMaxSize;
		foreach (FileInfo file2 in logsToMove)
		{
			try
			{
				if (delete)
				{
					File.Delete(file2.FullName);
				}
				else
				{
					File.Move(file2.FullName, Path.Combine(sessionArchiveFolder, file2.Name));
				}
			}
			catch (Exception)
			{
			}
		}
		List<DirectoryInfo> directories = (from folder in Directory.GetDirectories(archiveFolder)
			select new DirectoryInfo(folder)).ToList();
		if (directories.Count < archiveLogFileCount)
		{
			return;
		}
		foreach (DirectoryInfo dir in directories.OrderBy((DirectoryInfo folder) => folder.CreationTime).Take(directories.Count - archiveLogFileCount))
		{
			string[] files = Directory.GetFiles(dir.FullName);
			for (int i = 0; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
			Directory.Delete(dir.FullName);
		}
	}

	private static string GetSessionTimeStamp(string logFile)
	{
		string sessionTimeStamp = null;
		IEnumerator<string> logFileLines = ReadLines(logFile).GetEnumerator();
		logFileLines.MoveNext();
		string timeStamp = string.Join(' ', logFileLines.Current?.Split(' ').Take(2) ?? Array.Empty<string>());
		logFileLines.Dispose();
		if (string.IsNullOrWhiteSpace(timeStamp))
		{
			return DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
		}
		if (DateTime.TryParseExact(timeStamp, "d.M.yyyy HH:mm:ss", null, DateTimeStyles.None, out var sessionTimeStampParsed))
		{
			return sessionTimeStampParsed.ToString("yyyy-MM-dd_HH_mm_ss", CultureInfo.InvariantCulture);
		}
		return DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss");
	}

	[IteratorStateMachine(typeof(_003CReadLines_003Ed__16))]
	public static IEnumerable<string> ReadLines(string path)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CReadLines_003Ed__16(-2)
		{
			_003C_003E3__path = path
		};
	}

	public void Dispose()
	{
		foreach (DisposableWriter value in fileWriters.Values)
		{
			value.Dispose();
		}
		fileWriters.Clear();
		disposed = true;
	}

	public abstract string getLogFile(EnumLogType logType);

	public abstract bool printToConsole(EnumLogType logType);

	public abstract bool printToDebugWindow(EnumLogType logType);

	protected override void LogImpl(EnumLogType logType, string message, params object[] args)
	{
		if (disposed)
		{
			return;
		}
		try
		{
			string logFileName = getLogFile(logType);
			if (logFileName != null)
			{
				try
				{
					LogToFile(logFileName, logType, message, args);
				}
				catch (NotSupportedException)
				{
					Console.WriteLine("Unable to write to log file " + logFileName);
				}
				catch (ObjectDisposedException)
				{
					Console.WriteLine("Unable to write to log file " + logFileName);
				}
			}
			if (base.TraceLog && printToDebugWindow(logType))
			{
				Trace.WriteLine(FormatLogEntry(logType, message, args));
			}
			if (printToConsole(logType))
			{
				SetColorForLogType(logType);
				Console.WriteLine(FormatLogEntry(logType, message, args));
				if (canUseColor)
				{
					Console.ResetColor();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public string FormatLogEntry(EnumLogType logType, string message, params object[] args)
	{
		return string.Format(DateTime.Now.ToString((logType == EnumLogType.VerboseDebug) ? "d.M.yyyy HH:mm:ss.fff" : "d.M.yyyy HH:mm:ss") + " [" + program + " " + logType.ToString() + "] " + message, args);
	}

	public virtual void LogToFile(string logFileName, EnumLogType logType, string message, params object[] args)
	{
		if (!fileWriters.ContainsKey(logFileName) || disposed)
		{
			return;
		}
		try
		{
			LinesWritten[logFileName]++;
			if (LinesWritten[logFileName] > LogFileSplitAfterLine)
			{
				LinesWritten[logFileName] = 0u;
				string filename = $"{logFileName.Replace(".log", "")}-{LogfileSplitNumbers[logFileName]}.log";
				fileWriters[logFileName].Dispose();
				fileWriters[logFileName] = new DisposableWriter(filename, clearOldFiles: true);
				LogfileSplitNumbers[logFileName]++;
			}
			string type = logType.ToString() ?? "";
			if (logType == EnumLogType.StoryEvent)
			{
				type = "Event";
			}
			fileWriters[logFileName].writer.WriteLine(DateTime.Now.ToString((logType == EnumLogType.VerboseDebug) ? "d.M.yyyy HH:mm:ss.fff" : "d.M.yyyy HH:mm:ss") + " [" + type + "] " + message, args);
			fileWriters[logFileName].writer.Flush();
		}
		catch (FormatException)
		{
			if (!exceptionPrinted)
			{
				exceptionPrinted = true;
				Error("Couldn't write to log file, failed formatting {0} (FormatException)", message);
			}
		}
		catch (Exception e)
		{
			if (!exceptionPrinted)
			{
				exceptionPrinted = true;
				Error("Couldn't write to log file {0}!", logFileName);
				Error(e);
			}
		}
	}

	private void SetColorForLogType(EnumLogType logType)
	{
		if (!canUseColor)
		{
			return;
		}
		try
		{
			switch (logType)
			{
			case EnumLogType.Error:
			case EnumLogType.Fatal:
				Console.ForegroundColor = ConsoleColor.Red;
				break;
			case EnumLogType.Warning:
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				break;
			}
		}
		catch (Exception)
		{
			canUseColor = false;
		}
	}
}
