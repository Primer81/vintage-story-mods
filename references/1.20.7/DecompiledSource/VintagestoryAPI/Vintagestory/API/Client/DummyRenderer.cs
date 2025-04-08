using System;

namespace Vintagestory.API.Client;

public class DummyRenderer : IRenderer, IDisposable
{
	public Action<float> action;

	public double RenderOrder { get; set; }

	public int RenderRange { get; set; }

	public void Dispose()
	{
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		action(deltaTime);
	}
}
