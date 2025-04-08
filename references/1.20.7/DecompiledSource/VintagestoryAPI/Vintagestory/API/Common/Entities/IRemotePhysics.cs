namespace Vintagestory.API.Common.Entities;

public interface IRemotePhysics
{
	void HandleRemotePhysics(float dt, bool isTeleport);

	void OnReceivedClientPos(int version);
}
