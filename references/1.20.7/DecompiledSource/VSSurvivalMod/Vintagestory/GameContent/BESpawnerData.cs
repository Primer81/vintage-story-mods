using System.ComponentModel;
using ProtoBuf;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class BESpawnerData
{
	[ProtoMember(1)]
	public string[] EntityCodes;

	[ProtoMember(2)]
	public Cuboidi SpawnArea;

	[ProtoMember(3)]
	public float InGameHourInterval;

	[ProtoMember(4)]
	public int MaxCount;

	[ProtoMember(5)]
	[DefaultValue(1)]
	public int GroupSize = 1;

	[ProtoMember(6)]
	public int RemoveAfterSpawnCount;

	[ProtoMember(7)]
	[DefaultValue(0)]
	public int InitialSpawnQuantity;

	[ProtoMember(8)]
	[DefaultValue(true)]
	public bool SpawnOnlyAfterImport = true;

	[ProtoMember(9)]
	[DefaultValue(false)]
	public bool WasImported;

	[ProtoMember(10)]
	[DefaultValue(0)]
	public int InitialQuantitySpawned;

	[ProtoMember(11)]
	public int MinPlayerRange;

	[ProtoMember(13)]
	public int InternalCapacity;

	[ProtoMember(14)]
	public double InternalCharge;

	[ProtoMember(15)]
	public double RechargePerHour;

	[ProtoMember(16)]
	public double LastChargeUpdateTotalHours;

	[ProtoMember(17)]
	public double LastSpawnTotalHours;

	[ProtoMember(18)]
	public EnumSpawnRangeMode SpawnRangeMode;

	[ProtoMember(19)]
	public int MaxPlayerRange = -1;

	[ProtoAfterDeserialization]
	private void afterDeserialization()
	{
		initDefaults();
	}

	public BESpawnerData initDefaults()
	{
		if (SpawnArea == null)
		{
			SpawnArea = new Cuboidi(-3, 0, -3, 3, 3, 3);
		}
		return this;
	}

	public void ToTreeAttributes(ITreeAttribute tree)
	{
		tree.SetInt("maxCount", MaxCount);
		tree.SetFloat("intervalHours", InGameHourInterval);
		tree["entityCodes"] = new StringArrayAttribute((EntityCodes == null) ? new string[0] : EntityCodes);
		tree.SetInt("x1", SpawnArea.X1);
		tree.SetInt("y1", SpawnArea.Y1);
		tree.SetInt("z1", SpawnArea.Z1);
		tree.SetInt("x2", SpawnArea.X2);
		tree.SetInt("y2", SpawnArea.Y2);
		tree.SetInt("z2", SpawnArea.Z2);
		tree.SetInt("minPlayerRange", MinPlayerRange);
		tree.SetInt("maxPlayerRange", MaxPlayerRange);
		tree.SetInt("spawnRangeMode", (int)SpawnRangeMode);
		tree.SetInt("spawnCount", RemoveAfterSpawnCount);
		tree.SetBool("spawnOnlyAfterImport", SpawnOnlyAfterImport);
		tree.SetInt("initialQuantitySpawned", InitialQuantitySpawned);
		tree.SetInt("initialSpawnQuantity", InitialSpawnQuantity);
		tree.SetInt("groupSize", GroupSize);
		tree.SetBool("wasImported", WasImported);
		tree.SetDouble("lastSpawnTotalHours", LastSpawnTotalHours);
		tree.SetInt("internalCapacity", InternalCapacity);
		tree.SetDouble("internalCharge", InternalCharge);
		tree.SetDouble("rechargePerHour", RechargePerHour);
		tree.SetDouble("lastChargeUpdateTotalHours", LastChargeUpdateTotalHours);
	}

	public void FromTreeAttributes(ITreeAttribute tree)
	{
		EntityCodes = (tree["entityCodes"] as StringArrayAttribute)?.value;
		MaxCount = tree.GetInt("maxCount");
		InGameHourInterval = tree.GetFloat("intervalHours");
		SpawnArea = new Cuboidi(tree.GetInt("x1"), tree.GetInt("y1"), tree.GetInt("z1"), tree.GetInt("x2"), tree.GetInt("y2"), tree.GetInt("z2"));
		MinPlayerRange = tree.GetInt("minPlayerRange");
		MaxPlayerRange = tree.GetInt("maxPlayerRange");
		SpawnRangeMode = (EnumSpawnRangeMode)tree.GetInt("spawnRangeMode");
		RemoveAfterSpawnCount = tree.GetInt("spawnCount");
		SpawnOnlyAfterImport = tree.GetBool("spawnOnlyAfterImport");
		GroupSize = tree.GetInt("groupSize");
		InitialQuantitySpawned = tree.GetInt("initialQuantitySpawned");
		InitialSpawnQuantity = tree.GetInt("initialSpawnQuantity");
		WasImported = tree.GetBool("wasImported");
		LastSpawnTotalHours = tree.GetDouble("lastSpawnTotalHours");
		InternalCapacity = tree.GetInt("internalCapacity");
		InternalCharge = tree.GetDouble("internalCharge");
		RechargePerHour = tree.GetDouble("rechargePerHour");
		LastChargeUpdateTotalHours = tree.GetDouble("lastChargeUpdateTotalHours");
	}
}
