namespace Vintagestory.Client.NoObf;

public class ProcessPacketTask
{
	internal ClientMain game;

	internal Packet_Server packet;

	public void Run()
	{
		ProcessPacket(packet);
	}

	internal void ProcessPacket(Packet_Server packet)
	{
		if (!game.disposed)
		{
			game.PacketHandlers[packet.Id]?.Invoke(packet);
		}
	}
}
