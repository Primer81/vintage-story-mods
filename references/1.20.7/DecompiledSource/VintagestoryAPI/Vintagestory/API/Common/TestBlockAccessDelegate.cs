namespace Vintagestory.API.Common;

public delegate EnumWorldAccessResponse TestBlockAccessDelegate(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response);
