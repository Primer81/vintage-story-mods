using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPan : Block, ITexPositionSource
{
	private ITexPositionSource ownTextureSource;

	private TextureAtlasPosition matTexPosition;

	private ILoadedSound sound;

	private Dictionary<string, PanningDrop[]> dropsBySourceMat;

	private WorldInteraction[] interactions;

	public Size2i AtlasSize { get; set; }

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == "material")
			{
				return matTexPosition;
			}
			return ownTextureSource[textureCode];
		}
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		if (GetBlockMaterialCode(activeHotbarSlot.Itemstack) == null)
		{
			return null;
		}
		return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		dropsBySourceMat = Attributes["panningDrops"].AsObject<Dictionary<string, PanningDrop[]>>();
		foreach (PanningDrop[] drops in dropsBySourceMat.Values)
		{
			for (int i = 0; i < drops.Length; i++)
			{
				if (!drops[i].Code.Path.Contains("{rocktype}"))
				{
					drops[i].Resolve(api.World, "panningdrop");
				}
			}
		}
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "panInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block current in api.World.Blocks)
			{
				if (!current.IsMissing && current.CreativeInventoryTabs != null && current.CreativeInventoryTabs.Length != 0 && IsPannableMaterial(current))
				{
					list.Add(new ItemStack(current));
				}
			}
			ItemStack[] stacksArray = list.ToArray();
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-addmaterialtopan",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = delegate
					{
						ItemStack itemstack2 = (api as ICoreClientAPI).World.Player.InventoryManager.ActiveHotbarSlot.Itemstack;
						return (GetBlockMaterialCode(itemstack2) != null) ? null : stacksArray;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-pan",
					MouseButton = EnumMouseButton.Right,
					ShouldApply = delegate
					{
						ItemStack itemstack = (api as ICoreClientAPI).World.Player.InventoryManager.ActiveHotbarSlot.Itemstack;
						return GetBlockMaterialCode(itemstack) != null;
					}
				}
			};
		});
	}

	private ItemStack Resolve(EnumItemClass type, string code)
	{
		if (type == EnumItemClass.Block)
		{
			Block block = api.World.GetBlock(new AssetLocation(code));
			if (block == null)
			{
				api.World.Logger.Error("Failed resolving panning block drop with code {0}. Will skip.", code);
				return null;
			}
			return new ItemStack(block);
		}
		Item item = api.World.GetItem(new AssetLocation(code));
		if (item == null)
		{
			api.World.Logger.Error("Failed resolving panning item drop with code {0}. Will skip.", code);
			return null;
		}
		return new ItemStack(item);
	}

	public string GetBlockMaterialCode(ItemStack stack)
	{
		return stack?.Attributes?.GetString("materialBlockCode");
	}

	public void SetMaterial(ItemSlot slot, Block block)
	{
		slot.Itemstack.Attributes.SetString("materialBlockCode", block.Code.ToShortString());
	}

	public void RemoveMaterial(ItemSlot slot)
	{
		slot.Itemstack.Attributes.RemoveAttribute("materialBlockCode");
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		string blockMaterialCode = GetBlockMaterialCode(itemstack);
		if (blockMaterialCode != null)
		{
			string key = "pan-filled-" + blockMaterialCode + target;
			renderinfo.ModelRef = ObjectCacheUtil.GetOrCreate(capi, key, delegate
			{
				AssetLocation shapePath = new AssetLocation("shapes/block/wood/pan/filled.json");
				Shape shapeBase = Vintagestory.API.Common.Shape.TryGet(capi, shapePath);
				Block block = capi.World.GetBlock(new AssetLocation(blockMaterialCode));
				AtlasSize = capi.BlockTextureAtlas.Size;
				matTexPosition = capi.BlockTextureAtlas.GetPosition(block, "up");
				ownTextureSource = capi.Tesselator.GetTextureSource(this);
				capi.Tesselator.TesselateShape("filledpan", shapeBase, out var modeldata, this, null, 0, 0, 0);
				return capi.Render.UploadMultiTextureMesh(modeldata);
			});
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		handling = EnumHandHandling.PreventDefault;
		if (!firstEvent)
		{
			return;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (byPlayer != null && (blockSel == null || byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak)))
		{
			string blockMatCode = GetBlockMaterialCode(slot.Itemstack);
			if (!byEntity.FeetInLiquid && api.Side == EnumAppSide.Client && blockMatCode != null)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "notinwater", Lang.Get("ingameerror-panning-notinwater"));
			}
			else if (blockMatCode == null && blockSel != null)
			{
				TryTakeMaterial(slot, byEntity, blockSel.Position);
				slot.Itemstack.TempAttributes.SetBool("canpan", value: false);
			}
			else if (blockMatCode != null)
			{
				slot.Itemstack.TempAttributes.SetBool("canpan", value: true);
			}
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if ((byEntity.Controls.TriesToMove || byEntity.Controls.Jump) && !byEntity.Controls.Sneak)
		{
			return false;
		}
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (byPlayer == null)
		{
			return false;
		}
		if (blockSel != null && !byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		string blockMaterialCode = GetBlockMaterialCode(slot.Itemstack);
		if (blockMaterialCode == null || !slot.Itemstack.TempAttributes.GetBool("canpan"))
		{
			return false;
		}
		Vec3d pos = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ;
		pos.Y += byEntity.LocalEyePos.Y - 0.4000000059604645;
		if (secondsUsed > 0.5f && api.World.Rand.NextDouble() > 0.5)
		{
			Block block = api.World.GetBlock(new AssetLocation(blockMaterialCode));
			Vec3d particlePos = pos.Clone();
			particlePos.X += GameMath.Sin((0f - secondsUsed) * 20f) / 5f;
			particlePos.Z += GameMath.Cos((0f - secondsUsed) * 20f) / 5f;
			particlePos.Y -= 0.07000000029802322;
			byEntity.World.SpawnCubeParticles(particlePos, new ItemStack(block), 0.3f, (int)(1.5f + (float)api.World.Rand.NextDouble()), 0.3f + (float)api.World.Rand.NextDouble() / 6f, (byEntity as EntityPlayer)?.Player);
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform tf = new ModelTransform();
			tf.EnsureDefaultValues();
			tf.Origin.Set(0f, 0f, 0f);
			if (secondsUsed > 0.5f)
			{
				tf.Translation.X = Math.Min(0.25f, GameMath.Cos(10f * secondsUsed) / 4f);
				tf.Translation.Y = Math.Min(0.15f, GameMath.Sin(10f * secondsUsed) / 6.666f);
				if (sound == null)
				{
					sound = (api as ICoreClientAPI).World.LoadSound(new SoundParams
					{
						Location = new AssetLocation("sounds/player/panning.ogg"),
						ShouldLoop = false,
						RelativePosition = true,
						Position = new Vec3f(),
						DisposeOnFinish = true,
						Volume = 0.5f,
						Range = 8f
					});
					sound.Start();
				}
			}
			tf.Translation.X -= Math.Min(1.6f, secondsUsed * 4f * 1.57f);
			tf.Translation.Y -= Math.Min(0.1f, secondsUsed * 2f);
			tf.Translation.Z -= Math.Min(1f, secondsUsed * 180f);
			tf.Scale = 1f + Math.Min(0.6f, 2f * secondsUsed);
			byEntity.Controls.UsingHeldItemTransformAfter = tf;
			return secondsUsed <= 4f;
		}
		return true;
	}

	public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		if (cancelReason == EnumItemUseCancelReason.ReleasedMouse)
		{
			return false;
		}
		if (api.Side == EnumAppSide.Client)
		{
			sound?.Stop();
			sound = null;
		}
		return true;
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		sound?.Stop();
		sound = null;
		if (secondsUsed >= 3.4f)
		{
			string code = GetBlockMaterialCode(slot.Itemstack);
			if (api.Side == EnumAppSide.Server && code != null)
			{
				CreateDrop(byEntity, code);
			}
			RemoveMaterial(slot);
			slot.MarkDirty();
			byEntity.GetBehavior<EntityBehaviorHunger>()?.ConsumeSaturation(4f);
		}
	}

	private void CreateDrop(EntityAgent byEntity, string fromBlockCode)
	{
		IPlayer player = (byEntity as EntityPlayer)?.Player;
		PanningDrop[] drops = null;
		foreach (string val2 in dropsBySourceMat.Keys)
		{
			if (WildcardUtil.Match(val2, fromBlockCode))
			{
				drops = dropsBySourceMat[val2];
			}
		}
		if (drops == null)
		{
			throw new InvalidOperationException("Coding error, no drops defined for source mat " + fromBlockCode);
		}
		string rocktype = api.World.GetBlock(new AssetLocation(fromBlockCode))?.Variant["rock"];
		drops.Shuffle(api.World.Rand);
		for (int i = 0; i < drops.Length; i++)
		{
			PanningDrop drop = drops[i];
			double num = api.World.Rand.NextDouble();
			float extraMul = 1f;
			if (drop.DropModbyStat != null)
			{
				extraMul = byEntity.Stats.GetBlended(drop.DropModbyStat);
			}
			float val = drop.Chance.nextFloat() * extraMul;
			ItemStack stack = drop.ResolvedItemstack;
			if (drops[i].Code.Path.Contains("{rocktype}"))
			{
				stack = Resolve(drops[i].Type, drops[i].Code.Path.Replace("{rocktype}", rocktype));
			}
			if (num < (double)val && stack != null)
			{
				stack = stack.Clone();
				if (player == null || !player.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
				{
					api.World.SpawnItemEntity(stack, byEntity.ServerPos.XYZ);
				}
				break;
			}
		}
	}

	public virtual bool IsPannableMaterial(Block block)
	{
		return block.Attributes?.IsTrue("pannable") ?? false;
	}

	protected virtual void TryTakeMaterial(ItemSlot slot, EntityAgent byEntity, BlockPos position)
	{
		Block block = api.World.BlockAccessor.GetBlock(position);
		if (!IsPannableMaterial(block))
		{
			return;
		}
		if (api.World.BlockAccessor.GetBlock(position.UpCopy()).Id != 0)
		{
			if (api.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).TriggerIngameError(this, "noair", Lang.Get("ingameerror-panning-requireairabove"));
			}
			return;
		}
		string layer = block.Variant["layer"];
		if (layer != null)
		{
			string baseCode = block.FirstCodePart() + "-" + block.FirstCodePart(1);
			Block origblock = api.World.GetBlock(new AssetLocation(baseCode));
			SetMaterial(slot, origblock);
			if (layer == "1")
			{
				api.World.BlockAccessor.SetBlock(0, position);
			}
			else
			{
				AssetLocation code = block.CodeWithVariant("layer", (int.Parse(layer) - 1).ToString() ?? "");
				Block reducedBlock = api.World.GetBlock(code);
				api.World.BlockAccessor.SetBlock(reducedBlock.BlockId, position);
			}
			api.World.BlockAccessor.TriggerNeighbourBlockUpdate(position);
		}
		else
		{
			string pannedBlock = block.Attributes["pannedBlock"].AsString();
			Block reducedBlock2 = ((pannedBlock == null) ? api.World.GetBlock(block.CodeWithVariant("layer", "7")) : api.World.GetBlock(AssetLocation.Create(pannedBlock, block.Code.Domain)));
			if (reducedBlock2 != null)
			{
				SetMaterial(slot, block);
				api.World.BlockAccessor.SetBlock(reducedBlock2.BlockId, position);
				api.World.BlockAccessor.TriggerNeighbourBlockUpdate(position);
			}
			else
			{
				api.Logger.Warning("Missing \"pannedBlock\" attribute for pannable block " + block.Code.ToShortString());
			}
		}
		slot.MarkDirty();
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
