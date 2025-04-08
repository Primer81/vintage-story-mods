using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiElementMap : GuiElement
{
	public List<MapLayer> mapLayers;

	public bool IsDragingMap;

	public float ZoomLevel = 1f;

	internal Vec3d prevPlayerPos = new Vec3d();

	public Cuboidi chunkViewBoundsBefore = new Cuboidi();

	public OnViewChangedDelegate viewChanged;

	private bool snapToPlayer;

	public Cuboidd CurrentBlockViewBounds = new Cuboidd();

	private GuiDialogWorldMap worldmapdlg;

	private float tkeyDeltaX;

	private float tkeyDeltaY;

	private float skeyDeltaX;

	private float skeyDeltaY;

	private int prevMouseX;

	private int prevMouseY;

	private List<Vec2i> nowVisible = new List<Vec2i>();

	private List<Vec2i> nowHidden = new List<Vec2i>();

	public ICoreClientAPI Api => api;

	private bool dialogHasFocus
	{
		get
		{
			if (worldmapdlg.Focused)
			{
				return worldmapdlg.DialogType == EnumDialogType.Dialog;
			}
			return false;
		}
	}

	public GuiElementMap(List<MapLayer> mapLayers, ICoreClientAPI capi, GuiDialogWorldMap worldmapdlg, ElementBounds bounds, bool snapToPlayer)
		: base(capi, bounds)
	{
		this.mapLayers = mapLayers;
		this.snapToPlayer = snapToPlayer;
		this.worldmapdlg = worldmapdlg;
		prevPlayerPos.X = api.World.Player.Entity.Pos.X;
		prevPlayerPos.Z = api.World.Player.Entity.Pos.Z;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		chunkViewBoundsBefore = new Cuboidi();
		BlockPos start = api.World.Player.Entity.Pos.AsBlockPos;
		CurrentBlockViewBounds = new Cuboidd((double)start.X - Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)start.Z - Bounds.InnerHeight / 2.0 / (double)ZoomLevel, (double)start.X + Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)start.Z + Bounds.InnerHeight / 2.0 / (double)ZoomLevel);
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.PushScissor(Bounds);
		for (int i = 0; i < mapLayers.Count; i++)
		{
			mapLayers[i].Render(this, deltaTime);
		}
		api.Render.PopScissor();
		api.Render.CheckGlError();
	}

	public override void PostRenderInteractiveElements(float deltaTime)
	{
		base.PostRenderInteractiveElements(deltaTime);
		EntityPlayer plr = api.World.Player.Entity;
		double diffx = plr.Pos.X - prevPlayerPos.X;
		double diffz = plr.Pos.Z - prevPlayerPos.Z;
		if (Math.Abs(diffx) > 0.0002 || Math.Abs(diffz) > 0.0002)
		{
			if (snapToPlayer)
			{
				EntityPos start = api.World.Player.Entity.Pos;
				CurrentBlockViewBounds.X1 = start.X - Bounds.InnerWidth / 2.0 / (double)ZoomLevel;
				CurrentBlockViewBounds.Z1 = start.Z - Bounds.InnerHeight / 2.0 / (double)ZoomLevel;
				CurrentBlockViewBounds.X2 = start.X + Bounds.InnerWidth / 2.0 / (double)ZoomLevel;
				CurrentBlockViewBounds.Z2 = start.Z + Bounds.InnerHeight / 2.0 / (double)ZoomLevel;
			}
			else
			{
				CurrentBlockViewBounds.Translate(diffx, 0.0, diffz);
			}
		}
		prevPlayerPos.Set(plr.Pos.X, plr.Pos.Y, plr.Pos.Z);
		if (dialogHasFocus)
		{
			if (api.Input.KeyboardKeyStateRaw[45])
			{
				tkeyDeltaY = 15f;
			}
			else if (api.Input.KeyboardKeyStateRaw[46])
			{
				tkeyDeltaY = -15f;
			}
			else
			{
				tkeyDeltaY = 0f;
			}
			if (api.Input.KeyboardKeyStateRaw[47])
			{
				tkeyDeltaX = 15f;
			}
			else if (api.Input.KeyboardKeyStateRaw[48])
			{
				tkeyDeltaX = -15f;
			}
			else
			{
				tkeyDeltaX = 0f;
			}
			skeyDeltaX += (tkeyDeltaX - skeyDeltaX) * deltaTime * 15f;
			skeyDeltaY += (tkeyDeltaY - skeyDeltaY) * deltaTime * 15f;
			if (Math.Abs(skeyDeltaX) > 0.5f || Math.Abs(skeyDeltaY) > 0.5f)
			{
				CurrentBlockViewBounds.Translate((0f - skeyDeltaX) / ZoomLevel, 0.0, (0f - skeyDeltaY) / ZoomLevel);
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		if (args.Button == EnumMouseButton.Left)
		{
			IsDragingMap = true;
			prevMouseX = args.X;
			prevMouseY = args.Y;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseUp(api, args);
		IsDragingMap = false;
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (IsDragingMap)
		{
			CurrentBlockViewBounds.Translate((float)(-(args.X - prevMouseX)) / ZoomLevel, 0.0, (float)(-(args.Y - prevMouseY)) / ZoomLevel);
			prevMouseX = args.X;
			prevMouseY = args.Y;
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (Bounds.ParentBounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			float px = (float)(((double)api.Input.MouseX - Bounds.absX) / Bounds.InnerWidth);
			float py = (float)(((double)api.Input.MouseY - Bounds.absY) / Bounds.InnerHeight);
			ZoomAdd((args.delta > 0) ? 0.25f : (-0.25f), px, py);
			args.SetHandled();
		}
	}

	public void ZoomAdd(float zoomDiff, float px, float pz)
	{
		if ((!(zoomDiff < 0f) || !(ZoomLevel + zoomDiff < 0.25f)) && (!(zoomDiff > 0f) || !(ZoomLevel + zoomDiff > 6f)))
		{
			ZoomLevel += zoomDiff;
			double nowRelSize = 1f / ZoomLevel;
			double diffX = Bounds.InnerWidth * nowRelSize - CurrentBlockViewBounds.Width;
			double diffZ = Bounds.InnerHeight * nowRelSize - CurrentBlockViewBounds.Length;
			CurrentBlockViewBounds.X2 += diffX;
			CurrentBlockViewBounds.Z2 += diffZ;
			CurrentBlockViewBounds.Translate((0.0 - diffX) * (double)px, 0.0, (0.0 - diffZ) * (double)pz);
			EnsureMapFullyLoaded();
		}
	}

	public void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos)
	{
		if (worldPos == null)
		{
			throw new ArgumentNullException("worldPos is null");
		}
		double blocksWidth = CurrentBlockViewBounds.X2 - CurrentBlockViewBounds.X1;
		double blocksLength = CurrentBlockViewBounds.Z2 - CurrentBlockViewBounds.Z1;
		viewPos.X = (float)((worldPos.X - CurrentBlockViewBounds.X1) / blocksWidth * Bounds.InnerWidth);
		viewPos.Y = (float)((worldPos.Z - CurrentBlockViewBounds.Z1) / blocksLength * Bounds.InnerHeight);
	}

	public void ClampButPreserveAngle(ref Vec2f viewPos, int border)
	{
		if (!(viewPos.X >= (float)border) || !((double)viewPos.X <= Bounds.InnerWidth - 2.0) || !(viewPos.Y >= (float)border) || !((double)viewPos.Y <= Bounds.InnerHeight - 2.0))
		{
			double centerX = Bounds.InnerWidth / 2.0 - (double)border;
			double centerY = Bounds.InnerHeight / 2.0 - (double)border;
			double value = ((double)viewPos.X - centerX) / centerX;
			double factor = Math.Max(val2: Math.Abs(((double)viewPos.Y - centerY) / centerY), val1: Math.Abs(value));
			viewPos.X = (float)(((double)viewPos.X - centerX) / factor + centerX);
			viewPos.Y = (float)(((double)viewPos.Y - centerY) / factor + centerY);
		}
	}

	public void TranslateViewPosToWorldPos(Vec2f viewPos, ref Vec3d worldPos)
	{
		if (worldPos == null)
		{
			throw new ArgumentNullException("viewPos is null");
		}
		double blocksWidth = CurrentBlockViewBounds.X2 - CurrentBlockViewBounds.X1;
		double blocksLength = CurrentBlockViewBounds.Z2 - CurrentBlockViewBounds.Z1;
		worldPos.X = (double)viewPos.X * blocksWidth / Bounds.InnerWidth + CurrentBlockViewBounds.X1;
		worldPos.Z = (double)viewPos.Y * blocksLength / Bounds.InnerHeight + CurrentBlockViewBounds.Z1;
		worldPos.Y = api.World.BlockAccessor.GetRainMapHeightAt(worldPos.AsBlockPos);
	}

	public void EnsureMapFullyLoaded()
	{
		nowVisible.Clear();
		nowHidden.Clear();
		Cuboidi chunkviewBounds = CurrentBlockViewBounds.ToCuboidi();
		chunkviewBounds.Div(32);
		BlockPos cur = new BlockPos().Set(chunkviewBounds.X1, 0, chunkviewBounds.Z1);
		bool beforeBoundsEmpty = chunkViewBoundsBefore.SizeX == 0 && chunkViewBoundsBefore.SizeZ == 0;
		while (cur.X <= chunkviewBounds.X2)
		{
			cur.Z = chunkviewBounds.Z1;
			while (cur.Z <= chunkviewBounds.Z2)
			{
				if (beforeBoundsEmpty || !chunkViewBoundsBefore.ContainsOrTouches(cur))
				{
					nowVisible.Add(new Vec2i(cur.X, cur.Z));
				}
				cur.Z++;
			}
			cur.X++;
		}
		cur.Set(chunkViewBoundsBefore.X1, 0, chunkViewBoundsBefore.Z1);
		while (cur.X <= chunkViewBoundsBefore.X2)
		{
			cur.Z = chunkViewBoundsBefore.Z1;
			while (cur.Z <= chunkViewBoundsBefore.Z2)
			{
				if (!chunkviewBounds.ContainsOrTouches(cur))
				{
					nowHidden.Add(new Vec2i(cur.X, cur.Z));
				}
				cur.Z++;
			}
			cur.X++;
		}
		chunkViewBoundsBefore = chunkviewBounds.Clone();
		if (nowHidden.Count > 0 || nowVisible.Count > 0)
		{
			viewChanged(nowVisible, nowHidden);
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		base.OnKeyDown(api, args);
		if (args.KeyCode == 51)
		{
			CenterMapTo(api.World.Player.Entity.Pos.AsBlockPos);
		}
		if (api.Input.KeyboardKeyStateRaw[45] || api.Input.KeyboardKeyStateRaw[46] || api.Input.KeyboardKeyStateRaw[47] || api.Input.KeyboardKeyStateRaw[48])
		{
			args.Handled = true;
		}
		if (api.Input.KeyboardKeyStateRaw[121] || api.Input.KeyboardKeyStateRaw[80])
		{
			ZoomAdd(0.25f, 0.5f, 0.5f);
		}
		if (api.Input.KeyboardKeyStateRaw[120] || api.Input.KeyboardKeyStateRaw[79])
		{
			ZoomAdd(-0.25f, 0.5f, 0.5f);
		}
	}

	public override void OnKeyUp(ICoreClientAPI api, KeyEvent args)
	{
		base.OnKeyUp(api, args);
	}

	public void CenterMapTo(BlockPos pos)
	{
		CurrentBlockViewBounds = new Cuboidd((double)pos.X - Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)pos.Z - Bounds.InnerHeight / 2.0 / (double)ZoomLevel, (double)pos.X + Bounds.InnerWidth / 2.0 / (double)ZoomLevel, 0.0, (double)pos.Z + Bounds.InnerHeight / 2.0 / (double)ZoomLevel);
	}

	public override void Dispose()
	{
	}
}
