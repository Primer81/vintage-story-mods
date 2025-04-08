using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class TimeSwitchState
{
	[ProtoMember(1)]
	public bool Enabled;

	[ProtoMember(2)]
	public bool Activated;

	[ProtoMember(3)]
	public string playerUID;

	[ProtoMember(4)]
	public int baseChunkX;

	[ProtoMember(5)]
	public int baseChunkZ;

	[ProtoMember(6)]
	public int size = 3;

	[ProtoMember(7)]
	public int forcedY;

	[ProtoMember(8)]
	public string failureReason = "";

	public TimeSwitchState()
	{
	}

	public TimeSwitchState(string uid)
	{
		playerUID = uid;
	}
}
