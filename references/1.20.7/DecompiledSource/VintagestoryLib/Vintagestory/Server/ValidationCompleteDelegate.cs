using Vintagestory.API.Server;

namespace Vintagestory.Server;

public delegate void ValidationCompleteDelegate(EnumServerResponse response, string playerEntitlements, string errorReason);
