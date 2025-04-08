using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent.Mechanics;

public class MechNetworkRenderer : IRenderer, IDisposable
{
	private MechanicalPowerMod mechanicalPowerMod;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	private List<MechBlockRenderer> MechBlockRenderer = new List<MechBlockRenderer>();

	private Dictionary<int, int> MechBlockRendererByShape = new Dictionary<int, int>();

	public static Dictionary<string, Type> RendererByCode = new Dictionary<string, Type>
	{
		{
			"generic",
			typeof(GenericMechBlockRenderer)
		},
		{
			"angledgears",
			typeof(AngledGearsBlockRenderer)
		},
		{
			"angledgearcage",
			typeof(AngledCageGearRenderer)
		},
		{
			"transmission",
			typeof(TransmissionBlockRenderer)
		},
		{
			"clutch",
			typeof(ClutchBlockRenderer)
		},
		{
			"pulverizer",
			typeof(PulverizerRenderer)
		},
		{
			"autorotor",
			typeof(CreativeRotorRenderer)
		}
	};

	public double RenderOrder => 0.5;

	public int RenderRange => 100;

	public MechNetworkRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod)
	{
		this.mechanicalPowerMod = mechanicalPowerMod;
		this.capi = capi;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "mechnetwork");
		capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowFar, "mechnetwork");
		capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowNear, "mechnetwork");
		prog = capi.Shader.GetProgramByName("instanced");
	}

	public void AddDevice(IMechanicalPowerRenderable device)
	{
		if (device.Shape != null)
		{
			int index = -1;
			string rendererCode = "generic";
			JsonObject attributes = device.Block.Attributes;
			if (attributes != null && (attributes["mechanicalPower"]?["renderer"].Exists).GetValueOrDefault())
			{
				rendererCode = device.Block.Attributes?["mechanicalPower"]?["renderer"].AsString("generic");
			}
			int hashCode = device.Shape.GetHashCode() + rendererCode.GetHashCode();
			if (!MechBlockRendererByShape.TryGetValue(hashCode, out index))
			{
				object obj = Activator.CreateInstance(RendererByCode[rendererCode], capi, mechanicalPowerMod, device.Block, device.Shape);
				MechBlockRenderer.Add((MechBlockRenderer)obj);
				index = (MechBlockRendererByShape[hashCode] = MechBlockRenderer.Count - 1);
			}
			MechBlockRenderer[index].AddDevice(device);
		}
	}

	public void RemoveDevice(IMechanicalPowerRenderable device)
	{
		if (device.Shape == null)
		{
			return;
		}
		using List<MechBlockRenderer>.Enumerator enumerator = MechBlockRenderer.GetEnumerator();
		while (enumerator.MoveNext() && !enumerator.Current.RemoveDevice(device))
		{
		}
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
			prog.BindTexture2D("tex", capi.BlockTextureAtlas.Positions[0].atlasTextureId, 0);
			prog.Uniform("rgbaFogIn", capi.Render.FogColor);
			prog.Uniform("rgbaAmbientIn", capi.Render.AmbientColor);
			prog.Uniform("fogMinIn", capi.Render.FogMin);
			prog.Uniform("fogDensityIn", capi.Render.FogDensity);
			prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			prog.UniformMatrix("modelViewMatrix", capi.Render.CameraMatrixOriginf);
			for (int i = 0; i < MechBlockRenderer.Count; i++)
			{
				MechBlockRenderer[i].OnRenderFrame(deltaTime, prog);
			}
			prog.Stop();
		}
		capi.Render.GlEnableCullFace();
	}

	public void Dispose()
	{
		for (int i = 0; i < MechBlockRenderer.Count; i++)
		{
			MechBlockRenderer[i].Dispose();
		}
	}
}
