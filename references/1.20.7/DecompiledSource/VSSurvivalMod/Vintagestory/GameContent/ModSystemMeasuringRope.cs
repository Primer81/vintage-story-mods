using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemMeasuringRope : ModSystem, IRenderer, IDisposable
{
	private LoadedTexture hudTexture;

	private ICoreClientAPI capi;

	private BlockPos blockstart;

	private BlockPos blockend;

	private Vec3d start;

	private Vec3d end = new Vec3d();

	private BlockSelection blockSel;

	private float accum;

	private bool requireRedraw;

	private MeshData updateModel;

	private MeshRef pathModelRef;

	public double RenderOrder => 0.9;

	public int RenderRange => 99;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "measuringRope");
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "measuringrope");
		hudTexture = new LoadedTexture(api);
		MeshData pathModel = new MeshData(4, 4, withNormals: false, withUv: false);
		pathModel.SetMode(EnumDrawMode.LineStrip);
		pathModelRef = null;
		pathModel.AddVertexSkipTex(0f, 0f, 0f);
		pathModel.AddIndex(0);
		pathModel.AddVertexSkipTex(1f, 1f, 1f);
		pathModel.AddIndex(1);
		updateModel = new MeshData(initialiseArrays: false);
		updateModel.xyz = new float[6];
		pathModelRef = capi.Render.UploadMesh(pathModel);
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		ItemSlot slot = capi.World.Player.Entity.RightHandItemSlot;
		if (!(slot.Itemstack?.Collectible is ItemMeasuringRope))
		{
			return;
		}
		if (stage == EnumRenderStage.Opaque)
		{
			if (start != null && hudTexture.TextureId > 0)
			{
				onRender3D(dt);
			}
			return;
		}
		ITreeAttribute attr = slot.Itemstack.Attributes;
		bool didHavePos = start != null;
		bool num = attr.HasAttribute("startX");
		requireRedraw |= hudTexture.TextureId == 0;
		requireRedraw |= capi.World.Player.CurrentBlockSelection == null != (blockSel == null);
		if (num)
		{
			blockSel = capi.World.Player.CurrentBlockSelection;
			Vec3d newEndPos = blockSel?.FullPosition ?? capi.World.Player.Entity.Pos.XYZ;
			requireRedraw |= !end.Equals(newEndPos, 0.001);
			end = newEndPos;
			blockend = blockSel?.Position ?? capi.World.Player.Entity.Pos.AsBlockPos;
			if (!didHavePos)
			{
				start = new Vec3d();
				blockstart = new BlockPos();
				requireRedraw |= true;
			}
			else
			{
				double x = attr.GetDouble("startX");
				double y = attr.GetDouble("startY");
				double z = attr.GetDouble("startZ");
				requireRedraw |= !start.Equals(new Vec3d(x, y, z), 0.001);
				start.Set(x, y, z);
				blockstart = new BlockPos(attr.GetInt("blockX"), attr.GetInt("blockY"), attr.GetInt("blockZ"));
			}
		}
		else if (didHavePos)
		{
			start = null;
			requireRedraw |= true;
		}
		accum += dt;
		if (requireRedraw && (double)accum > 0.1)
		{
			redrawHud();
			accum = 0f;
		}
		if (hudTexture.TextureId > 0)
		{
			capi.Render.Render2DLoadedTexture(hudTexture, capi.Render.FrameWidth / 2 - hudTexture.Width / 2, capi.Render.FrameHeight / 2 - hudTexture.Height - 50);
		}
	}

	private void onRender3D(float dt)
	{
		IShaderProgram program = capi.Shader.GetProgram(23);
		updateModel.xyz[3] = (float)(end.X - start.X);
		updateModel.xyz[4] = (float)(end.Y - start.Y);
		updateModel.xyz[5] = (float)(end.Z - start.Z);
		updateModel.VerticesCount = 2;
		capi.Render.UpdateMesh(pathModelRef, updateModel);
		program.Use();
		capi.Render.LineWidth = 2f;
		capi.Render.BindTexture2d(0);
		capi.Render.GlPushMatrix();
		capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		capi.Render.GlTranslate((float)(start.X - cameraPos.X), (float)(start.Y - cameraPos.Y), (float)(start.Z - cameraPos.Z));
		program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		program.UniformMatrix("modelViewMatrix", capi.Render.CurrentModelviewMatrix);
		capi.Render.RenderMesh(pathModelRef);
		capi.Render.GlPopMatrix();
		program.Stop();
	}

	private void redrawHud()
	{
		string text = "Right click to set starting point. Left click to clear starting point.";
		if (start != null)
		{
			text = string.Format("{0:0.#}\nDistance: {1}\nOffset: ~{2} ~{3} ~{4}", (blockSel != null) ? "Measuring Block to Block" : "Measuring Block to Player", start.DistanceTo(end), blockend.X - blockstart.X, blockend.Y - blockstart.Y, blockend.Z - blockstart.Z);
		}
		capi.Gui.TextTexture.GenOrUpdateTextTexture(text, CairoFont.WhiteSmallText(), 400, 110, ref hudTexture, new TextBackground
		{
			FillColor = GuiStyle.DialogLightBgColor,
			Padding = 5
		});
		requireRedraw = false;
	}

	public override void Dispose()
	{
		hudTexture?.Dispose();
		pathModelRef?.Dispose();
	}
}
