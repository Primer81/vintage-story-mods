using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class HailParticleProps : WeatherParticleProps
{
	public override Vec3d Pos
	{
		get
		{
			double px = MinPos.X + SimpleParticleProperties.rand.NextDouble() * SimpleParticleProperties.rand.NextDouble() * 80.0 * (double)(1 - 2 * SimpleParticleProperties.rand.Next(2));
			double pz = MinPos.Z + SimpleParticleProperties.rand.NextDouble() * SimpleParticleProperties.rand.NextDouble() * 80.0 * (double)(1 - 2 * SimpleParticleProperties.rand.Next(2));
			tmpPos.Set(px, MinPos.Y + AddPos.Y * SimpleParticleProperties.rand.NextDouble(), pz);
			int num = (int)(tmpPos.X - (double)centerPos.X);
			int dz = (int)(tmpPos.Z - (double)centerPos.Z);
			int lx = GameMath.Clamp(num / 4 + 8, 0, 15);
			int lz = GameMath.Clamp(dz / 4 + 8, 0, 15);
			tmpPos.Y = Math.Max(tmpPos.Y, lowResRainHeightMap[lx, lz] + 3);
			return tmpPos;
		}
	}
}
