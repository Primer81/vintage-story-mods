using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common.CommandAbbr;

public static class CmdUtil
{
	public delegate TextCommandResult EntityEachDelegate(Entity entity);

	public static TextCommandResult EntityEach(TextCommandCallingArgs args, EntityEachDelegate onEntity, int index = 0)
	{
		Entity[] entities = (Entity[])args.Parsers[index].GetValue();
		int successCnt = 0;
		if (entities.Length == 0)
		{
			return TextCommandResult.Error(Lang.Get("No matching player/entity found"), "nonefound");
		}
		TextCommandResult lastResult = null;
		Entity[] array = entities;
		foreach (Entity entity in array)
		{
			lastResult = onEntity(entity);
			if (lastResult.Status == EnumCommandStatus.Success)
			{
				successCnt++;
			}
		}
		if (entities.Length == 1)
		{
			return lastResult;
		}
		return TextCommandResult.Success(Lang.Get("Executed commands on {0}/{1} entities", successCnt, entities.Length));
	}
}
