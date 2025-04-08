using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WaypointMapComponent : MapComponent
{
	private Vec2f viewPos = new Vec2f();

	private Vec4f color = new Vec4f();

	private Waypoint waypoint;

	private int waypointIndex;

	private Matrixf mvMat = new Matrixf();

	private WaypointMapLayer wpLayer;

	private bool mouseOver;

	public static float IconScale = 0.85f;

	private GuiDialogEditWayPoint editWpDlg;

	public WaypointMapComponent(int waypointIndex, Waypoint waypoint, WaypointMapLayer wpLayer, ICoreClientAPI capi)
		: base(capi)
	{
		this.waypointIndex = waypointIndex;
		this.waypoint = waypoint;
		this.wpLayer = wpLayer;
		ColorUtil.ToRGBAVec4f(waypoint.Color, ref color);
	}

	public override void Render(GuiElementMap map, float dt)
	{
		map.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
		if (waypoint.Pinned)
		{
			map.Api.Render.PushScissor(null);
			map.ClampButPreserveAngle(ref viewPos, 2);
		}
		else if (viewPos.X < -10f || viewPos.Y < -10f || (double)viewPos.X > map.Bounds.OuterWidth + 10.0 || (double)viewPos.Y > map.Bounds.OuterHeight + 10.0)
		{
			return;
		}
		float x = (float)(map.Bounds.renderX + (double)viewPos.X);
		float y = (float)(map.Bounds.renderY + (double)viewPos.Y);
		ICoreClientAPI api = map.Api;
		IShaderProgram prog = api.Render.GetEngineShader(EnumShaderProgram.Gui);
		prog.Uniform("rgbaIn", color);
		prog.Uniform("extraGlow", 0);
		prog.Uniform("applyColor", 0);
		prog.Uniform("noTexture", 0f);
		float hover = (float)(mouseOver ? 6 : 0) - 1.5f * Math.Max(1f, 1f / map.ZoomLevel);
		if (!wpLayer.texturesByIcon.TryGetValue(waypoint.Icon, out var tex))
		{
			wpLayer.texturesByIcon.TryGetValue("circle", out tex);
		}
		if (tex != null)
		{
			prog.BindTexture2D("tex2d", tex.TextureId, 0);
			prog.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
			mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale((float)tex.Width + hover, (float)tex.Height + hover, 0f)
				.Scale(0.5f * IconScale, 0.5f * IconScale, 0f);
			Matrixf shadowMvMat = mvMat.Clone().Scale(1.25f, 1.25f, 1.25f);
			prog.Uniform("rgbaIn", new Vec4f(0f, 0f, 0f, 0.6f));
			prog.UniformMatrix("modelViewMatrix", shadowMvMat.Values);
			api.Render.RenderMesh(wpLayer.quadModel);
			prog.Uniform("rgbaIn", color);
			prog.UniformMatrix("modelViewMatrix", mvMat.Values);
			api.Render.RenderMesh(wpLayer.quadModel);
		}
		if (waypoint.Pinned)
		{
			map.Api.Render.PopScissor();
		}
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		Vec2f viewPos = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
		double x = (double)viewPos.X + mapElem.Bounds.renderX;
		double y = (double)viewPos.Y + mapElem.Bounds.renderY;
		if (waypoint.Pinned)
		{
			mapElem.ClampButPreserveAngle(ref viewPos, 2);
			x = (double)viewPos.X + mapElem.Bounds.renderX;
			y = (double)viewPos.Y + mapElem.Bounds.renderY;
			x = (float)GameMath.Clamp(x, mapElem.Bounds.renderX + 2.0, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2.0);
			y = (float)GameMath.Clamp(y, mapElem.Bounds.renderY + 2.0, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2.0);
		}
		double dX = (double)args.X - x;
		double dY = (double)args.Y - y;
		float size = RuntimeEnv.GUIScale * 8f;
		if (mouseOver = Math.Abs(dX) < (double)size && Math.Abs(dY) < (double)size)
		{
			string text = Lang.Get("Waypoint {0}", waypointIndex) + "\n" + waypoint.Title;
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
		mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
		double x = (double)viewPos.X + mapElem.Bounds.renderX;
		double y = (double)viewPos.Y + mapElem.Bounds.renderY;
		if (waypoint.Pinned)
		{
			mapElem.ClampButPreserveAngle(ref viewPos, 2);
			x = (double)viewPos.X + mapElem.Bounds.renderX;
			y = (double)viewPos.Y + mapElem.Bounds.renderY;
			x = (float)GameMath.Clamp(x, mapElem.Bounds.renderX + 2.0, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2.0);
			y = (float)GameMath.Clamp(y, mapElem.Bounds.renderY + 2.0, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2.0);
		}
		double value = (double)args.X - x;
		double dY = (double)args.Y - y;
		float size = RuntimeEnv.GUIScale * 8f;
		if (!(Math.Abs(value) < (double)size) || !(Math.Abs(dY) < (double)size))
		{
			return;
		}
		if (editWpDlg != null)
		{
			editWpDlg.TryClose();
			editWpDlg.Dispose();
		}
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative && capi.World.Player.Entity.Controls.ShiftKey)
		{
			BlockPos pos = waypoint.Position.AsBlockPos;
			capi.SendChatMessage($"/tp ={pos.X} {pos.Y} ={pos.Z}");
			mapElem.prevPlayerPos.Set(pos);
			mapElem.CenterMapTo(pos);
		}
		else
		{
			GuiDialogWorldMap mapdlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
			editWpDlg = new GuiDialogEditWayPoint(capi, mapdlg.MapLayers.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer, waypoint, waypointIndex);
			editWpDlg.TryOpen();
			editWpDlg.OnClosed += delegate
			{
				capi.Gui.RequestFocus(mapdlg);
			};
		}
		args.Handled = true;
	}
}
