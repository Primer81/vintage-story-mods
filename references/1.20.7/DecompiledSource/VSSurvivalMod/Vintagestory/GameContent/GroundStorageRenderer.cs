using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GroundStorageRenderer : IRenderer, IDisposable
{
	private readonly ICoreClientAPI capi;

	private readonly BlockEntityGroundStorage groundStorage;

	public Matrixf ModelMat = new Matrixf();

	private int[] itemTemps;

	private float accumDelta;

	private bool check500;

	private bool check450;

	public double RenderOrder => 0.5;

	public int RenderRange => 30;

	public GroundStorageRenderer(ICoreClientAPI capi, BlockEntityGroundStorage groundStorage)
	{
		this.capi = capi;
		this.groundStorage = groundStorage;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque);
		itemTemps = new int[groundStorage.Inventory.Count];
		UpdateTemps();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		accumDelta += deltaTime;
		EntityPos pos = capi.World.Player.Entity.Pos;
		float dist = groundStorage.Pos.DistanceSqTo(pos.X, pos.Y, pos.Z);
		bool outOfRange = (float)(RenderRange * RenderRange) < dist;
		if (accumDelta > 1f)
		{
			UpdateTemps();
		}
		if (!groundStorage.UseRenderer || groundStorage.Inventory.Empty || outOfRange)
		{
			return;
		}
		IRenderAPI rpi = capi.Render;
		Vec3d camPos = capi.World.Player.Entity.CameraPos;
		IStandardShaderProgram prog = rpi.PreparedStandardShader(groundStorage.Pos.X, groundStorage.Pos.Y, groundStorage.Pos.Z);
		Vec3f[] offs = new Vec3f[groundStorage.DisplayedItems];
		groundStorage.GetLayoutOffset(offs);
		Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(groundStorage.Pos.X, groundStorage.Pos.Y, groundStorage.Pos.Z);
		rpi.GlDisableCullFace();
		rpi.GlToggleBlend(blend: true);
		prog.ViewMatrix = rpi.CameraMatrixOriginf;
		prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
		for (int index = 0; index < groundStorage.MeshRefs.Length; index++)
		{
			ItemStack stack = groundStorage.Inventory[index]?.Itemstack;
			if (stack != null && groundStorage.MeshRefs != null)
			{
				float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(itemTemps[index]);
				int gi = GameMath.Clamp((itemTemps[index] - 500) / 3, 0, 255);
				ModelMat.Identity().Translate((double)groundStorage.Pos.X - camPos.X, (double)groundStorage.Pos.Y - camPos.Y, (double)groundStorage.Pos.Z - camPos.Z).Translate(0.5f, 0.5f, 0.5f)
					.RotateY(groundStorage.MeshAngle)
					.Translate(-0.5f, -0.5f, -0.5f)
					.Translate(offs[index].X, offs[index].Y, offs[index].Z);
				ModelTransform transform = groundStorage.ModelTransformsRenderer[index];
				if (transform != null)
				{
					ModelMat.Translate(0.5f, 0.5f, 0.5f).RotateY(transform.Rotation.Y).Translate(-0.5f, -0.5f, -0.5f)
						.Translate(0.5f, 0f, 0.5f)
						.Scale(transform.ScaleXYZ.X, transform.ScaleXYZ.Y, transform.ScaleXYZ.Z)
						.Translate(-0.5f, -0f, -0.5f);
				}
				prog.ModelMatrix = ModelMat.Values;
				prog.TempGlowMode = 1;
				prog.RgbaLightIn = lightrgbs;
				prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)gi / 255f);
				prog.ExtraGlow = gi;
				prog.AverageColor = ColorUtil.ToRGBAVec4f(capi.BlockTextureAtlas.GetAverageColor((stack.Item?.FirstTexture ?? stack.Block.FirstTextureInventory).Baked.TextureSubId));
				MultiTextureMeshRef meshRef = groundStorage.MeshRefs[index];
				if (meshRef != null && !meshRef.Disposed)
				{
					rpi.RenderMultiTextureMesh(meshRef, "tex");
				}
			}
		}
		prog.TempGlowMode = 0;
		prog.Stop();
	}

	public void UpdateTemps()
	{
		accumDelta = 0f;
		float maxTemp = 0f;
		for (int index = 0; index < groundStorage.Inventory.Count; index++)
		{
			ItemStack itemStack = groundStorage.Inventory[index].Itemstack;
			itemTemps[index] = (int)(itemStack?.Collectible.GetTemperature(capi.World, itemStack) ?? 0f);
			maxTemp = Math.Max(maxTemp, itemTemps[index]);
		}
		if (!groundStorage.NeedsRetesselation)
		{
			if (maxTemp < 500f && !check500)
			{
				check500 = true;
				groundStorage.NeedsRetesselation = true;
				groundStorage.MarkDirty(redrawOnClient: true);
			}
			if (maxTemp < 450f && !check450)
			{
				check450 = true;
				groundStorage.NeedsRetesselation = true;
				groundStorage.MarkDirty(redrawOnClient: true);
			}
		}
		if (maxTemp > 500f && (check500 || check450))
		{
			check500 = false;
			check450 = false;
		}
	}

	public void Dispose()
	{
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
	}
}
