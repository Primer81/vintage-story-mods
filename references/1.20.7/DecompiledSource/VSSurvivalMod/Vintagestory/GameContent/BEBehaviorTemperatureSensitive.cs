using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BEBehaviorTemperatureSensitive : BlockEntityBehavior
{
	private ITemperatureSensitive its;

	private WeatherSystemBase wsys;

	private Vec3d tmpPos = new Vec3d();

	private float wateredSum;

	public BEBehaviorTemperatureSensitive(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		Blockentity.RegisterGameTickListener(onTick, 1900 + Api.World.Rand.Next(200), Api.World.Rand.Next(500));
		its = Blockentity as ITemperatureSensitive;
		if (!(Blockentity is ITemperatureSensitive))
		{
			throw new InvalidOperationException("Applying BehaviorTemperatureSensitive to a block entity requires that block entity class to implement ITemperatureSensitive");
		}
		wsys = api.ModLoader.GetModSystem<WeatherSystemBase>();
	}

	public void OnWatered(float dt)
	{
		wateredSum += dt;
		if (wateredSum > 0.2f)
		{
			its.CoolNow(1f);
			wateredSum -= 0.2f;
		}
	}

	private void onTick(float dt)
	{
		if (!its.IsHot)
		{
			return;
		}
		wateredSum = Math.Max(0f, wateredSum - dt / 2f);
		Block lblock = Api.World.BlockAccessor.GetBlock(base.Pos, 2);
		if (lblock.IsLiquid() && lblock.LiquidCode != "lava")
		{
			its.CoolNow(25f);
			return;
		}
		tmpPos.Set((double)base.Pos.X + 0.5, (double)base.Pos.Y + 0.5, (double)base.Pos.Z + 0.5);
		float rainLevel = 0f;
		if (Api.Side == EnumAppSide.Server && Api.World.Rand.NextDouble() < 0.75 && Api.World.BlockAccessor.GetRainMapHeightAt(base.Pos.X, base.Pos.Z) <= base.Pos.Y && (double)(rainLevel = wsys.GetPrecipitation(tmpPos)) > 0.04 && Api.World.Rand.NextDouble() < (double)(rainLevel * 5f))
		{
			its.CoolNow(rainLevel);
		}
	}
}
