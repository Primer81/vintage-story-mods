namespace Vintagestory.GameContent;

public interface ITemperatureSensitive
{
	bool IsHot { get; }

	void CoolNow(float amountRel);
}
