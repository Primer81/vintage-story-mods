using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class GameEffectsPacket
{
	public bool RainAndFogActive;

	public bool GlitchPresent;

	public bool SlomoActive;
}
