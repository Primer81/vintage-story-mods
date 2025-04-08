namespace Vintagestory.GameContent;

public class TraderPersonality
{
	public float ChordDelayMul = 1f;

	public float PitchModifier = 1f;

	public float VolumneModifier = 1f;

	public TraderPersonality(float chordDelayMul, float pitchModifier, float volumneModifier)
	{
		ChordDelayMul = chordDelayMul;
		PitchModifier = pitchModifier;
		VolumneModifier = volumneModifier;
	}
}
