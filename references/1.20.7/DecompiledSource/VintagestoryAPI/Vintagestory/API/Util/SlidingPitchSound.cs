using Vintagestory.API.Client;

namespace Vintagestory.API.Util;

public class SlidingPitchSound
{
	public EnumTalkType TalkType;

	public ILoadedSound sound;

	public float startPitch;

	public float endPitch;

	public float length;

	public long startMs;

	public float StartVolumne;

	public float EndVolumne;

	public bool Vibrato;
}
