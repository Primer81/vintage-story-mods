using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderAim : ClientSystem
{
	private int aimTextureId;

	private int aimHostileTextureId;

	public override string Name => "remi";

	public SystemRenderAim(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnRenderFrame2DOverlay, EnumRenderStage.Ortho, Name, 1.02);
	}

	public override void OnBlockTexturesLoaded()
	{
		aimTextureId = game.GetOrLoadCachedTexture(new AssetLocation("gui/target.png"));
		aimHostileTextureId = game.GetOrLoadCachedTexture(new AssetLocation("gui/targethostile.png"));
	}

	public void OnRenderFrame2DOverlay(float deltaTime)
	{
		if (game.MouseGrabbed)
		{
			DrawAim(game);
		}
	}

	internal void DrawAim(ClientMain game)
	{
		if (game.MainCamera.CameraMode != 0 || game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			return;
		}
		int aimwidth = 32;
		int aimheight = 32;
		Entity entity = game.EntitySelection?.Entity;
		ItemStack heldStack = game.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
		float attackRange = heldStack?.Collectible.GetAttackRange(heldStack) ?? GlobalConstants.DefaultAttackRange;
		int texId = aimTextureId;
		if (entity != null && game.EntityPlayer != null)
		{
			Cuboidd cuboidd = entity.SelectionBox.ToDouble().Translate(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
			EntityPos pos = game.EntityPlayer.SidedPos;
			if (cuboidd.ShortestDistanceFrom(pos.X + game.EntityPlayer.LocalEyePos.X, pos.Y + game.EntityPlayer.LocalEyePos.Y, pos.Z + game.EntityPlayer.LocalEyePos.Z) <= (double)attackRange - 0.08)
			{
				texId = aimHostileTextureId;
			}
		}
		game.Render2DTexture(texId, game.Width / 2 - aimwidth / 2, game.Height / 2 - aimheight / 2, aimwidth, aimheight, 10000f);
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.GLDeleteTexture(aimTextureId);
		game.Platform.GLDeleteTexture(aimHostileTextureId);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
