using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

internal class CmdTime
{
	private ServerMain server;

	public CmdTime(ServerMain server)
	{
		this.server = server;
		IChatCommandApi cmdapi = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		cmdapi.Create("time").RequiresPrivilege(Privilege.time).HandleWith(cmdGetTime)
			.WithDescription("Get or set world time or time speed")
			.BeginSub("stop")
			.WithDesc("Stop passage of time and time affected processes")
			.HandleWith(cmdStopTime)
			.EndSub()
			.BeginSub("resume")
			.WithDesc("Resume passage of time and time affected processes")
			.HandleWith(cmdResumeTime)
			.EndSub()
			.BeginSub("speed")
			.WithDesc("Get/Set speed of time passage. Not recommended for normal gameplay! If you want longer days, use /time calendarspeedmul")
			.WithArgs(parsers.OptionalFloat("speed", 60f))
			.HandleWith(cmdTimeSpeed)
			.EndSub()
			.BeginSub("set")
			.WithDesc("Fast forward to a time of day")
			.WithArgs(parsers.Word("24 hour format or word", new string[11]
			{
				"lunch", "day", "night", "latenight", "morning", "latemorning", "sunrise", "sunset", "afternoon", "midnight",
				"witchinghour"
			}))
			.HandleWith(cmdTimeSet)
			.EndSub()
			.BeginSub("setmonth")
			.WithDesc("Fast forward to a given month")
			.WithArgs(parsers.WordRange("month", "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"))
			.HandleWith(cmdTimeSetMonth)
			.EndSub()
			.BeginSub("add")
			.WithDesc("Fast forward by given time span")
			.WithArgs(parsers.Float("amount"), parsers.OptionalWordRange("span", "minute", "minutes", "hour", "hours", "day", "days", "month", "months", "year", "years"))
			.HandleWith(cmdTimeAdd)
			.EndSub()
			.BeginSub("calendarspeedmul")
			.WithAlias("csm")
			.WithDesc("Determines the relationship between in-game time and real-world time. A value of 1 means one in-game minute is 1 real world second. A value of 0.5 means one in-game minute is 2 real world second")
			.WithArgs(parsers.OptionalFloat("value", 0.5f))
			.HandleWith(cmdCalendarSpeedMul)
			.EndSub()
			.BeginSub("hoursperday")
			.WithDesc("Determines how many hours a day has.")
			.WithArgs(parsers.OptionalFloat("value", 24f))
			.HandleWith(cmdHoursPerDay)
			.EndSub();
		cmdapi.GetOrCreate("debug").BeginSub("time").BeginSub("nexteclipse")
			.HandleWith(handleCmdNextEclipse)
			.EndSub()
			.EndSub();
	}

	private TextCommandResult handleCmdNextEclipse(TextCommandCallingArgs args)
	{
		Vec3d pos = args.Caller.Pos;
		double peakDotResult = -1.0;
		double lastEclipseTotalDays = -99.0;
		StringBuilder sb = new StringBuilder();
		double timeNow = server.GameWorldCalendar.TotalDays;
		for (double daysdelta = 0.0; daysdelta <= 300.0; daysdelta += 1.0 / 120.0)
		{
			double totalDays = timeNow + daysdelta;
			Vec3f vec3f = server.GameWorldCalendar.GetSunPosition(pos, totalDays).Normalize();
			Vec3f moonPos = server.GameWorldCalendar.GetMoonPosition(pos, totalDays);
			float hereDotResult = vec3f.Dot(moonPos);
			if ((double)hereDotResult < peakDotResult)
			{
				if (peakDotResult > 0.9997 && totalDays - lastEclipseTotalDays > 1.0)
				{
					double hourOfDay = (totalDays - 1.0 / 120.0) % 1.0 * (double)server.GameWorldCalendar.HoursPerDay;
					if (hourOfDay > 6.0 && hourOfDay < 17.0)
					{
						sb.AppendLine($"Eclipse will happen in {daysdelta - 1.0 / 12.0:0} days, will get within {Math.Acos(peakDotResult) * 57.2957763671875:0.#} degrees of the sun, at {(int)hourOfDay:00}:{(hourOfDay - (double)(int)hourOfDay) * 60.0:00}.");
						lastEclipseTotalDays = totalDays;
						peakDotResult = 0.0;
					}
				}
			}
			else
			{
				peakDotResult = hereDotResult;
			}
		}
		if (sb.Length > 0)
		{
			return TextCommandResult.Success(sb.ToString());
		}
		return TextCommandResult.Success("No eclipse found in 1000 days");
	}

	private TextCommandResult cmdGetTime(TextCommandCallingArgs args)
	{
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-gettime", server.GameWorldCalendar.PrettyDate(), Math.Round(server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f, 1)));
	}

	private TextCommandResult cmdHoursPerDay(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-hoursperday", server.GameWorldCalendar.HoursPerDay));
		}
		float hpd = (float)args[0];
		if ((double)hpd < 0.1)
		{
			return TextCommandResult.Error("Cannot be less than 0.1");
		}
		server.GameWorldCalendar.HoursPerDay = hpd;
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-hoursperdayset", hpd));
	}

	private TextCommandResult cmdCalendarSpeedMul(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-calendarspeedmul", server.GameWorldCalendar.CalendarSpeedMul));
		}
		server.GameWorldCalendar.CalendarSpeedMul = (float)args[0];
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-calendarspeedmulset", server.GameWorldCalendar.CalendarSpeedMul, Math.Round(server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f, 1)));
	}

	private TextCommandResult cmdTimeAdd(TextCommandCallingArgs args)
	{
		float amount = (float)args[0];
		string type = (string)args[1];
		if (amount < 0f)
		{
			return TextCommandResult.Error("Only positive values are allowed");
		}
		if (args.Parsers[1].IsMissing)
		{
			type = "hour";
		}
		if (type.Last().Equals('s'))
		{
			type = type.Substring(0, type.Length - 1);
		}
		switch (type)
		{
		case "minute":
			server.GameWorldCalendar.Add(amount / 60f);
			break;
		case "hour":
			server.GameWorldCalendar.Add(amount);
			break;
		case "day":
			server.GameWorldCalendar.Add(amount * server.GameWorldCalendar.HoursPerDay);
			break;
		case "month":
			server.GameWorldCalendar.Add(amount * server.GameWorldCalendar.HoursPerDay * (float)server.GameWorldCalendar.DaysPerMonth);
			break;
		case "year":
			server.GameWorldCalendar.Add(amount * server.GameWorldCalendar.HoursPerDay * (float)server.GameWorldCalendar.DaysPerYear);
			break;
		default:
			return TextCommandResult.Error("Invalid time span type");
		}
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeadd-" + type, amount, server.GameWorldCalendar.PrettyDate()));
	}

	private TextCommandResult cmdTimeSetMonth(TextCommandCallingArgs args)
	{
		int month = args.Parsers[0].GetValidRange(args.RawArgs).IndexOf((string)args[0]);
		server.GameWorldCalendar.SetMonth(month);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", server.GameWorldCalendar.PrettyDate()));
	}

	private TextCommandResult cmdTimeSet(TextCommandCallingArgs args)
	{
		float? hour = null;
		string strValue = (string)args[0];
		switch (strValue)
		{
		case "lunch":
			hour = 12f;
			break;
		case "day":
			hour = 12f;
			break;
		case "night":
			hour = 20f;
			break;
		case "latenight":
			hour = 22f;
			break;
		case "morning":
			hour = 8f;
			break;
		case "latemorning":
			hour = 10f;
			break;
		case "sunrise":
			hour = 6.5f;
			break;
		case "sunset":
			hour = 17.5f;
			break;
		case "afternoon":
			hour = 14f;
			break;
		case "midnight":
			hour = 0f;
			break;
		case "witchinghour":
			hour = 3f;
			break;
		}
		if (hour.HasValue)
		{
			server.GameWorldCalendar.SetDayTime(hour.Value / 24f * server.GameWorldCalendar.HoursPerDay);
			resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", server.GameWorldCalendar.PrettyDate()));
		}
		if (ParseTimeSpan(strValue, out var hours))
		{
			if (hours < 0f)
			{
				return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-time-negativeerror"));
			}
			server.GameWorldCalendar.SetDayTime(hours);
			resendTimePacket();
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-timeset", server.GameWorldCalendar.PrettyDate()));
		}
		return TextCommandResult.Error(Lang.GetL(args.LanguageCode, "command-time-invalidtimespan", strValue));
	}

	private TextCommandResult cmdTimeSpeed(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-speed", server.GameWorldCalendar.TimeSpeedModifiers["baseline"]));
		}
		float speed = (float)args[0];
		server.GameWorldCalendar.SetTimeSpeedModifier("baseline", speed);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, (speed == 0f) ? "command-time-speed0set" : "command-time-speedset", speed, Math.Round(server.GameWorldCalendar.DayLengthInRealLifeSeconds / 60f, 1)));
	}

	private TextCommandResult cmdResumeTime(TextCommandCallingArgs args)
	{
		server.GameWorldCalendar.SetTimeSpeedModifier("baseline", 60f);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-resumed"));
	}

	private TextCommandResult cmdStopTime(TextCommandCallingArgs args)
	{
		server.GameWorldCalendar.SetTimeSpeedModifier("baseline", 0f);
		resendTimePacket();
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, "command-time-stopped"));
	}

	private bool ParseTimeSpan(string timespan, out float hours)
	{
		int minutes = 0;
		int hoursi;
		bool valid;
		if (timespan.Contains(":"))
		{
			string[] parts = timespan.Split(':');
			valid = int.TryParse(parts[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out hoursi);
			valid &= int.TryParse(parts[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out minutes);
		}
		else
		{
			valid = int.TryParse(timespan, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out hoursi);
		}
		hours = (float)hoursi + (float)minutes / 60f;
		return valid;
	}

	private void resendTimePacket()
	{
		server.lastUpdateSentToClient = -1000 * MagicNum.CalendarPacketSecondInterval;
	}
}
