namespace Vintagestory.Client.NoObf;

public abstract class AudioData
{
	public int Loaded;

	public abstract bool Load();

	public abstract int Load_Async(MainThreadAction onCompleted);
}
