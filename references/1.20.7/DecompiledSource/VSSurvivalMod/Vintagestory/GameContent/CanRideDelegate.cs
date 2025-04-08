using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public delegate bool CanRideDelegate(IMountableSeat seat, out string errorMessage);
