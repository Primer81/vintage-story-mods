using System;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Client.NoObf;

public class HudDebugScreen : HudElement
{
	private long lastUpdateMilliseconds;

	private int frameCount;

	private float longestframedt;

	private GuiComposer debugTextComposer;

	private GuiComposer systemInfoComposer;

	private GuiElementDynamicText textElement;

	private float[] dtHistory;

	private const int QuantityRenderedFrameSlices = 300;

	private MeshData frameSlicesUpdate;

	private MeshRef frameSlicesRef;

	private bool displayFullDebugInfo;

	private bool displayOnlyFpsDebugInfo;

	private bool displayOnlyFpsDebugInfoTemporary;

	private LoadedTexture[] fpsLabels;

	private Process _process;

	private int historyheight = 80;

	public override string ToggleKeyCombinationCode => null;

	public override bool Focusable => false;

	public HudDebugScreen(ICoreClientAPI capi)
		: base(capi)
	{
		displayFullDebugInfo = false;
		dtHistory = new float[300];
		for (int i = 0; i < 300; i++)
		{
			dtHistory[i] = 0f;
		}
		GenFrameSlicesMesh();
		CairoFont font = CairoFont.WhiteDetailText();
		fpsLabels = new LoadedTexture[4]
		{
			capi.Gui.TextTexture.GenUnscaledTextTexture("30", font),
			capi.Gui.TextTexture.GenUnscaledTextTexture("60", font),
			capi.Gui.TextTexture.GenUnscaledTextTexture("75", font),
			capi.Gui.TextTexture.GenUnscaledTextTexture("150", font)
		};
		capi.ChatCommands.GetOrCreate("debug").BeginSubCommand("edi").RequiresPrivilege(Privilege.chat)
			.WithRootAlias("edi")
			.WithDescription("Show/Hide Extended information on debug screen")
			.HandleWith(ToggleExtendedDebugInfo)
			.EndSubCommand();
		capi.Event.RegisterGameTickListener(EveryOtherSecond, 2000);
		capi.Event.RegisterEventBusListener(delegate
		{
			displayOnlyFpsDebugInfoTemporary = false;
			if (!displayFullDebugInfo && !displayOnlyFpsDebugInfo)
			{
				TryClose();
			}
		}, 0.5, "leftGraphicsDlg");
		capi.Event.RegisterEventBusListener(delegate
		{
			displayOnlyFpsDebugInfoTemporary = true;
			TryOpen();
		}, 0.5, "enteredGraphicsDlg");
		debugTextComposer = capi.Gui.CreateCompo("debugScreenText", ElementBounds.Percentual(EnumDialogArea.RightTop, 0.5, 0.7).WithFixedAlignmentOffset(-5.0, 5.0)).AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Right), ElementBounds.Fill, "debugScreenTextElem").OnlyDynamic()
			.Compose();
		systemInfoComposer = capi.Gui.CreateCompo("sysInfoText", ElementBounds.Percentual(EnumDialogArea.LeftTop, 0.5, 0.7).WithFixedAlignmentOffset(5.0, 5.0)).AddDynamicText("Game Version: " + GameVersion.LongGameVersion + "\n" + ScreenManager.Platform.GetGraphicCardInfos() + "\n" + ScreenManager.Platform.GetFrameworkInfos(), CairoFont.WhiteSmallishText(), ElementBounds.Fill).OnlyDynamic()
			.Compose();
		textElement = debugTextComposer.GetDynamicText("debugScreenTextElem");
		ScreenManager.hotkeyManager.SetHotKeyHandler("fpsgraph", OnKeyGraph);
		ScreenManager.hotkeyManager.SetHotKeyHandler("debugscreenandgraph", OnKeyDebugScreenAndGraph);
	}

	private void EveryOtherSecond(float dt)
	{
		GenFrameSlicesMesh();
	}

	private static TextCommandResult ToggleExtendedDebugInfo(TextCommandCallingArgs textCommandCallingArgs)
	{
		ClientSettings.ExtendedDebugInfo = !ClientSettings.ExtendedDebugInfo;
		return TextCommandResult.Success("Extended debug info " + (ClientSettings.ExtendedDebugInfo ? "on" : "off"));
	}

	public override void OnFinalizeFrame(float dt)
	{
		UpdateGraph(dt);
		UpdateText(dt);
		debugTextComposer.PostRender(dt);
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (displayOnlyFpsDebugInfo || displayFullDebugInfo || displayOnlyFpsDebugInfoTemporary)
		{
			DrawGraph();
			debugTextComposer.Render(deltaTime);
		}
		if (displayFullDebugInfo)
		{
			systemInfoComposer.Render(deltaTime);
		}
	}

	private void UpdateText(float dt)
	{
		frameCount++;
		longestframedt = Math.Max(longestframedt, dt);
		float seconds = (capi.ElapsedMilliseconds - lastUpdateMilliseconds) / 1000;
		if (!(seconds >= 1f) || (!displayFullDebugInfo && !displayOnlyFpsDebugInfo && !displayOnlyFpsDebugInfoTemporary))
		{
			return;
		}
		lastUpdateMilliseconds = capi.ElapsedMilliseconds;
		ClientMain game = capi.World as ClientMain;
		string fpstext = GetFpsText(seconds);
		RuntimeStats.drawCallsCount = 0;
		longestframedt = 0f;
		frameCount = 0;
		if (!displayFullDebugInfo)
		{
			textElement.SetNewTextAsync(fpstext);
			return;
		}
		OrderedDictionary<string, string> perfInfo = game.DebugScreenInfo;
		string managed = decimal.Round((decimal)((float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		if (_process == null)
		{
			_process = Process.GetCurrentProcess();
		}
		_process.Refresh();
		string total = decimal.Round((decimal)((float)_process.WorkingSet64 / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		if (ClientSettings.GlDebugMode)
		{
			fpstext += " (gl debug mode enabled!)";
		}
		if (game.extendedDebugInfo)
		{
			fpstext += " (edi enabled!)";
		}
		game.DebugScreenInfo["fps"] = fpstext;
		game.DebugScreenInfo["mem"] = "CPU Mem Managed/Total: " + managed + " / " + total + " MB";
		game.DebugScreenInfo["entitycount"] = "entities: " + RuntimeStats.renderedEntities + " / " + game.LoadedEntities.Count;
		bool allowCoordinateHud = capi.World.Config.GetBool("allowCoordinateHud", defaultValue: true);
		if (game.EntityPlayer != null)
		{
			EntityPos pos3 = game.EntityPlayer.Pos;
			if (!allowCoordinateHud)
			{
				perfInfo["position"] = "(disabled)";
				perfInfo["chunkpos"] = "(disabled)";
				perfInfo["regpos"] = "(disabled)";
			}
			else
			{
				perfInfo["position"] = "Position: " + pos3.OnlyPosToString() + ((pos3.Dimension > 0) ? (", dim " + pos3.Dimension) : "");
				perfInfo["chunkpos"] = "Chunk: " + (int)(pos3.X / (double)game.WorldMap.ClientChunkSize) + ", " + (int)(pos3.Y / (double)game.WorldMap.ClientChunkSize) + ", " + (int)(pos3.Z / (double)game.WorldMap.ClientChunkSize);
				perfInfo["regpos"] = "Region: " + (int)(pos3.X / (double)game.WorldMap.RegionSize) + ", " + (int)(pos3.Z / (double)game.WorldMap.RegionSize);
			}
			float yaw = GameMath.Mod(game.EntityPlayer.Pos.Yaw, (float)Math.PI * 2f);
			perfInfo["orientation"] = "Yaw: " + (180f * yaw / (float)Math.PI).ToString("#.##", GlobalConstants.DefaultCultureInfo) + " deg., Facing: " + BlockFacing.HorizontalFromYaw(yaw);
		}
		if (game.BlockSelection != null)
		{
			BlockPos pos2 = game.BlockSelection.Position;
			Block solid = game.WorldMap.RelaxedBlockAccess.GetBlock(pos2, 1);
			Block fluid = game.WorldMap.RelaxedBlockAccess.GetBlock(pos2, 2);
			BlockEntity be = game.WorldMap.RelaxedBlockAccess.GetBlockEntity(game.BlockSelection.Position);
			string curBlock = string.Concat("Selected: ", solid.BlockId.ToString(), "/", solid.Code, " @", allowCoordinateHud ? pos2.ToString() : "(disabled)");
			if (fluid.BlockId != 0)
			{
				curBlock = curBlock + "\nFluids layer: " + fluid.BlockId + "/" + fluid.Code;
			}
			perfInfo["curblock"] = curBlock;
			perfInfo["curblockentity"] = "Selected BE: " + be?.GetType();
		}
		else
		{
			perfInfo["curblock"] = "";
			perfInfo["curblocklight"] = "";
		}
		if (game.extendedDebugInfo)
		{
			if (game.BlockSelection != null)
			{
				BlockPos pos = game.BlockSelection.Position.AddCopy(game.BlockSelection.Face);
				int[] hsvvalues = game.WorldMap.GetLightHSVLevels(pos.X, pos.Y, pos.Z);
				perfInfo["curblocklight"] = "FO: Sun V: " + hsvvalues[0] + ", Block H: " + hsvvalues[2] + ", Block S: " + hsvvalues[3] + ", Block V: " + hsvvalues[1];
				pos = game.BlockSelection.Position;
				hsvvalues = game.WorldMap.GetLightHSVLevels(pos.X, pos.Y, pos.Z);
				perfInfo["curblocklight2"] = "Sun V: " + hsvvalues[0] + ", Block H: " + hsvvalues[2] + ", Block S: " + hsvvalues[3] + ", Block V: " + hsvvalues[1];
			}
			perfInfo["tickstopwatch"] = game.tickSummary;
		}
		else
		{
			perfInfo["curblocklight"] = "";
			perfInfo["curblocklight2"] = "";
			perfInfo["tickstopwatch"] = "";
		}
		string newfpstext = "";
		foreach (string value in game.DebugScreenInfo.Values)
		{
			newfpstext = newfpstext + value + "\n";
		}
		textElement.SetNewTextAsync(newfpstext);
	}

	private string GetFpsText(float seconds)
	{
		if (!displayFullDebugInfo)
		{
			return $"Avg FPS: {(int)(1f * (float)frameCount / seconds)}, Min FPS: {(int)(1f / longestframedt)}";
		}
		return $"Avg FPS: {(int)(1f * (float)frameCount / seconds)}, Min FPS: {(int)(1f / longestframedt)}, DCs: {(int)((float)RuntimeStats.drawCallsCount / (1f * (float)frameCount / seconds))}";
	}

	public override bool TryClose()
	{
		return false;
	}

	public void DoClose()
	{
		base.TryClose();
	}

	private bool OnKeyDebugScreenAndGraph(KeyCombination viaKeyComb)
	{
		if (displayFullDebugInfo)
		{
			displayFullDebugInfo = false;
			DoClose();
			return true;
		}
		displayFullDebugInfo = true;
		TryOpen();
		return true;
	}

	private bool OnKeyGraph(KeyCombination viaKeyComb)
	{
		if (displayFullDebugInfo)
		{
			return true;
		}
		if (displayOnlyFpsDebugInfo)
		{
			displayOnlyFpsDebugInfo = false;
			if (!displayOnlyFpsDebugInfoTemporary)
			{
				DoClose();
			}
			return true;
		}
		displayOnlyFpsDebugInfo = true;
		TryOpen();
		return true;
	}

	private void UpdateGraph(float dt)
	{
		for (int i = 0; i < 299; i++)
		{
			dtHistory[i] = dtHistory[i + 1];
		}
		dtHistory[299] = dt;
	}

	private void DrawGraph()
	{
		updateFrameSlicesMesh();
		ClientMain game = capi.World as ClientMain;
		int posx = game.Width - 310;
		int posy = game.Height - historyheight - 40;
		game.Platform.BindTexture2d(game.WhiteTexture());
		game.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
		game.guiShaderProg.ExtraGlow = 0;
		game.guiShaderProg.ApplyColor = 1;
		game.guiShaderProg.AlphaTest = 0f;
		game.guiShaderProg.DarkEdges = 0;
		game.guiShaderProg.NoTexture = 1f;
		game.guiShaderProg.Tex2d2D = game.WhiteTexture();
		game.guiShaderProg.ProjectionMatrix = game.CurrentProjectionMatrix;
		game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
		game.Platform.RenderMesh(frameSlicesRef);
		game.Render2DTexture(game.WhiteTexture(), posx, posy - historyheight, 300f, 1f);
		game.Render2DTexture(game.WhiteTexture(), posx, (float)posy - (float)(historyheight * 60) * (1f / 75f), 300f, 1f);
		game.Render2DTexture(game.WhiteTexture(), posx, (float)posy - (float)(historyheight * 60) * (1f / 30f), 300f, 1f);
		game.Render2DTexture(game.WhiteTexture(), posx, (float)posy - (float)(historyheight * 60) * (1f / 150f), 300f, 1f);
		game.Platform.GlToggleBlend(on: true, EnumBlendMode.PremultipliedAlpha);
		game.Render2DLoadedTexture(fpsLabels[0], posx, (float)posy - (float)(historyheight * 60) * (1f / 30f));
		game.Render2DLoadedTexture(fpsLabels[1], posx, (float)posy - (float)(historyheight * 60) * (1f / 60f));
		game.Render2DLoadedTexture(fpsLabels[2], posx, (float)posy - (float)(historyheight * 60) * (1f / 75f));
		game.Render2DLoadedTexture(fpsLabels[3], posx, (float)posy - (float)(historyheight * 60) * (1f / 150f));
		game.Platform.GlToggleBlend(on: true);
	}

	private void updateFrameSlicesMesh()
	{
		int posy = capi.Render.FrameHeight - historyheight - 40;
		for (int i = 0; i < 300; i++)
		{
			float frameTime = dtHistory[i];
			frameTime = frameTime * 60f * (float)historyheight;
			int vertIndex = i * 4 * 3;
			frameSlicesUpdate.xyz[vertIndex + 7] = (float)posy - frameTime;
			frameSlicesUpdate.xyz[vertIndex + 10] = (float)posy - frameTime;
		}
		capi.Render.UpdateMesh(frameSlicesRef, frameSlicesUpdate);
	}

	private void GenFrameSlicesMesh()
	{
		MeshData frameSlices = new MeshData(1200, 1800, withNormals: false, withUv: true, withRgba: true, withFlags: false);
		int posx = capi.Render.FrameWidth - 310;
		int posy = capi.Render.FrameHeight - historyheight - 40;
		for (int i = 0; i < 300; i++)
		{
			byte r = (byte)(255f * (float)i / 300f);
			MeshData sliceMesh = QuadMeshUtilExt.GetCustomQuadModelData(posx + i, posy, 50f, 1f, 1f, r, 0, 0, byte.MaxValue);
			frameSlices.AddMeshData(sliceMesh);
		}
		if (frameSlicesRef != null)
		{
			capi.Render.DeleteMesh(frameSlicesRef);
		}
		frameSlicesRef = capi.Render.UploadMesh(frameSlices);
		frameSlicesUpdate = frameSlices;
		frameSlicesUpdate.Rgba = null;
		frameSlicesUpdate.Indices = null;
	}

	public override void Dispose()
	{
		base.Dispose();
		debugTextComposer?.Dispose();
		systemInfoComposer?.Dispose();
		frameSlicesRef?.Dispose();
		int i = 0;
		while (fpsLabels != null && i < fpsLabels.Length)
		{
			fpsLabels[i]?.Dispose();
			i++;
		}
		_process?.Dispose();
	}
}
