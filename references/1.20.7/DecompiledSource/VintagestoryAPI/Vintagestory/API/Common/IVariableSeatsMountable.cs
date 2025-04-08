namespace Vintagestory.API.Common;

public interface IVariableSeatsMountable : IMountable
{
	void RegisterSeat(SeatConfig seat);

	void RemoveSeat(string seatId);
}
