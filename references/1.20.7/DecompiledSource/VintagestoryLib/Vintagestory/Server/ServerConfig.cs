using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server;

[JsonObject(MemberSerialization.OptIn)]
public class ServerConfig : IServerConfig
{
	public string LoadedConfigVersion;

	private bool upnp;

	private bool advertiseServer;

	public bool RuntimeUpnp;

	private bool temporaryIpBlockList;

	public int? Seed;

	internal PlayerRole DefaultRole;

	public Dictionary<string, PlayerRole> RolesByCode = new Dictionary<string, PlayerRole>();

	internal HashSet<string> RuntimePrivileveCodes = new HashSet<string>();

	[JsonProperty]
	public string FileEditWarning { get; set; }

	[JsonProperty]
	public string ConfigVersion { get; set; }

	[JsonProperty]
	public string ServerName { get; set; }

	[JsonProperty]
	public string ServerUrl { get; set; }

	[JsonProperty]
	public string ServerDescription { get; set; }

	[JsonProperty]
	public string WelcomeMessage { get; set; }

	[JsonProperty]
	public string Ip { get; set; }

	[JsonProperty]
	public int Port { get; set; }

	[JsonProperty]
	public bool Upnp
	{
		get
		{
			return upnp;
		}
		set
		{
			upnp = value;
			this.onUpnpChanged?.Invoke();
		}
	}

	[JsonProperty]
	public bool CompressPackets { get; set; }

	[JsonProperty]
	public bool AdvertiseServer
	{
		get
		{
			return advertiseServer;
		}
		set
		{
			advertiseServer = value;
			this.onAdvertiseChanged?.Invoke();
		}
	}

	[JsonProperty]
	public int MaxClients { get; set; }

	[JsonProperty]
	public int MaxClientsInQueue { get; set; }

	[JsonProperty]
	public bool PassTimeWhenEmpty { get; set; }

	[JsonProperty]
	public string MasterserverUrl { get; set; }

	[JsonProperty]
	public string ModDbUrl { get; set; }

	[JsonProperty]
	public int ClientConnectionTimeout { get; set; }

	[JsonProperty]
	public bool EntityDebugMode { get; set; }

	[JsonProperty]
	public string Password { get; set; }

	[JsonProperty]
	public int MapSizeX { get; set; }

	[JsonProperty]
	public int MapSizeY { get; set; }

	[JsonProperty]
	public int MapSizeZ { get; set; }

	[JsonProperty]
	public string ServerLanguage { get; set; }

	[JsonProperty]
	public int MaxChunkRadius { get; set; }

	[JsonProperty]
	public float TickTime { get; set; }

	[JsonProperty]
	public float SpawnCapPlayerScaling { get; set; }

	[JsonProperty]
	public int BlockTickChunkRange { get; set; }

	[JsonProperty]
	public int MaxMainThreadBlockTicks { get; set; }

	[JsonProperty]
	public int RandomBlockTicksPerChunk { get; set; }

	[JsonProperty]
	public int BlockTickInterval { get; set; }

	[JsonProperty]
	public int SkipEveryChunkRow { get; set; }

	[JsonProperty]
	public int SkipEveryChunkRowWidth { get; set; }

	[JsonProperty]
	public List<PlayerRole> Roles { get; set; }

	[JsonProperty]
	public string DefaultRoleCode { get; set; }

	[JsonProperty]
	public string[] ModPaths { get; set; }

	[JsonProperty]
	public EnumProtectionLevel AntiAbuse { get; set; }

	[JsonProperty]
	public StartServerArgs WorldConfig { get; set; }

	[JsonProperty]
	public int NextPlayerGroupUid { get; set; }

	[JsonProperty]
	public int GroupChatHistorySize { get; set; }

	[JsonProperty]
	public int MaxOwnedGroupChannelsPerUser { get; set; }

	[JsonProperty]
	[Obsolete("No longer used. Use WhitelistMode instead")]
	private bool OnlyWhitelisted { get; set; }

	[JsonProperty]
	public EnumWhitelistMode WhitelistMode { get; set; }

	[JsonProperty]
	public bool VerifyPlayerAuth { get; set; }

	[JsonProperty]
	[Obsolete("No longer used. Retrieve value from the savegame instead")]
	public PlayerSpawnPos DefaultSpawn { get; set; }

	[JsonProperty]
	public bool AllowPvP { get; set; }

	[JsonProperty]
	public bool AllowFireSpread { get; set; }

	[JsonProperty]
	public bool AllowFallingBlocks { get; set; }

	[JsonProperty]
	public bool HostedMode { get; set; }

	[JsonProperty]
	public bool HostedModeAllowMods { get; set; }

	[JsonProperty]
	public string StartupCommands { get; set; }

	[JsonProperty]
	public bool RepairMode { get; set; }

	[JsonProperty]
	public bool AnalyzeMode { get; set; }

	[JsonProperty]
	public bool CorruptionProtection { get; set; }

	[JsonProperty]
	public bool RegenerateCorruptChunks { get; set; }

	List<IPlayerRole> IServerConfig.Roles => ((IEnumerable<PlayerRole>)Roles).Select((System.Func<PlayerRole, IPlayerRole>)((PlayerRole e) => e)).ToList();

	[JsonProperty]
	public int ChatRateLimitMs { get; set; }

	[JsonProperty]
	public int DieBelowDiskSpaceMb { get; set; }

	[JsonProperty]
	public string[] ModIdBlackList { get; set; }

	[JsonProperty]
	public string[] ModIdWhiteList { get; set; }

	[JsonProperty]
	public string ServerIdentifier { get; set; }

	[JsonProperty]
	public bool LogBlockBreakPlace { get; set; }

	[JsonProperty]
	public uint LogFileSplitAfterLine { get; set; }

	[JsonProperty]
	public int DieAboveErrorCount { get; set; }

	[JsonProperty]
	public bool LoginFloodProtection { get; set; }

	[JsonProperty]
	public bool TemporaryIpBlockList
	{
		get
		{
			return temporaryIpBlockList;
		}
		set
		{
			temporaryIpBlockList = value;
			TcpNetConnection.TemporaryIpBlockList = value;
		}
	}

	[JsonProperty]
	public int DieAboveMemoryUsageMb { get; set; }

	public event Action onUpnpChanged;

	public event Action onAdvertiseChanged;

	public bool IsPasswordProtected()
	{
		return !string.IsNullOrEmpty(Password);
	}

	public int GetMaxClients(ServerMain server)
	{
		if (server.progArgs.MaxClients != null && int.TryParse(server.progArgs.MaxClients, out var maxClients))
		{
			return maxClients;
		}
		return MaxClients;
	}

	public ServerConfig()
	{
		ConfigVersion = "1.7";
		ServerName = "Vintage Story Server";
		WelcomeMessage = Lang.GetUnformatted("survive-and-star-trek");
		DefaultRoleCode = "suplayer";
		MasterserverUrl = "http://masterserver.vintagestory.at/api/v1/servers/";
		ModDbUrl = "https://mods.vintagestory.at/";
		VerifyPlayerAuth = true;
		AdvertiseServer = false;
		CorruptionProtection = true;
		Port = 42420;
		MaxClients = 16;
		ClientConnectionTimeout = 150;
		MapSizeX = 1024000;
		MapSizeY = 256;
		MapSizeZ = 1024000;
		Seed = null;
		SpawnCapPlayerScaling = 0.5f;
		ServerLanguage = "en";
		MaxChunkRadius = 12;
		SkipEveryChunkRow = 0;
		SkipEveryChunkRowWidth = 0;
		ModPaths = new string[2]
		{
			"Mods",
			GamePaths.DataPathMods
		};
		AntiAbuse = EnumProtectionLevel.Off;
		NextPlayerGroupUid = 10;
		GroupChatHistorySize = 20;
		MaxOwnedGroupChannelsPerUser = 10;
		WhitelistMode = EnumWhitelistMode.Default;
		CompressPackets = true;
		TickTime = 33.333332f;
		AllowPvP = true;
		AllowFireSpread = true;
		AllowFallingBlocks = true;
		HostedMode = false;
		HostedModeAllowMods = false;
		Upnp = false;
		BlockTickChunkRange = 5;
		RandomBlockTicksPerChunk = 16;
		BlockTickInterval = 300;
		MaxMainThreadBlockTicks = 10000;
		ChatRateLimitMs = 1000;
		DieBelowDiskSpaceMb = 400;
		PassTimeWhenEmpty = false;
		LogBlockBreakPlace = false;
		DieAboveErrorCount = 100000;
		DieAboveMemoryUsageMb = 50000;
		LogFileSplitAfterLine = 500000u;
		LoginFloodProtection = false;
		TemporaryIpBlockList = false;
		WorldConfig = new StartServerArgs
		{
			SaveFileLocation = Path.Combine(GamePaths.Saves, GamePaths.DefaultSaveFilenameWithoutExtension + ".vcdbs"),
			WorldName = "A new world",
			AllowCreativeMode = true,
			PlayStyle = "surviveandbuild",
			PlayStyleLangCode = "surviveandbuild-bands",
			WorldType = "standard"
		};
	}

	public object Get(string propertyName)
	{
		PropertyInfo[] properties = typeof(ServerConfig).GetProperties();
		foreach (PropertyInfo prop in properties)
		{
			if (prop.Name == propertyName)
			{
				return prop.GetValue(this, null);
			}
		}
		throw new ArgumentException("No such property exists");
	}

	public void Set(string propertyName, object value)
	{
		PropertyInfo[] properties = typeof(ServerConfig).GetProperties();
		foreach (PropertyInfo prop in properties)
		{
			if (prop.Name == propertyName)
			{
				prop.SetValue(this, value);
				return;
			}
		}
		throw new ArgumentException("No such property exists");
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Roles != null && Roles.Count > 0)
		{
			RolesByCode.Clear();
			foreach (PlayerRole group in Roles)
			{
				RolesByCode[group.Code] = group;
				if (group.AutoGrant)
				{
					if (group.Privileges != null)
					{
						group.Privileges = group.Privileges.Union(Privilege.AllCodes()).ToList();
					}
					else
					{
						group.Privileges = Privilege.AllCodes().ToList();
					}
				}
			}
		}
		if (ConfigVersion == "1.0" && RolesByCode.Count == 0)
		{
			InitializeRoles();
		}
		if (GameVersion.IsLowerVersionThan(ConfigVersion, "1.1.0"))
		{
			WelcomeMessage = Lang.GetUnformatted("survive-and-star-trek");
		}
		if (GameVersion.IsLowerVersionThan(ConfigVersion, "1.3"))
		{
			foreach (KeyValuePair<string, PlayerRole> val in RolesByCode)
			{
				if (!val.Value.AutoGrant)
				{
					val.Value.Privileges.Add(Privilege.attackcreatures);
					val.Value.Privileges.Add(Privilege.attackplayers);
				}
			}
		}
		if (GameVersion.IsLowerVersionThan(ConfigVersion, "1.4"))
		{
			CorruptionProtection = true;
		}
		if (GameVersion.IsLowerVersionThan(ConfigVersion, "1.5"))
		{
			RolesByCode["limitedsuplayer"].GrantPrivilege(Privilege.selfkill);
			RolesByCode["limitedcrplayer"].GrantPrivilege(Privilege.selfkill);
			RolesByCode["suplayer"].GrantPrivilege(Privilege.selfkill);
			RolesByCode["crplayer"].GrantPrivilege(Privilege.selfkill);
			RolesByCode["sumod"].GrantPrivilege(Privilege.selfkill);
			RolesByCode["crmod"].GrantPrivilege(Privilege.selfkill);
		}
		if (GameVersion.IsLowerVersionThan(ConfigVersion, "1.6"))
		{
			WhitelistMode = (OnlyWhitelisted ? EnumWhitelistMode.On : EnumWhitelistMode.Default);
		}
		if (GameVersion.IsLowerVersionThan(ConfigVersion, "1.7"))
		{
			ServerSystemLoadAndSaveGame.SetDefaultSpawnOnce = DefaultSpawn;
		}
		if (!RolesByCode.ContainsKey(DefaultRoleCode))
		{
			ServerMain.Logger.Fatal("You have configured a default group code " + DefaultRoleCode + " but no such group exists! Killing server");
			Environment.Exit(0);
			return;
		}
		if (ServerIdentifier == null)
		{
			ServerIdentifier = Guid.NewGuid().ToString();
		}
		DefaultRole = RolesByCode[DefaultRoleCode];
		LoadedConfigVersion = ConfigVersion;
		ConfigVersion = "1.7";
	}

	public void ApplyStartServerArgs(StartServerArgs serverargs)
	{
		if (serverargs == null)
		{
			return;
		}
		WorldConfig = serverargs.Clone();
		if (serverargs.Language != null)
		{
			ServerLanguage = serverargs.Language;
		}
		if (WorldConfig.Seed != null && WorldConfig.Seed.Length > 0)
		{
			if (int.TryParse(WorldConfig.Seed, out var seed))
			{
				Seed = seed;
			}
			else
			{
				Seed = GameMath.DotNetStringHash(WorldConfig.Seed);
			}
			ServerMain.Logger.Notification("Using world seed: {0}", Seed);
		}
		if (serverargs.MapSizeY.HasValue)
		{
			MapSizeY = serverargs.MapSizeY.Value;
		}
		RepairMode = serverargs.RepairMode;
		if (RepairMode)
		{
			AnalyzeMode = true;
		}
	}

	public void InitializeRoles()
	{
		Roles = new List<PlayerRole>();
		Roles.Add(new PlayerRole
		{
			Code = "suvisitor",
			Name = "Survival Visitor",
			Description = "Can only visit this world and chat but not use/place/break anything",
			PrivilegeLevel = -1,
			Color = Color.Green,
			DefaultGameMode = EnumGameMode.Survival,
			Privileges = new List<string>(new string[1] { Privilege.chat })
		});
		Roles.Add(new PlayerRole
		{
			Code = "crvisitor",
			Name = "Creative Visitor",
			Description = "Can only visit this world, chat and fly but not use/place/break anything",
			PrivilegeLevel = -1,
			Color = Color.DarkGray,
			DefaultGameMode = EnumGameMode.Creative,
			Privileges = new List<string>(new string[1] { Privilege.chat })
		});
		Roles.Add(new PlayerRole
		{
			Code = "limitedsuplayer",
			Name = "Limited Survival Player",
			Description = "Can use/place/break blocks only in permitted areas (priv level -1), create/manage player groups and chat",
			PrivilegeLevel = -1,
			Color = Color.White,
			DefaultGameMode = EnumGameMode.Survival,
			Privileges = new List<string>(new string[8]
			{
				Privilege.controlplayergroups,
				Privilege.manageplayergroups,
				Privilege.chat,
				Privilege.buildblocks,
				Privilege.useblock,
				Privilege.attackcreatures,
				Privilege.attackplayers,
				Privilege.selfkill
			})
		});
		Roles.Add(new PlayerRole
		{
			Code = "limitedcrplayer",
			Name = "Limited Creative Player",
			Description = "Can use/place/break blocks in only in permitted areas (priv level -1), create/manage player groups, chat, fly and set his own game mode (= allows fly and change of move speed)",
			PrivilegeLevel = -1,
			Color = Color.LightGreen,
			DefaultGameMode = EnumGameMode.Creative,
			Privileges = new List<string>(new string[10]
			{
				Privilege.controlplayergroups,
				Privilege.manageplayergroups,
				Privilege.chat,
				Privilege.buildblocks,
				Privilege.useblock,
				Privilege.gamemode,
				Privilege.freemove,
				Privilege.attackcreatures,
				Privilege.attackplayers,
				Privilege.selfkill
			})
		});
		Roles.Add(new PlayerRole
		{
			Code = "suplayer",
			Name = "Survival Player",
			Description = "Can use/place/break blocks in unprotected areas (priv level 0), create/manage player groups and chat. Can claim an area of up to 8 chunks.",
			PrivilegeLevel = 0,
			LandClaimAllowance = 262144,
			LandClaimMaxAreas = 3,
			Color = Color.White,
			DefaultGameMode = EnumGameMode.Survival,
			Privileges = new List<string>(new string[9]
			{
				Privilege.controlplayergroups,
				Privilege.manageplayergroups,
				Privilege.chat,
				Privilege.claimland,
				Privilege.buildblocks,
				Privilege.useblock,
				Privilege.attackcreatures,
				Privilege.attackplayers,
				Privilege.selfkill
			})
		});
		Roles.Add(new PlayerRole
		{
			Code = "crplayer",
			Name = "Creative Player",
			Description = "Can use/place/break blocks in all areas (priv level 100), create/manage player groups, chat, fly and set his own game mode (= allows fly and change of move speed). Can claim an area of up to 40 chunks.",
			PrivilegeLevel = 100,
			LandClaimAllowance = 1310720,
			LandClaimMaxAreas = 6,
			Color = Color.LightGreen,
			DefaultGameMode = EnumGameMode.Creative,
			Privileges = new List<string>(new string[11]
			{
				Privilege.controlplayergroups,
				Privilege.manageplayergroups,
				Privilege.chat,
				Privilege.claimland,
				Privilege.buildblocks,
				Privilege.useblock,
				Privilege.gamemode,
				Privilege.freemove,
				Privilege.attackcreatures,
				Privilege.attackplayers,
				Privilege.selfkill
			})
		});
		Roles.Add(new PlayerRole
		{
			Code = "sumod",
			Name = "Survival Moderator",
			Description = "Can use/place/break blocks everywhere (priv level 200), create/manage player groups, chat, kick/ban players and do serverwide announcements. Can claim an area of up to 4 chunks.",
			PrivilegeLevel = 200,
			LandClaimAllowance = 1310720,
			LandClaimMaxAreas = 60,
			Color = Color.Cyan,
			DefaultGameMode = EnumGameMode.Survival,
			Privileges = new List<string>(new string[15]
			{
				Privilege.controlplayergroups,
				Privilege.manageplayergroups,
				Privilege.chat,
				Privilege.claimland,
				Privilege.buildblocks,
				Privilege.useblock,
				Privilege.buildblockseverywhere,
				Privilege.useblockseverywhere,
				Privilege.kick,
				Privilege.ban,
				Privilege.announce,
				Privilege.readlists,
				Privilege.attackcreatures,
				Privilege.attackplayers,
				Privilege.selfkill
			})
		});
		Roles.Add(new PlayerRole
		{
			Code = "crmod",
			Name = "Creative Moderator",
			Description = "Can use/place/break blocks everywhere (priv level 500), create/manage player groups, chat, kick/ban players, fly and set his own or other players game modes (= allows fly and change of move speed). Can claim an area of up to 40 chunks.",
			LandClaimAllowance = 1310720,
			LandClaimMaxAreas = 60,
			PrivilegeLevel = 500,
			Color = Color.Cyan,
			DefaultGameMode = EnumGameMode.Creative,
			Privileges = new List<string>(new string[18]
			{
				Privilege.controlplayergroups,
				Privilege.manageplayergroups,
				Privilege.chat,
				Privilege.claimland,
				Privilege.buildblocks,
				Privilege.useblock,
				Privilege.buildblockseverywhere,
				Privilege.useblockseverywhere,
				Privilege.kick,
				Privilege.ban,
				Privilege.gamemode,
				Privilege.freemove,
				Privilege.commandplayer,
				Privilege.announce,
				Privilege.readlists,
				Privilege.attackcreatures,
				Privilege.attackplayers,
				Privilege.selfkill
			})
		});
		Roles.Add(new PlayerRole
		{
			Code = "admin",
			Name = "Admin",
			Description = "Has all privileges, including giving other players admin status.",
			LandClaimAllowance = int.MaxValue,
			LandClaimMaxAreas = 99999,
			PrivilegeLevel = 99999,
			Color = Color.LightBlue,
			DefaultGameMode = EnumGameMode.Survival,
			AutoGrant = true
		});
		Roles.Sort();
		foreach (PlayerRole group2 in Roles)
		{
			if (group2.AutoGrant)
			{
				if (group2.Privileges != null)
				{
					group2.Privileges = group2.Privileges.Union(Privilege.AllCodes()).ToList();
				}
				else
				{
					group2.Privileges = Privilege.AllCodes().ToList();
				}
			}
		}
		foreach (PlayerRole group in Roles)
		{
			RolesByCode[group.Code] = group;
		}
		DefaultRole = RolesByCode[DefaultRoleCode];
	}
}
