using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class PotInFirepitRenderer : IInFirepitRenderer, IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private MultiTextureMeshRef potWithFoodRef;

	private MultiTextureMeshRef potRef;

	private MultiTextureMeshRef lidRef;

	private BlockPos pos;

	private float temp;

	private ILoadedSound cookingSound;

	private bool isInOutputSlot;

	private Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 20;

	public PotInFirepitRenderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos, bool isInOutputSlot)
	{
		this.capi = capi;
		this.pos = pos;
		this.isInOutputSlot = isInOutputSlot;
		BlockCookedContainer potBlock = capi.World.GetBlock(stack.Collectible.CodeWithVariant("type", "cooked")) as BlockCookedContainer;
		if (isInOutputSlot)
		{
			MealMeshCache meshcache = capi.ModLoader.GetModSystem<MealMeshCache>();
			potWithFoodRef = meshcache.GetOrCreateMealInContainerMeshRef(potBlock, potBlock.GetCookingRecipe(capi.World, stack), potBlock.GetNonEmptyContents(capi.World, stack), new Vec3f(0f, 5f / 32f, 0f));
			return;
		}
		string basePath = (potBlock.Code.PathStartsWith("dirtyclaypot") ? "shapes/block/clay/pot-dirty-" : "shapes/block/clay/pot-");
		capi.Tesselator.TesselateShape(potBlock, Shape.TryGet(capi, basePath + "opened-empty.json"), out var potMesh);
		potRef = capi.Render.UploadMultiTextureMesh(potMesh);
		capi.Tesselator.TesselateShape(potBlock, Shape.TryGet(capi, basePath + "part-lid.json"), out var lidMesh);
		lidRef = capi.Render.UploadMultiTextureMesh(lidMesh);
	}

	public void Dispose()
	{
		potRef?.Dispose();
		lidRef?.Dispose();
		cookingSound?.Stop();
		cookingSound?.Dispose();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		IRenderAPI rpi = capi.Render;
		Vec3d camPos = capi.World.Player.Entity.CameraPos;
		rpi.GlDisableCullFace();
		rpi.GlToggleBlend(blend: true);
		IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
		prog.DontWarpVertices = 0;
		prog.AddRenderFlags = 0;
		prog.RgbaAmbientIn = rpi.AmbientColor;
		prog.RgbaFogIn = rpi.FogColor;
		prog.FogMinIn = rpi.FogMin;
		prog.FogDensityIn = rpi.FogDensity;
		prog.RgbaTint = ColorUtil.WhiteArgbVec;
		prog.NormalShaded = 1;
		prog.ExtraGodray = 0f;
		prog.SsaoAttn = 0f;
		prog.AlphaTest = 0.05f;
		prog.OverlayOpacity = 0f;
		prog.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X + 0.0010000000474974513, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z - 0.0010000000474974513).Translate(0f, 0.0625f, 0f)
			.Values;
		prog.ViewMatrix = rpi.CameraMatrixOriginf;
		prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
		rpi.RenderMultiTextureMesh((potRef == null) ? potWithFoodRef : potRef, "tex");
		if (!isInOutputSlot)
		{
			float origx = GameMath.Sin((float)capi.World.ElapsedMilliseconds / 300f) * 5f / 16f;
			float origz = GameMath.Cos((float)capi.World.ElapsedMilliseconds / 300f) * 5f / 16f;
			float cookIntensity = GameMath.Clamp((temp - 50f) / 50f, 0f, 1f);
			prog.ModelMatrix = ModelMat.Identity().Translate((double)pos.X - camPos.X, (double)pos.Y - camPos.Y, (double)pos.Z - camPos.Z).Translate(0f, 13f / 32f, 0f)
				.Translate(0f - origx, 0f, 0f - origz)
				.RotateX(cookIntensity * GameMath.Sin((float)capi.World.ElapsedMilliseconds / 50f) / 60f)
				.RotateZ(cookIntensity * GameMath.Sin((float)capi.World.ElapsedMilliseconds / 50f) / 60f)
				.Translate(origx, 0f, origz)
				.Values;
			prog.ViewMatrix = rpi.CameraMatrixOriginf;
			prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			rpi.RenderMultiTextureMesh(lidRef, "tex");
		}
		prog.Stop();
	}

	public void OnUpdate(float temperature)
	{
		temp = temperature;
		float soundIntensity = GameMath.Clamp((temp - 50f) / 50f, 0f, 1f);
		SetCookingSoundVolume(isInOutputSlot ? 0f : soundIntensity);
	}

	public void OnCookingComplete()
	{
		isInOutputSlot = true;
	}

	public void SetCookingSoundVolume(float volume)
	{
		if (volume > 0f)
		{
			if (cookingSound == null)
			{
				cookingSound = capi.World.LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/effect/cooking.ogg"),
					ShouldLoop = true,
					Position = pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Range = 10f,
					ReferenceDistance = 3f,
					Volume = volume
				});
				cookingSound.Start();
			}
			else
			{
				cookingSound.SetVolume(volume);
			}
		}
		else if (cookingSound != null)
		{
			cookingSound.Stop();
			cookingSound.Dispose();
			cookingSound = null;
		}
	}
}
