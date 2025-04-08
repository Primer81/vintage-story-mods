using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public static class MountableUtil
{
	public static bool IsMountedBy(this IMountable mountable, Entity entity)
	{
		IMountableSeat[] seats = mountable.Seats;
		for (int i = 0; i < seats.Length; i++)
		{
			if (seats[i].Passenger == entity)
			{
				return true;
			}
		}
		return false;
	}

	public static IMountableSeat GetSeatOfMountedEntity(this IMountable mountable, Entity entity)
	{
		IMountableSeat[] seats = mountable.Seats;
		foreach (IMountableSeat seat in seats)
		{
			if (seat.Passenger == entity)
			{
				return seat;
			}
		}
		return null;
	}

	public static bool IsBeingControlled(this IMountable mountable)
	{
		IMountableSeat[] seats = mountable.Seats;
		foreach (IMountableSeat seat in seats)
		{
			if (seat.CanControl && seat.Passenger != null)
			{
				return true;
			}
		}
		return false;
	}
}
