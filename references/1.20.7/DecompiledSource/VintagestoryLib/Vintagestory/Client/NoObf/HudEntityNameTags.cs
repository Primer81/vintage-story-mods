using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.Client.NoObf;

public class HudEntityNameTags : HudElement
{
	private ClientMain game;

	public override double DrawOrder => -0.1;

	public HudEntityNameTags(ICoreClientAPI capi)
		: base(capi)
	{
		TryOpen();
		game = (ClientMain)capi.World;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		int plrDim = game.EntityPlayer.Pos.Dimension;
		foreach (Entity entity in game.LoadedEntities.Values)
		{
			if (game.frustumCuller.SphereInFrustum((float)entity.Pos.X, (float)(entity.Pos.Y + entity.LocalEyePos.Y), (float)entity.Pos.Z, 0.5) && entity.Pos.Dimension == plrDim)
			{
				EntityRenderer renderer = null;
				game.EntityRenderers.TryGetValue(entity, out renderer);
				renderer?.DoRender2D(deltaTime);
			}
		}
	}

	public override bool TryClose()
	{
		return false;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return false;
	}

	public override bool ShouldReceiveRenderEvents()
	{
		return true;
	}
}
