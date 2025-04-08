using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModTemperature : ModSystem
{
	private ICoreAPI api;

	public SimplexNoise YearlyTemperatureNoise;

	public SimplexNoise DailyTemperatureNoise;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		api.Event.OnGetClimate += Event_OnGetClimate;
		YearlyTemperatureNoise = SimplexNoise.FromDefaultOctaves(3, 0.001, 0.95, api.World.Seed + 12109);
		DailyTemperatureNoise = SimplexNoise.FromDefaultOctaves(3, 1.0, 0.95, api.World.Seed + 128109);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("exptempplot").WithDescription("Export a 1 year long temperatures at a 6 hour interval at this location")
			.RequiresPrivilege(Privilege.controlserver)
			.RequiresPlayer()
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				exportPlotHere(args.Caller.Entity.Pos.AsBlockPos);
				return TextCommandResult.Success("ok exported");
			})
			.EndSubCommand();
	}

	private void Event_OnGetClimate(ref ClimateCondition climate, BlockPos pos, EnumGetClimateMode mode, double totalDays)
	{
		if (mode != 0)
		{
			double yearRel = totalDays / (double)api.World.Calendar.DaysPerYear % 1.0;
			double hourOfDay = totalDays % 1.0 * (double)api.World.Calendar.HoursPerDay;
			updateTemperature(ref climate, pos, yearRel, hourOfDay, totalDays);
		}
	}

	private void updateTemperature(ref ClimateCondition climate, BlockPos pos, double yearRel, double hourOfDay, double totalDays)
	{
		double heretemp = climate.WorldGenTemperature;
		double latitude = api.World.Calendar.OnGetLatitude(pos.Z);
		double seasonalVariationAmplitude = Math.Abs(latitude) * 65.0;
		heretemp -= seasonalVariationAmplitude / 2.0;
		float? seasonOverride = api.World.Calendar.SeasonOverride;
		if (seasonOverride.HasValue)
		{
			double distanceToJanuary = GameMath.Smootherstep(Math.Abs(GameMath.CyclicValueDistance(0.5f, seasonOverride.Value * 12f, 12f) / 6f));
			heretemp += seasonalVariationAmplitude * distanceToJanuary;
		}
		else if (latitude > 0.0)
		{
			double distanceToJanuary2 = GameMath.Smootherstep(Math.Abs(GameMath.CyclicValueDistance(0.5, yearRel * 12.0, 12.0) / 6.0));
			heretemp += seasonalVariationAmplitude * distanceToJanuary2;
		}
		else
		{
			double distanceToJuly = GameMath.Smootherstep(Math.Abs(GameMath.CyclicValueDistance(6.5, yearRel * 12.0, 12.0) / 6.0));
			heretemp += seasonalVariationAmplitude * distanceToJuly;
		}
		double diurnalVariationAmplitude = 18f - climate.Rainfall * 13f;
		double distanceTo6Am = GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(4.0, hourOfDay, 24.0) / 12.0));
		heretemp += (distanceTo6Am - 0.5) * diurnalVariationAmplitude;
		heretemp += YearlyTemperatureNoise.Noise(totalDays, 0.0) * 3.0;
		heretemp += DailyTemperatureNoise.Noise(totalDays, 0.0);
		climate.Temperature = (float)heretemp;
	}

	private void exportPlotHere(BlockPos pos)
	{
		ClimateCondition cond = api.World.BlockAccessor.GetClimateAt(pos);
		double totalhours = 0.0;
		double starttemp = cond.Temperature;
		double hoursPerday = api.World.Calendar.HoursPerDay;
		double daysPerYear = api.World.Calendar.DaysPerYear;
		double daysPerMonth = api.World.Calendar.DaysPerMonth;
		double monthsPerYear = daysPerYear / daysPerMonth;
		List<string> entries = new List<string>();
		for (double plothours = 0.0; plothours < 3456.0; plothours += 1.0)
		{
			cond.Temperature = (float)starttemp;
			double totalDays = totalhours / hoursPerday;
			double yearRel = totalDays / daysPerYear % 1.0;
			double hourOfDay = totalhours % hoursPerday;
			double month = yearRel * monthsPerYear;
			updateTemperature(ref cond, pos, yearRel, hourOfDay, totalDays);
			entries.Add($"{(int)(totalDays % daysPerMonth) + 1}.{(int)month + 1}.{(int)(totalDays / daysPerYear + 1386.0)} {(int)hourOfDay}:00" + ";" + cond.Temperature);
			totalhours += 1.0;
		}
		File.WriteAllText("temperatureplot.csv", string.Join("\r\n", entries));
	}
}
