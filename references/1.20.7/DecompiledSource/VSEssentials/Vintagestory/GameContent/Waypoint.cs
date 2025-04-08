using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class Waypoint
{
	[ProtoMember(6)]
	public Vec3d Position = new Vec3d();

	[ProtoMember(10)]
	public string Title;

	[ProtoMember(9)]
	public string Text;

	[ProtoMember(1)]
	public int Color;

	[ProtoMember(2)]
	public string Icon = "circle";

	[ProtoMember(7)]
	public bool ShowInWorld;

	[ProtoMember(5)]
	public bool Pinned;

	[ProtoMember(4)]
	public string OwningPlayerUid;

	[ProtoMember(3)]
	public int OwningPlayerGroupId = -1;

	[ProtoMember(8)]
	public bool Temporary;

	[ProtoMember(11)]
	public string Guid { get; set; }
}
