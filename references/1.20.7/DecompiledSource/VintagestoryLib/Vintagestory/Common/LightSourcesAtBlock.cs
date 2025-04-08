namespace Vintagestory.Common;

public class LightSourcesAtBlock
{
	public byte[] lightHsvs = new byte[45];

	public byte lightCount;

	public byte LastBrightness => lightHsvs[lightCount - 1];

	public void AddHsv(byte h, byte s, byte v)
	{
		if (lightCount <= 14)
		{
			lightHsvs[3 * lightCount] = h;
			lightHsvs[3 * lightCount + 1] = s;
			lightHsvs[3 * lightCount + 2] = v;
			lightCount++;
		}
	}
}
