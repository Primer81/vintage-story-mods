namespace Vintagestory.API.Common;

public class ProfileEntry
{
	public int ElapsedTicks;

	public int CallCount;

	public ProfileEntry()
	{
	}

	public ProfileEntry(int elaTicks, int callCount)
	{
		ElapsedTicks = elaTicks;
		CallCount = callCount;
	}
}
