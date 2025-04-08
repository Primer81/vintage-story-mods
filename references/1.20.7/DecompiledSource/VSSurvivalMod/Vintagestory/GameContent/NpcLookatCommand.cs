using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class NpcLookatCommand : INpcCommand
{
	public float yaw;

	private EntityAgent entity;

	public string Type => "lookat";

	public NpcLookatCommand(EntityAgent entity, float yaw)
	{
		this.entity = entity;
		this.yaw = yaw;
	}

	public bool IsFinished()
	{
		return true;
	}

	public void Start()
	{
		entity.ServerPos.Yaw = yaw;
	}

	public void Stop()
	{
	}

	public void FromAttribute(ITreeAttribute tree)
	{
		yaw = tree.GetFloat("yaw");
	}

	public void ToAttribute(ITreeAttribute tree)
	{
		tree.SetFloat("yaw", yaw);
	}

	public override string ToString()
	{
		return "Look at " + yaw;
	}
}
