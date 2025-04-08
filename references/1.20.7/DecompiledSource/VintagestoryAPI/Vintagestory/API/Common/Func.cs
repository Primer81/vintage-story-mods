namespace Vintagestory.API.Common;

public delegate TResult Func<T1, TResult>(T1 t1);
public delegate TResult Func<T1, T2, TResult>(T1 t1, T2 t2);
public delegate TResult Func<T1, T2, T3, TResult>(T1 t1, T2 t2, T3 t3);
public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 t1, T2 t2, T3 t3, T4 t4);
public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
