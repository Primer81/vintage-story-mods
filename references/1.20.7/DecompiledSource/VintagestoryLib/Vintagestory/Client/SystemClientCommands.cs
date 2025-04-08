using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

internal class SystemClientCommands : ClientSystem
{
	private enum EnumPngsExportRequest
	{
		None,
		CreativeInventory,
		All,
		One,
		Hand
	}

	private EnumPngsExportRequest exportRequest;

	private string exportDomain;

	private int size;

	private EnumItemClass exportType;

	private string exportCode;

	public override string Name => "ccom";

	public SystemClientCommands(ClientMain game)
		: base(game)
	{
		game.api.RegisterLinkProtocol("command", onCommandLinkClicked);
		ICoreClientAPI api = game.api;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("help").RequiresPrivilege(Privilege.chat).WithArgs(parsers.OptionalWord("commandname"), parsers.OptionalWord("subcommand"), parsers.OptionalWord("subsubcommand"))
			.WithDescription("Display list of available server commands")
			.HandleWith(handleHelp);
		api.ChatCommands.GetOrCreate("dev").BeginSubCommand("reload").WithRootAlias("reload")
			.WithDescription("Asseted reloading utility. Incase of shape reload will also Re-tesselate. Incase of textures will regenerate the texture atlasses.")
			.WithArgs(parsers.Word("assetcategory"))
			.HandleWith(OnCmdReload)
			.EndSubCommand();
		api.ChatCommands.Create("clients").WithAlias("online").WithDescription("List of connected players")
			.WithArgs(parsers.OptionalWord("ping"))
			.HandleWith(OnCmdListClients);
		api.ChatCommands.Create("freemove").WithDescription("Toggle Freemove").WithArgs(parsers.OptionalBool("freeMove"))
			.HandleWith(OnCmdFreeMove);
		api.ChatCommands.Create("gui").WithDescription("Hide/Show all GUIs").WithArgs(parsers.OptionalBool("show_gui"))
			.HandleWith(OnCmdToggleGUI);
		api.ChatCommands.Create("movespeed").WithDescription("Set Movespeed").WithArgs(parsers.OptionalFloat("speed", 1f))
			.HandleWith(OnCmdMoveSpeed);
		api.ChatCommands.Create("noclip").WithDescription("Toggle noclip").WithArgs(parsers.OptionalBool("noclip"))
			.HandleWith(OnCmdNoClip);
		api.ChatCommands.Create("viewdistance").WithDescription("Set view distance").WithArgs(parsers.OptionalInt("viewdistance"))
			.HandleWith(OnCmdViewDistance);
		api.ChatCommands.Create("lockfly").WithDescription("Locks a movement axis during flying/swimming").WithArgs(parsers.OptionalInt("axis"))
			.HandleWith(OnCmdLockFly);
		api.ChatCommands.Create("resolution").WithDescription("Sets the screen size to given width and height. Can be either [width] [height] or [360p|480p|720p|1080p|2160p]").WithArgs(parsers.OptionalWord("width"), parsers.OptionalWord("height"))
			.HandleWith(OnCmdResolution);
		api.ChatCommands.Create("clientconfig").WithAlias("cf").WithDescription("Set/Gets a client setting")
			.WithArgs(parsers.Word("name"))
			.IgnoreAdditionalArgs()
			.HandleWith(OnCmdSetting);
		api.ChatCommands.Create("clientconfigcreate").WithDescription("Create a new client setting that does not exist").WithArgs(parsers.Word("name"), parsers.Word("datatype"))
			.IgnoreAdditionalArgs()
			.HandleWith(OnCmdSettingCreate);
		api.ChatCommands.Create("cp").WithDescription("Copy something to your clipboard").BeginSubCommand("posi")
			.WithDescription("Copy position as integer")
			.HandleWith(OnCmdCpPosi)
			.EndSubCommand()
			.BeginSubCommand("aposi")
			.WithDescription("Copy position as absolute integer")
			.HandleWith(OnCmdCpAposi)
			.EndSubCommand()
			.BeginSubCommand("apos")
			.WithDescription("Copy position as absolute floating point number")
			.HandleWith(OnCmdCpApos)
			.EndSubCommand()
			.BeginSubCommand("chat")
			.WithDescription("Copy the chat history")
			.HandleWith(OnCmdCpChat)
			.EndSubCommand();
		api.ChatCommands.Create("reconnect").WithDescription("Reconnect to server").HandleWith(OnCmdReconnect);
		api.ChatCommands.Create("recordingmode").WithDescription("Makes the game brighter for recording (Sets gamma level to 1.1 and brightness level to 1.5)").HandleWith(OnCmdRecordingMode);
		api.ChatCommands.Create("blockitempngexport").WithDescription("Export all items and blocks as png images").WithArgs(parsers.OptionalWordRange("exportRequest", "inv", "all"), parsers.OptionalInt("size", 100), parsers.OptionalWord("exportDomain"))
			.HandleWith(OnCmdBlockItemPngExport);
		api.ChatCommands.Create("exponepng").BeginSubCommand("code").WithDescription("Export one items as png image")
			.WithArgs(parsers.WordRange("exportType", "block", "item"), parsers.Word("exportCode"), parsers.OptionalInt("size", 100))
			.HandleWith(OnCmdOnePngExportCode)
			.EndSubCommand()
			.BeginSubCommand("hand")
			.WithDescription("Export icon for currently held item/block")
			.WithArgs(parsers.OptionalInt("size", 100))
			.HandleWith(OnCmdOnePngExportHand)
			.EndSubCommand();
		api.ChatCommands.Create("gencraftjson").WithDescription("Copies a snippet of json from your currently held item usable as a crafting recipe ingredient").HandleWith(OnCmdGenCraftJson);
		api.ChatCommands.Create("zfar").WithDescription("Sets the zfar clipping plane. Useful when up the limit of 1km view distance.").WithArgs(parsers.OptionalFloat("zfar"))
			.HandleWith(OnCmdZfar);
		api.ChatCommands.Create("crash").WithDescription("Crashes the game.").HandleWith(OnCmdCrash);
		api.ChatCommands.Create("timelapse").WithDescription("Start a sequence of timelapse photography, with specified interval (days) and duration (months)").WithArgs(parsers.Float("interval"), parsers.Float("duration"))
			.IgnoreAdditionalArgs()
			.HandleWith(OnCmdTimelapse);
		game.eventManager.RegisterRenderer(OnRenderBlockItemPngs, EnumRenderStage.Ortho, "renderblockitempngs", 0.5);
	}

	private TextCommandResult OnCmdCpPosi(TextCommandCallingArgs args)
	{
		if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			BlockPos pos = game.EntityPlayer.Pos.AsBlockPos;
			pos.Sub(game.SpawnPosition.AsBlockPos.X, 0, game.SpawnPosition.AsBlockPos.Z);
			game.Platform.XPlatInterface.SetClipboardText(pos?.ToString() ?? "");
			return TextCommandResult.Success("Position as integer copied to clipboard");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdCpAposi(TextCommandCallingArgs args)
	{
		if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZInt?.ToString() ?? "");
			return TextCommandResult.Success("Absolute Position as integer copied to clipboard");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdCpApos(TextCommandCallingArgs args)
	{
		if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZ?.ToString() ?? "");
			return TextCommandResult.Success("Absolute Position copied to clipboard");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdCpChat(TextCommandCallingArgs args)
	{
		StringBuilder b = new StringBuilder();
		foreach (string line in game.ChatHistoryByPlayerGroup[game.currentGroupid])
		{
			b.AppendLine(line);
		}
		game.Platform.XPlatInterface.SetClipboardText(b.ToString());
		return TextCommandResult.Success("Current chat history copied to clipboard");
	}

	private TextCommandResult handleHelp(TextCommandCallingArgs args)
	{
		StringBuilder text = new StringBuilder();
		Dictionary<string, IChatCommand> commands = IChatCommandApi.GetOrdered(game.api.chatcommandapi.AllSubcommands());
		Caller caller = args.Caller;
		if (caller.CallerPrivileges == null)
		{
			caller.CallerPrivileges = new string[1] { "*" };
		}
		if (args.Parsers[0].IsMissing)
		{
			text.AppendLine("Available commands:");
			ChatCommandImpl.WriteCommandsList(text, commands, args.Caller);
			text.Append("\n" + Lang.Get("Type /help [commandname] to see more info about a command"));
			return TextCommandResult.Success(text.ToString());
		}
		string arg = (string)args[0];
		if (!args.Parsers[1].IsMissing)
		{
			bool found = false;
			foreach (KeyValuePair<string, IChatCommand> entry3 in commands)
			{
				if (entry3.Key == arg)
				{
					commands = IChatCommandApi.GetOrdered(entry3.Value.AllSubcommands);
					found = true;
					break;
				}
			}
			if (!found)
			{
				return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + arg + " " + (string)args[1]);
			}
			arg = (string)args[1];
			if (!args.Parsers[2].IsMissing)
			{
				found = false;
				foreach (KeyValuePair<string, IChatCommand> entry2 in commands)
				{
					if (entry2.Key == arg)
					{
						commands = IChatCommandApi.GetOrdered(entry2.Value.AllSubcommands);
						found = true;
						break;
					}
				}
				if (!found)
				{
					return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + (string)args[0] + arg + " " + (string)args[2]);
				}
				arg = (string)args[2];
			}
		}
		foreach (KeyValuePair<string, IChatCommand> entry in commands)
		{
			if (entry.Key == arg)
			{
				IChatCommand cm = entry.Value;
				if (cm.IsAvailableTo(args.Caller))
				{
					return TextCommandResult.Success(cm.GetFullSyntaxConsole(args.Caller));
				}
				return TextCommandResult.Error("Insufficient privilege to use this command");
			}
		}
		return TextCommandResult.Error(Lang.Get("No such command found") + ": " + arg);
	}

	private void onCommandLinkClicked(LinkTextComponent linkComp)
	{
		game.eventManager?.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, linkComp.Href.Substring("command://".Length), EnumChatType.Macro, null);
	}

	private TextCommandResult OnCmdCrash(TextCommandCallingArgs textCommandCallingArgs)
	{
		throw new Exception("Crash on request");
	}

	private TextCommandResult OnCmdRecordingMode(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (ClientSettings.BrightnessLevel == 1f)
		{
			ClientSettings.BrightnessLevel = 1.2f;
			ClientSettings.ExtraGammaLevel = 1.3f;
			game.ShowChatMessage("Recording bright mode now on");
		}
		else
		{
			ClientSettings.BrightnessLevel = 1f;
			ClientSettings.ExtraGammaLevel = 1f;
			game.ShowChatMessage("Recording bright mode now off");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdTimelapse(TextCommandCallingArgs args)
	{
		float interval = (float)args[0];
		float duration = (float)args[1];
		game.timelapse = interval;
		game.timelapseEnd = duration * (float)game.Calendar.DaysPerMonth;
		game.ShouldRender2DOverlays = false;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdGenCraftJson(TextCommandCallingArgs args)
	{
		ItemSlot slot = game.player.inventoryMgr.ActiveHotbarSlot;
		if (slot.Itemstack == null)
		{
			return TextCommandResult.Success("Require something held in your hands");
		}
		StringBuilder sb = new StringBuilder();
		sb.Append("{");
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder);
		handler.AppendLiteral("type: \"");
		handler.AppendFormatted(slot.Itemstack.Class);
		handler.AppendLiteral("\", code: \"");
		handler.AppendFormatted(slot.Itemstack.Collectible.Code);
		handler.AppendLiteral("\"");
		stringBuilder2.Append(ref handler);
		TreeAttribute attrs = slot.Itemstack.Attributes.Clone() as TreeAttribute;
		for (int i = 0; i < GlobalConstants.IgnoredStackAttributes.Length; i++)
		{
			attrs.RemoveAttribute(GlobalConstants.IgnoredStackAttributes[i]);
		}
		string attrjson = attrs.ToJsonToken();
		if (attrjson.Length > 0 && attrs.Count > 0)
		{
			stringBuilder = sb;
			StringBuilder stringBuilder3 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder);
			handler.AppendLiteral(", attributes: ");
			handler.AppendFormatted(attrjson);
			stringBuilder3.Append(ref handler);
		}
		sb.Append(" }");
		game.Platform.XPlatInterface.SetClipboardText(sb.ToString());
		return TextCommandResult.Success("Ok, copied to your clipboard");
	}

	private void OnRenderBlockItemPngs(float dt)
	{
		if (exportRequest == EnumPngsExportRequest.None)
		{
			return;
		}
		bool all = exportRequest == EnumPngsExportRequest.All;
		FrameBufferRef fb = game.Platform.CreateFramebuffer(new FramebufferAttrs("PngExport", size, size)
		{
			Attachments = new FramebufferAttrsAttachment[2]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = size,
						Height = size,
						PixelFormat = EnumTexturePixelFormat.Rgba,
						PixelInternalFormat = EnumTextureInternalFormat.Rgba8
					}
				},
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.DepthAttachment,
					Texture = new RawTexture
					{
						Width = size,
						Height = size,
						PixelFormat = EnumTexturePixelFormat.DepthComponent,
						PixelInternalFormat = EnumTextureInternalFormat.DepthComponent32
					}
				}
			}
		});
		game.Platform.LoadFrameBuffer(fb);
		game.Platform.GlEnableDepthTest();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.OrthoMode(size, size);
		float[] clearCol = new float[4];
		GamePaths.EnsurePathExists("icons/block");
		GamePaths.EnsurePathExists("icons/item");
		if (exportRequest == EnumPngsExportRequest.One || exportRequest == EnumPngsExportRequest.Hand)
		{
			game.Platform.ClearFrameBuffer(fb, clearCol);
			ItemStack stack;
			if (exportRequest == EnumPngsExportRequest.One)
			{
				if (exportType == EnumItemClass.Item)
				{
					Item item = game.GetItem(new AssetLocation(exportCode));
					if (item == null)
					{
						game.ShowChatMessage("Not an item " + exportCode);
						exportRequest = EnumPngsExportRequest.None;
						return;
					}
					stack = new ItemStack(item);
				}
				else
				{
					Block block = game.GetBlock(new AssetLocation(exportCode));
					if (block == null)
					{
						game.ShowChatMessage("Not a block " + exportCode);
						exportRequest = EnumPngsExportRequest.None;
						return;
					}
					stack = new ItemStack(block);
				}
			}
			else
			{
				stack = game.player.inventoryMgr.ActiveHotbarSlot.Itemstack;
				if (stack == null)
				{
					game.ShowChatMessage("Nothing in hands");
					exportRequest = EnumPngsExportRequest.None;
					return;
				}
			}
			game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(stack), size / 2, size / 2, 500.0, size / 2, -1, shading: true, origRotate: false, showStackSize: false);
			game.Platform.GrabScreenshot(size, size, scaleScreenshot: false, flip: true, withAlpha: true).Save("icons/" + exportType.Name() + "/" + stack.Collectible.Code.Path + ".png");
		}
		else
		{
			for (int j = 0; j < game.Blocks.Count; j++)
			{
				game.Platform.ClearFrameBuffer(fb, clearCol);
				Block block2 = game.Blocks[j];
				if (!(block2?.Code == null) && (all || (block2.CreativeInventoryTabs != null && block2.CreativeInventoryTabs.Length != 0)) && (exportDomain == null || !(block2.Code.Domain != exportDomain)))
				{
					game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(new ItemStack(block2)), size / 2, size / 2, 500.0, size / 2, -1, shading: true, origRotate: false, showStackSize: false);
					game.Platform.GrabScreenshot(size, size, scaleScreenshot: false, flip: true, withAlpha: true).Save("icons/block/" + block2.Code.Path + ".png");
				}
			}
			for (int i = 0; i < game.Items.Count; i++)
			{
				game.Platform.ClearFrameBuffer(fb, clearCol);
				Item item2 = game.Items[i];
				if (!(item2?.Code == null) && (all || (item2.CreativeInventoryTabs != null && item2.CreativeInventoryTabs.Length != 0)) && (exportDomain == null || !(item2.Code.Domain != exportDomain)))
				{
					game.api.renderapi.inventoryItemRenderer.RenderItemstackToGui(new DummySlot(new ItemStack(item2)), size / 2, size / 2, 500.0, size / 2, -1, shading: true, origRotate: false, showStackSize: false);
					BitmapRef bitmapRef = game.Platform.GrabScreenshot(size, size, scaleScreenshot: false, flip: true, withAlpha: true);
					string name = item2.Code.Path;
					if (name.Contains("/"))
					{
						name = name.Replace("/", "-");
					}
					bitmapRef.Save("icons/item/" + name + ".png");
				}
			}
		}
		exportRequest = EnumPngsExportRequest.None;
		game.OrthoMode(game.Width, game.Height);
		game.Platform.UnloadFrameBuffer(fb);
		game.Platform.DisposeFrameBuffer(fb);
		game.ShowChatMessage("Ok, exported to " + Path.GetFullPath("icons/"));
	}

	private void cCopy(int groupId, CmdArgs args)
	{
		switch (args.PopWord())
		{
		case "posi":
			if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
			{
				BlockPos pos = game.EntityPlayer.Pos.AsBlockPos;
				pos.Sub(game.SpawnPosition.AsBlockPos.X, 0, game.SpawnPosition.AsBlockPos.Z);
				game.Platform.XPlatInterface.SetClipboardText(pos?.ToString() ?? "");
				game.ShowChatMessage("Position as integer copied to clipboard");
			}
			break;
		case "apos":
			if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
			{
				game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZ?.ToString() ?? "");
				game.ShowChatMessage("Absolute Position copied to clipboard");
			}
			break;
		case "aposi":
			if (game.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
			{
				game.Platform.XPlatInterface.SetClipboardText(game.EntityPlayer.Pos.XYZInt?.ToString() ?? "");
				game.ShowChatMessage("Absolute Position as integer copied to clipboard");
			}
			break;
		case "chat":
		{
			StringBuilder b = new StringBuilder();
			foreach (string line in game.ChatHistoryByPlayerGroup[game.currentGroupid])
			{
				b.AppendLine(line);
			}
			game.Platform.XPlatInterface.SetClipboardText(b.ToString());
			game.ShowChatMessage("Current chat history copied to clipboard");
			break;
		}
		}
	}

	private TextCommandResult OnCmdSetting(TextCommandCallingArgs targs)
	{
		CmdArgs args = targs.RawArgs;
		string name = targs[0] as string;
		if (name == "sedi")
		{
			name = "showentitydebuginfo";
		}
		if (args.Length == 0)
		{
			if (!ClientSettings.Inst.HasSetting(name))
			{
				return TextCommandResult.Success("No such setting '" + name + "' (you can create setttings using .clientconfigcreate)");
			}
			return TextCommandResult.Success($"{name} is set to {ClientSettings.Inst.GetSetting(name)}");
		}
		Type type = ClientSettings.Inst.GetSettingType(name);
		if (type == null)
		{
			return TextCommandResult.Success("No such setting '" + name + "'");
		}
		if (type == typeof(string))
		{
			string value = args.PopWord();
			ClientSettings.Inst.String[name] = value;
			game.ShowChatMessage(name + " now set to " + value);
		}
		if (type == typeof(int))
		{
			int? intVal = args.PopInt(0);
			if (intVal.HasValue)
			{
				ClientSettings.Inst.Int[name] = intVal.Value;
				game.ShowChatMessage($"{name} now set to {intVal}");
			}
			else
			{
				game.ShowChatMessage("Supplied value is not an integer");
			}
		}
		if (type == typeof(float))
		{
			float? floatVal = args.PopFloat(0f);
			if (floatVal.HasValue)
			{
				ClientSettings.Inst.Float[name] = floatVal.Value;
				game.ShowChatMessage($"{name} now set to {floatVal}");
			}
			else
			{
				game.ShowChatMessage("Supplied value is not an integer");
			}
		}
		if (type == typeof(bool))
		{
			bool boolVal = false;
			boolVal = ((!(args.PeekWord() == "toggle")) ? args.PopBool(false).GetValueOrDefault() : (!ClientSettings.Inst.Bool[name]));
			ClientSettings.Inst.Bool[name] = boolVal;
			game.ShowChatMessage($"{name} now set to {boolVal}");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdSettingCreate(TextCommandCallingArgs targs)
	{
		CmdArgs args = targs.RawArgs;
		string name = targs[0] as string;
		if (ClientSettings.Inst.HasSetting(name))
		{
			return TextCommandResult.Success("Setting '" + name + "' already exists");
		}
		string type = targs[1] as string;
		switch (type)
		{
		case "string":
		{
			string value = args.PopAll();
			ClientSettings.Inst.String[name] = value;
			return TextCommandResult.Success(name + " now set to " + value);
		}
		case "int":
		{
			int? intVal = args.PopInt(0);
			if (intVal.HasValue)
			{
				ClientSettings.Inst.Int[name] = intVal.Value;
				return TextCommandResult.Success($"{name} now set to {intVal}");
			}
			return TextCommandResult.Success($"Supplied value is not an integer");
		}
		case "float":
		{
			float? floatVal = args.PopFloat(0f);
			if (floatVal.HasValue)
			{
				ClientSettings.Inst.Float[name] = floatVal.Value;
				return TextCommandResult.Success($"{name} now set to {floatVal}");
			}
			return TextCommandResult.Success($"Supplied value is not an integer");
		}
		case "bool":
		{
			bool boolVal = args.PopBool(false).GetValueOrDefault();
			ClientSettings.Inst.Bool[name] = boolVal;
			return TextCommandResult.Success($"{name} now set to {boolVal}");
		}
		default:
			return TextCommandResult.Success("Unknown datatype: " + type + ". Must be string, int, float or bool");
		}
	}

	private TextCommandResult OnCmdLockFly(TextCommandCallingArgs args)
	{
		EnumFreeMovAxisLock mode = EnumFreeMovAxisLock.None;
		if (!args.Parsers[0].IsMissing)
		{
			int val = (int)args[0];
			if (val <= 3)
			{
				mode = (EnumFreeMovAxisLock)val;
			}
		}
		game.player.worlddata.RequestMode(game, mode);
		return TextCommandResult.Success("Lock fly axis " + mode);
	}

	private TextCommandResult OnCmdResolution(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current resolution: " + game.Platform.WindowSize.Width + "x" + game.Platform.WindowSize.Height);
		}
		bool found = true;
		int width = 0;
		int height = 0;
		switch (args[0] as string)
		{
		case "360p":
			width = 640;
			height = 360;
			break;
		case "480p":
			width = 854;
			height = 480;
			break;
		case "720p":
			width = 1280;
			height = 720;
			break;
		case "1080p":
			width = 1920;
			height = 1080;
			break;
		case "1440p":
			width = 2560;
			height = 1440;
			break;
		case "2160p":
			width = 3840;
			height = 2160;
			break;
		default:
			found = false;
			break;
		}
		if (!found && args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success("Width or Height missing");
		}
		if (!found)
		{
			int.TryParse(args[0] as string, out width);
			int.TryParse(args[1] as string, out height);
		}
		if (width <= 0 || height <= 0)
		{
			return TextCommandResult.Success("Width or Height not a number or 0 or below 0");
		}
		game.Platform.SetWindowSize(width, height);
		return TextCommandResult.Success($"Resolution {width}x{height} set.");
	}

	private TextCommandResult OnCmdZfar(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current Zfar: " + game.MainCamera.ZFar);
		}
		try
		{
			game.MainCamera.ZFar = (float)args[0];
			game.Reset3DProjection();
			return TextCommandResult.Success("Zfar is now: " + game.MainCamera.ZFar);
		}
		catch (Exception)
		{
			return TextCommandResult.Success("Failed parsing param");
		}
	}

	private TextCommandResult OnCmdReload(TextCommandCallingArgs args)
	{
		AssetCategory.categories.TryGetValue(args[0] as string, out var cat);
		if (cat == null)
		{
			return TextCommandResult.Success("No such asset category found");
		}
		int reloaded = game.Platform.AssetManager.Reload(cat);
		if (cat == AssetCategory.shaders)
		{
			bool ok = ShaderRegistry.ReloadShaders();
			bool ok2 = game.eventManager != null && game.eventManager.TriggerReloadShaders();
			ok = ok && ok2;
			return TextCommandResult.Success("Shaders reloaded" + (ok ? "" : ". errors occured, please check client log"));
		}
		if (cat == AssetCategory.shapes)
		{
			game.eventManager?.TriggerReloadShapes();
			return TextCommandResult.Success(reloaded + " assets reloaded and shapes re-tesselated");
		}
		if (cat == AssetCategory.textures)
		{
			game.ReloadTextures();
			return TextCommandResult.Success(reloaded + " assets reloaded and atlasses re-generated");
		}
		if (cat == AssetCategory.sounds)
		{
			ScreenManager.LoadSoundsInitial();
		}
		if (cat == AssetCategory.lang)
		{
			Lang.Load(game.Logger, game.AssetManager, ClientSettings.Language);
			return TextCommandResult.Success("language files reloaded");
		}
		return TextCommandResult.Success(reloaded + " assets reloaded");
	}

	private TextCommandResult OnCmdViewDistance(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current view distance: " + ClientSettings.ViewDistance);
		}
		ClientSettings.ViewDistance = (int)args[0];
		return TextCommandResult.Success("View distance set");
	}

	private TextCommandResult OnCmdListClients(TextCommandCallingArgs args)
	{
		bool withping = args[0] as string == "ping";
		StringBuilder sb = new StringBuilder();
		int cnt = 0;
		foreach (KeyValuePair<string, ClientPlayer> val in game.PlayersByUid)
		{
			string name = val.Value.PlayerName;
			if (name != null)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}
				if (withping)
				{
					StringBuilder stringBuilder = sb;
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder);
					handler.AppendFormatted(name);
					handler.AppendLiteral(" (");
					handler.AppendFormatted((int)(val.Value.Ping * 1000f));
					handler.AppendLiteral("ms)");
					stringBuilder2.Append(ref handler);
				}
				else
				{
					StringBuilder stringBuilder = sb;
					StringBuilder stringBuilder3 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder);
					handler.AppendFormatted(name);
					stringBuilder3.Append(ref handler);
				}
				cnt++;
			}
		}
		return TextCommandResult.Success($"{cnt} Players: {sb}");
	}

	private TextCommandResult OnCmdReconnect(TextCommandCallingArgs textCommandCallingArgs)
	{
		game.exitReason = "reconnect command triggered";
		game.DoReconnect();
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdFreeMove(TextCommandCallingArgs args)
	{
		if (game.AllowFreemove)
		{
			game.player.worlddata.RequestModeFreeMove(game, (bool)args[0]);
			return TextCommandResult.Success();
		}
		return TextCommandResult.Success(Lang.Get("Flymode not allowed"));
	}

	private TextCommandResult OnCmdToggleGUI(TextCommandCallingArgs args)
	{
		game.ShouldRender2DOverlays = (bool)args[0];
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMoveSpeed(TextCommandCallingArgs args)
	{
		if (!game.AllowFreemove)
		{
			return TextCommandResult.Success(Lang.Get("Flymode not allowed"));
		}
		float speed = (float)args[0];
		if (speed > 500f)
		{
			return TextCommandResult.Success("Entered movespeed to high! max. 500x");
		}
		game.player.worlddata.SetMode(game, speed);
		return TextCommandResult.Success("Movespeed: " + speed + "x");
	}

	private TextCommandResult OnCmdNoClip(TextCommandCallingArgs args)
	{
		game.player.worlddata.RequestModeNoClip(game, (bool)args[0]);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdBlockItemPngExport(TextCommandCallingArgs args)
	{
		exportRequest = ((args[0] as string == "inv") ? EnumPngsExportRequest.CreativeInventory : EnumPngsExportRequest.All);
		size = (int)args[1];
		exportDomain = (args.Parsers[2].IsMissing ? null : (args[2] as string));
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdOnePngExportHand(TextCommandCallingArgs args)
	{
		exportRequest = EnumPngsExportRequest.Hand;
		size = (int)args[0];
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdOnePngExportCode(TextCommandCallingArgs args)
	{
		exportRequest = EnumPngsExportRequest.One;
		exportType = ((!(args[0] as string == "block")) ? EnumItemClass.Item : EnumItemClass.Block);
		exportCode = args[1] as string;
		size = (int)args[2];
		return TextCommandResult.Success();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
