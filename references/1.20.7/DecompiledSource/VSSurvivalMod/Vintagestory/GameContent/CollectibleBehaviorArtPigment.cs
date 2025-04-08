using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorArtPigment : CollectibleBehavior
{
	private EnumBlockMaterial[] paintableOnBlockMaterials;

	private MeshRef[] meshes;

	private TextureAtlasPosition texPos;

	private SkillItem[] toolModes;

	private List<Block> decorBlocks = new List<Block>();

	private string[] onmaterialsStrTmp;

	private AssetLocation[] decorCodesTmp;

	private bool requireSprintKey = true;

	private static int[] quadVertices = new int[12]
	{
		-1, -1, 0, 1, -1, 0, 1, 1, 0, -1,
		1, 0
	};

	private static int[] quadTextureCoords = new int[8] { 0, 0, 1, 0, 1, 1, 0, 1 };

	private static int[] quadVertexIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	private float consumeChance;

	public CollectibleBehaviorArtPigment(CollectibleObject collObj)
		: base(collObj)
	{
		base.collObj = collObj;
	}

	public override void Initialize(JsonObject properties)
	{
		onmaterialsStrTmp = properties["paintableOnBlockMaterials"].AsArray(new string[0]);
		decorCodesTmp = properties["decorBlockCodes"].AsObject(new AssetLocation[0], collObj.Code.Domain);
		consumeChance = properties["consumeChance"].AsFloat(0.15f);
		requireSprintKey = properties["requireSprintKey"]?.AsBool(defaultValue: true) ?? true;
		base.Initialize(properties);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		paintableOnBlockMaterials = new EnumBlockMaterial[onmaterialsStrTmp.Length];
		for (int k = 0; k < onmaterialsStrTmp.Length; k++)
		{
			if (onmaterialsStrTmp[k] != null)
			{
				try
				{
					paintableOnBlockMaterials[k] = (EnumBlockMaterial)Enum.Parse(typeof(EnumBlockMaterial), onmaterialsStrTmp[k]);
				}
				catch (Exception)
				{
					api.Logger.Warning("ArtPigment behavior for collectible {0}, paintable on material {1} is not a valid block material, will default to stone", collObj.Code, onmaterialsStrTmp[k]);
					paintableOnBlockMaterials[k] = EnumBlockMaterial.Stone;
				}
			}
		}
		onmaterialsStrTmp = null;
		ICoreClientAPI capi = api as ICoreClientAPI;
		AssetLocation[] array = decorCodesTmp;
		foreach (AssetLocation loc in array)
		{
			if (loc.Path.Contains('*'))
			{
				Block[] blocks = (from block in api.World.SearchBlocks(loc)
					orderby block.Variant["col"].ToInt() + 1000 * block.Variant["row"].ToInt()
					select block).ToArray();
				Block[] array2 = blocks;
				foreach (Block block2 in array2)
				{
					decorBlocks.Add(block2);
				}
				if (blocks.Length == 0)
				{
					api.Logger.Warning("ArtPigment behavior for collectible {0}, decor {1}, no such block using this wildcard found", collObj.Code, loc);
				}
			}
			else
			{
				Block block3 = api.World.GetBlock(loc);
				if (block3 == null)
				{
					api.Logger.Warning("ArtPigment behavior for collectible {0}, decor {1} is not a loaded block", collObj.Code, loc);
				}
				else
				{
					decorBlocks.Add(block3);
				}
			}
		}
		if (api.Side == EnumAppSide.Client)
		{
			if (decorBlocks.Count > 0)
			{
				BakedCompositeTexture tex = decorBlocks[0].Textures["up"].Baked;
				texPos = capi.BlockTextureAtlas.Positions[tex.TextureSubId];
			}
			else
			{
				texPos = capi.BlockTextureAtlas.UnknownTexturePosition;
			}
		}
		AssetLocation blockCode = collObj.Code;
		toolModes = new SkillItem[decorBlocks.Count];
		for (int j = 0; j < toolModes.Length; j++)
		{
			toolModes[j] = new SkillItem
			{
				Code = blockCode.CopyWithPath("art" + j),
				Linebreak = (j % GlobalConstants.CaveArtColsPerRow == 0),
				Name = "",
				Data = decorBlocks[j],
				RenderHandler = delegate(AssetLocation code, float dt, double atPosX, double atPosY)
				{
					float num = (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
					string s = code.Path.Substring(3);
					capi.Render.Render2DTexture(meshes[int.Parse(s)], texPos.atlasTextureId, (float)atPosX, (float)atPosY, num, num);
				}
			};
		}
		if (capi != null)
		{
			meshes = new MeshRef[decorBlocks.Count];
			for (int i = 0; i < meshes.Length; i++)
			{
				MeshData mesh = genMesh(i);
				meshes[i] = capi.Render.UploadMesh(mesh);
			}
		}
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (api is ICoreClientAPI && meshes != null)
		{
			for (int i = 0; i < meshes.Length; i++)
			{
				meshes[i]?.Dispose();
			}
		}
	}

	public MeshData genMesh(int index)
	{
		MeshData k = new MeshData(4, 6, withNormals: false, withUv: true, withRgba: false, withFlags: false);
		float x1 = texPos.x1;
		float y1 = texPos.y1;
		float x2 = texPos.x2;
		float y2 = texPos.y2;
		float xSize = (x2 - x1) / (float)GlobalConstants.CaveArtColsPerRow;
		float ySize = (y2 - y1) / (float)GlobalConstants.CaveArtColsPerRow;
		x1 += (float)(index % GlobalConstants.CaveArtColsPerRow) * xSize;
		y1 += (float)(index / GlobalConstants.CaveArtColsPerRow) * ySize;
		for (int j = 0; j < 4; j++)
		{
			k.AddVertex(quadVertices[j * 3], quadVertices[j * 3 + 1], quadVertices[j * 3 + 2], x1 + (float)(1 - quadTextureCoords[j * 2]) * xSize, y1 + (float)quadTextureCoords[j * 2 + 1] * ySize);
		}
		for (int i = 0; i < 6; i++)
		{
			k.AddIndex(quadVertexIndices[i]);
		}
		return k;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
	{
		if (!(blockSel?.Position == null))
		{
			IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
			if (byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak) && byPlayer != null && (!requireSprintKey || byPlayer.Entity.Controls.CtrlKey) && SuitablePosition(byEntity.World.BlockAccessor, blockSel))
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel?.Position == null)
		{
			return false;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		if (byPlayer == null || (requireSprintKey && !byPlayer.Entity.Controls.CtrlKey))
		{
			return false;
		}
		if (!SuitablePosition(byEntity.World.BlockAccessor, blockSel))
		{
			return false;
		}
		handling = EnumHandling.PreventSubsequent;
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
	{
		if (blockSel?.Position == null)
		{
			return;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak) || byPlayer == null || (requireSprintKey && !byPlayer.Entity.Controls.CtrlKey))
		{
			return;
		}
		IBlockAccessor blockAccessor = byEntity.World.BlockAccessor;
		if (SuitablePosition(blockAccessor, blockSel))
		{
			handling = EnumHandling.PreventDefault;
			DrawCaveArt(blockSel, blockAccessor, byPlayer);
			if (byEntity.World.Side == EnumAppSide.Server && byEntity.World.Rand.NextDouble() < (double)consumeChance)
			{
				slot.TakeOut(1);
				slot.MarkDirty();
			}
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/chalkdraw"), (double)blockSel.Position.X + blockSel.HitPosition.X, (double)blockSel.Position.InternalY + blockSel.HitPosition.Y, (double)blockSel.Position.Z + blockSel.HitPosition.Z, byPlayer, randomizePitch: true, 8f);
		}
	}

	private void DrawCaveArt(BlockSelection blockSel, IBlockAccessor blockAccessor, IPlayer byPlayer)
	{
		int toolMode = GetToolMode(null, byPlayer, blockSel);
		Block blockToPlace = (Block)toolModes[toolMode].Data;
		blockAccessor.SetDecor(blockToPlace, blockSel.Position, blockSel.ToDecorIndex());
	}

	public static int BlockSelectionToSubPosition(BlockFacing face, Vec3i voxelPos)
	{
		return new DecorBits(face, voxelPos.X, 15 - voxelPos.Y, voxelPos.Z);
	}

	private bool SuitablePosition(IBlockAccessor blockAccessor, BlockSelection blockSel)
	{
		Block attachingBlock = blockAccessor.GetBlock(blockSel.Position);
		if (attachingBlock.SideSolid[blockSel.Face.Index])
		{
			goto IL_005c;
		}
		if (attachingBlock is BlockMicroBlock)
		{
			BlockEntityMicroBlock obj = blockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMicroBlock;
			if (obj != null && obj.sideAlmostSolid[blockSel.Face.Index])
			{
				goto IL_005c;
			}
		}
		goto IL_008b;
		IL_008b:
		return false;
		IL_005c:
		EnumBlockMaterial targetMaterial = attachingBlock.GetBlockMaterial(blockAccessor, blockSel.Position);
		for (int i = 0; i < paintableOnBlockMaterials.Length; i++)
		{
			if (targetMaterial == paintableOnBlockMaterials[i])
			{
				return true;
			}
		}
		goto IL_008b;
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		if (!requireSprintKey)
		{
			return toolModes;
		}
		if (blockSel == null)
		{
			return null;
		}
		IBlockAccessor blockAccessor = forPlayer.Entity.World.BlockAccessor;
		if (!SuitablePosition(blockAccessor, blockSel))
		{
			return null;
		}
		return toolModes;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer?.Entity == null)
		{
			return 0;
		}
		return byPlayer.Entity.WatchedAttributes.GetInt("toolModeCaveArt");
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
	{
		byPlayer?.Entity.WatchedAttributes.SetInt("toolModeCaveArt", toolMode);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "ctrl",
				ActionLangCode = "heldhelp-draw",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot, ref handling));
	}
}
