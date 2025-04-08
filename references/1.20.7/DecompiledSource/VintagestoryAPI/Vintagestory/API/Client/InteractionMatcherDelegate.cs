using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public delegate bool InteractionMatcherDelegate(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection);
