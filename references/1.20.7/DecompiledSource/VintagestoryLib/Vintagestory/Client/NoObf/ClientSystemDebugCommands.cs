using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf;

internal class ClientSystemDebugCommands : ClientSystem
{
	public override string Name => "debmc";

	private WireframeModes wfmodes => game.api.renderapi.WireframeDebugRender;

	public ClientSystemDebugCommands(ClientMain game)
		: base(game)
	{
		IChatCommandApi chatCommands = game.api.ChatCommands;
		CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
		chatCommands.GetOrCreate("debug").WithDescription("Debug and Developer utilities").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("clobjc")
			.WithDescription("clobjc")
			.HandleWith(OnCmdClobjc)
			.EndSubCommand()
			.BeginSubCommand("self")
			.RequiresPrivilege(Privilege.chat)
			.HandleWith(OnCmdSelfDebugInfo)
			.EndSubCommand()
			.BeginSubCommand("talk")
			.WithDescription("talk")
			.WithArgs(parsers.OptionalWordRange("talk", Enum.GetNames<EnumTalkType>()))
			.HandleWith(OnCmdTalk)
			.EndSubCommand()
			.BeginSubCommand("normalview")
			.WithDescription("normalview")
			.HandleWith(OnCmdNormalview)
			.EndSubCommand()
			.BeginSubCommand("perceptioneffect")
			.WithAlias("pc")
			.WithDescription("perceptioneffect")
			.WithArgs(parsers.OptionalWord("effectname"), parsers.OptionalFloat("intensity", 1f))
			.HandleWith(OnCmdPerceptioneffect)
			.EndSubCommand()
			.BeginSubCommand("debdc")
			.WithDescription("debdc")
			.HandleWith(OnCmdDebdc)
			.EndSubCommand()
			.BeginSubCommand("tofb")
			.WithDescription("tofb")
			.WithArgs(parsers.OptionalBool("enable"))
			.HandleWith(OnCmdTofb)
			.EndSubCommand()
			.BeginSubCommand("cmr")
			.WithDescription("cmr")
			.HandleWith(OnCmdCmr)
			.EndSubCommand()
			.BeginSubCommand("us")
			.WithDescription("us")
			.HandleWith(OnCmdUs)
			.EndSubCommand()
			.BeginSubCommand("gl")
			.WithDescription("gl")
			.WithArgs(parsers.OptionalBool("GlDebugMode"))
			.HandleWith(OnCmdGl)
			.EndSubCommand()
			.BeginSubCommand("plranims")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("plranims")
			.HandleWith(OnCmdPlranims)
			.EndSubCommand()
			.BeginSubCommand("uiclick")
			.WithDescription("uiclick")
			.HandleWith(OnCmdUiclick)
			.EndSubCommand()
			.BeginSubCommand("discovery")
			.WithDescription("discovery")
			.WithArgs(parsers.All("text"))
			.HandleWith(OnCmdDiscovery)
			.EndSubCommand()
			.BeginSubCommand("soundsummary")
			.WithDescription("soundsummary")
			.HandleWith(OnCmdSoundsummary)
			.EndSubCommand()
			.BeginSubCommand("meshsummary")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("meshsummary")
			.HandleWith(OnCmdMeshsummary)
			.EndSubCommand()
			.BeginSubCommand("chunksummary")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("chunksummary")
			.HandleWith(OnCmdChunksummary)
			.EndSubCommand()
			.BeginSubCommand("logticks")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("logticks")
			.WithArgs(parsers.OptionalInt("ticksThreshold", 40))
			.HandleWith(OnCmdLogticks)
			.EndSubCommand()
			.BeginSubCommand("renderers")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("renderers")
			.WithArgs(parsers.OptionalBool("print"))
			.HandleWith(OnCmdRenderers)
			.EndSubCommand()
			.BeginSubCommand("exptexatlas")
			.WithDescription("exptexatlas")
			.WithArgs(parsers.OptionalWordRange("atlas", "block", "item", "entity"))
			.HandleWith(OnCmdExptexatlas)
			.EndSubCommand()
			.BeginSubCommand("liquidselectable")
			.WithDescription("liquidselectable")
			.WithArgs(parsers.OptionalBool("forceLiquidSelectable"))
			.HandleWith(OnCmdLiquidselectable)
			.EndSubCommand()
			.BeginSubCommand("relightchunk")
			.WithDescription("relightchunk")
			.HandleWith(OnCmdRelightchunk)
			.EndSubCommand()
			.BeginSubCommand("fog")
			.WithDescription("fog")
			.WithArgs(parsers.OptionalFloat("density"), parsers.OptionalFloat("min", 1f))
			.HandleWith(OnCmdFog)
			.EndSubCommand()
			.BeginSubCommand("fov")
			.WithDescription("fov")
			.WithArgs(parsers.OptionalInt("fov"))
			.HandleWith(OnCmdFov)
			.EndSubCommand()
			.BeginSubCommand("wgen")
			.WithDescription("wgen")
			.HandleWith(OnWgenCommand)
			.EndSubCommand()
			.BeginSubCommand("redrawall")
			.WithDescription("redrawall")
			.HandleWith(OnRedrawAll)
			.EndSubCommand()
			.BeginSubCommand("ci")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("ci")
			.HandleWith(OnChunkInfo)
			.EndSubCommand()
			.BeginSubCommand("plrattr")
			.WithDescription("plrattr")
			.WithArgs(parsers.All("path"))
			.HandleWith(OnCmdPlrattr)
			.EndSubCommand()
			.BeginSubCommand("crw")
			.WithDescription("crw")
			.HandleWith(OnCmdCrw)
			.EndSubCommand()
			.BeginSubCommand("shake")
			.WithDescription("shake")
			.WithArgs(parsers.OptionalFloat("strength", 0.5f))
			.HandleWith(OnCmdShake)
			.EndSubCommand()
			.BeginSubCommand("recalctrav")
			.WithDescription("recalctrav")
			.HandleWith(OnCmdRecalctrav)
			.EndSubCommand()
			.BeginSubCommand("wireframe")
			.WithDescription("View wireframes showing various game elements")
			.BeginSubCommand("scene")
			.WithDescription("GUI elements converted to wireframe triangles")
			.HandleWith(OnCmdScene)
			.EndSubCommand()
			.BeginSubCommand("ambsounds")
			.WithDescription("Show the current sources of ambient sounds")
			.HandleWith(OnCmdAmbsounds)
			.EndSubCommand()
			.BeginSubCommand("entity")
			.WithDescription("For every entity, the collision box (red) and selection box (blue)")
			.HandleWith(OnCmdEntity)
			.EndSubCommand()
			.BeginSubCommand("chunk")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The boundaries of the current chunk")
			.HandleWith(OnCmdChunk)
			.EndSubCommand()
			.BeginSubCommand("inside")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The block(s) the player is currently 'inside'")
			.HandleWith(OnCmdInside)
			.EndSubCommand()
			.BeginSubCommand("serverchunk")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The boundaries of the current serverchunk")
			.HandleWith(OnCmdServerchunk)
			.EndSubCommand()
			.BeginSubCommand("region")
			.RequiresPrivilege(Privilege.chat)
			.WithDescription("The boundaries of the current MapRegion")
			.HandleWith(OnCmdRegion)
			.EndSubCommand()
			.BeginSubCommand("blockentity")
			.WithDescription("All the BlockEntities")
			.HandleWith(OnCmdBlockentity)
			.EndSubCommand()
			.BeginSubCommand("landclaim")
			.WithDescription("All the LandClaims in the current Map region")
			.HandleWith(OnCmdLandClaim)
			.EndSubCommand()
			.BeginSubCommand("structures")
			.WithDescription("All the Structures in the current mapregion")
			.HandleWith(OnCmdStructure)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("find")
			.WithDescription("find")
			.WithArgs(parsers.Word("searchString"))
			.HandleWith(OnCmdFind)
			.EndSubCommand()
			.BeginSubCommand("dumpanimstate")
			.WithDescription("Dump animation state into log file")
			.WithArgs(parsers.Entities("target entity"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => handleDumpAnimState(e, args)))
			.EndSubCommand();
	}

	private TextCommandResult handleDumpAnimState(Entity e, TextCommandCallingArgs args)
	{
		game.Logger.Notification(e.AnimManager?.Animator?.DumpCurrentState());
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdSelfDebugInfo(TextCommandCallingArgs args)
	{
		game.EntityPlayer.UpdateDebugAttributes();
		StringBuilder text = new StringBuilder();
		foreach (KeyValuePair<string, IAttribute> val in game.EntityPlayer.DebugAttributes)
		{
			text.AppendLine(val.Key + ": " + val.Value.ToString());
		}
		return TextCommandResult.Success(text.ToString());
	}

	private TextCommandResult OnCmdFind(TextCommandCallingArgs args)
	{
		if (game.EntityPlayer.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			if (args.Parsers[0].IsMissing)
			{
				return TextCommandResult.Success(Lang.Get("Specify all or part of the name of a block to find"));
			}
			game.FindCmd(args[0] as string);
			return TextCommandResult.Success();
		}
		return TextCommandResult.Success(Lang.Get("Need to be in Creative mode to use the command .debug find [blockname]"));
	}

	private TextCommandResult OnCmdBlockentity(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.BlockEntity, "Block entity wireframes");
	}

	private TextCommandResult OnCmdStructure(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Structures, "Structure wireframes");
	}

	private TextCommandResult OnCmdRegion(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Region, "Region wireframe");
	}

	private TextCommandResult OnCmdLandClaim(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.LandClaim, "Land claim wireframe");
	}

	private TextCommandResult OnCmdServerchunk(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.ServerChunk, "Server chunk wireframe");
	}

	private TextCommandResult OnCmdChunk(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Chunk, "Chunk wireframe");
	}

	private TextCommandResult OnCmdEntity(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Entity, "Entity wireframes");
	}

	private TextCommandResult OnCmdInside(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.Inside, "Inside block wireframe");
	}

	private TextCommandResult OnCmdAmbsounds(TextCommandCallingArgs args)
	{
		return WireframeCommon(ref wfmodes.AmbientSounds, "Ambient sounds wireframes");
	}

	private TextCommandResult WireframeCommon(ref bool toggle, string name)
	{
		toggle = !toggle;
		return TextCommandResult.Success(Lang.Get(name + " now {0}", toggle ? Lang.Get("on") : Lang.Get("off")));
	}

	private TextCommandResult OnCmdScene(TextCommandCallingArgs args)
	{
		game.Platform.GLWireframes(wfmodes.Vertex = !wfmodes.Vertex);
		return TextCommandResult.Success(Lang.Get("Scene wireframes now {0}", wfmodes.Vertex ? Lang.Get("on") : Lang.Get("off")));
	}

	private TextCommandResult OnCmdRecalctrav(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, ClientChunk> val in game.WorldMap.chunks)
		{
			ChunkPos vec = game.WorldMap.ChunkPosFromChunkIndex3D(val.Key);
			if (vec.Dimension == 0)
			{
				lock (game.chunkPositionsLock)
				{
					game.chunkPositionsForRegenTrav.Add(vec);
				}
			}
		}
		return TextCommandResult.Success("Ok queued all chunks to recalc their traverseability");
	}

	private TextCommandResult OnCmdCrw(TextCommandCallingArgs args)
	{
		BlockPos pos = game.EntityPlayer.Pos.AsBlockPos;
		game.WorldMap.MarkChunkDirty(pos.X / 32, pos.Y / 32, pos.Z / 32);
		return TextCommandResult.Success("Ok, chunk marked dirty for redraw");
	}

	private TextCommandResult OnCmdPlrattr(TextCommandCallingArgs args)
	{
		string path = args[0] as string;
		IAttribute attr = game.EntityPlayer.WatchedAttributes.GetAttributeByPath(path);
		if (attr == null)
		{
			return TextCommandResult.Success("No such path found");
		}
		return TextCommandResult.Success(Lang.Get("Value is: {0}", attr.GetValue()));
	}

	private TextCommandResult OnCmdRenderers(TextCommandCallingArgs args)
	{
		if (game.eventManager == null)
		{
			return TextCommandResult.Error("Client already shutting down");
		}
		List<RenderHandler>[] renderers = game.eventManager.renderersByStage;
		StringBuilder sb = new StringBuilder();
		bool print = (bool)args[0];
		Dictionary<string, int> rendererSummary = new Dictionary<string, int>();
		for (int i = 0; i < renderers.Length; i++)
		{
			EnumRenderStage stage = (EnumRenderStage)i;
			sb.AppendLine(stage.ToString() + ": " + renderers[i].Count);
			if (!print)
			{
				continue;
			}
			foreach (RenderHandler item in renderers[i])
			{
				string key = item.Renderer.GetType()?.ToString() ?? "";
				if (rendererSummary.ContainsKey(key))
				{
					rendererSummary[key]++;
				}
				else
				{
					rendererSummary[key] = 1;
				}
			}
		}
		game.ShowChatMessage("Renderers:");
		game.ShowChatMessage(sb.ToString());
		if (print)
		{
			game.Logger.Notification("Renderer summary:");
			foreach (KeyValuePair<string, int> val in rendererSummary)
			{
				game.Logger.Notification(val.Value + "x " + val.Key);
			}
			game.ShowChatMessage("Summary printed to client log file");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdLogticks(TextCommandCallingArgs args)
	{
		ScreenManager.FrameProfiler.PrintSlowTicks = !ScreenManager.FrameProfiler.PrintSlowTicks;
		ScreenManager.FrameProfiler.Enabled = ScreenManager.FrameProfiler.PrintSlowTicks;
		ScreenManager.FrameProfiler.PrintSlowTicksThreshold = (int)args[0];
		ScreenManager.FrameProfiler.Begin(null);
		game.ShowChatMessage("Client Tick Profiling now " + (ScreenManager.FrameProfiler.PrintSlowTicks ? ("on, threshold " + ScreenManager.FrameProfiler.PrintSlowTicksThreshold + " ms") : "off"));
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdChunksummary(TextCommandCallingArgs args)
	{
		int total = 0;
		int packed = 0;
		int cntData = 0;
		int cntEmpty = 0;
		foreach (KeyValuePair<long, ClientChunk> val in game.WorldMap.chunks)
		{
			total++;
			if (val.Value.IsPacked())
			{
				packed++;
			}
			if (val.Value.Empty)
			{
				cntEmpty++;
			}
			else
			{
				cntData++;
			}
		}
		game.ShowChatMessage($"{total} Total chunks ({cntData} with data and {cntEmpty} empty)\n{packed} of which are packed");
		ClientChunkDataPool pool = game.WorldMap.chunkDataPool;
		game.ShowChatMessage($"Free pool objects {pool.CountFree()}");
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMeshsummary(TextCommandCallingArgs args)
	{
		Dictionary<string, int> grouped = new Dictionary<string, int>();
		for (int blockid = 0; blockid < game.TesselatorManager.blockModelDatas.Length; blockid++)
		{
			MeshData mesh = game.TesselatorManager.blockModelDatas[blockid];
			if (mesh == null)
			{
				continue;
			}
			Block block = game.Blocks[blockid];
			int size = mesh.SizeInBytes();
			int sizeSum = 0;
			grouped.TryGetValue(block.FirstCodePart(), out sizeSum);
			sizeSum += size;
			MeshData[] meshes = game.TesselatorManager.altblockModelDatasLod1[blockid];
			int k = 0;
			while (meshes != null && k < meshes.Length)
			{
				MeshData altmesh3 = meshes[k];
				if (altmesh3 != null)
				{
					sizeSum += altmesh3.SizeInBytes();
				}
				k++;
			}
			MeshData[][] altblockModelDatasLod = game.TesselatorManager.altblockModelDatasLod0;
			meshes = ((altblockModelDatasLod != null) ? altblockModelDatasLod[blockid] : null);
			int j = 0;
			while (meshes != null && j < meshes.Length)
			{
				MeshData altmesh2 = meshes[j];
				if (altmesh2 != null)
				{
					sizeSum += altmesh2.SizeInBytes();
				}
				j++;
			}
			MeshData[][] altblockModelDatasLod2 = game.TesselatorManager.altblockModelDatasLod2;
			meshes = ((altblockModelDatasLod2 != null) ? altblockModelDatasLod2[blockid] : null);
			int i = 0;
			while (meshes != null && i < meshes.Length)
			{
				MeshData altmesh = meshes[i];
				if (altmesh != null)
				{
					sizeSum += altmesh.SizeInBytes();
				}
				i++;
			}
			grouped[block.FirstCodePart()] = sizeSum;
		}
		foreach (KeyValuePair<string, int> val in grouped)
		{
			if (val.Value > 102400)
			{
				game.Logger.Debug("{0}: {1} kb", val.Key, val.Value / 1024);
			}
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdSoundsummary(TextCommandCallingArgs args)
	{
		int loaded = 0;
		int total = ScreenManager.soundAudioData.Count;
		foreach (KeyValuePair<AssetLocation, AudioData> val in ScreenManager.soundAudioData)
		{
			if (val.Value.Loaded == 1)
			{
				loaded++;
				if ((val.Value as AudioMetaData).Pcm.Length > 100000)
				{
					game.Logger.Debug("{0}: {1} kb", val.Key, (val.Value as AudioMetaData).Pcm.Length / 1024);
				}
			}
		}
		return TextCommandResult.Success($"{loaded} of {total} sounds loaded");
	}

	private TextCommandResult OnCmdDiscovery(TextCommandCallingArgs args)
	{
		string text = args[0] as string;
		game.eventManager?.TriggerIngameDiscovery(this, "no", text);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdUiclick(TextCommandCallingArgs args)
	{
		GuiManager.DEBUG_PRINT_INTERACTIONS = !GuiManager.DEBUG_PRINT_INTERACTIONS;
		return TextCommandResult.Success("UI Debug pring interactions now " + (GuiManager.DEBUG_PRINT_INTERACTIONS ? "on" : "off"));
	}

	private TextCommandResult OnCmdPlranims(TextCommandCallingArgs args)
	{
		IAnimationManager AnimManager = game.player.Entity.AnimManager;
		string anims = "";
		int i = 0;
		foreach (string anim2 in AnimManager.ActiveAnimationsByAnimCode.Keys)
		{
			if (i++ > 0)
			{
				anims += ",";
			}
			anims += anim2;
		}
		i = 0;
		StringBuilder runninganims = new StringBuilder();
		RunningAnimation[] animations = AnimManager.Animator.Animations;
		foreach (RunningAnimation anim in animations)
		{
			if (anim.Active)
			{
				if (i++ > 0)
				{
					runninganims.Append(",");
				}
				runninganims.Append(anim.Animation.Code);
			}
		}
		game.ShowChatMessage("Active Animations: " + ((anims.Length > 0) ? anims : "-"));
		return TextCommandResult.Success("Running Animations: " + ((runninganims.Length > 0) ? runninganims.ToString() : "-"));
	}

	private TextCommandResult OnCmdGl(TextCommandCallingArgs args)
	{
		ScreenManager.Platform.GlDebugMode = (bool)args[0];
		return TextCommandResult.Success("OpenGL debug mode now " + (ScreenManager.Platform.GlDebugMode ? "on" : "off"));
	}

	private TextCommandResult OnCmdUs(TextCommandCallingArgs args)
	{
		game.unbindSamplers = !game.unbindSamplers;
		return TextCommandResult.Success("Unpind samplers mode now " + (game.unbindSamplers ? "on" : "off"));
	}

	private TextCommandResult OnCmdCmr(TextCommandCallingArgs args)
	{
		float[] arr = game.shUniforms.ColorMapRects4;
		for (int i = 0; i < arr.Length; i += 4)
		{
			game.Logger.Notification("x: {0}, y: {1}, w: {2}, h: {3}", arr[i], arr[i + 1], arr[i + 2], arr[i + 3]);
		}
		return TextCommandResult.Success("Color map rects printed to client-main.log");
	}

	private TextCommandResult OnCmdTofb(TextCommandCallingArgs args)
	{
		ScreenManager.Platform.ToggleOffscreenBuffer((bool)args[0]);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdDebdc(TextCommandCallingArgs args)
	{
		ScreenManager.debugDrawCallNextFrame = true;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdPerceptioneffect(TextCommandCallingArgs args)
	{
		PerceptionEffects pcReg = game.api.Render.PerceptionEffects;
		if (args.Parsers[0].IsMissing)
		{
			StringBuilder sbe = new StringBuilder();
			sbe.Append("Missing effect name argument. Available: ");
			int i = 0;
			foreach (string vap in pcReg.RegisteredEffects)
			{
				if (i > 0)
				{
					sbe.Append(", ");
				}
				i++;
				sbe.Append(vap);
			}
			return TextCommandResult.Success(sbe.ToString());
		}
		string effectname = args[0] as string;
		if (pcReg.RegisteredEffects.Contains(effectname))
		{
			pcReg.TriggerEffect(effectname, (float)args[1]);
			return TextCommandResult.Success();
		}
		return TextCommandResult.Success("No such effect registered.");
	}

	private TextCommandResult OnCmdNormalview(TextCommandCallingArgs args)
	{
		ShaderRegistry.NormalView = !ShaderRegistry.NormalView;
		bool ok = ShaderRegistry.ReloadShaders();
		bool ok2 = game.eventManager != null && game.eventManager.TriggerReloadShaders();
		ok = ok && ok2;
		return TextCommandResult.Success("Shaders reloaded" + (ok ? "" : ". errors occured, please check client log"));
	}

	private TextCommandResult OnCmdTalk(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			StringBuilder sbt = new StringBuilder();
			foreach (object talktype in Enum.GetValues(typeof(EnumTalkType)))
			{
				if (sbt.Length > 0)
				{
					sbt.Append(", ");
				}
				sbt.Append(talktype);
			}
			return TextCommandResult.Success(sbt.ToString());
		}
		if (Enum.TryParse<EnumTalkType>(args[0] as string, ignoreCase: true, out var tt))
		{
			game.api.World.Player.Entity.talkUtil.Talk(tt);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdClobjc(TextCommandCallingArgs args)
	{
		game.api.ObjectCache.Clear();
		return TextCommandResult.Success("Ok, cleared");
	}

	private TextCommandResult OnCmdFog(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing && args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success("Current fog density = " + game.AmbientManager.Base.FogDensity.Value + ", fog min= " + game.AmbientManager.Base.FogMin.Value);
		}
		float density = (float)args[0];
		float min = (float)args[1];
		game.AmbientManager.SetFogRange(density, min);
		return TextCommandResult.Success("Fog set to density=" + density + ", min=" + min);
	}

	private TextCommandResult OnCmdFov(TextCommandCallingArgs args)
	{
		int fov = (int)args[0];
		int minfov = 1;
		int maxfov = 179;
		if (!game.IsSingleplayer)
		{
			minfov = 60;
		}
		if (fov < minfov || fov > maxfov)
		{
			return TextCommandResult.Success($"Valid field of view: {minfov}-{maxfov}");
		}
		float fov_ = (float)Math.PI * 2f * ((float)fov / 360f);
		game.MainCamera.Fov = fov_;
		game.OnResize();
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdShake(TextCommandCallingArgs args)
	{
		float strength = (float)args[0];
		game.MainCamera.CameraShakeStrength += strength;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdLiquidselectable(TextCommandCallingArgs args)
	{
		if (!args.Parsers[0].IsMissing)
		{
			game.forceLiquidSelectable = (bool)args[0];
		}
		else
		{
			game.forceLiquidSelectable = !game.forceLiquidSelectable;
		}
		return TextCommandResult.Success("Forced Liquid selectable now " + (game.LiquidSelectable ? "on" : "off"));
	}

	private TextCommandResult OnCmdRelightchunk(TextCommandCallingArgs args)
	{
		BlockPos chunkpos = game.EntityPlayer.Pos.AsBlockPos / 32;
		ClientChunk chunk = game.WorldMap.GetClientChunk(chunkpos.X, chunkpos.Y, chunkpos.Z);
		game.terrainIlluminator.SunRelightChunk(chunk, chunkpos.X, chunkpos.Y, chunkpos.Z);
		long chunkindex3d = game.WorldMap.ChunkIndex3D(chunkpos.X, chunkpos.Y, chunkpos.Z);
		game.WorldMap.SetChunkDirty(chunkindex3d, priority: true);
		return TextCommandResult.Success("Chunk sunlight recaculated and queued for redrawing");
	}

	private TextCommandResult OnCmdExptexatlas(TextCommandCallingArgs args)
	{
		if (!(args[0] is string type))
		{
			return TextCommandResult.Success();
		}
		TextureAtlasManager mgr = null;
		string name = "";
		switch (type)
		{
		case "block":
			mgr = game.BlockAtlasManager;
			name = "Block";
			break;
		case "item":
			mgr = game.ItemAtlasManager;
			name = "Item";
			break;
		case "entity":
			mgr = game.EntityAtlasManager;
			name = "Entity";
			break;
		}
		if (mgr == null)
		{
			return TextCommandResult.Success("Usage: /exptexatlas [block, item or entity]");
		}
		for (int i = 0; i < mgr.Atlasses.Count; i++)
		{
			mgr.Atlasses[i].Export(type + "Atlas-" + i, game, mgr.AtlasTextures[i].TextureId);
		}
		return TextCommandResult.Success(name + " atlas(ses) exported");
	}

	private TextCommandResult OnChunkInfo(TextCommandCallingArgs textCommandCallingArgs)
	{
		BlockPos pos = game.EntityPlayer.Pos.AsBlockPos;
		ClientChunk chunk = game.WorldMap.GetChunkAtBlockPos(pos.X, pos.Y, pos.Z);
		if (chunk == null)
		{
			game.ShowChatMessage("Not loaded yet");
		}
		else
		{
			string rendering = "no";
			if (chunk.centerModelPoolLocations != null)
			{
				rendering = "center";
			}
			if (chunk.edgeModelPoolLocations != null)
			{
				rendering = ((chunk.centerModelPoolLocations != null) ? "yes" : "edge");
			}
			game.ShowChatMessage($"Loaded: {chunk.loadedFromServer}, Rendering: {rendering}, #Drawn: {chunk.quantityDrawn}, #Relit: {chunk.quantityRelit}, Queued4Redraw: {chunk.enquedForRedraw}, Queued4Upload: {chunk.queuedForUpload}, Packed: {chunk.IsPacked()}, Empty: {chunk.Empty}");
			game.ShowChatMessage("Traversability: " + Convert.ToString(chunk.traversability, 2));
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnRedrawAll(TextCommandCallingArgs textCommandCallingArgs)
	{
		game.RedrawAllBlocks();
		return TextCommandResult.Success("Ok, will redraw all chunks, might take some time to take effect.");
	}

	private TextCommandResult OnWgenCommand(TextCommandCallingArgs textCommandCallingArgs)
	{
		BlockPos pos = game.EntityPlayer.Pos.AsBlockPos;
		int climate = game.WorldMap.GetClimate(pos.X, pos.Z);
		int rain = Climate.GetRainFall((climate >> 8) & 0xFF, pos.Y);
		int temp = Climate.GetAdjustedTemperature((climate >> 16) & 0xFF, pos.Y - ClientWorldMap.seaLevel);
		game.ShowChatMessage("Rain=" + rain + ", temp=" + temp);
		return TextCommandResult.Success();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
