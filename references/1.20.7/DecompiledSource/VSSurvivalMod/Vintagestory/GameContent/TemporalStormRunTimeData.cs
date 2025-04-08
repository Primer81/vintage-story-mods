using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class TemporalStormRunTimeData
{
	public string spawnPatternCode = "default";

	public bool nowStormActive;

	public int stormDayNotify = 99;

	public float stormGlitchStrength;

	public double stormActiveTotalDays;

	public double nextStormTotalDays = 5.0;

	public EnumTempStormStrength nextStormStrength;

	public double nextStormStrDouble;

	public Dictionary<string, int> rareSpawnCount;
}
