using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

public class UpgradeTasks : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("upgradeTasks").RegisterMessageType<UpgradeHerePacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		this.api = api;
		capi = api;
		api.Input.InWorldAction += Input_InWorldAction;
	}

	private void Input_InWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		if (action != EnumEntityAction.RightMouseDown)
		{
			return;
		}
		BlockSelection bs = capi.World.Player.CurrentBlockSelection;
		if (bs == null)
		{
			return;
		}
		Block block = api.World.BlockAccessor.GetBlock(bs.Position);
		if (!(block.Code == null))
		{
			string[] parts = block.Code.Path.Split(new char[1] { '-' }, 3);
			if ((parts[0] == "clayplanter" || parts[0] == "flowerpot") && parts.Length >= 3)
			{
				capi.Network.GetChannel("upgradeTasks").SendPacket(new UpgradeHerePacket
				{
					Pos = bs.Position
				});
			}
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Network.GetChannel("upgradeTasks").SetMessageHandler<UpgradeHerePacket>(didUseBlock);
		api.Event.DidBreakBlock += Event_DidBreakBlock;
		_ = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("we").BeginSubCommand("chisel").BeginSubCommand("upgradearea")
			.WithDescription("Fixes chiseled blocks, pots and planters broken in v1.13")
			.HandleWith(OnUpgradeCmd)
			.EndSubCommand()
			.BeginSubCommand("setchiselblockmat")
			.WithDescription("Sets the material of a currently looked at chisel block to the material in the active hands")
			.HandleWith(OnSetChiselMat)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult OnSetChiselMat(TextCommandCallingArgs textCommandCallingArgs)
	{
		IPlayer player = textCommandCallingArgs.Caller.Player;
		BlockPos pos = player.CurrentBlockSelection?.Position;
		if (pos == null)
		{
			return TextCommandResult.Success("Look at a block first");
		}
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityChisel bechisel))
		{
			return TextCommandResult.Success("Not looking at a chiseled block");
		}
		Block block = player.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Block;
		if (block == null)
		{
			return TextCommandResult.Success("You need a block in your active hand");
		}
		for (int i = 0; i < bechisel.BlockIds.Length; i++)
		{
			bechisel.BlockIds[i] = block.Id;
		}
		bechisel.MarkDirty(redrawOnClient: true);
		return TextCommandResult.Success("Ok material set");
	}

	private TextCommandResult OnUpgradeCmd(TextCommandCallingArgs textCommandCallingArgs)
	{
		WorldEditWorkspace workspace = api.ModLoader.GetModSystem<Vintagestory.ServerMods.WorldEdit.WorldEdit>().GetWorkSpace(textCommandCallingArgs.Caller.Player.PlayerUID);
		if (workspace == null || workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Success("Select an area with worldedit first");
		}
		int startx = Math.Min(workspace.StartMarker.X, workspace.EndMarker.X);
		int endx = Math.Max(workspace.StartMarker.X, workspace.EndMarker.X);
		int starty = Math.Min(workspace.StartMarker.Y, workspace.EndMarker.Y);
		int endy = Math.Max(workspace.StartMarker.Y, workspace.EndMarker.Y);
		int startz = Math.Min(workspace.StartMarker.Z, workspace.EndMarker.Z);
		int endZ = Math.Max(workspace.StartMarker.Z, workspace.EndMarker.Z);
		BlockPos pos = new BlockPos(textCommandCallingArgs.Caller.Player.Entity.Pos.Dimension);
		Dictionary<string, Block> blocksByName = new Dictionary<string, Block>();
		foreach (Block block in api.World.Blocks)
		{
			if (!block.IsMissing)
			{
				blocksByName[block.GetHeldItemName(new ItemStack(block))] = block;
			}
		}
		int graniteBlockId = api.World.GetBlock(new AssetLocation("rock-granite")).Id;
		for (int x = startx; x < endx; x++)
		{
			for (int y = starty; y < endy; y++)
			{
				for (int z = startz; z < endZ; z++)
				{
					pos.Set(x, y, z);
					Block block2 = api.World.BlockAccessor.GetBlock(pos);
					if (block2 is BlockChisel)
					{
						BlockEntityChisel bechisel = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
						if (bechisel.BlockIds != null && bechisel.BlockIds.Length != 0 && bechisel.BlockIds[0] == graniteBlockId)
						{
							Block matblock = null;
							if (blocksByName.TryGetValue(bechisel.BlockName, out matblock))
							{
								bechisel.BlockIds[0] = matblock.Id;
								bechisel.MarkDirty(redrawOnClient: true);
							}
						}
					}
					if (block2 is BlockPlantContainer)
					{
						FixOldPlantContainers(pos);
					}
				}
			}
		}
		return TextCommandResult.Success();
	}

	private void didUseBlock(IServerPlayer fromPlayer, UpgradeHerePacket networkMessage)
	{
		FixOldPlantContainers(networkMessage.Pos);
	}

	private void Event_DidBreakBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
	{
		DropPlantContainer(oldblockId, blockSel.Position);
	}

	private void FixOldPlantContainers(BlockPos pos)
	{
		string[] parts = api.World.BlockAccessor.GetBlock(pos).Code.Path.Split(new char[1] { '-' }, 3);
		if ((parts[0] == "clayplanter" || parts[0] == "flowerpot") && parts.Length >= 3)
		{
			Block potblock = api.World.GetBlock(new AssetLocation(parts[0] + "-" + parts[1]));
			api.World.BlockAccessor.SetBlock(potblock.Id, pos);
			BlockEntityPlantContainer bepcont = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPlantContainer;
			Block plantBlock = null;
			if (plantBlock == null)
			{
				plantBlock = api.World.GetBlock(new AssetLocation("flower-" + parts[2] + "-free"));
			}
			if (plantBlock == null)
			{
				plantBlock = api.World.GetBlock(new AssetLocation("sapling-" + parts[2] + "-free"));
			}
			if (plantBlock == null)
			{
				plantBlock = api.World.GetBlock(new AssetLocation("mushroom-" + parts[2] + "-normal-free"));
			}
			if (plantBlock != null)
			{
				ItemStack plantStack = new ItemStack(plantBlock);
				bepcont.TrySetContents(plantStack);
			}
		}
	}

	private void DropPlantContainer(int blockid, BlockPos pos)
	{
		Block block = api.World.GetBlock(blockid);
		if (block.Code == null)
		{
			return;
		}
		string[] parts = block.Code.Path.Split(new char[1] { '-' }, 3);
		if (parts.Length >= 3 && (parts[0] == "clayplanter" || parts[1] == "flowerpot") && parts.Length >= 3)
		{
			ItemStack potStack = new ItemStack(api.World.GetBlock(new AssetLocation(parts[0] + "-" + parts[1])));
			api.World.SpawnItemEntity(potStack, pos);
			Block plantBlock = null;
			if (plantBlock == null)
			{
				plantBlock = api.World.GetBlock(new AssetLocation("flower-" + parts[2]));
			}
			if (plantBlock == null)
			{
				plantBlock = api.World.GetBlock(new AssetLocation("sapling-" + parts[2]));
			}
			if (plantBlock == null)
			{
				plantBlock = api.World.GetBlock(new AssetLocation("mushroom-" + parts[2] + "-normal"));
			}
			if (plantBlock != null)
			{
				ItemStack plantStack = new ItemStack(plantBlock);
				api.World.SpawnItemEntity(plantStack, pos);
			}
		}
	}
}
