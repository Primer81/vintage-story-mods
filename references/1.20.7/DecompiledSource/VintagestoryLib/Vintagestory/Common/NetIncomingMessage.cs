namespace Vintagestory.Common;

public class NetIncomingMessage
{
	public NetConnection SenderConnection;

	public NetworkMessageType Type;

	public byte[] message;

	public int messageLength;

	public int originalMessageLength;

	public static int ReadInt(byte[] readBuf)
	{
		return (readBuf[0] << 24) + (readBuf[1] << 16) + (readBuf[2] << 8) + readBuf[3];
	}

	public static void WriteInt(byte[] writeBuf, int n)
	{
		int a = (n >> 24) & 0xFF;
		int b = (n >> 16) & 0xFF;
		int c = (n >> 8) & 0xFF;
		int d = n & 0xFF;
		writeBuf[0] = (byte)a;
		writeBuf[1] = (byte)b;
		writeBuf[2] = (byte)c;
		writeBuf[3] = (byte)d;
	}
}
