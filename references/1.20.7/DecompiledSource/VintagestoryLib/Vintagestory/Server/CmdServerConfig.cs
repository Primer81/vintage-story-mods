using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdServerConfig
{
	private ServerMain server;

	private HashSet<string> unavailableInHostedMode = new HashSet<string>(new string[6] { "MaxChunkRadius", "MaxClients", "Upnp", "EntityDebugMode", "TickTime", "RandomBlockTicksPerChunk" });

	private ServerConfig Config => server.Config;

	private bool ConfigNeedsSaving
	{
		get
		{
			return server.ConfigNeedsSaving;
		}
		set
		{
			server.ConfigNeedsSaving = value;
		}
	}

	public CmdServerConfig(ServerMain server)
	{
		CmdServerConfig cmdServerConfig = this;
		this.server = server;
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		chatCommands.Create("serverconfig").WithAlias("sc").WithDesc("Read or Set server configuration")
			.RequiresPrivilege(Privilege.controlserver)
			.BeginSub("nopassword")
			.WithDesc("Remove password protection, if set")
			.HandleWith(delegate
			{
				if (server.Config.Password == null || server.Config.Password == "")
				{
					return TextCommandResult.Error("There is already no password protection in place");
				}
				server.Config.Password = "";
				server.ConfigNeedsSaving = true;
				return TextCommandResult.Success("Password protection now removed");
			})
			.EndSub()
			.BeginSub("simrange")
			.WithDesc("Get or temporarily set entity simulation range. Value is not saved. Default is 128")
			.WithArgs(parsers.OptionalFloat("range"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("simrange", args))
			.EndSub()
			.BeginSub("spawncapplayerscaling")
			.WithAlias("scps")
			.WithDesc("Get or set spawn cap player scaling. The lower the value, the less additional mobs are spawned for each additional online player")
			.WithArgs(parsers.OptionalFloat("Scaling value"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("SpawnCapPlayerScaling", args))
			.EndSub()
			.BeginSub("setspawnhere")
			.WithDesc("Set the default spawn point to the callers location")
			.HandleWith(handleSetSpawnhere)
			.EndSub()
			.BeginSub("setspawn")
			.WithDesc("Get or Set the default spawn point to given xz or xyz coordinates")
			.WithArgs(parsers.OptionalInt("x position"), parsers.OptionalInt("z position (optional)"), parsers.OptionalInt("z position"))
			.HandleWith(handleSetSpawn)
			.EndSub()
			.BeginSub("welcome")
			.WithDesc("Get or set welcome message")
			.WithArgs(parsers.OptionalAll("Welcome message"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("WelcomeMessage", args))
			.EndSub()
			.BeginSub("name")
			.WithDesc("Get or set server name")
			.WithArgs(parsers.OptionalAll("Server name"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("ServerName", args))
			.EndSub()
			.BeginSub("description")
			.WithDesc("Get or set server description")
			.WithArgs(parsers.OptionalAll("Server description"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("ServerDescription", args))
			.EndSub()
			.BeginSub("url")
			.WithDesc("Get or set server url")
			.WithArgs(parsers.OptionalAll("Server url"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("ServerUrl", args))
			.EndSub()
			.BeginSub("maxchunkradius")
			.WithDesc("Get or set the maximum view distance in chunks the server will load for players")
			.WithArgs(parsers.OptionalInt("Radius in chunks"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("MaxChunkRadius", args))
			.EndSub()
			.BeginSub("maxclients")
			.WithDesc("Get or set the maximum amount players that can join the server")
			.WithArgs(parsers.OptionalInt("Max amount of clients"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("MaxClients", args))
			.EndSub()
			.BeginSub("maxclientsinqueue")
			.WithDesc("Get or set the maximum amount players that can wait in the server join queue")
			.WithArgs(parsers.OptionalInt("Max amount of clients in queue"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("MaxClientsInQueue", args))
			.EndSub()
			.BeginSub("passtimewhenempty")
			.WithDesc("Get or toggle the passing of time when the server empty")
			.WithArgs(parsers.OptionalBool("Empty server time passing mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("PassTimeWhenEmpty", args))
			.EndSub()
			.BeginSub("upnp")
			.WithDesc("Enable/Disable Upnp discovery")
			.WithArgs(parsers.OptionalBool("Upnp Mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("Upnp", args))
			.EndSub()
			.BeginSub("advertise")
			.WithDesc("Whether to list your server on the public server listing")
			.WithArgs(parsers.OptionalBool("Advertise mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("AdvertiseServer", args))
			.EndSub()
			.BeginSub("allowpvp")
			.WithDesc("Whether to allow Player versus Player combat")
			.WithArgs(parsers.OptionalBool("PvP Mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("AllowPvP", args))
			.EndSub()
			.BeginSub("allowfirespread")
			.WithDesc("Whether to allow the spreading of fire")
			.WithArgs(parsers.OptionalBool("Fire spread mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("AllowFireSpread", args))
			.EndSub()
			.BeginSub("allowfallingblocks")
			.WithAlias("fallingblocks")
			.WithDesc("Whether to allow falling block physics")
			.WithArgs(parsers.OptionalBool("Falling block physics mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("AllowFallingBlocks", args))
			.EndSub()
			.BeginSub("whitelistmode")
			.WithDesc("Whether to only allow whitelisted players to join your server")
			.WithArgs(parsers.OptionalWordRange("Whitelist mode", "on", "off", "default"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("WhitelistMode", args))
			.EndSub()
			.BeginSub("entityspawning")
			.WithDesc("Whether to spawn new creatures and monsters over time")
			.WithArgs(parsers.OptionalBool("Entity spawning mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("EntitySpawning", args))
			.EndSub()
			.BeginSub("entitydebugmode")
			.WithDesc("Whether to enable entity debug mode")
			.WithArgs(parsers.OptionalBool("Entity debug mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("EntityDebugMode", args))
			.EndSub()
			.BeginSub("password")
			.WithDesc("Sets a password when connecting to the server. Cannot use spaces. Use /serverconfig nopassword to clear.")
			.WithArgs(parsers.Word("password"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("Password", args))
			.EndSub()
			.BeginSub("tickrate")
			.WithDesc("How often the server should tick per second.")
			.WithArgs(parsers.OptionalFloat("tick interval in ms"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				if (server.Config.HostedMode && args.Caller.Type != EnumCallerType.Console)
				{
					return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode"));
				}
				if (args.Parsers[0].IsMissing)
				{
					return TextCommandResult.Success("The current tick rate is at " + 1000f / server.Config.TickTime + " tp/s");
				}
				float num3 = (float)args[0];
				server.Config.TickTime = 1000f / num3;
				server.ConfigNeedsSaving = true;
				return TextCommandResult.Success(Lang.Get("Ok, tick rate now at {0} tp/s", num3), EnumChatType.CommandSuccess);
			})
			.EndSub()
			.BeginSub("autosaveintervall")
			.WithDesc("How often the server save to disk while it is running.")
			.WithArgs(parsers.OptionalIntRange("save interval in seconds", 30, 3600))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				if (server.Config.HostedMode && args.Caller.Type != EnumCallerType.Console)
				{
					return TextCommandResult.Error(Lang.Get("Can't access this feature, server is in hosted mode"));
				}
				if (args.Parsers[0].IsMissing)
				{
					return TextCommandResult.Success("Autosave interval is at " + MagicNum.ServerAutoSave + " seconds");
				}
				int num = (int)args[0];
				int num2 = GameMath.Max(num, 30);
				MagicNum.ServerAutoSave = num2;
				MagicNum.Save();
				return TextCommandResult.Success(Lang.Get("Ok, autosave interval now at {0} s", num2));
			})
			.EndSub()
			.BeginSub("blockTicksPerChunk")
			.WithAlias("btpc")
			.WithAlias("blockticks")
			.WithDesc("How often blocks around the player should randomly tick to update their state")
			.WithArgs(parsers.OptionalInt("block tick rate"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("RandomBlockTicksPerChunk", args))
			.EndSub()
			.BeginSub("welcomemessage")
			.WithAlias("motd")
			.WithDesc("Set a message to be shown in chat when a player joins.")
			.WithArgs(parsers.Word("message"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("WelcomeMessage", args))
			.EndSub()
			.BeginSub("loginfloodprotection")
			.WithDesc("Enable or disable the ip based login flood protection")
			.WithArgs(parsers.OptionalBool("LoginFloodProtection mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("LoginFloodProtection", args))
			.EndSub()
			.BeginSub("temporaryipblocklist")
			.WithDesc("Enable or disable the ip based block list")
			.WithArgs(parsers.OptionalBool("TemporaryIpBlockList mode"))
			.HandleWith((TextCommandCallingArgs args) => cmdServerConfig.getOrSet("TemporaryIpBlockList", args))
			.EndSub();
	}

	private TextCommandResult getOrSet(string name, TextCommandCallingArgs args)
	{
		if (name == "simrange")
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.Get("Current DefaultSimulationRange is {0}", GlobalConstants.DefaultSimulationRange));
			}
			GlobalConstants.DefaultSimulationRange = (int)args[0];
			return TextCommandResult.Success(Lang.Get("DefaultSimulationRange set to {0}", GlobalConstants.DefaultSimulationRange));
		}
		if (name == "MaxClients" && server.progArgs.MaxClients != null)
		{
			return TextCommandResult.Success("Current Max Clients is overridden by command line supplied max clients with a value of  " + server.progArgs.MaxClients);
		}
		if (name == "WhitelistMode")
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.Get("Current WhitelistMode is {0}", Config.WhitelistMode));
			}
			switch (((string)args[0]).ToLowerInvariant())
			{
			case "off":
				Config.WhitelistMode = EnumWhitelistMode.Off;
				break;
			case "on":
				Config.WhitelistMode = EnumWhitelistMode.On;
				break;
			case "default":
				Config.WhitelistMode = EnumWhitelistMode.Default;
				break;
			}
			ConfigNeedsSaving = true;
			return TextCommandResult.Success(Lang.Get("WhitelistMode set to {0}", Config.WhitelistMode));
		}
		if (server.Config.HostedMode && args.Caller.Type != EnumCallerType.Console && unavailableInHostedMode.Contains(name))
		{
			return TextCommandResult.Error(Lang.Get("Can't access this feature, this server is in hosted mode"));
		}
		if (args.Parsers[0].IsMissing)
		{
			object value = ((!(name == "EntitySpawning")) ? server.Config.Get(name) : ((object)server.SaveGameData.EntitySpawning));
			if (value is bool)
			{
				value = (((bool)value) ? "on" : "off");
			}
			return TextCommandResult.Success(Lang.Get("Current {0} is {1}", args.Parsers[0].ArgumentName, value));
		}
		object nowvalue = args[0];
		if (nowvalue is bool)
		{
			nowvalue = (((bool)nowvalue) ? "on" : "off");
		}
		if (name == "EntitySpawning")
		{
			server.SaveGameData.EntitySpawning = (bool)args[0];
		}
		else
		{
			Config.Set(name, args[0]);
		}
		ConfigNeedsSaving = true;
		ServerMain.Logger.Audit(Lang.Get("{0} changes server config {1} to {2}.", args.Caller.GetName(), name, nowvalue));
		return TextCommandResult.Success(Lang.Get("{0} set to {1}", args.Parsers[0].ArgumentName, nowvalue));
	}

	private TextCommandResult handleSetSpawn(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success((server.SaveGameData.DefaultSpawn == null) ? Lang.Get("Default Spawnpoint is not set.") : Lang.Get("Default spawnpoint is at ={0} ={1} ={2}", server.SaveGameData.DefaultSpawn.x, server.SaveGameData.DefaultSpawn.y, server.SaveGameData.DefaultSpawn.z));
		}
		int x = (int)args[0];
		int z;
		int y;
		if (args.Parsers[2].IsMissing)
		{
			z = (int)args[1];
			y = server.WorldMap.GetTerrainGenSurfacePosY(x, z);
		}
		else
		{
			y = (int)args[1];
			z = (int)args[2];
		}
		if (!server.WorldMap.IsValidPos(x, y, z))
		{
			return TextCommandResult.Error(Lang.Get("Invalid coordinates - beyond world bounds"));
		}
		server.SaveGameData.DefaultSpawn = new PlayerSpawnPos
		{
			x = x,
			y = y,
			z = z
		};
		ConfigNeedsSaving = true;
		return TextCommandResult.Success(Lang.Get("Default spawnpoint now set to ={0} ={1} ={2}", x, y, z));
	}

	private TextCommandResult handleSetSpawnhere(TextCommandCallingArgs args)
	{
		BlockPos plrPos = args.Caller.Pos.AsBlockPos;
		if (!server.WorldMap.IsValidPos(plrPos.X, plrPos.Y, plrPos.Z))
		{
			return TextCommandResult.Error(Lang.Get("Invalid coordinates (probably beyond world bounds)"));
		}
		server.SaveGameData.DefaultSpawn = new PlayerSpawnPos
		{
			x = plrPos.X,
			y = plrPos.Y,
			z = plrPos.Z
		};
		return TextCommandResult.Success(Lang.Get("Default spawnpoint now set to ={0} ={1} ={2}", server.SaveGameData.DefaultSpawn.x, server.SaveGameData.DefaultSpawn.y, server.SaveGameData.DefaultSpawn.z));
	}
}
