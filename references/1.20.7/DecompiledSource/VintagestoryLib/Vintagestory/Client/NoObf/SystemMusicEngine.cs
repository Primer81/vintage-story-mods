using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemMusicEngine : ClientSystem, IMusicEngine
{
	public ConcurrentQueue<TrackToLoad> TracksToLoad = new ConcurrentQueue<TrackToLoad>();

	private Thread trackLoader;

	private readonly CancellationToken _token;

	private IMusicTrack currentlyCheckedTrack;

	private ConcurrentDictionary<IMusicTrack, long> tracksOnCooldown = new ConcurrentDictionary<IMusicTrack, long>();

	private MusicConfig config;

	private IMusicTrack currentTrack;

	private IMusicTrack lastTrack;

	private IMusicTrack[] shuffledTracks;

	private long msSinceLastTrack;

	private bool debugSimulation;

	private Thread initialisingThread;

	private Random rand = new Random();

	private double totalHoursStop;

	private long listenerId;

	private OrderedDictionary<string, int> trackPlayCount = new OrderedDictionary<string, int>();

	public override string Name => "mus";

	public bool CurrentActive
	{
		get
		{
			if (currentTrack != null)
			{
				return currentTrack.IsActive;
			}
			return false;
		}
	}

	public IMusicTrack CurrentTrack => currentTrack;

	public IMusicTrack LastPlayedTrack => lastTrack;

	public long MillisecondsSinceLastTrack => msSinceLastTrack;

	public SystemMusicEngine(ClientMain game, CancellationToken token)
		: base(game)
	{
		_token = token;
		game.eventManager.TrackStarter = StartTrack;
		game.eventManager.TrackStarterLoaded = StartTrack;
		game.eventManager.CurrentTrackSupplier = () => CurrentTrack;
		game.RegisterGameTickListener(OnEverySecond, 1000);
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("music").WithDescription("Show current playing music track")
			.HandleWith(OnCmdCurrentTrack)
			.BeginSubCommand("sim")
			.WithDescription("Simulate music playing for x days")
			.WithArgs(game.api.ChatCommands.Parsers.OptionalInt("days", 5))
			.HandleWith(OnCmdSim)
			.EndSubCommand()
			.BeginSubCommand("simstop")
			.WithDescription("Stop music simulation")
			.HandleWith(OnCmdSimstop)
			.EndSubCommand()
			.BeginSubCommand("stop")
			.WithDescription("Stop current music track")
			.HandleWith(OnCmdStop)
			.EndSubCommand()
			.EndSubCommand();
	}

	internal void Initialise_SeparateThread()
	{
		initialisingThread = new Thread(new ThreadMusicInitialise(this, game).Process);
		initialisingThread.IsBackground = true;
		initialisingThread.Start();
	}

	internal void EarlyInitialise()
	{
		ClientSettings.Inst.AddWatcher<int>("musicLevel", OnMusicLevelChanged);
		Queue<IMusicTrack> tracks = new Queue<IMusicTrack>();
		foreach (IAsset musicConfig in game.Platform.AssetManager.GetManyInCategory("music", "musicconfig.json"))
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			};
			settings.Converters.Add(new AssetLocationJsonParser(musicConfig.Location.Domain));
			config = musicConfig.ToObject<MusicConfig>(settings);
			if (config.Tracks != null)
			{
				IMusicTrack[] tracks2 = config.Tracks;
				foreach (IMusicTrack track in tracks2)
				{
					track.Initialize(game.Platform.AssetManager, game.api, this);
					tracks.Enqueue(track);
				}
			}
		}
		if (config == null)
		{
			config = new MusicConfig();
		}
		shuffledTracks = tracks.ToArray();
		config.Tracks = tracks.ToArray();
		trackLoader = new Thread(ProcessTrackQueue);
		trackLoader.IsBackground = true;
		trackLoader.Start();
	}

	public override void OnBlockTexturesLoaded()
	{
		while (initialisingThread.IsAlive)
		{
			Thread.Sleep(10);
		}
		game.Logger.Notification("Initialized Music Engine");
	}

	public MusicTrack StartTrack(AssetLocation soundLocation, float priority, EnumSoundType soundType, Action<ILoadedSound> onLoaded = null)
	{
		if (CurrentTrack != null && CurrentTrack.Priority > priority)
		{
			return null;
		}
		CurrentTrack?.FadeOut(2f);
		MusicTrack track = new MusicTrack
		{
			Location = soundLocation,
			Priority = priority
		};
		track.Initialize(game.AssetManager, game.api, this);
		track.loading = true;
		currentTrack = track;
		TracksToLoad.Enqueue(new TrackToLoad
		{
			ByTrack = track,
			SoundType = soundType,
			Location = track.Location,
			OnLoaded = delegate(ILoadedSound sound)
			{
				if (onLoaded == null)
				{
					sound.Start();
				}
				else
				{
					onLoaded(sound);
				}
				if (!track.loading)
				{
					sound?.Stop();
					if (!track.ManualDispose)
					{
						sound?.Dispose();
					}
				}
				else
				{
					track.Sound = sound;
				}
				track.loading = false;
			}
		});
		return track;
	}

	public void StartTrack(MusicTrack track, float priority, EnumSoundType soundType, bool playNow = true)
	{
		if ((CurrentTrack == null || !(CurrentTrack.Priority > priority)) && track != currentTrack)
		{
			CurrentTrack?.FadeOut(2f);
			currentTrack = track;
			if (playNow)
			{
				track.Sound.Start();
			}
		}
	}

	private void ProcessTrackQueue()
	{
		try
		{
			while (!_token.IsCancellationRequested)
			{
				TrackToLoad trackToLoad = null;
				if (TracksToLoad.TryDequeue(out trackToLoad))
				{
					IAsset music = game.Platform.AssetManager.TryGet(trackToLoad.Location);
					if (music != null)
					{
						(ScreenManager.LoadMusicTrack(music) as AudioMetaData).AutoUnload = true;
						game.EnqueueMainThreadTask(delegate
						{
							ILoadedSound obj = game.LoadSound(new SoundParams
							{
								Location = trackToLoad.Location,
								SoundType = trackToLoad.SoundType,
								Volume = trackToLoad.volume,
								Pitch = trackToLoad.pitch,
								DisposeOnFinish = false
							});
							trackToLoad.OnLoaded(obj);
						}, "loadtrack");
					}
					else
					{
						game.Logger.Warning("Music File not found: {0}", trackToLoad.Location);
						game.EnqueueMainThreadTask(delegate
						{
							trackToLoad.OnLoaded(null);
						}, "loadtrack");
					}
				}
				if (!debugSimulation)
				{
					Thread.Sleep(75);
					continue;
				}
				break;
			}
		}
		catch (TaskCanceledException)
		{
		}
	}

	private TextCommandResult OnCmdSimstop(TextCommandCallingArgs args)
	{
		totalHoursStop = 0.0;
		return TextCommandResult.Success("Ok, sim stopped");
	}

	private TextCommandResult OnCmdStop(TextCommandCallingArgs args)
	{
		currentTrack?.FadeOut(1f);
		return TextCommandResult.Success("Ok, track stopped");
	}

	private TextCommandResult OnCmdSim(TextCommandCallingArgs args)
	{
		int days = (int)args[0];
		if (ClientSettings.MusicLevel > 0)
		{
			return TextCommandResult.Error("Set music level to 0 first");
		}
		debugSimulation = true;
		while (trackLoader.IsAlive)
		{
			Thread.Sleep(100);
		}
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.All
		};
		game.Platform.AssetManager.Reload(AssetCategory.music);
		config = game.Platform.AssetManager.TryGet("music/musicconfig.json")?.ToObject<MusicConfig>(settings);
		if (config == null)
		{
			config = new MusicConfig();
		}
		if (config.Tracks != null)
		{
			shuffledTracks = (IMusicTrack[])config.Tracks.Clone();
			IMusicTrack[] tracks = config.Tracks;
			for (int i = 0; i < tracks.Length; i++)
			{
				tracks[i].Initialize(game.Platform.AssetManager, game.api, this);
			}
		}
		game.ignoreServerCalendarUpdates = true;
		totalHoursStop = game.GameWorldCalendar.TotalHours + (double)((float)days * game.GameWorldCalendar.HoursPerDay);
		trackPlayCount.Clear();
		listenerId = game.RegisterGameTickListener(DebugSimTick, 20);
		return TextCommandResult.Success("Ok, sim started");
	}

	private TextCommandResult OnCmdCurrentTrack(TextCommandCallingArgs args)
	{
		IMusicTrack musicTrack = currentTrack;
		if (musicTrack != null && musicTrack.IsActive)
		{
			return TextCommandResult.Success("Currently playing: " + currentTrack.Name);
		}
		return TextCommandResult.Success((!TracksToLoad.IsEmpty) ? "Loading track(s)... " : "Searching for fitting track... ");
	}

	private void OnEverySecond(float dt)
	{
		if ((ScreenManager.IntroMusic != null && !ScreenManager.IntroMusic.HasStopped) || ClientSettings.MusicLevel <= 0)
		{
			return;
		}
		IMusicTrack musicTrack = currentTrack;
		if (musicTrack != null && !musicTrack.ContinuePlay(dt, game.playerProperties))
		{
			lastTrack = currentTrack;
			currentTrack = null;
			msSinceLastTrack = game.ElapsedMilliseconds;
		}
		if (shuffledTracks == null)
		{
			return;
		}
		GameMath.Shuffle(rand, shuffledTracks);
		IMusicTrack[] array = shuffledTracks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BeginSort();
		}
		BlockPos pos = game.player.Entity.Pos.AsBlockPos;
		ClimateCondition conds = game.BlockAccessor.GetClimateAt(pos);
		foreach (IMusicTrack item in shuffledTracks.OrderBy((IMusicTrack t) => t.StartPriority).Reverse())
		{
			IMusicTrack track = (currentlyCheckedTrack = item);
			if (CurrentActive && currentTrack != track)
			{
				tracksOnCooldown[track] = game.ElapsedMilliseconds + 8000;
				if (track.Priority <= currentTrack.Priority)
				{
					break;
				}
			}
			if (currentTrack != track && track.ShouldPlay(game.playerProperties, conds, pos))
			{
				if (currentTrack != null)
				{
					game.Logger.Notification("Current track {0} got replaced by a higher priority one ({1}). Fading out.", currentTrack.Name, track.Name);
					currentTrack.FadeOut(5f);
					msSinceLastTrack = game.ElapsedMilliseconds;
				}
				game.Logger.Notification("Track {0} now started", track.Name);
				currentTrack = track;
				currentTrack.BeginPlay(game.playerProperties);
				break;
			}
		}
	}

	public void LoadTrack(AssetLocation location, Action<ILoadedSound> onLoaded, float volume = 1f, float pitch = 1f)
	{
		TracksToLoad.Enqueue(new TrackToLoad
		{
			ByTrack = currentlyCheckedTrack,
			Location = location,
			volume = volume,
			pitch = pitch,
			OnLoaded = onLoaded
		});
		if (debugSimulation)
		{
			ProcessTrackQueue();
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Audio;
	}

	private void OnMusicLevelChanged(int newValue)
	{
		if (currentTrack != null)
		{
			currentTrack.UpdateVolume();
		}
	}

	public void StopTrack(IMusicTrack musicTrack)
	{
		if (CurrentTrack == musicTrack)
		{
			currentTrack = null;
		}
	}

	private void DebugSimTick(float dt)
	{
		float irlsecondsper5minutes = 10f;
		double totalHours = game.GameWorldCalendar.TotalHours;
		totalHours += 0.0833333358168602;
		BlockPos pos = game.player.Entity.Pos.AsBlockPos;
		ClimateCondition conds = game.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDateValues, totalHours / (double)game.GameWorldCalendar.HoursPerDay);
		if (totalHours > totalHoursStop)
		{
			game.UnregisterGameTickListener(listenerId);
			debugSimulation = false;
			game.ignoreServerCalendarUpdates = false;
			game.Logger.Notification("Simulation executed. Results");
			{
				foreach (KeyValuePair<string, int> val in trackPlayCount)
				{
					game.Logger.Notification("{0}: {1}", val.Key, val.Value);
				}
				return;
			}
		}
		game.GameWorldCalendar.Add(1f / 12f);
		game.GameWorldCalendar.Tick();
		IMusicTrack musicTrack = currentTrack;
		if (musicTrack != null && !musicTrack.ContinuePlay(dt, game.playerProperties))
		{
			lastTrack = currentTrack;
			currentTrack = null;
			msSinceLastTrack = game.ElapsedMilliseconds;
		}
		if (currentTrack != null && currentTrack.IsActive)
		{
			currentTrack.FastForward(irlsecondsper5minutes);
			game.Logger.Notification("{0}", currentTrack.PositionString);
		}
		else
		{
			SurfaceMusicTrack.globalCooldownUntilMs -= (int)irlsecondsper5minutes * 1000;
			foreach (string item in new List<string>(SurfaceMusicTrack.tracksCooldownUntilMs.Keys))
			{
				SurfaceMusicTrack.tracksCooldownUntilMs[item] -= (int)irlsecondsper5minutes * 1000;
			}
		}
		GameMath.Shuffle(rand, shuffledTracks);
		IMusicTrack[] array = shuffledTracks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BeginSort();
		}
		shuffledTracks.OrderBy((IMusicTrack t) => t.StartPriority).Reverse();
		foreach (IMusicTrack item2 in shuffledTracks.OrderBy((IMusicTrack t) => t.Priority).Reverse())
		{
			IMusicTrack track = (currentlyCheckedTrack = item2);
			if (CurrentActive && currentTrack != track)
			{
				tracksOnCooldown[track] = game.ElapsedMilliseconds + 8000;
				if (track.Priority <= currentTrack.Priority)
				{
					break;
				}
			}
			if (currentTrack != track && track.ShouldPlay(game.playerProperties, conds, pos))
			{
				if (currentTrack != null && currentTrack.IsActive)
				{
					game.Logger.Notification("Current track {0} got replaced by a higher priority one ({1}). Fading out.", currentTrack.Name, track.Name);
					currentTrack.FadeOut(5f);
					msSinceLastTrack = game.ElapsedMilliseconds;
				}
				game.Logger.Notification("Track {0} now started", track.Name);
				currentTrack = track;
				currentTrack.BeginPlay(game.playerProperties);
				if (trackPlayCount.ContainsKey(track.Name))
				{
					trackPlayCount[track.Name]++;
				}
				else
				{
					trackPlayCount[track.Name] = 1;
				}
				break;
			}
		}
	}
}
