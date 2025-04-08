using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VSSurvivalMod.Systems.ChiselModes;

public class RenameChiselMode : ChiselMode
{
	public override DrawSkillIconDelegate DrawAction(ICoreClientAPI capi)
	{
		return Drawedit_svg;
	}

	public override bool Apply(BlockEntityChisel chiselEntity, IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak, byte currentMaterialIndex)
	{
		_ = (IClientWorldAccessor)chiselEntity.Api.World;
		string prevName = chiselEntity.BlockName;
		GuiDialogBlockEntityTextInput guiDialogBlockEntityTextInput = new GuiDialogBlockEntityTextInput(Lang.Get("Block name"), chiselEntity.Pos, chiselEntity.BlockName, chiselEntity.Api as ICoreClientAPI, new TextAreaConfig
		{
			MaxWidth = 500
		});
		guiDialogBlockEntityTextInput.OnTextChanged = delegate(string text)
		{
			chiselEntity.BlockName = text;
		};
		guiDialogBlockEntityTextInput.OnCloseCancel = delegate
		{
			chiselEntity.BlockName = prevName;
		};
		guiDialogBlockEntityTextInput.TryOpen();
		return false;
	}

	public void Drawedit_svg(Context cr, int x, int y, float width, float height, double[] rgba)
	{
		Pattern pattern = null;
		Matrix matrix = cr.Matrix;
		cr.Save();
		float w = 382f;
		float h = 200f;
		float scale = Math.Min(width / w, height / h);
		matrix.Translate((float)x + Math.Max(0f, (width - w * scale) / 2f), (float)y + Math.Max(0f, (height - h * scale) / 2f));
		matrix.Scale(scale, scale);
		cr.Matrix = matrix;
		cr.Operator = Operator.Over;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(10.628906, 10.628906);
		cr.LineTo(371.445313, 10.628906);
		cr.LineTo(371.445313, 189.617188);
		cr.LineTo(10.628906, 189.617188);
		cr.ClosePath();
		cr.MoveTo(10.628906, 10.628906);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(75.972656, 47.5625);
		cr.LineTo(75.972656, 150.789063);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(52.308594, 49.4375);
		cr.LineTo(98.714844, 49.4375);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Operator = Operator.Over;
		cr.LineWidth = 9.0;
		cr.MiterLimit = 4.0;
		cr.LineCap = LineCap.Butt;
		cr.LineJoin = LineJoin.Miter;
		pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
		cr.SetSource(pattern);
		cr.NewPath();
		cr.MoveTo(53.265625, 151.5);
		cr.LineTo(99.667969, 151.5);
		cr.Tolerance = 0.1;
		cr.Antialias = Antialias.Default;
		matrix = new Matrix(3.543307, 0.0, 0.0, 3.543307, -219.495455, -129.753943);
		pattern.Matrix = matrix;
		cr.StrokePreserve();
		pattern?.Dispose();
		cr.Restore();
	}
}
