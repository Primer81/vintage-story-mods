using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods.WorldEdit;

namespace VSCreativeMod;

public class HudWorldEditInputCapture : HudElement
{
	private readonly WorldEditClientHandler _handler;

	private readonly WorldEdit _we;

	private readonly HotKey _toolSelectHotkey;

	private readonly ActionConsumable<KeyCombination> _handlerToolSelect;

	public HudWorldEditInputCapture(ICoreClientAPI capi, WorldEditClientHandler worldEditClientHandler)
		: base(capi)
	{
		_we = capi.ModLoader.GetModSystem<WorldEdit>();
		_handler = worldEditClientHandler;
		_toolSelectHotkey = capi.Input.GetHotKeyByCode("toolmodeselect");
		_handlerToolSelect = _toolSelectHotkey.Handler;
	}

	public bool Toogle(KeyCombination t1)
	{
		WorldEditClientHandler handler = _handler;
		if (handler != null && (handler.ownWorkspace?.ToolInstance?.ScrollEnabled).GetValueOrDefault())
		{
			WorldEditScrollToolMode toolModeSelect = _handler.toolModeSelect;
			if (toolModeSelect != null && toolModeSelect.IsOpened())
			{
				return _handler.toolModeSelect.TryClose();
			}
			return _handler.toolModeSelect?.TryOpen(withFocus: true) ?? false;
		}
		return _handlerToolSelect(t1);
	}

	public override bool TryOpen(bool withFocus)
	{
		_toolSelectHotkey.Handler = Toogle;
		return base.TryOpen(withFocus);
	}

	public override void OnGuiClosed()
	{
		_toolSelectHotkey.Handler = _handlerToolSelect;
		base.OnGuiClosed();
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		base.OnMouseWheel(args);
		if (args.IsHandled)
		{
			return;
		}
		WorldEditClientHandler handler = _handler;
		if (handler == null || !(handler.ownWorkspace?.ToolInstance?.ScrollEnabled).GetValueOrDefault() || !capi.Input.IsHotKeyPressed("ctrl"))
		{
			return;
		}
		WorldEditWorkspace ownWorkspace = _we.clientHandler.ownWorkspace;
		BlockFacing blockFacing = ownWorkspace.GetFacing(capi.World.Player.Entity.Pos);
		char facing = blockFacing.Code[0];
		ownWorkspace.IntValues.TryGetValue("std.stepSize", out var amount);
		EnumWeToolMode? obj = _handler?.ownWorkspace?.ToolInstance.ScrollMode;
		amount = ((args.delta > 0) ? amount : (-1 * amount));
		EnumWeToolMode? enumWeToolMode = obj;
		if (!enumWeToolMode.HasValue)
		{
			return;
		}
		switch (enumWeToolMode.GetValueOrDefault())
		{
		case EnumWeToolMode.Move:
		{
			ToolBase toolBase = _handler?.ownWorkspace?.ToolInstance;
			if (!(toolBase is SelectTool))
			{
				if (toolBase is MoveTool || toolBase is ImportTool)
				{
					capi.SendChatMessage($"/we move {facing} {amount} true");
				}
			}
			else
			{
				capi.SendChatMessage($"/we shift {facing} {amount} true");
			}
			args.SetHandled();
			break;
		}
		case EnumWeToolMode.MoveNear:
			if (_handler?.ownWorkspace?.ToolInstance is SelectTool)
			{
				capi.SendChatMessage($"/we g {blockFacing.Opposite.Code[0]} {-1 * amount} true");
				args.SetHandled();
			}
			break;
		case EnumWeToolMode.MoveFar:
			if (_handler?.ownWorkspace?.ToolInstance is SelectTool)
			{
				capi.SendChatMessage($"/we g {facing} {amount} true");
				args.SetHandled();
			}
			break;
		case EnumWeToolMode.Rotate:
			capi.SendChatMessage($"/we imr {((args.delta > 0) ? 90 : 270)}");
			args.SetHandled();
			break;
		}
	}
}
