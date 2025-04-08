using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class WindPatternState
{
	public int Index;

	public float BaseStrength;

	public double ActiveUntilTotalHours;
}
