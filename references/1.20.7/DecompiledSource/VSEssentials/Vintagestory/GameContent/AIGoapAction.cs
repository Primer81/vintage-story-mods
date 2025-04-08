using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class AIGoapAction
{
	private Dictionary<string, GoapCondition> conditions = new Dictionary<string, GoapCondition>();

	private Dictionary<string, GoapCondition> effect = new Dictionary<string, GoapCondition>();

	public virtual float Cost => 1f;
}
