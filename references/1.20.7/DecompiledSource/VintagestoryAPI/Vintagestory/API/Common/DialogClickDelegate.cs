using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

/// <summary>
/// The delegate for a dialogue click.
/// </summary>
/// <param name="byPlayer">The player that clicked the dialogue.</param>
/// <param name="widgetId">The internal name of the Widget.</param>
public delegate void DialogClickDelegate(IServerPlayer byPlayer, string widgetId);
