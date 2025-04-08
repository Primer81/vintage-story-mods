using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FirstStepsTutorial : TutorialBase
{
	public FirstStepsTutorial(ICoreClientAPI capi)
		: base(capi, "tutorial-firststeps")
	{
	}

	protected override void initTutorialSteps()
	{
		steps.Clear();
		addSteps(TutorialStepBase.Press(capi, "wasdkeys", "tutorial-firststeps-1", "walkforward", "walkbackward", "walkleft", "walkright", "jump", "sneak", "sprint"), TutorialStepBase.Press(capi, "clicklink", "tutorial-firststeps-2", "togglemousecontrol"), TutorialStepBase.Press(capi, "keymods", "tutorial-firststeps-3", "inventorydialog", "characterdialog"), TutorialStepBase.Collect(capi, "getknappablestones", "tutorial-firststeps-4", (ItemStack stack) => stack.ItemAttributes?.IsTrue("knappable") ?? false, 3), TutorialStepBase.Collect(capi, "getsticks", "tutorial-firststeps-5", (ItemStack stack) => stack.Collectible.Code.Path == "stick", 5), TutorialStepBase.Knap(capi, "knapknife", "tutorial-firststeps-6", (ItemStack stack) => stack.Collectible.Code.Path.Contains("knifeblade"), 1), TutorialStepBase.Craft(capi, "craftknife", "tutorial-firststeps-7", (ItemStack stack) => stack.Collectible.Tool == EnumTool.Knife, 1), TutorialStepBase.Collect(capi, "getcattails", "tutorial-firststeps-8", (ItemStack stack) => stack.Collectible.Code.Path == "papyrustops" || stack.Collectible.Code.Path == "cattailtops", 10), TutorialStepBase.Craft(capi, "craftbasket", "tutorial-firststeps-9", (ItemStack stack) => stack.Collectible.GetCollectibleInterface<IHeldBag>() != null, 1), TutorialStepBase.Collect(capi, "getfood", "tutorial-firststeps-10", (ItemStack stack) => stack.Collectible.NutritionProps != null, 10), TutorialStepBase.Craft(capi, "knapaxe", "tutorial-firststeps-11", (ItemStack stack) => stack.Collectible.Tool.GetValueOrDefault() == EnumTool.Axe, 1), TutorialStepBase.Collect(capi, "getlogs", "tutorial-firststeps-12", (ItemStack stack) => stack.Collectible is BlockLog, 4), TutorialStepBase.Craft(capi, "craftfirewood", "tutorial-firststeps-13", (ItemStack stack) => stack.Collectible is ItemFirewood, 4), TutorialStepBase.Collect(capi, "getdrygrass", "tutorial-firststeps-14", (ItemStack stack) => stack.Collectible.Code.Path == "drygrass", 3), TutorialStepBase.LookAt(capi, "makefirepit", "tutorial-firststeps-15", (BlockSelection blocksel) => blocksel.Block is BlockFirepit), TutorialStepBase.LookAt(capi, "finishfirepit", "tutorial-firststeps-16", delegate(BlockSelection blocksel)
		{
			BlockFirepit obj2 = blocksel.Block as BlockFirepit;
			return obj2 != null && obj2.Stage == 5;
		}), TutorialStepBase.Craft(capi, "createfirestarter", "tutorial-firststeps-17", (ItemStack stack) => stack.Collectible is ItemFirestarter, 1), TutorialStepBase.LookAt(capi, "ignitefirepit", "tutorial-firststeps-18", delegate(BlockSelection blocksel)
		{
			if (!(blocksel.Block is BlockFirepit))
			{
				return false;
			}
			return capi.World.BlockAccessor.GetBlockEntity(blocksel.Position) is BlockEntityFirepit blockEntityFirepit && blockEntityFirepit.IsBurning;
		}), TutorialStepBase.Craft(capi, "maketorch", "tutorial-firststeps-19", (ItemStack stack) => stack.Collectible.Code.Path.Contains("torch-basic-extinct"), 1), TutorialStepBase.Grab(capi, "ignitetorch", "tutorial-firststeps-20", delegate(ItemStack stack)
		{
			BlockTorch obj = stack.Collectible as BlockTorch;
			return obj != null && !obj.IsExtinct;
		}, 1), TutorialStepBase.Place(capi, "finished", "tutorial-firststeps-21", (BlockPos pos, Block block, ItemStack stack) => block is BlockTorch blockTorch && !blockTorch.IsExtinct, 1));
	}
}
