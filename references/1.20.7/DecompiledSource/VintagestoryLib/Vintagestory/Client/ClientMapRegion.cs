using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client;

internal class ClientMapRegion : IMapRegion
{
	public IntDataMap2D LandformMap;

	public IntDataMap2D ForestMap;

	public IntDataMap2D ClimateMap;

	public IntDataMap2D ShrubMap;

	public IntDataMap2D FlowerMap;

	public IntDataMap2D BeachMap;

	public IntDataMap2D GeologicProvinceMap;

	public Dictionary<string, byte[]> ModData { get; set; }

	public Dictionary<string, IntDataMap2D> ModMaps => null;

	public Dictionary<string, IntDataMap2D> OreMaps => null;

	public List<GeneratedStructure> GeneratedStructures { get; private set; }

	public IntDataMap2D[] RockStrata
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public IntDataMap2D RockStrata2DDistort
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public IntDataMap2D OreMapVerticalDistortTop
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IntDataMap2D OreMapVerticalDistortBottom
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public Dictionary<string, IntDataMap2D> BlockPatchMaps
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

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

	IntDataMap2D IMapRegion.ShrubMap
	{
		get
		{
			return ShrubMap;
		}
		set
		{
			ShrubMap = value;
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

	bool IMapRegion.DirtyForSaving
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public IntDataMap2D UpheavelMap
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public IntDataMap2D OceanMap
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public void SetModdata(string key, byte[] data)
	{
		ModData[key] = data;
	}

	public void RemoveModdata(string key)
	{
		ModData.Remove(key);
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

	public void UpdateFromPacket(Packet_Server packet)
	{
		LandformMap = IntMapFromPacket(packet.MapRegion.LandformMap);
		ForestMap = IntMapFromPacket(packet.MapRegion.ForestMap);
		ClimateMap = IntMapFromPacket(packet.MapRegion.ClimateMap);
		GeologicProvinceMap = IntMapFromPacket(packet.MapRegion.GeologicProvinceMap);
		GeneratedStructures = FromPacket(packet.MapRegion.GeneratedStructures, packet.MapRegion.GeneratedStructuresCount);
		ModData = SerializerUtil.Deserialize<Dictionary<string, byte[]>>(packet.MapRegion.Moddata);
	}

	private List<GeneratedStructure> FromPacket(Packet_GeneratedStructure[] generatedStructures, int cnt)
	{
		GeneratedStructure[] strucs = new GeneratedStructure[cnt];
		for (int i = 0; i < strucs.Length; i++)
		{
			Packet_GeneratedStructure p = generatedStructures[i];
			strucs[i] = new GeneratedStructure
			{
				Code = p.Code,
				Group = p.Group,
				Location = new Cuboidi(p.X1, p.Y1, p.Z1, p.X2, p.Y2, p.Z2)
			};
		}
		return new List<GeneratedStructure>(strucs);
	}

	private IntDataMap2D IntMapFromPacket(Packet_IntMap p)
	{
		return new IntDataMap2D
		{
			Data = p.Data,
			Size = p.Size,
			BottomRightPadding = p.BottomRightPadding,
			TopLeftPadding = p.TopLeftPadding
		};
	}

	public void AddGeneratedStructure(GeneratedStructure generatedStructure)
	{
		GeneratedStructures.Add(generatedStructure);
	}
}
