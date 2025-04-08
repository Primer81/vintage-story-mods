using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public interface IParticlePool
{
	MeshRef Model { get; }

	int QuantityAlive { get; }

	int SpawnParticles(IParticlePropertiesProvider properties);

	bool ShouldRender();

	void OnNewFrame(float dt, Vec3d playerPos);

	void OnNewFrameOffThread(float dt, Vec3d playerPos);

	void Dipose();
}
