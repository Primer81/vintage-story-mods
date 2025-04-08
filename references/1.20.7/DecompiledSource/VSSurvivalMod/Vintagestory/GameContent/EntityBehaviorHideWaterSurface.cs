using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorHideWaterSurface : EntityBehavior, IRenderer, IDisposable, ITexPositionSource
{
	private MultiTextureMeshRef meshref;

	private ICoreClientAPI capi;

	private string hideWaterElement;

	private Size2i dummysize = new Size2i(2048, 2048);

	private TextureAtlasPosition dummyPos = new TextureAtlasPosition
	{
		x1 = 0f,
		y1 = 0f,
		x2 = 1f,
		y2 = 1f
	};

	protected float[] tmpMvMat = Mat4f.Create();

	public double RenderOrder => 0.36;

	public int RenderRange => 99;

	public Size2i AtlasSize => dummysize;

	public TextureAtlasPosition this[string textureCode] => dummyPos;

	public EntityBehaviorHideWaterSurface(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		capi = entity.World.Api as ICoreClientAPI;
		capi.Event.RegisterRenderer(this, EnumRenderStage.OIT, "re-ebhhws");
		hideWaterElement = attributes["hideWaterElement"].AsString();
	}

	public override void OnTesselated()
	{
		CompositeShape compositeShape = entity.Properties.Client.Shape;
		Shape entityShape = entity.Properties.Client.LoadedShapeForEntity;
		try
		{
			TesselationMetaData tesselationMetaData = new TesselationMetaData();
			tesselationMetaData.QuantityElements = compositeShape.QuantityElements;
			tesselationMetaData.SelectiveElements = new string[1] { hideWaterElement };
			tesselationMetaData.TexSource = this;
			tesselationMetaData.WithJointIds = true;
			tesselationMetaData.WithDamageEffect = true;
			tesselationMetaData.TypeForLogging = "entity";
			tesselationMetaData.Rotation = new Vec3f(compositeShape.rotateX, compositeShape.rotateY, compositeShape.rotateZ);
			TesselationMetaData meta = tesselationMetaData;
			capi.Tesselator.TesselateShape(meta, entityShape, out var meshdata);
			meshdata.Translate(compositeShape.offsetX, compositeShape.offsetY, compositeShape.offsetZ);
			meshref?.Dispose();
			meshref = capi.Render.UploadMultiTextureMesh(meshdata);
		}
		catch (Exception e)
		{
			capi.World.Logger.Fatal("Failed tesselating entity {0} with id {1}. Entity will probably be invisible!.", entity.Code, entity.EntityId);
			capi.World.Logger.Fatal(e);
		}
	}

	public void Dispose()
	{
		meshref?.Dispose();
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		meshref?.Dispose();
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (meshref != null && entity.Properties.Client.Renderer is EntityShapeRenderer esr)
		{
			capi.Render.GLDepthMask(on: true);
			IShaderProgram program = capi.Shader.GetProgram(32);
			program.Use();
			float[] modelMat = esr.ModelMat;
			Mat4f.Mul(tmpMvMat, capi.Render.CurrentProjectionMatrix, capi.Render.CameraMatrixOriginf);
			Mat4f.Mul(tmpMvMat, tmpMvMat, modelMat);
			program.BindTexture2D("tex2d", 0, 0);
			program.UniformMatrix("mvpMatrix", tmpMvMat);
			program.Uniform("origin", new Vec3f(0f, 0f, 0f));
			capi.Render.RenderMultiTextureMesh(meshref, "tex2d");
			program.Stop();
			capi.Render.GLDepthMask(on: false);
			capi.Render.GLEnableDepthTest();
		}
	}

	public override string PropertyName()
	{
		return "hidewatersurface";
	}
}
