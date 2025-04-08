namespace Vintagestory.GameContent;

public class CloudRendererBase
{
	public int CloudTileLength = 5;

	public double windOffsetX;

	public double windOffsetZ;

	public int CloudTileSize { get; set; } = 50;


	public virtual void UpdateCloudTiles(int changeSpeed = 1)
	{
	}
}
