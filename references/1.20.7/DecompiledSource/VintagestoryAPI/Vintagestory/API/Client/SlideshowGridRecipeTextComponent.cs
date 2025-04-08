using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

/// <summary>
/// Draws multiple itemstacks
/// </summary>
public class SlideshowGridRecipeTextComponent : ItemstackComponentBase
{
	public GridRecipeAndUnnamedIngredients[] GridRecipesAndUnIn;

	private Action<ItemStack> onStackClicked;

	private ItemSlot dummyslot = new DummySlot();

	private double size;

	private float secondsVisible = 1f;

	private int curItemIndex;

	private Dictionary<AssetLocation, ItemStack[]> resolveCache = new Dictionary<AssetLocation, ItemStack[]>();

	private Dictionary<int, LoadedTexture> extraTexts = new Dictionary<int, LoadedTexture>();

	private LoadedTexture hoverTexture;

	public GridRecipeAndUnnamedIngredients CurrentVisibleRecipe;

	private static int[][,] variantDisplaySequence = new int[30][,];

	private int secondCounter;

	/// <summary>
	/// Flips through given array of grid recipes every second
	/// </summary>
	/// <param name="capi"></param>
	/// <param name="gridrecipes"></param>
	/// <param name="size"></param>
	/// <param name="floatType"></param>
	/// <param name="onStackClicked"></param>
	/// <param name="allStacks">If set, will resolve wildcards based on this list, otherwise will search all available blocks/items</param>
	public SlideshowGridRecipeTextComponent(ICoreClientAPI capi, GridRecipe[] gridrecipes, double size, EnumFloat floatType, Action<ItemStack> onStackClicked = null, ItemStack[] allStacks = null)
		: base(capi)
	{
		SlideshowGridRecipeTextComponent slideshowGridRecipeTextComponent = this;
		size = GuiElement.scaled(size);
		this.onStackClicked = onStackClicked;
		Float = floatType;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, 3.0 * (size + 3.0), 3.0 * (size + 3.0))
		};
		this.size = size;
		Random fixedRand = new Random(123);
		for (int j = 0; j < 30; j++)
		{
			int[,] sq = (variantDisplaySequence[j] = new int[3, 3]);
			for (int x = 0; x < 3; x++)
			{
				for (int y = 0; y < 3; y++)
				{
					sq[x, y] = capi.World.Rand.Next(99999);
				}
			}
		}
		List<GridRecipeAndUnnamedIngredients> resolvedGridRecipes = new List<GridRecipeAndUnnamedIngredients>();
		Queue<GridRecipe> halfResolvedRecipes = new Queue<GridRecipe>(gridrecipes);
		bool allResolved = false;
		while (!allResolved)
		{
			allResolved = true;
			int cnt = halfResolvedRecipes.Count;
			while (cnt-- > 0)
			{
				GridRecipe toTestRecipe = halfResolvedRecipes.Dequeue();
				Dictionary<int, ItemStack[]> unnamedIngredients = null;
				bool thisResolved = true;
				for (int k = 0; k < toTestRecipe.resolvedIngredients.Length; k++)
				{
					CraftingRecipeIngredient ingred = toTestRecipe.resolvedIngredients[k];
					if (ingred == null || !ingred.IsWildCard)
					{
						continue;
					}
					allResolved = false;
					thisResolved = false;
					ItemStack[] stacks = ResolveWildCard(capi.World, ingred, allStacks);
					if (stacks.Length == 0)
					{
						resolveCache.Remove(ingred.Code);
						stacks = ResolveWildCard(capi.World, ingred);
						if (stacks.Length == 0)
						{
							throw new ArgumentException(string.Concat("Attempted to resolve the recipe ingredient wildcard ", ingred.Type.ToString(), " ", ingred.Code, " but there are no such items/blocks!"));
						}
					}
					if (ingred.Name == null)
					{
						if (unnamedIngredients == null)
						{
							unnamedIngredients = new Dictionary<int, ItemStack[]>();
						}
						unnamedIngredients[k] = ((ItemStack[])stacks.Clone()).Shuffle(fixedRand);
						thisResolved = true;
						continue;
					}
					for (int l = 0; l < stacks.Length; l++)
					{
						GridRecipe cloned = toTestRecipe.Clone();
						for (int m = 0; m < cloned.resolvedIngredients.Length; m++)
						{
							CraftingRecipeIngredient clonedingred = cloned.resolvedIngredients[m];
							if (clonedingred != null && clonedingred.Code.Equals(ingred.Code))
							{
								clonedingred.Code = stacks[l].Collectible.Code;
								clonedingred.IsWildCard = false;
								clonedingred.ResolvedItemstack = stacks[l];
							}
						}
						halfResolvedRecipes.Enqueue(cloned);
					}
					break;
				}
				if (thisResolved)
				{
					resolvedGridRecipes.Add(new GridRecipeAndUnnamedIngredients
					{
						Recipe = toTestRecipe,
						unnamedIngredients = unnamedIngredients
					});
				}
			}
		}
		resolveCache.Clear();
		GridRecipesAndUnIn = resolvedGridRecipes.ToArray();
		GridRecipesAndUnIn.Shuffle(fixedRand);
		bool extraline = false;
		for (int i = 0; i < GridRecipesAndUnIn.Length; i++)
		{
			string trait = GridRecipesAndUnIn[i].Recipe.RequiresTrait;
			if (trait != null)
			{
				extraTexts[i] = capi.Gui.TextTexture.GenTextTexture(Lang.Get("gridrecipe-requirestrait", Lang.Get("traitname-" + trait)), CairoFont.WhiteDetailText());
				if (!extraline)
				{
					BoundsPerLine[0].Height += GuiElement.scaled(20.0);
				}
				extraline = true;
			}
		}
		if (GridRecipesAndUnIn.Length == 0)
		{
			throw new ArgumentException("Could not resolve any of the supplied grid recipes?");
		}
		genHover();
		stackInfo.onRenderStack = delegate
		{
			GridRecipeAndUnnamedIngredients gridRecipeAndUnnamedIngredients = slideshowGridRecipeTextComponent.GridRecipesAndUnIn[slideshowGridRecipeTextComponent.curItemIndex];
			double num = (int)GuiElement.scaled(30.0 + GuiElementItemstackInfo.ItemStackSize / 2.0);
			ItemSlot curSlot = slideshowGridRecipeTextComponent.stackInfo.curSlot;
			int stackSize = curSlot.StackSize;
			curSlot.Itemstack.StackSize = 1;
			curSlot.Itemstack.Collectible.OnHandbookRecipeRender(capi, gridRecipeAndUnnamedIngredients.Recipe, curSlot, (double)(int)slideshowGridRecipeTextComponent.stackInfo.Bounds.renderX + num, (double)(int)slideshowGridRecipeTextComponent.stackInfo.Bounds.renderY + num + (double)(int)GuiElement.scaled(GuiElementItemstackInfo.MarginTop), 1000.0 + GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize) * 2.0, (float)GuiElement.scaled(GuiElementItemstackInfo.ItemStackSize) * 1f / 0.58f);
			curSlot.Itemstack.StackSize = stackSize;
		};
	}

	private void genHover()
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
		Context context = new Context(surface);
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.6);
		context.Paint();
		hoverTexture = new LoadedTexture(capi);
		api.Gui.LoadOrUpdateCairoTexture(surface, linearMag: false, ref hoverTexture);
		context.Dispose();
		surface.Dispose();
	}

	private ItemStack[] ResolveWildCard(IWorldAccessor world, CraftingRecipeIngredient ingred, ItemStack[] allStacks = null)
	{
		if (resolveCache.ContainsKey(ingred.Code))
		{
			return resolveCache[ingred.Code];
		}
		List<ItemStack> matches = new List<ItemStack>();
		if (allStacks != null)
		{
			foreach (ItemStack val in allStacks)
			{
				if (val.Class == ingred.Type && !(val.Collectible.Code == null) && WildcardUtil.Match(ingred.Code, val.Collectible.Code, ingred.AllowedVariants))
				{
					matches.Add(new ItemStack(val.Collectible, ingred.Quantity));
				}
			}
			resolveCache[ingred.Code] = matches.ToArray();
			return matches.ToArray();
		}
		foreach (CollectibleObject val2 in world.Collectibles)
		{
			if (WildcardUtil.Match(ingred.Code, val2.Code, ingred.AllowedVariants))
			{
				matches.Add(new ItemStack(val2, ingred.Quantity));
			}
		}
		resolveCache[ingred.Code] = matches.ToArray();
		return resolveCache[ingred.Code];
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath curfp = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool requireLinebreak = offsetX + BoundsPerLine[0].Width > curfp.X2;
		BoundsPerLine[0].X = (requireLinebreak ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (requireLinebreak ? (currentLineHeight + GuiElement.scaled(UnscaledMarginTop)) : 0.0);
		BoundsPerLine[0].Width = 3.0 * (size + 3.0) + GuiElement.scaled(PaddingRight);
		nextOffsetX = (requireLinebreak ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!requireLinebreak)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 3; y++)
			{
				ctx.Rectangle(BoundsPerLine[0].X + (double)x * (size + GuiElement.scaled(3.0)), BoundsPerLine[0].Y + (double)y * (size + GuiElement.scaled(3.0)), size, size);
				ctx.Fill();
			}
		}
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		LineRectangled bounds = BoundsPerLine[0];
		int relx = (int)((double)api.Input.MouseX - renderX);
		int rely = (int)((double)api.Input.MouseY - renderY);
		if (!bounds.PointInside(relx, rely) && (secondsVisible -= deltaTime) <= 0f)
		{
			secondsVisible = 1f;
			curItemIndex = (curItemIndex + 1) % GridRecipesAndUnIn.Length;
			secondCounter++;
		}
		GridRecipeAndUnnamedIngredients recipeunin = (CurrentVisibleRecipe = GridRecipesAndUnIn[curItemIndex]);
		if (extraTexts.TryGetValue(curItemIndex, out var extraTextTexture))
		{
			capi.Render.Render2DTexturePremultipliedAlpha(extraTextTexture.TextureId, (float)(renderX + bounds.X), (float)(renderY + bounds.Y + 3.0 * (size + 3.0)), extraTextTexture.Width, extraTextTexture.Height);
		}
		double rx = 0.0;
		double ry = 0.0;
		int mx = api.Input.MouseX;
		int my = api.Input.MouseY;
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 3; y++)
			{
				int index = recipeunin.Recipe.GetGridIndex(y, x, recipeunin.Recipe.resolvedIngredients, recipeunin.Recipe.Width);
				rx = renderX + bounds.X + (double)x * (size + GuiElement.scaled(3.0));
				ry = renderY + bounds.Y + (double)y * (size + GuiElement.scaled(3.0));
				float scale = RuntimeEnv.GUIScale;
				ElementBounds scissorBounds = ElementBounds.Fixed(rx / (double)scale, ry / (double)scale, size / (double)scale, size / (double)scale).WithEmptyParent();
				scissorBounds.CalcWorldBounds();
				CraftingRecipeIngredient ingred = recipeunin.Recipe.GetElementInGrid(y, x, recipeunin.Recipe.resolvedIngredients, recipeunin.Recipe.Width);
				if (ingred != null)
				{
					api.Render.PushScissor(scissorBounds, stacking: true);
					ItemStack[] unnamedWildcardStacklist = null;
					Dictionary<int, ItemStack[]> unnamedIngredients = recipeunin.unnamedIngredients;
					if (unnamedIngredients != null && unnamedIngredients.TryGetValue(index, out unnamedWildcardStacklist) && unnamedWildcardStacklist.Length != 0)
					{
						dummyslot.Itemstack = unnamedWildcardStacklist[variantDisplaySequence[secondCounter % 30][x, y] % unnamedWildcardStacklist.Length];
						dummyslot.Itemstack.StackSize = ingred.Quantity;
					}
					else
					{
						dummyslot.Itemstack = ingred.ResolvedItemstack.Clone();
					}
					dummyslot.BackgroundIcon = index.ToString() ?? "";
					dummyslot.Itemstack.Collectible.OnHandbookRecipeRender(capi, recipeunin.Recipe, dummyslot, rx + size * 0.5, ry + size * 0.5, 100.0, size);
					api.Render.PopScissor();
					double dx = (double)mx - rx + 1.0;
					double dy = (double)my - ry + 2.0;
					if (dx >= 0.0 && dx < size && dy >= 0.0 && dy < size)
					{
						RenderItemstackTooltip(dummyslot, rx + dx, ry + dy, deltaTime);
					}
					dummyslot.BackgroundIcon = null;
				}
			}
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		GridRecipeAndUnnamedIngredients recipeunin = GridRecipesAndUnIn[curItemIndex];
		GridRecipe recipe = recipeunin.Recipe;
		LineRectangled[] boundsPerLine = BoundsPerLine;
		foreach (LineRectangled val in boundsPerLine)
		{
			if (val.PointInside(args.X, args.Y))
			{
				int x = (int)(((double)args.X - val.X) / (size + 3.0));
				int y = (int)(((double)args.Y - val.Y) / (size + 3.0));
				CraftingRecipeIngredient ingred = recipe.GetElementInGrid(y, x, recipe.resolvedIngredients, recipe.Width);
				if (ingred == null)
				{
					break;
				}
				int index = recipe.GetGridIndex(y, x, recipe.resolvedIngredients, recipe.Width);
				ItemStack[] unnamedWildcardStacklist = null;
				Dictionary<int, ItemStack[]> unnamedIngredients = recipeunin.unnamedIngredients;
				if (unnamedIngredients != null && unnamedIngredients.TryGetValue(index, out unnamedWildcardStacklist))
				{
					onStackClicked?.Invoke(unnamedWildcardStacklist[variantDisplaySequence[secondCounter % 30][x, y] % unnamedWildcardStacklist.Length]);
				}
				else
				{
					onStackClicked?.Invoke(ingred.ResolvedItemstack);
				}
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		foreach (KeyValuePair<int, LoadedTexture> extraText in extraTexts)
		{
			extraText.Value.Dispose();
		}
		hoverTexture?.Dispose();
	}
}
