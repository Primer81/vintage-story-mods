using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class PlacedBeam
{
	[ProtoMember(1)]
	public Vec3f Start;

	[ProtoMember(2)]
	public Vec3f End;

	[ProtoMember(3)]
	public int BlockId;

	[ProtoMember(4)]
	public int FacingIndex;

	private Block block;

	public float SlumpPerMeter;

	public Block Block
	{
		get
		{
			return block;
		}
		set
		{
			block = value;
			SlumpPerMeter = block.Attributes?["slumpPerMeter"].AsFloat() ?? 0f;
		}
	}
}
