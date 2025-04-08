using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class EyesOverlayRenderer : IRenderer, IDisposable
{
	internal MeshRef quadRef;

	private ICoreClientAPI capi;

	public IShaderProgram eyeShaderProg;

	public bool ShouldRender;

	public float Level;

	public float rndTarget;

	public float curRndVal;

	private LoadedTexture exitHelpTexture;

	public double RenderOrder => 0.95;

	public int RenderRange => 1;

	public EyesOverlayRenderer(ICoreClientAPI capi, IShaderProgram eyeShaderProg)
	{
		this.capi = capi;
		this.eyeShaderProg = eyeShaderProg;
		MeshData quadMesh = QuadMeshUtil.GetCustomQuadModelData(-1f, -1f, -19848f, 2f, 2f);
		quadMesh.Rgba = null;
		quadRef = capi.Render.UploadMesh(quadMesh);
		string hotkey = capi.Input.HotKeys["sneak"].CurrentMapping.ToString();
		exitHelpTexture = capi.Gui.TextTexture.GenTextTexture(Lang.Get("bed-exithint", hotkey), CairoFont.WhiteSmallishText());
	}

	public void Dispose()
	{
		capi.Render.DeleteMesh(quadRef);
		exitHelpTexture?.Dispose();
		eyeShaderProg.Dispose();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (!(Level <= 0f) && capi.World.Player.CameraMode == EnumCameraMode.FirstPerson)
		{
			if ((double)Level > 0.2 && capi.World.Rand.Next(60) == 0)
			{
				rndTarget = (float)capi.World.Rand.NextDouble() / 5f - 0.1f;
			}
			curRndVal += (rndTarget - curRndVal) * deltaTime;
			capi.Render.Render2DLoadedTexture(exitHelpTexture, capi.Render.FrameWidth / 2 - exitHelpTexture.Width / 2, (float)(capi.Render.FrameHeight * 3) / 4f);
			IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
			currentActiveShader.Stop();
			eyeShaderProg.Use();
			capi.Render.GlToggleBlend(blend: true);
			capi.Render.GLDepthMask(on: false);
			eyeShaderProg.Uniform("level", Level + curRndVal);
			capi.Render.RenderMesh(quadRef);
			eyeShaderProg.Stop();
			capi.Render.GLDepthMask(on: true);
			currentActiveShader.Use();
		}
	}
}
