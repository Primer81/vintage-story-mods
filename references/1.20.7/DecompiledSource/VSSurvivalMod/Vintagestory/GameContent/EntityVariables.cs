using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class EntityVariables
{
	[ProtoMember(1)]
	public Dictionary<string, string> Variables = new Dictionary<string, string>();

	public string this[string key]
	{
		get
		{
			Variables.TryGetValue(key, out var value);
			return value;
		}
		set
		{
			Variables[key] = value;
		}
	}
}
