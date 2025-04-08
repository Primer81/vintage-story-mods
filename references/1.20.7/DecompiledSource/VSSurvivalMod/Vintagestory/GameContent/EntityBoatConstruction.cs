using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBoatConstruction : Entity
{
	private ConstructionStage[] stages;

	private string material = "oak";

	private Vec3f launchStartPos = new Vec3f();

	private Dictionary<string, string> storedWildCards = new Dictionary<string, string>();

	private WorldInteraction[] nextConstructWis;

	private EntityAgent launchingEntity;

	public override double FrustumSphereRadius => base.FrustumSphereRadius * 2.0;

	private int CurrentStage
	{
		get
		{
			return WatchedAttributes.GetInt("currentStage");
		}
		set
		{
			WatchedAttributes.SetInt("currentStage", value);
		}
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		requirePosesOnServer = true;
		WatchedAttributes.RegisterModifiedListener("currentStage", stagedChanged);
		WatchedAttributes.RegisterModifiedListener("wildcards", loadWildcards);
		base.Initialize(properties, api, InChunkIndex3d);
		stages = properties.Attributes["stages"].AsArray<ConstructionStage>();
		genNextInteractionStage();
	}

	private void stagedChanged()
	{
		MarkShapeModified();
		genNextInteractionStage();
	}

	public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
	{
		HashSet<string> addElements = new HashSet<string>();
		int cstage = CurrentStage;
		for (int i = 0; i <= cstage; i++)
		{
			ConstructionStage stage = stages[i];
			if (stage.AddElements != null)
			{
				string[] addElements2 = stage.AddElements;
				foreach (string addele in addElements2)
				{
					addElements.Add(addele + "/*");
				}
			}
			if (stage.RemoveElements != null)
			{
				string[] addElements2 = stage.RemoveElements;
				foreach (string remele in addElements2)
				{
					addElements.Remove(remele + "/*");
				}
			}
		}
		if (base.Properties.Client.Renderer is EntityShapeRenderer esr)
		{
			esr.OverrideSelectiveElements = addElements.ToArray();
		}
		if (Api is ICoreClientAPI)
		{
			setTexture("debarked", new AssetLocation($"block/wood/debarked/{material}"));
			setTexture("planks", new AssetLocation($"block/wood/planks/{material}1"));
		}
		base.OnTesselation(ref entityShape, shapePathForLogging);
	}

	private void setTexture(string code, AssetLocation assetLocation)
	{
		ICoreClientAPI obj = Api as ICoreClientAPI;
		CompositeTexture compositeTexture2 = (base.Properties.Client.Textures[code] = new CompositeTexture(assetLocation));
		CompositeTexture ctex = compositeTexture2;
		obj.EntityTextureAtlas.GetOrInsertTexture(ctex, out var tui, out var _);
		ctex.Baked.TextureSubId = tui;
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot handslot, Vec3d hitPosition, EnumInteractMode mode)
	{
		base.OnInteract(byEntity, handslot, hitPosition, mode);
		if (Api.Side == EnumAppSide.Client || CurrentStage >= stages.Length - 1)
		{
			return;
		}
		if (CurrentStage == 0 && handslot.Empty && byEntity.Controls.ShiftKey)
		{
			byEntity.TryGiveItemStack(new ItemStack(Api.World.GetItem(new AssetLocation("roller")), 5));
			Die();
		}
		else if (tryConsumeIngredients(byEntity, handslot))
		{
			if (CurrentStage < stages.Length - 1)
			{
				CurrentStage++;
				MarkShapeModified();
			}
			if (CurrentStage >= stages.Length - 2 && !AnimManager.IsAnimationActive("launch"))
			{
				launchingEntity = byEntity;
				launchStartPos = getCenterPos();
				StartAnimation("launch");
			}
			genNextInteractionStage();
		}
	}

	private Vec3f getCenterPos()
	{
		AttachmentPointAndPose apap = AnimManager.Animator?.GetAttachmentPointPose("Center");
		if (apap != null)
		{
			Matrixf mat = new Matrixf();
			mat.RotateY(ServerPos.Yaw + (float)Math.PI / 2f);
			apap.Mul(mat);
			return mat.TransformVector(new Vec4f(0f, 0f, 0f, 1f)).XYZ;
		}
		return null;
	}

	private void genNextInteractionStage()
	{
		if (CurrentStage + 1 >= stages.Length)
		{
			nextConstructWis = null;
			return;
		}
		ConstructionStage stage = stages[CurrentStage + 1];
		if (stage.RequireStacks == null)
		{
			nextConstructWis = null;
			return;
		}
		List<WorldInteraction> wis = new List<WorldInteraction>();
		int i = 0;
		ConstructionIgredient[] requireStacks = stage.RequireStacks;
		foreach (ConstructionIgredient ingred in requireStacks)
		{
			List<ItemStack> stacksl = new List<ItemStack>();
			foreach (KeyValuePair<string, string> val in storedWildCards)
			{
				ingred.FillPlaceHolder(val.Key, val.Value);
			}
			if (!ingred.Resolve(Api.World, "Require stack for construction stage " + (CurrentStage + 1) + " on entity " + Code))
			{
				return;
			}
			i++;
			foreach (CollectibleObject collectible in Api.World.Collectibles)
			{
				ItemStack stack = new ItemStack(collectible);
				if (ingred.SatisfiesAsIngredient(stack, checkStacksize: false))
				{
					stack.StackSize = ingred.Quantity;
					stacksl.Add(stack);
				}
			}
			ItemStack[] stacks = stacksl.ToArray();
			wis.Add(new WorldInteraction
			{
				ActionLangCode = stage.ActionLangCode,
				Itemstacks = stacks,
				GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => stacks,
				MouseButton = EnumMouseButton.Right
			});
		}
		if (stage.RequireStacks.Length == 0)
		{
			wis.Add(new WorldInteraction
			{
				ActionLangCode = stage.ActionLangCode,
				MouseButton = EnumMouseButton.Right
			});
		}
		nextConstructWis = wis.ToArray();
	}

	private bool tryConsumeIngredients(EntityAgent byEntity, ItemSlot handslot)
	{
		_ = Api;
		IServerPlayer plr = (byEntity as EntityPlayer).Player as IServerPlayer;
		ConstructionStage stage = stages[CurrentStage + 1];
		IInventory hotbarinv = plr.InventoryManager.GetHotbarInventory();
		List<KeyValuePair<ItemSlot, int>> takeFrom = new List<KeyValuePair<ItemSlot, int>>();
		List<ConstructionIgredient> requireIngreds = new List<ConstructionIgredient>();
		if (stage.RequireStacks == null)
		{
			return true;
		}
		for (int j = 0; j < stage.RequireStacks.Length; j++)
		{
			requireIngreds.Add(stage.RequireStacks[j].Clone());
		}
		Dictionary<string, string> storeWildCard = new Dictionary<string, string>();
		bool skipMatCost = plr != null && plr.WorldData.CurrentGameMode == EnumGameMode.Creative && byEntity.Controls.CtrlKey;
		foreach (ItemSlot slot in hotbarinv)
		{
			if (slot.Empty)
			{
				continue;
			}
			if (requireIngreds.Count == 0)
			{
				break;
			}
			for (int i = 0; i < requireIngreds.Count; i++)
			{
				ConstructionIgredient ingred2 = requireIngreds[i];
				foreach (KeyValuePair<string, string> val2 in storedWildCards)
				{
					ingred2.FillPlaceHolder(val2.Key, val2.Value);
				}
				ingred2.Resolve(Api.World, "Require stack for construction stage " + i + " on entity " + Code);
				if (!skipMatCost && ingred2.SatisfiesAsIngredient(slot.Itemstack, checkStacksize: false))
				{
					int amountToTake = Math.Min(ingred2.Quantity, slot.Itemstack.StackSize);
					takeFrom.Add(new KeyValuePair<ItemSlot, int>(slot, amountToTake));
					ingred2.Quantity -= amountToTake;
					if (ingred2.Quantity <= 0)
					{
						requireIngreds.RemoveAt(i);
						i--;
						if (ingred2.StoreWildCard != null)
						{
							storeWildCard[ingred2.StoreWildCard] = slot.Itemstack.Collectible.Variant[ingred2.StoreWildCard];
						}
					}
				}
				else if (skipMatCost && ingred2.StoreWildCard != null)
				{
					storeWildCard[ingred2.StoreWildCard] = slot.Itemstack.Collectible.Variant[ingred2.StoreWildCard];
				}
			}
		}
		if (!skipMatCost && requireIngreds.Count > 0)
		{
			ConstructionIgredient ingred = requireIngreds[0];
			string langCode = plr.LanguageCode;
			plr.SendIngameError("missingstack", null, ingred.Quantity, ingred.IsWildCard ? Lang.GetL(langCode, ingred.Name ?? "") : ingred.ResolvedItemstack.GetName());
			return false;
		}
		foreach (KeyValuePair<string, string> val in storeWildCard)
		{
			storedWildCards[val.Key] = val.Value;
		}
		if (!skipMatCost)
		{
			bool soundPlayed = false;
			foreach (KeyValuePair<ItemSlot, int> kvp in takeFrom)
			{
				if (!soundPlayed)
				{
					AssetLocation soundLoc = null;
					ItemStack stack = kvp.Key.Itemstack;
					if (stack.Block != null)
					{
						soundLoc = stack.Block.Sounds?.Place;
					}
					if (soundLoc == null)
					{
						soundLoc = stack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps?.PlaceRemoveSound;
					}
					if (soundLoc != null)
					{
						soundPlayed = true;
						Api.World.PlaySoundAt(soundLoc, this);
					}
				}
				kvp.Key.TakeOut(kvp.Value);
				kvp.Key.MarkDirty();
			}
		}
		storeWildcards();
		WatchedAttributes.MarkPathDirty("wildcards");
		return true;
	}

	private ItemSlot tryTakeFrom(CraftingRecipeIngredient requireStack, List<ItemSlot> skipSlots, IReadOnlyCollection<ItemSlot> fromSlots)
	{
		foreach (ItemSlot slot in fromSlots)
		{
			if (!slot.Empty && !skipSlots.Contains(slot) && requireStack.SatisfiesAsIngredient(slot.Itemstack))
			{
				return slot;
			}
		}
		return null;
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if ((double)(AnimManager.Animator?.GetAnimationState("launch").AnimProgress ?? 0f) >= 0.99)
		{
			AnimManager.StopAnimation("launch");
			CurrentStage = 0;
			MarkShapeModified();
			if (World.Side == EnumAppSide.Server)
			{
				Spawn();
			}
		}
	}

	private void Spawn()
	{
		Vec3f nowOff = getCenterPos();
		Vec3f offset = ((nowOff == null) ? new Vec3f() : (nowOff - launchStartPos));
		EntityProperties type = World.GetEntityType(new AssetLocation("boat-sailed-" + material));
		Entity entity = World.ClassRegistry.CreateEntity(type);
		if ((int)Math.Abs(ServerPos.Yaw * (180f / (float)Math.PI)) == 90 || (int)Math.Abs(ServerPos.Yaw * (180f / (float)Math.PI)) == 270)
		{
			offset.X *= 1.1f;
		}
		offset.Y = 0.5f;
		entity.ServerPos.SetFrom(ServerPos).Add(offset);
		entity.ServerPos.Motion.Add((double)offset.X / 50.0, 0.0, (double)offset.Z / 50.0);
		IPlayer plr = (launchingEntity as EntityPlayer)?.Player;
		if (plr != null)
		{
			entity.WatchedAttributes.SetString("createdByPlayername", plr.PlayerName);
			entity.WatchedAttributes.SetString("createdByPlayerUID", plr.PlayerUID);
		}
		entity.Pos.SetFrom(entity.ServerPos);
		World.SpawnEntity(entity);
	}

	public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
	{
		WorldInteraction[] wis = base.GetInteractionHelp(world, es, player);
		if (nextConstructWis == null)
		{
			return wis;
		}
		wis = wis.Append(nextConstructWis);
		if (CurrentStage == 0)
		{
			wis = wis.Append(new WorldInteraction
			{
				HotKeyCode = "sneak",
				RequireFreeHand = true,
				MouseButton = EnumMouseButton.Right,
				ActionLangCode = "rollers-deconstruct"
			});
		}
		return wis;
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		storeWildcards();
		base.ToBytes(writer, forClient);
	}

	private void storeWildcards()
	{
		TreeAttribute tree = new TreeAttribute();
		foreach (KeyValuePair<string, string> val in storedWildCards)
		{
			tree[val.Key] = new StringAttribute(val.Value);
		}
		WatchedAttributes["wildcards"] = tree;
	}

	public override void FromBytes(BinaryReader reader, bool isSync)
	{
		base.FromBytes(reader, isSync);
		loadWildcards();
	}

	public override string GetInfoText()
	{
		return base.GetInfoText() + "\n" + Lang.Get("Material: {0}", Lang.Get("material-" + material));
	}

	private void loadWildcards()
	{
		storedWildCards.Clear();
		if (WatchedAttributes["wildcards"] is TreeAttribute tree)
		{
			foreach (KeyValuePair<string, IAttribute> val in tree)
			{
				storedWildCards[val.Key] = (val.Value as StringAttribute).value;
			}
		}
		if (storedWildCards.TryGetValue("wood", out var wood))
		{
			material = wood;
			if (material == null || material.Length == 0)
			{
				storedWildCards["wood"] = (material = "oak");
			}
		}
	}
}
