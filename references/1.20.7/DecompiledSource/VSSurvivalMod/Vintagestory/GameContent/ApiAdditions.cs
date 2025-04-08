using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public static class ApiAdditions
{
	public static List<CookingRecipe> GetCookingRecipes(this ICoreAPI api)
	{
		return api.ModLoader.GetModSystem<RecipeRegistrySystem>().CookingRecipes;
	}

	public static CookingRecipe GetCookingRecipe(this ICoreAPI api, string recipecode)
	{
		return api.ModLoader.GetModSystem<RecipeRegistrySystem>().CookingRecipes.FirstOrDefault((CookingRecipe rec) => recipecode == rec.Code);
	}

	public static List<BarrelRecipe> GetBarrelRecipes(this ICoreAPI api)
	{
		return api.ModLoader.GetModSystem<RecipeRegistrySystem>().BarrelRecipes;
	}

	public static List<AlloyRecipe> GetMetalAlloys(this ICoreAPI api)
	{
		return api.ModLoader.GetModSystem<RecipeRegistrySystem>().MetalAlloys;
	}

	public static List<SmithingRecipe> GetSmithingRecipes(this ICoreAPI api)
	{
		return api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes;
	}

	public static List<KnappingRecipe> GetKnappingRecipes(this ICoreAPI api)
	{
		return api.ModLoader.GetModSystem<RecipeRegistrySystem>().KnappingRecipes;
	}

	public static List<ClayFormingRecipe> GetClayformingRecipes(this ICoreAPI api)
	{
		return api.ModLoader.GetModSystem<RecipeRegistrySystem>().ClayFormingRecipes;
	}

	public static void RegisterCookingRecipe(this ICoreServerAPI api, CookingRecipe r)
	{
		api.ModLoader.GetModSystem<RecipeRegistrySystem>().RegisterCookingRecipe(r);
	}

	public static void RegisterSmithingRecipe(this ICoreServerAPI api, SmithingRecipe r)
	{
		api.ModLoader.GetModSystem<RecipeRegistrySystem>().RegisterSmithingRecipe(r);
	}

	public static void RegisterClayFormingRecipe(this ICoreServerAPI api, ClayFormingRecipe r)
	{
		api.ModLoader.GetModSystem<RecipeRegistrySystem>().RegisterClayFormingRecipe(r);
	}

	public static void RegisterKnappingRecipe(this ICoreServerAPI api, KnappingRecipe r)
	{
		api.ModLoader.GetModSystem<RecipeRegistrySystem>().RegisterKnappingRecipe(r);
	}

	public static void RegisterBarrelRecipe(this ICoreServerAPI api, BarrelRecipe r)
	{
		api.ModLoader.GetModSystem<RecipeRegistrySystem>().RegisterBarrelRecipe(r);
	}

	public static void RegisterMetalAlloy(this ICoreServerAPI api, AlloyRecipe r)
	{
		api.ModLoader.GetModSystem<RecipeRegistrySystem>().RegisterMetalAlloy(r);
	}
}
