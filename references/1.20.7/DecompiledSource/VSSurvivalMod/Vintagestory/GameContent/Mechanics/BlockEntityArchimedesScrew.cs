using System;

namespace Vintagestory.GameContent.Mechanics;

public class BlockEntityArchimedesScrew : BlockEntityItemFlow
{
	public override float ItemFlowRate
	{
		get
		{
			BEBehaviorMPArchimedesScrew bh = GetBehavior<BEBehaviorMPArchimedesScrew>();
			if (bh?.Network == null)
			{
				return 0f;
			}
			return Math.Abs(bh.Network.Speed) * itemFlowRate;
		}
	}
}
