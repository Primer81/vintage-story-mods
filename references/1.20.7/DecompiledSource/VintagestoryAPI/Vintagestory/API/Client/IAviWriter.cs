namespace Vintagestory.API.Client;

public interface IAviWriter
{
	void Open(string filename, int width, int height);

	void AddFrame();

	void Close();
}
