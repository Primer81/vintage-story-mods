using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

/// <summary>
/// Called after a physics tick has happened
/// </summary>
/// <param name="accum">Amount of seconds left in the accumulator after physics ticking</param>
/// <param name="prevPos"></param>
public delegate void PhysicsTickDelegate(float accum, Vec3d prevPos);
