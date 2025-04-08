using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

[ProtoContract]
public class ServerMapRegion : IMapRegion
{
	[ProtoMember(1)]
	public IntDataMap2D LandformMap;

	[ProtoMember(2)]
	public IntDataMap2D ForestMap;

	[ProtoMember(3)]
	public IntDataMap2D ClimateMap;

	[ProtoMember(4)]
	public IntDataMap2D GeologicProvinceMap;

	[ProtoMember(5)]
	public IntDataMap2D BushMap;

	[ProtoMember(6)]
	public IntDataMap2D FlowerMap;

	[ProtoMember(8)]
	public Dictionary<string, IntDataMap2D> OreMaps;

	[ProtoMember(9)]
	public Dictionary<string, byte[]> ModData;

	[ProtoMember(10)]
	public Dictionary<string, IntDataMap2D> ModMaps;

	[ProtoMember(11)]
	public List<GeneratedStructure> GeneratedStructures;

	[ProtoMember(12)]
	public IntDataMap2D[] RockStrata;

	[ProtoMember(15)]
	public IntDataMap2D BeachMap;

	[ProtoMember(17)]
	public IntDataMap2D UpheavelMap;

	[ProtoMember(18)]
	public IntDataMap2D OceanMap;

	public bool DirtyForSaving;

	public bool NeighbourRegionsChecked;

	public long loadedTotalMs;

	[ProtoMember(13)]
	public IntDataMap2D OreMapVerticalDistortTop { get; set; }

	[ProtoMember(14)]
	public IntDataMap2D OreMapVerticalDistortBottom { get; set; }

	[ProtoMember(16)]
	public Dictionary<string, IntDataMap2D> BlockPatchMaps { get; set; }

	IntDataMap2D IMapRegion.ClimateMap
	{
		get
		{
			return ClimateMap;
		}
		set
		{
			ClimateMap = value;
		}
	}

	IntDataMap2D IMapRegion.LandformMap
	{
		get
		{
			return LandformMap;
		}
		set
		{
			LandformMap = value;
		}
	}

	IntDataMap2D IMapRegion.ForestMap
	{
		get
		{
			return ForestMap;
		}
		set
		{
			ForestMap = value;
		}
	}

	IntDataMap2D IMapRegion.BeachMap
	{
		get
		{
			return BeachMap;
		}
		set
		{
			BeachMap = value;
		}
	}

	IntDataMap2D IMapRegion.UpheavelMap
	{
		get
		{
			return UpheavelMap;
		}
		set
		{
			UpheavelMap = value;
		}
	}

	IntDataMap2D IMapRegion.OceanMap
	{
		get
		{
			return OceanMap;
		}
		set
		{
			OceanMap = value;
		}
	}

	IntDataMap2D IMapRegion.ShrubMap
	{
		get
		{
			return BushMap;
		}
		set
		{
			BushMap = value;
		}
	}

	IntDataMap2D IMapRegion.FlowerMap
	{
		get
		{
			return FlowerMap;
		}
		set
		{
			FlowerMap = value;
		}
	}

	IntDataMap2D IMapRegion.GeologicProvinceMap
	{
		get
		{
			return GeologicProvinceMap;
		}
		set
		{
			GeologicProvinceMap = value;
		}
	}

	IntDataMap2D[] IMapRegion.RockStrata
	{
		get
		{
			return RockStrata;
		}
		set
		{
			RockStrata = value;
		}
	}

	bool IMapRegion.DirtyForSaving
	{
		get
		{
			return DirtyForSaving;
		}
		set
		{
			DirtyForSaving = value;
		}
	}

	Dictionary<string, byte[]> IMapRegion.ModData => ModData;

	Dictionary<string, IntDataMap2D> IMapRegion.ModMaps => ModMaps;

	Dictionary<string, IntDataMap2D> IMapRegion.OreMaps => OreMaps;

	List<GeneratedStructure> IMapRegion.GeneratedStructures => GeneratedStructures;

	public static ServerMapRegion CreateNew()
	{
		return new ServerMapRegion
		{
			LandformMap = IntDataMap2D.CreateEmpty(),
			UpheavelMap = IntDataMap2D.CreateEmpty(),
			ForestMap = IntDataMap2D.CreateEmpty(),
			BushMap = IntDataMap2D.CreateEmpty(),
			FlowerMap = IntDataMap2D.CreateEmpty(),
			ClimateMap = IntDataMap2D.CreateEmpty(),
			BeachMap = IntDataMap2D.CreateEmpty(),
			OreMapVerticalDistortTop = IntDataMap2D.CreateEmpty(),
			OreMapVerticalDistortBottom = IntDataMap2D.CreateEmpty(),
			GeologicProvinceMap = IntDataMap2D.CreateEmpty(),
			OreMaps = new Dictionary<string, IntDataMap2D>(),
			ModMaps = new Dictionary<string, IntDataMap2D>(),
			ModData = new Dictionary<string, byte[]>(),
			GeneratedStructures = new List<GeneratedStructure>(),
			BlockPatchMaps = new Dictionary<string, IntDataMap2D>(),
			OceanMap = IntDataMap2D.CreateEmpty(),
			DirtyForSaving = true
		};
	}

	public void AddGeneratedStructure(GeneratedStructure newStructure)
	{
		List<GeneratedStructure> newlist = new List<GeneratedStructure>(GeneratedStructures.Count + 1);
		foreach (GeneratedStructure oldstruct in GeneratedStructures)
		{
			newlist.Add(oldstruct);
		}
		newlist.Add(newStructure);
		GeneratedStructures = newlist;
		DirtyForSaving = true;
	}

	public static ServerMapRegion FromBytes(byte[] serializedMapRegion)
	{
		ServerMapRegion mapreg = SerializerUtil.Deserialize<ServerMapRegion>(serializedMapRegion);
		if (mapreg.OreMaps == null)
		{
			mapreg.OreMaps = new Dictionary<string, IntDataMap2D>();
		}
		if (mapreg.ModMaps == null)
		{
			mapreg.ModMaps = new Dictionary<string, IntDataMap2D>();
		}
		if (mapreg.ModData == null)
		{
			mapreg.ModData = new Dictionary<string, byte[]>();
		}
		if (mapreg.GeneratedStructures == null)
		{
			mapreg.GeneratedStructures = new List<GeneratedStructure>();
		}
		if (mapreg.BeachMap == null)
		{
			mapreg.BeachMap = IntDataMap2D.CreateEmpty();
		}
		if (mapreg.BlockPatchMaps == null)
		{
			mapreg.BlockPatchMaps = new Dictionary<string, IntDataMap2D>();
		}
		return mapreg;
	}

	public byte[] ToBytes()
	{
		return SerializerUtil.Serialize(this);
	}

	public Packet_Server ToPacket(int regionX, int regionZ)
	{
		Packet_MapRegion p = new Packet_MapRegion
		{
			ClimateMap = ToPacket(ClimateMap),
			ForestMap = ToPacket(ForestMap),
			GeologicProvinceMap = ToPacket(GeologicProvinceMap),
			LandformMap = ToPacket(LandformMap),
			RegionX = regionX,
			RegionZ = regionZ
		};
		p.SetGeneratedStructures(ToPacket(GeneratedStructures));
		p.SetModdata(SerializerUtil.Serialize(ModData));
		return new Packet_Server
		{
			Id = 42,
			MapRegion = p
		};
	}

	private static Packet_GeneratedStructure[] ToPacket(List<GeneratedStructure> generatedStructures)
	{
		Packet_GeneratedStructure[] packets = new Packet_GeneratedStructure[generatedStructures.Count];
		for (int i = 0; i < packets.Length; i++)
		{
			GeneratedStructure struc = generatedStructures[i];
			Packet_GeneratedStructure obj = (packets[i] = new Packet_GeneratedStructure());
			obj.X1 = struc.Location.X1;
			obj.Y1 = struc.Location.Y1;
			obj.Z1 = struc.Location.Z1;
			obj.X2 = struc.Location.X2;
			obj.Y2 = struc.Location.Y2;
			obj.Z2 = struc.Location.Z2;
			obj.Code = struc.Code;
			obj.Group = struc.Group;
		}
		return packets;
	}

	public static Packet_IntMap ToPacket(IntDataMap2D map)
	{
		if (map?.Data == null)
		{
			return new Packet_IntMap
			{
				Data = new int[0],
				DataCount = 0,
				DataLength = 0,
				Size = 0
			};
		}
		return new Packet_IntMap
		{
			Data = map.Data,
			DataCount = map.Data.Length,
			DataLength = map.Data.Length,
			Size = map.Size,
			BottomRightPadding = map.BottomRightPadding,
			TopLeftPadding = map.TopLeftPadding
		};
	}

	public void SetModdata(string key, byte[] data)
	{
		ModData[key] = data;
		DirtyForSaving = true;
	}

	public void RemoveModdata(string key)
	{
		if (ModData.Remove(key))
		{
			DirtyForSaving = true;
		}
	}

	public byte[] GetModdata(string key)
	{
		ModData.TryGetValue(key, out var data);
		return data;
	}

	public void SetModdata<T>(string key, T data)
	{
		SetModdata(key, SerializerUtil.Serialize(data));
	}

	public T GetModdata<T>(string key)
	{
		byte[] data = GetModdata(key);
		if (data != null)
		{
			return SerializerUtil.Deserialize<T>(data);
		}
		return default(T);
	}
}
