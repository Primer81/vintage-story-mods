#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.Datastructures;

//
// Summary:
//     A fast implementation of IDictionary using arrays. Only suitable for small dictionaries,
//     typically 1-20 entries.
//     Note that Add is implemented differently from a standard Dictionary, it does
//     not check that the key is not already present (and does not throw an ArgumentException)
//
//     Additional methods AddIfNotPresent() and Clone() are provided for convenience.
//     There are also additional convenient constructors
//
// Type parameters:
//   TKey:
//
//   TValue:
public class FastSmallDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
    [CompilerGenerated]
    private sealed class _003CGetEnumerator_003Ed__27 : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
    {
        private int _003C_003E1__state;

        private KeyValuePair<TKey, TValue> _003C_003E2__current;

        public FastSmallDictionary<TKey, TValue> _003C_003E4__this;

        private int _003Ci_003E5__2;

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
        public _003CGetEnumerator_003Ed__27(int _003C_003E1__state)
        {
            this._003C_003E1__state = _003C_003E1__state;
        }

        [DebuggerHidden]
        void IDisposable.Dispose()
        {
            _003C_003E1__state = -2;
        }

        private bool MoveNext()
        {
            int num = _003C_003E1__state;
            FastSmallDictionary<TKey, TValue> fastSmallDictionary = _003C_003E4__this;
            switch (num)
            {
                default:
                    return false;
                case 0:
                    _003C_003E1__state = -1;
                    _003Ci_003E5__2 = 0;
                    break;
                case 1:
                    _003C_003E1__state = -1;
                    _003Ci_003E5__2++;
                    break;
            }

            if (_003Ci_003E5__2 < fastSmallDictionary.count)
            {
                _003C_003E2__current = new KeyValuePair<TKey, TValue>(fastSmallDictionary.keys[_003Ci_003E5__2], fastSmallDictionary.values[_003Ci_003E5__2]);
                _003C_003E1__state = 1;
                return true;
            }

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

    private TKey[] keys;

    private TValue[] values;

    private int count;

    public ICollection<TKey> Keys
    {
        get
        {
            TKey[] array = new TKey[count];
            Array.Copy(keys, array, count);
            return array;
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            TValue[] array = new TValue[count];
            Array.Copy(values, array, count);
            return array;
        }
    }

    int ICollection<KeyValuePair<TKey, TValue>>.Count => count;

    public bool IsReadOnly => false;

    public int Count => count;

    //
    // Summary:
    //     It is calling code's responsibility to ensure the key being searched for is not
    //     null
    public TValue this[TKey key]
    {
        get
        {
            for (int i = 0; i < keys.Length && i < count; i++)
            {
                ref TKey reference = ref key;
                TKey val = default(TKey);
                if (val == null)
                {
                    val = reference;
                    reference = ref val;
                }

                if (reference.Equals(keys[i]))
                {
                    return values[i];
                }
            }

            throw new KeyNotFoundException("The key " + key.ToString() + " was not found");
        }
        set
        {
            for (int i = 0; i < count; i++)
            {
                ref TKey reference = ref key;
                TKey val = default(TKey);
                if (val == null)
                {
                    val = reference;
                    reference = ref val;
                }

                if (reference.Equals(keys[i]))
                {
                    values[i] = value;
                    return;
                }
            }

            if (count == keys.Length)
            {
                ExpandArrays();
            }

            keys[count] = key;
            values[count++] = value;
        }
    }

    public FastSmallDictionary(int size)
    {
        keys = new TKey[size];
        values = new TValue[size];
    }

    public FastSmallDictionary(TKey key, TValue value)
        : this(1)
    {
        keys[0] = key;
        values[0] = value;
        count = 1;
    }

    public FastSmallDictionary(IDictionary<TKey, TValue> dict)
        : this(dict.Count)
    {
        foreach (KeyValuePair<TKey, TValue> item in dict)
        {
            Add(item);
        }
    }

    public FastSmallDictionary<TKey, TValue> Clone()
    {
        FastSmallDictionary<TKey, TValue> fastSmallDictionary = new FastSmallDictionary<TKey, TValue>(count);
        fastSmallDictionary.keys = new TKey[count];
        fastSmallDictionary.values = new TValue[count];
        fastSmallDictionary.count = count;
        Array.Copy(keys, fastSmallDictionary.keys, count);
        Array.Copy(values, fastSmallDictionary.values, count);
        return fastSmallDictionary;
    }

    public TKey GetFirstKey()
    {
        return keys[0];
    }

    public TValue TryGetValue(string key)
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            if (key.Equals(keys[i]))
            {
                return values[i];
            }
        }

        return default(TValue);
    }

    private void ExpandArrays()
    {
        int num = keys.Length + 3;
        TKey[] array = new TKey[num];
        TValue[] array2 = new TValue[num];
        for (int i = 0; i < keys.Length; i++)
        {
            array[i] = keys[i];
            array2[i] = values[i];
        }

        values = array2;
        keys = array;
    }

    public bool ContainsKey(TKey key)
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            ref TKey reference = ref key;
            TKey val = default(TKey);
            if (val == null)
            {
                val = reference;
                reference = ref val;
            }

            if (reference.Equals(keys[i]))
            {
                return true;
            }
        }

        return false;
    }

    public void Add(TKey key, TValue value)
    {
        if (count == keys.Length)
        {
            ExpandArrays();
        }

        keys[count] = key;
        values[count++] = value;
    }

    //
    // Summary:
    //     It is the calling code's responsibility to make sure that key is not null
    //
    // Parameters:
    //   key:
    //
    //   value:
    public bool TryGetValue(TKey key, out TValue value)
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            ref TKey reference = ref key;
            TKey val = default(TKey);
            if (val == null)
            {
                val = reference;
                reference = ref val;
            }

            if (reference.Equals(keys[i]))
            {
                value = values[i];
                return true;
            }
        }

        value = default(TValue);
        return false;
    }

    public void Clear()
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            keys[i] = default(TKey);
            values[i] = default(TValue);
        }

        count = 0;
    }

    [IteratorStateMachine(typeof(FastSmallDictionary<,>._003CGetEnumerator_003Ed__27))]
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        //yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
        return new _003CGetEnumerator_003Ed__27(0)
        {
            _003C_003E4__this = this
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    internal void AddIfNotPresent(TKey key, TValue value)
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            ref TKey reference = ref key;
            TKey val = default(TKey);
            if (val == null)
            {
                val = reference;
                reference = ref val;
            }

            if (reference.Equals(keys[i]))
            {
                return;
            }
        }

        Add(key, value);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            if (item.Key.Equals(keys[i]))
            {
                TValue val = values[i];
                if (item.Value == null)
                {
                    return val == null;
                }

                return item.Value.Equals(val);
            }
        }

        return false;
    }

    public bool Remove(TKey key)
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            ref TKey reference = ref key;
            TKey val = default(TKey);
            if (val == null)
            {
                val = reference;
                reference = ref val;
            }

            if (reference.Equals(keys[i]))
            {
                removeEntry(i);
                return true;
            }
        }

        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        for (int i = 0; i < keys.Length && i < count; i++)
        {
            if (!item.Key.Equals(keys[i]))
            {
                continue;
            }

            TValue val = values[i];
            if (item.Value == null)
            {
                if (val == null)
                {
                    removeEntry(i);
                    return true;
                }

                return false;
            }

            if (item.Value.Equals(val))
            {
                removeEntry(i);
                return true;
            }

            return false;
        }

        return false;
    }

    private void removeEntry(int index)
    {
        for (int i = index + 1; i < keys.Length && i < count; i++)
        {
            keys[i - 1] = keys[i];
            values[i - 1] = values[i];
        }

        count--;
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        KeyValuePair<TKey, TValue>[] array2 = new KeyValuePair<TKey, TValue>[count];
        for (int i = 0; i < count; i++)
        {
            array2[i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
        }

        Array.Copy(array2, 0, array, arrayIndex, count);
    }
}
#if false // Decompilation log
'182' items in cache
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
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
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
