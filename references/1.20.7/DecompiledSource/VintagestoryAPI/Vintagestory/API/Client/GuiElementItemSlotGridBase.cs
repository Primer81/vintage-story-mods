using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

/// <summary>
/// A base class for the slot grid.  For all your slot grid needs.
/// </summary>
public abstract class GuiElementItemSlotGridBase : GuiElement
{
	public static double unscaledSlotPadding = 3.0;

	protected IInventory inventory;

	internal OrderedDictionary<int, ItemSlot> availableSlots = new OrderedDictionary<int, ItemSlot>();

	internal OrderedDictionary<int, ItemSlot> renderedSlots = new OrderedDictionary<int, ItemSlot>();

	protected int cols;

	protected int rows;

	protected int prevSlotQuantity;

	private Dictionary<string, int> slotTextureIdsByBgIconAndColor = new Dictionary<string, int>();

	private Dictionary<int, float> slotNotifiedZoomEffect = new Dictionary<int, float>();

	public ElementBounds[] SlotBounds;

	protected ElementBounds[] scissorBounds;

	protected LoadedTexture slotTexture;

	protected LoadedTexture highlightSlotTexture;

	protected LoadedTexture crossedOutTexture;

	protected LoadedTexture[] slotQuantityTextures;

	protected GuiElementStaticText textComposer;

	protected int highlightSlotId = -1;

	protected int hoverSlotId = -1;

	protected string searchText;

	protected Action<object> SendPacketHandler;

	private bool isLastSlotGridInComposite;

	private bool isRightMouseDownStartedInsideElem;

	private bool isLeftMouseDownStartedInsideElem;

	private HashSet<int> wasMouseDownOnSlotIndex = new HashSet<int>();

	private OrderedDictionary<int, int> distributeStacksPrevStackSizeBySlotId = new OrderedDictionary<int, int>();

	private OrderedDictionary<int, int> distributeStacksAddedStackSizeBySlotId = new OrderedDictionary<int, int>();

	private ItemStack referenceDistributStack;

	public CanClickSlotDelegate CanClickSlot;

	private IInventory hoverInv;

	public DrawIconDelegate DrawIconHandler;

	private int tabbedSlotId = -1;

	public bool KeyboardControlEnabled = true;

	public LoadedTexture HighlightSlotTexture => highlightSlotTexture;

	public bool AlwaysRenderIcon { get; set; }

	public override bool Focusable => true;

	/// <summary>
	/// Creates a new instance of this class.
	/// </summary>
	/// <param name="capi">The client API</param>
	/// <param name="inventory">The attached inventory</param>
	/// <param name="sendPacket">A handler that should send supplied network packet to the server, if the inventory modifications should be synced</param>
	/// <param name="columns">The number of columns in the GUI.</param>
	/// <param name="bounds">The bounds of the slot grid.</param>
	public GuiElementItemSlotGridBase(ICoreClientAPI capi, IInventory inventory, Action<object> sendPacket, int columns, ElementBounds bounds)
		: base(capi, bounds)
	{
		slotTexture = new LoadedTexture(capi);
		highlightSlotTexture = new LoadedTexture(capi);
		crossedOutTexture = new LoadedTexture(capi);
		prevSlotQuantity = inventory.Count;
		this.inventory = inventory;
		cols = columns;
		SendPacketHandler = sendPacket;
		inventory.SlotNotified += OnSlotNotified;
		DrawIconHandler = api.Gui.Icons.DrawIconInt;
	}

	private void OnSlotNotified(int slotid)
	{
		slotNotifiedZoomEffect[slotid] = 0.4f;
	}

	public override void ComposeElements(Context unusedCtx, ImageSurface unusedSurface)
	{
		ComposeInteractiveElements();
	}

	private void ComposeInteractiveElements()
	{
		SlotBounds = new ElementBounds[availableSlots.Count];
		scissorBounds = new ElementBounds[availableSlots.Count];
		if (slotQuantityTextures != null)
		{
			Dispose();
		}
		slotQuantityTextures = new LoadedTexture[availableSlots.Count];
		for (int i = 0; i < slotQuantityTextures.Length; i++)
		{
			slotQuantityTextures[i] = new LoadedTexture(api);
		}
		rows = (int)Math.Ceiling(1f * (float)availableSlots.Count / (float)cols);
		Bounds.CalcWorldBounds();
		double unscaledSlotWidth = GuiElementPassiveItemSlot.unscaledSlotSize;
		double unscaledSlotHeight = GuiElementPassiveItemSlot.unscaledSlotSize;
		double absSlotPadding = GuiElement.scaled(unscaledSlotPadding);
		double absSlotWidth = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double absSlotHeight = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, GuiElementPassiveItemSlot.unscaledSlotSize - GuiStyle.SmallishFontSize - 2.0, GuiElementPassiveItemSlot.unscaledSlotSize - 5.0, GuiElementPassiveItemSlot.unscaledSlotSize - 5.0).WithEmptyParent();
		CairoFont font = CairoFont.WhiteSmallText().WithFontSize((float)GuiStyle.SmallishFontSize);
		font.FontWeight = FontWeight.Bold;
		font.Color = new double[4] { 1.0, 1.0, 1.0, 1.0 };
		font.StrokeColor = new double[4] { 0.0, 0.0, 0.0, 1.0 };
		font.StrokeWidth = RuntimeEnv.GUIScale;
		textComposer = new GuiElementStaticText(api, "", EnumTextOrientation.Right, textBounds, font);
		ImageSurface slotSurface = new ImageSurface(Format.Argb32, (int)absSlotWidth, (int)absSlotWidth);
		Context slotCtx = genContext(slotSurface);
		slotCtx.SetSourceRGBA(GuiStyle.DialogSlotBackColor);
		GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, GuiStyle.ElementBGRadius);
		slotCtx.Fill();
		slotCtx.SetSourceRGBA(GuiStyle.DialogSlotFrontColor);
		GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, GuiStyle.ElementBGRadius);
		slotCtx.LineWidth = GuiElement.scaled(4.5);
		slotCtx.Stroke();
		slotSurface.BlurFull(GuiElement.scaled(4.0));
		slotSurface.BlurFull(GuiElement.scaled(4.0));
		GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, 1.0);
		slotCtx.LineWidth = GuiElement.scaled(4.5);
		slotCtx.SetSourceRGBA(0.0, 0.0, 0.0, 0.8);
		slotCtx.Stroke();
		generateTexture(slotSurface, ref slotTexture);
		slotCtx.Dispose();
		slotSurface.Dispose();
		foreach (KeyValuePair<int, ItemSlot> availableSlot in availableSlots)
		{
			ItemSlot slot2 = availableSlot.Value;
			string key = slot2.BackgroundIcon + "-" + slot2.HexBackgroundColor;
			if ((slot2.BackgroundIcon != null || slot2.HexBackgroundColor != null) && !slotTextureIdsByBgIconAndColor.ContainsKey(key))
			{
				slotSurface = new ImageSurface(Format.Argb32, (int)absSlotWidth, (int)absSlotWidth);
				slotCtx = genContext(slotSurface);
				if (slot2.HexBackgroundColor != null)
				{
					double[] bgcolor = ColorUtil.Hex2Doubles(slot2.HexBackgroundColor);
					slotCtx.SetSourceRGBA(bgcolor);
					GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, GuiStyle.ElementBGRadius);
					slotCtx.Fill();
					slotCtx.SetSourceRGBA(bgcolor[0] * 0.25, bgcolor[1] * 0.25, bgcolor[2] * 0.25, 1.0);
					GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, GuiStyle.ElementBGRadius);
					slotCtx.LineWidth = GuiElement.scaled(4.5);
					slotCtx.Stroke();
					slotSurface.BlurFull(GuiElement.scaled(4.0));
					slotSurface.BlurFull(GuiElement.scaled(4.0));
					slotCtx.SetSourceRGBA(0.0, 0.0, 0.0, 0.8);
					GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, 1.0);
					slotCtx.LineWidth = GuiElement.scaled(4.5);
					slotCtx.Stroke();
				}
				else
				{
					slotCtx.SetSourceRGBA(GuiStyle.DialogSlotBackColor);
					GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, GuiStyle.ElementBGRadius);
					slotCtx.Fill();
					slotCtx.SetSourceRGBA(GuiStyle.DialogSlotFrontColor);
					GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, GuiStyle.ElementBGRadius);
					slotCtx.LineWidth = GuiElement.scaled(4.5);
					slotCtx.Stroke();
					slotSurface.BlurFull(GuiElement.scaled(4.0));
					slotSurface.BlurFull(GuiElement.scaled(4.0));
					GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth, absSlotHeight, 1.0);
					slotCtx.LineWidth = GuiElement.scaled(4.5);
					slotCtx.SetSourceRGBA(0.0, 0.0, 0.0, 0.8);
					slotCtx.Stroke();
				}
				if (slot2.BackgroundIcon != null)
				{
					DrawIconHandler?.Invoke(slotCtx, slot2.BackgroundIcon, 2 * (int)absSlotPadding, 2 * (int)absSlotPadding, (int)(absSlotWidth - 4.0 * absSlotPadding), (int)(absSlotHeight - 4.0 * absSlotPadding), new double[4] { 0.0, 0.0, 0.0, 0.2 });
				}
				int texId = 0;
				generateTexture(slotSurface, ref texId);
				slotCtx.Dispose();
				slotSurface.Dispose();
				slotTextureIdsByBgIconAndColor[key] = texId;
			}
		}
		int csize = (int)absSlotWidth - 4;
		slotSurface = new ImageSurface(Format.Argb32, csize, csize);
		slotCtx = genContext(slotSurface);
		slotCtx.SetSourceRGBA(0.0, 0.0, 0.0, 0.8);
		api.Gui.Icons.DrawCross(slotCtx, 4.0, 4.0, 7.0, csize - 18, preserverePath: true);
		slotCtx.SetSourceRGBA(1.0, 0.2, 0.2, 0.8);
		slotCtx.LineWidth = 2.0;
		slotCtx.Stroke();
		generateTexture(slotSurface, ref crossedOutTexture);
		slotCtx.Dispose();
		slotSurface.Dispose();
		slotSurface = new ImageSurface(Format.Argb32, (int)absSlotWidth + 4, (int)absSlotWidth + 4);
		slotCtx = genContext(slotSurface);
		slotCtx.SetSourceRGBA(GuiStyle.ActiveSlotColor);
		GuiElement.RoundRectangle(slotCtx, 0.0, 0.0, absSlotWidth + 4.0, absSlotHeight + 4.0, GuiStyle.ElementBGRadius);
		slotCtx.LineWidth = GuiElement.scaled(9.0);
		slotCtx.StrokePreserve();
		slotSurface.BlurFull(GuiElement.scaled(6.0));
		slotCtx.StrokePreserve();
		slotSurface.BlurFull(GuiElement.scaled(6.0));
		slotCtx.LineWidth = GuiElement.scaled(3.0);
		slotCtx.Stroke();
		slotCtx.LineWidth = GuiElement.scaled(1.0);
		slotCtx.SetSourceRGBA(GuiStyle.ActiveSlotColor);
		slotCtx.Stroke();
		generateTexture(slotSurface, ref highlightSlotTexture);
		slotCtx.Dispose();
		slotSurface.Dispose();
		int slotIndex = 0;
		foreach (KeyValuePair<int, ItemSlot> val in availableSlots)
		{
			int num = slotIndex % cols;
			int row = slotIndex / cols;
			double x = (double)num * (unscaledSlotWidth + unscaledSlotPadding);
			double y = (double)row * (unscaledSlotHeight + unscaledSlotPadding);
			ItemSlot slot = inventory[val.Key];
			SlotBounds[slotIndex] = ElementBounds.Fixed(x, y, unscaledSlotWidth, unscaledSlotHeight).WithParent(Bounds);
			SlotBounds[slotIndex].CalcWorldBounds();
			scissorBounds[slotIndex] = ElementBounds.Fixed(x + 2.0, y + 2.0, unscaledSlotWidth - 4.0, unscaledSlotHeight - 4.0).WithParent(Bounds);
			scissorBounds[slotIndex].CalcWorldBounds();
			ComposeSlotOverlays(slot, val.Key, slotIndex);
			slotIndex++;
		}
	}

	private bool ComposeSlotOverlays(ItemSlot slot, int slotId, int slotIndex)
	{
		if (!availableSlots.ContainsKey(slotId))
		{
			return false;
		}
		if (slot.Itemstack == null)
		{
			return true;
		}
		bool drawItemDamage = slot.Itemstack.Collectible.ShouldDisplayItemDamage(slot.Itemstack);
		if (!drawItemDamage)
		{
			slotQuantityTextures[slotIndex].Dispose();
			slotQuantityTextures[slotIndex] = new LoadedTexture(api);
			return true;
		}
		ImageSurface textSurface = new ImageSurface(Format.Argb32, (int)SlotBounds[slotIndex].InnerWidth, (int)SlotBounds[slotIndex].InnerHeight);
		Context textCtx = genContext(textSurface);
		textCtx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		textCtx.Paint();
		if (drawItemDamage)
		{
			double x = GuiElement.scaled(4.0);
			double y = (double)(int)SlotBounds[slotIndex].InnerHeight - GuiElement.scaled(3.0) - GuiElement.scaled(4.0);
			textCtx.SetSourceRGBA(GuiStyle.DialogStrongBgColor);
			double width = SlotBounds[slotIndex].InnerWidth - GuiElement.scaled(8.0);
			double height = GuiElement.scaled(4.0);
			GuiElement.RoundRectangle(textCtx, x, y, width, height, 1.0);
			textCtx.FillPreserve();
			ShadePath(textCtx);
			float[] color = ColorUtil.ToRGBAFloats(slot.Itemstack.Collectible.GetItemDamageColor(slot.Itemstack));
			textCtx.SetSourceRGB(color[0], color[1], color[2]);
			int dura = slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack);
			width = (double)((float)slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack) / (float)dura) * (SlotBounds[slotIndex].InnerWidth - GuiElement.scaled(8.0));
			GuiElement.RoundRectangle(textCtx, x, y, width, height, 1.0);
			textCtx.FillPreserve();
			ShadePath(textCtx);
		}
		generateTexture(textSurface, ref slotQuantityTextures[slotIndex]);
		textCtx.Dispose();
		textSurface.Dispose();
		return true;
	}

	public override void PostRenderInteractiveElements(float deltaTime)
	{
		if (slotNotifiedZoomEffect.Count > 0)
		{
			foreach (int slotId3 in new List<int>(slotNotifiedZoomEffect.Keys))
			{
				slotNotifiedZoomEffect[slotId3] -= deltaTime;
				if (slotNotifiedZoomEffect[slotId3] <= 0f)
				{
					slotNotifiedZoomEffect.Remove(slotId3);
				}
			}
		}
		if (prevSlotQuantity != inventory.Count)
		{
			prevSlotQuantity = inventory.Count;
			inventory.DirtySlots.Clear();
			ComposeElements(null, null);
		}
		else
		{
			if (inventory.DirtySlots.Count == 0)
			{
				return;
			}
			List<int> handled = new List<int>();
			foreach (int slotId2 in inventory.DirtySlots)
			{
				ItemSlot slot = inventory[slotId2];
				if (ComposeSlotOverlays(slot, slotId2, availableSlots.IndexOfKey(slotId2)))
				{
					handled.Add(slotId2);
				}
			}
			if (!isLastSlotGridInComposite)
			{
				return;
			}
			foreach (int slotId in handled)
			{
				inventory.DirtySlots.Remove(slotId);
			}
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		double num = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double absItemstackSize = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize);
		double offset = num / 2.0;
		_ = InsideClipBounds?.absFixedY ?? 0.0;
		_ = InsideClipBounds?.OuterHeight;
		bool nowInside = false;
		double parentAbsY = Bounds.ParentBounds.absY;
		int i = 0;
		foreach (KeyValuePair<int, ItemSlot> val in renderedSlots)
		{
			ElementBounds bounds = SlotBounds[i];
			_ = bounds.absFixedY;
			_ = bounds.OuterHeight;
			if (bounds.absY + bounds.absInnerHeight < parentAbsY)
			{
				i++;
				continue;
			}
			if (bounds.PartiallyInside(Bounds.ParentBounds))
			{
				nowInside = true;
				ItemSlot slot = val.Value;
				int slotId = val.Key;
				if (((slot.Itemstack == null || AlwaysRenderIcon) && slot.BackgroundIcon != null) || slot.HexBackgroundColor != null)
				{
					string key = slot.BackgroundIcon + "-" + slot.HexBackgroundColor;
					if (slotTextureIdsByBgIconAndColor.ContainsKey(key))
					{
						api.Render.Render2DTexturePremultipliedAlpha(slotTextureIdsByBgIconAndColor[key], bounds);
					}
					else
					{
						api.Render.Render2DTexturePremultipliedAlpha(slotTexture.TextureId, bounds);
					}
				}
				else
				{
					api.Render.Render2DTexturePremultipliedAlpha(slotTexture.TextureId, bounds);
				}
				if (highlightSlotId == slotId || hoverSlotId == slotId || distributeStacksPrevStackSizeBySlotId.ContainsKey(slotId))
				{
					api.Render.Render2DTexturePremultipliedAlpha(highlightSlotTexture.TextureId, (int)(bounds.renderX - 2.0), (int)(bounds.renderY - 2.0), bounds.OuterWidthInt + 4, bounds.OuterHeightInt + 4);
				}
				if (slot.Itemstack == null)
				{
					i++;
					continue;
				}
				float dx = 0f;
				float dy = 0f;
				if (slotNotifiedZoomEffect.ContainsKey(slotId))
				{
					dx = 4f * (float)api.World.Rand.NextDouble() - 2f;
					dy = 4f * (float)api.World.Rand.NextDouble() - 2f;
				}
				api.Render.PushScissor(scissorBounds[i], stacking: true);
				api.Render.RenderItemstackToGui(slot, SlotBounds[i].renderX + offset + (double)dy, SlotBounds[i].renderY + offset + (double)dx, 90.0, (float)absItemstackSize, -1, deltaTime);
				api.Render.PopScissor();
				if (slot.DrawUnavailable)
				{
					api.Render.Render2DTexturePremultipliedAlpha(crossedOutTexture.TextureId, (int)bounds.renderX, (int)bounds.renderY, crossedOutTexture.Width, crossedOutTexture.Height, 250f);
				}
				if (slotQuantityTextures[i].TextureId != 0)
				{
					api.Render.Render2DTexturePremultipliedAlpha(slotQuantityTextures[i].TextureId, SlotBounds[i]);
				}
			}
			else if (nowInside)
			{
				break;
			}
			i++;
		}
	}

	public void OnGuiClosed(ICoreClientAPI api)
	{
		if (hoverSlotId != -1 && inventory[hoverSlotId] != null)
		{
			api.Input.TriggerOnMouseLeaveSlot(inventory[hoverSlotId]);
		}
		hoverSlotId = -1;
		tabbedSlotId = -1;
		(inventory as InventoryBase).InvNetworkUtil.PauseInventoryUpdates = false;
		api.World.Player.InventoryManager.MouseItemSlot.Inventory.InvNetworkUtil.PauseInventoryUpdates = false;
	}

	public override int OutlineColor()
	{
		return -16711936;
	}

	/// <summary>
	/// Renders only a subset of all available slots filtered by searching given text on the item name/description
	/// </summary>
	/// <param name="text"></param>
	/// <param name="searchCache">Can be set to increase search performance, otherwise a slow search is performed</param>
	/// <param name="searchCacheNames"></param>
	public void FilterItemsBySearchText(string text, Dictionary<int, string> searchCache = null, Dictionary<int, string> searchCacheNames = null)
	{
		searchText = text.ToSearchFriendly().ToLowerInvariant();
		renderedSlots.Clear();
		OrderedDictionary<int, WeightedSlot> wSlots = new OrderedDictionary<int, WeightedSlot>();
		foreach (KeyValuePair<int, ItemSlot> val in availableSlots)
		{
			ItemSlot slot = inventory[val.Key];
			if (slot.Itemstack == null)
			{
				continue;
			}
			if (searchText == null || searchText.Length == 0)
			{
				renderedSlots.Add(val.Key, slot);
			}
			else
			{
				if (searchCacheNames == null)
				{
					continue;
				}
				string cachedtext = "";
				string name = searchCacheNames[val.Key];
				if (searchCache != null && searchCache.TryGetValue(val.Key, out cachedtext))
				{
					int index = name.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase);
					if (index == 0 && name.Length == searchText.Length)
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 0f
						});
					}
					else if (index == 0 && name.Length > searchText.Length && name[searchText.Length] == ' ')
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 0.125f
						});
					}
					else if (index > 0 && name[index - 1] == ' ' && index + searchText.Length == name.Length)
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 0.25f
						});
					}
					else if (index > 0 && name[index - 1] == ' ')
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 0.5f
						});
					}
					else if (index == 0)
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 0.75f
						});
					}
					else if (index > 0)
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 1f
						});
					}
					else if (cachedtext.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase))
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 2f
						});
					}
					else if (cachedtext.CaseInsensitiveContains(searchText, StringComparison.InvariantCultureIgnoreCase))
					{
						wSlots.Add(val.Key, new WeightedSlot
						{
							slot = slot,
							weight = 3f
						});
					}
				}
				else if (slot.Itemstack.MatchesSearchText(api.World, searchText))
				{
					renderedSlots.Add(val.Key, slot);
				}
			}
		}
		foreach (KeyValuePair<int, WeightedSlot> pair2 in wSlots.OrderBy((KeyValuePair<int, WeightedSlot> pair) => pair.Value.weight))
		{
			renderedSlots.Add(pair2.Key, pair2.Value.slot);
		}
		rows = (int)Math.Ceiling(1f * (float)renderedSlots.Count / (float)cols);
		ComposeInteractiveElements();
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		base.OnMouseWheel(api, args);
		if ((!api.Input.KeyboardKeyState[3] && !api.Input.KeyboardKeyState[4]) || !KeyboardControlEnabled || !IsPositionInside(api.Input.MouseX, api.Input.MouseY))
		{
			return;
		}
		for (int i = 0; i < SlotBounds.Length && i < renderedSlots.Count; i++)
		{
			if (SlotBounds[i].PointInside(api.Input.MouseX, api.Input.MouseY))
			{
				SlotMouseWheel(renderedSlots.GetKeyAtIndex(i), args.delta);
				args.SetHandled();
			}
		}
	}

	private void SlotMouseWheel(int slotId, int wheelDelta)
	{
		ItemStackMoveOperation op = new ItemStackMoveOperation(api.World, EnumMouseButton.Wheel, (EnumModifierKey)0, EnumMergePriority.AutoMerge, 1);
		op.WheelDir = ((wheelDelta > 0) ? 1 : (-1));
		op.ActingPlayer = api.World.Player;
		IInventory ownInventory = api.World.Player.InventoryManager.GetOwnInventory("mouse");
		IInventory targetInv = inventory;
		ItemSlot sourceSlot = ownInventory[0];
		object packet = targetInv.ActivateSlot(slotId, sourceSlot, ref op);
		if (packet == null)
		{
			return;
		}
		if (packet is object[] packets)
		{
			for (int i = 0; i < packets.Length; i++)
			{
				SendPacketHandler(packets[i]);
			}
		}
		else
		{
			SendPacketHandler?.Invoke(packet);
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		base.OnKeyDown(api, args);
		if (base.HasFocus && KeyboardControlEnabled)
		{
			if (args.KeyCode == 45)
			{
				tabbedSlotId = Math.Max(-1, tabbedSlotId - cols);
				highlightSlotId = ((tabbedSlotId >= 0) ? renderedSlots.GetKeyAtIndex(tabbedSlotId) : (-1));
			}
			if (args.KeyCode == 46)
			{
				tabbedSlotId = Math.Min(renderedSlots.Count - 1, tabbedSlotId + cols);
				highlightSlotId = renderedSlots.GetKeyAtIndex(tabbedSlotId);
			}
			if (args.KeyCode == 48)
			{
				tabbedSlotId = Math.Min(renderedSlots.Count - 1, tabbedSlotId + 1);
				highlightSlotId = renderedSlots.GetKeyAtIndex(tabbedSlotId);
			}
			if (args.KeyCode == 47)
			{
				tabbedSlotId = Math.Max(-1, tabbedSlotId - 1);
				highlightSlotId = ((tabbedSlotId >= 0) ? renderedSlots.GetKeyAtIndex(tabbedSlotId) : (-1));
			}
			if (args.KeyCode == 49 && highlightSlotId >= 0)
			{
				SlotClick(api, highlightSlotId, EnumMouseButton.Left, shiftPressed: true, ctrlPressed: false, altPressed: false);
			}
		}
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent mouse)
	{
		base.OnMouseDown(api, mouse);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		wasMouseDownOnSlotIndex.Clear();
		distributeStacksPrevStackSizeBySlotId.Clear();
		distributeStacksAddedStackSizeBySlotId.Clear();
		for (int i = 0; i < SlotBounds.Length && i < renderedSlots.Count; i++)
		{
			if (!SlotBounds[i].PointInside(args.X, args.Y))
			{
				continue;
			}
			CanClickSlotDelegate canClickSlot = CanClickSlot;
			if (canClickSlot == null || canClickSlot(i))
			{
				isRightMouseDownStartedInsideElem = args.Button == EnumMouseButton.Right && api.World.Player.InventoryManager.MouseItemSlot.Itemstack != null;
				isLeftMouseDownStartedInsideElem = args.Button == EnumMouseButton.Left && api.World.Player.InventoryManager.MouseItemSlot.Itemstack != null;
				wasMouseDownOnSlotIndex.Add(i);
				int slotid = renderedSlots.GetKeyAtIndex(i);
				int prevStackSize = inventory[slotid].StackSize;
				if (isLeftMouseDownStartedInsideElem)
				{
					referenceDistributStack = api.World.Player.InventoryManager.MouseItemSlot.Itemstack.Clone();
					distributeStacksPrevStackSizeBySlotId.Add(slotid, inventory[slotid].StackSize);
				}
				SlotClick(api, renderedSlots.GetKeyAtIndex(i), args.Button, api.Input.KeyboardKeyState[1] || api.Input.KeyboardKeyState[2], api.Input.KeyboardKeyState[3], api.Input.KeyboardKeyState[5]);
				(inventory as InventoryBase).InvNetworkUtil.PauseInventoryUpdates = isLeftMouseDownStartedInsideElem;
				api.World.Player.InventoryManager.MouseItemSlot.Inventory.InvNetworkUtil.PauseInventoryUpdates = isLeftMouseDownStartedInsideElem;
				distributeStacksAddedStackSizeBySlotId[slotid] = inventory[slotid].StackSize - prevStackSize;
				args.Handled = true;
			}
			break;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		isRightMouseDownStartedInsideElem = false;
		isLeftMouseDownStartedInsideElem = false;
		wasMouseDownOnSlotIndex.Clear();
		distributeStacksPrevStackSizeBySlotId.Clear();
		distributeStacksAddedStackSizeBySlotId.Clear();
		(inventory as InventoryBase).InvNetworkUtil.PauseInventoryUpdates = false;
		api.World.Player.InventoryManager.MouseItemSlot.Inventory.InvNetworkUtil.PauseInventoryUpdates = false;
		base.OnMouseUp(api, args);
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			if (hoverSlotId != -1)
			{
				api.Input.TriggerOnMouseLeaveSlot(inventory[hoverSlotId]);
			}
			hoverSlotId = -1;
			return;
		}
		for (int i = 0; i < SlotBounds.Length && i < renderedSlots.Count; i++)
		{
			if (!SlotBounds[i].PointInside(args.X, args.Y))
			{
				continue;
			}
			int nowHoverSlotid = renderedSlots.GetKeyAtIndex(i);
			ItemSlot nowHoverSlot = inventory[nowHoverSlotid];
			ItemStack stack = nowHoverSlot.Itemstack;
			if (isRightMouseDownStartedInsideElem && !wasMouseDownOnSlotIndex.Contains(i))
			{
				wasMouseDownOnSlotIndex.Add(i);
				if (stack == null || stack.Equals(api.World, api.World.Player.InventoryManager.MouseItemSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
				{
					SlotClick(api, nowHoverSlotid, EnumMouseButton.Right, api.Input.KeyboardKeyState[1], api.Input.KeyboardKeyState[3], api.Input.KeyboardKeyState[5]);
				}
			}
			if (isLeftMouseDownStartedInsideElem && !wasMouseDownOnSlotIndex.Contains(i) && (stack == null || stack.Equals(api.World, referenceDistributStack, GlobalConstants.IgnoredStackAttributes)))
			{
				wasMouseDownOnSlotIndex.Add(i);
				distributeStacksPrevStackSizeBySlotId.Add(nowHoverSlotid, nowHoverSlot.StackSize);
				if (api.World.Player.InventoryManager.MouseItemSlot.StackSize > 0)
				{
					SlotClick(api, nowHoverSlotid, EnumMouseButton.Left, api.Input.KeyboardKeyState[1], api.Input.KeyboardKeyState[3], api.Input.KeyboardKeyState[5]);
				}
				if (api.World.Player.InventoryManager.MouseItemSlot.StackSize <= 0)
				{
					RedistributeStacks(nowHoverSlotid);
				}
			}
			if (nowHoverSlotid != hoverSlotId && nowHoverSlot != null)
			{
				api.Input.TriggerOnMouseEnterSlot(nowHoverSlot);
				hoverInv = nowHoverSlot.Inventory;
			}
			if (nowHoverSlotid != hoverSlotId)
			{
				tabbedSlotId = -1;
			}
			hoverSlotId = nowHoverSlotid;
			return;
		}
		if (hoverSlotId != -1)
		{
			api.Input.TriggerOnMouseLeaveSlot(inventory[hoverSlotId]);
		}
		hoverSlotId = -1;
	}

	public override bool OnMouseLeaveSlot(ICoreClientAPI api, ItemSlot slot)
	{
		if (slot.Inventory == hoverInv)
		{
			hoverSlotId = -1;
		}
		return false;
	}

	private void RedistributeStacks(int intoSlotId)
	{
		int stacksPerSlot = referenceDistributStack.StackSize / distributeStacksPrevStackSizeBySlotId.Count;
		for (int i = 0; i < distributeStacksPrevStackSizeBySlotId.Count - 1; i++)
		{
			int sourceSlotid = distributeStacksPrevStackSizeBySlotId.GetKeyAtIndex(i);
			if (sourceSlotid == intoSlotId)
			{
				continue;
			}
			ItemSlot sourceSlot = inventory[sourceSlotid];
			distributeStacksAddedStackSizeBySlotId.TryGetValue(sourceSlotid, out var addedSrcSize);
			if (addedSrcSize > stacksPerSlot)
			{
				int beforeSrcSize = distributeStacksPrevStackSizeBySlotId[sourceSlotid];
				int nowSrcSize = beforeSrcSize + addedSrcSize;
				ItemSlot targetSlot = inventory[intoSlotId];
				ItemStackMoveOperation op = new ItemStackMoveOperation(api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge);
				op.ActingPlayer = api.World.Player;
				op.RequestedQuantity = nowSrcSize - beforeSrcSize - stacksPerSlot;
				object packet = api.World.Player.InventoryManager.TryTransferTo(sourceSlot, targetSlot, ref op);
				distributeStacksAddedStackSizeBySlotId.TryGetValue(intoSlotId, out var addedBefore);
				distributeStacksAddedStackSizeBySlotId[intoSlotId] = addedBefore + op.MovedQuantity;
				distributeStacksAddedStackSizeBySlotId[sourceSlotid] -= op.MovedQuantity;
				if (packet != null)
				{
					SendPacketHandler(packet);
				}
			}
		}
	}

	public virtual void SlotClick(ICoreClientAPI api, int slotId, EnumMouseButton mouseButton, bool shiftPressed, bool ctrlPressed, bool altPressed)
	{
		_ = api.World.Player.InventoryManager.OpenedInventories;
		IInventory mouseCursorInv = api.World.Player.InventoryManager.GetOwnInventory("mouse");
		EnumModifierKey modifiers = (shiftPressed ? EnumModifierKey.SHIFT : ((EnumModifierKey)0)) | (ctrlPressed ? EnumModifierKey.CTRL : ((EnumModifierKey)0)) | (altPressed ? EnumModifierKey.ALT : ((EnumModifierKey)0));
		ItemStackMoveOperation op = new ItemStackMoveOperation(api.World, mouseButton, modifiers, EnumMergePriority.AutoMerge);
		op.ActingPlayer = api.World.Player;
		object packet;
		if (shiftPressed)
		{
			ItemSlot sourceSlot = inventory[slotId];
			op.RequestedQuantity = sourceSlot.StackSize;
			packet = inventory.ActivateSlot(slotId, sourceSlot, ref op);
		}
		else
		{
			op.CurrentPriority = EnumMergePriority.DirectMerge;
			bool wasEmpty = mouseCursorInv.Empty;
			CollectibleObject wasCollObj = mouseCursorInv[0].Itemstack?.Collectible;
			packet = inventory.ActivateSlot(slotId, mouseCursorInv[0], ref op);
			if (wasEmpty && !mouseCursorInv.Empty)
			{
				api.World.PlaySoundAt(mouseCursorInv[0].Itemstack.Collectible?.HeldSounds?.InvPickup ?? HeldSounds.InvPickUpDefault, 0.0, 0.0, 0.0, null, EnumSoundType.Sound, 1f);
			}
			else if ((!wasEmpty && mouseCursorInv.Empty) || wasCollObj?.Id != mouseCursorInv[0].Itemstack?.Collectible?.Id)
			{
				api.World.PlaySoundAt(wasCollObj?.HeldSounds?.InvPlace ?? HeldSounds.InvPlaceDefault, 0.0, 0.0, 0.0, null, EnumSoundType.Sound, 1f);
			}
		}
		if (packet != null)
		{
			if (packet is object[] packets)
			{
				for (int i = 0; i < packets.Length; i++)
				{
					SendPacketHandler(packets[i]);
				}
			}
			else
			{
				SendPacketHandler?.Invoke(packet);
			}
		}
		api.Input.TriggerOnMouseClickSlot(inventory[slotId]);
	}

	/// <summary>
	/// Highlights a specific slot.
	/// </summary>
	/// <param name="slotId">The slot to highlight.</param>
	public void HighlightSlot(int slotId)
	{
		highlightSlotId = slotId;
	}

	/// <summary>
	/// Removes the active slot highlight.
	/// </summary>
	public void RemoveSlotHighlight()
	{
		highlightSlotId = -1;
	}

	internal static void UpdateLastSlotGridFlag(GuiComposer composer)
	{
		Dictionary<IInventory, GuiElementItemSlotGridBase> lastelembyInventory = new Dictionary<IInventory, GuiElementItemSlotGridBase>();
		foreach (GuiElement elem in composer.interactiveElements.Values)
		{
			if (elem is GuiElementItemSlotGridBase)
			{
				GuiElementItemSlotGridBase slotgridelem = elem as GuiElementItemSlotGridBase;
				slotgridelem.isLastSlotGridInComposite = false;
				lastelembyInventory[slotgridelem.inventory] = slotgridelem;
			}
		}
		foreach (GuiElementItemSlotGridBase value in lastelembyInventory.Values)
		{
			value.isLastSlotGridInComposite = true;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		int i = 0;
		while (slotQuantityTextures != null && i < slotQuantityTextures.Length)
		{
			slotQuantityTextures[i]?.Dispose();
			i++;
		}
		slotTexture.Dispose();
		highlightSlotTexture.Dispose();
		crossedOutTexture?.Dispose();
	}
}
