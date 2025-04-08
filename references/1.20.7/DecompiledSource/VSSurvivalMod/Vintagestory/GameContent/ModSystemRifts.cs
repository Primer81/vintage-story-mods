using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemRifts : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private RiftRenderer renderer;

	public Dictionary<int, Rift> riftsById = new Dictionary<int, Rift>();

	public ILoadedSound[] riftSounds = new ILoadedSound[4];

	public Rift[] nearestRifts = new Rift[0];

	public IServerNetworkChannel schannel;

	public int despawnDistance = 240;

	public int spawnMinDistance = 8;

	public int spawnAddDistance = 230;

	private bool riftsEnabled = true;

	private Dictionary<string, long> chunkIndexbyPlayer = new Dictionary<string, long>();

	private ModSystemRiftWeather modRiftWeather;

	private string riftMode;

	private int riftId;

	public int NextRiftId => riftId++;

	public event OnTrySpawnRiftDelegate OnTrySpawnRift;

	public event OnRiftSpawnedDelegate OnRiftSpawned;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		api.Network.RegisterChannel("rifts").RegisterMessageType<RiftList>();
		modRiftWeather = api.ModLoader.GetModSystem<ModSystemRiftWeather>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		renderer = new RiftRenderer(api, riftsById);
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.RegisterGameTickListener(onClientTick, 100);
		api.Network.GetChannel("rifts").SetMessageHandler<RiftList>(onRifts);
	}

	private void onRifts(RiftList riftlist)
	{
		HashSet<int> toRemove = new HashSet<int>();
		toRemove.AddRange(riftsById.Keys);
		foreach (Rift rift in riftlist.rifts)
		{
			toRemove.Remove(rift.RiftId);
			if (riftsById.TryGetValue(rift.RiftId, out var existingRift))
			{
				existingRift.SetFrom(rift);
			}
			else
			{
				riftsById[rift.RiftId] = rift;
			}
		}
		foreach (int id in toRemove)
		{
			riftsById.Remove(id);
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.RegisterGameTickListener(OnServerTick100ms, 101);
		api.Event.RegisterGameTickListener(OnServerTick3s, 2999);
		setupCommands();
		schannel = sapi.Network.GetChannel("rifts");
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		if (riftsEnabled)
		{
			BroadCastRifts(byPlayer);
		}
	}

	private void OnServerTick100ms(float dt)
	{
		if (riftMode != "visible")
		{
			return;
		}
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer plr = (IServerPlayer)allOnlinePlayers[i];
			if (plr.ConnectionState != EnumClientState.Playing)
			{
				continue;
			}
			EntityBehaviorTemporalStabilityAffected bh = plr.Entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
			if (bh == null)
			{
				continue;
			}
			bh.stabilityOffset = 0.0;
			Vec3d plrPos = plr.Entity.Pos.XYZ;
			Rift rift = riftsById.Values.Where((Rift r) => r.Size > 0f).Nearest((Rift r) => r.Position.SquareDistanceTo(plrPos));
			if (rift != null)
			{
				float dist = Math.Max(0f, GameMath.Sqrt(plrPos.SquareDistanceTo(rift.Position)) - 2f - rift.Size / 2f);
				if (bh != null)
				{
					bh.stabilityOffset = (0.0 - Math.Pow(Math.Max(0f, 1f - dist / 3f), 2.0)) * 20.0;
				}
			}
		}
	}

	private void OnServerTick3s(float dt)
	{
		if (!riftsEnabled)
		{
			return;
		}
		IPlayer[] players = sapi.World.AllOnlinePlayers;
		Dictionary<string, List<Rift>> nearbyRiftsByPlayerUid = new Dictionary<string, List<Rift>>();
		IPlayer[] array = players;
		foreach (IPlayer player2 in array)
		{
			nearbyRiftsByPlayerUid[player2.PlayerUID] = new List<Rift>();
		}
		if (KillOldRifts(nearbyRiftsByPlayerUid) | SpawnNewRifts(nearbyRiftsByPlayerUid))
		{
			BroadCastRifts();
			return;
		}
		array = players;
		foreach (IPlayer player in array)
		{
			long index3d = getPlayerChunkIndex(player);
			if (!chunkIndexbyPlayer.ContainsKey(player.PlayerUID) || chunkIndexbyPlayer[player.PlayerUID] != index3d)
			{
				BroadCastRifts(player);
			}
			chunkIndexbyPlayer[player.PlayerUID] = index3d;
		}
	}

	private bool SpawnNewRifts(Dictionary<string, List<Rift>> nearbyRiftsByPlayerUid)
	{
		Dictionary<string, List<Rift>>.KeyCollection uids = nearbyRiftsByPlayerUid.Keys;
		int riftsSpawned = 0;
		foreach (string uid in uids)
		{
			float cap = GetRiftCap(uid);
			IPlayer plr = api.World.PlayerByUid(uid);
			if (plr.WorldData.CurrentGameMode == EnumGameMode.Creative || plr.WorldData.CurrentGameMode == EnumGameMode.Spectator)
			{
				continue;
			}
			int nearbyRifts = nearbyRiftsByPlayerUid[uid].Count;
			int canSpawnCount = (int)(cap - (float)nearbyRifts);
			float fract = cap - (float)nearbyRifts - (float)canSpawnCount;
			if (api.World.Rand.NextDouble() < (double)fract / 50.0)
			{
				canSpawnCount++;
			}
			if (canSpawnCount <= 0 || (api.World.Calendar.TotalDays < 2.0 && api.World.Calendar.GetDayLightStrength(plr.Entity.Pos.AsBlockPos) > 0.9f))
			{
				continue;
			}
			for (int i = 0; i < canSpawnCount; i++)
			{
				double num = (double)spawnMinDistance + api.World.Rand.NextDouble() * (double)spawnAddDistance;
				double angle = api.World.Rand.NextDouble() * 6.2831854820251465;
				double dz = num * Math.Sin(angle);
				double dx = num * Math.Cos(angle);
				Vec3d riftPos = plr.Entity.Pos.XYZ.Add(dx, 0.0, dz);
				BlockPos pos = new BlockPos((int)riftPos.X, 0, (int)riftPos.Z);
				pos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(pos);
				if (!canSpawnRiftAt(pos))
				{
					continue;
				}
				float size = 2f + (float)api.World.Rand.NextDouble() * 4f;
				riftPos.Y = (float)pos.Y + size / 2f + 1f;
				Rift rift = new Rift
				{
					RiftId = NextRiftId,
					Position = riftPos,
					Size = size,
					SpawnedTotalHours = api.World.Calendar.TotalHours,
					DieAtTotalHours = api.World.Calendar.TotalHours + 8.0 + api.World.Rand.NextDouble() * 48.0
				};
				this.OnRiftSpawned?.Invoke(rift);
				riftsById[rift.RiftId] = rift;
				riftsSpawned++;
				foreach (string item in uids)
				{
					_ = item;
					if (plr.Entity.Pos.HorDistanceTo(riftPos) <= (double)despawnDistance)
					{
						nearbyRiftsByPlayerUid[plr.PlayerUID].Add(rift);
					}
				}
			}
		}
		return riftsSpawned > 0;
	}

	private bool canSpawnRiftAt(BlockPos pos)
	{
		if (api.World.BlockAccessor.GetBlock(pos).Replaceable < 6000)
		{
			pos.Up();
		}
		if (api.World.BlockAccessor.GetBlock(pos, 2).IsLiquid() && api.World.Rand.NextDouble() > 0.1)
		{
			return false;
		}
		int lightLevel = api.World.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlyBlockLight);
		int blocklightup = api.World.BlockAccessor.GetLightLevel(pos.UpCopy(), EnumLightLevelType.OnlyBlockLight);
		int blocklightup2 = api.World.BlockAccessor.GetLightLevel(pos.UpCopy(2), EnumLightLevelType.OnlyBlockLight);
		if (lightLevel >= 3 || blocklightup >= 3 || blocklightup2 >= 3)
		{
			return false;
		}
		bool executeDefault = true;
		if (this.OnTrySpawnRift != null)
		{
			Delegate[] invocationList = this.OnTrySpawnRift.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				OnTrySpawnRiftDelegate obj = (OnTrySpawnRiftDelegate)invocationList[i];
				EnumHandling handling = EnumHandling.PassThrough;
				obj(pos, ref handling);
				if (handling != EnumHandling.PreventSubsequent && handling == EnumHandling.PreventDefault)
				{
					executeDefault = false;
				}
			}
		}
		return executeDefault;
	}

	private float GetRiftCap(string playerUid)
	{
		EntityPos pos = api.World.PlayerByUid(playerUid).Entity.Pos;
		float daylight = api.World.Calendar.GetDayLightStrength(pos.X, pos.Z);
		return 5f * modRiftWeather.CurrentPattern.MobSpawnMul * GameMath.Clamp(1.1f - daylight, 0.35f, 1f);
	}

	private bool KillOldRifts(Dictionary<string, List<Rift>> nearbyRiftsByPlayerUid)
	{
		bool riftModified = false;
		double totalHours = api.World.Calendar.TotalHours;
		IPlayer[] players = sapi.World.AllOnlinePlayers;
		HashSet<int> toRemove = new HashSet<int>();
		foreach (Rift rift2 in riftsById.Values)
		{
			if (rift2.DieAtTotalHours <= totalHours)
			{
				toRemove.Add(rift2.RiftId);
				riftModified = true;
				continue;
			}
			List<IPlayer> nearbyPlrs = players.InRange((IPlayer player) => player.Entity.Pos.HorDistanceTo(rift2.Position), despawnDistance);
			if (nearbyPlrs.Count == 0)
			{
				rift2.DieAtTotalHours = Math.Min(rift2.DieAtTotalHours, api.World.Calendar.TotalHours + 0.2);
				riftModified = true;
				continue;
			}
			foreach (IPlayer plr in nearbyPlrs)
			{
				nearbyRiftsByPlayerUid[plr.PlayerUID].Add(rift2);
			}
		}
		foreach (int id in toRemove)
		{
			riftsById.Remove(id);
		}
		foreach (KeyValuePair<string, List<Rift>> val in nearbyRiftsByPlayerUid)
		{
			float cap = GetRiftCap(val.Key);
			float overSpawn = (float)val.Value.Count - cap;
			if (!(overSpawn <= 0f))
			{
				_ = api.World.PlayerByUid(val.Key).Entity.Pos.XYZ;
				List<Rift> rifts = val.Value.OrderBy((Rift rift) => rift.DieAtTotalHours).ToList();
				for (int i = 0; i < Math.Min(rifts.Count, (int)overSpawn); i++)
				{
					rifts[i].DieAtTotalHours = Math.Min(rifts[i].DieAtTotalHours, api.World.Calendar.TotalHours + 0.2);
					riftModified = true;
				}
			}
		}
		return riftModified;
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("rifts", riftsById);
	}

	private void Event_SaveGameLoaded()
	{
		riftMode = api.World.Config.GetString("temporalRifts", "visible");
		riftsEnabled = riftMode != "off";
		if (riftsEnabled)
		{
			try
			{
				riftsById = sapi.WorldManager.SaveGame.GetData<Dictionary<int, Rift>>("rifts");
			}
			catch (Exception)
			{
			}
			if (riftsById == null)
			{
				riftsById = new Dictionary<int, Rift>();
			}
		}
	}

	public void BroadCastRifts(IPlayer onlyToPlayer = null)
	{
		if (riftMode != "visible")
		{
			return;
		}
		List<Rift> plrLists = new List<Rift>();
		float minDistSq = (float)Math.Pow(despawnDistance + 10, 2.0);
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		foreach (IPlayer plr in allOnlinePlayers)
		{
			if (onlyToPlayer != null && onlyToPlayer.PlayerUID != plr.PlayerUID)
			{
				continue;
			}
			chunkIndexbyPlayer[plr.PlayerUID] = getPlayerChunkIndex(plr);
			IServerPlayer splr = plr as IServerPlayer;
			Vec3d plrPos = splr.Entity.Pos.XYZ;
			foreach (Rift rift in riftsById.Values)
			{
				if (rift.Position.SquareDistanceTo(plrPos) < minDistSq)
				{
					plrLists.Add(rift);
				}
			}
			schannel.SendPacket(new RiftList
			{
				rifts = plrLists
			}, splr);
			plrLists.Clear();
		}
	}

	private long getPlayerChunkIndex(IPlayer plr)
	{
		EntityPos pos = plr.Entity.Pos;
		return (api as ICoreServerAPI).WorldManager.ChunkIndex3D((int)pos.X / 32, (int)pos.Y / 32, (int)pos.Z / 32);
	}

	private void onClientTick(float dt)
	{
		if (!riftsEnabled)
		{
			return;
		}
		Vec3d plrPos = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
		nearestRifts = (from r in riftsById.Values
			where r.Size > 0f
			select r into rift
			orderby rift.Position.SquareDistanceTo(plrPos) + (float)((!rift.HasLineOfSight) ? 400 : 0)
			select rift).ToArray();
		for (int j = 0; j < Math.Min(4, nearestRifts.Length); j++)
		{
			Rift rift2 = nearestRifts[j];
			rift2.OnNearTick(capi, dt);
			ILoadedSound sound = riftSounds[j];
			if (!sound.IsPlaying)
			{
				sound.Start();
				sound.PlaybackPosition = sound.SoundLengthSeconds * (float)capi.World.Rand.NextDouble();
			}
			float vol = GameMath.Clamp(rift2.GetNowSize(capi) / 3f, 0.1f, 1f);
			sound.SetVolume(vol * rift2.VolumeMul);
			sound.SetPosition((float)rift2.Position.X, (float)rift2.Position.Y, (float)rift2.Position.Z);
		}
		for (int i = nearestRifts.Length; i < 4; i++)
		{
			if (riftSounds[i].IsPlaying)
			{
				riftSounds[i].Stop();
			}
		}
	}

	private void Event_LeaveWorld()
	{
		for (int i = 0; i < 4; i++)
		{
			riftSounds[i]?.Stop();
			riftSounds[i]?.Dispose();
		}
	}

	private void Event_BlockTexturesLoaded()
	{
		for (int i = 0; i < 4; i++)
		{
			riftSounds[i] = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/rift.ogg"),
				ShouldLoop = true,
				Position = null,
				DisposeOnFinish = false,
				Volume = 1f,
				Range = 24f,
				SoundType = EnumSoundType.AmbientGlitchunaffected
			});
		}
	}

	private void setupCommands()
	{
		sapi.ChatCommands.GetOrCreate("debug").BeginSub("rift").WithDesc("Rift debug commands")
			.WithAdditionalInformation("With no sub-command, simply counts the number of loaded rifts")
			.HandleWith((TextCommandCallingArgs args) => TextCommandResult.Success(riftsById.Count + " rifts loaded"))
			.BeginSub("clear")
			.WithDesc("Immediately remove all loaded rifts")
			.HandleWith(delegate
			{
				riftsById.Clear();
				BroadCastRifts();
				return TextCommandResult.Success();
			})
			.EndSub()
			.BeginSub("fade")
			.WithDesc("Slowly remove all loaded rifts, over a few minutes")
			.HandleWith(delegate
			{
				foreach (Rift value in riftsById.Values)
				{
					value.DieAtTotalHours = Math.Min(value.DieAtTotalHours, api.World.Calendar.TotalHours + 0.2);
				}
				BroadCastRifts();
				return TextCommandResult.Success();
			})
			.EndSub()
			.BeginSub("spawn")
			.WithDesc("Spawn the specified quantity of rifts")
			.WithArgs(sapi.ChatCommands.Parsers.OptionalInt("quantity", 200))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				int num = (int)args[0];
				for (int i = 0; i < num; i++)
				{
					double num2 = (double)spawnMinDistance + api.World.Rand.NextDouble() * (double)spawnAddDistance;
					double num3 = api.World.Rand.NextDouble() * 6.2831854820251465;
					double z = num2 * Math.Sin(num3);
					double x = num2 * Math.Cos(num3);
					Vec3d vec3d = args.Caller.Pos.AddCopy(x, 0.0, z);
					BlockPos blockPos = new BlockPos((int)vec3d.X, 0, (int)vec3d.Z);
					blockPos.Y = api.World.BlockAccessor.GetTerrainMapheightAt(blockPos);
					if (!api.World.BlockAccessor.GetBlock(blockPos, 2).IsLiquid() || !(api.World.Rand.NextDouble() > 0.1))
					{
						float num4 = 2f + (float)api.World.Rand.NextDouble() * 4f;
						vec3d.Y = (float)blockPos.Y + num4 / 2f + 1f;
						Rift rift2 = new Rift
						{
							RiftId = NextRiftId,
							Position = vec3d,
							Size = num4,
							SpawnedTotalHours = api.World.Calendar.TotalHours,
							DieAtTotalHours = api.World.Calendar.TotalHours + 8.0 + api.World.Rand.NextDouble() * 48.0
						};
						this.OnRiftSpawned?.Invoke(rift2);
						riftsById[rift2.RiftId] = rift2;
					}
				}
				BroadCastRifts();
				return TextCommandResult.Success("ok, " + num + " spawned.");
			})
			.EndSub()
			.BeginSub("spawnhere")
			.WithDesc("Spawn one rift")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				Vec3d position = args.Caller.Pos.AddCopy(args.Caller.Entity?.LocalEyePos ?? new Vec3d());
				float size = 3f;
				Rift rift = new Rift
				{
					RiftId = NextRiftId,
					Position = position,
					Size = size,
					SpawnedTotalHours = api.World.Calendar.TotalHours,
					DieAtTotalHours = api.World.Calendar.TotalHours + 8.0 + api.World.Rand.NextDouble() * 48.0
				};
				this.OnRiftSpawned?.Invoke(rift);
				riftsById[rift.RiftId] = rift;
				BroadCastRifts();
				return TextCommandResult.Success("ok, rift spawned.");
			})
			.EndSub()
			.EndSub();
	}
}
