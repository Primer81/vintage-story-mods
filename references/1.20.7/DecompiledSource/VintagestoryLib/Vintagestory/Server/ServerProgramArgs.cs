using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Vintagestory.Server;

public class ServerProgramArgs
{
	[Option('v', "version", HelpText = "Print game version and exit")]
	public bool PrintVersion { get; set; }

	[Option('h', "help", HelpText = "Print help info and exit")]
	public bool PrintHelp { get; set; }

	[Option('s', "standby", HelpText = "Don't fully launch server. Instead wait until the first connection attempt before launching.")]
	public bool Standby { get; set; }

	[Option("tracelog", HelpText = "Print log also via Trace.WriteLine() to get it to show up in the visual studio output window")]
	public bool TraceLog { get; set; }

	[Option("append", HelpText = "Do not overwrite log files")]
	public bool Append { get; set; }

	[Option("genconfig", HelpText = "Generate a new default serverconfig.json and exit. Warning, this deletes any existing config.")]
	public bool GenConfigAndExit { get; set; }

	[Option("setconfig", HelpText = "Set a config value and exit. Generates a serverconfig.json if it doesn't exist. Use the format --setconfig=\"{ key: 3, foo: 'value' }\"")]
	public string SetConfigAndExit { get; set; }

	[Option("withconfig", HelpText = "Can be used to override any config value. Launches the server. Use the format --withconfig=\"{ key: 3, foo: 'value' }\"")]
	public string WithConfig { get; set; }

	[Option("dataPath", HelpText = "Set a custom data path, default is Environment.SpecialFolder.ApplicationData")]
	public string DataPath { get; set; }

	[Option("logPath", HelpText = "Default logs folder is in dataPath/Logs/. This option can only set an absolute path.")]
	public string LogPath { get; set; }

	[Option("addOrigin", HelpText = "Tells the asset manager to also load assets from this path")]
	public IEnumerable<string> AddOrigin { get; set; }

	[Option("addModPath", HelpText = "Tells the mod loader to also load mods from this path")]
	public IEnumerable<string> AddModPath { get; set; }

	[Option("ip", HelpText = "Bind server to given ip, overwrites configured value (default: all ips)")]
	public string Ip { get; set; }

	[Option("port", HelpText = "Bind server to given port, overwrites configured value  (default: 42420)")]
	public string Port { get; set; }

	[Option("maxclients", HelpText = "Maximum quantity of clients to be connected at the same time, overwrites configured value  (default 16)")]
	public string MaxClients { get; set; }

	[Option("archiveLogFileCount", HelpText = "The Amount of logs to archive and keep. The oldest will be deleted when a new archive is created and the limit is exceeded (default: 5)")]
	public int ArchiveLogFileCount { get; set; } = 5;


	[Option("archiveLogFileMaxSizeMb", HelpText = "The max size (in MB) of a set of log files to be archived. If it exceeds this value it will not be archived. (default: 1024 MB)")]
	public int ArchiveLogFileMaxSizeMb { get; set; } = 1024;


	public string GetUsage(ParserResult<ServerProgramArgs> parser)
	{
		HelpText helpText = HelpText.AutoBuild(parser, (HelpText h) => h, (Example e) => e);
		helpText.Heading = "Vintage Story Server 1.20.7";
		return helpText.ToString();
	}
}
