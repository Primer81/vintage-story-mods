using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

/// <summary>
/// A custom itemstack render handler. This method is called after Collectible.OnBeforeRender(). For render target gui, the gui shader and its uniforms are already fully prepared, you may only call RenderMesh() and ignore the modelMat, position and size values - stack sizes however, are not covered by this.
/// </summary>
/// <param name="inSlot">The slot in which the itemstack resides in</param>
/// <param name="renderInfo">The render info for this stack, you can choose to ignore these values</param>
/// <param name="modelMat">The model transformation matrix with position and size already preapplied, you can choose to ignore this value</param>
/// <param name="posX">The center x-position where the stack has to be rendered</param>
/// <param name="posY">The center y-position where the stack has to be rendered</param>
/// <param name="posZ">The depth position. Higher values might be required for very large models, which can cause them to poke through dialogs in front of them, however</param>
/// <param name="size">The size of the stack that has to be rendered</param>
/// <param name="color">The requested color, usually always white</param>
/// <param name="rotate">Whether or not to rotate it (some parts of the game have this on or off)</param>
/// <param name="showStackSize">Whether or not to show the stack size (some parts of the game have this on or off)</param>
public delegate void ItemRenderDelegate(ItemSlot inSlot, ItemRenderInfo renderInfo, Matrixf modelMat, double posX, double posY, double posZ, float size, int color, bool rotate = false, bool showStackSize = true);
