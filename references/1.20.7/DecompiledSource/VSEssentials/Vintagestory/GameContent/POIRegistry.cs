using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class POIRegistry : ModSystem
{
	private Dictionary<Vec2i, List<IPointOfInterest>> PoisByChunkColumn = new Dictionary<Vec2i, List<IPointOfInterest>>();

	private Vec2i tmp = new Vec2i();

	private const int chunksize = 32;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
	}

	public void WalkPois(Vec3d centerPos, float radius, PoiMatcher callback = null)
	{
		int num = (int)(centerPos.X - (double)radius) / 32;
		int mincz = (int)(centerPos.Z - (double)radius) / 32;
		int maxcx = (int)(centerPos.X + (double)radius) / 32;
		int maxcz = (int)(centerPos.Z + (double)radius) / 32;
		float radiusSq = radius * radius;
		for (int cx = num; cx < maxcx; cx++)
		{
			for (int cz = mincz; cz < maxcz; cz++)
			{
				List<IPointOfInterest> pois = null;
				tmp.Set(cx, cz);
				PoisByChunkColumn.TryGetValue(tmp, out pois);
				if (pois == null)
				{
					continue;
				}
				for (int i = 0; i < pois.Count; i++)
				{
					if (!(pois[i].Position.SquareDistanceTo(centerPos) > radiusSq))
					{
						callback(pois[i]);
					}
				}
			}
		}
	}

	public IPointOfInterest GetNearestPoi(Vec3d centerPos, float radius, PoiMatcher matcher = null)
	{
		int num = (int)(centerPos.X - (double)radius) / 32;
		int mincz = (int)(centerPos.Z - (double)radius) / 32;
		int maxcx = (int)(centerPos.X + (double)radius) / 32;
		int maxcz = (int)(centerPos.Z + (double)radius) / 32;
		float radiusSq = radius * radius;
		float nearestDistSq = 9999999f;
		IPointOfInterest nearestPoi = null;
		for (int cx = num; cx <= maxcx; cx++)
		{
			for (int cz = mincz; cz <= maxcz; cz++)
			{
				tmp.Set(cx, cz);
				PoisByChunkColumn.TryGetValue(tmp, out var pois);
				if (pois == null)
				{
					continue;
				}
				for (int i = 0; i < pois.Count; i++)
				{
					float distSq = pois[i].Position.SquareDistanceTo(centerPos);
					if (!(distSq > radiusSq) && distSq < nearestDistSq && matcher(pois[i]))
					{
						nearestPoi = pois[i];
						nearestDistSq = distSq;
					}
				}
			}
		}
		return nearestPoi;
	}

	public IPointOfInterest GetWeightedNearestPoi(Vec3d centerPos, float radius, PoiMatcher matcher = null)
	{
		int num = (int)(centerPos.X - (double)radius) / 32;
		int mincz = (int)(centerPos.Z - (double)radius) / 32;
		int maxcx = (int)(centerPos.X + (double)radius) / 32;
		int maxcz = (int)(centerPos.Z + (double)radius) / 32;
		float radiusSq = radius * radius;
		float nearestDistSq = 9999999f;
		IPointOfInterest nearestPoi = null;
		for (int cx = num; cx <= maxcx; cx++)
		{
			double chunkDistX = 0.0;
			if ((double)(cx * 32) > centerPos.X)
			{
				chunkDistX = (double)(cx * 32) - centerPos.X;
			}
			else if ((double)((cx + 1) * 32) < centerPos.X)
			{
				chunkDistX = centerPos.X - (double)((cx + 1) * 32);
			}
			for (int cz = mincz; cz <= maxcz; cz++)
			{
				double cdistZ = 0.0;
				if ((double)(cz * 32) > centerPos.Z)
				{
					cdistZ = (double)(cz * 32) - centerPos.Z;
				}
				else if ((double)((cz + 1) * 32) < centerPos.Z)
				{
					cdistZ = centerPos.Z - (double)((cz + 1) * 32);
				}
				if (chunkDistX * chunkDistX + cdistZ * cdistZ > (double)nearestDistSq)
				{
					continue;
				}
				List<IPointOfInterest> pois = null;
				tmp.Set(cx, cz);
				PoisByChunkColumn.TryGetValue(tmp, out pois);
				if (pois == null)
				{
					continue;
				}
				for (int i = 0; i < pois.Count; i++)
				{
					Vec3d position = pois[i].Position;
					float weight = ((pois[i] is IAnimalNest nest) ? nest.DistanceWeighting : 1f);
					float distSq = position.SquareDistanceTo(centerPos) * weight;
					if (!(distSq > radiusSq) && distSq < nearestDistSq && matcher(pois[i]))
					{
						nearestPoi = pois[i];
						nearestDistSq = distSq;
					}
				}
			}
		}
		return nearestPoi;
	}

	public void AddPOI(IPointOfInterest poi)
	{
		tmp.Set((int)poi.Position.X / 32, (int)poi.Position.Z / 32);
		List<IPointOfInterest> pois = null;
		PoisByChunkColumn.TryGetValue(tmp, out pois);
		if (pois == null)
		{
			pois = (PoisByChunkColumn[tmp] = new List<IPointOfInterest>());
		}
		if (!pois.Contains(poi))
		{
			pois.Add(poi);
		}
	}

	public void RemovePOI(IPointOfInterest poi)
	{
		tmp.Set((int)poi.Position.X / 32, (int)poi.Position.Z / 32);
		List<IPointOfInterest> pois = null;
		PoisByChunkColumn.TryGetValue(tmp, out pois);
		pois?.Remove(poi);
	}
}
