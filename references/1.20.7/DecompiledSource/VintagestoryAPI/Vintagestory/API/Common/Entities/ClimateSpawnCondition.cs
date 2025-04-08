namespace Vintagestory.API.Common.Entities;

/// <summary>
/// A list of conditions based on climate.
/// </summary>
[DocumentAsJson]
public class ClimateSpawnCondition
{
	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>-40</jsondefault>-->
	/// The minimum tempurature for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MinTemp = -40f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>40</jsondefault>-->
	/// The maximum tempurature for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MaxTemp = 40f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The minimum amount of rain for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MinRain;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The maximum amount of rain for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MaxRain = 1f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The minimum amount of forest cover needed for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MinForest;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The maximum amount of forest cover needed for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MaxForest = 1f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The minimum amount of shrubbery needed for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MinShrubs;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>1</jsondefault>-->
	/// The maximum amount of shrubbery needed for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MaxShrubs = 1f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// Won't span below minY. 0...1 is world bottom to sea level, 1...2 is sea level to world top
	/// </summary>
	[DocumentAsJson]
	public float MinY;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>2</jsondefault>-->
	/// Won't span above maxY. 0...1 is world bottom to sea level, 1...2 is sea level to world top
	/// </summary>
	[DocumentAsJson]
	public float MaxY = 2f;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// The minimum amount of forest or shrubs for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public float MinForestOrShrubs;

	public void SetFrom(ClimateSpawnCondition conds)
	{
		MinTemp = conds.MinTemp;
		MaxTemp = conds.MaxTemp;
		MinRain = conds.MinRain;
		MaxRain = conds.MaxRain;
		MinForest = conds.MinForest;
		MaxForest = conds.MaxForest;
		MinShrubs = conds.MinShrubs;
		MaxShrubs = conds.MaxShrubs;
		MinY = conds.MinY;
		MaxY = conds.MaxY;
		MinForestOrShrubs = conds.MinForestOrShrubs;
	}
}
