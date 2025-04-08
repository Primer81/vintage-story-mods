using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemHammer : Item
{
	private SkillItem[] toolModes;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (capi == null)
		{
			return;
		}
		toolModes = ObjectCacheUtil.GetOrCreate(api, "hammerToolModes", () => new SkillItem[6]
		{
			new SkillItem
			{
				Code = new AssetLocation("hit"),
				Name = Lang.Get("Heavy Hit")
			}.WithIcon(capi, DrawHit),
			new SkillItem
			{
				Code = new AssetLocation("upsetup"),
				Name = Lang.Get("Upset Up")
			}.WithIcon(capi, delegate(Context cr, int x, int y, float w, float h, double[] c)
			{
				DrawUpset(cr, x, y, w, h, c, 0.0);
			}),
			new SkillItem
			{
				Code = new AssetLocation("upsetright"),
				Name = Lang.Get("Upset Right")
			}.WithIcon(capi, delegate(Context cr, int x, int y, float w, float h, double[] c)
			{
				DrawUpset(cr, x, y, w, h, c, 1.5707963705062866);
			}),
			new SkillItem
			{
				Code = new AssetLocation("upsetdown"),
				Name = Lang.Get("Upset Down")
			}.WithIcon(capi, delegate(Context cr, int x, int y, float w, float h, double[] c)
			{
				DrawUpset(cr, x, y, w, h, c, 3.1415927410125732);
			}),
			new SkillItem
			{
				Code = new AssetLocation("upsetleft"),
				Name = Lang.Get("Upset Left")
			}.WithIcon(capi, delegate(Context cr, int x, int y, float w, float h, double[] c)
			{
				DrawUpset(cr, x, y, w, h, c, 4.71238899230957);
			}),
			new SkillItem
			{
				Code = new AssetLocation("split"),
				Name = Lang.Get("Split")
			}.WithIcon(capi, DrawSplit)
		});
	}

	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		if ((byEntity as EntityPlayer)?.EntitySelection != null)
		{
			return "hammerhit";
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		int i = 0;
		while (toolModes != null && i < toolModes.Length)
		{
			toolModes[i]?.Dispose();
			i++;
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
		}
		else if ((byEntity as EntityPlayer)?.Player != null)
		{
			if (!(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAnvilPart) && !(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockAnvil))
			{
				base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
			}
			else
			{
				handling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override void OnHeldActionAnimStart(ItemSlot slot, EntityAgent byEntity, EnumHandInteract type)
	{
		EntityPlayer obj = byEntity as EntityPlayer;
		IPlayer byPlayer = obj.Player;
		BlockSelection blockSel = obj.BlockSelection;
		if (type != EnumHandInteract.HeldItemAttack || blockSel == null)
		{
			return;
		}
		BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (be is BlockEntityAnvilPart beap)
		{
			if (beap.TestReadyToMerge())
			{
				startHitAction(slot, byEntity, merge: true);
			}
		}
		else if (byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockAnvil && be is BlockEntityAnvil bea)
		{
			bea.OnBeginUse(byPlayer, blockSel);
			startHitAction(slot, byEntity, merge: false);
		}
	}

	private void startHitAction(ItemSlot slot, EntityAgent byEntity, bool merge)
	{
		string anim = GetHeldTpHitAnimation(slot, byEntity);
		float framesound = CollectibleBehaviorAnimationAuthoritative.getSoundAtFrame(byEntity, anim);
		float framehitaction = CollectibleBehaviorAnimationAuthoritative.getHitDamageAtFrame(byEntity, anim);
		slot.Itemstack.TempAttributes.SetBool("isAnvilAction", value: true);
		byEntity.AnimManager.GetAnimationState(anim);
		byEntity.AnimManager.RegisterFrameCallback(new AnimFrameCallback
		{
			Animation = anim,
			Frame = framesound,
			Callback = delegate
			{
				strikeAnvilSound(byEntity, merge);
			}
		});
		byEntity.AnimManager.RegisterFrameCallback(new AnimFrameCallback
		{
			Animation = anim,
			Frame = framehitaction,
			Callback = delegate
			{
				strikeAnvil(byEntity, slot);
			}
		});
	}

	public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		if (!slot.Itemstack.TempAttributes.GetBool("isAnvilAction"))
		{
			return base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason);
		}
		if (cancelReason == EnumItemUseCancelReason.Death || cancelReason == EnumItemUseCancelReason.Destroyed)
		{
			slot.Itemstack.TempAttributes.SetBool("isAnvilAction", value: false);
			return true;
		}
		return false;
	}

	public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		if (!slot.Itemstack.TempAttributes.GetBool("isAnvilAction"))
		{
			return base.OnHeldAttackStep(secondsUsed, slot, byEntity, blockSelection, entitySel);
		}
		if (blockSelection == null)
		{
			return false;
		}
		if (byEntity.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is BlockEntityAnvilPart beap && !beap.TestReadyToMerge())
		{
			return false;
		}
		string animCode = GetHeldTpHitAnimation(slot, byEntity);
		return byEntity.AnimManager.IsAnimationActive(animCode);
	}

	protected virtual void strikeAnvil(EntityAgent byEntity, ItemSlot slot)
	{
		IPlayer byPlayer = (byEntity as EntityPlayer).Player;
		if (byPlayer == null)
		{
			return;
		}
		BlockSelection blockSel = byPlayer.CurrentBlockSelection;
		if (blockSel == null)
		{
			return;
		}
		BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (be is BlockEntityAnvilPart bep)
		{
			bep.OnHammerHitOver(byPlayer, blockSel.HitPosition);
		}
		else
		{
			if (!(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockAnvil) || !(be is BlockEntityAnvil bea))
			{
				return;
			}
			if (api.World.Side == EnumAppSide.Client)
			{
				bea.OnUseOver(byPlayer, blockSel.SelectionBoxIndex);
			}
		}
		slot.Itemstack?.TempAttributes.SetBool("isAnvilAction", value: false);
	}

	protected virtual void strikeAnvilSound(EntityAgent byEntity, bool merge)
	{
		IPlayer byPlayer = (byEntity as EntityPlayer).Player;
		if (byPlayer != null && byPlayer.CurrentBlockSelection != null)
		{
			byPlayer.Entity.World.PlaySoundAt(merge ? new AssetLocation("sounds/effect/anvilmergehit") : new AssetLocation("sounds/effect/anvilhit"), byPlayer.Entity, byPlayer, 0.9f + (float)byEntity.World.Rand.NextDouble() * 0.2f, 16f, 0.35f);
		}
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		if (blockSel == null)
		{
			return null;
		}
		if (!(forPlayer.Entity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockAnvil))
		{
			return null;
		}
		return toolModes;
	}

	private void DrawSplit(Context cr, int x, int y, float width, float height, double[] colordoubles)
	{
		Pattern pattern = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float w = 220f;
		float h = 182f;
		float scale = Math.Min(width / w, height / h);
		matrix.Translate((float)x + Math.Max(0f, (width - w * scale) / 2f), (float)y + Math.Max(0f, (height - h * scale) / 2f));
		matrix.Scale(scale, scale);
		cr.Matrix = matrix;
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(59.0, 105.003906);
		cr.LineTo(1.0, 105.003906);
		cr.LineTo(1.0, 182.003906);
		cr.LineTo(101.5, 182.003906);
		cr.ClosePath();
		cr.MoveTo(59.0, 105.003906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(59.0, 105.003906);
		cr.LineTo(1.0, 105.003906);
		cr.LineTo(1.0, 182.003906);
		cr.LineTo(101.5, 182.003906);
		cr.ClosePath();
		cr.MoveTo(59.0, 105.003906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(161.5, 105.003906);
		cr.LineTo(219.5, 105.003906);
		cr.LineTo(219.5, 182.003906);
		cr.LineTo(119.0, 182.003906);
		cr.ClosePath();
		cr.MoveTo(161.5, 105.003906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(161.5, 105.003906);
		cr.LineTo(219.5, 105.003906);
		cr.LineTo(219.5, 182.003906);
		cr.LineTo(119.0, 182.003906);
		cr.ClosePath();
		cr.MoveTo(161.5, 105.003906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(106.648438, 118.003906);
		cr.CurveTo(104.824219, 113.109375, 103.148438, 108.210938, 101.621094, 103.316406);
		cr.CurveTo(100.0625, 98.421875, 98.644531, 93.523438, 97.25, 88.628906);
		cr.CurveTo(95.914063, 83.730469, 94.53125, 78.835938, 93.371094, 73.941406);
		cr.CurveTo(92.183594, 69.042969, 91.214844, 64.148438, 90.199219, 59.253906);
		cr.CurveTo(89.710938, 56.804688, 89.003906, 54.355469, 88.191406, 51.90625);
		cr.CurveTo(87.378906, 49.460938, 86.460938, 47.011719, 85.734375, 44.5625);
		cr.CurveTo(85.015625, 42.117188, 84.542969, 39.667969, 84.503906, 37.21875);
		cr.CurveTo(84.453125, 34.773438, 84.820313, 32.324219, 85.5, 29.875);
		cr.CurveTo(86.886719, 24.980469, 89.078125, 20.085938, 92.378906, 15.1875);
		cr.CurveTo(95.769531, 10.292969, 99.902344, 5.394531, 106.648438, 0.5);
		cr.LineTo(111.351563, 0.5);
		cr.CurveTo(118.097656, 5.394531, 122.230469, 10.292969, 125.621094, 15.1875);
		cr.CurveTo(128.921875, 20.085938, 131.113281, 24.980469, 132.5, 29.875);
		cr.CurveTo(133.179688, 32.324219, 133.546875, 34.773438, 133.496094, 37.21875);
		cr.CurveTo(133.457031, 39.667969, 132.984375, 42.117188, 132.265625, 44.5625);
		cr.CurveTo(131.539063, 47.011719, 130.621094, 49.460938, 129.808594, 51.90625);
		cr.CurveTo(128.996094, 54.355469, 128.289063, 56.804688, 127.800781, 59.253906);
		cr.CurveTo(126.785156, 64.148438, 125.820313, 69.042969, 124.628906, 73.941406);
		cr.CurveTo(123.46875, 78.835938, 122.085938, 83.730469, 120.75, 88.628906);
		cr.CurveTo(119.355469, 93.523438, 117.9375, 98.421875, 116.378906, 103.316406);
		cr.CurveTo(114.855469, 108.210938, 113.175781, 113.105469, 111.351563, 118.003906);
		cr.ClosePath();
		cr.MoveTo(106.648438, 118.003906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 4.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(130.261719, 118.003906);
		cr.LineTo(165.261719, 70.003906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 4.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(51.25, 70.003906);
		cr.LineTo(86.25, 118.003906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Restore();
	}

	private void DrawUpset(Context cr, int x, int y, float width, float height, double[] colordoubles, double rot)
	{
		Pattern pattern = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float w = 91f;
		float h = 170f;
		float scale = Math.Min(width / w, height / h);
		matrix.Translate((float)x + Math.Max(0f, (width - w * scale) / 2f), (float)y + Math.Max(0f, (height - h * scale) / 2f));
		matrix.Scale(scale, scale);
		matrix.Translate(w / 2f, h / 2f);
		matrix.Rotate(rot);
		matrix.Translate((0f - w) / 2f, (0f - h) / 2f);
		cr.Matrix = matrix;
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(91.0, 124.667969);
		cr.CurveTo(91.0, 149.519531, 70.851563, 169.667969, 46.0, 169.667969);
		cr.CurveTo(21.148438, 169.667969, 1.0, 149.519531, 1.0, 124.667969);
		cr.CurveTo(1.0, 99.816406, 21.148438, 79.667969, 46.0, 79.667969);
		cr.CurveTo(70.851563, 79.667969, 91.0, 99.816406, 91.0, 124.667969);
		cr.ClosePath();
		cr.MoveTo(91.0, 124.667969);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(91.0, 124.667969);
		cr.CurveTo(91.0, 149.519531, 70.851563, 169.667969, 46.0, 169.667969);
		cr.CurveTo(21.148438, 169.667969, 1.0, 149.519531, 1.0, 124.667969);
		cr.CurveTo(1.0, 99.816406, 21.148438, 79.667969, 46.0, 79.667969);
		cr.CurveTo(70.851563, 79.667969, 91.0, 99.816406, 91.0, 124.667969);
		cr.ClosePath();
		cr.MoveTo(91.0, 124.667969);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(82.265625, 21.296875);
		cr.LineTo(47.160156, 0.5);
		cr.LineTo(11.734375, 21.296875);
		cr.LineTo(26.457031, 21.296875);
		cr.LineTo(26.457031, 71.335938);
		cr.LineTo(67.808594, 71.335938);
		cr.LineTo(67.808594, 21.296875);
		cr.ClosePath();
		cr.MoveTo(82.265625, 21.296875);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Restore();
	}

	private void DrawHit(Context cr, int x, int y, float width, float height, double[] colordoubles)
	{
		Pattern pattern = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float w = 227f;
		float h = 218f;
		float scale = Math.Min(width / w, height / h);
		matrix.Translate((float)x + Math.Max(0f, (width - w * scale) / 2f), (float)y + Math.Max(0f, (height - h * scale) / 2f));
		matrix.Scale(scale, scale);
		cr.Matrix = matrix;
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(159.96875, 110.125);
		cr.CurveTo(159.96875, 134.976563, 139.824219, 155.125, 114.96875, 155.125);
		cr.CurveTo(90.117188, 155.125, 69.96875, 134.976563, 69.96875, 110.125);
		cr.CurveTo(69.96875, 85.273438, 90.117188, 65.125, 114.96875, 65.125);
		cr.CurveTo(139.824219, 65.125, 159.96875, 85.273438, 159.96875, 110.125);
		cr.ClosePath();
		cr.MoveTo(159.96875, 110.125);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(159.96875, 110.125);
		cr.CurveTo(159.96875, 134.976563, 139.824219, 155.125, 114.96875, 155.125);
		cr.CurveTo(90.117188, 155.125, 69.96875, 134.976563, 69.96875, 110.125);
		cr.CurveTo(69.96875, 85.273438, 90.117188, 65.125, 114.96875, 65.125);
		cr.CurveTo(139.824219, 65.125, 159.96875, 85.273438, 159.96875, 110.125);
		cr.ClosePath();
		cr.MoveTo(159.96875, 110.125);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(110.71875, 0.0);
		cr.LineTo(119.21875, 0.0);
		cr.LineTo(119.21875, 52.0);
		cr.LineTo(110.71875, 52.0);
		cr.ClosePath();
		cr.MoveTo(110.71875, 0.0);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(110.71875, 0.0);
		cr.LineTo(119.21875, 0.0);
		cr.LineTo(119.21875, 52.0);
		cr.LineTo(110.71875, 52.0);
		cr.ClosePath();
		cr.MoveTo(110.71875, 0.0);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(110.71875, 164.710938);
		cr.LineTo(119.21875, 164.710938);
		cr.LineTo(119.21875, 216.710938);
		cr.LineTo(110.71875, 216.710938);
		cr.ClosePath();
		cr.MoveTo(110.71875, 164.710938);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(110.71875, 164.710938);
		cr.LineTo(119.21875, 164.710938);
		cr.LineTo(119.21875, 216.710938);
		cr.LineTo(110.71875, 216.710938);
		cr.ClosePath();
		cr.MoveTo(110.71875, 164.710938);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(173.804688, 105.875);
		cr.LineTo(225.804688, 105.875);
		cr.LineTo(225.804688, 114.375);
		cr.LineTo(173.804688, 114.375);
		cr.ClosePath();
		cr.MoveTo(173.804688, 105.875);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(173.804688, 105.875);
		cr.LineTo(225.804688, 105.875);
		cr.LineTo(225.804688, 114.375);
		cr.LineTo(173.804688, 114.375);
		cr.ClosePath();
		cr.MoveTo(173.804688, 105.875);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(0.0, 105.375);
		cr.LineTo(52.0, 105.375);
		cr.LineTo(52.0, 113.875);
		cr.LineTo(0.0, 113.875);
		cr.ClosePath();
		cr.MoveTo(0.0, 105.375);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(0.0, 105.375);
		cr.LineTo(52.0, 105.375);
		cr.LineTo(52.0, 113.875);
		cr.LineTo(0.0, 113.875);
		cr.ClosePath();
		cr.MoveTo(0.0, 105.375);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(173.757813, 68.78125);
		cr.LineTo(167.75, 62.769531);
		cr.LineTo(204.515625, 26.003906);
		cr.LineTo(210.527344, 32.011719);
		cr.ClosePath();
		cr.MoveTo(173.757813, 68.78125);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(173.757813, 68.78125);
		cr.LineTo(167.75, 62.769531);
		cr.LineTo(204.515625, 26.003906);
		cr.LineTo(210.527344, 32.011719);
		cr.ClosePath();
		cr.MoveTo(173.757813, 68.78125);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(-0.7071, -0.7071, 0.7071, -0.7071, 289.3736, 214.6403);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(32.007813, 190.707031);
		cr.LineTo(25.996094, 184.699219);
		cr.LineTo(62.757813, 147.925781);
		cr.LineTo(68.769531, 153.933594);
		cr.ClosePath();
		cr.MoveTo(32.007813, 190.707031);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(32.007813, 190.707031);
		cr.LineTo(25.996094, 184.699219);
		cr.LineTo(62.757813, 147.925781);
		cr.LineTo(68.769531, 153.933594);
		cr.ClosePath();
		cr.MoveTo(32.007813, 190.707031);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(-0.7072, -0.707, 0.707, -0.7072, -38.8126, 322.5648);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(68.78125, 62.773438);
		cr.LineTo(62.769531, 68.78125);
		cr.LineTo(26.0, 32.015625);
		cr.LineTo(32.011719, 26.003906);
		cr.ClosePath();
		cr.MoveTo(68.78125, 62.773438);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(68.78125, 62.773438);
		cr.LineTo(62.769531, 68.78125);
		cr.LineTo(26.0, 32.015625);
		cr.LineTo(32.011719, 26.003906);
		cr.ClosePath();
		cr.MoveTo(68.78125, 62.773438);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(-0.7071, 0.7071, -0.7071, -0.7071, 114.4105, 47.3931);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(210.527344, 184.6875);
		cr.LineTo(204.515625, 190.695313);
		cr.LineTo(167.75, 153.921875);
		cr.LineTo(173.761719, 147.910156);
		cr.ClosePath();
		cr.MoveTo(210.527344, 184.6875);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		cr.FillRule = FillRule.Winding;
		cr.FillPreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 1.0;
		cr.MiterLimit = 10.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(colordoubles[0], colordoubles[1], colordoubles[2], colordoubles[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(210.527344, 184.6875);
		cr.LineTo(204.515625, 190.695313);
		cr.LineTo(167.75, 153.921875);
		cr.LineTo(173.761719, 147.910156);
		cr.ClosePath();
		cr.MoveTo(210.527344, 184.6875);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(-0.7072, 0.707, -0.707, -0.7072, 442.6037, 155.3283);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Restore();
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		return slot.Itemstack.Attributes.GetInt("toolMode");
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
	}
}
