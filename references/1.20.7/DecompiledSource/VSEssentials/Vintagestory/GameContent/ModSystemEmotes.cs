using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemEmotes : ModSystem
{
	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		IChatCommandApi chatCommands = api.ChatCommands;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		chatCommands.Create("emote").RequiresPrivilege(Privilege.chat).WithDescription("Execute an emote on your player")
			.WithArgs(parsers.OptionalWord("emote"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				EntityAgent entityAgent = args.Caller.Entity as EntityAgent;
				string[] array = entityAgent.Properties.Attributes["emotes"].AsArray<string>();
				string text = (string)args[0];
				if (text == null || !array.Contains(text))
				{
					return TextCommandResult.Error(Lang.Get("Choose emote: {0}", string.Join(", ", array)));
				}
				if (text != "shakehead" && !entityAgent.RightHandItemSlot.Empty && entityAgent.RightHandItemSlot.Itemstack.Collectible.GetHeldTpIdleAnimation(entityAgent.RightHandItemSlot, entityAgent, EnumHand.Right) != null)
				{
					return TextCommandResult.Error("Only with free hands");
				}
				api.Network.BroadcastEntityPacket(entityAgent.EntityId, 197, SerializerUtil.Serialize(text));
				return TextCommandResult.Success();
			});
	}
}
