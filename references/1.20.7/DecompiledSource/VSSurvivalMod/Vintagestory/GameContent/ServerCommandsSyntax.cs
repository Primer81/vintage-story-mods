using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ServerCommandsSyntax
{
	[ProtoMember(1)]
	public ChatCommandSyntax[] Commands;
}
