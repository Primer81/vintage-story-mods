using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Contains all the data for the given model pool.
/// </summary>
public class ModelDataPoolLocation
{
	public static int VisibleBufIndex;

	/// <summary>
	/// The ID of the pool model.
	/// </summary>
	public int PoolId;

	/// <summary>
	/// Where the indices of the model start.
	/// </summary>
	public int IndicesStart;

	/// <summary>
	/// Where the indices of the model end.
	/// </summary>
	public int IndicesEnd;

	/// <summary>
	/// Where the vertices start.
	/// </summary>
	public int VerticesStart;

	/// <summary>
	/// Where the vertices end.
	/// </summary>
	public int VerticesEnd;

	/// <summary>
	/// The culling sphere.
	/// </summary>
	public Sphere FrustumCullSphere;

	/// <summary>
	/// Whether this model is visible or not.
	/// </summary>
	public bool FrustumVisible;

	public Bools CullVisible = new Bools(a: true, b: true);

	public int LodLevel;

	public bool Hide;

	/// <summary>
	/// Used for models with movements (like a door).
	/// </summary>
	public int TransitionCounter;

	private bool UpdateVisibleFlag(bool inFrustum)
	{
		FrustumVisible = inFrustum;
		return FrustumVisible;
	}

	public bool IsVisible(EnumFrustumCullMode mode, FrustumCulling culler)
	{
		switch (mode)
		{
		case EnumFrustumCullMode.CullInstant:
			if (!Hide && CullVisible[VisibleBufIndex])
			{
				return culler.InFrustum(FrustumCullSphere);
			}
			return false;
		case EnumFrustumCullMode.CullInstantShadowPassNear:
			if (!Hide && CullVisible[VisibleBufIndex])
			{
				return culler.InFrustumShadowPass(FrustumCullSphere);
			}
			return false;
		case EnumFrustumCullMode.CullInstantShadowPassFar:
			if (!Hide && CullVisible[VisibleBufIndex] && culler.InFrustumShadowPass(FrustumCullSphere))
			{
				return LodLevel >= 1;
			}
			return false;
		case EnumFrustumCullMode.CullNormal:
			if (!Hide && CullVisible[VisibleBufIndex])
			{
				return UpdateVisibleFlag(culler.InFrustumAndRange(FrustumCullSphere, FrustumVisible, LodLevel));
			}
			return false;
		default:
			return !Hide;
		}
	}
}
