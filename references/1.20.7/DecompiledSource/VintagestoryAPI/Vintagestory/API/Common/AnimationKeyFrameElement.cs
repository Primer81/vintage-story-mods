using Newtonsoft.Json;

namespace Vintagestory.API.Common;

[JsonObject(MemberSerialization.OptIn)]
public class AnimationKeyFrameElement
{
	[JsonProperty]
	public double? OffsetX;

	[JsonProperty]
	public double? OffsetY;

	[JsonProperty]
	public double? OffsetZ;

	[JsonProperty]
	public double? StretchX;

	[JsonProperty]
	public double? StretchY;

	[JsonProperty]
	public double? StretchZ;

	[JsonProperty]
	public double? RotationX;

	[JsonProperty]
	public double? RotationY;

	[JsonProperty]
	public double? RotationZ;

	[JsonProperty]
	public double? OriginX;

	[JsonProperty]
	public double? OriginY;

	[JsonProperty]
	public double? OriginZ;

	[JsonProperty]
	public bool RotShortestDistanceX;

	[JsonProperty]
	public bool RotShortestDistanceY;

	[JsonProperty]
	public bool RotShortestDistanceZ;

	internal int Frame;

	public ShapeElement ForElement;

	public bool AnySet
	{
		get
		{
			if (!PositionSet && !StretchSet && !RotationSet)
			{
				return OriginSet;
			}
			return true;
		}
	}

	public bool PositionSet
	{
		get
		{
			if (!OffsetX.HasValue && !OffsetY.HasValue)
			{
				return OffsetZ.HasValue;
			}
			return true;
		}
	}

	public bool StretchSet
	{
		get
		{
			if (!StretchX.HasValue && !StretchY.HasValue)
			{
				return StretchZ.HasValue;
			}
			return true;
		}
	}

	public bool RotationSet
	{
		get
		{
			if (!RotationX.HasValue && !RotationY.HasValue)
			{
				return RotationZ.HasValue;
			}
			return true;
		}
	}

	public bool OriginSet
	{
		get
		{
			if (!OriginX.HasValue && !OriginY.HasValue)
			{
				return OriginZ.HasValue;
			}
			return true;
		}
	}

	internal bool IsSet(int flag)
	{
		return flag switch
		{
			0 => PositionSet, 
			1 => RotationSet, 
			2 => StretchSet, 
			3 => OriginSet, 
			_ => false, 
		};
	}
}
