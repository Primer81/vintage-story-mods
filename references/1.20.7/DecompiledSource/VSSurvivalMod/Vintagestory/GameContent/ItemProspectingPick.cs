using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class ItemProspectingPick : Item
{
	private ProPickWorkSpace ppws;

	private SkillItem[] toolModes;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		ICoreClientAPI capi = api as ICoreClientAPI;
		toolModes = ObjectCacheUtil.GetOrCreate(api, "proPickToolModes", delegate
		{
			SkillItem[] array = ((api.World.Config.GetString("propickNodeSearchRadius").ToInt() > 0) ? new SkillItem[2]
			{
				new SkillItem
				{
					Code = new AssetLocation("density"),
					Name = Lang.Get("Density Search Mode (Long range, chance based search)")
				},
				new SkillItem
				{
					Code = new AssetLocation("node"),
					Name = Lang.Get("Node Search Mode (Short range, exact search)")
				}
			} : new SkillItem[1]
			{
				new SkillItem
				{
					Code = new AssetLocation("density"),
					Name = Lang.Get("Density Search Mode (Long range, chance based search)")
				}
			});
			if (capi != null)
			{
				array[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/heatmap.svg"), 48, 48, 5, -1));
				array[0].TexturePremultipliedAlpha = false;
				if (array.Length > 1)
				{
					array[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/rocks.svg"), 48, 48, 5, -1));
					array[1].TexturePremultipliedAlpha = false;
				}
			}
			return array;
		});
		if (api.Side == EnumAppSide.Server)
		{
			ppws = ObjectCacheUtil.GetOrCreate(api, "propickworkspace", delegate
			{
				ProPickWorkSpace proPickWorkSpace = new ProPickWorkSpace();
				proPickWorkSpace.OnLoaded(api);
				return proPickWorkSpace;
			});
		}
	}

	public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		float remain = base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
		if (GetToolMode(itemslot, player, blockSel) == 1)
		{
			remain = (remain + remainingResistance) / 2f;
		}
		return remain;
	}

	public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
	{
		int toolMode = GetToolMode(itemslot, (byEntity as EntityPlayer).Player, blockSel);
		int radius = api.World.Config.GetString("propickNodeSearchRadius").ToInt();
		int damage = 1;
		if (toolMode == 1 && radius > 0)
		{
			ProbeBlockNodeMode(world, byEntity, itemslot, blockSel, radius);
			damage = 2;
		}
		else
		{
			ProbeBlockDensityMode(world, byEntity, itemslot, blockSel);
		}
		if (DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
		{
			DamageItem(world, byEntity, itemslot, damage);
		}
		return true;
	}

	public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
	{
		return toolModes;
	}

	public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		return Math.Min(toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode"));
	}

	public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
	{
		slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
	}

	protected virtual void ProbeBlockNodeMode(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, int radius)
	{
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		Block block = world.BlockAccessor.GetBlock(blockSel.Position);
		block.OnBlockBroken(world, blockSel.Position, byPlayer, 0f);
		if (!isPropickable(block) || !(byPlayer is IServerPlayer splr))
		{
			return;
		}
		BlockPos pos = blockSel.Position.Copy();
		Dictionary<string, int> quantityFound = new Dictionary<string, int>();
		api.World.BlockAccessor.WalkBlocks(pos.AddCopy(radius, radius, radius), pos.AddCopy(-radius, -radius, -radius), delegate(Block nblock, int x, int y, int z)
		{
			if (nblock.BlockMaterial == EnumBlockMaterial.Ore && nblock.Variant.ContainsKey("type"))
			{
				string key = "ore-" + nblock.Variant["type"];
				int value = 0;
				quantityFound.TryGetValue(key, out value);
				quantityFound[key] = value + 1;
			}
		});
		List<KeyValuePair<string, int>> resultsOrderedDesc = quantityFound.OrderByDescending((KeyValuePair<string, int> val) => val.Value).ToList();
		if (resultsOrderedDesc.Count == 0)
		{
			splr.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(splr.LanguageCode, "No ore node nearby"), EnumChatType.Notification);
			return;
		}
		splr.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(splr.LanguageCode, "Found the following ore nodes"), EnumChatType.Notification);
		foreach (KeyValuePair<string, int> val2 in resultsOrderedDesc)
		{
			string orename = Lang.GetL(splr.LanguageCode, val2.Key);
			string resultText = Lang.GetL(splr.LanguageCode, resultTextByQuantity(val2.Value), Lang.Get(val2.Key));
			splr.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(splr.LanguageCode, resultText, orename), EnumChatType.Notification);
		}
	}

	private bool isPropickable(Block block)
	{
		if (block == null)
		{
			return false;
		}
		return (block.Attributes?["propickable"].AsBool()).GetValueOrDefault();
	}

	protected virtual string resultTextByQuantity(int value)
	{
		if (value < 10)
		{
			return "propick-nodesearch-traceamount";
		}
		if (value < 20)
		{
			return "propick-nodesearch-smallamount";
		}
		if (value < 40)
		{
			return "propick-nodesearch-mediumamount";
		}
		if (value < 80)
		{
			return "propick-nodesearch-largeamount";
		}
		if (value < 160)
		{
			return "propick-nodesearch-verylargeamount";
		}
		return "propick-nodesearch-hugeamount";
	}

	protected virtual void ProbeBlockDensityMode(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel)
	{
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		Block block = world.BlockAccessor.GetBlock(blockSel.Position);
		float dropMul = 1f;
		if (block.BlockMaterial == EnumBlockMaterial.Ore || block.BlockMaterial == EnumBlockMaterial.Stone)
		{
			dropMul = 0f;
		}
		block.OnBlockBroken(world, blockSel.Position, byPlayer, dropMul);
		if (!isPropickable(block) || !(byPlayer is IServerPlayer splr))
		{
			return;
		}
		if (splr.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			PrintProbeResults(world, splr, itemslot, blockSel.Position);
			return;
		}
		if (!(itemslot.Itemstack.Attributes["probePositions"] is IntArrayAttribute { value: not null } attr2) || attr2.value.Length == 0)
		{
			IntArrayAttribute attr = (IntArrayAttribute)(itemslot.Itemstack.Attributes["probePositions"] = new IntArrayAttribute());
			attr.AddInt(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
			splr.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(splr.LanguageCode, "Ok, need 2 more samples"), EnumChatType.Notification);
			return;
		}
		float requiredSamples = 2f;
		attr2.AddInt(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
		int[] vals = attr2.value;
		for (int i = 0; i < vals.Length; i += 3)
		{
			int x = vals[i];
			int y = vals[i + 1];
			int z = vals[i + 2];
			float mindist = 99f;
			for (int j = i + 3; j < vals.Length; j += 3)
			{
				int dx = x - vals[j];
				int dy = y - vals[j + 1];
				int dz = z - vals[j + 2];
				mindist = Math.Min(mindist, GameMath.Sqrt(dx * dx + dy * dy + dz * dz));
			}
			if (i + 3 < vals.Length)
			{
				requiredSamples -= GameMath.Clamp(mindist * mindist, 3f, 16f) / 16f;
				if (mindist > 20f)
				{
					splr.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(splr.LanguageCode, "Sample too far away from initial reading. Sampling around this point now, need 2 more samples."), EnumChatType.Notification);
					attr2.value = new int[3]
					{
						blockSel.Position.X,
						blockSel.Position.Y,
						blockSel.Position.Z
					};
					return;
				}
			}
		}
		if (requiredSamples > 0f)
		{
			int q = (int)Math.Ceiling(requiredSamples);
			splr.SendMessage(GlobalConstants.InfoLogChatGroup, (q > 1) ? Lang.GetL(splr.LanguageCode, "propick-xsamples", q) : Lang.GetL(splr.LanguageCode, "propick-1sample"), EnumChatType.Notification);
		}
		else
		{
			int startX = vals[0];
			int startY = vals[1];
			int startZ = vals[2];
			PrintProbeResults(world, splr, itemslot, new BlockPos(startX, startY, startZ));
			attr2.value = new int[0];
		}
	}

	protected virtual void PrintProbeResults(IWorldAccessor world, IServerPlayer splr, ItemSlot itemslot, BlockPos pos)
	{
		PropickReading results = GenProbeResults(world, pos);
		string textResults = results.ToHumanReadable(splr.LanguageCode, ppws.pageCodes);
		splr.SendMessage(GlobalConstants.InfoLogChatGroup, textResults, EnumChatType.Notification);
		world.Api.ModLoader.GetModSystem<ModSystemOreMap>()?.DidProbe(results, splr);
	}

	protected virtual PropickReading GenProbeResults(IWorldAccessor world, BlockPos pos)
	{
		if (api.ModLoader.GetModSystem<GenDeposits>()?.Deposits == null)
		{
			return null;
		}
		int regsize = world.BlockAccessor.RegionSize;
		IMapRegion mapRegion = world.BlockAccessor.GetMapRegion(pos.X / regsize, pos.Z / regsize);
		int lx = pos.X % regsize;
		int lz = pos.Z % regsize;
		pos = pos.Copy();
		pos.Y = world.BlockAccessor.GetTerrainMapheightAt(pos);
		int[] blockColumn = ppws.GetRockColumn(pos.X, pos.Z);
		PropickReading readings = new PropickReading
		{
			Position = new Vec3d(pos.X, pos.Y, pos.Z)
		};
		foreach (KeyValuePair<string, IntDataMap2D> val in mapRegion.OreMaps)
		{
			IntDataMap2D value = val.Value;
			int noiseSize = value.InnerSize;
			float posXInRegionOre = (float)lx / (float)regsize * (float)noiseSize;
			float posZInRegionOre = (float)lz / (float)regsize * (float)noiseSize;
			int oreDist = value.GetUnpaddedColorLerped(posXInRegionOre, posZInRegionOre);
			if (ppws.depositsByCode.ContainsKey(val.Key))
			{
				ppws.depositsByCode[val.Key].GetPropickReading(pos, oreDist, blockColumn, out var ppt, out var totalFactor);
				if (totalFactor > 0.0)
				{
					OreReading reading = new OreReading();
					reading.TotalFactor = totalFactor;
					reading.PartsPerThousand = ppt;
					readings.OreReadings[val.Key] = reading;
				}
			}
		}
		return readings;
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		base.OnHeldIdle(slot, byEntity);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		if (api is ICoreServerAPI sapi)
		{
			ppws?.Dispose(api);
			sapi.ObjectCache.Remove("propickworkspace");
		}
		int i = 0;
		while (toolModes != null && i < toolModes.Length)
		{
			toolModes[i]?.Dispose();
			i++;
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "Change tool mode",
				HotKeyCodes = new string[1] { "toolmodeselect" },
				MouseButton = EnumMouseButton.None
			}
		};
	}
}
