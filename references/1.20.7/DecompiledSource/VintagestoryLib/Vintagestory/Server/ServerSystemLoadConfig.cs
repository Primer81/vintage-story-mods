using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerSystemLoadConfig : ServerSystem
{
	public ServerSystemLoadConfig(ServerMain server)
		: base(server)
	{
		server.EventManager.OnSaveGameLoaded += OnSaveGameLoaded;
	}

	public override int GetUpdateInterval()
	{
		return 100;
	}

	public override void OnServerTick(float dt)
	{
		if (server.ConfigNeedsSaving)
		{
			server.ConfigNeedsSaving = false;
			SaveConfig(server);
		}
	}

	public override void OnBeginConfiguration()
	{
		EnsureConfigExists(server);
		LoadConfig(server);
		if (server.Standalone)
		{
			server.Config.ApplyStartServerArgs(server.Config.WorldConfig);
		}
		else
		{
			server.Config.ApplyStartServerArgs(server.serverStartArgs);
		}
		if (server.Config.Roles == null || server.Config.Roles.Count == 0)
		{
			server.Config.InitializeRoles();
		}
		if (server.Config.LoadedConfigVersion == "1.0")
		{
			server.Config.InitializeRoles();
			SaveConfig(server);
		}
	}

	public static void EnsureConfigExists(ServerMain server)
	{
		string filename = "serverconfig.json";
		if (!File.Exists(Path.Combine(GamePaths.Config, filename)))
		{
			ServerMain.Logger.Notification("serverconfig.json not found, creating new one");
			GenerateConfig(server);
			SaveConfig(server);
		}
	}

	private void OnSaveGameLoaded()
	{
		ServerConfig config = server.Config;
		ServerWorldMap worldmap = server.WorldMap;
		server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(worldmap.ChunkMapSizeX / 2, worldmap.ChunkMapSizeZ / 2));
		PlayerSpawnPos plrSpawn = server.SaveGameData.DefaultSpawn;
		if (plrSpawn != null)
		{
			server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(plrSpawn.x / 32, plrSpawn.z / 32));
		}
		foreach (PlayerRole role in config.RolesByCode.Values)
		{
			if (role.DefaultSpawn != null)
			{
				server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(role.DefaultSpawn.x / 32, role.DefaultSpawn.z / 32));
			}
			if (role.ForcedSpawn != null)
			{
				server.AddChunkColumnToForceLoadedList(worldmap.MapChunkIndex2D(role.ForcedSpawn.x / 32, role.ForcedSpawn.z / 32));
			}
		}
	}

	public override void OnBeginRunGame()
	{
		base.OnBeginRunGame();
		if (server.Config.StartupCommands != null)
		{
			ServerMain.Logger.Notification("Running startup commands");
			string[] array = server.Config.StartupCommands.Split(new string[1] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in array)
			{
				server.ReceiveServerConsole(line);
			}
		}
	}

	public static void GenerateConfig(ServerMain server)
	{
		server.Config = new ServerConfig();
		server.Config.InitializeRoles();
		if (server.Standalone)
		{
			server.Config.ApplyStartServerArgs(server.Config.WorldConfig);
		}
		else
		{
			server.Config.ApplyStartServerArgs(server.serverStartArgs);
		}
	}

	public static void LoadConfig(ServerMain server)
	{
		string filename = "serverconfig.json";
		try
		{
			using TextReader textReader2 = new StreamReader(Path.Combine(GamePaths.Config, filename));
			server.Config = JsonConvert.DeserializeObject<ServerConfig>(textReader2.ReadToEnd());
			textReader2.Close();
		}
		catch (JsonReaderException e)
		{
			ServerMain.Logger.Error("Failed to read serverconfig.json");
			ServerMain.Logger.Error(e);
			ServerMain.Logger.StoryEvent("Failed to read serverconfig.json. Did you modify it? See server-main.log for the affected line. Will stop the server.");
			server.Config = new ServerConfig();
			server.Stop("serverconfig.json read error");
			return;
		}
		if (server.Config == null)
		{
			ServerMain.Logger?.Notification("The deserialized serverconfig.json was null? Creating new one.");
			server.Config = new ServerConfig();
			server.Config.InitializeRoles();
			SaveConfig(server);
		}
		if (server.progArgs.WithConfig != null)
		{
			JObject fileConfig;
			using (TextReader textReader = new StreamReader(Path.Combine(GamePaths.Config, filename)))
			{
				fileConfig = JToken.Parse(textReader.ReadToEnd()) as JObject;
				textReader.Close();
			}
			JObject runtimeConfig = JToken.Parse(server.progArgs.WithConfig) as JObject;
			fileConfig.Merge(runtimeConfig);
			server.Config = fileConfig.ToObject<ServerConfig>();
			SaveConfig(server);
		}
		Logger.LogFileSplitAfterLine = server.Config.LogFileSplitAfterLine;
	}

	public static void SaveConfig(ServerMain server)
	{
		if (server.Standalone)
		{
			server.Config.FileEditWarning = "";
		}
		else
		{
			server.Config.FileEditWarning = "PLEASE NOTE: This file is also loaded when you start a single player world. If you want to run a dedicated server without affecting single player, we recommend you install the game into a different folder and run the server from there.";
		}
		StreamWriter streamWriter = new StreamWriter(Path.Combine(GamePaths.Config, "serverconfig.json"));
		streamWriter.Write(JsonConvert.SerializeObject(server.Config, Formatting.Indented));
		streamWriter.Close();
	}
}
