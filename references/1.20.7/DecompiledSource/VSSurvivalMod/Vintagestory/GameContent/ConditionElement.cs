using Newtonsoft.Json;

namespace Vintagestory.GameContent;

[JsonConverter(typeof(DialogueConditionComponentJsonConverter))]
public class ConditionElement
{
	public string Variable;

	public string IsValue;

	public bool Invert;
}
