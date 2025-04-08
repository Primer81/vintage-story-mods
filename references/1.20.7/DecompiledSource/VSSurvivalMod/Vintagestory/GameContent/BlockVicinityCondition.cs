using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class BlockVicinityCondition : IActionCondition, IStorableTypedComponent
{
	[JsonProperty]
	private AssetLocation blockCode;

	[JsonProperty]
	private float searchRange;

	protected EntityActivitySystem vas;

	private long lastSearchTotalMs;

	private bool conditionsatisfied;

	[JsonProperty]
	public bool Invert { get; set; }

	public string Type => "blockvicinity";

	public BlockVicinityCondition()
	{
	}

	public BlockVicinityCondition(EntityActivitySystem vas, AssetLocation blockCode, float searchRange, bool invert = false)
	{
		this.vas = vas;
		this.blockCode = blockCode;
		this.searchRange = searchRange;
		Invert = invert;
	}

	public virtual bool ConditionSatisfied(Entity e)
	{
		long ela = vas.Entity.Api.World.ElapsedMilliseconds;
		if (ela - lastSearchTotalMs > 1500)
		{
			lastSearchTotalMs = ela;
			conditionsatisfied = getTarget() != null;
		}
		return conditionsatisfied;
	}

	private BlockPos getTarget()
	{
		float range = GameMath.Clamp(searchRange, -10f, 10f);
		ICoreAPI api = vas.Entity.Api;
		BlockPos minPos = vas.Entity.ServerPos.XYZ.Add(0f - range, -1.0, 0f - range).AsBlockPos;
		BlockPos maxPos = vas.Entity.ServerPos.XYZ.Add(range, 1.0, range).AsBlockPos;
		BlockPos targetPos = null;
		api.World.BlockAccessor.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			if (!(blockCode == null) && block.WildCardMatch(blockCode))
			{
				targetPos = new BlockPos(x, y, z);
			}
		}, centerOrder: true);
		return targetPos;
	}

	public void LoadState(ITreeAttribute tree)
	{
	}

	public void StoreState(ITreeAttribute tree)
	{
	}

	public void AddGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		ElementBounds b = ElementBounds.Fixed(0.0, 0.0, 250.0, 25.0);
		singleComposer.AddStaticText("Search Range (capped 10 blocks)", CairoFont.WhiteDetailText(), b).AddNumberInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "searchRange").AddStaticText("Block Code", CairoFont.WhiteDetailText(), b = b.BelowCopy(0.0, 15.0))
			.AddTextInput(b = b.BelowCopy(0.0, -5.0), null, CairoFont.WhiteDetailText(), "blockCode");
		singleComposer.GetNumberInput("searchRange").SetValue(searchRange);
		singleComposer.GetTextInput("blockCode").SetValue(blockCode?.ToShortString());
	}

	public void StoreGuiEditFields(ICoreClientAPI capi, GuiComposer singleComposer)
	{
		blockCode = new AssetLocation(singleComposer.GetTextInput("blockCode").GetText());
		searchRange = singleComposer.GetNumberInput("searchRange").GetValue();
	}

	public IActionCondition Clone()
	{
		return new BlockVicinityCondition(vas, blockCode, searchRange, Invert);
	}

	public override string ToString()
	{
		return (Invert ? "When not near block" : "When near block") + blockCode;
	}

	public void OnLoaded(EntityActivitySystem vas)
	{
		this.vas = vas;
	}
}
