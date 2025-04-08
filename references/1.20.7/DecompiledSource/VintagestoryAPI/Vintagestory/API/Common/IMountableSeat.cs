using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Represents a seat of a mountable object.
/// </summary>
public interface IMountableSeat
{
	SeatConfig Config { get; set; }

	string SeatId { get; set; }

	long PassengerEntityIdForInit { get; set; }

	bool DoTeleportOnUnmount { get; set; }

	/// <summary>
	/// The entity behind this mountable supplier, if any
	/// </summary>
	Entity Entity { get; }

	/// <summary>
	/// The entity sitting on this seat
	/// </summary>
	Entity Passenger { get; }

	/// <summary>
	/// The supplier of this mount provider. e.g. the raft entity for the 2 raft seats
	/// </summary>
	IMountable MountSupplier { get; }

	/// <summary>
	/// If this "mountable seat" is the one that controls the mountable entity/block
	/// </summary>
	bool CanControl { get; }

	/// <summary>
	/// How the mounted entity should rotate
	/// </summary>
	EnumMountAngleMode AngleMode { get; }

	/// <summary>
	/// What animation the mounted entity should play
	/// </summary>
	AnimationMetaData SuggestedAnimation { get; }

	/// <summary>
	/// Whether or not the mount should play the idle anim
	/// </summary>
	bool SkipIdleAnimation { get; }

	float FpHandPitchFollow { get; }

	/// <summary>
	/// Where to place the first person camera
	/// </summary>
	Vec3f LocalEyePos { get; }

	/// <summary>
	/// Exact position of this seat
	/// </summary>
	EntityPos SeatPosition { get; }

	/// <summary>
	/// Transformation matrix that can be used to render the mounted entity at the right position. The transform is relative to the SeatPosition. May be null.
	/// </summary>
	Matrixf RenderTransform { get; }

	/// <summary>
	/// The control scheme of this seat
	/// </summary>
	EntityControls Controls { get; }

	/// <summary>
	/// When the entity unloads you should write whatever you need in here to reconstruct the IMountable after it's loaded again
	/// Reconstruct it by registering a mountable instancer through api.RegisterMountable(string className, GetMountableDelegate mountableInstancer)
	/// You must also set a string with key className, that is the same string that you used for RegisterMountable()
	/// </summary>
	/// <param name="tree"></param>
	void MountableToTreeAttributes(TreeAttribute tree);

	/// <summary>
	/// Called when the entity unmounted himself
	/// </summary>
	/// <param name="entityAgent"></param>
	void DidUnmount(EntityAgent entityAgent);

	/// <summary>
	/// Called when the entity mounted himself
	/// </summary>
	/// <param name="entityAgent"></param>
	void DidMount(EntityAgent entityAgent);

	/// <summary>
	/// Return true if the currently mounted entity can unmount (or if not mounted in the first place)
	/// </summary>
	/// <param name="entityAgent"></param>
	/// <returns></returns>
	bool CanUnmount(EntityAgent entityAgent);

	bool CanMount(EntityAgent entityAgent);
}
