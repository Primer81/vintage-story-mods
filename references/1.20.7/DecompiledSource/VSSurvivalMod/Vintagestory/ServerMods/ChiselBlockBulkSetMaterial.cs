using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

public class ChiselBlockBulkSetMaterial : ModSystem
{
	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		sapi.ChatCommands.GetOrCreate("we").BeginSubCommand("microblock").WithDescription("Recalculate microblocks")
			.RequiresPrivilege("worldedit")
			.BeginSubCommand("recalc")
			.WithDescription("Recalc")
			.RequiresPlayer()
			.HandleWith(OnMicroblockCmd)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult OnMicroblockCmd(TextCommandCallingArgs args)
	{
		WorldEditWorkspace workspace = sapi.ModLoader.GetModSystem<Vintagestory.ServerMods.WorldEdit.WorldEdit>().GetWorkSpace(args.Caller.Player.PlayerUID);
		if (workspace == null || workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Success("Select an area with worldedit first");
		}
		int i = 0;
		sapi.World.BlockAccessor.WalkBlocks(workspace.StartMarker, workspace.EndMarker, delegate(Block block, int x, int y, int z)
		{
			if (block is BlockMicroBlock && sapi.World.BlockAccessor.GetBlockEntity(new BlockPos(x, y, z)) is BlockEntityMicroBlock blockEntityMicroBlock)
			{
				blockEntityMicroBlock.RebuildCuboidList();
				blockEntityMicroBlock.MarkDirty(redrawOnClient: true);
				i++;
			}
		});
		return TextCommandResult.Success(i + " microblocks recalced");
	}
}
