using System;
using System.IO;

namespace Vintagestory.API.Datastructures;

public class BoolAttribute : ScalarAttribute<bool>, IAttribute
{
	public BoolAttribute()
	{
	}

	public BoolAttribute(bool value)
	{
		base.value = value;
	}

	public void FromBytes(BinaryReader stream)
	{
		value = stream.ReadBoolean();
	}

	public void ToBytes(BinaryWriter stream)
	{
		stream.Write(value);
	}

	public int GetAttributeId()
	{
		return 9;
	}

	public override string ToJsonToken()
	{
		if (!value)
		{
			return "false";
		}
		return "true";
	}

	public IAttribute Clone()
	{
		return new BoolAttribute(value);
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
