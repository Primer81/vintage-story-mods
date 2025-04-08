using System;
using System.Runtime;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class SystemHotkeys : ClientSystem
{
	public override string Name => "ho";

	public SystemHotkeys(ClientMain game)
		: base(game)
	{
	}

	public override void OnBlockTexturesLoaded()
	{
		HotkeyManager hotkeyManager = ScreenManager.hotkeyManager;
		hotkeyManager.SetHotKeyHandler("decspeed", KeyNormalSpeed);
		hotkeyManager.SetHotKeyHandler("incspeed", KeyFastSpeed);
		hotkeyManager.SetHotKeyHandler("decspeedfrac", KeyNormalSpeed);
		hotkeyManager.SetHotKeyHandler("incspeedfrac", KeyFastSpeed);
		hotkeyManager.SetHotKeyHandler("cycleflymodes", KeyCycleFlyModes);
		hotkeyManager.SetHotKeyHandler("fly", KeyToggleFly);
		hotkeyManager.SetHotKeyHandler("dropitem", KeyDropItem);
		hotkeyManager.SetHotKeyHandler("dropitems", KeyDropItems);
		hotkeyManager.SetHotKeyHandler("pickblock", KeyPickBlock);
		hotkeyManager.SetHotKeyHandler("reloadshaders", KeyReloadShaders);
		hotkeyManager.SetHotKeyHandler("reloadtextures", KeyReloadTextures);
		hotkeyManager.SetHotKeyHandler("togglehud", KeyToggleHUD);
		hotkeyManager.SetHotKeyHandler("compactheap", KeyCompactHeap);
		hotkeyManager.SetHotKeyHandler("rendermetablocks", KeyRenderMetaBlocks);
		hotkeyManager.SetHotKeyHandler("primarymouse", OnPrimaryMouseButton);
		hotkeyManager.SetHotKeyHandler("secondarymouse", OnSecondaryMouseButton);
		hotkeyManager.SetHotKeyHandler("middlemouse", OnMiddleMouseButton);
		game.api.RegisterLinkProtocol("hotkey", hotKeyLinkClicked);
	}

	private bool OnPrimaryMouseButton(KeyCombination mb)
	{
		return game.UpdateMouseButtonState(EnumMouseButton.Left, !mb.OnKeyUp);
	}

	private bool OnSecondaryMouseButton(KeyCombination mb)
	{
		return game.UpdateMouseButtonState(EnumMouseButton.Right, !mb.OnKeyUp);
	}

	private bool OnMiddleMouseButton(KeyCombination mb)
	{
		return game.UpdateMouseButtonState(EnumMouseButton.Middle, !mb.OnKeyUp);
	}

	private void hotKeyLinkClicked(LinkTextComponent comp)
	{
		string hotkey = comp.Href.Substring("hotkey://".Length);
		if (ScreenManager.hotkeyManager.HotKeys.TryGetValue(hotkey, out var hk))
		{
			hk.Handler(hk.CurrentMapping);
		}
	}

	private bool KeyRenderMetaBlocks(KeyCombination t1)
	{
		ClientSettings.RenderMetaBlocks = !ClientSettings.RenderMetaBlocks;
		game.ShowChatMessage("Render meta blocks now " + (ClientSettings.RenderMetaBlocks ? "on" : "off"));
		return true;
	}

	private bool KeyNormalSpeed(KeyCombination viaKeyComb)
	{
		float size = (viaKeyComb.Shift ? 0.1f : 1f);
		float speed = (float)Math.Max(size, Math.Round(10f * (game.player.worlddata.MoveSpeedMultiplier - size)) / 10.0);
		game.player.worlddata.SetMode(game, speed);
		game.ShowChatMessage($"Movespeed: {speed}");
		return true;
	}

	private bool KeyFastSpeed(KeyCombination viaKeyComb)
	{
		float size = (viaKeyComb.Shift ? 0.1f : 1f);
		float speed = (float)Math.Max(size, Math.Round(10f * (game.player.worlddata.MoveSpeedMultiplier + size)) / 10.0);
		game.player.worlddata.SetMode(game, speed);
		game.ShowChatMessage($"Movespeed: {speed}");
		return true;
	}

	private bool KeyToggleFly(KeyCombination t1)
	{
		if ((game.player.worlddata.CurrentGameMode != EnumGameMode.Creative && game.player.worlddata.CurrentGameMode != EnumGameMode.Spectator) || !game.AllowFreemove)
		{
			return false;
		}
		game.EntityPlayer.Pos.Motion.Set(0.0, 0.0, 0.0);
		if (!game.player.worlddata.FreeMove)
		{
			game.player.worlddata.RequestModeFreeMove(game, freeMove: true);
		}
		else
		{
			game.player.worlddata.RequestMode(game, noClip: false, freeMove: false);
		}
		return true;
	}

	private bool KeyCycleFlyModes(KeyCombination viaKeyComb)
	{
		if ((game.player.worlddata.CurrentGameMode != EnumGameMode.Creative && game.player.worlddata.CurrentGameMode != EnumGameMode.Spectator) || !game.AllowFreemove)
		{
			return false;
		}
		game.EntityPlayer.Pos.Motion.Set(0.0, 0.0, 0.0);
		if (!game.player.worlddata.FreeMove)
		{
			game.player.worlddata.RequestModeFreeMove(game, freeMove: true);
			game.ShowChatMessage(Lang.Get("Fly mode on"));
		}
		else if (game.player.worlddata.FreeMove && !game.player.worlddata.NoClip)
		{
			game.player.worlddata.RequestModeNoClip(game, noClip: true);
			game.ShowChatMessage(Lang.Get("Fly mode + noclip on"));
		}
		else if (game.player.worlddata.FreeMove && game.player.worlddata.NoClip)
		{
			game.player.worlddata.RequestMode(game, noClip: false, freeMove: false);
			game.ShowChatMessage(Lang.Get("Fly mode off, noclip off"));
		}
		return true;
	}

	private bool KeyDropItem(KeyCombination viaKeyComb)
	{
		ItemSlot slot = game.player.inventoryMgr.currentHoveredSlot;
		if (slot == null)
		{
			slot = game.player.inventoryMgr.ActiveHotbarSlot;
		}
		if (game.player.inventoryMgr.DropItem(slot, fullStack: false))
		{
			game.PlaySound(new AssetLocation("sounds/player/quickthrow"), randomizePitch: true);
		}
		return true;
	}

	private bool KeyDropItems(KeyCombination viaKeyComb)
	{
		ItemSlot slot = game.player.inventoryMgr.currentHoveredSlot;
		if (slot == null)
		{
			slot = game.player.inventoryMgr.ActiveHotbarSlot;
		}
		if (game.player.inventoryMgr.DropItem(slot, fullStack: true))
		{
			game.PlaySound(new AssetLocation("sounds/player/quickthrow"), randomizePitch: true);
		}
		return true;
	}

	private bool KeyPickBlock(KeyCombination viaKeyComb)
	{
		game.PickBlock = !viaKeyComb.OnKeyUp;
		return true;
	}

	private bool KeyReloadShaders(KeyCombination viaKeyComb)
	{
		bool ok = ShaderRegistry.ReloadShaders();
		bool ok2 = game.eventManager != null && game.eventManager.TriggerReloadShaders();
		game.Logger.Notification("Shaders reloaded.");
		ok = ok && ok2;
		game.ShowChatMessage("Shaders reloaded" + (ok ? "" : ". errors occured, please check client log"));
		return true;
	}

	private bool KeyReloadTextures(KeyCombination viaKeyComb)
	{
		game.AssetManager.Reload(AssetCategory.textures);
		game.ReloadTextures();
		game.ShowChatMessage("Textures reloaded");
		return true;
	}

	private bool KeyToggleHUD(KeyCombination viaKeyComb)
	{
		game.ShouldRender2DOverlays = !game.ShouldRender2DOverlays;
		return true;
	}

	private bool KeyCompactHeap(KeyCombination viaKeyComb)
	{
		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();
		game.ShowChatMessage("Compacted large object heap");
		return true;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
