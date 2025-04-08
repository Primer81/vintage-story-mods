using System;
using System.Runtime.InteropServices;
using CommandLine;
using VSPlatform;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.Common.Convert;

namespace Vintagestory.Server;

public class ServerProgram
{
	private static ServerMain server;

	private static string[] args;

	private static ServerProgramArgs progArgs;

	private static readonly PosixSignalRegistration[] Signals = new PosixSignalRegistration[2];

	public static void Main(string[] args)
	{
		AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.AssemblyResolve;
		if (RuntimeEnv.OS == OS.Windows)
		{
			ConsoleWindowUtil.QuickEditMode(Enable: false);
		}
		ServerProgram.args = args;
		new ServerProgram();
	}

	private static void OnExit(PosixSignalContext ctx)
	{
		ctx.Cancel = true;
		ServerMain.Logger.Notification("Server termination event received. Shutting down server");
		if (server != null && server.RunPhase != EnumServerRunPhase.Standby)
		{
			server.Stop("External close event (CTRL+C/kill/etc)");
		}
		ServerMain.Logger.Notification("Server: Exit() called");
	}

	public ServerProgram()
	{
		progArgs = new ServerProgramArgs();
		ParserResult<ServerProgramArgs> progArgsRaw = new Parser(delegate(ParserSettings config)
		{
			config.HelpWriter = null;
			config.AutoHelp = false;
			config.AutoVersion = false;
		}).ParseArguments<ServerProgramArgs>(args);
		progArgs = progArgsRaw.Value;
		if (progArgs.DataPath != null && progArgs.DataPath.Length > 0)
		{
			GamePaths.DataPath = progArgs.DataPath;
		}
		if (progArgs.LogPath != null && progArgs.LogPath.Length > 0)
		{
			GamePaths.CustomLogPath = progArgs.LogPath;
		}
		if (progArgs.PrintVersion)
		{
			Console.WriteLine();
			Console.Write("1.20.7");
		}
		else if (progArgs.PrintHelp)
		{
			Console.WriteLine();
			Console.Write(progArgs.GetUsage(progArgsRaw));
		}
		else
		{
			GamePaths.EnsurePathsExist();
			CrashReporter.EnableGlobalExceptionHandling(blnIsConsole: true);
			new CrashReporter(EnumAppSide.Server).Start(Main);
		}
	}

	private void Main()
	{
		ServerMain.xPlatInterface = XPlatformInterfaces.GetInterface();
		Signals[0] = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnExit);
		Signals[1] = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnExit);
		ServerMain.Logger = new ServerLogger(progArgs);
		Lang.PreLoad(ServerMain.Logger, GamePaths.AssetsPath, ServerSettings.Language);
		if (!CleanInstallCheck.IsCleanInstall())
		{
			ServerMain.Logger.Error("Your Server installation still contains old files from a previous game version, which may break things. Please fully delete the /assets folder and then do a full reinstallation. Shutting down server.");
			Environment.Exit(0);
			return;
		}
		server = new ServerMain(null, args, progArgs);
		if (progArgs.GenConfigAndExit || progArgs.SetConfigAndExit != null)
		{
			Environment.Exit(server.ExitCode);
			return;
		}
		ServerMain.Logger.Notification("C# Framework: " + FrameworkInfos());
		ServerMain.Logger.Notification("Zstd Version: " + ZstdNative.Version);
		ServerMain.Logger.Notification("Operating System: " + RuntimeEnv.GetOsString());
		server.exit = new GameExit();
		server.Standalone = true;
		server.PreLaunch();
		if (progArgs.Standby)
		{
			server.StandbyLaunch();
		}
		else
		{
			server.Launch();
		}
		do
		{
			server.Process();
		}
		while (server.exit == null || !server.exit.GetExit());
		server.Stop("Stop through standalone server exit request");
		server.Dispose();
		Environment.Exit(server.ExitCode);
	}

	public static string FrameworkInfos()
	{
		return ".net " + Environment.Version;
	}
}
