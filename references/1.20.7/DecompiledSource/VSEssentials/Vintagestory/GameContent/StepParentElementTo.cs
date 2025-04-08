using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class StepParentElementTo : ModelTransform
{
	[JsonProperty]
	public string ElementName;
}
