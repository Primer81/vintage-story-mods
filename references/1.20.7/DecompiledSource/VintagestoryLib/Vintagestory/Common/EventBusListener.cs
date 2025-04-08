using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class EventBusListener
{
	public EventBusListenerDelegate handler;

	public double priority;

	public string filterByName;
}
