using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface ISeatInstSupplier
{
	IMountableSeat CreateSeat(IMountable mountable, string seatId, SeatConfig config = null);
}
