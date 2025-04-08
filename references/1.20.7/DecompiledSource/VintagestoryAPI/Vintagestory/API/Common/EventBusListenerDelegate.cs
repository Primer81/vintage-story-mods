using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common;

/// <summary>
/// For handling events on the event bus
/// </summary>
/// <param name="eventName"></param>
/// <param name="handling">Set to EnumHandling.Last to stop further propagation of the event</param>
/// <param name="data"></param>
public delegate void EventBusListenerDelegate(string eventName, ref EnumHandling handling, IAttribute data);
