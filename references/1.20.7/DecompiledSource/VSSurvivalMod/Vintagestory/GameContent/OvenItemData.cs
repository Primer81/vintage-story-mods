using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class OvenItemData
{
	public float BrowningPoint;

	public float TimeToBake;

	public float BakedLevel;

	public float CurHeightMul;

	public float temp;

	public OvenItemData()
	{
	}

	public OvenItemData(float browning, float time, float baked = 0f, float risen = 0f, float tCurrent = 20f)
	{
		BrowningPoint = browning;
		TimeToBake = time;
		BakedLevel = baked;
		CurHeightMul = risen;
		temp = tCurrent;
	}

	public OvenItemData(ItemStack stack)
	{
		BakingProperties bakeprops = BakingProperties.ReadFrom(stack);
		BrowningPoint = bakeprops.Temp.GetValueOrDefault(160f);
		CombustibleProperties combustibleProps = stack.Collectible.CombustibleProps;
		TimeToBake = ((combustibleProps != null) ? (combustibleProps.MeltingDuration * 10f) : 150f);
		BakedLevel = bakeprops.LevelFrom;
		CurHeightMul = bakeprops.StartScaleY;
		temp = 20f;
	}

	public static OvenItemData ReadFromTree(ITreeAttribute tree, int i)
	{
		return new OvenItemData(tree.GetFloat("brown" + i), tree.GetFloat("tbake" + i), tree.GetFloat("baked" + i), tree.GetFloat("heightmul" + i), tree.GetFloat("temp" + i));
	}

	public void WriteToTree(ITreeAttribute tree, int i)
	{
		tree.SetFloat("brown" + i, BrowningPoint);
		tree.SetFloat("tbake" + i, TimeToBake);
		tree.SetFloat("baked" + i, BakedLevel);
		tree.SetFloat("heightmul" + i, CurHeightMul);
		tree.SetFloat("temp" + i, temp);
	}
}
