using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WindPattern
{
	public WindPatternConfig config;

	protected SimplexNoise strengthNoiseGen;

	private ICoreAPI api;

	private LCGRandom rand;

	public WindPatternState State = new WindPatternState();

	public float Strength;

	public WindPattern(ICoreAPI api, WindPatternConfig config, int index, LCGRandom rand, int seed)
	{
		this.rand = rand;
		this.config = config;
		this.api = api;
		State.Index = index;
		if (config.StrengthNoise != null)
		{
			strengthNoiseGen = new SimplexNoise(config.StrengthNoise.Amplitudes, config.StrengthNoise.Frequencies, seed + index);
		}
	}

	public virtual void OnBeginUse()
	{
		State.BaseStrength = (Strength = config.Strength.nextFloat(1f, rand));
		State.ActiveUntilTotalHours = api.World.Calendar.TotalHours + (double)config.DurationHours.nextFloat(1f, rand);
	}

	public virtual void Update(float dt)
	{
		if (strengthNoiseGen != null)
		{
			double timeAxis = api.World.Calendar.TotalDays * 10.0;
			Strength = State.BaseStrength + (float)GameMath.Clamp(strengthNoiseGen.Noise(0.0, timeAxis), 0.0, 1.0);
		}
	}

	public virtual string GetWindName()
	{
		return config.Name;
	}
}
