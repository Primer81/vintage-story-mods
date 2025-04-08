namespace Vintagestory.ServerMods;

internal class MapLayerExactZoom : MapLayerBase
{
	private MapLayerBase parent;

	private int zoomLevel;

	public MapLayerExactZoom(MapLayerBase parent, int zoomLevel)
		: base(0L)
	{
		this.parent = parent;
		this.zoomLevel = zoomLevel;
	}

	public override int[] GenLayer(int xCoord, int yCoord, int sizeX, int sizeY)
	{
		sizeX += zoomLevel;
		sizeY += zoomLevel;
		int[] outCache = new int[sizeX * sizeY];
		int parentXCoord = xCoord / zoomLevel - 1;
		int parentZCoord = yCoord / zoomLevel - 1;
		int smallXSize = sizeX / zoomLevel;
		int smallZSize = sizeY / zoomLevel;
		int[] inInts = parent.GenLayer(parentXCoord, parentZCoord, smallXSize, smallZSize);
		for (int i = 0; i < inInts.Length; i++)
		{
			int xpos = i % smallXSize;
			int zpos = i / smallXSize;
			int inValue = inInts[i];
			int index = zoomLevel * xpos + zoomLevel * zpos * sizeX;
			for (int j = 0; j < zoomLevel * zoomLevel; j++)
			{
				outCache[index + sizeX * (j / zoomLevel) + j % zoomLevel] = inValue;
			}
		}
		return CutRightAndBottom(outCache, sizeX, sizeY, zoomLevel);
	}
}
