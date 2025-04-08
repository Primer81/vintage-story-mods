namespace Vintagestory.Client.NoObf;

public interface INetworkConnection
{
	bool Connected { get; }

	bool Disconnected { get; }

	void Disconnect();

	void Receive(byte[] data, int dataLength, out int total);

	void Send(byte[] data, int length);
}
