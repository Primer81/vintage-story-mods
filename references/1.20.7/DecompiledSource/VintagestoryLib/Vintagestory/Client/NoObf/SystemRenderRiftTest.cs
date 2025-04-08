using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class SystemRenderRiftTest : ClientSystem, IRenderer, IDisposable
{
	public override string Name => "rendertest";

	public double RenderOrder => 0.05;

	public int RenderRange => 100;

	public SystemRenderRiftTest(ClientMain game)
		: base(game)
	{
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
	}

	public void Dispose()
	{
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
