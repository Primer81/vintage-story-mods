using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class WireframeCube
{
	private MeshRef modelRef;

	private Matrixf mvMat = new Matrixf();

	/// <summary>
	/// Creates a cube mesh with edge points 0/0/0 and 1/1/1
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="color"></param>
	/// <returns></returns>
	public static WireframeCube CreateUnitCube(ICoreClientAPI capi, int color = int.MinValue)
	{
		WireframeCube cube = new WireframeCube();
		MeshData data = LineMeshUtil.GetCube(color);
		data.Scale(new Vec3f(), 0.5f, 0.5f, 0.5f);
		data.Translate(0.5f, 0.5f, 0.5f);
		data.Flags = new int[data.VerticesCount];
		for (int i = 0; i < data.Flags.Length; i++)
		{
			data.Flags[i] = 256;
		}
		cube.modelRef = capi.Render.UploadMesh(data);
		return cube;
	}

	/// <summary>
	/// Creates a cube mesh with edge points -1/-1/-1 and 1/1/1
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="color"></param>
	/// <returns></returns>
	public static WireframeCube CreateCenterOriginCube(ICoreClientAPI capi, int color = int.MinValue)
	{
		WireframeCube cube = new WireframeCube();
		MeshData data = LineMeshUtil.GetCube(color);
		data.Flags = new int[data.VerticesCount];
		for (int i = 0; i < data.Flags.Length; i++)
		{
			data.Flags[i] = 256;
		}
		cube.modelRef = capi.Render.UploadMesh(data);
		return cube;
	}

	public void Render(ICoreClientAPI capi, double posx, double posy, double posz, float scalex, float scaley, float scalez, float lineWidth = 1.6f, Vec4f color = null)
	{
		EntityPlayer eplr = capi.World.Player.Entity;
		mvMat.Identity().Set(capi.Render.CameraMatrixOrigin).Translate(posx - eplr.CameraPos.X, posy - eplr.CameraPos.Y, posz - eplr.CameraPos.Z)
			.Scale(scalex, scaley, scalez);
		Render(capi, mvMat, lineWidth, color);
	}

	public void Render(ICoreClientAPI capi, Matrixf mat, float lineWidth = 1.6f, Vec4f color = null)
	{
		IShaderProgram program = capi.Shader.GetProgram(25);
		program.Use();
		capi.Render.LineWidth = lineWidth;
		capi.Render.GLEnableDepthTest();
		capi.Render.GLDepthMask(on: false);
		capi.Render.GlToggleBlend(blend: true);
		program.Uniform("origin", new Vec3f(0f, 0f, 0f));
		program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		program.UniformMatrix("modelViewMatrix", mat.Values);
		program.Uniform("colorIn", color ?? ColorUtil.WhiteArgbVec);
		capi.Render.RenderMesh(modelRef);
		program.Stop();
		if (lineWidth != 1.6f)
		{
			capi.Render.LineWidth = 1.6f;
		}
	}

	public void Dispose()
	{
		modelRef?.Dispose();
	}
}
