namespace Vintagestory.Server;

public class StatsCollection
{
	public int statTotalPackets;

	public int statTotalUdpPackets;

	public int statTotalPacketsLength;

	public int statTotalUdpPacketsLength;

	public long tickTimeTotal;

	public long ticksTotal;

	public long[] tickTimes = new long[10];

	public int tickTimeIndex;
}
