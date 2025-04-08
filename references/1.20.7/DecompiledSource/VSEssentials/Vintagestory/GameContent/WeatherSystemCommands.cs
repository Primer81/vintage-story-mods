using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using AnimatedGif;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WeatherSystemCommands : ModSystem
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private ICoreAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public override void StartServerSide(ICoreServerAPI sapi)
	{
		this.sapi = sapi;
		sapi.Event.ServerRunPhase(EnumServerRunPhase.GameReady, delegate
		{
			sapi.ChatCommands.Create("whenwillitstopraining").WithDescription("When does it finally stop to rain around here?!").RequiresPrivilege(Privilege.controlserver)
				.RequiresPlayer()
				.HandleWith(CmdWhenWillItStopRaining);
			WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
			sapi.ChatCommands.Create("weather").WithDescription("Show/Set current weather info").RequiresPrivilege(Privilege.controlserver)
				.HandleWith(CmdWeatherinfo)
				.BeginSubCommand("setprecip")
				.WithDescription("Running with no arguments returns the current precip. override, if one is set. Including an argument overrides the precipitation intensity and in turn also the rain cloud overlay. '-1' removes all rain clouds, '0' stops any rain but keeps some rain clouds, while '1' causes the heaviest rain and full rain clouds. The server will remain indefinitely in that rain state until reset with '/weather setprecipa'.")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.OptionalFloat("level"))
				.HandleWith(CmdWeatherSetprecip)
				.EndSubCommand()
				.BeginSubCommand("setprecipa")
				.WithDescription("Resets the current precip override to auto mode.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherSetprecipa)
				.EndSubCommand()
				.BeginSubCommand("cloudypos")
				.WithAlias("cyp")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.OptionalFloat("level"))
				.HandleWith(CmdWeatherCloudypos)
				.EndSubCommand()
				.BeginSubCommand("stoprain")
				.WithDescription("Stops any current rain by forwarding to a time in the future where there is no rain.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherStoprain)
				.EndSubCommand()
				.BeginSubCommand("acp")
				.WithDescription("Toggles auto-changing weather patterns.")
				.RequiresPlayer()
				.WithArgs(sapi.ChatCommands.Parsers.OptionalBool("mode"))
				.HandleWith(CmdWeatherAcp)
				.EndSubCommand()
				.BeginSubCommand("lp")
				.WithDescription("Lists all loaded weather patterns.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherLp)
				.EndSubCommand()
				.BeginSubCommand("t")
				.WithDescription("Transitions to a random weather pattern.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherT)
				.EndSubCommand()
				.BeginSubCommand("c")
				.WithDescription("Quickly transitions to a random weather pattern.")
				.RequiresPlayer()
				.HandleWith(CmdWeatherC)
				.EndSubCommand()
				.BeginSubCommand("setw")
				.WithDescription("Sets the current wind pattern to the given wind pattern.")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("windpattern", modSystem.WindConfigs.Select((WindPatternConfig w) => w.Code).ToArray()))
				.HandleWith(CmdWeatherSetw)
				.EndSubCommand()
				.BeginSubCommand("randomevent")
				.RequiresPlayer()
				.HandleWith(CmdWeatherRandomevent)
				.EndSubCommand()
				.BeginSubCommand("setev")
				.WithAlias("setevr")
				.WithDescription("setev - Sets a weather event globally.\n  setevr - Set a weather event only in the player's region.")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("weather_event", modSystem.WeatherEventConfigs.Select((WeatherEventConfig w) => w.Code).ToArray()), api.ChatCommands.Parsers.OptionalBool("allowStop"))
				.HandleWith(CmdWeatherSetev)
				.EndSubCommand()
				.BeginSubCommand("set")
				.WithAlias("seti")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("weatherpattern", modSystem.WeatherConfigs.Select((WeatherPatternConfig w) => w.Code).ToArray()))
				.HandleWith(CmdWeatherSet)
				.EndSubCommand()
				.BeginSubCommand("setirandom")
				.RequiresPlayer()
				.HandleWith(CmdWeatherSetirandom)
				.EndSubCommand()
				.BeginSubCommand("setir")
				.RequiresPlayer()
				.WithArgs(api.ChatCommands.Parsers.WordRange("weatherpattern", modSystem.WeatherConfigs.Select((WeatherPatternConfig w) => w.Code).ToArray()))
				.HandleWith(CmdWeatherSetir)
				.EndSubCommand();
		});
	}

	private TextCommandResult CmdWeatherinfo(TextCommandCallingArgs args)
	{
		return TextCommandResult.Success(GetWeatherInfo<WeatherSystemServer>(args.Caller.Player));
	}

	private TextCommandResult CmdWeatherSetir(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.ReloadConfigs();
		string code = args.Parsers[0].GetValue() as string;
		BlockPos asBlockPos = (args.Caller.Player as IServerPlayer).Entity.SidedPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.World.BlockAccessor.RegionSize;
		int regionZ = asBlockPos.Z / api.World.BlockAccessor.RegionSize;
		long index2d = modSystem.MapRegionIndex2D(regionX, regionZ);
		modSystem.weatherSimByMapRegion.TryGetValue(index2d, out var weatherSim);
		if (weatherSim == null)
		{
			return TextCommandResult.Success("Weather sim not loaded (yet) for this region");
		}
		if (weatherSim.SetWeatherPattern(code, updateInstant: true))
		{
			weatherSim.TickEvery25ms(0.025f);
			return TextCommandResult.Success("Ok weather pattern set for current region");
		}
		return TextCommandResult.Error("No such weather pattern found");
	}

	private TextCommandResult CmdWeatherSetirandom(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.ReloadConfigs();
		bool ok = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> val in modSystem.weatherSimByMapRegion)
		{
			ok &= val.Value.SetWeatherPattern(val.Value.RandomWeatherPattern().config.Code, updateInstant: true);
			if (ok)
			{
				val.Value.TickEvery25ms(0.025f);
			}
		}
		if (ok)
		{
			return TextCommandResult.Success("Ok random weather pattern set");
		}
		return TextCommandResult.Error("No such weather pattern found");
	}

	private TextCommandResult CmdWeatherSet(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		string code = args.Parsers[0].GetValue() as string;
		modSystem.ReloadConfigs();
		bool ok = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> val in modSystem.weatherSimByMapRegion)
		{
			val.Value.ReloadPatterns(api.World.Seed);
			ok &= val.Value.SetWeatherPattern(code, updateInstant: true);
			if (ok)
			{
				val.Value.TickEvery25ms(0.025f);
			}
		}
		if (ok)
		{
			return TextCommandResult.Success("Ok weather pattern set for all loaded regions");
		}
		return TextCommandResult.Error("No such weather pattern found");
	}

	private TextCommandResult CmdWeatherSetev(TextCommandCallingArgs args)
	{
		string code = args.Parsers[0].GetValue() as string;
		bool allowStop = (bool)args.Parsers[1].GetValue();
		string subCmdCode = args.SubCmdCode;
		WeatherSystemServer wsysServer = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		wsysServer.ReloadConfigs();
		BlockPos asBlockPos = (args.Caller.Player as IServerPlayer).Entity.SidedPos.XYZ.AsBlockPos;
		int regionX = asBlockPos.X / api.World.BlockAccessor.RegionSize;
		int regionZ = asBlockPos.Z / api.World.BlockAccessor.RegionSize;
		if (subCmdCode == "setevr")
		{
			long index2d = wsysServer.MapRegionIndex2D(regionX, regionZ);
			wsysServer.weatherSimByMapRegion.TryGetValue(index2d, out var weatherSim);
			if (weatherSim == null)
			{
				return TextCommandResult.Success("Weather sim not loaded (yet) for this region");
			}
			if (weatherSim.SetWeatherEvent(code, updateInstant: true))
			{
				weatherSim.CurWeatherEvent.AllowStop = allowStop;
				weatherSim.CurWeatherEvent.OnBeginUse();
				weatherSim.TickEvery25ms(0.025f);
				return TextCommandResult.Success("Ok weather event for this region set");
			}
			return TextCommandResult.Error("No such weather event found");
		}
		bool ok = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> val in wsysServer.weatherSimByMapRegion)
		{
			ok &= val.Value.SetWeatherEvent(code, updateInstant: true);
			val.Value.CurWeatherEvent.AllowStop = allowStop;
			if (ok)
			{
				val.Value.CurWeatherEvent.OnBeginUse();
				val.Value.TickEvery25ms(0.025f);
			}
		}
		if (ok)
		{
			return TextCommandResult.Success("Ok weather event set for all loaded regions");
		}
		return TextCommandResult.Error("No such weather event found");
	}

	private TextCommandResult CmdWeatherRandomevent(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, WeatherSimulationRegion> val in sapi.ModLoader.GetModSystem<WeatherSystemServer>().weatherSimByMapRegion)
		{
			val.Value.selectRandomWeatherEvent();
			val.Value.sendWeatherUpdatePacket();
		}
		return TextCommandResult.Success("Random weather event selected for all regions");
	}

	private TextCommandResult CmdWeatherSetw(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		modSystem.ReloadConfigs();
		string code = args.Parsers[0].GetValue() as string;
		bool ok = true;
		foreach (KeyValuePair<long, WeatherSimulationRegion> val in modSystem.weatherSimByMapRegion)
		{
			val.Value.ReloadPatterns(api.World.Seed);
			ok &= val.Value.SetWindPattern(code, updateInstant: true);
			if (ok)
			{
				val.Value.TickEvery25ms(0.025f);
			}
		}
		if (ok)
		{
			return TextCommandResult.Success("Ok wind pattern set");
		}
		return TextCommandResult.Error("No such wind pattern found");
	}

	private TextCommandResult CmdWeatherC(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in sapi.ModLoader.GetModSystem<WeatherSystemServer>().weatherSimByMapRegion)
		{
			item.Value.TriggerTransition(1f);
		}
		return TextCommandResult.Success("Ok selected another weather pattern");
	}

	private TextCommandResult CmdWeatherT(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, WeatherSimulationRegion> item in sapi.ModLoader.GetModSystem<WeatherSystemServer>().weatherSimByMapRegion)
		{
			item.Value.TriggerTransition();
		}
		return TextCommandResult.Success("Ok transitioning to another weather pattern");
	}

	private TextCommandResult CmdWeatherLp(TextCommandCallingArgs args)
	{
		WeatherSystemServer wsysServer = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		string patterns = string.Join(", ", wsysServer.WeatherConfigs.Select((WeatherPatternConfig c) => c.Code));
		return TextCommandResult.Success("Patterns: " + patterns);
	}

	private TextCommandResult CmdWeatherAcp(TextCommandCallingArgs args)
	{
		WeatherSystemServer wsysServer = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		if (args.Parsers[0].IsMissing)
		{
			wsysServer.autoChangePatterns = !wsysServer.autoChangePatterns;
		}
		else
		{
			wsysServer.autoChangePatterns = (bool)args[0];
		}
		return TextCommandResult.Success("Ok autochange weather patterns now " + (wsysServer.autoChangePatterns ? "on" : "off"));
	}

	private TextCommandResult CmdWeatherStoprain(TextCommandCallingArgs args)
	{
		TextCommandResult result = RainStopFunc(args.Caller.Player, skipForward: true);
		sapi.ModLoader.GetModSystem<WeatherSystemServer>().broadCastConfigUpdate();
		return result;
	}

	private TextCommandResult CmdWeatherCloudypos(TextCommandCallingArgs args)
	{
		WeatherSystemServer wsysServer = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Cloud level rel = " + wsysServer.CloudLevelRel);
		}
		wsysServer.CloudLevelRel = (float)args.Parsers[0].GetValue();
		wsysServer.serverChannel.BroadcastPacket(new WeatherCloudYposPacket
		{
			CloudYRel = wsysServer.CloudLevelRel
		});
		return TextCommandResult.Success($"Cloud level rel {wsysServer.CloudLevelRel:0.##} set. (y={(int)(wsysServer.CloudLevelRel * (float)wsysServer.api.World.BlockAccessor.MapSizeY)})");
	}

	private TextCommandResult CmdWeatherSetprecip(TextCommandCallingArgs args)
	{
		WeatherSystemServer wsysServer = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		float level = (float)args.Parsers[0].GetValue();
		if (args.Parsers[0].IsMissing)
		{
			if (!wsysServer.OverridePrecipitation.HasValue)
			{
				return TextCommandResult.Success("Currently no precipitation override active.");
			}
			return TextCommandResult.Success($"Override precipitation value is currently at {wsysServer.OverridePrecipitation}.");
		}
		wsysServer.OverridePrecipitation = level;
		wsysServer.serverChannel.BroadcastPacket(new WeatherConfigPacket
		{
			OverridePrecipitation = wsysServer.OverridePrecipitation,
			RainCloudDaysOffset = wsysServer.RainCloudDaysOffset
		});
		return TextCommandResult.Success($"Ok precipitation set to {level}");
	}

	private TextCommandResult CmdWeatherSetprecipa(TextCommandCallingArgs args)
	{
		WeatherSystemServer wsysServer = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		wsysServer.OverridePrecipitation = null;
		wsysServer.serverChannel.BroadcastPacket(new WeatherConfigPacket
		{
			OverridePrecipitation = wsysServer.OverridePrecipitation,
			RainCloudDaysOffset = wsysServer.RainCloudDaysOffset
		});
		return TextCommandResult.Success("Ok auto precipitation on");
	}

	private TextCommandResult CmdPrecTestServerClimate(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		bool climate = (bool)args.Parsers[0].GetValue();
		WeatherSystemServer wsys = api.ModLoader.GetModSystem<WeatherSystemServer>();
		EntityPos pos = player.Entity.Pos;
		int wdt = 400;
		float hourStep = 4f;
		float days = 1f;
		float posStep = 2f;
		double totaldays = api.World.Calendar.TotalDays;
		ClimateCondition conds = api.World.BlockAccessor.GetClimateAt(new BlockPos((int)pos.X, (int)pos.Y, (int)pos.Z), EnumGetClimateMode.WorldGenValues, totaldays);
		int offset = wdt / 2;
		if (RuntimeEnv.OS != 0)
		{
			return TextCommandResult.Success("Command only supported on windows, try sub argument \"here\"");
		}
		Bitmap bmpgif = new Bitmap(wdt, wdt);
		int[] pixels = new int[wdt * wdt];
		using (AnimatedGifCreator gif = new AnimatedGifCreator("precip.gif", 100, -1))
		{
			for (int i = 0; (float)i < days * 24f; i++)
			{
				if (climate)
				{
					for (int dx = 0; dx < wdt; dx++)
					{
						for (int dz = 0; dz < wdt; dz++)
						{
							conds.Rainfall = (float)i / (days * 24f);
							float precip = wsys.GetRainCloudness(conds, pos.X + (double)((float)dx * posStep) - (double)offset, pos.Z + (double)((float)dz * posStep) - (double)offset, api.World.Calendar.TotalDays);
							int precipi = (int)GameMath.Clamp(255f * precip, 0f, 254f);
							pixels[dz * wdt + dx] = ColorUtil.ColorFromRgba(precipi, precipi, precipi, 255);
						}
					}
				}
				else
				{
					for (int dx2 = 0; dx2 < wdt; dx2++)
					{
						for (int dz2 = 0; dz2 < wdt; dz2++)
						{
							float precip2 = wsys.GetPrecipitation(pos.X + (double)((float)dx2 * posStep) - (double)offset, pos.Y, pos.Z + (double)((float)dz2 * posStep) - (double)offset, totaldays);
							int precipi2 = (int)GameMath.Clamp(255f * precip2, 0f, 254f);
							pixels[dz2 * wdt + dx2] = ColorUtil.ColorFromRgba(precipi2, precipi2, precipi2, 255);
						}
					}
				}
				totaldays += (double)(hourStep / 24f);
				bmpgif.SetPixels(pixels);
				gif.AddFrame(bmpgif, 100, GifQuality.Grayscale);
			}
		}
		return TextCommandResult.Success("Ok exported");
	}

	private TextCommandResult CmdPrecTestServerHere(TextCommandCallingArgs args)
	{
		WeatherSystemServer wsys = api.ModLoader.GetModSystem<WeatherSystemServer>();
		EntityPos pos = args.Caller.Player.Entity.Pos;
		double totaldays = api.World.Calendar.TotalDays;
		api.World.BlockAccessor.GetClimateAt(new BlockPos((int)pos.X, (int)pos.Y, (int)pos.Z), EnumGetClimateMode.WorldGenValues, totaldays);
		int wdt = 400;
		int offset = wdt / 2;
		SKBitmap bmp = new SKBitmap(wdt, wdt);
		int[] pixels = new int[wdt * wdt];
		float posStep = 3f;
		for (int dx = 0; dx < wdt; dx++)
		{
			for (int dz = 0; dz < wdt; dz++)
			{
				float x = (float)dx * posStep - (float)offset;
				float z = (float)dz * posStep - (float)offset;
				if ((int)x == 0 && (int)z == 0)
				{
					pixels[dz * wdt + dx] = ColorUtil.ColorFromRgba(255, 0, 0, 255);
					continue;
				}
				float precip = wsys.GetPrecipitation(pos.X + (double)x, pos.Y, pos.Z + (double)z, totaldays);
				int precipi = (int)GameMath.Clamp(255f * precip, 0f, 254f);
				pixels[dz * wdt + dx] = ColorUtil.ColorFromRgba(precipi, precipi, precipi, 255);
			}
		}
		bmp.SetPixels(pixels);
		bmp.Save("preciphere.png");
		return TextCommandResult.Success("Ok exported");
	}

	private TextCommandResult CmdPrecTestServerPos(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		EntityPos pos = args.Caller.Player.Entity.Pos;
		return TextCommandResult.Success("Prec here: " + modSystem.GetPrecipitation(pos.X, pos.Y, pos.Z, api.World.Calendar.TotalDays));
	}

	private TextCommandResult CmdSnowAccumHere(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		float amount = (float)args.Parsers[0].GetValue();
		BlockPos plrPos = args.Caller.Player.Entity.Pos.AsBlockPos;
		Vec2i chunkPos = new Vec2i(plrPos.X / 32, plrPos.Z / 32);
		IServerMapChunk mc = sapi.WorldManager.GetMapChunk(chunkPos.X, chunkPos.Y);
		int reso = WeatherSimulationRegion.snowAccumResolution;
		SnowAccumSnapshot sumsnapshot = new SnowAccumSnapshot
		{
			SumTemperatureByRegionCorner = new FloatDataMap3D(reso, reso, reso),
			SnowAccumulationByRegionCorner = new FloatDataMap3D(reso, reso, reso)
		};
		sumsnapshot.SnowAccumulationByRegionCorner.Data.Fill(amount);
		UpdateSnowLayerChunk updatepacket = modSystem.snowSimSnowAccu.UpdateSnowLayer(sumsnapshot, ignoreOldAccum: true, mc, chunkPos, null);
		modSystem.snowSimSnowAccu.accum = 1f;
		IBulkBlockAccessor ba = sapi.World.GetBlockAccessorBulkMinimalUpdate(synchronize: true);
		ba.UpdateSnowAccumMap = false;
		modSystem.snowSimSnowAccu.processBlockUpdates(mc, updatepacket, ba);
		ba.Commit();
		return TextCommandResult.Success("Ok, test snow accum gen complete");
	}

	private TextCommandResult CmdSnowAccumInfo(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		BlockPos plrPos = player.Entity.Pos.AsBlockPos;
		Vec2i chunkPos = new Vec2i(plrPos.X / 32, plrPos.Z / 32);
		double lastSnowAccumUpdateTotalHours = sapi.WorldManager.GetMapChunk(chunkPos.X, chunkPos.Y).GetModdata("lastSnowAccumUpdateTotalHours", 0.0);
		player.SendMessage(GlobalConstants.GeneralChatGroup, "lastSnowAccumUpdate: " + (api.World.Calendar.TotalHours - lastSnowAccumUpdateTotalHours) + " hours ago", EnumChatType.CommandSuccess);
		int regionX = (int)player.Entity.Pos.X / sapi.World.BlockAccessor.RegionSize;
		int regionZ = (int)player.Entity.Pos.Z / sapi.World.BlockAccessor.RegionSize;
		WeatherSystemServer modSystem = sapi.ModLoader.GetModSystem<WeatherSystemServer>();
		long index2d = modSystem.MapRegionIndex2D(regionX, regionZ);
		modSystem.weatherSimByMapRegion.TryGetValue(index2d, out var simregion);
		int reso = WeatherSimulationRegion.snowAccumResolution;
		float[] sumdata = new SnowAccumSnapshot
		{
			SnowAccumulationByRegionCorner = new FloatDataMap3D(reso, reso, reso)
		}.SnowAccumulationByRegionCorner.Data;
		float max = 3.5f;
		int len = simregion.SnowAccumSnapshots.Length;
		int i = simregion.SnowAccumSnapshots.EndPosition;
		while (len-- > 0)
		{
			SnowAccumSnapshot hoursnapshot = simregion.SnowAccumSnapshots[i];
			i = (i + 1) % simregion.SnowAccumSnapshots.Length;
			if (hoursnapshot != null)
			{
				float[] snowaccumdata = hoursnapshot.SnowAccumulationByRegionCorner.Data;
				for (int k = 0; k < snowaccumdata.Length; k++)
				{
					sumdata[k] = GameMath.Clamp(sumdata[k] + snowaccumdata[k], 0f - max, max);
				}
				lastSnowAccumUpdateTotalHours = Math.Max(lastSnowAccumUpdateTotalHours, hoursnapshot.TotalHours);
			}
		}
		for (int j = 0; j < sumdata.Length; j++)
		{
			player.SendMessage(GlobalConstants.GeneralChatGroup, j + ": " + sumdata[j], EnumChatType.CommandSuccess);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdSnowAccumProcesshere(TextCommandCallingArgs args)
	{
		WeatherSystemServer modSystem = api.ModLoader.GetModSystem<WeatherSystemServer>();
		BlockPos plrPos = args.Caller.Player.Entity.Pos.AsBlockPos;
		Vec2i chunkPos = new Vec2i(plrPos.X / 32, plrPos.Z / 32);
		modSystem.snowSimSnowAccu.AddToCheckQueue(chunkPos);
		return TextCommandResult.Success("Ok, added to check queue");
	}

	private TextCommandResult CmdSnowAccumOff(TextCommandCallingArgs args)
	{
		api.ModLoader.GetModSystem<WeatherSystemServer>().snowSimSnowAccu.ProcessChunks = false;
		return TextCommandResult.Success("Snow accum process chunks off");
	}

	private TextCommandResult CmdSnowAccumOn(TextCommandCallingArgs args)
	{
		api.ModLoader.GetModSystem<WeatherSystemServer>().snowSimSnowAccu.ProcessChunks = true;
		return TextCommandResult.Success("Snow accum process chunks on");
	}

	private TextCommandResult CmdWhenWillItStopRaining(TextCommandCallingArgs args)
	{
		return RainStopFunc(args.Caller.Player);
	}

	private TextCommandResult RainStopFunc(IPlayer player, bool skipForward = false)
	{
		WeatherSystemServer wsys = api.ModLoader.GetModSystem<WeatherSystemServer>();
		if (wsys.OverridePrecipitation.HasValue)
		{
			return TextCommandResult.Success("Override precipitation set, rain pattern will not change. Fix by typing /weather setprecipa.");
		}
		Vec3d pos = player.Entity.Pos.XYZ;
		float days = 0f;
		float daysrainless = 0f;
		float firstRainLessDay = 0f;
		bool found = false;
		for (; days < 21f; days += 1f / sapi.World.Calendar.HoursPerDay)
		{
			if (wsys.GetPrecipitation(pos.X, pos.Y, pos.Z, sapi.World.Calendar.TotalDays + (double)days) < 0.04f)
			{
				if (!found)
				{
					firstRainLessDay = days;
				}
				found = true;
				daysrainless += 1f / sapi.World.Calendar.HoursPerDay;
			}
			else if (found)
			{
				break;
			}
		}
		if (daysrainless > 0f)
		{
			if (skipForward)
			{
				wsys.RainCloudDaysOffset += daysrainless;
				return TextCommandResult.Success($"Ok, forwarded rain simulation by {firstRainLessDay:0.##} days. The rain should stop for about {daysrainless:0.##} days now", EnumChatType.CommandSuccess);
			}
			return TextCommandResult.Success($"In about {firstRainLessDay:0.##} days the rain should stop for about {daysrainless:0.##} days");
		}
		return TextCommandResult.Success("No rain less days found for the next 3 in-game weeks :O");
	}

	public override void StartClientSide(ICoreClientAPI capi)
	{
		this.capi = capi;
		this.capi.ChatCommands.Create("weather").WithDescription("Show current weather info").HandleWith(CmdWeatherClient);
	}

	private TextCommandResult CmdWeatherClient(TextCommandCallingArgs textCommandCallingArgs)
	{
		return TextCommandResult.Success(GetWeatherInfo<WeatherSystemClient>(capi.World.Player));
	}

	private string GetWeatherInfo<T>(IPlayer player) where T : WeatherSystemBase
	{
		T wsys = api.ModLoader.GetModSystem<T>();
		Vec3d plrPos = player.Entity.SidedPos.XYZ;
		BlockPos pos = plrPos.AsBlockPos;
		WeatherDataReaderPreLoad wreader = wsys.getWeatherDataReaderPreLoad();
		wreader.LoadAdjacentSimsAndLerpValues(plrPos, 1f);
		int regionX = pos.X / api.World.BlockAccessor.RegionSize;
		int regionZ = pos.Z / api.World.BlockAccessor.RegionSize;
		long index2d = wsys.MapRegionIndex2D(regionX, regionZ);
		wsys.weatherSimByMapRegion.TryGetValue(index2d, out var weatherSim);
		if (weatherSim == null)
		{
			return "weatherSim is null. No idea what to do here";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Weather by region:");
		string[] cornerNames = new string[4] { "tl", "tr", "bl", "br" };
		double tlLerp = GameMath.BiLerp(1.0, 0.0, 0.0, 0.0, wreader.LerpLeftRight, wreader.LerpTopBot);
		double trLerp = GameMath.BiLerp(0.0, 1.0, 0.0, 0.0, wreader.LerpLeftRight, wreader.LerpTopBot);
		double blLerp = GameMath.BiLerp(0.0, 0.0, 1.0, 0.0, wreader.LerpLeftRight, wreader.LerpTopBot);
		double brLerp = GameMath.BiLerp(0.0, 0.0, 0.0, 1.0, wreader.LerpLeftRight, wreader.LerpTopBot);
		int[] lerps = new int[4]
		{
			(int)(100.0 * tlLerp),
			(int)(100.0 * trLerp),
			(int)(100.0 * blLerp),
			(int)(100.0 * brLerp)
		};
		for (int i = 0; i < 4; i++)
		{
			WeatherSimulationRegion sim = wreader.AdjacentSims[i];
			if (sim == wsys.dummySim)
			{
				sb.AppendLine($"{cornerNames[i]}: missing");
				continue;
			}
			string weatherpattern = sim.OldWePattern.GetWeatherName();
			if (sim.Weight < 1f)
			{
				weatherpattern = $"{sim.OldWePattern.GetWeatherName()} transitioning to {sim.NewWePattern.GetWeatherName()} ({(int)(100f * sim.Weight)}%)";
			}
			sb.AppendLine(string.Format("{0}: {1}% {2}. Wind: {3} (str={4}), Event: {5}", cornerNames[i], lerps[i], weatherpattern, sim.CurWindPattern.GetWindName(), sim.GetWindSpeed(pos.Y).ToString("0.###"), sim.CurWeatherEvent.config.Code));
		}
		ClimateCondition climate = api.World.BlockAccessor.GetClimateAt(player.Entity.Pos.AsBlockPos);
		sb.AppendLine($"Current precipitation: {(int)(climate.Rainfall * 100f)}%");
		sb.AppendLine($"Current wind: {GlobalConstants.CurrentWindSpeedClient}");
		return sb.ToString();
	}
}
