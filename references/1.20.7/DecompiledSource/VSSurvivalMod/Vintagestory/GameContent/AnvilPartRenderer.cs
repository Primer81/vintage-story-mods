using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AnvilPartRenderer : IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private BlockEntityAnvilPart beAnvil;

	public Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 25;

	public AnvilPartRenderer(ICoreClientAPI capi, BlockEntityAnvilPart beAnvil)
	{
		this.capi = capi;
		this.beAnvil = beAnvil;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (!beAnvil.Inventory[0].Empty)
		{
			IRenderAPI rpi = capi.Render;
			Vec3d camPos = capi.World.Player.Entity.CameraPos;
			ItemStack stack = beAnvil.Inventory[0].Itemstack;
			int num = (int)stack.Collectible.GetTemperature(capi.World, stack);
			Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(beAnvil.Pos.X, beAnvil.Pos.Y, beAnvil.Pos.Z);
			float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
			int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
			rpi.GlDisableCullFace();
			rpi.GlToggleBlend(blend: true);
			IStandardShaderProgram prog = rpi.PreparedStandardShader(beAnvil.Pos.X, beAnvil.Pos.Y, beAnvil.Pos.Z);
			prog.ModelMatrix = ModelMat.Identity().Translate((double)beAnvil.Pos.X - camPos.X, (double)beAnvil.Pos.Y - camPos.Y, (double)beAnvil.Pos.Z - camPos.Z).Values;
			prog.RgbaLightIn = lightrgbs;
			prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)extraGlow / 255f);
			prog.ExtraGlow = extraGlow;
			prog.ViewMatrix = rpi.CameraMatrixOriginf;
			prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			prog.AverageColor = ColorUtil.ToRGBAVec4f(capi.BlockTextureAtlas.GetAverageColor((stack.Item?.FirstTexture ?? stack.Block.FirstTextureInventory).Baked.TextureSubId));
			prog.TempGlowMode = stack.ItemAttributes?["tempGlowMode"].AsInt() ?? 0;
			if (beAnvil.BaseMeshRef != null && !beAnvil.BaseMeshRef.Disposed)
			{
				rpi.RenderMultiTextureMesh(beAnvil.BaseMeshRef, "tex");
			}
			if (beAnvil.FluxMeshRef != null && !beAnvil.FluxMeshRef.Disposed)
			{
				prog.ExtraGlow = 0;
				rpi.RenderMultiTextureMesh(beAnvil.FluxMeshRef, "tex");
			}
			if (beAnvil.TopMeshRef != null && !beAnvil.TopMeshRef.Disposed)
			{
				int num2 = (int)beAnvil.Inventory[2].Itemstack.Collectible.GetTemperature(capi.World, beAnvil.Inventory[2].Itemstack);
				lightrgbs = capi.World.BlockAccessor.GetLightRGBs(beAnvil.Pos.X, beAnvil.Pos.Y, beAnvil.Pos.Z);
				glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num2);
				extraGlow = GameMath.Clamp((num2 - 550) / 2, 0, 255);
				prog.ModelMatrix = ModelMat.Identity().Translate((double)beAnvil.Pos.X - camPos.X, (double)beAnvil.Pos.Y - camPos.Y - (double)((float)beAnvil.hammerHits / 250f), (double)beAnvil.Pos.Z - camPos.Z).Values;
				prog.RgbaLightIn = lightrgbs;
				prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)extraGlow / 255f);
				prog.ExtraGlow = extraGlow;
				rpi.RenderMultiTextureMesh(beAnvil.TopMeshRef, "tex");
			}
			prog.Stop();
		}
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
	}
}
