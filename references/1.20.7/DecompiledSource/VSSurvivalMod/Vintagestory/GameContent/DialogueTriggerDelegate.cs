using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public delegate int DialogueTriggerDelegate(EntityAgent triggeringEntity, string value, JsonObject data);
