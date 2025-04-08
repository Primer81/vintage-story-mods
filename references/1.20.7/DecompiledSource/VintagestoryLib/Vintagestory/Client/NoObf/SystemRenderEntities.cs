using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderEntities : ClientSystem
{
	public override string Name => "ree";

	public SystemRenderEntities(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnBeforeRender, EnumRenderStage.Before, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderOpaque3D, EnumRenderStage.Opaque, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderOIT, EnumRenderStage.OIT, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderAfterOIT, EnumRenderStage.AfterOIT, Name, 0.7);
		game.eventManager.RegisterRenderer(OnRenderFrame2D, EnumRenderStage.Ortho, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderFrameShadows, EnumRenderStage.ShadowFar, Name, 0.4);
		game.eventManager.RegisterRenderer(OnRenderFrameShadows, EnumRenderStage.ShadowNear, Name, 0.4);
	}

	private void OnBeforeRender(float dt)
	{
		int viewDistSq = ClientSettings.ViewDistance * ClientSettings.ViewDistance;
		Vec3d plrPos = game.EntityPlayer.Pos.XYZ;
		int plrDim = game.EntityPlayer.Pos.Dimension;
		foreach (KeyValuePair<Entity, EntityRenderer> val in game.EntityRenderers)
		{
			Entity entity = val.Key;
			if (game.frustumCuller.SphereInFrustum((float)entity.Pos.X, (float)entity.Pos.InternalY, (float)entity.Pos.Z, entity.FrustumSphereRadius) && entity.Pos.Dimension == plrDim && (entity.AllowOutsideLoadedRange || (plrPos.HorizontalSquareDistanceTo(entity.Pos.X, entity.Pos.Z) < (float)viewDistSq && (entity == game.EntityPlayer || game.WorldMap.IsChunkRendered((int)entity.Pos.X / 32, (int)entity.Pos.InternalY / 32, (int)entity.Pos.Z / 32)))))
			{
				entity.IsRendered = true;
				val.Value.BeforeRender(dt);
			}
			else
			{
				entity.IsRendered = false;
			}
			game.api.World.FrameProfiler.Mark("esr-beforeanim");
			try
			{
				entity.AnimManager?.OnClientFrame(dt);
			}
			catch (Exception)
			{
				game.Logger.Error("Animations error for entity " + entity.Code.ToShortString() + " at " + entity.ServerPos.AsBlockPos);
				throw;
			}
			game.api.World.FrameProfiler.Mark("esr-afteranim");
		}
	}

	public void OnRenderOpaque3D(float deltaTime)
	{
		RuntimeStats.renderedEntities = 0;
		game.GlMatrixModeModelView();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.Platform.GlEnableDepthTest();
		foreach (KeyValuePair<Entity, EntityRenderer> val2 in game.EntityRenderers)
		{
			if (val2.Key.IsRendered)
			{
				val2.Value.DoRender3DOpaque(deltaTime, isShadowPass: false);
				RuntimeStats.renderedEntities++;
			}
		}
		ScreenManager.FrameProfiler.Mark("ree-op");
		ShaderProgramEntityanimated prog = ShaderPrograms.Entityanimated;
		prog.Use();
		prog.RgbaAmbientIn = game.api.renderapi.AmbientColor;
		prog.RgbaFogIn = game.api.renderapi.FogColor;
		prog.FogMinIn = game.api.renderapi.FogMin;
		prog.FogDensityIn = game.api.renderapi.FogDensity;
		prog.ProjectionMatrix = game.CurrentProjectionMatrix;
		prog.EntityTex2D = game.EntityAtlasManager.AtlasTextures[0].TextureId;
		prog.AlphaTest = 0.05f;
		prog.LightPosition = game.shUniforms.LightPosition3D;
		game.Platform.GlDisableCullFace();
		game.GlMatrixModeModelView();
		game.GlPushMatrix();
		game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
		game.Platform.GlToggleBlend(on: true);
		foreach (KeyValuePair<Entity, EntityRenderer> val in game.EntityRenderers)
		{
			if (val.Key.IsRendered)
			{
				val.Value.DoRender3DOpaqueBatched(deltaTime, isShadowPass: false);
			}
		}
		game.GlPopMatrix();
		prog.Stop();
		ScreenManager.FrameProfiler.Mark("ree-op-b");
		game.Platform.GlToggleBlend(on: false);
	}

	private void OnRenderOIT(float dt)
	{
	}

	private void OnRenderAfterOIT(float dt)
	{
		game.GlMatrixModeModelView();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.Platform.GlEnableDepthTest();
		foreach (KeyValuePair<Entity, EntityRenderer> val in game.EntityRenderers)
		{
			if (val.Key.IsRendered)
			{
				val.Value.DoRender3DAfterOIT(dt, isShadowPass: false);
			}
		}
	}

	private void OnRenderFrameShadows(float dt)
	{
		int plrDim = game.EntityPlayer.Pos.Dimension;
		foreach (KeyValuePair<Entity, EntityRenderer> val2 in game.EntityRenderers)
		{
			Entity entity = val2.Key;
			if (game.frustumCuller.SphereInFrustum((float)entity.Pos.X, (float)entity.Pos.InternalY, (float)entity.Pos.Z, 3.0) && (entity == game.EntityPlayer || (game.WorldMap.IsValidPos((int)entity.Pos.X, (int)entity.Pos.InternalY, (int)entity.Pos.Z) && game.WorldMap.IsChunkRendered((int)entity.Pos.X / 32, (int)entity.Pos.InternalY / 32, (int)entity.Pos.Z / 32))) && entity.Pos.Dimension == plrDim)
			{
				entity.IsShadowRendered = true;
				val2.Value.DoRender3DOpaque(dt, isShadowPass: true);
			}
			else
			{
				entity.IsShadowRendered = false;
			}
		}
		ShaderProgramShadowmapgeneric prog = (ShaderProgramShadowmapgeneric)ShaderProgramBase.CurrentShaderProgram;
		prog.Stop();
		ShaderProgramShadowmapentityanimated proge = ShaderPrograms.Shadowmapentityanimated;
		proge.Use();
		proge.ProjectionMatrix = game.CurrentProjectionMatrix;
		proge.EntityTex2D = game.EntityAtlasManager.AtlasTextures[0].TextureId;
		_ = game.api.Render;
		_ = game.api.World.Player.Entity;
		foreach (KeyValuePair<Entity, EntityRenderer> val in game.EntityRenderers)
		{
			if (val.Key.IsShadowRendered)
			{
				val.Value.DoRender3DOpaqueBatched(dt, isShadowPass: true);
			}
		}
		proge.Stop();
		prog.Use();
		prog.MvpMatrix = game.shadowMvpMatrix;
	}

	private void OnRenderFrame2D(float dt)
	{
		foreach (KeyValuePair<Entity, EntityRenderer> val in game.EntityRenderers)
		{
			Entity key = val.Key;
			EntityRenderer renderer = val.Value;
			if (key.IsRendered)
			{
				renderer.DoRender2D(dt);
			}
		}
		ScreenManager.FrameProfiler.Mark("ree2d-d");
	}

	public override void Dispose(ClientMain game)
	{
		foreach (KeyValuePair<Entity, EntityRenderer> entityRenderer in game.EntityRenderers)
		{
			entityRenderer.Value.Dispose();
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
