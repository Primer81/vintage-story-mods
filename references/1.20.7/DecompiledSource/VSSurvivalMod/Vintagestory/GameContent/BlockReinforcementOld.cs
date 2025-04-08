using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class BlockReinforcementOld
{
	public int Strength;

	public string PlayerUID;

	public string LastPlayername;

	public BlockReinforcement Update()
	{
		return new BlockReinforcement
		{
			Strength = Strength,
			PlayerUID = PlayerUID,
			LastPlayername = LastPlayername
		};
	}
}
