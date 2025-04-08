namespace Vintagestory.Client.NoObf;

public interface IVertexShaderProgramShadowcoords
{
	float ShadowRangeFar { set; }

	float[] ToShadowMapSpaceMatrixFar { set; }

	float ShadowRangeNear { set; }

	float[] ToShadowMapSpaceMatrixNear { set; }
}
