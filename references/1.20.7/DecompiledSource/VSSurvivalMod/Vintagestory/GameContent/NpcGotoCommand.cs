using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class NpcGotoCommand : INpcCommand
{
	protected EntityAnimalBot entity;

	public Vec3d Target;

	public float AnimSpeed;

	public float GotoSpeed;

	public string AnimCode;

	public bool astar;

	public string Type => "goto";

	public NpcGotoCommand(EntityAnimalBot entity, Vec3d target, bool astar, string animCode = "walk", float gotoSpeed = 0.02f, float animSpeed = 1f)
	{
		this.entity = entity;
		this.astar = astar;
		Target = target;
		AnimSpeed = animSpeed;
		AnimCode = animCode;
		GotoSpeed = gotoSpeed;
	}

	public void Start()
	{
		if (astar)
		{
			entity.wppathTraverser.WalkTowards(Target, GotoSpeed, 0.2f, OnDone, OnDone);
		}
		else
		{
			entity.linepathTraverser.NavigateTo(Target, GotoSpeed, OnDone, OnDone);
		}
		if (AnimSpeed != 0.02f)
		{
			entity.AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = AnimCode,
				Code = AnimCode,
				AnimationSpeed = AnimSpeed
			}.Init());
		}
		else if (!entity.AnimManager.StartAnimation(AnimCode))
		{
			entity.AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = AnimCode,
				Code = AnimCode,
				AnimationSpeed = AnimSpeed
			}.Init());
		}
		entity.Controls.Sprint = AnimCode == "run" || AnimCode == "sprint";
	}

	public void Stop()
	{
		entity.linepathTraverser.Stop();
		entity.wppathTraverser.Stop();
		entity.AnimManager.StopAnimation(AnimCode);
		entity.Controls.Sprint = false;
	}

	private void OnDone()
	{
		entity.AnimManager.StopAnimation(AnimCode);
		entity.Controls.Sprint = false;
	}

	public bool IsFinished()
	{
		return !entity.linepathTraverser.Active;
	}

	public override string ToString()
	{
		return $"{AnimCode} to {Target} (gotospeed {GotoSpeed}, animspeed {AnimSpeed})";
	}

	public void ToAttribute(ITreeAttribute tree)
	{
		tree.SetDouble("x", Target.X);
		tree.SetDouble("y", Target.Y);
		tree.SetDouble("z", Target.Z);
		tree.SetFloat("animSpeed", AnimSpeed);
		tree.SetFloat("gotoSpeed", GotoSpeed);
		tree.SetString("animCode", AnimCode);
		tree.SetBool("astar", astar);
	}

	public void FromAttribute(ITreeAttribute tree)
	{
		Target = new Vec3d(tree.GetDouble("x"), tree.GetDouble("y"), tree.GetDouble("z"));
		AnimSpeed = tree.GetFloat("animSpeed");
		GotoSpeed = tree.GetFloat("gotoSpeed");
		AnimCode = tree.GetString("animCode");
		astar = tree.GetBool("astar");
	}
}
