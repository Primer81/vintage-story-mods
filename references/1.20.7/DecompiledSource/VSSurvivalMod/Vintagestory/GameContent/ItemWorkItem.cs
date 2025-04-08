using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemWorkItem : Item, IAnvilWorkable
{
	private static int nextMeshRefId;

	public bool isBlisterSteel;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		isBlisterSteel = Variant["metal"] == "blistersteel";
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (!itemstack.Attributes.HasAttribute("voxels"))
		{
			CachedMeshRef ccmr = ObjectCacheUtil.GetOrCreate(capi, "clearWorkItem" + Variant["metal"], delegate
			{
				byte[,,] voxels2 = new byte[16, 6, 16];
				ItemIngot.CreateVoxelsFromIngot(capi, ref voxels2);
				int textureId2;
				MeshData data2 = GenMesh(capi, itemstack, voxels2, out textureId2);
				return new CachedMeshRef
				{
					meshref = capi.Render.UploadMultiTextureMesh(data2),
					TextureId = textureId2
				};
			});
			renderinfo.ModelRef = ccmr.meshref;
			renderinfo.TextureId = ccmr.TextureId;
			base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
			return;
		}
		int meshrefId = itemstack.Attributes.GetInt("meshRefId", -1);
		if (meshrefId == -1)
		{
			meshrefId = ++nextMeshRefId;
		}
		CachedMeshRef cmr = ObjectCacheUtil.GetOrCreate(capi, meshrefId.ToString() ?? "", delegate
		{
			byte[,,] voxels = GetVoxels(itemstack);
			int textureId;
			MeshData data = GenMesh(capi, itemstack, voxels, out textureId);
			return new CachedMeshRef
			{
				meshref = capi.Render.UploadMultiTextureMesh(data),
				TextureId = textureId
			};
		});
		renderinfo.ModelRef = cmr.meshref;
		renderinfo.TextureId = cmr.TextureId;
		itemstack.Attributes.SetInt("meshRefId", meshrefId);
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public static MeshData GenMesh(ICoreClientAPI capi, ItemStack workitemStack, byte[,,] voxels, out int textureId)
	{
		textureId = 0;
		if (workitemStack == null)
		{
			return null;
		}
		MeshData workItemMesh = new MeshData(24, 36);
		workItemMesh.CustomBytes = new CustomMeshDataPartByte
		{
			Conversion = DataConversion.NormalizedFloat,
			Count = workItemMesh.VerticesCount,
			InterleaveSizes = new int[1] { 1 },
			Instanced = false,
			InterleaveOffsets = new int[1],
			InterleaveStride = 1,
			Values = new byte[workItemMesh.VerticesCount]
		};
		TextureAtlasPosition tposSlag;
		TextureAtlasPosition tposMetal;
		if (workitemStack.Collectible.FirstCodePart() == "ironbloom")
		{
			tposSlag = capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("anvil-copper")), "ironbloom");
			tposMetal = capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("ingotpile")), "iron");
		}
		else
		{
			tposMetal = capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("ingotpile")), workitemStack.Collectible.Variant["metal"]);
			tposSlag = tposMetal;
		}
		MeshData metalVoxelMesh = CubeMeshUtil.GetCubeOnlyScaleXyz(1f / 32f, 1f / 32f, new Vec3f(1f / 32f, 1f / 32f, 1f / 32f));
		CubeMeshUtil.SetXyzFacesAndPacketNormals(metalVoxelMesh);
		metalVoxelMesh.CustomBytes = new CustomMeshDataPartByte
		{
			Conversion = DataConversion.NormalizedFloat,
			Count = metalVoxelMesh.VerticesCount,
			Values = new byte[metalVoxelMesh.VerticesCount]
		};
		textureId = tposMetal.atlasTextureId;
		for (int m = 0; m < 6; m++)
		{
			metalVoxelMesh.AddTextureId(textureId);
		}
		metalVoxelMesh.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
		metalVoxelMesh.XyzFacesCount = 6;
		metalVoxelMesh.Rgba.Fill(byte.MaxValue);
		MeshData slagVoxelMesh = metalVoxelMesh.Clone();
		for (int l = 0; l < metalVoxelMesh.Uv.Length; l++)
		{
			if (l % 2 > 0)
			{
				metalVoxelMesh.Uv[l] = tposMetal.y1 + metalVoxelMesh.Uv[l] * 2f / (float)capi.BlockTextureAtlas.Size.Height;
				slagVoxelMesh.Uv[l] = tposSlag.y1 + slagVoxelMesh.Uv[l] * 2f / (float)capi.BlockTextureAtlas.Size.Height;
			}
			else
			{
				metalVoxelMesh.Uv[l] = tposMetal.x1 + metalVoxelMesh.Uv[l] * 2f / (float)capi.BlockTextureAtlas.Size.Width;
				slagVoxelMesh.Uv[l] = tposSlag.x1 + slagVoxelMesh.Uv[l] * 2f / (float)capi.BlockTextureAtlas.Size.Width;
			}
		}
		MeshData metVoxOffset = metalVoxelMesh.Clone();
		MeshData slagVoxOffset = slagVoxelMesh.Clone();
		for (int x = 0; x < 16; x++)
		{
			for (int y = 0; y < 6; y++)
			{
				for (int z = 0; z < 16; z++)
				{
					EnumVoxelMaterial mat = (EnumVoxelMaterial)voxels[x, y, z];
					if (mat != 0)
					{
						float px = (float)x / 16f;
						float py = 0.625f + (float)y / 16f;
						float pz = (float)z / 16f;
						MeshData mesh = ((mat == EnumVoxelMaterial.Metal) ? metalVoxelMesh : slagVoxelMesh);
						MeshData meshVoxOffset = ((mat == EnumVoxelMaterial.Metal) ? metVoxOffset : slagVoxOffset);
						for (int k = 0; k < mesh.xyz.Length; k += 3)
						{
							meshVoxOffset.xyz[k] = px + mesh.xyz[k];
							meshVoxOffset.xyz[k + 1] = py + mesh.xyz[k + 1];
							meshVoxOffset.xyz[k + 2] = pz + mesh.xyz[k + 2];
						}
						float textureSize = 32f / (float)capi.BlockTextureAtlas.Size.Width;
						float offsetX = px * textureSize;
						float offsetY = py * 32f / (float)capi.BlockTextureAtlas.Size.Width;
						float offsetZ = pz * textureSize;
						for (int j = 0; j < mesh.Uv.Length; j += 2)
						{
							meshVoxOffset.Uv[j] = mesh.Uv[j] + GameMath.Mod(offsetX + offsetY, textureSize);
							meshVoxOffset.Uv[j + 1] = mesh.Uv[j + 1] + GameMath.Mod(offsetZ + offsetY, textureSize);
						}
						for (int i = 0; i < meshVoxOffset.CustomBytes.Values.Length; i++)
						{
							byte glowSub = (byte)GameMath.Clamp(10 * (Math.Abs(x - 8) + Math.Abs(z - 8) + Math.Abs(y - 2)), 100, 250);
							meshVoxOffset.CustomBytes.Values[i] = (byte)((mat != EnumVoxelMaterial.Metal) ? glowSub : 0);
						}
						workItemMesh.AddMeshData(meshVoxOffset);
					}
				}
			}
		}
		return workItemMesh;
	}

	public static byte[,,] GetVoxels(ItemStack workitemStack)
	{
		return BlockEntityAnvil.deserializeVoxels(workitemStack.Attributes.GetBytes("voxels"));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		int recipeId = inSlot.Itemstack.Attributes.GetInt("selectedRecipeId");
		SmithingRecipe recipe = api.GetSmithingRecipes().FirstOrDefault((SmithingRecipe r) => r.RecipeId == recipeId);
		if (recipe == null)
		{
			dsc.AppendLine("Unknown work item");
			return;
		}
		dsc.AppendLine(Lang.Get("Unfinished {0}", recipe.Output.ResolvedItemstack.GetName()));
	}

	public int GetRequiredAnvilTier(ItemStack stack)
	{
		string metalcode = Variant["metal"];
		int tier = 0;
		if (api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(metalcode, out var var))
		{
			tier = var.Tier - 1;
		}
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes["requiresAnvilTier"].Exists)
		{
			tier = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(tier);
		}
		return tier;
	}

	public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
	{
		stack = GetBaseMaterial(stack);
		return (from r in api.GetSmithingRecipes()
			where r.Ingredient.SatisfiesAsIngredient(stack)
			orderby r.Output.ResolvedItemstack.Collectible.Code
			select r).ToList();
	}

	public bool CanWork(ItemStack stack)
	{
		float temperature = stack.Collectible.GetTemperature(api.World, stack);
		float meltingpoint = stack.Collectible.GetMeltingPoint(api.World, null, new DummySlot(stack));
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes["workableTemperature"].Exists)
		{
			return stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingpoint / 2f) <= temperature;
		}
		return temperature >= meltingpoint / 2f;
	}

	public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
	{
		if (beAnvil.WorkItemStack != null)
		{
			return null;
		}
		try
		{
			beAnvil.Voxels = BlockEntityAnvil.deserializeVoxels(stack.Attributes.GetBytes("voxels"));
			beAnvil.SelectedRecipeId = stack.Attributes.GetInt("selectedRecipeId");
		}
		catch (Exception)
		{
		}
		return stack.Clone();
	}

	public ItemStack GetBaseMaterial(ItemStack stack)
	{
		return new ItemStack(api.World.GetItem(AssetLocation.Create("ingot-" + Variant["metal"], Attributes?["baseMaterialDomain"].AsString("game"))) ?? throw new Exception(string.Format("Base material for {0} not found, there is no item with code 'ingot-{1}'", stack.Collectible.Code, Variant["metal"])));
	}

	public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
	{
		if (beAnvil.SelectedRecipe.Name.Path == "plate" || beAnvil.SelectedRecipe.Name.Path == "blistersteel")
		{
			return EnumHelveWorkableMode.TestSufficientVoxelsWorkable;
		}
		return EnumHelveWorkableMode.NotWorkable;
	}
}
