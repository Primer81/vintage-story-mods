using System;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class JsonExport : ModSystem
{
	private ICoreServerAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		api.ChatCommands.GetOrCreate("dev").BeginSubCommand("jsonexport").WithDescription("Export items and blocks as json files")
			.RequiresPrivilege(Privilege.controlserver)
			.HandleWith(CmdExport)
			.EndSubCommand();
		this.api = api;
	}

	private TextCommandResult CmdExport(TextCommandCallingArgs textCommandCallingArgs)
	{
		StringBuilder sql = new StringBuilder();
		sql.Append("[");
		int cnt = 0;
		for (int j = 0; j < api.World.Blocks.Count; j++)
		{
			Block block = api.World.Blocks[j];
			if (block != null && !(block.Code == null))
			{
				if (cnt > 0)
				{
					sql.Append(",");
				}
				sql.Append("{");
				sql.Append($"\"name\": \"{new ItemStack(block).GetName()}\", ");
				sql.Append($"\"code\": \"{block.Code}\", ");
				sql.Append($"\"material\": \"{block.BlockMaterial}\", ");
				sql.Append($"\"shape\": \"{block.Shape.Base.Path}\", ");
				sql.Append($"\"tool\": \"{block.Tool}\"");
				sql.Append("}");
				cnt++;
			}
		}
		sql.Append("]");
		File.WriteAllText("blocks.json", sql.ToString());
		sql = new StringBuilder();
		sql.Append("[");
		cnt = 0;
		for (int i = 0; i < api.World.Items.Count; i++)
		{
			Item item = api.World.Items[i];
			if (item != null && !(item.Code == null))
			{
				if (cnt > 0)
				{
					sql.Append(",");
				}
				sql.Append("{");
				sql.Append($"\"name\": \"{new ItemStack(item).GetName()}\", ");
				sql.Append($"\"code\": \"{item.Code}\", ");
				sql.Append($"\"shape\": \"{item.Shape?.Base?.Path}\", ");
				sql.Append($"\"tool\": \"{item.Tool}\"");
				sql.Append("}");
				cnt++;
			}
		}
		sql.Append("]");
		File.WriteAllText("items.json", sql.ToString());
		return TextCommandResult.Success("All Blocks and Items written to block.json and item.json in " + AppDomain.CurrentDomain.BaseDirectory);
	}
}
