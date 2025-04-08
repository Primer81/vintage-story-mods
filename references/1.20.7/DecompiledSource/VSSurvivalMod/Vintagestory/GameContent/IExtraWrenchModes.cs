using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IExtraWrenchModes
{
	SkillItem[] GetExtraWrenchModes(IPlayer byPlayer, BlockSelection blockSelection);

	void OnWrenchInteract(IPlayer player, BlockSelection blockSel, int mode, int v);
}
