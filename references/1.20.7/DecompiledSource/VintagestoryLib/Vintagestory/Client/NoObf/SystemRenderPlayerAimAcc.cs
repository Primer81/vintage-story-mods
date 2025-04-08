using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderPlayerAimAcc : ClientSystem
{
	private MeshRef[] aimLinesRef = new MeshRef[4];

	private MeshRef aimRectangleRef;

	public override string Name => "repa";

	public SystemRenderPlayerAimAcc(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnRenderFrame2DOverlay, EnumRenderStage.Ortho, Name, 0.98);
		GenAim();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}

	private void OnRenderFrame2DOverlay(float dt)
	{
		if (game.MouseGrabbed && game.EntityPlayer.Attributes.GetInt("aiming") != 0)
		{
			game.guiShaderProg.RgbaIn = new Vec4f(1f, 1f, 1f, 1f);
			game.guiShaderProg.ExtraGlow = 0;
			game.guiShaderProg.ApplyColor = 1;
			game.guiShaderProg.Tex2d2D = 0;
			game.guiShaderProg.NoTexture = 1f;
			ScreenManager.Platform.CheckGlError();
			game.Platform.GLLineWidth(0.5f);
			game.Platform.SmoothLines(on: true);
			ScreenManager.Platform.CheckGlError();
			game.Platform.GlToggleBlend(on: true);
			game.Platform.BindTexture2d(0);
			ScreenManager.Platform.CheckGlError();
			game.GlPushMatrix();
			game.GlTranslate(game.Width / 2, game.Height / 2, 50.0);
			float aimAcc = Math.Max(0.01f, 1f - game.EntityPlayer.Attributes.GetFloat("aimingAccuracy"));
			float scale = 800f * aimAcc;
			game.GlScale(scale, scale, 0.0);
			game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(aimRectangleRef);
			game.GlPopMatrix();
			game.Platform.GLLineWidth(1f);
			game.GlPushMatrix();
			game.GlTranslate(game.Width / 2, game.Height / 2, 50.0);
			game.GlScale(20.0, 20.0, 0.0);
			game.GlTranslate(0.0, -10f * aimAcc + 0.5f, 0.0);
			game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(aimLinesRef[0]);
			game.GlTranslate(0.0, 20f * aimAcc - 1f, 0.0);
			game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(aimLinesRef[1]);
			game.GlTranslate(-10f * aimAcc + 0.5f, -10f * aimAcc + 0.5f, 0.0);
			game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(aimLinesRef[2]);
			game.GlTranslate(20f * aimAcc - 1f, 0.0, 0.0);
			game.guiShaderProg.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(aimLinesRef[3]);
			game.GlPopMatrix();
		}
	}

	public void GenAim()
	{
		for (int i = 0; i < 4; i++)
		{
			MeshData mesh = new MeshData();
			mesh.SetMode(EnumDrawMode.Lines);
			mesh.Rgba = new byte[8];
			mesh.xyz = new float[6];
			mesh.Indices = new int[2];
			mesh.Uv = new float[4];
			if (i == 0)
			{
				LineMeshUtil.AddLine2D(mesh, 0f, -0.5f, 0f, -1f, -1);
			}
			if (i == 1)
			{
				LineMeshUtil.AddLine2D(mesh, 0f, 0.5f, 0f, 1f, -1);
			}
			if (i == 2)
			{
				LineMeshUtil.AddLine2D(mesh, -1f, 0f, -0.5f, 0f, -1);
			}
			if (i == 3)
			{
				LineMeshUtil.AddLine2D(mesh, 0.5f, 0f, 1f, 0f, -1);
			}
			aimLinesRef[i] = game.Platform.UploadMesh(mesh);
		}
		MeshData meshrect = new MeshData();
		meshrect.SetMode(EnumDrawMode.Lines);
		meshrect.xyz = new float[48];
		meshrect.Rgba = new byte[64];
		meshrect.Indices = new int[16];
		meshrect.Uv = new float[32];
		int color = ColorUtil.ToRgba(128, 255, 255, 255);
		LineMeshUtil.AddLine2D(meshrect, -0.5f, -0.5f, -0.05f, -0.5f, color);
		LineMeshUtil.AddLine2D(meshrect, -0.5f, -0.5f, -0.5f, -0.05f, color);
		LineMeshUtil.AddLine2D(meshrect, 0.05f, -0.5f, 0.5f, -0.5f, color);
		LineMeshUtil.AddLine2D(meshrect, 0.5f, -0.5f, 0.5f, -0.05f, color);
		LineMeshUtil.AddLine2D(meshrect, 0.5f, 0.05f, 0.5f, 0.5f, color);
		LineMeshUtil.AddLine2D(meshrect, 0.5f, 0.5f, 0.05f, 0.5f, color);
		LineMeshUtil.AddLine2D(meshrect, -0.05f, 0.5f, -0.5f, 0.5f, color);
		LineMeshUtil.AddLine2D(meshrect, -0.5f, 0.5f, -0.5f, 0.05f, color);
		aimRectangleRef = game.Platform.UploadMesh(meshrect);
	}

	public override void Dispose(ClientMain game)
	{
		game.Platform.DeleteMesh(aimRectangleRef);
		for (int i = 0; i < aimLinesRef.Length; i++)
		{
			game.Platform.DeleteMesh(aimLinesRef[i]);
		}
	}
}
