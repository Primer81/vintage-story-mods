using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FruitRendererSystem : IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private Dictionary<AssetLocation, FruitRenderer> renderers = new Dictionary<AssetLocation, FruitRenderer>();

	public double RenderOrder => 0.5;

	public int RenderRange => 80;

	public FruitRendererSystem(ICoreClientAPI capi)
	{
		this.capi = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "fruit");
		prog = capi.Shader.GetProgramByName("instanced");
	}

	public void AddFruit(Item fruit, Vec3d position, FruitData data)
	{
		if (fruit.Shape != null)
		{
			if (!renderers.TryGetValue(fruit.Code, out var renderer))
			{
				renderer = new FruitRenderer(capi, fruit);
				renderers.Add(fruit.Code, renderer);
			}
			renderer.AddFruit(position, data);
		}
	}

	public void RemoveFruit(Item fruit, Vec3d position)
	{
		renderers.TryGetValue(fruit.Code, out var renderer);
		renderer?.RemoveFruit(position);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (prog.Disposed)
		{
			prog = capi.Shader.GetProgramByName("instanced");
		}
		capi.Render.GlDisableCullFace();
		if (stage == EnumRenderStage.Opaque)
		{
			capi.Render.GlToggleBlend(blend: false);
			prog.Use();
			prog.BindTexture2D("tex", capi.ItemTextureAtlas.Positions[0].atlasTextureId, 0);
			prog.Uniform("rgbaFogIn", capi.Render.FogColor);
			prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
			prog.Uniform("fogMinIn", capi.Render.FogMin);
			prog.Uniform("fogDensityIn", capi.Render.FogDensity);
			prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			prog.UniformMatrix("modelViewMatrix", capi.Render.CameraMatrixOriginf);
			foreach (KeyValuePair<AssetLocation, FruitRenderer> renderer in renderers)
			{
				renderer.Value.OnRenderFrame(deltaTime, prog);
			}
			prog.Stop();
		}
		capi.Render.GlEnableCullFace();
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		foreach (KeyValuePair<AssetLocation, FruitRenderer> renderer in renderers)
		{
			renderer.Value.Dispose();
		}
	}
}
