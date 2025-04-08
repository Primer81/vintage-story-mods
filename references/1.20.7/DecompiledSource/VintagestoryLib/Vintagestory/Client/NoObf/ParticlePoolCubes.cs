using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ParticlePoolCubes : ParticlePoolQuads
{
	internal override float ParticleHeight => 0.0625f;

	public ParticlePoolCubes(int capacity, ClientMain game, bool offthread)
		: base(capacity, game, offthread)
	{
		ModelType = EnumParticleModel.Cube;
	}

	public override MeshData LoadModel()
	{
		MeshData modeldata = CubeMeshUtil.GetCubeOnlyScaleXyz(1f / 32f, 1f / 32f, new Vec3f());
		modeldata.WithNormals();
		modeldata.Rgba = null;
		for (int i = 0; i < 24; i++)
		{
			BlockFacing face = BlockFacing.ALLFACES[i / 4];
			modeldata.AddNormal(face);
		}
		return modeldata;
	}

	internal override void UpdateDebugScreen()
	{
		if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["cubeparticlepool"] = "Cube Particle pool: " + ParticlesPool.AliveCount + " / " + (int)((float)poolSize * (float)game.particleLevel / 100f);
		}
		else
		{
			game.DebugScreenInfo["cubeparticlepool"] = "";
		}
	}
}
