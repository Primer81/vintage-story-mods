using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class WeatherState
{
	[ProtoMember(1)]
	public int RegionX;

	[ProtoMember(2)]
	public int RegionZ;

	[ProtoMember(3)]
	public WeatherPatternState NewPattern;

	[ProtoMember(4)]
	public WeatherPatternState OldPattern;

	[ProtoMember(5)]
	public WindPatternState WindPattern;

	[ProtoMember(6)]
	public WeatherEventState WeatherEvent;

	[ProtoMember(7)]
	public float TransitionDelay;

	[ProtoMember(8)]
	public float Weight;

	[ProtoMember(9)]
	public bool Transitioning;

	[ProtoMember(10)]
	public bool updateInstant;

	[ProtoMember(11)]
	public double LastUpdateTotalHours;

	[ProtoMember(12)]
	public long LcgWorldSeed;

	[ProtoMember(13)]
	public long LcgMapGenSeed;

	[ProtoMember(14)]
	public long LcgCurrentSeed;

	[ProtoMember(15)]
	public SnowAccumSnapshot[] SnowAccumSnapshots;

	[ProtoMember(16)]
	public int Ringarraycursor;
}
