using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VSCreativeMod;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods.WorldEdit;

public class ImportTool : ToolBase
{
	private readonly Random _rand;

	public override bool ScrollEnabled => true;

	public string BlockDataFilename
	{
		get
		{
			return workspace.StringValues["std.fileNames"];
		}
		set
		{
			workspace.StringValues["std.fileNames"] = value;
		}
	}

	public EnumOrigin Origin
	{
		get
		{
			return (EnumOrigin)workspace.IntValues["std.pasteToolOrigin"];
		}
		set
		{
			workspace.IntValues["std.pasteToolOrigin"] = (int)value;
		}
	}

	public EnumReplaceMode ReplaceMode
	{
		get
		{
			return (EnumReplaceMode)workspace.IntValues["std.importReplaceMode"];
		}
		set
		{
			workspace.IntValues["std.importReplaceMode"] = (int)value;
		}
	}

	public bool RandomRotate
	{
		get
		{
			return workspace.IntValues["std.pasteToolRandomRotate"] > 0;
		}
		set
		{
			workspace.IntValues["std.pasteToolRandomRotate"] = (value ? 1 : 0);
		}
	}

	public bool UpdatePreviewPos
	{
		get
		{
			return workspace.IntValues["std.updatePreviewPos"] > 0;
		}
		set
		{
			workspace.IntValues["std.updatePreviewPos"] = (value ? 1 : 0);
		}
	}

	public bool PreviewAtPlayer
	{
		get
		{
			return workspace.IntValues["std.previewAtPlayer"] > 0;
		}
		set
		{
			workspace.IntValues["std.previewAtPlayer"] = (value ? 1 : 0);
		}
	}

	public override Vec3i Size
	{
		get
		{
			if (workspace.PreviewBlockData == null)
			{
				return new Vec3i(0, 0, 0);
			}
			BlockSchematic bd = workspace.PreviewBlockData;
			return new Vec3i(bd.SizeX, bd.SizeY, bd.SizeZ);
		}
	}

	public ImportTool()
	{
	}

	public ImportTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccessor)
		: base(workspace, blockAccessor)
	{
		if (!workspace.StringValues.ContainsKey("std.fileNames"))
		{
			BlockDataFilename = "\n\n\n";
		}
		if (!workspace.IntValues.ContainsKey("std.pasteToolOrigin"))
		{
			Origin = EnumOrigin.BottomCenter;
		}
		if (!workspace.IntValues.ContainsKey("std.importReplaceMode"))
		{
			ReplaceMode = EnumReplaceMode.Replaceable;
		}
		if (!workspace.IntValues.ContainsKey("std.pasteToolRandomRotate"))
		{
			RandomRotate = false;
		}
		if (!workspace.IntValues.ContainsKey("std.updatePreviewPos"))
		{
			UpdatePreviewPos = true;
		}
		if (!workspace.IntValues.ContainsKey("std.previewAtPlayer"))
		{
			PreviewAtPlayer = false;
		}
		if (workspace.clipboardBlockData != null)
		{
			workspace.PreviewBlockData = workspace.clipboardBlockData;
		}
		_rand = new Random();
	}

	public void SetBlockDatas(WorldEdit worldEdit, BlockSchematic schematics)
	{
		workspace.PreviewBlockData = schematics;
		workspace.ResendBlockHighlights();
	}

	private void Move(Vec3i dir)
	{
		if (!(workspace.PreviewPos == null) && workspace.PreviewBlockData != null)
		{
			Vec3i vec = dir * workspace.StepSize;
			workspace.PreviewPos.Add(vec);
			workspace.SendPreviewOriginToClient(workspace.PreviewPos, workspace.previewBlocks.subDimensionId);
		}
	}

	public override bool OnWorldEditCommand(WorldEdit worldEdit, TextCommandCallingArgs callerArgs)
	{
		IServerPlayer player = (IServerPlayer)callerArgs.Caller.Player;
		CmdArgs args = callerArgs.RawArgs;
		switch (args[0])
		{
		case "south":
		case "north":
		case "west":
		case "down":
		case "east":
		case "up":
		{
			BlockFacing facing = BlockFacing.FromCode(args[0]);
			Move(facing.Normali);
			return true;
		}
		case "look":
		{
			Vec3f lookVec = player.Entity.SidedPos.GetViewVector();
			BlockFacing facing2 = BlockFacing.FromVector(lookVec.X, lookVec.Y, lookVec.Z);
			Move(facing2.Normali);
			return true;
		}
		case "imc":
			if (workspace.clipboardBlockData != null)
			{
				workspace.PreviewBlockData = workspace.clipboardBlockData;
				if (workspace.PreviewPos == null)
				{
					if (!PreviewAtPlayer && workspace.StartMarker != null)
					{
						workspace.PreviewPos = workspace.StartMarker.Copy();
					}
					else
					{
						workspace.PreviewPos = workspace.world.PlayerByUid(workspace.PlayerUID).Entity.Pos.AsBlockPos;
					}
				}
				WorldEdit.Good(player, "Ok, using copied blockdata");
				workspace.CreatePreview(workspace.PreviewBlockData, workspace.PreviewPos);
			}
			else
			{
				WorldEdit.Good(player, "No copied block data available");
			}
			return true;
		case "ims":
		{
			string orCreateDataPath = worldEdit.sapi.GetOrCreateDataPath("WorldEdit");
			string filename = (args[1].StartsWith(Path.DirectorySeparatorChar) ? args[1].Substring(1) : args[1]);
			string filepath = Path.Combine(orCreateDataPath, filename);
			string error = null;
			workspace.PreviewBlockData = BlockSchematic.LoadFromFile(filepath, ref error);
			if (workspace.PreviewPos == null)
			{
				if (!PreviewAtPlayer && workspace.StartMarker != null)
				{
					workspace.PreviewPos = workspace.StartMarker.Copy();
				}
				else
				{
					workspace.PreviewPos = worldEdit.sapi.World.PlayerByUid(workspace.PlayerUID).Entity.Pos.AsBlockPos;
				}
			}
			workspace.CreatePreview(workspace.PreviewBlockData, workspace.PreviewPos);
			return true;
		}
		case "imo":
			Origin = EnumOrigin.BottomCenter;
			if (args.Length > 1)
			{
				int.TryParse(args[1], out var origin);
				if (Enum.IsDefined(typeof(EnumOrigin), origin))
				{
					Origin = (EnumOrigin)origin;
				}
			}
			workspace.ResendBlockHighlights();
			WorldEdit.Good(player, "Paste origin " + Origin.ToString() + " set.");
			return true;
		case "tm":
			ReplaceMode = EnumReplaceMode.Replaceable;
			if (args.Length > 1)
			{
				int replaceable = 0;
				int.TryParse(args[1], out replaceable);
				if (Enum.IsDefined(typeof(EnumReplaceMode), replaceable))
				{
					ReplaceMode = (EnumReplaceMode)replaceable;
				}
			}
			WorldEdit.Good(player, "Replace mode " + ReplaceMode.ToString() + " set.");
			workspace.ResendBlockHighlights();
			return true;
		case "imrrand":
			RandomRotate = args.Length > 1 && (args[1] == "1" || args[1] == "true" || args[1] == "on");
			WorldEdit.Good(player, "Random rotation now " + (RandomRotate ? "on" : "off"));
			SetRandomAngle(worldEdit.sapi.World);
			workspace.ResendBlockHighlights();
			return true;
		case "imn":
			workspace.ResendBlockHighlights();
			break;
		case "imr":
		{
			if (workspace.PreviewBlockData == null)
			{
				WorldEdit.Bad(player, "Please define a block data source first.");
				return true;
			}
			int angle = 90;
			if (args.Length > 1 && !int.TryParse(args[1], out angle))
			{
				WorldEdit.Bad(player, "Invalid Angle (not a number)");
				return true;
			}
			if (angle < 0)
			{
				angle += 360;
			}
			if (angle != 0 && angle != 90 && angle != 180 && angle != 270)
			{
				WorldEdit.Bad(player, "Invalid Angle, allowed values are -270, -180, -90, 0, 90, 180 and 270");
				return true;
			}
			workspace.PreviewBlockData.TransformWhilePacked(worldEdit.sapi.World, Origin, angle);
			workspace.ResendBlockHighlights();
			return true;
		}
		case "imflip":
			if (workspace.PreviewBlockData == null)
			{
				WorldEdit.Bad(player, "Please define a block data source first.");
				return true;
			}
			workspace.PreviewBlockData.TransformWhilePacked(worldEdit.sapi.World, EnumOrigin.BottomCenter, 0, EnumAxis.Y);
			workspace.ResendBlockHighlights();
			WorldEdit.Good(player, "Ok, imported sources flipped");
			return true;
		case "immirror":
		{
			if (workspace.PreviewBlockData == null)
			{
				WorldEdit.Bad(player, "Please define a block data source first.");
				return true;
			}
			EnumAxis axis = EnumAxis.X;
			if (args.PopWord().ToLowerInvariant() == "z")
			{
				axis = EnumAxis.Z;
			}
			workspace.PreviewBlockData.TransformWhilePacked(worldEdit.sapi.World, EnumOrigin.BottomCenter, 0, axis);
			workspace.ResendBlockHighlights();
			WorldEdit.Good(player, "Ok, imported sources mirrored around " + axis.ToString() + " axis");
			return true;
		}
		case "place":
		case "apply":
		case "commit":
			if (Commit(worldEdit))
			{
				return true;
			}
			break;
		case "updatePreviewPos":
			UpdatePreviewPos = args[1] == "1";
			WorldEdit.Good(player, workspace.ToolName + " Update preview position on right click set to " + UpdatePreviewPos);
			return true;
		case "previewAtPlayer":
			PreviewAtPlayer = args[1] == "1";
			WorldEdit.Good(player, workspace.ToolName + " Spawn the preview at player set to " + PreviewAtPlayer);
			return true;
		}
		return false;
	}

	private bool Commit(WorldEdit worldEdit)
	{
		if (workspace.PreviewBlockData == null)
		{
			return true;
		}
		workspace.PreviewBlockData.Init(ba);
		workspace.PreviewBlockData.Place(ba, worldEdit.sapi.World, workspace.PreviewPos, ReplaceMode, WorldEdit.ReplaceMetaBlocks);
		workspace.PreviewBlockData.PlaceDecors(ba, workspace.PreviewPos);
		ba.Commit();
		workspace.PreviewBlockData.PlaceEntitiesAndBlockEntities(ba, worldEdit.sapi.World, workspace.PreviewPos, workspace.PreviewBlockData.BlockCodes, workspace.PreviewBlockData.ItemCodes, replaceBlockEntities: false, null, 0, null, WorldEdit.ReplaceMetaBlocks);
		ba.CommitBlockEntityData();
		if (RandomRotate)
		{
			SetRandomAngle(worldEdit.sapi.World);
		}
		workspace.ResendBlockHighlights();
		return true;
	}

	public override void OnInteractStart(WorldEdit worldEdit, BlockSelection blockSelection)
	{
		if (blockSelection != null && workspace.PreviewBlockData != null)
		{
			if (UpdatePreviewPos)
			{
				BlockPos origin = workspace.PreviewBlockData.GetStartPos(blockSelection.Position.UpCopy(), Origin);
				workspace.PreviewPos = origin;
			}
			Commit(worldEdit);
		}
	}

	public override void OnAttackStart(WorldEdit worldEdit, BlockSelection blockSelection)
	{
		if (blockSelection != null && workspace.PreviewBlockData != null)
		{
			BlockPos origin = workspace.PreviewBlockData.GetStartPos(blockSelection.Position.UpCopy(), Origin);
			workspace.PreviewPos = origin;
			if (workspace.previewBlocks == null)
			{
				workspace.ResendBlockHighlights();
			}
			else
			{
				workspace.SendPreviewOriginToClient(workspace.PreviewPos, workspace.previewBlocks.subDimensionId);
			}
		}
	}

	private void SetRandomAngle(IWorldAccessor world)
	{
		workspace.PreviewBlockData.TransformWhilePacked(world, EnumOrigin.BottomCenter, _rand.Next(4) * 90);
	}

	public override void Load(ICoreAPI api)
	{
		string folder = api.GetOrCreateDataPath("WorldEdit");
		List<string> names = (from p in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
			select p.Substring(folder.Length)).ToList();
		string join = string.Join("||", names);
		BlockDataFilename = join + "\n" + join + "\n";
		if (workspace.PreviewBlockData != null && workspace.PreviewPos == null)
		{
			if (!PreviewAtPlayer && workspace.StartMarker != null)
			{
				workspace.PreviewPos = workspace.StartMarker.Copy();
			}
			else
			{
				workspace.PreviewPos = api.World.PlayerByUid(workspace.PlayerUID).Entity.Pos.AsBlockPos;
			}
		}
	}

	public override void Unload(ICoreAPI api)
	{
		workspace.previewBlocks.ClearChunks();
		workspace.PreviewPos = null;
		workspace.PreviewBlockData = null;
	}

	public override List<SkillItem> GetAvailableModes(ICoreClientAPI capi)
	{
		string move = EnumWeToolMode.Move.ToString();
		string rotate = EnumWeToolMode.Rotate.ToString();
		List<SkillItem> list = new List<SkillItem>();
		list.Add(new SkillItem
		{
			Name = Lang.Get(move),
			Code = new AssetLocation(move)
		});
		list.Add(new SkillItem
		{
			Texture = capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/worldedit/rotate.svg"), 48, 48, 5, -1),
			Name = Lang.Get(rotate),
			Code = new AssetLocation(rotate)
		});
		list[0].WithIcon(capi, "move");
		return list;
	}
}
