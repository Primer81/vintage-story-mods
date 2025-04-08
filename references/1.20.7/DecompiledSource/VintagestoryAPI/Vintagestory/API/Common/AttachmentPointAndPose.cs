using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class AttachmentPointAndPose
{
	/// <summary>
	/// The current model matrix for this attachment point for this entity for the current animation frame.
	/// </summary>
	public float[] AnimModelMatrix;

	/// <summary>
	/// The pose shared across all entities using the same shape. Don't use. It's used internally for calculating the animation state. Once calculated, the value is copied over to AnimModelMatrix
	/// </summary>
	public ElementPose CachedPose;

	/// <summary>
	/// The attachment point
	/// </summary>
	public AttachmentPoint AttachPoint;

	public AttachmentPointAndPose()
	{
		AnimModelMatrix = Mat4f.Create();
	}

	public Matrixf Mul(Matrixf m)
	{
		AttachmentPoint ap = AttachPoint;
		m.Mul(AnimModelMatrix);
		m.Translate(ap.PosX / 16.0, ap.PosY / 16.0, ap.PosZ / 16.0);
		m.Translate(-0.5f, -0.5f, -0.5f);
		m.RotateX((float)ap.RotationX * ((float)Math.PI / 180f));
		m.RotateY((float)ap.RotationY * ((float)Math.PI / 180f));
		m.RotateZ((float)ap.RotationZ * ((float)Math.PI / 180f));
		m.Translate(0.5f, 0.5f, 0.5f);
		return m;
	}

	public Matrixf MulUncentered(Matrixf m)
	{
		AttachmentPoint ap = AttachPoint;
		m.Mul(AnimModelMatrix);
		m.Translate(ap.PosX / 16.0, ap.PosY / 16.0, ap.PosZ / 16.0);
		m.RotateX((float)ap.RotationX * ((float)Math.PI / 180f));
		m.RotateY((float)ap.RotationY * ((float)Math.PI / 180f));
		m.RotateZ((float)ap.RotationZ * ((float)Math.PI / 180f));
		return m;
	}
}
