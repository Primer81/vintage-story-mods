using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class OreMapComponent : MapComponent
{
	private Vec2f viewPos = new Vec2f();

	private Vec4f color = new Vec4f();

	private PropickReading reading;

	private int waypointIndex;

	private Matrixf mvMat = new Matrixf();

	private OreMapLayer oreLayer;

	private bool mouseOver;

	public static float IconScale = 0.85f;

	public string filterByOreCode;

	public OreMapComponent(int waypointIndex, PropickReading reading, OreMapLayer wpLayer, ICoreClientAPI capi, string filterByOreCode)
		: base(capi)
	{
		this.waypointIndex = waypointIndex;
		this.reading = reading;
		oreLayer = wpLayer;
		int col = GuiStyle.DamageColorGradient[(int)Math.Min(99.0, reading.HighestReading * 150.0)];
		if (filterByOreCode != null)
		{
			col = GuiStyle.DamageColorGradient[(int)Math.Min(99.0, reading.OreReadings[filterByOreCode].TotalFactor * 150.0)];
		}
		color = new Vec4f();
		ColorUtil.ToRGBAVec4f(col, ref color);
		color.W = 1f;
	}

	public override void Render(GuiElementMap map, float dt)
	{
		map.TranslateWorldPosToViewPos(reading.Position, ref viewPos);
		if (!(viewPos.X < -10f) && !(viewPos.Y < -10f) && !((double)viewPos.X > map.Bounds.OuterWidth + 10.0) && !((double)viewPos.Y > map.Bounds.OuterHeight + 10.0))
		{
			float x = (float)(map.Bounds.renderX + (double)viewPos.X);
			float y = (float)(map.Bounds.renderY + (double)viewPos.Y);
			ICoreClientAPI api = map.Api;
			IShaderProgram prog = api.Render.GetEngineShader(EnumShaderProgram.Gui);
			prog.Uniform("rgbaIn", color);
			prog.Uniform("extraGlow", 0);
			prog.Uniform("applyColor", 0);
			prog.Uniform("noTexture", 0f);
			LoadedTexture tex = oreLayer.oremapTexture;
			float hover = (float)(mouseOver ? 6 : 0) - 1.5f * Math.Max(1f, 1f / map.ZoomLevel);
			if (tex != null)
			{
				prog.BindTexture2D("tex2d", tex.TextureId, 0);
				prog.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
				mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale((float)tex.Width + hover, (float)tex.Height + hover, 0f)
					.Scale(0.5f * IconScale, 0.5f * IconScale, 0f);
				Matrixf shadowMvMat = mvMat.Clone().Scale(1.25f, 1.25f, 1.25f);
				prog.Uniform("rgbaIn", new Vec4f(0f, 0f, 0f, 0.7f));
				prog.UniformMatrix("modelViewMatrix", shadowMvMat.Values);
				api.Render.RenderMesh(oreLayer.quadModel);
				prog.Uniform("rgbaIn", color);
				prog.UniformMatrix("modelViewMatrix", mvMat.Values);
				api.Render.RenderMesh(oreLayer.quadModel);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		Vec2f viewPos = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(reading.Position, ref viewPos);
		double x = (double)viewPos.X + mapElem.Bounds.renderX;
		double y = (double)viewPos.Y + mapElem.Bounds.renderY;
		double dX = (double)args.X - x;
		double dY = (double)args.Y - y;
		float size = RuntimeEnv.GUIScale * 8f;
		if (mouseOver = Math.Abs(dX) < (double)size && Math.Abs(dY) < (double)size)
		{
			Dictionary<string, string> pageCodes = capi.ModLoader.GetModSystem<ModSystemOreMap>().prospectingMetaData.PageCodes;
			string text = reading.ToHumanReadable(capi.Settings.String["language"], pageCodes);
			hoverText.AppendLine(text);
		}
	}

	public override void OnMouseUpOnElement(MouseEvent args, GuiElementMap mapElem)
	{
		if (args.Button != EnumMouseButton.Right)
		{
			return;
		}
		Vec2f viewPos = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(reading.Position, ref viewPos);
		double x = (double)viewPos.X + mapElem.Bounds.renderX;
		double y = (double)viewPos.Y + mapElem.Bounds.renderY;
		double value = (double)args.X - x;
		double dY = (double)args.Y - y;
		float size = RuntimeEnv.GUIScale * 8f;
		if (Math.Abs(value) < (double)size && Math.Abs(dY) < (double)size)
		{
			GuiDialogConfirm guiDialogConfirm = new GuiDialogConfirm(capi, Lang.Get("prospecting-reading-confirmdelete"), onConfirmDone);
			guiDialogConfirm.TryOpen();
			GuiDialogWorldMap mapdlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
			guiDialogConfirm.OnClosed += delegate
			{
				capi.Gui.RequestFocus(mapdlg);
			};
			args.Handled = true;
		}
	}

	private void onConfirmDone(bool confirm)
	{
		if (confirm)
		{
			oreLayer.Delete(capi.World.Player, waypointIndex);
		}
	}
}
