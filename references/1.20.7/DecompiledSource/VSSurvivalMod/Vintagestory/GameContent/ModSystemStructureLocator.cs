using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemStructureLocator : ModSystem
{
	private ICoreServerAPI sapi;

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
	}

	public GeneratedStructure GetStructure(StructureLocation loc)
	{
		IMapRegion reg = sapi.World.BlockAccessor.GetMapRegion(loc.RegionX, loc.RegionZ);
		GeneratedStructure structure = null;
		if (loc.Position != null)
		{
			structure = reg?.GeneratedStructures.Find((GeneratedStructure s) => s.Location.X1 == loc.Position.X && s.Location.Y1 == loc.Position.Y && s.Location.Z1 == loc.Position.Z);
		}
		else if (loc.StructureIndex >= 0 && loc.StructureIndex < reg?.GeneratedStructures.Count)
		{
			structure = reg.GeneratedStructures[loc.StructureIndex];
		}
		return structure;
	}

	public StructureLocation FindFreshStructureLocation(string code, BlockPos nearPos, int searchRange)
	{
		return FindStructureLocation(delegate(GeneratedStructure struc, int index, IMapRegion region)
		{
			if (struc.Code.Split('/')[0] == code)
			{
				int[] moddata = region.GetModdata<int[]>("consumedStructureLocations");
				List<Vec3i> moddata2 = region.GetModdata<List<Vec3i>>("consumedStrucLocPos");
				bool result = moddata == null || !moddata.Contains(index);
				if (moddata2 != null && moddata2.Contains(struc.Location.Start))
				{
					result = false;
				}
				return result;
			}
			return false;
		}, nearPos, searchRange);
	}

	public StructureLocation FindStructureLocation(ActionBoolReturn<GeneratedStructure, int, IMapRegion> matcher, BlockPos pos, int searchRange)
	{
		int regionSize = sapi.WorldManager.RegionSize;
		int num = (pos.X - searchRange) / regionSize;
		int maxrx = (pos.X + searchRange) / regionSize;
		int minrz = (pos.Z - searchRange) / regionSize;
		int maxrz = (pos.Z + searchRange) / regionSize;
		for (int rx = num; rx <= maxrx; rx++)
		{
			for (int rz = minrz; rz <= maxrz; rz++)
			{
				IMapRegion reg = sapi.World.BlockAccessor.GetMapRegion(rx, rz);
				if (reg == null)
				{
					continue;
				}
				for (int i = 0; i < reg.GeneratedStructures.Count; i++)
				{
					GeneratedStructure struc = reg.GeneratedStructures[i];
					if (struc.Location.ShortestDistanceFrom(pos.X, pos.Y, pos.Z) < (double)searchRange && matcher(struc, i, reg))
					{
						return new StructureLocation
						{
							Position = struc.Location.Start,
							RegionX = rx,
							RegionZ = rz
						};
					}
				}
			}
		}
		return null;
	}

	public void ConsumeStructureLocation(StructureLocation strucLoc)
	{
		IMapRegion mapRegion = sapi.World.BlockAccessor.GetMapRegion(strucLoc.RegionX, strucLoc.RegionZ);
		List<Vec3i> locs = mapRegion.GetModdata<List<Vec3i>>("consumedStrucLocPos") ?? new List<Vec3i>();
		locs.Add(strucLoc.Position);
		mapRegion.SetModdata("consumedStrucLocPos", locs);
	}
}
