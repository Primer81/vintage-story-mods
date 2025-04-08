namespace Vintagestory.API.Common;

/// <summary>
/// Return true if the action/event was successfull
/// </summary>
/// <returns></returns>
public delegate bool ActionBoolReturn();
/// <summary>
/// Returns true if the action/event was successfull.
/// </summary>
/// <typeparam name="T">The additional type to pass in.</typeparam>
/// <param name="t">The arguments for the event.</param>
/// <returns></returns>
public delegate bool ActionBoolReturn<T>(T t);
/// <summary>
/// Returns true if the action/event was successfull.
/// </summary>
/// <typeparam name="T1">The additional type to pass in.</typeparam>
/// <typeparam name="T2">The additional type to pass in.</typeparam>
/// <returns></returns>
public delegate bool ActionBoolReturn<T1, T2>(T1 t1, T2 t2);
/// <summary>
/// Returns true if the action/event was successfull.
/// </summary>
/// <typeparam name="T1">The additional type to pass in.</typeparam>
/// <typeparam name="T2">The additional type to pass in.</typeparam>
/// <typeparam name="T3">The additional type to pass in.</typeparam>
/// <returns></returns>
public delegate bool ActionBoolReturn<T1, T2, T3>(T1 t1, T2 t2, T3 t3);
