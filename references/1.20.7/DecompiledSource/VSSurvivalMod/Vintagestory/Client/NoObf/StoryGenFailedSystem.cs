using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Vintagestory.Client.NoObf;

public class StoryGenFailedSystem : ModSystem
{
	private IClientNetworkChannel clientChannel;

	private ICoreClientAPI capi;

	private GuiDialogStoryGenFailed storyGenGui;

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		clientChannel = api.Network.RegisterChannel("StoryGenFailed");
		clientChannel.RegisterMessageType<StoryGenFailed>();
		clientChannel.SetMessageHandler<StoryGenFailed>(OnReceived);
		storyGenGui = new GuiDialogStoryGenFailed(capi);
		capi.Gui.RegisterDialog(storyGenGui);
	}

	private void OnReceived(StoryGenFailed packet)
	{
		storyGenGui.storyGenFailed = packet;
		if (storyGenGui.isInitilized)
		{
			storyGenGui.TryOpen();
		}
	}
}
