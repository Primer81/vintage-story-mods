using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class ModSystemItemRendererOptimizer : ModSystem, IRenderer, IDisposable
{
	private int itemCount;

	private ICoreClientAPI capi;

	public double RenderOrder => 1.0;

	public int RenderRange => 1;

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		EntityItemRenderer.RenderCount = 0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterRenderer(this, EnumRenderStage.Before);
		api.Event.RegisterGameTickListener(onTick, 1001);
	}

	private void onTick(float dt)
	{
		itemCount = 0;
		foreach (KeyValuePair<long, Entity> loadedEntity in capi.World.LoadedEntities)
		{
			if (loadedEntity.Value is EntityItem)
			{
				itemCount++;
			}
		}
		EntityItemRenderer.RunWittySkipRenderAlgorithm = itemCount > 400;
		EntityItemRenderer.RenderModulo = itemCount / 200;
		EntityItemRenderer.LastPos.Set(-99, -99, -99);
	}
}
