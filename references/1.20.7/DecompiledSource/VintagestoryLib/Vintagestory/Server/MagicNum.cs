using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Config;

namespace Vintagestory.Server;

public class MagicNum
{
	public static string FileName;

	[JsonProperty(Order = 1)]
	public const string Comment = "You can use this config to increase/lower the cpu and network load of the server. A Warning though: Changing these numbers might make your server run unstable or slow. Use at your own risk! Until there is an official documentation, feel free to ask in the forums what the numbers do.";

	[JsonProperty(Order = 2)]
	public static int DefaultEntityTrackingRange;

	[JsonProperty(Order = 3)]
	public static int DefaultSimulationRange;

	public static int ServerChunkSize;

	public static int ServerChunkSizeMask;

	[JsonProperty(Order = 5)]
	public static int RequestChunkColumnsQueueSize;

	[JsonProperty(Order = 6)]
	public static int ReadyChunksQueueSize;

	[JsonProperty(Order = 7)]
	public static int ChunksColumnsToRequestPerTick;

	[JsonProperty(Order = 8)]
	public static int ChunksToSendPerTick;

	[JsonProperty(Order = 9)]
	public static int ChunkRequestTickTime;

	[JsonProperty(Order = 10)]
	public static int ChunkColumnsToGeneratePerThreadTick;

	[JsonProperty(Order = 11)]
	public static long ServerAutoSave;

	[JsonProperty(Order = 12)]
	public static int SpawnChunksWidth;

	[JsonProperty(Order = 15)]
	public static int TrackedEntitiesPerClient;

	public static int ChunkRegionSizeInChunks;

	[JsonProperty(Order = 17)]
	public static int CalendarPacketSecondInterval;

	[JsonProperty(Order = 18)]
	public static int ChunkUnloadInterval;

	[JsonProperty(Order = 19)]
	public static int UncompressedChunkTTL;

	[JsonProperty(Order = 20)]
	public static long CompressedChunkTTL;

	public static int MapRegionSize;

	[JsonProperty(Order = 21)]
	public static double PlayerDesyncTolerance;

	[JsonProperty(Order = 22)]
	public static double PlayerDesyncMaxIntervalls;

	[JsonProperty(Order = 23)]
	public static int ChunkThreadTickTime;

	[JsonProperty(Order = 24)]
	public static int AntiAbuseMaxWalkBlocksPer200ms;

	[JsonProperty(Order = 25)]
	public static int AntiAbuseMaxFlySuspicions;

	[JsonProperty(Order = 26)]
	public static int AntiAbuseMaxTeleSuspicions;

	[JsonProperty(Order = 27)]
	public static int MaxPhysicsThreads;

	[JsonProperty(Order = 28)]
	public static int MaxWorldgenThreads;

	[JsonProperty(Order = 29)]
	public static int MaxEntitySpawnsPerTick;

	[JsonProperty(Order = 30)]
	public static string ServerMagicNumVersion;

	static MagicNum()
	{
		FileName = Path.Combine(GamePaths.Config, "servermagicnumbers.json");
		DefaultEntityTrackingRange = 4;
		DefaultSimulationRange = 128;
		ServerChunkSize = 32;
		ServerChunkSizeMask = 31;
		RequestChunkColumnsQueueSize = 2000;
		ReadyChunksQueueSize = 200;
		ChunksColumnsToRequestPerTick = 4;
		ChunksToSendPerTick = 192;
		ChunkRequestTickTime = 20;
		ChunkColumnsToGeneratePerThreadTick = 30;
		ServerAutoSave = 300L;
		SpawnChunksWidth = 7;
		TrackedEntitiesPerClient = 3000;
		ChunkRegionSizeInChunks = 16;
		CalendarPacketSecondInterval = 60;
		ChunkUnloadInterval = 4000;
		UncompressedChunkTTL = 15000;
		CompressedChunkTTL = 45000L;
		MapRegionSize = ChunkRegionSizeInChunks * ServerChunkSize;
		PlayerDesyncTolerance = 0.02;
		PlayerDesyncMaxIntervalls = 20.0;
		ChunkThreadTickTime = 5;
		AntiAbuseMaxWalkBlocksPer200ms = 3;
		AntiAbuseMaxFlySuspicions = 3;
		AntiAbuseMaxTeleSuspicions = 8;
		MaxPhysicsThreads = 1;
		MaxWorldgenThreads = 1;
		MaxEntitySpawnsPerTick = 8;
		ServerMagicNumVersion = null;
		Load();
	}

	public static void Save()
	{
		ServerMagicNumVersion = "1.3";
		using TextWriter textWriter = new StreamWriter(FileName);
		textWriter.Write(JsonConvert.SerializeObject(new MagicNum(), Formatting.Indented));
		textWriter.Close();
	}

	public static void Load()
	{
		bool shouldSave = false;
		if (File.Exists(FileName))
		{
			using TextReader textReader = new StreamReader(FileName);
			JsonConvert.DeserializeObject<MagicNum>(textReader.ReadToEnd());
			textReader.Close();
		}
		else
		{
			shouldSave = true;
		}
		if (ServerAutoSave > 0 && ServerAutoSave < 15)
		{
			ServerAutoSave = 15L;
		}
		if (ServerMagicNumVersion == null || GameVersion.IsLowerVersionThan(ServerMagicNumVersion, "1.1"))
		{
			if (ChunkColumnsToGeneratePerThreadTick == 7)
			{
				ChunkColumnsToGeneratePerThreadTick = 30;
			}
			if (ChunksColumnsToRequestPerTick == 1)
			{
				ChunksColumnsToRequestPerTick = 4;
			}
			if (ChunksToSendPerTick == 32)
			{
				ChunksToSendPerTick = 192;
			}
			shouldSave = true;
		}
		if (ServerMagicNumVersion == null || GameVersion.IsLowerVersionThan(ServerMagicNumVersion, "1.2"))
		{
			if (ChunkColumnsToGeneratePerThreadTick == 30 && RuntimeEnv.OS == OS.Linux)
			{
				ChunkColumnsToGeneratePerThreadTick = 15;
			}
			if (ChunkRequestTickTime == 40)
			{
				ChunkRequestTickTime = 10;
			}
			if (ChunkThreadTickTime == 10)
			{
				ChunkThreadTickTime = 5;
			}
			shouldSave = true;
		}
		if (ServerMagicNumVersion == null || GameVersion.IsLowerVersionThan(ServerMagicNumVersion, "1.3"))
		{
			shouldSave = true;
		}
		if (shouldSave)
		{
			Save();
		}
		GlobalConstants.DefaultSimulationRange = DefaultSimulationRange;
	}
}
