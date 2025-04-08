using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ReinforcedPrivilegeGrantsGroup
{
	public string OwnedByPlayerUid;

	public int OwnedByGroupId;

	public EnumBlockAccessFlags DefaultGrants = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use;

	public Dictionary<string, EnumBlockAccessFlags> PlayerGrants = new Dictionary<string, EnumBlockAccessFlags>();

	public Dictionary<int, EnumBlockAccessFlags> GroupGrants = new Dictionary<int, EnumBlockAccessFlags>();
}
