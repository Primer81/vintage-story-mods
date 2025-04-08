using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class ProfileEntryRange
{
	public string Code;

	public long Start;

	public long LastMark;

	public int CallCount = 1;

	public long ElapsedTicks;

	public Dictionary<string, ProfileEntry> Marks;

	public Dictionary<string, ProfileEntryRange> ChildRanges;

	public ProfileEntryRange ParentRange;
}
