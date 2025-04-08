namespace Vintagestory.API.Common;

/// <summary>
/// Defines how multiple animations should be blended together.
/// </summary>
[DocumentAsJson]
public enum EnumAnimationBlendMode
{
	/// <summary>
	/// Add the animation without taking other animations into considerations
	/// </summary>
	Add,
	/// <summary>
	/// Add the pose and average it together with all other running animations with blendmode Average or AddAverage
	/// </summary>
	Average,
	/// <summary>
	/// Add the animation without taking other animations into consideration, but add it's weight for averaging 
	/// </summary>
	AddAverage
}
