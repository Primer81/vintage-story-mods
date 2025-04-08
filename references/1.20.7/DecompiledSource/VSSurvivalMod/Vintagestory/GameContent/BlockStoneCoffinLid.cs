using System;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockStoneCoffinLid : Block
{
	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[5]] is BlockStoneCoffinSection block)
		{
			int temp = block.GetTemperature(api.World, pos.DownCopy());
			int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);
			for (int j = 0; j < sourceMesh.FlagsCount; j++)
			{
				sourceMesh.Flags[j] &= -256;
				sourceMesh.Flags[j] |= extraGlow;
			}
			int[] incade = ColorUtil.getIncandescenceColor(temp);
			float ina = GameMath.Clamp((float)incade[3] / 255f, 0f, 1f);
			for (int i = 0; i < lightRgbsByCorner.Length; i++)
			{
				int num = lightRgbsByCorner[i];
				int r = num & 0xFF;
				int g = (num >> 8) & 0xFF;
				int b = (num >> 16) & 0xFF;
				int a = (num >> 24) & 0xFF;
				lightRgbsByCorner[i] = (GameMath.Mix(a, 0, Math.Min(1f, 1.5f * ina)) << 24) | (GameMath.Mix(b, incade[2], ina) << 16) | (GameMath.Mix(g, incade[1], ina) << 8) | GameMath.Mix(r, incade[0], ina);
			}
		}
	}
}
