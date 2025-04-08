using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

[ProtoContract]
public class ServerMapChunk : IServerMapChunk, IMapChunk
{
	[ProtoMember(1)]
	public Dictionary<string, byte[]> Moddata;

	[ProtoMember(3)]
	public ushort[] RainHeightMap;

	[ProtoMember(4)]
	internal int currentpass;

	[ProtoMember(6)]
	public ushort[] TopRockIdMapOld;

	[ProtoMember(13)]
	public int[] TopRockIdMap;

	[ProtoMember(7)]
	public ushort[] WorldGenTerrainHeightMap;

	[ProtoMember(8)]
	public List<BlockPos> ScheduledBlockUpdates = new List<BlockPos>();

	[ProtoMember(9)]
	public HashSet<BlockPos> NewBlockEntities = new HashSet<BlockPos>();

	[ProtoMember(10)]
	public ushort YMax;

	[ProtoMember(16)]
	public int WorldGenVersion;

	[ProtoMember(17)]
	public List<Vec4i> ScheduledBlockLightUpdates;

	public int QuantityNeighboursLoaded;

	public ServerMapRegion MapRegion;

	public bool DirtyForSaving;

	private object modDataLock = new object();

	public SmallBoolArray NeighboursLoaded;

	public byte UnloadGeneration = 3;

	public EnumWorldGenPass CurrentIncompletePass
	{
		get
		{
			return (EnumWorldGenPass)currentpass;
		}
		set
		{
			DirtyForSaving = true;
			currentpass = (int)value;
		}
	}

	[ProtoMember(11)]
	public byte[] CaveHeightDistort { get; set; }

	[ProtoMember(12)]
	public ushort[] SedimentaryThicknessMap { get; set; }

	[ProtoMember(15)]
	public ConcurrentDictionary<Vec2i, float> SnowAccum { get; set; }

	EnumWorldGenPass IMapChunk.CurrentPass
	{
		get
		{
			return CurrentIncompletePass;
		}
		set
		{
			CurrentIncompletePass = value;
		}
	}

	ushort IMapChunk.YMax
	{
		get
		{
			return YMax;
		}
		set
		{
			YMax = value;
		}
	}

	public bool SelfLoaded
	{
		get
		{
			return NeighboursLoaded[8];
		}
		set
		{
			NeighboursLoaded[8] = value;
		}
	}

	ushort[] IMapChunk.RainHeightMap => RainHeightMap;

	ushort[] IMapChunk.WorldGenTerrainHeightMap => WorldGenTerrainHeightMap;

	IMapRegion IMapChunk.MapRegion => MapRegion;

	int[] IMapChunk.TopRockIdMap => TopRockIdMap;

	public static ServerMapChunk CreateNew(ServerMapRegion mapRegion)
	{
		return new ServerMapChunk
		{
			MapRegion = mapRegion,
			Moddata = new Dictionary<string, byte[]>(),
			RainHeightMap = new ushort[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
			WorldGenTerrainHeightMap = new ushort[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
			TopRockIdMap = new int[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
			TopRockIdMapOld = null,
			SedimentaryThicknessMap = new ushort[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
			CaveHeightDistort = new byte[MagicNum.ServerChunkSize * MagicNum.ServerChunkSize],
			DirtyForSaving = true,
			CurrentIncompletePass = EnumWorldGenPass.Terrain,
			SnowAccum = new ConcurrentDictionary<Vec2i, float>()
		};
	}

	public static ServerMapChunk FromBytes(byte[] serializedChunk)
	{
		ServerMapChunk mapchunk = Serializer.Deserialize<ServerMapChunk>(new MemoryStream(serializedChunk));
		if (mapchunk.WorldGenTerrainHeightMap == null)
		{
			mapchunk.WorldGenTerrainHeightMap = (ushort[])mapchunk.RainHeightMap.Clone();
		}
		if (mapchunk.TopRockIdMapOld != null)
		{
			if (mapchunk.TopRockIdMap == null)
			{
				mapchunk.TopRockIdMap = new int[mapchunk.TopRockIdMapOld.Length];
			}
			for (int i = 0; i < mapchunk.TopRockIdMapOld.Length; i++)
			{
				mapchunk.TopRockIdMap[i] = mapchunk.TopRockIdMapOld[i];
			}
		}
		if (mapchunk.Moddata == null)
		{
			mapchunk.Moddata = new Dictionary<string, byte[]>();
		}
		if (mapchunk.SnowAccum == null)
		{
			mapchunk.SnowAccum = new ConcurrentDictionary<Vec2i, float>();
		}
		return mapchunk;
	}

	public byte[] ToBytes()
	{
		using FastMemoryStream ms = new FastMemoryStream();
		return ToBytes(ms);
	}

	public byte[] ToBytes(FastMemoryStream ms)
	{
		ms.Reset();
		Serializer.Serialize((Stream)ms, this);
		return ms.ToArray();
	}

	public void SetData(string key, byte[] data)
	{
		SetModdata(key, data);
	}

	public byte[] GetData(string key)
	{
		return GetModdata(key);
	}

	public void SetModdata(string key, byte[] data)
	{
		lock (modDataLock)
		{
			Moddata[key] = data;
			MarkDirty();
		}
	}

	public byte[] GetModdata(string key)
	{
		lock (modDataLock)
		{
			Moddata.TryGetValue(key, out var data);
			return data;
		}
	}

	public void SetModdata<T>(string key, T data)
	{
		SetModdata(key, SerializerUtil.Serialize(data));
	}

	public T GetModdata<T>(string key, T defaultValue = default(T))
	{
		byte[] moddata = GetModdata(key);
		if (moddata == null)
		{
			return defaultValue;
		}
		return SerializerUtil.Deserialize<T>(moddata);
	}

	public void RemoveModdata(string key)
	{
		Moddata.Remove(key);
		MarkDirty();
	}

	public void MarkFresh()
	{
		UnloadGeneration = 5;
	}

	public void DoAge()
	{
		UnloadGeneration--;
	}

	public bool IsOld()
	{
		return UnloadGeneration <= 1;
	}

	public Packet_Server ToPacket(int chunkX, int chunkZ)
	{
		Packet_ServerMapChunk p = new Packet_ServerMapChunk
		{
			ChunkX = chunkX,
			ChunkZ = chunkZ,
			Ymax = YMax,
			RainHeightMap = ArrayConvert.UshortToByte(RainHeightMap),
			TerrainHeightMap = ArrayConvert.UshortToByte(WorldGenTerrainHeightMap),
			Structures = null
		};
		return new Packet_Server
		{
			Id = 17,
			MapChunk = p
		};
	}

	public void MarkDirty()
	{
		DirtyForSaving = true;
	}
}
