using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class VAO : MeshRef
{
	public int VaoId;

	public int IndicesCount;

	public PrimitiveType drawMode = PrimitiveType.Triangles;

	public int vaoSlotNumber;

	public int vboIdIndex;

	public int xyzVboId;

	public int normalsVboId;

	public int uvVboId;

	public int rgbaVboId;

	public int flagsVboId;

	public int customDataFloatVboId;

	public int customDataIntVboId;

	public int customDataShortVboId;

	public int customDataByteVboId;

	public bool Persistent;

	public nint xyzPtr;

	public nint normalsPtr;

	public nint uvPtr;

	public nint rgbaPtr;

	public nint flagsPtr;

	public nint customDataFloatPtr;

	public nint customDataIntPtr;

	public nint customDataShortPtr;

	public nint customDataBytePtr;

	public nint indicesPtr;

	private string trace;

	public override bool Initialized => VaoId != 0;

	public VAO()
	{
		if (RuntimeEnv.DebugVAODispose)
		{
			trace = Environment.StackTrace;
		}
	}

	public override void Dispose()
	{
		if (!base.Disposed)
		{
			if (xyzVboId != 0)
			{
				GL.DeleteBuffer(xyzVboId);
			}
			if (normalsVboId != 0)
			{
				GL.DeleteBuffer(normalsVboId);
			}
			if (uvVboId != 0)
			{
				GL.DeleteBuffer(uvVboId);
			}
			if (rgbaVboId != 0)
			{
				GL.DeleteBuffer(rgbaVboId);
			}
			if (customDataFloatVboId != 0)
			{
				GL.DeleteBuffer(customDataFloatVboId);
			}
			if (customDataShortVboId != 0)
			{
				GL.DeleteBuffer(customDataShortVboId);
			}
			if (customDataIntVboId != 0)
			{
				GL.DeleteBuffer(customDataIntVboId);
			}
			if (customDataByteVboId != 0)
			{
				GL.DeleteBuffer(customDataByteVboId);
			}
			if (vboIdIndex != 0)
			{
				GL.DeleteBuffer(vboIdIndex);
			}
			if (flagsVboId != 0)
			{
				GL.DeleteBuffer(flagsVboId);
			}
			GL.DeleteVertexArray(VaoId);
			base.Dispose();
		}
	}

	~VAO()
	{
		if (!base.Disposed && !ScreenManager.Platform.IsShuttingDown)
		{
			if (!RuntimeEnv.DebugVAODispose)
			{
				ScreenManager.Platform.Logger.Debug("MeshRef with vao id {0} with {1} indices is leaking memory, missing call to Dispose. Set env var VAO_DEBUG_DISPOSE to get allocation trace.", VaoId, IndicesCount);
			}
			else
			{
				ScreenManager.Platform.Logger.Debug("MeshRef with vao id {0} with {1} indices is leaking memory, missing call to Dispose. Allocated at {2}.", VaoId, IndicesCount, trace);
			}
		}
	}
}
