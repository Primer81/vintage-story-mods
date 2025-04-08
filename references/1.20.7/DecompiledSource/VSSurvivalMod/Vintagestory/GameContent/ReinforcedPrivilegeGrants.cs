using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ReinforcedPrivilegeGrants
{
	public string OwnedByPlayerUid;

	public int OwnedByGroupId;

	public Dictionary<string, EnumBlockAccessFlags> PlayerGrants = new Dictionary<string, EnumBlockAccessFlags>();

	public Dictionary<int, EnumBlockAccessFlags> GroupGrants = new Dictionary<int, EnumBlockAccessFlags>();
}
