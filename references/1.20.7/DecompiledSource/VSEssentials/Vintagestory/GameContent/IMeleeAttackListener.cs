using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public interface IMeleeAttackListener
{
	void DidAttack(Entity targetEntity);
}
