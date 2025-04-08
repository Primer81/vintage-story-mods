namespace Vintagestory.API.Client;

/// <summary>
/// Return false to stop spawning particles
/// </summary>
/// <param name="dt"></param>
/// <param name="manager"></param>
/// <returns></returns>
public delegate bool ContinousParticleSpawnTaskDelegate(float dt, IAsyncParticleManager manager);
