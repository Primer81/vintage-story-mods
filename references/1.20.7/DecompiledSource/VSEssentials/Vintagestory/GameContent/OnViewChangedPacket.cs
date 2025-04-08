using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class OnViewChangedPacket
{
	public List<Vec2i> NowVisible = new List<Vec2i>();

	public List<Vec2i> NowHidden = new List<Vec2i>();
}
