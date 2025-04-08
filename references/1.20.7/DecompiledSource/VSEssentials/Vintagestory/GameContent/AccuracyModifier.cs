using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class AccuracyModifier
{
	internal EntityAgent entity;

	internal long aimStartMs;

	public float SecondsSinceAimStart => (float)(entity.World.ElapsedMilliseconds - aimStartMs) / 1000f;

	public AccuracyModifier(EntityAgent entity)
	{
		this.entity = entity;
	}

	public virtual void BeginAim()
	{
		aimStartMs = entity.World.ElapsedMilliseconds;
	}

	public virtual void EndAim()
	{
	}

	public virtual void OnHurt(float damage)
	{
	}

	public virtual void Update(float dt, ref float accuracy)
	{
	}
}
