using System;
using System.IO;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// The available controls to move around a character in a game world
/// </summary>
public class EntityControls
{
	/// <summary>
	/// To execute a call handler registered by the engine. Don't use this one, use api.Input.InWorldAction instead.
	/// </summary>
	public OnEntityAction OnAction = delegate
	{
	};

	private bool[] flags = new bool[15];

	/// <summary>
	/// If true, the entity is either flying, gliding or swimming.
	/// </summary>
	public bool DetachedMode;

	/// <summary>
	/// If true, the entity has NoClip active.
	/// </summary>
	public bool NoClip;

	/// <summary>
	/// the axis lock for the fly plane.
	/// </summary>
	public EnumFreeMovAxisLock FlyPlaneLock;

	/// <summary>
	/// Current walking direction.
	/// </summary>
	public Vec3d WalkVector = new Vec3d();

	/// <summary>
	/// Current flying direction
	/// </summary>
	public Vec3d FlyVector = new Vec3d();

	/// <summary>
	/// Whether or not the entity is flying.
	/// </summary>
	public bool IsFlying;

	/// <summary>
	/// Whether or not the entity is climbing
	/// </summary>
	public bool IsClimbing;

	/// <summary>
	/// Whether or not the entity is aiming
	/// </summary>
	public bool IsAiming;

	/// <summary>
	/// Whether or not the entity is currently stepping up a block
	/// </summary>
	public bool IsStepping;

	/// <summary>
	/// If the player is currently using the currently held item in a special way (e.g. attacking with smithing hammer or eating an edible item)
	/// </summary>
	public EnumHandInteract HandUse;

	/// <summary>
	/// The block pos the player started using
	/// </summary>
	public BlockSelection HandUsingBlockSel;

	public int UsingCount;

	public long UsingBeginMS;

	public ModelTransform LeftUsingHeldItemTransformBefore;

	[Obsolete("Setting this value has no effect anymore. Add an animation to the seraph instead")]
	public ModelTransform UsingHeldItemTransformBefore;

	[Obsolete("Setting this value has no effect anymore. Add an animation to the seraph instead")]
	public ModelTransform UsingHeldItemTransformAfter;

	/// <summary>
	/// The movement speed multiplier.
	/// </summary>
	public float MovespeedMultiplier = 1f;

	/// <summary>
	/// Whether or not this entity is dirty.
	/// </summary>
	public bool Dirty;

	public double GlideSpeed;

	public bool[] Flags => flags;

	/// <summary>
	/// Checks to see if the entity is attempting to move in any direction (excluding jumping)
	/// </summary>
	public bool TriesToMove
	{
		get
		{
			if (!Forward && !Backward && !Left)
			{
				return Right;
			}
			return true;
		}
	}

	/// <summary>
	/// A check for if the entity is moving in the direction it's facing.
	/// </summary>
	public virtual bool Forward
	{
		get
		{
			return flags[0];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Forward, value);
		}
	}

	/// <summary>
	/// A check for if the entity is moving the opposite direction it's facing.
	/// </summary>
	public virtual bool Backward
	{
		get
		{
			return flags[1];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Backward, value);
		}
	}

	/// <summary>
	/// A check to see if the entity is moving left the direction it's facing.
	/// </summary>
	public virtual bool Left
	{
		get
		{
			return flags[2];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Left, value);
		}
	}

	/// <summary>
	/// A check to see if the entity is moving right the direction it's facing.
	/// </summary>
	public virtual bool Right
	{
		get
		{
			return flags[3];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Right, value);
		}
	}

	/// <summary>
	/// A check whether to see if the entity is jumping.
	/// </summary>
	public virtual bool Jump
	{
		get
		{
			return flags[4];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Jump, value);
		}
	}

	/// <summary>
	/// A check whether to see if the entity is sneaking. Use Controls.ShiftKey instead for mouse interaction modifiers, as it is a separable control.
	/// <br />A test for Sneak should be used only when we want to know whether the entity is crouching or using Sneak motion, which affects things like whether it is detectable by other entities, seen on the map, or how the shield is used
	/// </summary>
	public virtual bool Sneak
	{
		get
		{
			return flags[5];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Sneak, value);
		}
	}

	/// <summary>
	/// A check to see whether the entity is gliding
	/// </summary>
	public virtual bool Gliding
	{
		get
		{
			return flags[7];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Glide, value);
		}
	}

	/// <summary>
	/// A check to see whether the entity is sitting on the floor.
	/// </summary>
	public virtual bool FloorSitting
	{
		get
		{
			return flags[8];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.FloorSit, value);
		}
	}

	/// <summary>
	/// A check to see whether the entity is sprinting. Use Controls.CtrlKey instead for mouse interaction modifiers, as it is a separable control.
	/// <br />A test for Sprint should be used only when we want to know whether the entity is sprinting.
	/// </summary>
	public virtual bool Sprint
	{
		get
		{
			return flags[6];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Sprint, value);
		}
	}

	/// <summary>
	/// A check to see whether the entity is moving up.
	/// </summary>
	public virtual bool Up
	{
		get
		{
			return flags[11];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Up, value);
		}
	}

	/// <summary>
	/// A check to see whether the entity is moving down.
	/// </summary>
	public virtual bool Down
	{
		get
		{
			return flags[12];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.Down, value);
		}
	}

	/// <summary>
	/// A check to see if the entity is holding the in-world rleft mouse button down.
	/// </summary>
	public virtual bool LeftMouseDown
	{
		get
		{
			return flags[9];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.LeftMouseDown, value);
		}
	}

	/// <summary>
	/// A check to see if the entity is holding the in-world right mouse button down.
	/// </summary>
	public virtual bool RightMouseDown
	{
		get
		{
			return flags[10];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.RightMouseDown, value);
		}
	}

	/// <summary>
	/// A check to see if the entity is holding down the Ctrl key (which may be the same as the Sprint key or one or other may have been remapped).
	/// <br />Should normally be used in conjunction with a mouse button, including OnHeldInteractStart() methods etc
	/// </summary>
	public virtual bool CtrlKey
	{
		get
		{
			return flags[13];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.CtrlKey, value);
		}
	}

	/// <summary>
	/// A check to see if the entity is holding down the Shift key (which may be the same as the Sneak key or one or other may have been remapped).
	/// <br />Should normally be used in conjunction with a mouse button, including OnHeldInteractStart() methods etc
	/// </summary>
	public virtual bool ShiftKey
	{
		get
		{
			return flags[14];
		}
		set
		{
			AttemptToggleAction(EnumEntityAction.ShiftKey, value);
		}
	}

	public virtual bool this[EnumEntityAction action]
	{
		get
		{
			return flags[(int)action];
		}
		set
		{
			flags[(int)action] = value;
		}
	}

	protected virtual void AttemptToggleAction(EnumEntityAction action, bool on)
	{
		if (flags[(int)action] != on)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			OnAction(action, on, ref handling);
			if (handling == EnumHandling.PassThrough)
			{
				flags[(int)action] = on;
				Dirty = true;
			}
		}
	}

	/// <summary>
	/// Calculates the movement vectors for the player.
	/// </summary>
	/// <param name="pos">The position of the player.</param>
	/// <param name="dt">The change in time.</param>
	public virtual void CalcMovementVectors(EntityPos pos, float dt)
	{
		double moveSpeed = dt * GlobalConstants.BaseMoveSpeed * MovespeedMultiplier * GlobalConstants.OverallSpeedMultiplier;
		double dz = (Forward ? moveSpeed : 0.0) + (Backward ? (0.0 - moveSpeed) : 0.0);
		double dx = (Right ? (0.0 - moveSpeed) : 0.0) + (Left ? moveSpeed : 0.0);
		double cosPitch = Math.Cos(pos.Pitch);
		double sinPitch = Math.Sin(pos.Pitch);
		double cosYaw = Math.Cos(0f - pos.Yaw);
		double sinYaw = Math.Sin(0f - pos.Yaw);
		WalkVector.Set(dx * cosYaw - dz * sinYaw, 0.0, dx * sinYaw + dz * cosYaw);
		if (FlyPlaneLock == EnumFreeMovAxisLock.Y)
		{
			cosPitch = -1.0;
		}
		FlyVector.Set(dx * cosYaw + dz * cosPitch * sinYaw, dz * sinPitch, dx * sinYaw - dz * cosPitch * cosYaw);
		double normalization = (((Forward || Backward) && (Right || Left)) ? (1.0 / Math.Sqrt(2.0)) : 1.0);
		WalkVector.Mul(normalization);
		if (FlyPlaneLock == EnumFreeMovAxisLock.X)
		{
			FlyVector.X = 0.0;
		}
		if (FlyPlaneLock == EnumFreeMovAxisLock.Y)
		{
			FlyVector.Y = 0.0;
		}
		if (FlyPlaneLock == EnumFreeMovAxisLock.Z)
		{
			FlyVector.Z = 0.0;
		}
	}

	/// <summary>
	/// Copies the controls from the provided controls to this set of controls.
	/// </summary>
	/// <param name="controls">The controls to copy over.</param>
	public virtual void SetFrom(EntityControls controls)
	{
		for (int i = 0; i < controls.flags.Length; i++)
		{
			flags[i] = controls.flags[i];
		}
		DetachedMode = controls.DetachedMode;
		FlyPlaneLock = controls.FlyPlaneLock;
		IsFlying = controls.IsFlying;
		NoClip = controls.NoClip;
	}

	/// <summary>
	/// Updates the data from the packet.
	/// </summary>
	/// <param name="pressed">Whether or not the key was pressed.</param>
	/// <param name="action">the id of the key that was pressed.</param>
	public virtual void UpdateFromPacket(bool pressed, int action)
	{
		if (flags[action] != pressed)
		{
			AttemptToggleAction((EnumEntityAction)action, pressed);
		}
	}

	/// <summary>
	/// Forces the entity to stop all movements, resets all flags to false
	/// </summary>
	public virtual void StopAllMovement()
	{
		for (int i = 0; i < flags.Length; i++)
		{
			flags[i] = false;
		}
	}

	/// <summary>
	/// Converts the values to a single int flag.
	/// </summary>
	/// <returns>the compressed integer.</returns>
	public virtual int ToInt()
	{
		return (Forward ? 1 : 0) | (Backward ? 2 : 0) | (Left ? 4 : 0) | (Right ? 8 : 0) | (Jump ? 16 : 0) | (Sneak ? 32 : 0) | (Sprint ? 64 : 0) | (Up ? 128 : 0) | (Down ? 256 : 0) | (flags[7] ? 512 : 0) | (flags[8] ? 1024 : 0) | (flags[9] ? 2048 : 0) | (flags[10] ? 4096 : 0) | (IsClimbing ? 8192 : 0) | (flags[13] ? 16384 : 0) | (flags[14] ? 32768 : 0);
	}

	/// <summary>
	/// Converts the int flags to movement controls.
	/// </summary>
	/// <param name="flagsInt">The compressed integer.</param>
	public virtual void FromInt(int flagsInt)
	{
		Forward = (flagsInt & 1) > 0;
		Backward = (flagsInt & 2) > 0;
		Left = (flagsInt & 4) > 0;
		Right = (flagsInt & 8) > 0;
		Jump = (flagsInt & 0x10) > 0;
		Sneak = (flagsInt & 0x20) > 0;
		Sprint = (flagsInt & 0x40) > 0;
		Up = (flagsInt & 0x80) > 0;
		Down = (flagsInt & 0x100) > 0;
		flags[7] = (flagsInt & 0x200) > 0;
		flags[8] = (flagsInt & 0x400) > 0;
		flags[9] = (flagsInt & 0x800) > 0;
		flags[10] = (flagsInt & 0x1000) > 0;
		IsClimbing = (flagsInt & 0x2000) > 0;
		flags[13] = (flagsInt & 0x4000) > 0;
		flags[14] = (flagsInt & 0x8000) > 0;
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
		writer.Write(ToInt());
	}

	public virtual void FromBytes(BinaryReader reader, bool ignoreData)
	{
		int flags = reader.ReadInt32();
		if (!ignoreData)
		{
			FromInt(flags);
		}
	}
}
