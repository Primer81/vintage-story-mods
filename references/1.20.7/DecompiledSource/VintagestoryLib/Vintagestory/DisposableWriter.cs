using System.IO;

namespace Vintagestory;

public class DisposableWriter
{
	private FileStream stream;

	public StreamWriter writer;

	public DisposableWriter(string filename, bool clearOldFiles)
	{
		writer = new StreamWriter(stream = new FileStream(filename, clearOldFiles ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read));
	}

	public void Dispose()
	{
		writer.Dispose();
		stream.Dispose();
	}
}
