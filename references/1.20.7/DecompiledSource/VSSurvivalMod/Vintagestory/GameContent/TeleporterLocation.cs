using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class TeleporterLocation
{
	public string SourceName;

	public BlockPos SourcePos;

	public string TargetName;

	public BlockPos TargetPos;
}
