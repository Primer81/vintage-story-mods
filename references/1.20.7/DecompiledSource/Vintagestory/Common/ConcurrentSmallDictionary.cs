#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Vintagestory.Common;

//
// Summary:
//     Use like any IDictionary. Similar to a FastSmallDictionary, but this one is thread-safe
//     for simultaneous reads and writes - will not throw a ConcurrentModificationException
//
//     This also inherently behaves as an OrderedDictionary (though without the OrderedDictionary
//     extension methods such as ValuesOrdered, those can be added in future if required)
//
//     Low-lock: there is no lock or interlocked operation except when adding new keys
//     or when removing entries
//     Low-memory: and contains only a single null field, if it is empty
//     Two simultaneous writes, with the same key, at the same time, on different threads:
//     small chance of throwing an intentional ConcurrentModificationException if both
//     have the same keys, otherwise it's virtually impossible for us to preserve the
//     rule that the Dictionary holds exactly one entry per key
//
// Type parameters:
//   TKey:
//
//   TValue:
public class ConcurrentSmallDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
    [CompilerGenerated]
    private sealed class _003CGetEnumerator_003Ed__26 : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
    {
        private int _003C_003E1__state;

        private KeyValuePair<TKey, TValue> _003C_003E2__current;

        public ConcurrentSmallDictionary<TKey, TValue> _003C_003E4__this;

        private DTable<TKey, TValue> _003Ccontents_003E5__2;

        private int _003Cend_snapshot_003E5__3;

        private int _003Cpos_003E5__4;

        KeyValuePair<TKey, TValue> IEnumerator<KeyValuePair<TKey, TValue>>.Current
        {
            [DebuggerHidden]
            get
            {
                return _003C_003E2__current;
            }
        }

        object IEnumerator.Current
        {
            [DebuggerHidden]
            get
            {
                return _003C_003E2__current;
            }
        }

        [DebuggerHidden]
        public _003CGetEnumerator_003Ed__26(int _003C_003E1__state)
        {
            this._003C_003E1__state = _003C_003E1__state;
        }

        [DebuggerHidden]
        void IDisposable.Dispose()
        {
            _003Ccontents_003E5__2 = null;
            _003C_003E1__state = -2;
        }

        private bool MoveNext()
        {
            int num = _003C_003E1__state;
            ConcurrentSmallDictionary<TKey, TValue> concurrentSmallDictionary = _003C_003E4__this;
            if (num != 0)
            {
                if (num != 1)
                {
                    return false;
                }

                _003C_003E1__state = -1;
                _003Cpos_003E5__4++;
            }
            else
            {
                _003C_003E1__state = -1;
                _003Ccontents_003E5__2 = concurrentSmallDictionary.contents;
                if (_003Ccontents_003E5__2 == null)
                {
                    goto IL_00b6;
                }

                _003Cend_snapshot_003E5__3 = _003Ccontents_003E5__2.count;
                _003Cpos_003E5__4 = 0;
            }

            if (_003Cpos_003E5__4 < _003Cend_snapshot_003E5__3)
            {
                _003C_003E2__current = new KeyValuePair<TKey, TValue>(_003Ccontents_003E5__2.keys[_003Cpos_003E5__4], _003Ccontents_003E5__2.values[_003Cpos_003E5__4]);
                _003C_003E1__state = 1;
                return true;
            }

            goto IL_00b6;
        IL_00b6:
            return false;
        }

        bool IEnumerator.MoveNext()
        {
            //ILSpy generated this explicit interface implementation from .override directive in MoveNext
            return this.MoveNext();
        }

        [DebuggerHidden]
        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }

    private DTable<TKey, TValue> contents;

    public ICollection<TKey> Keys
    {
        get
        {
            DTable<TKey, TValue> dTable = contents;
            if (dTable != null)
            {
                return dTable.KeysCopy();
            }

            return new TKey[0];
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            DTable<TKey, TValue> dTable = contents;
            if (dTable != null)
            {
                return dTable.ValuesCopy();
            }

            return new TValue[0];
        }
    }

    public bool IsReadOnly => false;

    int ICollection<KeyValuePair<TKey, TValue>>.Count
    {
        get
        {
            DTable<TKey, TValue> dTable = contents;
            if (dTable != null)
            {
                return dTable.count;
            }

            return 0;
        }
    }

    //
    // Summary:
    //     Amount of entries currently in the Dictionary
    public int Count
    {
        get
        {
            DTable<TKey, TValue> dTable = contents;
            if (dTable != null)
            {
                return dTable.count;
            }

            return 0;
        }
    }

    public TValue this[TKey key]
    {
        get
        {
            return contents.GetValue(key);
        }
        set
        {
            Add(key, value);
        }
    }

    public ConcurrentSmallDictionary(int capacity)
    {
        if (capacity == 0)
        {
            contents = null;
        }
        else
        {
            contents = new DTable<TKey, TValue>(capacity);
        }
    }

    public ConcurrentSmallDictionary()
        : this(4)
    {
    }

    public bool IsEmpty()
    {
        DTable<TKey, TValue> dTable = contents;
        if (dTable != null)
        {
            return dTable.count == 0;
        }

        return true;
    }

    public void Add(TKey key, TValue value)
    {
        DTable<TKey, TValue> dTable = contents;
        if (dTable == null)
        {
            if (Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(key, value), dTable) != dTable)
            {
                Add(key, value);
            }
        }
        else if (!dTable.ReplaceIfKeyExists(key, value))
        {
            if (!dTable.Add(key, value) && Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(dTable, key, value), dTable) != dTable)
            {
                Add(key, value);
            }

            contents.DuplicateKeyCheck(key);
        }
    }

    public TValue TryGetValue(TKey key)
    {
        DTable<TKey, TValue> dTable = contents;
        if (dTable != null)
        {
            return dTable.TryGetValue(key);
        }

        return default(TValue);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        DTable<TKey, TValue> dTable = contents;
        if (dTable == null)
        {
            value = default(TValue);
            return false;
        }

        return dTable.TryGetValue(key, out value);
    }

    public bool ContainsKey(TKey key)
    {
        return contents?.ContainsKey(key) ?? false;
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (!contents.TryGetValue(item.Key, out var value))
        {
            return false;
        }

        return item.Value.Equals(value);
    }

    public bool Remove(TKey key)
    {
        DTable<TKey, TValue> dTable = contents;
        if (dTable == null)
        {
            return false;
        }

        int num = dTable.IndexOf(key);
        if (num < 0)
        {
            return false;
        }

        if (Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(dTable, num), dTable) != dTable)
        {
            return Remove(key);
        }

        return true;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        DTable<TKey, TValue> dTable = contents;
        if (dTable == null)
        {
            return false;
        }

        int num = dTable.IndexOf(item.Key, item.Value);
        if (num < 0)
        {
            return false;
        }

        if (Interlocked.CompareExchange(ref contents, new DTable<TKey, TValue>(dTable, num), dTable) != dTable)
        {
            return Remove(item);
        }

        return true;
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        contents.CopyTo(array, arrayIndex);
    }

    //
    // Summary:
    //     Threadsafe: but this might occasionally enumerate over a value which has, meanwhile,
    //     been removed from the Dictionary by a different thread Iterate over .Keys or
    //     .Values instead if an instantaneous snapshot is required (which will also therefore
    //     be a historical snapshot, if another thread meanwhile makes changes)
    [IteratorStateMachine(typeof(ConcurrentSmallDictionary<,>._003CGetEnumerator_003Ed__26))]
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        //yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
        return new _003CGetEnumerator_003Ed__26(0)
        {
            _003C_003E4__this = this
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    //
    // Summary:
    //     Wipes the contents and resets the count.
    public void Clear()
    {
        contents = null;
    }
}
#if false // Decompilation log
'180' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
