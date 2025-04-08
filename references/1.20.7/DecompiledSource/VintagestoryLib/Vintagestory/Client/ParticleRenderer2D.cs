using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class ParticleRenderer2D
{
	private int particleTex;

	public ParticlePool2D Pool;

	public float[] mvMatrix = Mat4f.Create();

	public float[] pMatrix;

	public int oitPass;

	public int heldItemMode;

	private ScreenManager screenManager;

	public ParticleRenderer2D(ScreenManager screenManager, ICoreClientAPI api, int poolSize = 1000)
	{
		this.screenManager = screenManager;
		Pool = new ParticlePool2D(api, poolSize);
	}

	public void Compose(string texture)
	{
		if (texture != null)
		{
			BitmapRef bmp = screenManager.GamePlatform.AssetManager.Get(texture).ToBitmap(screenManager.api);
			particleTex = ScreenManager.Platform.LoadTexture(bmp);
			bmp.Dispose();
		}
	}

	public void Spawn(IParticlePropertiesProvider prop)
	{
		Pool.Spawn(prop);
	}

	public void Render(float dt)
	{
		if (oitPass == 0)
		{
			screenManager.GamePlatform.GlToggleBlend(on: true);
		}
		else
		{
			screenManager.GamePlatform.GlDepthMask(flag: true);
		}
		Pool.OnNewFrame(dt);
		ShaderProgramParticlesquad2d particlesquad2d = ShaderPrograms.Particlesquad2d;
		particlesquad2d.Use();
		particlesquad2d.ParticleTex2D = particleTex;
		particlesquad2d.WithTexture = ((particleTex > 0) ? 1 : 0);
		particlesquad2d.OitPass = oitPass;
		particlesquad2d.HeldItemMode = heldItemMode;
		particlesquad2d.ProjectionMatrix = pMatrix;
		particlesquad2d.ModelViewMatrix = mvMatrix;
		ScreenManager.Platform.RenderMeshInstanced(Pool.Model, Pool.QuantityAlive);
		particlesquad2d.Stop();
		screenManager.GamePlatform.GlDepthMask(flag: false);
	}

	public void Dispose()
	{
		if (particleTex > 0)
		{
			screenManager.GamePlatform.GLDeleteTexture(particleTex);
		}
		Pool.Dispose();
	}
}
