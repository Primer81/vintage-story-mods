namespace Vintagestory.GameContent;

public class DayTimeFrame
{
	public double FromHour;

	public double ToHour;

	public bool Matches(double hourOfDay)
	{
		if (FromHour <= hourOfDay)
		{
			return ToHour >= hourOfDay;
		}
		return false;
	}
}
