namespace Vintagestory.Client.NoObf;

internal class ExtendedClientChunk
{
	private ClientChunk[,,] chunks = new ClientChunk[3, 3, 3];

	public ushort this[int x, int y, int z] => 0;
}
