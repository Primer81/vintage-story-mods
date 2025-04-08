using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class AnimCacheEntry
{
	/// <summary>
	/// Animations of this cache.
	/// </summary>
	public Animation[] Animations;

	/// <summary>
	/// The root elements of this cache.
	/// </summary>
	public ShapeElement[] RootElems;

	/// <summary>
	/// The poses of this cache
	/// </summary>
	public List<ElementPose> RootPoses;
}
