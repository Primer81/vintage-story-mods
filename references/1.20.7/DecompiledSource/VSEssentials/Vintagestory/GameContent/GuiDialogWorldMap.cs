using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogWorldMap : GuiDialogGeneric
{
	protected EnumDialogType dialogType = EnumDialogType.HUD;

	protected OnViewChangedDelegate viewChanged;

	protected long listenerId;

	protected bool requireRecompose;

	protected int mapWidth = 1200;

	protected int mapHeight = 800;

	protected GuiComposer fullDialog;

	protected GuiComposer hudDialog;

	protected List<GuiTab> tabs;

	private List<string> tabnames;

	private HashSet<string> renderLayerGroups = new HashSet<string>();

	private Vec3d hoveredWorldPos = new Vec3d();

	private GuiDialogAddWayPoint addWpDlg;

	public override bool PrefersUngrabbedMouse => true;

	public override EnumDialogType DialogType => dialogType;

	public override double DrawOrder
	{
		get
		{
			if (dialogType != EnumDialogType.HUD)
			{
				return 0.11;
			}
			return 0.07;
		}
	}

	public List<MapLayer> MapLayers => (base.SingleComposer.GetElement("mapElem") as GuiElementMap)?.mapLayers;

	public GuiDialogWorldMap(OnViewChangedDelegate viewChanged, ICoreClientAPI capi, List<string> tabnames)
		: base("", capi)
	{
		this.viewChanged = viewChanged;
		this.tabnames = tabnames;
		fullDialog = ComposeDialog(EnumDialogType.Dialog);
		hudDialog = ComposeDialog(EnumDialogType.HUD);
		CommandArgumentParsers parsers = capi.ChatCommands.Parsers;
		capi.ChatCommands.GetOrCreate("map").BeginSubCommand("worldmapsize").WithDescription("Show/set worldmap size")
			.WithArgs(parsers.OptionalInt("mapWidth", 1200), parsers.OptionalInt("mapHeight", 800))
			.HandleWith(OnCmdMapSize);
	}

	private TextCommandResult OnCmdMapSize(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success($"Current map size: {mapWidth}x{mapHeight}");
		}
		mapWidth = (int)args.Parsers[0].GetValue();
		mapHeight = (int)args.Parsers[1].GetValue();
		fullDialog = ComposeDialog(EnumDialogType.Dialog);
		return TextCommandResult.Success($"Map size {mapWidth}x{mapHeight} set");
	}

	private GuiComposer ComposeDialog(EnumDialogType dlgType)
	{
		ElementBounds mapBounds = ElementBounds.Fixed(0.0, 28.0, mapWidth, mapHeight);
		ElementBounds layerList = mapBounds.RightCopy().WithFixedSize(1.0, 350.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(3.0);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(mapBounds, layerList);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		GuiComposer compo;
		if (dlgType == EnumDialogType.HUD)
		{
			mapBounds = ElementBounds.Fixed(0.0, 0.0, 250.0, 250.0);
			bgBounds = ElementBounds.Fill.WithFixedPadding(2.0);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(mapBounds);
			dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(GetMinimapPosition(out var offsetX, out var offsetY)).WithFixedAlignmentOffset(offsetX, offsetY);
			compo = hudDialog;
		}
		else
		{
			compo = fullDialog;
		}
		Cuboidd beforeBounds = null;
		if (compo != null)
		{
			beforeBounds = (compo.GetElement("mapElem") as GuiElementMap)?.CurrentBlockViewBounds;
			compo.Dispose();
		}
		tabs = new List<GuiTab>();
		for (int i = 0; i < tabnames.Count; i++)
		{
			tabs.Add(new GuiTab
			{
				Name = Lang.Get("maplayer-" + tabnames[i]),
				DataInt = i,
				Active = true
			});
		}
		ElementBounds tabBounds = ElementBounds.Fixed(-200.0, 45.0, 200.0, 545.0);
		List<MapLayer> maplayers = capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers;
		compo = capi.Gui.CreateCompo("worldmap" + dlgType, dialogBounds).AddShadedDialogBG(bgBounds, withTitleBar: false).AddIf(dlgType == EnumDialogType.Dialog)
			.AddDialogTitleBar(Lang.Get("World Map"), OnTitleBarClose)
			.AddInset(mapBounds, 2)
			.EndIf()
			.BeginChildElements(bgBounds)
			.AddHoverText("", CairoFont.WhiteDetailText(), 350, mapBounds.FlatCopy(), "hoverText")
			.AddIf(dlgType == EnumDialogType.Dialog)
			.AddVerticalToggleTabs(tabs.ToArray(), tabBounds, OnTabClicked, "verticalTabs")
			.EndIf()
			.AddInteractiveElement(new GuiElementMap(maplayers, capi, this, mapBounds, dlgType == EnumDialogType.HUD), "mapElem")
			.EndChildElements()
			.Compose();
		compo.OnComposed += OnRecomposed;
		GuiElementMap mapElem = compo.GetElement("mapElem") as GuiElementMap;
		if (beforeBounds != null)
		{
			mapElem.chunkViewBoundsBefore = beforeBounds.ToCuboidi().Div(32);
		}
		mapElem.viewChanged = viewChanged;
		mapElem.ZoomAdd(1f, 0.5f, 0.5f);
		compo.GetHoverText("hoverText").SetAutoWidth(on: true);
		if (listenerId == 0L)
		{
			listenerId = capi.Event.RegisterGameTickListener(delegate
			{
				if (IsOpened())
				{
					(base.SingleComposer.GetElement("mapElem") as GuiElementMap)?.EnsureMapFullyLoaded();
					if (requireRecompose)
					{
						EnumDialogType asType = dialogType;
						capi.ModLoader.GetModSystem<WorldMapManager>().ToggleMap(asType);
						capi.ModLoader.GetModSystem<WorldMapManager>().ToggleMap(asType);
						requireRecompose = false;
					}
				}
			}, 100);
		}
		if (dlgType == EnumDialogType.Dialog)
		{
			foreach (MapLayer item in maplayers)
			{
				item.ComposeDialogExtras(this, compo);
			}
		}
		capi.World.FrameProfiler.Mark("composeworldmap");
		updateMaplayerExtrasState();
		return compo;
	}

	private void OnTabClicked(int arg1, GuiTab tab)
	{
		string layerGroupCode = tabnames[arg1];
		if (tab.Active)
		{
			renderLayerGroups.Remove(layerGroupCode);
		}
		else
		{
			renderLayerGroups.Add(layerGroupCode);
		}
		foreach (MapLayer ml in MapLayers)
		{
			if (ml.LayerGroupCode == layerGroupCode)
			{
				ml.Active = tab.Active;
			}
		}
		updateMaplayerExtrasState();
	}

	private void updateMaplayerExtrasState()
	{
		if (tabs == null)
		{
			return;
		}
		for (int i = 0; i < tabs.Count; i++)
		{
			string layerGroupCode = tabnames[i];
			GuiTab tab = tabs[i];
			if (Composers["worldmap-layer-" + layerGroupCode] != null)
			{
				Composers["worldmap-layer-" + layerGroupCode].Enabled = tab.Active && dialogType == EnumDialogType.Dialog;
			}
		}
	}

	private void OnRecomposed()
	{
		requireRecompose = true;
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		updateMaplayerExtrasState();
		if (dialogType == EnumDialogType.HUD)
		{
			base.SingleComposer = hudDialog;
			base.SingleComposer.Bounds.Alignment = GetMinimapPosition(out var offsetX, out var offsetY);
			base.SingleComposer.Bounds.fixedOffsetX = offsetX;
			base.SingleComposer.Bounds.fixedOffsetY = offsetY;
			base.SingleComposer.ReCompose();
		}
		else
		{
			base.SingleComposer = ComposeDialog(EnumDialogType.Dialog);
		}
		if (base.SingleComposer.GetElement("mapElem") is GuiElementMap mapElem)
		{
			mapElem.chunkViewBoundsBefore = new Cuboidi();
		}
		OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override bool TryClose()
	{
		if (DialogType == EnumDialogType.Dialog && capi.Settings.Bool["showMinimapHud"])
		{
			Open(EnumDialogType.HUD);
			return false;
		}
		return base.TryClose();
	}

	public void Open(EnumDialogType type)
	{
		dialogType = type;
		opened = false;
		TryOpen();
	}

	public override void OnGuiClosed()
	{
		updateMaplayerExtrasState();
		base.OnGuiClosed();
	}

	public override void Dispose()
	{
		base.Dispose();
		capi.Event.UnregisterGameTickListener(listenerId);
		listenerId = 0L;
		fullDialog.Dispose();
		hudDialog.Dispose();
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		if (base.SingleComposer == null || !base.SingleComposer.Bounds.PointInside(args.X, args.Y))
		{
			return;
		}
		loadWorldPos(args.X, args.Y, ref hoveredWorldPos);
		double yAbs = hoveredWorldPos.Y;
		hoveredWorldPos.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
		hoveredWorldPos.Y = yAbs;
		StringBuilder hoverText = new StringBuilder();
		hoverText.AppendLine($"{(int)hoveredWorldPos.X}, {(int)hoveredWorldPos.Y}, {(int)hoveredWorldPos.Z}");
		GuiElementMap mpc = base.SingleComposer.GetElement("mapElem") as GuiElementMap;
		GuiElementHoverText hoverTextElem = base.SingleComposer.GetHoverText("hoverText");
		foreach (MapLayer mapLayer in mpc.mapLayers)
		{
			mapLayer.OnMouseMoveClient(args, mpc, hoverText);
		}
		string text = hoverText.ToString().TrimEnd();
		hoverTextElem.SetNewText(text);
	}

	private void loadWorldPos(double mouseX, double mouseY, ref Vec3d worldPos)
	{
		double x = mouseX - base.SingleComposer.Bounds.absX;
		double y = mouseY - base.SingleComposer.Bounds.absY - ((dialogType == EnumDialogType.Dialog) ? GuiElement.scaled(30.0) : 0.0);
		(base.SingleComposer.GetElement("mapElem") as GuiElementMap).TranslateViewPosToWorldPos(new Vec2f((float)x, (float)y), ref worldPos);
		worldPos.Y += 1.0;
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
		capi.Render.CheckGlError("map-rend2d");
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		capi.Render.CheckGlError("map-fina");
		bool showHover = base.SingleComposer.Bounds.PointInside(capi.Input.MouseX, capi.Input.MouseY) && Focused;
		GuiElementHoverText hoverText = base.SingleComposer.GetHoverText("hoverText");
		hoverText.SetVisible(showHover);
		hoverText.SetAutoDisplay(showHover);
	}

	public void TranslateWorldPosToViewPos(Vec3d worldPos, ref Vec2f viewPos)
	{
		(base.SingleComposer.GetElement("mapElem") as GuiElementMap).TranslateWorldPosToViewPos(worldPos, ref viewPos);
	}

	public override void OnMouseUp(MouseEvent args)
	{
		if (!base.SingleComposer.Bounds.PointInside(args.X, args.Y))
		{
			base.OnMouseUp(args);
			return;
		}
		GuiElementMap mpc = base.SingleComposer.GetElement("mapElem") as GuiElementMap;
		foreach (MapLayer mapLayer in mpc.mapLayers)
		{
			mapLayer.OnMouseUpClient(args, mpc);
			if (args.Handled)
			{
				return;
			}
		}
		if (args.Button == EnumMouseButton.Right)
		{
			Vec3d wpPos = new Vec3d();
			loadWorldPos(args.X, args.Y, ref wpPos);
			if (addWpDlg != null)
			{
				addWpDlg.TryClose();
				addWpDlg.Dispose();
			}
			WaypointMapLayer wml = MapLayers.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer;
			addWpDlg = new GuiDialogAddWayPoint(capi, wml);
			addWpDlg.WorldPos = wpPos;
			addWpDlg.TryOpen();
			addWpDlg.OnClosed += delegate
			{
				capi.Gui.RequestFocus(this);
			};
		}
		base.OnMouseUp(args);
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		if (base.ShouldReceiveKeyboardEvents())
		{
			return dialogType == EnumDialogType.Dialog;
		}
		return false;
	}

	private EnumDialogArea GetMinimapPosition(out double offsetX, out double offsetY)
	{
		offsetX = GuiStyle.DialogToScreenPadding;
		offsetY = GuiStyle.DialogToScreenPadding;
		EnumDialogArea position;
		switch (capi.Settings.Int["minimapHudPosition"])
		{
		case 1:
			position = EnumDialogArea.LeftTop;
			break;
		case 2:
			position = EnumDialogArea.LeftBottom;
			offsetY = 0.0 - offsetY;
			break;
		case 3:
			position = EnumDialogArea.RightBottom;
			offsetX = 0.0 - offsetX;
			offsetY = 0.0 - offsetY;
			break;
		default:
			position = EnumDialogArea.RightTop;
			offsetX = 0.0 - offsetX;
			break;
		}
		return position;
	}
}
