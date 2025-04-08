using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class EntityParticleCoqui : EntityParticleGrasshopper
{
	private static long lastCoquiSound;

	private long soundWaitMs;

	private float pitch;

	public override string Type => "coqui";

	public EntityParticleCoqui(ICoreClientAPI capi, double x, double y, double z)
		: base(capi, x, y, z)
	{
		ColorRed = 86;
		ColorGreen = 144;
		ColorBlue = 193;
		jumpHeight = 0.8f;
		sound = new AssetLocation("sounds/creature/coqui");
		doubleJump = false;
		soundCoolDown = 4f + (float)EntityParticleInsect.rand.NextDouble() * 3f;
		soundWaitMs = 250 + EntityParticleInsect.rand.Next(250);
		pitch = (float)capi.World.Rand.NextDouble() * 0.2f + 0.89f;
	}

	protected override float RandomPitch()
	{
		return pitch;
	}

	protected override bool shouldPlaySound()
	{
		int num;
		if (EntityParticleInsect.rand.NextDouble() < 0.015 && capi.World.ElapsedMilliseconds - lastCoquiSound > soundWaitMs)
		{
			num = ((capi.World.BlockAccessor.GetLightLevel(Position.AsBlockPos, EnumLightLevelType.TimeOfDaySunLight) < 14) ? 1 : 0);
			if (num != 0)
			{
				lastCoquiSound = capi.World.ElapsedMilliseconds;
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}
}
