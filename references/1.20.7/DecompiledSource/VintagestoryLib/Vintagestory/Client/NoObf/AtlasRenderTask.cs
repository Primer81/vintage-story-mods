using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class AtlasRenderTask
{
	public ItemStack Stack;

	public ITextureAtlasAPI Atlas;

	public int Color;

	public int Size;

	public float Scale = 1f;

	public float SepiaLevel;

	public Action<int> OnComplete;
}
