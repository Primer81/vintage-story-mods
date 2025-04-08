using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ItemTextureFlipper : Item
{
	private SkillItem[] skillitems;

	private BlockPos pos;

	private ICoreClientAPI capi;

	private Dictionary<AssetLocation, MultiTextureMeshRef> skillTextures = new Dictionary<AssetLocation, MultiTextureMeshRef>();

	private void renderSkillItem(AssetLocation code, float dt, double atPosX, double atPosY)
	{
		if (!(api.World.BlockAccessor.GetBlock(this.pos) is ITextureFlippable block))
		{
			return;
		}
		OrderedDictionary<string, CompositeTexture> textures = block.GetAvailableTextures(this.pos);
		if (textures != null)
		{
			if (!skillTextures.TryGetValue(code, out var meshref))
			{
				int pos = textures[code.Path].Baked.TextureSubId;
				TextureAtlasPosition texPos = capi.BlockTextureAtlas.Positions[pos];
				MeshData mesh = QuadMeshUtil.GetCustomQuadModelData(texPos.x1, texPos.y1, texPos.x2, texPos.y2, 0f, 0f, 1f, 1f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
				mesh.TextureIds = new int[1] { texPos.atlasTextureId };
				mesh.TextureIndices = new byte[1];
				meshref = capi.Render.UploadMultiTextureMesh(mesh);
				skillTextures[code] = meshref;
			}
			float scale = RuntimeEnv.GUIScale;
			capi.Render.Render2DTexture(meshref, (float)atPosX - 24f * scale, (float)atPosY - 24f * scale, scale * 64f, scale * 64f);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return base.GetToolMode(slot, byPlayer, blockSelection);
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		if (blockSel == null)
		{
			return null;
		}
		BlockPos pos = blockSel.Position;
		if (pos != this.pos)
		{
			if (!(api.World.BlockAccessor.GetBlock(pos) is ITextureFlippable block))
			{
				return null;
			}
			OrderedDictionary<string, CompositeTexture> textures = block.GetAvailableTextures(pos);
			if (textures == null)
			{
				return null;
			}
			skillitems = new SkillItem[textures.Count];
			int i = 0;
			foreach (KeyValuePair<string, CompositeTexture> val in textures)
			{
				skillitems[i++] = new SkillItem
				{
					Code = new AssetLocation(val.Key),
					Name = val.Key,
					Data = val.Key,
					RenderHandler = renderSkillItem
				};
			}
			this.pos = pos;
		}
		return skillitems;
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
		base.SetToolMode(slot, byPlayer, blockSelection, toolMode);
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel != null)
		{
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		if (handling == EnumHandHandling.PreventDefault || blockSel == null)
		{
			return;
		}
		int toolMode = slot.Itemstack.Attributes.GetInt("toolMode");
		BlockPos pos = blockSel.Position;
		if (api.World.BlockAccessor.GetBlock(pos) is ITextureFlippable block)
		{
			OrderedDictionary<string, CompositeTexture> textures = block.GetAvailableTextures(pos);
			if (textures == null)
			{
				return;
			}
			block.FlipTexture(pos, textures.GetKeyAtIndex(toolMode));
		}
		handling = EnumHandHandling.PreventDefault;
	}
}
