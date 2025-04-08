using System;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class ActivateBlockAction : EntityActionBase
{
	[JsonProperty]
	private AssetLocation targetBlockCode;

	[JsonProperty]
	private float searchRange;

	[JsonProperty]
	private string activateArgs;

	public Vec3d ExactTarget = new Vec3d();

	public override string Type => "activateblock";

	[JsonProperty]
	public double targetX
	{
		get
		{
			return ExactTarget.X;
		}
		set
		{
			ExactTarget.X = value;
		}
	}

	[JsonProperty]
	public double targetY
	{
		get
		{
			return ExactTarget.Y;
		}
		set
		{
			ExactTarget.Y = value;
		}
	}

	[JsonProperty]
	public double targetZ
	{
		get
		{
			return ExactTarget.Z;
		}
		set
		{
			ExactTarget.Z = value;
		}
	}

	public ActivateBlockAction()
	{
	}

	public ActivateBlockAction(EntityActivitySystem vas, AssetLocation targetBlockCode, float searchRange, string activateArgs, Vec3d exacttarget)
	{
		base.vas = vas;
		this.targetBlockCode = targetBlockCode;
		this.searchRange = searchRange;
		this.activateArgs = activateArgs;
		ExactTarget = exacttarget;
	}

	public override void Start(EntityActivity act)
	{
		BlockPos targetPos = getTarget(vas.Entity.Api, vas.Entity.ServerPos.XYZ);
		ExecutionHasFailed = targetPos == null;
		if (targetPos != null)
		{
			Vec3f targetVec = new Vec3f();
			targetVec.Set((float)((double)targetPos.X + 0.5 - vas.Entity.ServerPos.X), (float)((double)targetPos.Y + 0.5 - vas.Entity.ServerPos.Y), (float)((double)targetPos.Z + 0.5 - vas.Entity.ServerPos.Z));
			vas.Entity.ServerPos.Yaw = (float)Math.Atan2(targetVec.X, targetVec.Z);
			Block block = vas.Entity.Api.World.BlockAccessor.GetBlock(targetPos);
			BlockSelection blockSel = new BlockSelection
			{
				Block = block,
				Position = targetPos,
				HitPosition = new Vec3d(0.5, 0.5, 0.5),
				Face = BlockFacing.NORTH
			};
			ITreeAttribute args = ((activateArgs == null) ? null : (TreeAttribute.FromJson(activateArgs) as ITreeAttribute));
			block.Activate(vas.Entity.World, new Caller
			{
				Entity = vas.Entity,
				Type = EnumCallerType.Entity,
				Pos = vas.Entity.Pos.XYZ
			}, blockSel, args);
		}
	}

	private BlockPos getTarget(ICoreAPI api, Vec3d fromPos)
	{
		if (ExactTarget.Length() > 0.0)
		{
			Vec3d exactTarget = ExactTarget.Clone();
			if (vas != null)
			{
				exactTarget.Add(vas.ActivityOffset);
			}
			if (exactTarget.DistanceTo(fromPos) < searchRange)
			{
				return exactTarget.AsBlockPos;
			}
			return null;
		}
		float range = GameMath.Clamp(searchRange, -10f, 10f);
		BlockPos minPos = fromPos.Clone().Add(0f - range, -1.0, 0f - range).AsBlockPos;
		BlockPos maxPos = fromPos.Clone().Add(range, 1.0, range).AsBlockPos;
		BlockPos targetPos = null;
		api.World.BlockAccessor.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			if (!(targetBlockCode == null) && block.WildCardMatch(targetBlockCode))
			{
				targetPos = new BlockPos(x, y, z);
			}
		}, centerOrder: true);
		return targetPos;
	}

	public override string ToString()
	{
		if (ExactTarget.Length() > 0.0)
		{
			Vec3d exactTarget = ExactTarget.Clone();
			if (vas != null)
			{
				exactTarget.Add(vas.ActivityOffset);
			}
			return "Activate block at " + exactTarget?.ToString() + ". Args: " + activateArgs;
		}
		return "Activate nearest block " + targetBlockCode.ToShortString() + " within " + searchRange + " blocks. Args: " + activateArgs;
	}

	public override void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 300.0, 25.0);
		ElementBounds bc = ElementBounds.Fixed(0.0, 0.0, 65.0, 20.0);
		singleComposer.AddStaticText("Block Code", CairoFont.WhiteDetailText(), b).AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "targetBlockCode").AddStaticText("OR exact x/y/z Pos", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 5.0))
			.AddTextInput(bc = bc.FlatCopy().FixedUnder(b, -3.0), null, CairoFont.WhiteDetailText(), "x")
			.AddTextInput(bc = bc.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "y")
			.AddTextInput(bc = bc.CopyOffsetedSibling(70.0), null, CairoFont.WhiteDetailText(), "z")
			.AddSmallButton("Tp to", () => onClickTpTo(capi), bc = bc.CopyOffsetedSibling(70.0), EnumButtonStyle.Small)
			.AddSmallButton("Insert Player Pos", () => onClickPlayerPos(capi, singleComposer), b = b.FlatCopy().WithFixedPosition(0.0, 0.0).FixedUnder(bc, 2.0), EnumButtonStyle.Small)
			.AddStaticText("Within Range (capped to 10 blocks)", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 30.0))
			.AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "searchRange")
			.AddStaticText("Activation Arguments", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 10.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "activateArgs");
		GuiComposer composer = singleComposer;
		composer.GetNumberInput("searchRange").SetValue(searchRange);
		composer.GetTextInput("targetBlockCode").SetValue(targetBlockCode?.ToShortString());
		composer.GetTextInput("activateArgs").SetValue(activateArgs);
		composer.GetTextInput("x").SetValue((ExactTarget?.X).ToString() ?? "");
		composer.GetTextInput("y").SetValue((ExactTarget?.Y).ToString() ?? "");
		composer.GetTextInput("z").SetValue((ExactTarget?.Z).ToString() ?? "");
	}

	private bool onClickTpTo(ICoreClientAPI capi)
	{
		double x = ExactTarget.X;
		double y = ExactTarget.Y;
		double z = ExactTarget.Z;
		if (vas != null)
		{
			x += (double)vas.ActivityOffset.X;
			y += (double)vas.ActivityOffset.Y;
			z += (double)vas.ActivityOffset.Z;
		}
		capi.SendChatMessage($"/tp ={x} ={y} ={z}");
		return false;
	}

	private bool onClickPlayerPos(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		Vec3d plrPos = capi.World.Player.Entity.Pos.XYZ;
		singleComposer.GetTextInput("x").SetValue(Math.Round(plrPos.X, 1).ToString() ?? "");
		singleComposer.GetTextInput("y").SetValue(Math.Round(plrPos.Y, 1).ToString() ?? "");
		singleComposer.GetTextInput("z").SetValue(Math.Round(plrPos.Z, 1).ToString() ?? "");
		return true;
	}

	public override IEntityAction Clone()
	{
		return new ActivateBlockAction(vas, targetBlockCode, searchRange, activateArgs, ExactTarget);
	}

	public override bool StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ExactTarget = new Vec3d(singleComposer.GetTextInput("x").GetText().ToDouble(), singleComposer.GetTextInput("y").GetText().ToDouble(), singleComposer.GetTextInput("z").GetText().ToDouble());
		searchRange = singleComposer.GetTextInput("searchRange").GetText().ToFloat();
		targetBlockCode = new AssetLocation(singleComposer.GetTextInput("targetBlockCode").GetText());
		activateArgs = singleComposer.GetTextInput("activateArgs").GetText();
		return true;
	}

	public override void OnVisualize(ActivityVisualizer visualizer)
	{
		if (!(visualizer.CurrentPos == null))
		{
			BlockPos target = getTarget(visualizer.Api, visualizer.CurrentPos);
			if (target != null)
			{
				visualizer.LineTo(visualizer.CurrentPos, target.ToVec3d().Add(0.5, 0.5, 0.5), ColorUtil.ColorFromRgba(255, 0, 0, 255));
			}
		}
	}
}
