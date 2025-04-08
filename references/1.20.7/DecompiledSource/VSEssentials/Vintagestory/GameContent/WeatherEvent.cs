using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherEvent
{
	public WeatherEventConfig config;

	protected SimplexNoise strengthNoiseGen;

	private ICoreAPI api;

	private LCGRandom rand;

	public WeatherEventState State = new WeatherEventState();

	public float Strength;

	internal float hereChance;

	public bool AllowStop { get; set; } = true;


	public bool ShouldStop(float rainfall, float temperature)
	{
		if (config.getWeight(rainfall, temperature) <= 0f)
		{
			return AllowStop;
		}
		return false;
	}

	public WeatherEvent(ICoreAPI api, WeatherEventConfig config, int index, LCGRandom rand, int seed)
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
		State.PrecType = config.PrecType;
		WeatherEventState state = State;
		LightningConfig lightning = config.Lightning;
		state.NearThunderRate = ((lightning != null) ? (lightning.NearThunderRate / 100f) : 0f);
		WeatherEventState state2 = State;
		LightningConfig lightning2 = config.Lightning;
		state2.LightningRate = ((lightning2 != null) ? (lightning2.LightningRate / 100f) : 0f);
		WeatherEventState state3 = State;
		LightningConfig lightning3 = config.Lightning;
		state3.DistantThunderRate = ((lightning3 != null) ? (lightning3.DistantThunderRate / 100f) : 0f);
		State.LightningMinTemp = config.Lightning?.MinTemperature ?? 0f;
	}

	public virtual void Update(float dt)
	{
		if (strengthNoiseGen != null)
		{
			double timeAxis = api.World.Calendar.TotalDays / 10.0;
			Strength = State.BaseStrength + (float)GameMath.Clamp(strengthNoiseGen.Noise(0.0, timeAxis), 0.0, 1.0);
		}
	}

	public virtual string GetWindName()
	{
		return config.Name;
	}

	internal void updateHereChance(float rainfall, float temperature)
	{
		hereChance = config.getWeight(rainfall, temperature);
	}
}
