using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class AmbientSound : IEquatable<AmbientSound>, IEqualityComparer<AmbientSound>
{
	public ILoadedSound Sound;

	public int QuantityNearbyBlocks;

	public AssetLocation AssetLoc;

	public List<Cuboidi> BoundingBoxes = new List<Cuboidi>();

	public Vec3i SectionPos;

	public float Ratio = 10f;

	public float VolumeMul = 1f;

	public EnumSoundType SoundType = EnumSoundType.Ambient;

	public double MaxDistanceMerge = 3.0;

	private Vec3f tmp = new Vec3f();

	private Vec3f tmpout = new Vec3f();

	public float AdjustedVolume => GameMath.Clamp(GameMath.Sqrt(QuantityNearbyBlocks) / Ratio, 1f / Ratio, 1f) * VolumeMul;

	public double DistanceTo(AmbientSound sound)
	{
		double minDistance = 9999999.0;
		for (int i = 0; i < BoundingBoxes.Count; i++)
		{
			for (int j = 0; j < sound.BoundingBoxes.Count; j++)
			{
				minDistance = Math.Min(minDistance, BoundingBoxes[i].ShortestDistanceFrom(sound.BoundingBoxes[j]));
			}
		}
		return minDistance;
	}

	public bool Equals(AmbientSound other)
	{
		if (AssetLoc.Equals(other.AssetLoc))
		{
			return SectionPos.Equals(other.SectionPos);
		}
		return false;
	}

	public bool Equals(AmbientSound x, AmbientSound y)
	{
		return x.Equals(y);
	}

	public override bool Equals(object obj)
	{
		if (obj is AmbientSound)
		{
			return (obj as AmbientSound).Equals(this);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return AssetLoc.GetHashCode() * 23 + SectionPos.GetHashCode();
	}

	public void FadeToNewVolumne()
	{
		float newVolumne = AdjustedVolume;
		if ((double)Math.Abs(newVolumne - Sound.Params.Volume) > 0.02)
		{
			Sound.FadeTo(newVolumne, 1f, null);
		}
	}

	public int GetHashCode(AmbientSound obj)
	{
		return obj.AssetLoc.GetHashCode() * 23 + obj.SectionPos.GetHashCode();
	}

	internal void updatePosition(EntityPos position)
	{
		double minDist = 999999.0;
		tmpout.Set(-99999f, -99999f, -99999f);
		foreach (Cuboidi box in BoundingBoxes)
		{
			tmp.X = (float)GameMath.Clamp(position.X, box.X1, box.X2);
			tmp.Y = (float)GameMath.Clamp(position.Y, box.Y1, box.Y2);
			tmp.Z = (float)GameMath.Clamp(position.Z, box.Z1, box.Z2);
			double dist = tmp.DistanceSq(position.X, position.Y, position.Z);
			if (dist < minDist)
			{
				minDist = dist;
				tmpout.Set(tmp);
			}
		}
		Sound.SetPosition(tmpout);
	}

	public void RenderWireFrame(ClientMain game, WireframeCube wireframe)
	{
		foreach (Cuboidi box in BoundingBoxes)
		{
			float scaleX = box.X2 - box.X1;
			float scaleY = box.Y2 - box.Y1;
			float scaleZ = box.Z2 - box.Z1;
			float x = box.X1;
			float y = box.Y1;
			float z = box.Z1;
			wireframe.Render(game.api, x, y, z, scaleX, scaleY, scaleZ, 1f);
		}
	}
}
