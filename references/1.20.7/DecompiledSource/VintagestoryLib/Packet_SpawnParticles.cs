public class Packet_SpawnParticles
{
	public string ParticlePropertyProviderClassName;

	public byte[] Data;

	public const int ParticlePropertyProviderClassNameFieldID = 1;

	public const int DataFieldID = 2;

	public void SetParticlePropertyProviderClassName(string value)
	{
		ParticlePropertyProviderClassName = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
