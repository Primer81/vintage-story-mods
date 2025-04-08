public abstract class CitoStream
{
	public abstract int Read(byte[] buffer, int read, int p);

	public abstract bool CanSeek();

	public abstract void Seek(int length, CitoSeekOrigin seekOrigin);

	public abstract void Write(byte[] val, int p, int p_3);

	public abstract void Seek_(int p, CitoSeekOrigin seekOrigin);

	public abstract int ReadByte();

	public abstract void WriteByte(byte p);

	public abstract void WriteSmallInt(int v);

	public abstract void WriteKey(byte k, byte wiretype);

	public abstract int Position();
}
