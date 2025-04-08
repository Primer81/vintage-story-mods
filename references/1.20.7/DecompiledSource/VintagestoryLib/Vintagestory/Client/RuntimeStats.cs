namespace Vintagestory.Client;

public class RuntimeStats
{
	public static int chunksReceived = 0;

	public static int chunksTesselatedPerSecond = 0;

	public static int chunksTesselatedEdgeOnly = 0;

	public static int chunksAwaitingTesselation = 0;

	public static int chunksAwaitingPooling = 0;

	public static int chunksTesselatedTotal = 0;

	public static int chunksUnloaded = 0;

	public static int renderedTriangles = 0;

	public static int availableTriangles = 0;

	public static int renderedEntities = 0;

	public static int drawCallsCount = 0;

	internal static long tesselationStart;

	internal static int TCTpacked;

	internal static int TCTunpacked = 1;

	internal static int OCpacked;

	internal static int OCunpacked;

	public static void Reset()
	{
		chunksTesselatedTotal = 0;
		chunksReceived = 0;
		chunksTesselatedPerSecond = 0;
		chunksTesselatedEdgeOnly = 0;
		chunksUnloaded = 0;
		renderedTriangles = 0;
		availableTriangles = 0;
		drawCallsCount = 0;
		renderedEntities = 0;
		TCTpacked = 0;
		TCTunpacked = 1;
		OCpacked = 0;
		OCunpacked = 1;
	}
}
