using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class LightningFlashPacket
{
	public Vec3d Pos;

	public int Seed;
}
