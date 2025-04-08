using System.Collections.Generic;
using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class CharacterSelectionPacket
{
	public bool DidSelect;

	public ClothStack[] Clothes;

	public string CharacterClass;

	public Dictionary<string, string> SkinParts;

	public string VoiceType;

	public string VoicePitch;
}
