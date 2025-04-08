using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityLocust : EntityGlowingAgent
{
	private double mul1;

	private double mul2;

	private bool lightEmitting;

	private int cnt;

	public override byte[] LightHsv
	{
		get
		{
			if (!lightEmitting)
			{
				return null;
			}
			return base.LightHsv;
		}
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		lightEmitting = !Code.Path.Contains("sawblade");
	}

	public override double GetWalkSpeedMultiplier(double groundDragFactor = 0.3)
	{
		double multiplier = (servercontrols.Sneak ? ((double)GlobalConstants.SneakSpeedMultiplier) : 1.0) * (servercontrols.Sprint ? GlobalConstants.SprintSpeedMultiplier : 1.0);
		if (FeetInLiquid)
		{
			multiplier /= 2.5;
		}
		multiplier *= mul1 * mul2;
		return multiplier * (double)GameMath.Clamp(Stats.GetBlended("walkspeed"), 0f, 999f);
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (cnt++ > 2)
		{
			cnt = 0;
			EntityPos pos = base.SidedPos;
			Block belowBlock = World.BlockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY - 0.05000000074505806), (int)pos.Z);
			Block insideblock = World.BlockAccessor.GetBlockRaw((int)pos.X, (int)(pos.InternalY + 0.009999999776482582), (int)pos.Z);
			mul1 = ((belowBlock.Code == null || belowBlock.Code.Path.Contains("metalspike")) ? 1f : belowBlock.WalkSpeedMultiplier);
			mul2 = ((insideblock.Code == null || insideblock.Code.Path.Contains("metalspike")) ? 1f : insideblock.WalkSpeedMultiplier);
		}
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		if (damageSource.GetCauseEntity() is EntityEidolon)
		{
			return false;
		}
		return base.ReceiveDamage(damageSource, damage);
	}
}
