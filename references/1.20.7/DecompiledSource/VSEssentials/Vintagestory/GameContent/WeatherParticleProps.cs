using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherParticleProps : SimpleParticleProperties
{
	public int[,] lowResRainHeightMap;

	public BlockPos centerPos;

	public override Vec3d Pos
	{
		get
		{
			tmpPos.Set(MinPos.X + AddPos.X * SimpleParticleProperties.rand.NextDouble(), MinPos.Y + AddPos.Y * SimpleParticleProperties.rand.NextDouble(), MinPos.Z + AddPos.Z * SimpleParticleProperties.rand.NextDouble());
			int num = (int)(tmpPos.X - (double)centerPos.X);
			int dz = (int)(tmpPos.Z - (double)centerPos.Z);
			int lx = GameMath.Clamp(num / 4 + 8, 0, 15);
			int lz = GameMath.Clamp(dz / 4 + 8, 0, 15);
			tmpPos.Y = Math.Max(tmpPos.Y, lowResRainHeightMap[lx, lz] + 3);
			return tmpPos;
		}
	}
}
