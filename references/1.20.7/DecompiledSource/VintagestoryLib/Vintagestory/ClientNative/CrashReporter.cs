using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Common;

namespace Vintagestory.ClientNative;

public class CrashReporter
{
	private string crashLogFileName = "";

	private bool launchCrashReporterGui;

	private static Logger logger;

	public Action OnCrash;

	public bool isCrashing;

	private static bool s_blnIsConsole = false;

	public static List<ModContainer> LoadedMods { get; set; } = new List<ModContainer>();


	public static void SetLogger(Logger logger)
	{
		CrashReporter.logger = logger;
	}

	public CrashReporter(EnumAppSide side)
	{
		crashLogFileName = ((side == EnumAppSide.Client) ? "client-crash.log" : "server-crash.log");
		launchCrashReporterGui = side == EnumAppSide.Client;
	}

	public static void EnableGlobalExceptionHandling(bool blnIsConsole)
	{
		s_blnIsConsole = blnIsConsole;
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
	}

	private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (s_blnIsConsole)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Out.WriteLine("Unhandled Exception occurred");
		}
		Exception ex = e.ExceptionObject as Exception;
		new CrashReporter(Process.GetCurrentProcess().MainModule.FileName.ToLowerInvariant().Contains("server") ? EnumAppSide.Server : EnumAppSide.Client).Crash(ex);
	}

	public void Start(ThreadStart start)
	{
		if (!Debugger.IsAttached)
		{
			try
			{
				start();
				return;
			}
			catch (Exception e)
			{
				Crash(e);
				return;
			}
		}
		start();
	}

	public void Crash(Exception exCrash)
	{
		isCrashing = true;
		StringBuilder fullCrashMsg = new StringBuilder();
		try
		{
			if (!Directory.Exists(GamePaths.Logs))
			{
				Directory.CreateDirectory(GamePaths.Logs);
			}
			string crashfile = Path.Combine(GamePaths.Logs, crashLogFileName);
			fullCrashMsg.AppendLine("Game Version: " + GameVersion.LongGameVersion);
			IEnumerable<ModContainer> codeMods = LoadedMods.Where((ModContainer mod) => mod.Assembly != null && mod.Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright != "Copyright © 2016-2024 Anego Studios");
			StringBuilder stackTraceMsg = new StringBuilder();
			HashSet<ModContainer> culprits = new HashSet<ModContainer>();
			HashSet<string> culpritHarmonyIds = new HashSet<string>();
			for (Exception exToLog = exCrash; exToLog != null; exToLog = exToLog.InnerException)
			{
				stackTraceMsg.AppendLine(LoggerBase.CleanStackTrace(exToLog.ToString()));
				StackFrame[] frames = new StackTrace(exToLog, fNeedFileInfo: true).GetFrames();
				foreach (StackFrame frame in frames)
				{
					MethodBase method;
					try
					{
						method = Harmony.GetMethodFromStackframe(frame);
					}
					catch (Exception)
					{
						continue;
					}
					if (!(method != null))
					{
						continue;
					}
					Assembly assembly = method.DeclaringType?.Assembly;
					if (assembly != null)
					{
						culprits.UnionWith(codeMods.Where((ModContainer mod) => mod.Assembly == assembly));
					}
					if (!(method is MethodInfo methodInfo))
					{
						continue;
					}
					MethodBase original = Harmony.GetOriginalMethod(methodInfo);
					if (original != null)
					{
						Patches patchInfo = Harmony.GetPatchInfo(original);
						if (patchInfo != null)
						{
							culpritHarmonyIds.UnionWith(patchInfo.Owners);
						}
					}
				}
			}
			fullCrashMsg.Append(DateTime.Now.ToString() + ": Critical error occurred");
			if (culprits.Count == 0)
			{
				fullCrashMsg.Append('\n');
			}
			else
			{
				fullCrashMsg.AppendFormat(" in the following mod{0}: {1}\n", (culprits.Count > 1) ? "s" : "", string.Join(", ", culprits.Select((ModContainer mod) => mod.Info?.ModID + "@" + mod.Info?.Version)));
			}
			fullCrashMsg.AppendLine("Loaded Mods: " + string.Join(", ", LoadedMods.Select((ModContainer mod) => mod.Info?.ModID + "@" + mod.Info?.Version)));
			if (culpritHarmonyIds.Count > 0)
			{
				fullCrashMsg.Append("Involved Harmony IDs: ");
				fullCrashMsg.AppendLine(string.Join(", ", culpritHarmonyIds));
			}
			fullCrashMsg.Append(stackTraceMsg);
			Process process = null;
			if (launchCrashReporterGui)
			{
				File.WriteAllText(Path.Combine(Path.GetTempPath(), "VSLastCrash.log"), fullCrashMsg.ToString());
				switch (RuntimeEnv.OS)
				{
				case OS.Windows:
					process = Process.Start(Path.Combine(GamePaths.Binaries, "VSCrashReporter.exe"), new string[1] { GamePaths.Logs });
					break;
				case OS.Mac:
					process = Process.Start("open", new string[1] { Path.Combine(GamePaths.Binaries, "VSCrashReporterMac.app", "--args", GamePaths.Logs) });
					break;
				case OS.Linux:
					process = Process.Start(Path.Combine(GamePaths.Binaries, "VSCrashReporter"), new string[1] { GamePaths.Logs });
					break;
				}
			}
			using (FileStream fs = File.Open(crashfile, FileMode.Append))
			{
				using StreamWriter crashLogger = new StreamWriter(fs);
				crashLogger.Write(fullCrashMsg.ToString());
			}
			fullCrashMsg.AppendLine("Crash written to file at \"" + crashfile + "\"");
			if (logger != null)
			{
				logger.Fatal("{0}", fullCrashMsg.ToString());
			}
			CallOnCrash();
			Console.WriteLine("{0}", fullCrashMsg);
			process?.WaitForExit();
		}
		catch (Exception ex)
		{
			StringBuilder stringBuilder = fullCrashMsg;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder);
			handler.AppendLiteral("Crashreport failed: ");
			handler.AppendFormatted(LoggerBase.CleanStackTrace(ex.ToString()));
			stringBuilder.AppendLine(ref handler);
			logger?.Fatal(fullCrashMsg.ToString());
		}
		finally
		{
			ScreenManager.Platform?.WindowExit("Game crashed");
		}
	}

	private void CallOnCrash()
	{
		if (OnCrash != null)
		{
			try
			{
				OnCrash();
			}
			catch (Exception)
			{
			}
		}
	}
}
