using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class EntityParticleCicada : EntityParticleGrasshopper
{
	private float pitch;

	public override string Type => "cicada";

	protected override float soundRange => 24f;

	protected override float despawnDistanceSq => 576f;

	public EntityParticleCicada(ICoreClientAPI capi, double x, double y, double z)
		: base(capi, x, y, z)
	{
		ColorRed = 42;
		ColorGreen = 72;
		ColorBlue = 96;
		jumpHeight = 0f;
		sound = new AssetLocation("sounds/creature/cicada");
		doubleJump = false;
		soundCoolDown = 12f + (float)EntityParticleInsect.rand.NextDouble() * 3f;
		pitch = (float)capi.World.Rand.NextDouble() * 0.2f + 0.85f;
		base.Size = 1f;
		base.GravityStrength = 0f;
	}

	protected override float RandomPitch()
	{
		return pitch;
	}
}
