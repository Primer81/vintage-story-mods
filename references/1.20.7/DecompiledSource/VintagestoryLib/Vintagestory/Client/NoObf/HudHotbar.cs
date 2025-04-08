using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class HudHotbar : HudElement
{
	private IInventory hotbarInv;

	private IInventory backpackInv;

	private GuiElementItemSlotGrid hotbarSlotGrid;

	private GuiElementItemSlotGrid backPackGrid;

	private long itemInfoTextActiveMs;

	private int lastActiveSlotStacksize = 99999;

	private int lastActiveSlotItemId;

	private EnumItemClass lastActiveClassItemClass;

	private bool scrollWheeledSlotInFrame;

	private bool shouldRecompose;

	private LoadedTexture currentToolModeTexture;

	private LoadedTexture toolModeBgTexture;

	private DrawWorldInteractionUtil wiUtil;

	private Matrixf modelMat = new Matrixf();

	private LoadedTexture gearTexture;

	private Dictionary<AssetLocation, ILoadedSound> leftActiveSlotIdleSound = new Dictionary<AssetLocation, ILoadedSound>();

	private Dictionary<AssetLocation, ILoadedSound> rightActiveSlotIdleSound = new Dictionary<AssetLocation, ILoadedSound>();

	private ItemStack prevLeftStack;

	private ItemStack prevRightStack;

	private ElementBounds dialogBounds;

	private bool temporalStabilityEnabled;

	private int prevValue = int.MaxValue;

	private int prevIndex = -1;

	private double gearPosition;

	public override double InputOrder => 1.0;

	public override string ToggleKeyCombinationCode => null;

	public HudHotbar(ICoreClientAPI capi)
		: base(capi)
	{
		wiUtil = new DrawWorldInteractionUtil(capi, Composers, "-heldItem");
		wiUtil.UnscaledLineHeight = 25.0;
		wiUtil.FontSize = 16f;
		capi.Event.RegisterGameTickListener(OnGameTick, 20);
		capi.Event.AfterActiveSlotChanged += delegate(ActiveSlotChangeEventArgs ev)
		{
			OnActiveSlotChanged(ev.ToSlot);
		};
		capi.Event.RegisterGameTickListener(OnCheckToolMode, 100);
	}

	public override void OnBlockTexturesLoaded()
	{
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot1", delegate
		{
			OnKeySlot(0, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot2", delegate
		{
			OnKeySlot(1, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot3", delegate
		{
			OnKeySlot(2, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot4", delegate
		{
			OnKeySlot(3, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot5", delegate
		{
			OnKeySlot(4, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot6", delegate
		{
			OnKeySlot(5, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot7", delegate
		{
			OnKeySlot(6, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot8", delegate
		{
			OnKeySlot(7, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot9", delegate
		{
			OnKeySlot(8, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot10", delegate
		{
			OnKeySlot(9, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot11", delegate
		{
			OnKeySlot(10, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot12", delegate
		{
			OnKeySlot(11, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot13", delegate
		{
			OnKeySlot(12, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("hotbarslot14", delegate
		{
			OnKeySlot(13, moveItems: true);
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("fliphandslots", KeyFlipHandSlots);
		ClientSettings.Inst.AddKeyCombinationUpdatedWatcher(delegate(string key, KeyCombination value)
		{
			if (key.StartsWithOrdinal("hotbarslot"))
			{
				shouldRecompose = true;
			}
		});
		temporalStabilityEnabled = capi.World.Config.GetBool("temporalStability", defaultValue: true);
		if (temporalStabilityEnabled)
		{
			ClientSettings.Inst.AddWatcher<float>("guiScale", delegate
			{
				genGearTexture();
			});
			genGearTexture();
		}
		int size = (int)GuiElement.scaled(32.0);
		toolModeBgTexture = capi.Gui.Icons.GenTexture(size, size, delegate(Context ctx, ImageSurface surface)
		{
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
			GuiElement.RoundRectangle(ctx, GuiElement.scaled(2.0), GuiElement.scaled(2.0), GuiElement.scaled(28.0), GuiElement.scaled(28.0), 1.0);
			ctx.Fill();
		});
	}

	private void genGearTexture()
	{
		int size = (int)Math.Ceiling(85f * ClientSettings.GUIScale);
		gearTexture?.Dispose();
		gearTexture = capi.Gui.Icons.GenTexture(size + 10, size + 10, delegate(Context ctx, ImageSurface surface)
		{
			capi.Gui.Icons.DrawVSGear(ctx, surface, 5, 5, size, size, new double[4] { 0.2, 0.2, 0.2, 1.0 });
		});
	}

	private void OnGameTick(float dt)
	{
		if (itemInfoTextActiveMs > 0 && capi.ElapsedMilliseconds - itemInfoTextActiveMs > 3500)
		{
			itemInfoTextActiveMs = 0L;
			Composers["hotbar"].GetHoverText("iteminfoHover").SetVisible(on: false);
			wiUtil.ComposeBlockWorldInteractionHelp(new WorldInteraction[0]);
		}
	}

	private void OnCheckToolMode(float dt)
	{
		UpdateCurrentToolMode(capi.World.Player.InventoryManager.ActiveHotbarSlotNumber);
	}

	private void UpdateCurrentToolMode(int newSlotIndex)
	{
		ItemSlot slot = capi.World.Player.InventoryManager.GetHotbarInventory()[newSlotIndex];
		SkillItem[] toolModes = slot?.Itemstack?.Collectible?.GetToolModes(slot, capi.World.Player, capi.World.Player.CurrentBlockSelection);
		bool num = toolModes != null && toolModes.Length != 0;
		currentToolModeTexture = null;
		if (num)
		{
			int curModeIndex = slot.Itemstack.Collectible.GetToolMode(slot, capi.World.Player, capi.World.Player.CurrentBlockSelection);
			if (curModeIndex < toolModes.Length)
			{
				currentToolModeTexture = toolModes[curModeIndex].Texture;
			}
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		ComposeGuis();
	}

	public void ComposeGuis()
	{
		hotbarInv = capi.World.Player.InventoryManager.GetOwnInventory("hotbar");
		backpackInv = capi.World.Player.InventoryManager.GetOwnInventory("backpack");
		_ = GuiStyle.ElementToDialogPadding;
		if (hotbarInv != null)
		{
			hotbarInv.Open(capi.World.Player);
			backpackInv.Open(capi.World.Player);
			hotbarInv.SlotModified += OnHotbarSlotModified;
			float width = 850f;
			dialogBounds = new ElementBounds
			{
				Alignment = EnumDialogArea.CenterBottom,
				BothSizing = ElementSizing.Fixed,
				fixedWidth = width,
				fixedHeight = 80.0
			}.WithFixedAlignmentOffset(0.0, 5.0);
			ElementBounds offhandBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 10.0, 10.0, 10, 1);
			ElementBounds hotBarBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 110.0, 10.0, 10, 1);
			ElementBounds backpackBounds = ElementStdBounds.SlotGrid(EnumDialogArea.RightFixed, 0.0, 10.0, 4, 1).WithFixedAlignmentOffset(-10.0, 0.0);
			ElementBounds iteminfoBounds = ElementBounds.Fixed(EnumDialogArea.CenterBottom, 0.0, 0.0, 400.0, 0.0).WithFixedAlignmentOffset(0.0, -150.0);
			ElementBounds gearBounds = ElementBounds.Fixed(EnumDialogArea.CenterBottom, 0.0, 0.0, 100.0, 80.0).WithFixedAlignmentOffset(0.0, -80.0);
			double[] color = (double[])GuiStyle.DialogDefaultTextColor.Clone();
			color[0] = (color[0] + 1.0) / 2.0;
			color[1] = (color[1] + 1.0) / 2.0;
			color[2] = (color[2] + 1.0) / 2.0;
			CairoFont hoverfont = CairoFont.WhiteSmallText().WithColor(color).WithStroke(GuiStyle.DarkBrownColor, 2.0)
				.WithOrientation(EnumTextOrientation.Center);
			Composers["hotbar"] = capi.Gui.CreateCompo("inventory-hotbar", dialogBounds.FlatCopy().FixedGrow(0.0, 20.0)).BeginChildElements(dialogBounds).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false)
				.AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
				{
					ctx.Rectangle(0.0, 0.0, bounds.OuterWidth, 25f * ClientSettings.GUIScale);
					ctx.Operator = Operator.Clear;
					ctx.Fill();
					ctx.Operator = Operator.Over;
				})
				.AddItemSlotGrid(hotbarInv, SendInvPacket, 1, new int[1] { 11 }, offhandBounds, "offhandgrid")
				.AddItemSlotGrid(hotbarInv, SendInvPacket, 10, new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, hotBarBounds, "hotbargrid")
				.AddItemSlotGrid(backpackInv, SendInvPacket, 4, new int[4] { 0, 1, 2, 3 }, backpackBounds, "backpackgrid")
				.AddTranspHoverText("", hoverfont, 400, iteminfoBounds, "iteminfoHover")
				.AddIf(temporalStabilityEnabled)
				.AddHoverText(" ", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), 50, gearBounds, "tempStabHoverText")
				.EndIf()
				.EndChildElements();
			(Composers["hotbar"].GetElement("element-2") as GuiElementDialogBackground).FullBlur = true;
			Composers["hotbar"].Tabbable = false;
			hotbarSlotGrid = Composers["hotbar"].GetSlotGrid("hotbargrid");
			hotbarSlotGrid.KeyboardControlEnabled = false;
			Composers["hotbar"].GetSlotGrid("offhandgrid").KeyboardControlEnabled = false;
			Composers["hotbar"].GetSlotGrid("backpackgrid").KeyboardControlEnabled = false;
			CairoFont numberFont = CairoFont.WhiteDetailText();
			numberFont.Color = GuiStyle.HotbarNumberTextColor;
			hotbarSlotGrid.AlwaysRenderIcon = true;
			hotbarSlotGrid.DrawIconHandler = delegate(Context cr, string type, int x, int y, float w, float h, double[] rgba)
			{
				CairoFont cairoFont = numberFont;
				cairoFont.SetupContext(cr);
				string text = ScreenManager.hotkeyManager.GetHotKeyByCode("hotbarslot" + type).CurrentMapping.ToString();
				double width2 = numberFont.GetTextExtents(text).Width;
				if (width2 > GuiElement.scaled(20.0))
				{
					cairoFont = cairoFont.Clone();
					cairoFont.UnscaledFontsize *= 0.6;
					cairoFont.SetupContext(cr);
					width2 = cairoFont.GetTextExtents(text).Width;
				}
				cr.MoveTo((double)((float)x + w) - width2 + 1.0, (double)y + cairoFont.GetFontExtents().Ascent - 3.0);
				cr.ShowText(text);
			};
			Composers["hotbar"].Compose();
			backPackGrid = Composers["hotbar"].GetSlotGrid("backpackgrid");
			OnActiveSlotChanged(capi.World.Player.InventoryManager.ActiveHotbarSlotNumber, triggerUpdate: false);
			GuiElementHoverText hoverText = Composers["hotbar"].GetHoverText("iteminfoHover");
			hoverText.ZPosition = 50f;
			hoverText.SetFollowMouse(on: false);
			hoverText.SetAutoWidth(on: true);
			hoverText.SetAutoDisplay(on: false);
			hoverText.fillBounds = true;
			TryOpen();
		}
		else
		{
			ScreenManager.Platform.Logger.Notification("Server did not send a hotbar inventory, so I won't display one");
		}
	}

	private void SendInvPacket(object packet)
	{
		capi.Network.SendPacketClient(packet);
	}

	public override bool TryClose()
	{
		return false;
	}

	public override bool ShouldReceiveKeyboardEvents()
	{
		return true;
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		args.SetHandled();
		if (!scrollWheeledSlotInFrame)
		{
			IPlayer plr = capi.World.Player;
			if (args.delta != 0 && args.value != prevValue)
			{
				int skoffset = ((!hotbarInv[10].Empty) ? 1 : 0);
				int max = ((plr.InventoryManager.ActiveHotbarSlotNumber >= 10 + skoffset || capi.Input.KeyboardKeyStateRaw[3]) ? 14 : 10);
				OnKeySlot(GameMath.Mod(plr.InventoryManager.ActiveHotbarSlotNumber - args.delta, max + skoffset), moveItems: false);
				prevValue = args.value;
				scrollWheeledSlotInFrame = true;
			}
		}
	}

	private bool KeyFlipHandSlots(KeyCombination t1)
	{
		IPlayer plr = capi.World.Player;
		Packet_Client packet = (Packet_Client)hotbarInv.TryFlipItems(plr.InventoryManager.ActiveHotbarSlotNumber, plr.Entity.LeftHandItemSlot);
		if (packet != null)
		{
			capi.Network.SendPacketClient(packet);
		}
		return true;
	}

	private void OnKeySlot(int index, bool moveItems)
	{
		IPlayer plr = capi.World.Player;
		if (plr.InventoryManager.ActiveHotbarSlot?.Itemstack != null && plr.Entity.Controls.HandUse != 0 && index != plr.InventoryManager.ActiveHotbarSlotNumber)
		{
			EnumHandInteract beforeUseType = plr.Entity.Controls.HandUse;
			if (!plr.Entity.TryStopHandAction(forceStop: false, EnumItemUseCancelReason.ChangeSlot))
			{
				return;
			}
			capi.Network.SendHandInteraction(2, plr.CurrentBlockSelection, plr.CurrentEntitySelection, beforeUseType, 1, firstEvent: false, EnumItemUseCancelReason.ChangeSlot);
		}
		plr.InventoryManager.ActiveHotbarSlotNumber = index;
		if (moveItems && plr.InventoryManager.CurrentHoveredSlot != null)
		{
			Packet_Client packet = (Packet_Client)hotbarInv.TryFlipItems(index, plr.InventoryManager.CurrentHoveredSlot);
			if (packet != null)
			{
				capi.Network.SendPacketClient(packet);
			}
		}
	}

	private void updateHotbarSounds()
	{
		ItemStack nowLeftStack = capi.World.Player.Entity.LeftHandItemSlot.Itemstack;
		updateHotbarSounds(prevLeftStack, nowLeftStack, leftActiveSlotIdleSound);
		prevLeftStack = nowLeftStack;
		ItemStack nowRightStack = capi.World.Player.Entity.RightHandItemSlot.Itemstack;
		updateHotbarSounds(prevRightStack, nowRightStack, rightActiveSlotIdleSound);
		prevRightStack = nowRightStack;
	}

	private void updateHotbarSounds(ItemStack prevStack, ItemStack nowStack, Dictionary<AssetLocation, ILoadedSound> activeSlotIdleSound)
	{
		if (nowStack != null && prevStack != null && nowStack.Equals(capi.World, prevStack, GlobalConstants.IgnoredStackAttributes))
		{
			return;
		}
		HeldSounds nowSounds = nowStack?.Collectible?.HeldSounds;
		HeldSounds prevSounds = prevStack?.Collectible?.HeldSounds;
		if (prevSounds != null)
		{
			if (prevSounds.Unequip != null)
			{
				capi.World.PlaySoundAt(prevSounds.Unequip, 0.0, 0.0, 0.0, null, 0.9f + (float)capi.World.Rand.NextDouble() * 0.2f);
			}
			if (prevSounds.Idle != null && prevSounds.Idle != nowSounds?.Idle && activeSlotIdleSound.TryGetValue(prevSounds.Idle, out var sound2))
			{
				sound2.FadeOut(1f, delegate(ILoadedSound s)
				{
					s.Stop();
					s.Dispose();
					activeSlotIdleSound.Remove(prevSounds.Idle);
				});
			}
		}
		if (nowSounds != null)
		{
			if (nowSounds.Equip != null)
			{
				capi.World.PlaySoundAt(nowSounds.Equip, 0.0, 0.0, 0.0, null, 0.9f + (float)capi.World.Rand.NextDouble() * 0.2f);
			}
			if (nowSounds.Idle != null && !activeSlotIdleSound.ContainsKey(nowSounds.Idle))
			{
				ILoadedSound sound = capi.World.LoadSound(new SoundParams
				{
					Location = nowSounds.Idle.Clone().WithPathAppendixOnce(".ogg").WithPathPrefixOnce("sounds/"),
					Pitch = 0.9f + (float)capi.World.Rand.NextDouble() * 0.2f,
					RelativePosition = true,
					ShouldLoop = true,
					SoundType = EnumSoundType.Sound,
					Volume = 1f
				});
				activeSlotIdleSound[nowSounds.Idle] = sound;
				sound.Start();
				sound.FadeIn(1f, delegate
				{
				});
			}
		}
	}

	private void OnActiveSlotChanged(int newSlot, bool triggerUpdate = true)
	{
		if (hotbarSlotGrid == null)
		{
			return;
		}
		IClientPlayer player = capi.World.Player;
		ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
		(player.Entity.AnimManager as PlayerAnimationManager)?.OnActiveSlotChanged(slot);
		int skoffset = ((!hotbarInv[10].Empty) ? 1 : 0);
		if (newSlot < 10 + skoffset)
		{
			hotbarSlotGrid.HighlightSlot(newSlot);
			backPackGrid.HighlightSlot(-1);
		}
		else
		{
			hotbarSlotGrid.HighlightSlot(-1);
			backPackGrid.HighlightSlot(newSlot - 10 - skoffset);
		}
		if (triggerUpdate)
		{
			(capi.World as ClientMain).EnqueueMainThreadTask(delegate
			{
				RecomposeActiveSlotHoverText(newSlot);
			}, "recomposeslothovertext");
			UpdateCurrentToolMode(newSlot);
		}
		updateHotbarSounds();
		(slot.Inventory as InventoryPlayerHotbar)?.updateSlotStatMods(slot);
	}

	private void OnHotbarSlotModified(int slotId)
	{
		UpdateCurrentToolMode(slotId);
		updateHotbarSounds();
		IPlayer plr = capi.World.Player;
		if (slotId != plr.InventoryManager.ActiveHotbarSlotNumber)
		{
			return;
		}
		IItemStack stack = plr.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (stack != null && (stack.StackSize != lastActiveSlotStacksize || stack.Id != lastActiveSlotItemId || stack.Class != lastActiveClassItemClass))
		{
			(capi.World as ClientMain).EnqueueMainThreadTask(delegate
			{
				RecomposeActiveSlotHoverText(plr.InventoryManager.ActiveHotbarSlotNumber);
			}, "recomposeslothovertext");
			lastActiveClassItemClass = stack.Class;
			lastActiveSlotItemId = stack.Id;
			lastActiveSlotStacksize = stack.StackSize;
		}
	}

	private void RecomposeActiveSlotHoverText(int newSlotIndex)
	{
		IPlayer plr = capi.World.Player;
		int skoffset = ((!hotbarInv[10].Empty) ? 1 : 0);
		ItemSlot activeSlot = ((newSlotIndex < 10 + skoffset) ? plr?.InventoryManager?.GetHotbarInventory()[newSlotIndex] : backpackInv[newSlotIndex - 10 - skoffset]);
		if (activeSlot?.Itemstack != null)
		{
			if (prevIndex != newSlotIndex)
			{
				prevIndex = newSlotIndex;
				itemInfoTextActiveMs = capi.ElapsedMilliseconds;
				GuiElementHoverText elem = Composers["hotbar"]?.GetHoverText("iteminfoHover");
				if (elem != null)
				{
					elem.SetNewText(activeSlot.Itemstack.GetName());
					elem.SetVisible(on: true);
				}
				if (ClientSettings.ShowBlockInteractionHelp)
				{
					WorldInteraction[] wis = activeSlot.Itemstack.Collectible.GetHeldInteractionHelp(activeSlot);
					wiUtil.ComposeBlockWorldInteractionHelp(wis);
				}
			}
		}
		else
		{
			Composers["hotbar"].GetHoverText("iteminfoHover").SetVisible(on: false);
			wiUtil.ComposeBlockWorldInteractionHelp(new WorldInteraction[0]);
			prevIndex = newSlotIndex;
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (shouldRecompose)
		{
			ComposeGuis();
			shouldRecompose = false;
		}
		if (temporalStabilityEnabled && capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			renderGear(deltaTime);
		}
		float screenWidth = capi.Render.FrameWidth;
		float screenHeight = capi.Render.FrameHeight;
		ElementBounds bounds = wiUtil.Composer?.Bounds;
		if (bounds != null)
		{
			bounds.Alignment = EnumDialogArea.None;
			bounds.fixedOffsetX = 0.0;
			bounds.fixedOffsetY = 0.0;
			bounds.absFixedX = (double)(screenWidth / 2f) - wiUtil.ActualWidth / 2.0;
			bounds.absFixedY = (double)screenHeight - GuiElement.scaled(95.0) - bounds.OuterHeight;
			bounds.absMarginX = 0.0;
			bounds.absMarginY = 0.0;
		}
		if (capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			base.OnRenderGUI(deltaTime);
		}
		if (currentToolModeTexture != null)
		{
			float size = (float)GuiElement.scaled(38.0);
			float x = (float)GuiElement.scaled(200.0);
			float y = (float)GuiElement.scaled(8.0);
			capi.Render.Render2DTexture(toolModeBgTexture.TextureId, (float)capi.Render.FrameWidth / 2f + x, y, size, size);
			capi.Render.Render2DTexture(currentToolModeTexture.TextureId, (float)capi.Render.FrameWidth / 2f + x, y, size, size);
		}
		if (!hotbarInv[10].Empty)
		{
			float num = (float)((double)screenHeight - dialogBounds.OuterHeight + GuiElement.scaled(15.0));
			float posX = screenWidth / 2f - (float)GuiElement.scaled(361.0);
			float posY = num + (float)GuiElement.scaled(10.0);
			(hotbarInv[10].Itemstack.Collectible as ISkillItemRenderer)?.Render(deltaTime, posX, posY, 200f);
			if (capi.World.Player.InventoryManager.ActiveHotbarSlotNumber == 10)
			{
				ElementBounds sbounds = hotbarSlotGrid.SlotBounds[0];
				capi.Render.Render2DTexturePremultipliedAlpha(hotbarSlotGrid.HighlightSlotTexture.TextureId, (int)(sbounds.renderX - 2.0 - (double)sbounds.OuterWidthInt - 4.0), (int)(sbounds.renderY - 2.0), sbounds.OuterWidthInt + 4, sbounds.OuterHeightInt + 4);
			}
		}
	}

	private void renderGear(float deltaTime)
	{
		float screenWidth = capi.Render.FrameWidth;
		float num = capi.Render.FrameHeight;
		IShaderProgram prevProg = capi.Render.CurrentActiveShader;
		prevProg.Stop();
		float yposhotbartop = (float)((double)num - dialogBounds.OuterHeight + GuiElement.scaled(15.0));
		float yposgeartop = yposhotbartop - (float)gearTexture.Height + (float)gearTexture.Height * 0.45f;
		float stabilityLevel = (float)capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability");
		float velocity = (float)capi.World.Player.Entity.Attributes.GetDouble("tempStabChangeVelocity");
		GuiElementHoverText elem = Composers["hotbar"].GetHoverText("tempStabHoverText");
		if (elem.IsNowShown)
		{
			elem.SetNewText((int)GameMath.Clamp(stabilityLevel * 100f, 0f, 100f) + "%");
		}
		ShaderProgramGuigear guigear = ShaderPrograms.Guigear;
		guigear.Use();
		guigear.Tex2d2D = gearTexture.TextureId;
		modelMat.Set(capi.Render.CurrentModelviewMatrix);
		modelMat.Translate(screenWidth / 2f - (float)(gearTexture.Width / 2) - 1f, yposgeartop, -200f);
		modelMat.Scale((float)gearTexture.Width / 2f, (float)gearTexture.Height / 2f, 0f);
		float f = (float)capi.ElapsedMilliseconds / 1000f;
		modelMat.Translate(1f, 1f, 0f);
		float rndMotion = (GameMath.Sin(f / 50f) * 1f + (GameMath.Sin(f / 5f) * 0.5f + GameMath.Sin(f) * 3f + GameMath.Sin(f / 2f) * 1.5f) / 20f) / 2f;
		if ((stabilityLevel < 1f && velocity > 0f) || (stabilityLevel > 0f && velocity < 0f))
		{
			gearPosition += velocity;
		}
		modelMat.RotateZ(rndMotion / 2f + (float)gearPosition + GlobalConstants.GuiGearRotJitter);
		guigear.GearCounter = f;
		guigear.StabilityLevel = stabilityLevel;
		guigear.ShadeYPos = yposhotbartop;
		guigear.GearHeight = gearTexture.Height;
		guigear.HotbarYPos = yposhotbartop + (float)GuiElement.scaled(10.0);
		guigear.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
		guigear.ModelViewMatrix = modelMat.Values;
		capi.Render.RenderMesh(capi.Gui.QuadMeshRef);
		guigear.Stop();
		prevProg.Use();
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
		scrollWheeledSlotInFrame = false;
	}

	public override void Dispose()
	{
		base.Dispose();
		gearTexture?.Dispose();
		toolModeBgTexture?.Dispose();
		wiUtil?.Dispose();
	}
}
