using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class VariableData
{
	[ProtoMember(1)]
	public EntityVariables GlobalVariables = new EntityVariables();

	[ProtoMember(2)]
	public Dictionary<string, EntityVariables> PlayerVariables = new Dictionary<string, EntityVariables>();

	[ProtoMember(4)]
	public Dictionary<string, EntityVariables> GroupVariables = new Dictionary<string, EntityVariables>();
}
