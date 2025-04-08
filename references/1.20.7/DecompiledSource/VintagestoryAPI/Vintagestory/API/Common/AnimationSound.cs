namespace Vintagestory.API.Common;

[DocumentAsJson]
public class AnimationSound
{
	public int Frame;

	public AssetLocation Location;

	public bool RandomizePitch = true;

	public float Range = 32f;

	public AnimationSound Clone()
	{
		return new AnimationSound
		{
			Frame = Frame,
			Location = Location.Clone(),
			RandomizePitch = RandomizePitch,
			Range = Range
		};
	}
}
