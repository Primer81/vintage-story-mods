using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class NpcTeleportCommand : INpcCommand
{
	protected EntityAnimalBot entity;

	public Vec3d Target;

	public string Type => "tp";

	public NpcTeleportCommand(EntityAnimalBot entity, Vec3d target)
	{
		this.entity = entity;
		Target = target;
	}

	public void Start()
	{
		entity.TeleportToDouble(Target.X, Target.Y, Target.Z);
	}

	public void Stop()
	{
	}

	public bool IsFinished()
	{
		return true;
	}

	public override string ToString()
	{
		return "Teleport to " + Target;
	}

	public void ToAttribute(ITreeAttribute tree)
	{
		tree.SetDouble("x", Target.X);
		tree.SetDouble("y", Target.Y);
		tree.SetDouble("z", Target.Z);
	}

	public void FromAttribute(ITreeAttribute tree)
	{
		Target = new Vec3d(tree.GetDouble("x"), tree.GetDouble("y"), tree.GetDouble("z"));
	}
}
