namespace Vintagestory.API.Common;

/// <summary>
/// Return true if the action/event should be "consumed" (e.g. mark a mouse click as handled)
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="t1"></param>
/// <returns></returns>
public delegate bool ActionConsumable<T>(T t1);
/// <summary>
/// Return true if the action/event should be "consumed" (e.g. mark a mouse click as handled)
/// </summary>
/// <returns></returns>
public delegate bool ActionConsumable();
/// <summary>
/// Return true if the action/event should be "consumed" (e.g. mark a mouse click as handled)
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
/// <param name="t1"></param>
/// <param name="t2"></param>
/// <returns></returns>
public delegate bool ActionConsumable<T1, T2>(T1 t1, T2 t2);
