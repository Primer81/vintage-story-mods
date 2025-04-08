using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleGrasshopper : EntityParticleInsect
{
	public override string Type => "grassHopper";

	public EntityParticleGrasshopper(ICoreClientAPI capi, double x, double y, double z)
		: base(capi, x, y, z)
	{
		Block block = capi.World.BlockAccessor.GetBlock((int)x, (int)y, (int)z);
		if (block.BlockMaterial == EnumBlockMaterial.Plant)
		{
			int col = block.GetColor(capi, new BlockPos((int)x, (int)y, (int)z));
			ColorRed = (byte)((uint)(col >> 16) & 0xFFu);
			ColorGreen = (byte)((uint)(col >> 8) & 0xFFu);
			ColorBlue = (byte)((uint)col & 0xFFu);
		}
		else
		{
			ColorRed = 31;
			ColorGreen = 178;
			ColorBlue = 144;
		}
		sound = new AssetLocation("sounds/creature/grasshopper");
	}

	protected override bool shouldPlaySound()
	{
		if (EntityParticleInsect.rand.NextDouble() < 0.01 && capi.World.BlockAccessor.GetLightLevel(Position.AsBlockPos, EnumLightLevelType.TimeOfDaySunLight) > 7)
		{
			float season = capi.World.Calendar.GetSeasonRel(Position.AsBlockPos);
			if (((double)season > 0.48 && (double)season < 0.63) || EntityParticleInsect.rand.NextDouble() < 0.33)
			{
				return true;
			}
		}
		return false;
	}
}
