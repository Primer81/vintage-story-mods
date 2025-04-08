using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemScythe : ItemShears
{
	private string[] allowedPrefixes;

	private string[] disallowedSuffixes;

	private SkillItem[] modes;

	private bool trimMode;

	public override int MultiBreakQuantity => 5;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		allowedPrefixes = Attributes["codePrefixes"].AsArray<string>();
		disallowedSuffixes = Attributes["disallowedSuffixes"].AsArray<string>();
		ICoreClientAPI capi = api as ICoreClientAPI;
		modes = new SkillItem[2]
		{
			new SkillItem
			{
				Code = new AssetLocation("trim grass"),
				Name = Lang.Get("Trim grass")
			},
			new SkillItem
			{
				Code = new AssetLocation("remove grass"),
				Name = Lang.Get("Remove grass")
			}
		};
		if (capi != null)
		{
			modes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/scythetrim.svg"), 48, 48, 5, -1));
			modes[0].TexturePremultipliedAlpha = false;
			modes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/scytheremove.svg"), 48, 48, 5, -1));
			modes[1].TexturePremultipliedAlpha = false;
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return base.GetHeldInteractionHelp(inSlot).Append(new WorldInteraction
		{
			ActionLangCode = "heldhelp-settoolmode",
			HotKeyCode = "toolmodeselect"
		});
	}

	public override bool CanMultiBreak(Block block)
	{
		for (int i = 0; i < allowedPrefixes.Length; i++)
		{
			if (!block.Code.PathStartsWith(allowedPrefixes[i]))
			{
				continue;
			}
			if (disallowedSuffixes != null)
			{
				for (int j = 0; j < disallowedSuffixes.Length; j++)
				{
					if (block.Code.Path.EndsWithOrdinal(disallowedSuffixes[j]))
					{
						return false;
					}
				}
			}
			return true;
		}
		return false;
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
		if (blockSel != null)
		{
			IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
			if (byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				byEntity.Attributes.SetBool("didBreakBlocks", value: false);
				byEntity.Attributes.SetBool("didPlayScytheSound", value: false);
				handling = EnumHandHandling.PreventDefault;
			}
		}
	}

	public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		if (byEntity.World.Side == EnumAppSide.Client)
		{
			ModelTransform tf = new ModelTransform();
			tf.EnsureDefaultValues();
			float t = secondsPassed / 1.35f;
			float f = (float)Easings.EaseOutBack(Math.Min(t * 2f, 1f));
			float f2 = (float)Math.Sin(GameMath.Clamp(4.398229640124304 * (double)(t - 0.5f), 0.0, 3.0));
			tf.Translation.X += Math.Min(0.2f, t * 3f);
			tf.Translation.Y -= Math.Min(0.75f, t * 3f);
			tf.Translation.Z -= Math.Min(1f, t * 3f);
			tf.ScaleXYZ += Math.Min(1f, t * 3f);
			tf.Origin.X -= Math.Min(0.75f, t * 3f);
			tf.Rotation.X = 0f - Math.Min(30f, t * 30f) + f * 30f + f2 * 120f;
			tf.Rotation.Z = (0f - f) * 110f;
			if (secondsPassed > 1.75f)
			{
				float b = 2f * (secondsPassed - 1.75f);
				tf.Rotation.Z += b * 140f;
				tf.Rotation.X /= 1f + b * 10f;
				tf.Translation.X -= b * 0.4f;
				tf.Translation.Y += b * 2f / 0.75f;
				tf.Translation.Z += b * 2f;
			}
			byEntity.Controls.UsingHeldItemTransformBefore = tf;
		}
		performActions(secondsPassed, byEntity, slot, blockSelection);
		if (api.Side == EnumAppSide.Server)
		{
			return true;
		}
		return secondsPassed < 2f;
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		performActions(secondsPassed, byEntity, slot, blockSelection);
	}

	private void performActions(float secondsPassed, EntityAgent byEntity, ItemSlot slot, BlockSelection blockSelection)
	{
		if (blockSelection == null)
		{
			return;
		}
		api.World.BlockAccessor.GetBlock(blockSelection.Position);
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		bool num = CanMultiBreak(api.World.BlockAccessor.GetBlock(blockSelection.Position));
		if (num && secondsPassed > 0.75f && !byEntity.Attributes.GetBool("didPlayScytheSound"))
		{
			api.World.PlaySoundAt(new AssetLocation("sounds/tool/scythe1"), byEntity, byPlayer, randomizePitch: true, 16f);
			byEntity.Attributes.SetBool("didPlayScytheSound", value: true);
		}
		if (num && secondsPassed > 1.05f && !byEntity.Attributes.GetBool("didBreakBlocks"))
		{
			if (byEntity.World.Side == EnumAppSide.Server && byEntity.World.Claims.TryAccess(byPlayer, blockSelection.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				trimMode = slot.Itemstack.Attributes.GetInt("toolMode") == 0;
				OnBlockBrokenWith(byEntity.World, byEntity, slot, blockSelection);
			}
			byEntity.Attributes.SetBool("didBreakBlocks", value: true);
		}
	}

	protected override void breakMultiBlock(BlockPos pos, IPlayer plr)
	{
		if (trimMode)
		{
			Block block = api.World.BlockAccessor.GetBlock(pos);
			Block trimmedBlock = api.World.GetBlock(block.CodeWithVariant("tallgrass", "eaten"));
			bool blockIsTallgrass = block.Variant.ContainsKey("tallgrass");
			if (blockIsTallgrass && block == trimmedBlock)
			{
				return;
			}
			if (blockIsTallgrass && trimmedBlock != null)
			{
				api.World.BlockAccessor.BreakBlock(pos, plr);
				api.World.BlockAccessor.MarkBlockDirty(pos);
				api.World.BlockAccessor.SetBlock(trimmedBlock.BlockId, pos);
				if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityTransient be)
				{
					be.ConvertToOverride = block.Code.ToShortString();
				}
				return;
			}
		}
		base.breakMultiBlock(pos, plr);
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return modes;
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
	{
		return slot.Itemstack.Attributes.GetInt("toolMode");
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		int i = 0;
		while (modes != null && i < modes.Length)
		{
			modes[i]?.Dispose();
			i++;
		}
	}
}
