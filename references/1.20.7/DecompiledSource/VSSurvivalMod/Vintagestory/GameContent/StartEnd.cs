using System;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class StartEnd : IEquatable<StartEnd>
{
	[ProtoMember(1)]
	public Vec3d Start;

	[ProtoMember(2)]
	public Vec3d End;

	public bool Equals(StartEnd other)
	{
		if (other != null && Start.Equals(other.Start))
		{
			return End.Equals(other.End);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as StartEnd);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Start, End);
	}
}
