using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ActivityVisualizer : IRenderer, IDisposable
{
	private EntityActivity entityActivity;

	private ICoreClientAPI capi;

	private MeshData pathModel;

	private MeshRef pathModelRef;

	private Vec3d origin;

	private List<VisualizerLabel> labels = new List<VisualizerLabel>();

	private int vertexIndex;

	private Entity sourceEntity;

	private Vec3d curPos;

	private int lineIndex;

	private float accum;

	public ICoreClientAPI Api => capi;

	public double RenderOrder => 1.0;

	public int RenderRange => 999;

	public Vec3d CurrentPos => curPos;

	public ActivityVisualizer(EntityActivity entityActivity)
	{
		this.entityActivity = entityActivity;
	}

	public ActivityVisualizer(ICoreClientAPI capi, EntityActivity entityActivity, Entity sourceEntity)
	{
		this.capi = capi;
		this.entityActivity = entityActivity;
		this.sourceEntity = sourceEntity;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "activityvisualizer");
		capi.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "activityvisualizer2d");
		GenerateCameraPathModel();
	}

	private void InitModel()
	{
		lineIndex = 0;
		vertexIndex = 0;
		pathModel = new MeshData(4, 4, withNormals: false, withUv: false);
		pathModel.SetMode(EnumDrawMode.LineStrip);
		pathModelRef?.Dispose();
		pathModelRef = null;
		origin = capi.World.Player.Entity.Pos.XYZ;
		foreach (VisualizerLabel label in labels)
		{
			label.Texture?.Dispose();
		}
		labels = new List<VisualizerLabel>();
	}

	private void GenerateCameraPathModel()
	{
		InitModel();
		curPos = null;
		IEntityAction[] actions = entityActivity.Actions;
		for (int i = 0; i < actions.Length; i++)
		{
			actions[i].OnVisualize(this);
		}
		pathModelRef?.Dispose();
		pathModelRef = capi.Render.UploadMesh(pathModel);
	}

	private void addPoint(double x, double y, double z, int color)
	{
		pathModel.AddVertexSkipTex((float)(x - origin.X), (float)(y - origin.Y + 0.1), (float)(z - origin.Z), color);
		pathModel.AddIndex(vertexIndex++);
	}

	public void GoTo(Vec3d target, int color = -1)
	{
		if (curPos == null)
		{
			curPos = target.Clone();
			return;
		}
		LineTo(curPos, target, color);
		curPos.Set(target);
	}

	public void LineTo(Vec3d source, Vec3d target, int color = -1)
	{
		if (color == -1)
		{
			color = ((lineIndex % 2 == 0) ? (-1) : ColorUtil.ToRgba(255, 255, 50, 50));
		}
		addPoint(curPos.X, curPos.Y, curPos.Z, color);
		addPoint(target.X, target.Y, target.Z, color);
		TextBackground bg = new TextBackground
		{
			FillColor = GuiStyle.DialogLightBgColor,
			Padding = 3,
			Radius = GuiStyle.ElementBGRadius
		};
		labels.Add(new VisualizerLabel
		{
			Pos = (curPos + target) / 2f,
			Texture = Api.Gui.TextTexture.GenTextTexture(lineIndex.ToString() ?? "", CairoFont.WhiteMediumText(), bg)
		});
		lineIndex++;
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (stage == EnumRenderStage.Ortho)
		{
			Render2d(dt);
			return;
		}
		accum += dt;
		if (accum > 1f)
		{
			GenerateCameraPathModel();
			accum = 0f;
		}
		IShaderProgram engineShader = capi.Render.GetEngineShader(EnumShaderProgram.Autocamera);
		engineShader.Use();
		capi.Render.LineWidth = 2f;
		capi.Render.BindTexture2d(0);
		capi.Render.GlPushMatrix();
		capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);
		Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
		capi.Render.GlTranslate((float)(origin.X - cameraPos.X), (float)(origin.Y - cameraPos.Y), (float)(origin.Z - cameraPos.Z));
		engineShader.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		engineShader.UniformMatrix("modelViewMatrix", capi.Render.CurrentModelviewMatrix);
		capi.Render.RenderMesh(pathModelRef);
		capi.Render.GlPopMatrix();
		engineShader.Stop();
	}

	private void Render2d(float deltaTime)
	{
		IRenderAPI rapi = capi.Render;
		foreach (VisualizerLabel label in labels)
		{
			Vec3d pos = MatrixToolsd.Project(label.Pos, rapi.PerspectiveProjectionMat, rapi.PerspectiveViewMat, rapi.FrameWidth, rapi.FrameHeight);
			pos.X -= label.Texture.Width / 2;
			pos.Y += (double)label.Texture.Height * 1.5;
			rapi.Render2DTexture(label.Texture.TextureId, (float)pos.X, (float)rapi.FrameHeight - (float)pos.Y, label.Texture.Width, label.Texture.Height, 20f);
		}
	}

	public void Dispose()
	{
		pathModelRef?.Dispose();
		capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		foreach (VisualizerLabel label in labels)
		{
			label.Texture?.Dispose();
		}
		labels.Clear();
	}
}
