public class Packet_ServerCalendar
{
	public long TotalSeconds;

	public string[] TimeSpeedModifierNames;

	public int TimeSpeedModifierNamesCount;

	public int TimeSpeedModifierNamesLength;

	public int[] TimeSpeedModifierSpeeds;

	public int TimeSpeedModifierSpeedsCount;

	public int TimeSpeedModifierSpeedsLength;

	public int MoonOrbitDays;

	public int HoursPerDay;

	public int Running;

	public int CalendarSpeedMul;

	public int DaysPerMonth;

	public long TotalSecondsStart;

	public const int TotalSecondsFieldID = 1;

	public const int TimeSpeedModifierNamesFieldID = 2;

	public const int TimeSpeedModifierSpeedsFieldID = 3;

	public const int MoonOrbitDaysFieldID = 4;

	public const int HoursPerDayFieldID = 5;

	public const int RunningFieldID = 6;

	public const int CalendarSpeedMulFieldID = 7;

	public const int DaysPerMonthFieldID = 8;

	public const int TotalSecondsStartFieldID = 9;

	public void SetTotalSeconds(long value)
	{
		TotalSeconds = value;
	}

	public string[] GetTimeSpeedModifierNames()
	{
		return TimeSpeedModifierNames;
	}

	public void SetTimeSpeedModifierNames(string[] value, int count, int length)
	{
		TimeSpeedModifierNames = value;
		TimeSpeedModifierNamesCount = count;
		TimeSpeedModifierNamesLength = length;
	}

	public void SetTimeSpeedModifierNames(string[] value)
	{
		TimeSpeedModifierNames = value;
		TimeSpeedModifierNamesCount = value.Length;
		TimeSpeedModifierNamesLength = value.Length;
	}

	public int GetTimeSpeedModifierNamesCount()
	{
		return TimeSpeedModifierNamesCount;
	}

	public void TimeSpeedModifierNamesAdd(string value)
	{
		if (TimeSpeedModifierNamesCount >= TimeSpeedModifierNamesLength)
		{
			if ((TimeSpeedModifierNamesLength *= 2) == 0)
			{
				TimeSpeedModifierNamesLength = 1;
			}
			string[] newArray = new string[TimeSpeedModifierNamesLength];
			for (int i = 0; i < TimeSpeedModifierNamesCount; i++)
			{
				newArray[i] = TimeSpeedModifierNames[i];
			}
			TimeSpeedModifierNames = newArray;
		}
		TimeSpeedModifierNames[TimeSpeedModifierNamesCount++] = value;
	}

	public int[] GetTimeSpeedModifierSpeeds()
	{
		return TimeSpeedModifierSpeeds;
	}

	public void SetTimeSpeedModifierSpeeds(int[] value, int count, int length)
	{
		TimeSpeedModifierSpeeds = value;
		TimeSpeedModifierSpeedsCount = count;
		TimeSpeedModifierSpeedsLength = length;
	}

	public void SetTimeSpeedModifierSpeeds(int[] value)
	{
		TimeSpeedModifierSpeeds = value;
		TimeSpeedModifierSpeedsCount = value.Length;
		TimeSpeedModifierSpeedsLength = value.Length;
	}

	public int GetTimeSpeedModifierSpeedsCount()
	{
		return TimeSpeedModifierSpeedsCount;
	}

	public void TimeSpeedModifierSpeedsAdd(int value)
	{
		if (TimeSpeedModifierSpeedsCount >= TimeSpeedModifierSpeedsLength)
		{
			if ((TimeSpeedModifierSpeedsLength *= 2) == 0)
			{
				TimeSpeedModifierSpeedsLength = 1;
			}
			int[] newArray = new int[TimeSpeedModifierSpeedsLength];
			for (int i = 0; i < TimeSpeedModifierSpeedsCount; i++)
			{
				newArray[i] = TimeSpeedModifierSpeeds[i];
			}
			TimeSpeedModifierSpeeds = newArray;
		}
		TimeSpeedModifierSpeeds[TimeSpeedModifierSpeedsCount++] = value;
	}

	public void SetMoonOrbitDays(int value)
	{
		MoonOrbitDays = value;
	}

	public void SetHoursPerDay(int value)
	{
		HoursPerDay = value;
	}

	public void SetRunning(int value)
	{
		Running = value;
	}

	public void SetCalendarSpeedMul(int value)
	{
		CalendarSpeedMul = value;
	}

	public void SetDaysPerMonth(int value)
	{
		DaysPerMonth = value;
	}

	public void SetTotalSecondsStart(long value)
	{
		TotalSecondsStart = value;
	}

	internal void InitializeValues()
	{
	}
}
