using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IAsyncParticleManager
{
	IBlockAccessor BlockAccess { get; }

	int Spawn(IParticlePropertiesProvider particleProperties);

	int ParticlesAlive(EnumParticleModel model);
}
