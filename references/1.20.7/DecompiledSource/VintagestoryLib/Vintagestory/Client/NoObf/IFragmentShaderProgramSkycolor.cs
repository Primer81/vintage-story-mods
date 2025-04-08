namespace Vintagestory.Client.NoObf;

public interface IFragmentShaderProgramSkycolor
{
	float PlayerToSealevelOffset { set; }

	int DitherSeed { set; }

	int HorizontalResolution { set; }

	float FogWaveCounter { set; }

	int Glow2D { set; }

	int Sky2D { set; }

	float SunsetMod { set; }
}
