#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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
        capi.Event.AfterActiveSlotChanged += delegate (ActiveSlotChangeEventArgs ev)
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
        ClientSettings.Inst.AddKeyCombinationUpdatedWatcher(delegate (string key, KeyCombination value)
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

        int num = (int)GuiElement.scaled(32.0);
        toolModeBgTexture = capi.Gui.Icons.GenTexture(num, num, delegate (Context ctx, ImageSurface surface)
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
        gearTexture = capi.Gui.Icons.GenTexture(size + 10, size + 10, delegate (Context ctx, ImageSurface surface)
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
        ItemSlot itemSlot = capi.World.Player.InventoryManager.GetHotbarInventory()[newSlotIndex];
        SkillItem[] array = itemSlot?.Itemstack?.Collectible?.GetToolModes(itemSlot, capi.World.Player, capi.World.Player.CurrentBlockSelection);
        bool num = array != null && array.Length != 0;
        currentToolModeTexture = null;
        if (num)
        {
            int toolMode = itemSlot.Itemstack.Collectible.GetToolMode(itemSlot, capi.World.Player, capi.World.Player.CurrentBlockSelection);
            if (toolMode < array.Length)
            {
                currentToolModeTexture = array[toolMode].Texture;
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
            float num = 850f;
            dialogBounds = new ElementBounds
            {
                Alignment = EnumDialogArea.CenterBottom,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = num,
                fixedHeight = 80.0
            }.WithFixedAlignmentOffset(0.0, 5.0);
            ElementBounds bounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 10.0, 10.0, 10, 1);
            ElementBounds bounds3 = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 110.0, 10.0, 10, 1);
            ElementBounds bounds4 = ElementStdBounds.SlotGrid(EnumDialogArea.RightFixed, 0.0, 10.0, 4, 1).WithFixedAlignmentOffset(-10.0, 0.0);
            ElementBounds bounds5 = ElementBounds.Fixed(EnumDialogArea.CenterBottom, 0.0, 0.0, 400.0, 0.0).WithFixedAlignmentOffset(0.0, -150.0);
            ElementBounds bounds6 = ElementBounds.Fixed(EnumDialogArea.CenterBottom, 0.0, 0.0, 100.0, 80.0).WithFixedAlignmentOffset(0.0, -80.0);
            double[] array = (double[])GuiStyle.DialogDefaultTextColor.Clone();
            array[0] = (array[0] + 1.0) / 2.0;
            array[1] = (array[1] + 1.0) / 2.0;
            array[2] = (array[2] + 1.0) / 2.0;
            CairoFont font = CairoFont.WhiteSmallText().WithColor(array).WithStroke(GuiStyle.DarkBrownColor, 2.0)
                .WithOrientation(EnumTextOrientation.Center);
            Composers["hotbar"] = capi.Gui.CreateCompo("inventory-hotbar", dialogBounds.FlatCopy().FixedGrow(0.0, 20.0)).BeginChildElements(dialogBounds).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false)
                .AddStaticCustomDraw(ElementBounds.Fill, delegate (Context ctx, ImageSurface surface, ElementBounds bounds)
                {
                    ctx.Rectangle(0.0, 0.0, bounds.OuterWidth, (double)(25f * ClientSettings.GUIScale));
                    ctx.Operator = (Operator)0;
                    ctx.Fill();
                    ctx.Operator = (Operator)2;
                })
                .AddItemSlotGrid(hotbarInv, SendInvPacket, 1, new int[1] { 11 }, bounds2, "offhandgrid")
                .AddItemSlotGrid(hotbarInv, SendInvPacket, 10, new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, bounds3, "hotbargrid")
                .AddItemSlotGrid(backpackInv, SendInvPacket, 4, new int[4] { 0, 1, 2, 3 }, bounds4, "backpackgrid")
                .AddTranspHoverText("", font, 400, bounds5, "iteminfoHover")
                .AddIf(temporalStabilityEnabled)
                .AddHoverText(" ", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), 50, bounds6, "tempStabHoverText")
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
            hotbarSlotGrid.DrawIconHandler = delegate (Context cr, string type, int x, int y, float w, float h, double[] rgba)
            {
                //IL_0035: Unknown result type (might be due to invalid IL or missing references)
                //IL_003a: Unknown result type (might be due to invalid IL or missing references)
                //IL_009f: Unknown result type (might be due to invalid IL or missing references)
                //IL_00a4: Unknown result type (might be due to invalid IL or missing references)
                //IL_007a: Unknown result type (might be due to invalid IL or missing references)
                //IL_007f: Unknown result type (might be due to invalid IL or missing references)
                CairoFont cairoFont = numberFont;
                cairoFont.SetupContext(cr);
                string text = ScreenManager.hotkeyManager.GetHotKeyByCode("hotbarslot" + type).CurrentMapping.ToString();
                TextExtents textExtents = numberFont.GetTextExtents(text);
                double width = ((TextExtents)(ref textExtents)).Width;
                if (width > GuiElement.scaled(20.0))
                {
                    cairoFont = cairoFont.Clone();
                    cairoFont.UnscaledFontsize *= 0.6;
                    cairoFont.SetupContext(cr);
                    textExtents = cairoFont.GetTextExtents(text);
                    width = ((TextExtents)(ref textExtents)).Width;
                }

                double num2 = (double)((float)x + w) - width + 1.0;
                double num3 = y;
                FontExtents fontExtents = cairoFont.GetFontExtents();
                cr.MoveTo(num2, num3 + ((FontExtents)(ref fontExtents)).Ascent - 3.0);
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
            IPlayer player = capi.World.Player;
            if (args.delta != 0 && args.value != prevValue)
            {
                int num = ((!hotbarInv[10].Empty) ? 1 : 0);
                int num2 = ((player.InventoryManager.ActiveHotbarSlotNumber >= 10 + num || capi.Input.KeyboardKeyStateRaw[3]) ? 14 : 10);
                OnKeySlot(GameMath.Mod(player.InventoryManager.ActiveHotbarSlotNumber - args.delta, num2 + num), moveItems: false);
                prevValue = args.value;
                scrollWheeledSlotInFrame = true;
            }
        }
    }

    private bool KeyFlipHandSlots(KeyCombination t1)
    {
        IPlayer player = capi.World.Player;
        Packet_Client packet_Client = (Packet_Client)hotbarInv.TryFlipItems(player.InventoryManager.ActiveHotbarSlotNumber, player.Entity.LeftHandItemSlot);
        if (packet_Client != null)
        {
            capi.Network.SendPacketClient(packet_Client);
        }

        return true;
    }

    private void OnKeySlot(int index, bool moveItems)
    {
        IPlayer player = capi.World.Player;
        if (player.InventoryManager.ActiveHotbarSlot?.Itemstack != null && player.Entity.Controls.HandUse != 0 && index != player.InventoryManager.ActiveHotbarSlotNumber)
        {
            EnumHandInteract handUse = player.Entity.Controls.HandUse;
            if (!player.Entity.TryStopHandAction(forceStop: false, EnumItemUseCancelReason.ChangeSlot))
            {
                return;
            }

            capi.Network.SendHandInteraction(2, player.CurrentBlockSelection, player.CurrentEntitySelection, handUse, 1, firstEvent: false, EnumItemUseCancelReason.ChangeSlot);
        }

        player.InventoryManager.ActiveHotbarSlotNumber = index;
        if (moveItems && player.InventoryManager.CurrentHoveredSlot != null)
        {
            Packet_Client packet_Client = (Packet_Client)hotbarInv.TryFlipItems(index, player.InventoryManager.CurrentHoveredSlot);
            if (packet_Client != null)
            {
                capi.Network.SendPacketClient(packet_Client);
            }
        }
    }

    private void updateHotbarSounds()
    {
        ItemStack itemstack = capi.World.Player.Entity.LeftHandItemSlot.Itemstack;
        updateHotbarSounds(prevLeftStack, itemstack, leftActiveSlotIdleSound);
        prevLeftStack = itemstack;
        ItemStack itemstack2 = capi.World.Player.Entity.RightHandItemSlot.Itemstack;
        updateHotbarSounds(prevRightStack, itemstack2, rightActiveSlotIdleSound);
        prevRightStack = itemstack2;
    }

    private void updateHotbarSounds(ItemStack prevStack, ItemStack nowStack, Dictionary<AssetLocation, ILoadedSound> activeSlotIdleSound)
    {
        if (nowStack != null && prevStack != null && nowStack.Equals(capi.World, prevStack, GlobalConstants.IgnoredStackAttributes))
        {
            return;
        }

        HeldSounds heldSounds = nowStack?.Collectible?.HeldSounds;
        HeldSounds prevSounds = prevStack?.Collectible?.HeldSounds;
        if (prevSounds != null)
        {
            if (prevSounds.Unequip != null)
            {
                capi.World.PlaySoundAt(prevSounds.Unequip, 0.0, 0.0, 0.0, null, 0.9f + (float)capi.World.Rand.NextDouble() * 0.2f);
            }

            if (prevSounds.Idle != null && prevSounds.Idle != heldSounds?.Idle && activeSlotIdleSound.TryGetValue(prevSounds.Idle, out var value))
            {
                value.FadeOut(1f, delegate (ILoadedSound s)
                {
                    s.Stop();
                    s.Dispose();
                    activeSlotIdleSound.Remove(prevSounds.Idle);
                });
            }
        }

        if (heldSounds != null)
        {
            if (heldSounds.Equip != null)
            {
                capi.World.PlaySoundAt(heldSounds.Equip, 0.0, 0.0, 0.0, null, 0.9f + (float)capi.World.Rand.NextDouble() * 0.2f);
            }

            if (heldSounds.Idle != null && !activeSlotIdleSound.ContainsKey(heldSounds.Idle))
            {
                ILoadedSound loadedSound = capi.World.LoadSound(new SoundParams
                {
                    Location = heldSounds.Idle.Clone().WithPathAppendixOnce(".ogg").WithPathPrefixOnce("sounds/"),
                    Pitch = 0.9f + (float)capi.World.Rand.NextDouble() * 0.2f,
                    RelativePosition = true,
                    ShouldLoop = true,
                    SoundType = EnumSoundType.Sound,
                    Volume = 1f
                });
                activeSlotIdleSound[heldSounds.Idle] = loadedSound;
                loadedSound.Start();
                loadedSound.FadeIn(1f, delegate
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
        ItemSlot activeHotbarSlot = player.InventoryManager.ActiveHotbarSlot;
        (player.Entity.AnimManager as PlayerAnimationManager)?.OnActiveSlotChanged(activeHotbarSlot);
        int num = ((!hotbarInv[10].Empty) ? 1 : 0);
        if (newSlot < 10 + num)
        {
            hotbarSlotGrid.HighlightSlot(newSlot);
            backPackGrid.HighlightSlot(-1);
        }
        else
        {
            hotbarSlotGrid.HighlightSlot(-1);
            backPackGrid.HighlightSlot(newSlot - 10 - num);
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
        (activeHotbarSlot.Inventory as InventoryPlayerHotbar)?.updateSlotStatMods(activeHotbarSlot);
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

        IItemStack itemstack = plr.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (itemstack != null && (itemstack.StackSize != lastActiveSlotStacksize || itemstack.Id != lastActiveSlotItemId || itemstack.Class != lastActiveClassItemClass))
        {
            (capi.World as ClientMain).EnqueueMainThreadTask(delegate
            {
                RecomposeActiveSlotHoverText(plr.InventoryManager.ActiveHotbarSlotNumber);
            }, "recomposeslothovertext");
            lastActiveClassItemClass = itemstack.Class;
            lastActiveSlotItemId = itemstack.Id;
            lastActiveSlotStacksize = itemstack.StackSize;
        }
    }

    private void RecomposeActiveSlotHoverText(int newSlotIndex)
    {
        IPlayer player = capi.World.Player;
        int num = ((!hotbarInv[10].Empty) ? 1 : 0);
        ItemSlot itemSlot = ((newSlotIndex < 10 + num) ? player?.InventoryManager?.GetHotbarInventory()[newSlotIndex] : backpackInv[newSlotIndex - 10 - num]);
        if (itemSlot?.Itemstack != null)
        {
            if (prevIndex != newSlotIndex)
            {
                prevIndex = newSlotIndex;
                itemInfoTextActiveMs = capi.ElapsedMilliseconds;
                GuiElementHoverText guiElementHoverText = Composers["hotbar"]?.GetHoverText("iteminfoHover");
                if (guiElementHoverText != null)
                {
                    guiElementHoverText.SetNewText(itemSlot.Itemstack.GetName());
                    guiElementHoverText.SetVisible(on: true);
                }

                if (ClientSettings.ShowBlockInteractionHelp)
                {
                    WorldInteraction[] heldInteractionHelp = itemSlot.Itemstack.Collectible.GetHeldInteractionHelp(itemSlot);
                    wiUtil.ComposeBlockWorldInteractionHelp(heldInteractionHelp);
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

        float num = capi.Render.FrameWidth;
        float num2 = capi.Render.FrameHeight;
        ElementBounds elementBounds = wiUtil.Composer?.Bounds;
        if (elementBounds != null)
        {
            elementBounds.Alignment = EnumDialogArea.None;
            elementBounds.fixedOffsetX = 0.0;
            elementBounds.fixedOffsetY = 0.0;
            elementBounds.absFixedX = (double)(num / 2f) - wiUtil.ActualWidth / 2.0;
            elementBounds.absFixedY = (double)num2 - GuiElement.scaled(95.0) - elementBounds.OuterHeight;
            elementBounds.absMarginX = 0.0;
            elementBounds.absMarginY = 0.0;
        }

        if (capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
        {
            base.OnRenderGUI(deltaTime);
        }

        if (currentToolModeTexture != null)
        {
            float num3 = (float)GuiElement.scaled(38.0);
            float num4 = (float)GuiElement.scaled(200.0);
            float posY = (float)GuiElement.scaled(8.0);
            capi.Render.Render2DTexture(toolModeBgTexture.TextureId, (float)capi.Render.FrameWidth / 2f + num4, posY, num3, num3);
            capi.Render.Render2DTexture(currentToolModeTexture.TextureId, (float)capi.Render.FrameWidth / 2f + num4, posY, num3, num3);
        }

        if (!hotbarInv[10].Empty)
        {
            float num5 = (float)((double)num2 - dialogBounds.OuterHeight + GuiElement.scaled(15.0));
            float x = num / 2f - (float)GuiElement.scaled(361.0);
            float y = num5 + (float)GuiElement.scaled(10.0);
            (hotbarInv[10].Itemstack.Collectible as ISkillItemRenderer)?.Render(deltaTime, x, y, 200f);
            if (capi.World.Player.InventoryManager.ActiveHotbarSlotNumber == 10)
            {
                ElementBounds elementBounds2 = hotbarSlotGrid.SlotBounds[0];
                capi.Render.Render2DTexturePremultipliedAlpha(hotbarSlotGrid.HighlightSlotTexture.TextureId, (int)(elementBounds2.renderX - 2.0 - (double)elementBounds2.OuterWidthInt - 4.0), (int)(elementBounds2.renderY - 2.0), elementBounds2.OuterWidthInt + 4, elementBounds2.OuterHeightInt + 4);
            }
        }
    }

    private void renderGear(float deltaTime)
    {
        float num = capi.Render.FrameWidth;
        float num2 = capi.Render.FrameHeight;
        IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
        currentActiveShader.Stop();
        float num3 = (float)((double)num2 - dialogBounds.OuterHeight + GuiElement.scaled(15.0));
        float y = num3 - (float)gearTexture.Height + (float)gearTexture.Height * 0.45f;
        float num4 = (float)capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability");
        float num5 = (float)capi.World.Player.Entity.Attributes.GetDouble("tempStabChangeVelocity");
        GuiElementHoverText hoverText = Composers["hotbar"].GetHoverText("tempStabHoverText");
        if (hoverText.IsNowShown)
        {
            hoverText.SetNewText((int)GameMath.Clamp(num4 * 100f, 0f, 100f) + "%");
        }

        ShaderProgramGuigear guigear = ShaderPrograms.Guigear;
        guigear.Use();
        guigear.Tex2d2D = gearTexture.TextureId;
        modelMat.Set(capi.Render.CurrentModelviewMatrix);
        modelMat.Translate(num / 2f - (float)(gearTexture.Width / 2) - 1f, y, -200f);
        modelMat.Scale((float)gearTexture.Width / 2f, (float)gearTexture.Height / 2f, 0f);
        float num6 = (float)capi.ElapsedMilliseconds / 1000f;
        modelMat.Translate(1f, 1f, 0f);
        float num7 = (GameMath.Sin(num6 / 50f) * 1f + (GameMath.Sin(num6 / 5f) * 0.5f + GameMath.Sin(num6) * 3f + GameMath.Sin(num6 / 2f) * 1.5f) / 20f) / 2f;
        if ((num4 < 1f && num5 > 0f) || (num4 > 0f && num5 < 0f))
        {
            gearPosition += num5;
        }

        modelMat.RotateZ(num7 / 2f + (float)gearPosition + GlobalConstants.GuiGearRotJitter);
        guigear.GearCounter = num6;
        guigear.StabilityLevel = num4;
        guigear.ShadeYPos = num3;
        guigear.GearHeight = gearTexture.Height;
        guigear.HotbarYPos = num3 + (float)GuiElement.scaled(10.0);
        guigear.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
        guigear.ModelViewMatrix = modelMat.Values;
        capi.Render.RenderMesh(capi.Gui.QuadMeshRef);
        guigear.Stop();
        currentActiveShader.Use();
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
#if false // Decompilation log
'166' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Could not find by name: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
Could not find by name: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
Could not find by name: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
Could not find by name: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Sockets.dll'
------------------
Resolve: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
Could not find by name: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
------------------
Resolve: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\gwlar\.nuget\packages\system.collections.immutable\8.0.0\lib\net7.0\System.Collections.Immutable.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
Could not find by name: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Compression.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Could not find by name: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\0Harmony.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
Could not find by name: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
------------------
Resolve: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Pipes.dll'
------------------
Resolve: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.FileVersionInfo.dll'
------------------
Resolve: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.dll'
------------------
Resolve: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
Could not find by name: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
------------------
Resolve: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
Could not find by name: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
