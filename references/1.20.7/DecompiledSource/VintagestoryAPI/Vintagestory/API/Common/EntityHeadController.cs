using System;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public class EntityHeadController
{
	public ElementPose HeadPose;

	public ElementPose NeckPose;

	public ElementPose UpperTorsoPose;

	public ElementPose LowerTorsoPose;

	public ElementPose UpperFootLPose;

	public ElementPose UpperFootRPose;

	protected EntityAgent entity;

	protected IAnimationManager animManager;

	public float yawOffset;

	public float pitchOffset;

	public EntityHeadController(IAnimationManager animator, EntityAgent entity, Shape entityShape)
	{
		this.entity = entity;
		animManager = animator;
		HeadPose = animator.Animator.GetPosebyName("Head");
		NeckPose = animator.Animator.GetPosebyName("Neck");
		UpperTorsoPose = animator.Animator.GetPosebyName("UpperTorso");
		LowerTorsoPose = animator.Animator.GetPosebyName("LowerTorso");
		UpperFootRPose = animator.Animator.GetPosebyName("UpperFootR");
		UpperFootLPose = animator.Animator.GetPosebyName("UpperFootL");
	}

	/// <summary>
	/// The event fired when the game ticks.
	/// </summary>
	/// <param name="dt"></param>
	public virtual void OnFrame(float dt)
	{
		HeadPose.degOffY = 0f;
		HeadPose.degOffZ = 0f;
		NeckPose.degOffZ = 0f;
		UpperTorsoPose.degOffY = 0f;
		UpperTorsoPose.degOffZ = 0f;
		LowerTorsoPose.degOffZ = 0f;
		UpperFootRPose.degOffZ = 0f;
		UpperFootLPose.degOffZ = 0f;
		if (entity.Pos.HeadYaw != 0f || entity.Pos.HeadPitch != 0f)
		{
			float degoffy = (entity.Pos.HeadYaw + yawOffset) * (180f / (float)Math.PI);
			float degoffz = (entity.Pos.HeadPitch + pitchOffset) * (180f / (float)Math.PI);
			HeadPose.degOffY = degoffy * 0.45f;
			HeadPose.degOffZ = degoffz * 0.35f;
			NeckPose.degOffY = degoffy * 0.35f;
			NeckPose.degOffZ = degoffz * 0.4f;
			ICoreClientAPI obj = entity.World.Api as ICoreClientAPI;
			IPlayer plr = (entity as EntityPlayer)?.Player;
			IPlayer obj2 = ((obj?.World.Player.PlayerUID == plr?.PlayerUID) ? plr : null);
			if (obj2 != null && obj2.ImmersiveFpMode)
			{
				UpperTorsoPose.degOffZ = degoffz * 0.3f;
				UpperTorsoPose.degOffY = degoffy * 0.2f;
				float offz = degoffz * 0.1f;
				LowerTorsoPose.degOffZ = offz;
				UpperFootRPose.degOffZ = 0f - offz;
				UpperFootLPose.degOffZ = 0f - offz;
			}
		}
	}
}
