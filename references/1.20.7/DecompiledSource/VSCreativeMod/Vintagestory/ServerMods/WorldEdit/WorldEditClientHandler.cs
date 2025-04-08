using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VSCreativeMod;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.WorldEdit;

public class WorldEditClientHandler
{
	public ICoreClientAPI capi;

	private bool shouldOpenGui;

	public GuiJsonDialog toolBarDialog;

	public GuiJsonDialog controlsDialog;

	public GuiJsonDialog toolOptionsDialog;

	public GuiJsonDialog settingsDialog;

	public HudWorldEditInputCapture scroll;

	public WorldEditScrollToolMode toolModeSelect;

	private JsonDialogSettings toolBarsettings;

	private JsonDialogSettings toolOptionsSettings;

	private IClientNetworkChannel clientChannel;

	public WorldEditWorkspace ownWorkspace;

	private bool isComposing;

	private bool beforeAmbientOverride;

	private readonly Queue<SchematicJsonPacket> _receivedSchematics;

	private GuiDialogConfirmAcceptFile _acceptDlg;

	private Dictionary<string, ToolBase> _toolInstances;

	public WorldEditClientHandler(ICoreClientAPI capi)
	{
		this.capi = capi;
		capi.Input.RegisterHotKey("worldedit", Lang.Get("World Edit"), GlKeys.Tilde, HotkeyType.CreativeTool);
		capi.Input.SetHotKeyHandler("worldedit", OnHotkeyWorldEdit);
		capi.Input.RegisterHotKey("worldeditcopy", Lang.Get("World Edit Copy"), GlKeys.C, HotkeyType.CreativeTool, altPressed: false, ctrlPressed: true);
		capi.Input.RegisterHotKey("worldeditundo", Lang.Get("World Edit Undo"), GlKeys.Z, HotkeyType.CreativeTool, altPressed: false, ctrlPressed: true);
		capi.Event.LeaveWorld += Event_LeaveWorld;
		capi.Event.FileDrop += Event_FileDrop;
		capi.Event.LevelFinalize += Event_LevelFinalize;
		capi.Input.InWorldAction += Input_InWorldAction;
		_receivedSchematics = new Queue<SchematicJsonPacket>();
		clientChannel = capi.Network.GetChannel("worldedit").SetMessageHandler<WorldEditWorkspace>(OnServerWorkspace).SetMessageHandler<CopyToClipboardPacket>(OnClipboardCopy)
			.SetMessageHandler<SchematicJsonPacket>(OnReceivedSchematic)
			.SetMessageHandler<PreviewBlocksPacket>(OnReceivedPreviewBlocks);
		if (!capi.Settings.Int.Exists("schematicMaxUploadSizeKb"))
		{
			capi.Settings.Int["schematicMaxUploadSizeKb"] = 200;
		}
		if (_toolInstances == null)
		{
			_toolInstances = new Dictionary<string, ToolBase>();
		}
	}

	private void Event_LevelFinalize()
	{
		capi.Gui.Icons.CustomIcons["worldedit/chiselbrush"] = capi.Gui.Icons.SvgIconSource(new AssetLocation("textures/icons/worldedit/chiselbrush.svg"));
	}

	private void Input_InWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		BlockSelection blockSel = capi.World.Player.CurrentBlockSelection;
		if (on)
		{
			WorldEditWorkspace worldEditWorkspace = ownWorkspace;
			if (worldEditWorkspace != null && worldEditWorkspace.ToolsEnabled && ownWorkspace.ToolName == "chiselbrush" && (action == EnumEntityAction.InWorldLeftMouseDown || action == EnumEntityAction.InWorldRightMouseDown) && blockSel != null)
			{
				handled = EnumHandling.PreventDefault;
				clientChannel.SendPacket(new WorldInteractPacket
				{
					Position = blockSel.Position,
					DidOffset = blockSel.DidOffset,
					Face = blockSel.Face.Index,
					HitPosition = blockSel.HitPosition,
					SelectionBoxIndex = blockSel.SelectionBoxIndex,
					Mode = ((action != EnumEntityAction.InWorldLeftMouseDown) ? 1 : 0)
				});
			}
		}
	}

	private void OnReceivedSchematic(SchematicJsonPacket message)
	{
		int allowCount = capi.Settings.Int["allowSaveFilesFromServer"];
		if (allowCount > 0)
		{
			receiveFile(message);
			return;
		}
		_receivedSchematics.Enqueue(message);
		if (allowCount == 0)
		{
			if (_acceptDlg == null || !_acceptDlg.IsOpened())
			{
				capi.ShowChatMessage(Lang.Get("schematic-confirm"));
				_acceptDlg = new GuiDialogConfirmAcceptFile(capi, Lang.Get("The server wants to send you a schematic file. Please confirm to accept the file.") + "\n\n" + Lang.Get("{0}.json ({1} Kb)", message.Filename, message.JsonCode.Length / 1024), delegate(string code)
				{
					onConfirm(code);
				});
				_acceptDlg.TryOpen();
			}
		}
		else
		{
			capi.ShowChatMessage(Lang.Get("schematic-ignored") + " <a href=\"chattype://.clientconfig allowSaveFilesFromServer 1\">allowSaveFilesFromServer 1</a>");
		}
	}

	private void onConfirm(string code)
	{
		capi.Event.EnqueueMainThreadTask(delegate
		{
			if (code == "ignore")
			{
				capi.Settings.Int["allowSaveFilesFromServer"] = -1;
			}
			if (code == "accept" || code == "accept10")
			{
				if (code == "accept10")
				{
					capi.Settings.Int["allowSaveFilesFromServer"] = 10;
				}
				else
				{
					capi.Settings.Int["allowSaveFilesFromServer"] = 1;
				}
				Queue<SchematicJsonPacket> queue = new Queue<SchematicJsonPacket>(_receivedSchematics);
				_receivedSchematics.Clear();
				while (queue.Count > 0)
				{
					if (capi.Settings.Int["allowSaveFilesFromServer"] > 0)
					{
						receiveFile(queue.Dequeue());
					}
					else
					{
						OnReceivedSchematic(queue.Dequeue());
					}
				}
			}
		}, "acceptfiles");
	}

	private void receiveFile(SchematicJsonPacket message)
	{
		try
		{
			string exportFolderPath = capi.GetOrCreateDataPath("WorldEdit");
			string outfilepath = Path.Combine(exportFolderPath, Path.GetFileName(message.Filename));
			if (capi.Settings.Bool["allowCreateFoldersFromServer"])
			{
				outfilepath = Path.Combine(exportFolderPath, message.Filename);
				GamePaths.EnsurePathExists(new FileInfo(outfilepath).Directory.FullName);
			}
			if (!outfilepath.EndsWithOrdinal(".json"))
			{
				outfilepath += ".json";
			}
			using (TextWriter textWriter = new StreamWriter(outfilepath))
			{
				textWriter.Write(message.JsonCode);
				textWriter.Close();
			}
			capi.Settings.Int["allowSaveFilesFromServer"]--;
			capi.ShowChatMessage(Lang.Get("schematic-received", "<a href=\"datafolder://worldedit\">" + message.Filename + ".json</a>", capi.Settings.Int["allowSaveFilesFromServer"]));
		}
		catch (IOException e)
		{
			capi.ShowChatMessage(Lang.Get("schematic-failed") + " " + e.Message);
		}
	}

	private void Event_FileDrop(FileDropEvent ev)
	{
		FileInfo info = null;
		long bytes = 0L;
		try
		{
			info = new FileInfo(ev.Filename);
			bytes = info.Length;
		}
		catch (Exception ex)
		{
			capi.TriggerIngameError(this, "importfailed", "Unable to import schematic: " + ex.Message);
			capi.Logger.Error(ex);
			return;
		}
		if (ownWorkspace == null || !ownWorkspace.ToolsEnabled || !(ownWorkspace.ToolName == "import"))
		{
			return;
		}
		int schematicMaxUploadSizeKb = capi.Settings.Int.Get("schematicMaxUploadSizeKb", 200);
		if (bytes / 1024 > schematicMaxUploadSizeKb)
		{
			capi.TriggerIngameError(this, "schematictoolarge", Lang.Get("worldedit-schematicupload-toolarge", schematicMaxUploadSizeKb));
			return;
		}
		string err = null;
		BlockSchematic.LoadFromFile(ev.Filename, ref err);
		if (err != null)
		{
			capi.TriggerIngameError(this, "importerror", err);
			return;
		}
		string json = "";
		using (TextReader textReader = new StreamReader(ev.Filename))
		{
			json = textReader.ReadToEnd();
			textReader.Close();
		}
		if (json.Length < 102400)
		{
			capi.World.Player.ShowChatNotification(Lang.Get("Sending {0} bytes of schematicdata to the server...", json.Length));
		}
		else
		{
			capi.World.Player.ShowChatNotification(Lang.Get("Sending {0} bytes of schematicdata to the server, this may take a while...", json.Length));
		}
		capi.Event.RegisterCallback(delegate
		{
			clientChannel.SendPacket(new SchematicJsonPacket
			{
				Filename = info.Name,
				JsonCode = json
			});
		}, 20, permittedWhilePaused: true);
	}

	private void OnClipboardCopy(CopyToClipboardPacket msg)
	{
		capi.Forms.SetClipboardText(msg.Text);
		capi.World.Player.ShowChatNotification("Ok, copied to your clipboard");
	}

	private void OnReceivedPreviewBlocks(PreviewBlocksPacket msg)
	{
		capi.World.SetBlocksPreviewDimension(msg.dimId);
		if (msg.dimId >= 0)
		{
			IMiniDimension orCreateDimension = capi.World.GetOrCreateDimension(msg.dimId, msg.pos.ToVec3d());
			orCreateDimension.selectionTrackingOriginalPos = msg.pos;
			orCreateDimension.TrackSelection = msg.TrackSelection;
		}
	}

	private void OnServerWorkspace(WorldEditWorkspace workspace)
	{
		ownWorkspace = workspace;
		ownWorkspace.ToolInstance = GetToolInstance(workspace.ToolName);
		if (shouldOpenGui)
		{
			GuiJsonDialog guiJsonDialog = toolBarDialog;
			if (guiJsonDialog != null && guiJsonDialog.IsOpened())
			{
				isComposing = true;
				toolBarDialog.Recompose();
				GuiJsonDialog guiJsonDialog2 = toolOptionsDialog;
				if (guiJsonDialog2 != null && guiJsonDialog2.IsOpened())
				{
					toolOptionsDialog.Recompose();
					toolOptionsDialog.UnfocusElements();
				}
				if (ownWorkspace != null && ownWorkspace.ToolName != null && ownWorkspace.ToolName.Length > 0 && ownWorkspace.ToolsEnabled)
				{
					GuiJsonDialog guiJsonDialog3 = toolBarDialog;
					if (guiJsonDialog3 != null && guiJsonDialog3.IsOpened())
					{
						OpenToolOptionsDialog(ownWorkspace.ToolName ?? "");
					}
				}
				isComposing = false;
				return;
			}
		}
		try
		{
			if (shouldOpenGui)
			{
				if (scroll == null)
				{
					scroll = new HudWorldEditInputCapture(capi, this);
					capi.Gui.RegisterDialog(scroll);
				}
				if (toolModeSelect == null)
				{
					toolModeSelect = new WorldEditScrollToolMode(capi, this);
					capi.Gui.RegisterDialog(toolModeSelect);
				}
				capi.Input.SetHotKeyHandler("worldeditcopy", OnHotkeyWorldEditCopy);
				capi.Input.SetHotKeyHandler("worldeditundo", OnHotkeyWorldEditUndo);
				if (!scroll.IsOpened())
				{
					scroll.TryOpen();
				}
				if (toolBarsettings == null || capi.Settings.Bool.Get("developerMode", defaultValue: false))
				{
					capi.Assets.Reload(AssetCategory.dialog);
					toolBarsettings = capi.Assets.Get<JsonDialogSettings>(new AssetLocation("dialog/worldedit-toolbar.json"));
					toolBarsettings.OnGet = OnGetValueToolbar;
					toolBarsettings.OnSet = OnSetValueToolbar;
				}
				toolBarDialog = new GuiJsonDialog(toolBarsettings, capi, focusFirstElement: false);
				toolBarDialog.TryOpen(withFocus: false);
				toolBarDialog.OnClosed += delegate
				{
					shouldOpenGui = false;
					toolOptionsDialog?.TryClose();
					settingsDialog?.TryClose();
					controlsDialog?.TryClose();
					clientChannel.SendPacket(new RequestWorkSpacePacket());
				};
				if (ownWorkspace != null && ownWorkspace.ToolName != null && ownWorkspace.ToolName.Length > 0 && ownWorkspace.ToolsEnabled)
				{
					OpenToolOptionsDialog(ownWorkspace.ToolName ?? "");
				}
				JsonDialogSettings dlgsettings = capi.Assets.Get<JsonDialogSettings>(new AssetLocation("dialog/worldedit-settings.json"));
				dlgsettings.OnGet = OnGetValueSettings;
				dlgsettings.OnSet = OnSetValueSettings;
				settingsDialog = new GuiJsonDialog(dlgsettings, capi, focusFirstElement: false);
				WorldEditWorkspace worldEditWorkspace = ownWorkspace;
				if (worldEditWorkspace != null && worldEditWorkspace.Rsp)
				{
					settingsDialog?.TryOpen();
				}
				JsonDialogSettings controlsSettings = capi.Assets.Get<JsonDialogSettings>(new AssetLocation("dialog/worldedit-controls.json"));
				controlsSettings.OnSet = OnSetValueControls;
				controlsSettings.OnGet = OnGetValueControls;
				controlsDialog = new GuiJsonDialog(controlsSettings, capi, focusFirstElement: false);
				controlsDialog.TryOpen();
				controlsDialog.Composers.First().Value.GetNumberInput("numberinput-5").DisableButtonFocus = true;
			}
			else
			{
				toolBarDialog?.TryClose();
				toolOptionsDialog?.TryClose();
				settingsDialog?.TryClose();
				controlsDialog?.TryClose();
			}
		}
		catch (Exception e)
		{
			capi.World.Logger.Error("Unable to load json dialogs:");
			capi.World.Logger.Error(e);
		}
	}

	private ToolBase GetToolInstance(string workspaceToolName)
	{
		if (workspaceToolName == null)
		{
			return null;
		}
		if (_toolInstances.TryGetValue(workspaceToolName, out var instance))
		{
			return instance;
		}
		_toolInstances[workspaceToolName] = Activator.CreateInstance(ToolRegistry.ToolTypes[ownWorkspace.ToolName]) as ToolBase;
		return _toolInstances[workspaceToolName];
	}

	private bool OnHotkeyWorldEdit(KeyCombination t1)
	{
		TriggerWorldEditDialog();
		return true;
	}

	private TextCommandResult CmdEditClient(TextCommandCallingArgs args)
	{
		TriggerWorldEditDialog();
		return TextCommandResult.Success();
	}

	private void TriggerWorldEditDialog()
	{
		if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			if (toolBarDialog == null || !toolBarDialog.IsOpened())
			{
				clientChannel.SendPacket(new RequestWorkSpacePacket());
				shouldOpenGui = true;
				return;
			}
			shouldOpenGui = false;
			toolBarDialog?.TryClose();
			toolOptionsDialog?.TryClose();
			settingsDialog?.TryClose();
			controlsDialog?.TryClose();
		}
	}

	private bool OnHotkeyWorldEditUndo(KeyCombination t1)
	{
		capi.SendChatMessage("/we undo");
		return true;
	}

	private bool OnHotkeyWorldEditCopy(KeyCombination t1)
	{
		capi.SendChatMessage("/we copy");
		return true;
	}

	private string OnGetValueControls(string elementcode)
	{
		switch (elementcode)
		{
		case "std.settingsAxisLock":
			return ownWorkspace?.ToolAxisLock.ToString() ?? "0";
		case "std.settingsRsp":
		{
			WorldEditWorkspace worldEditWorkspace = ownWorkspace;
			if (worldEditWorkspace == null || !worldEditWorkspace.Rsp)
			{
				return "0";
			}
			return "1";
		}
		case "std.stepSize":
			return ownWorkspace?.StepSize.ToString() ?? "1";
		case "std.constrain":
			return ownWorkspace?.WorldEditConstraint.ToString() ?? "None";
		default:
			return "";
		}
	}

	private void OnSetValueControls(string elementCode, string newValue)
	{
		switch (elementCode)
		{
		case "undo":
			capi.SendChatMessage("/we undo");
			break;
		case "redo":
			capi.SendChatMessage("/we redo");
			break;
		case "std.settingsRsp":
			if (ownWorkspace != null)
			{
				ownWorkspace.Rsp = string.Equals(newValue, "1");
				if (ownWorkspace.Rsp)
				{
					settingsDialog?.TryOpen();
				}
				else
				{
					settingsDialog?.TryClose();
				}
			}
			capi.SendChatMessage("/we rsp " + ownWorkspace.Rsp);
			break;
		case "std.settingsAxisLock":
			if (ownWorkspace != null)
			{
				ownWorkspace.ToolAxisLock = int.Parse(newValue);
			}
			capi.SendChatMessage("/we tal " + newValue);
			break;
		case "std.stepSize":
		{
			int.TryParse(newValue, out var size);
			if (ownWorkspace != null && ownWorkspace.StepSize != size)
			{
				ownWorkspace.StepSize = size;
				capi.SendChatMessage("/we step " + newValue);
			}
			break;
		}
		case "std.constrain":
			capi.SendChatMessage("/we constrain " + newValue.ToLowerInvariant());
			break;
		}
	}

	private void OnSetValueSettings(string elementCode, string newValue)
	{
		AmbientModifier amb = capi.Ambient.CurrentModifiers["serverambient"];
		amb.CloudBrightness.Weight = 0f;
		amb.CloudDensity.Weight = 0f;
		if (elementCode == null)
		{
			return;
		}
		float[] color;
		switch (elementCode.Length)
		{
		case 9:
			switch (elementCode[0])
			{
			case 't':
				if (elementCode == "timeofday")
				{
					float time = newValue.ToFloat();
					time = time / 24f * capi.World.Calendar.HoursPerDay;
					capi.SendChatMessage("/time set " + time + ":00");
				}
				break;
			case 'c':
				if (elementCode == "cloudypos")
				{
					capi.SendChatMessage("/weather cloudypos " + (newValue.ToFloat() / 255f).ToString(GlobalConstants.DefaultCultureInfo));
				}
				break;
			case 'm':
				if (elementCode == "movespeed")
				{
					capi.World.Player.WorldData.MoveSpeedMultiplier = newValue.ToFloat();
				}
				break;
			}
			break;
		case 8:
			switch (elementCode[3])
			{
			default:
				return;
			case 'l':
				if (elementCode == "foglevel")
				{
					amb.FogDensity.Weight = 1f;
					amb.FogDensity.Value = newValue.ToFloat() / 2000f;
					SendGlobalAmbient();
				}
				return;
			case 'g':
				break;
			case 's':
				if (elementCode == "axislock")
				{
					capi.World.Player.WorldData.FreeMovePlaneLock = (EnumFreeMovAxisLock)newValue.ToInt();
					clientChannel.SendPacket(new ChangePlayerModePacket
					{
						axisLock = capi.World.Player.WorldData.FreeMovePlaneLock
					});
				}
				return;
			}
			if (!(elementCode == "foggreen"))
			{
				break;
			}
			goto IL_0389;
		case 12:
			switch (elementCode[0])
			{
			case 'f':
				if (elementCode == "flatfoglevel")
				{
					amb.FlatFogDensity.Weight = 1f;
					amb.FlatFogDensity.Value = newValue.ToFloat() / 250f;
					SendGlobalAmbient();
				}
				break;
			case 'p':
				if (elementCode == "pickingrange")
				{
					capi.World.Player.WorldData.PickingRange = newValue.ToFloat();
					clientChannel.SendPacket(new ChangePlayerModePacket
					{
						pickingRange = capi.World.Player.WorldData.PickingRange
					});
				}
				break;
			}
			break;
		case 16:
			switch (elementCode[0])
			{
			case 'f':
				if (elementCode == "flatfoglevelypos")
				{
					amb.FlatFogYPos.Weight = 1f;
					amb.FlatFogYPos.Value = newValue.ToFloat();
					SendGlobalAmbient();
				}
				break;
			case 'l':
				if (elementCode == "liquidselectable")
				{
					capi.World.ForceLiquidSelectable = newValue == "1" || newValue == "true";
				}
				break;
			case 'a':
				if (elementCode == "ambientparticles")
				{
					capi.World.AmbientParticles = newValue == "1" || newValue == "true";
				}
				break;
			}
			break;
		case 7:
			switch (elementCode[1])
			{
			default:
				return;
			case 'o':
				break;
			case 'l':
				if (elementCode == "flymode")
				{
					bool fly = newValue == "1" || newValue == "2";
					bool noclip = newValue == "2";
					capi.World.Player.WorldData.FreeMove = fly;
					capi.World.Player.WorldData.NoClip = noclip;
					clientChannel.SendPacket(new ChangePlayerModePacket
					{
						fly = fly,
						noclip = noclip
					});
				}
				return;
			case 'p':
				if (elementCode == "fphands")
				{
					capi.Settings.Bool["hideFpHands"] = newValue != "true" && newValue != "1";
				}
				return;
			case 'm':
			case 'n':
				return;
			}
			if (!(elementCode == "fogblue"))
			{
				break;
			}
			goto IL_0389;
		case 6:
			if (!(elementCode == "fogred"))
			{
				break;
			}
			goto IL_0389;
		case 13:
			if (elementCode == "precipitation")
			{
				capi.SendChatMessage("/weather setprecip " + newValue.ToFloat() / 100f);
				SendGlobalAmbient();
			}
			break;
		case 14:
			if (elementCode == "weatherpattern")
			{
				capi.SendChatMessage("/weather seti " + newValue);
			}
			break;
		case 11:
			if (elementCode == "windpattern")
			{
				capi.SendChatMessage("/weather setw " + newValue);
			}
			break;
		case 24:
			if (elementCode == "serveroverloadprotection")
			{
				capi.SendChatMessage("/we sovp " + newValue);
			}
			break;
		case 15:
			if (elementCode == "overrideambient")
			{
				bool on = newValue == "1" || newValue == "true";
				SendGlobalAmbient(on);
			}
			break;
		case 10:
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
		case 22:
		case 23:
			break;
			IL_0389:
			color = amb.FogColor.Value;
			if (elementCode == "fogred")
			{
				color[0] = (float)newValue.ToInt() / 255f;
			}
			if (elementCode == "foggreen")
			{
				color[1] = (float)newValue.ToInt() / 255f;
			}
			if (elementCode == "fogblue")
			{
				color[2] = (float)newValue.ToInt() / 255f;
			}
			amb.FogColor.Weight = 1f;
			SendGlobalAmbient();
			break;
		}
	}

	private void SendGlobalAmbient(bool enable = true)
	{
		AmbientModifier ambientModifier = capi.Ambient.CurrentModifiers["serverambient"];
		float newWeight = (enable ? 1 : 0);
		ambientModifier.AmbientColor.Weight = 0f;
		ambientModifier.FogColor.Weight = newWeight;
		ambientModifier.FogDensity.Weight = newWeight;
		ambientModifier.FogMin.Weight = newWeight;
		ambientModifier.FlatFogDensity.Weight = newWeight;
		ambientModifier.FlatFogYPos.Weight = newWeight;
		ambientModifier.CloudBrightness.Weight = newWeight;
		ambientModifier.CloudDensity.Weight = newWeight;
		string jsoncode = JsonConvert.SerializeObject(ambientModifier);
		capi.SendChatMessage("/setambient " + jsoncode);
		if (!beforeAmbientOverride)
		{
			settingsDialog.ReloadValues();
		}
		if (!enable && beforeAmbientOverride)
		{
			capi.SendChatMessage("/weather setprecipa");
		}
		if (enable && !beforeAmbientOverride)
		{
			capi.SendChatMessage("/weather acp 0");
		}
		if (!enable && beforeAmbientOverride)
		{
			capi.SendChatMessage("/weather acp 1");
		}
		beforeAmbientOverride = enable;
	}

	private string OnGetValueSettings(string elementCode)
	{
		AmbientModifier amb = capi.Ambient.CurrentModifiers["serverambient"];
		switch (elementCode)
		{
		case "timeofday":
			return ((int)((float)capi.World.Calendar.FullHourOfDay / capi.World.Calendar.HoursPerDay * 24f)).ToString() ?? "";
		case "foglevel":
			return ((int)(amb.FogDensity.Value * 2000f)).ToString() ?? "";
		case "flatfoglevel":
			return ((int)(amb.FlatFogDensity.Value * 250f)).ToString() ?? "";
		case "flatfoglevelypos":
			return ((int)amb.FlatFogYPos.Value).ToString() ?? "";
		case "fogred":
			return ((int)(amb.FogColor.Value[0] * 255f)).ToString() ?? "";
		case "foggreen":
			return ((int)(amb.FogColor.Value[1] * 255f)).ToString() ?? "";
		case "fogblue":
			return ((int)(amb.FogColor.Value[2] * 255f)).ToString() ?? "";
		case "cloudlevel":
			return ((int)(amb.CloudDensity.Value * 100f)).ToString() ?? "";
		case "cloudypos":
			return 255.ToString() ?? "";
		case "cloudbrightness":
			return ((int)(amb.CloudBrightness.Value * 100f)).ToString() ?? "";
		case "movespeed":
			return capi.World.Player.WorldData.MoveSpeedMultiplier.ToString() ?? "";
		case "axislock":
			return ((int)capi.World.Player.WorldData.FreeMovePlaneLock).ToString() ?? "";
		case "pickingrange":
			return capi.World.Player.WorldData.PickingRange.ToString() ?? "";
		case "liquidselectable":
			if (!capi.World.ForceLiquidSelectable)
			{
				return "0";
			}
			return "1";
		case "serveroverloadprotection":
			if (ownWorkspace == null)
			{
				return "1";
			}
			if (!ownWorkspace.serverOverloadProtection)
			{
				return "0";
			}
			return "1";
		case "ambientparticles":
			if (!capi.World.AmbientParticles)
			{
				return "0";
			}
			return "1";
		case "flymode":
		{
			bool freeMove = capi.World.Player.WorldData.FreeMove;
			bool noclip = capi.World.Player.WorldData.NoClip;
			if (freeMove)
			{
				if (noclip)
				{
					return "2";
				}
				return "1";
			}
			return "0";
		}
		case "fphands":
			if (!capi.Settings.Bool["hideFpHands"])
			{
				return "1";
			}
			return "0";
		case "overrideambient":
			if (!(amb.FogColor.Weight >= 0.99f))
			{
				return "0";
			}
			return "1";
		default:
			return "";
		}
	}

	private void OpenToolOptionsDialog(string toolname)
	{
		if (toolOptionsDialog != null)
		{
			toolOptionsDialog.TryClose();
		}
		int index = Array.FindIndex(toolBarsettings.Rows[0].Elements[0].Values, (string w) => w.Equals(toolname.ToLowerInvariant()));
		if (index < 0)
		{
			return;
		}
		string code = toolBarsettings.Rows[0].Elements[0].Values[index];
		toolOptionsDialog?.TryClose();
		capi.Assets.Reload(AssetCategory.dialog);
		toolOptionsSettings = capi.Assets.TryGet("dialog/worldedit-tooloptions-" + code + ".json")?.ToObject<JsonDialogSettings>();
		if (toolOptionsSettings != null)
		{
			toolOptionsSettings.OnSet = delegate(string elem, string newval)
			{
				OnSetValueToolOptions(code, elem, newval);
			};
			toolOptionsSettings.OnGet = OnGetValueToolOptions;
			isComposing = true;
			toolOptionsDialog = new GuiJsonDialog(toolOptionsSettings, capi, focusFirstElement: false);
			toolOptionsDialog.TryOpen();
			isComposing = false;
		}
	}

	private void OnSetValueToolbar(string elementCode, string newValue)
	{
		if (isComposing || !(elementCode == "tooltype"))
		{
			return;
		}
		capi.SendChatMessage("/we t " + newValue);
		OpenToolOptionsDialog(newValue);
		if (newValue == "-1")
		{
			scroll?.TryClose();
			toolModeSelect?.TryClose();
			capi.Input.GetHotKeyByCode("worldeditcopy").Handler = null;
			capi.Input.GetHotKeyByCode("worldeditundo").Handler = null;
			return;
		}
		HudWorldEditInputCapture hudWorldEditInputCapture = scroll;
		if (hudWorldEditInputCapture == null || !hudWorldEditInputCapture.IsOpened())
		{
			scroll?.TryOpen();
		}
		capi.Input.SetHotKeyHandler("worldeditcopy", OnHotkeyWorldEditCopy);
		capi.Input.SetHotKeyHandler("worldeditundo", OnHotkeyWorldEditUndo);
	}

	private void OnSetValueToolOptions(string code, string elem, string newval)
	{
		if (isComposing)
		{
			return;
		}
		toolOptionsSettings = capi.Assets.TryGet("dialog/worldedit-tooloptions-" + code + ".json")?.ToObject<JsonDialogSettings>();
		if (toolOptionsSettings == null)
		{
			return;
		}
		DialogRow[] rows = toolOptionsSettings.Rows;
		int index = 0;
		int row = 0;
		for (row = 0; row < rows.Length; row++)
		{
			index = Array.FindIndex(rows[row].Elements, (DialogElement el) => el.Code.Equals(elem));
			if (index != -1)
			{
				break;
			}
		}
		string cmd = rows[row].Elements[index].Param;
		capi.SendChatMessage(cmd + " " + newval);
		if (ownWorkspace.FloatValues.ContainsKey(elem))
		{
			float val2 = 0f;
			if (float.TryParse(newval, out val2))
			{
				ownWorkspace.FloatValues[elem] = val2;
			}
		}
		if (ownWorkspace.IntValues.ContainsKey(elem))
		{
			int val = 0;
			if (int.TryParse(newval, out val))
			{
				ownWorkspace.IntValues[elem] = val;
			}
		}
		if (ownWorkspace.StringValues.ContainsKey(elem))
		{
			ownWorkspace.StringValues[elem] = newval;
		}
	}

	private string OnGetValueToolbar(string elementCode)
	{
		if (ownWorkspace == null)
		{
			return "";
		}
		if (elementCode == "tooltype")
		{
			if (ownWorkspace.ToolName == null || ownWorkspace.ToolName.Length == 0 || !ownWorkspace.ToolsEnabled)
			{
				return "-1";
			}
			return ownWorkspace.ToolName.ToLowerInvariant();
		}
		return "";
	}

	private string OnGetValueToolOptions(string elementCode)
	{
		if (ownWorkspace == null)
		{
			return "";
		}
		if (ownWorkspace.FloatValues.ContainsKey(elementCode))
		{
			return ownWorkspace.FloatValues[elementCode].ToString() ?? "";
		}
		if (ownWorkspace.IntValues.ContainsKey(elementCode))
		{
			return ownWorkspace.IntValues[elementCode].ToString() ?? "";
		}
		if (ownWorkspace.StringValues.ContainsKey(elementCode))
		{
			return ownWorkspace.StringValues[elementCode] ?? "";
		}
		if (elementCode == "tooloffsetmode")
		{
			if (ownWorkspace != null)
			{
				int toolOffsetMode = (int)ownWorkspace.ToolOffsetMode;
				return toolOffsetMode.ToString();
			}
			return "0";
		}
		return "";
	}

	private void Event_LeaveWorld()
	{
		toolBarDialog?.Dispose();
		controlsDialog?.Dispose();
		toolOptionsDialog?.Dispose();
		settingsDialog?.Dispose();
	}
}
