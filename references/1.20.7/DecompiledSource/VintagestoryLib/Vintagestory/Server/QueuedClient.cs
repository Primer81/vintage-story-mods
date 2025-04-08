namespace Vintagestory.Server;

public class QueuedClient
{
	public ConnectedClient Client;

	public Packet_ClientIdentification Identification;

	public string Entitlements;

	public QueuedClient(ConnectedClient client, Packet_ClientIdentification identification, string entitlements)
	{
		Client = client;
		Identification = identification;
		Entitlements = entitlements;
	}
}
