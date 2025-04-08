using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Vintagestory.Client;

public class ClientProgramArgs
{
	[Option('v', "version", HelpText = "Print game version and exit")]
	public bool PrintVersion { get; set; }

	[Option('h', "help", HelpText = "Print help info and exit")]
	public bool PrintHelp { get; set; }

	[Option("tracelog", HelpText = "Print log also via Trace.WriteLine() to get it to show up in the visual studio output window")]
	public bool TraceLog { get; set; }

	[Option('o', "openWorld", HelpText = "Opens given world. If it doesn't exist it will be created")]
	public string OpenWorldName { get; set; }

	[Option('c', "connect", HelpText = "Connect to given server")]
	public string ConnectServerAddress { get; set; }

	[Option('i', "installmod", HelpText = "Install given mod in the format: modid@version")]
	public string InstallModId { get; set; }

	[Option("pw", HelpText = "Password for the server (if any)")]
	public string Password { get; set; }

	[Option("rndWorld", HelpText = "Creates a new world with a random name. Use -p modifier to set playstyle")]
	public bool CreateRndWorld { get; set; }

	[Option('p', "playStyle", Default = "creativebuilding", HelpText = "Used when creating a new world")]
	public string PlayStyle { get; set; }

	[Option("dataPath", HelpText = "Set a custom data path, default is Environment.SpecialFolder.ApplicationData")]
	public string DataPath { get; set; }

	[Option("logPath", HelpText = "Default logs folder is in dataPath/Logs/. This option can only set an absolute path.")]
	public string LogPath { get; set; }

	[Option("addOrigin", HelpText = "Tells the asset manager to also load assets from this path")]
	public IEnumerable<string> AddOrigin { get; set; }

	[Option("addModPath", HelpText = "Tells the mod loader to also load mods from this path")]
	public IEnumerable<string> AddModPath { get; set; }

	public string GetUsage(ParserResult<ClientProgramArgs> parser)
	{
		HelpText helpText = HelpText.AutoBuild(parser, (HelpText h) => h, (Example e) => e);
		helpText.Heading = "Vintage Story Client 1.20.7";
		return helpText.ToString();
	}
}
