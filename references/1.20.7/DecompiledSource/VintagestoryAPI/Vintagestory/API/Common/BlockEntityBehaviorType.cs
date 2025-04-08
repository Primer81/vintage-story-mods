using Newtonsoft.Json;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

/// <summary>
/// Allows adding behaviors for block entities. Remember, block entities are specific instances of blocks placed within the world.
/// </summary>
/// <example>
/// <code language="json">
///             "entityClass": "Brake",
///             "entityBehaviors": [
///             	{ "name": "MPBrake" },
///             	{ "name": "Animatable" }
///             ],
/// </code>
/// </example>
[DocumentAsJson]
public class BlockEntityBehaviorType
{
	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The ID for this block entity behavior.
	/// </summary>
	[JsonProperty]
	public string Name;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// A set of properties specific to the block entity behavior class.
	/// </summary>
	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject properties;
}
