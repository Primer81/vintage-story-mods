#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

//
// Summary:
//     Same as your normal C# Dictionary but ensures that the order in which the items
//     are added is remembered. That way you can iterate over the dictionary with the
//     insert order intact or set/get elements by index. Taken from http://www.codeproject.com/Articles/18615/OrderedDictionary-T-A-generic-implementation-of-IO
//     Please be aware that this is not a very efficient implementation, recommed use
//     only for small sets of data.
//
// Type parameters:
//   TKey:
//
//   TValue:
[ProtoContract]
public class OrderedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>
{
    private static readonly string _keyTypeName = typeof(TKey).FullName;

    private static readonly string _valueTypeName = typeof(TValue).FullName;

    private static readonly bool _valueTypeIsReferenceType = !typeof(ValueType).IsAssignableFrom(typeof(TValue));

    private Dictionary<TKey, TValue> _dictionary;

    private List<KeyValuePair<TKey, TValue>> _list;

    private IEqualityComparer<TKey> _comparer;

    private int _initialCapacity;

    [ProtoMember(1)]
    private List<KeyValuePair<TKey, TValue>> listSer;

    public Dictionary<TKey, TValue> InternalDictionary => Dictionary;

    private Dictionary<TKey, TValue> Dictionary
    {
        get
        {
            if (_dictionary == null)
            {
                _dictionary = new Dictionary<TKey, TValue>(_initialCapacity, _comparer);
            }

            return _dictionary;
        }
    }

    private List<KeyValuePair<TKey, TValue>> List
    {
        get
        {
            if (_list == null)
            {
                _list = new List<KeyValuePair<TKey, TValue>>(_initialCapacity);
            }

            return _list;
        }
    }

    public TValue this[TKey key]
    {
        get
        {
            return Dictionary[key];
        }
        set
        {
            if (Dictionary.ContainsKey(key))
            {
                Dictionary[key] = value;
                List[IndexOfKey(key)] = new KeyValuePair<TKey, TValue>(key, value);
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public int Count => List.Count;

    public ICollection<TKey> Keys => Dictionary.Keys;

    public IEnumerable<TValue> ValuesOrdered => List.Select((KeyValuePair<TKey, TValue> val) => val.Value);

    //
    // Summary:
    //     Important: these are NOT ordered. The ordered values is obtained by .ValuesOrdered
    public ICollection<TValue> Values => Dictionary.Values;

    public bool IsReadOnly => false;

    public OrderedDictionary()
        : this(0, (IEqualityComparer<TKey>)null)
    {
    }

    public OrderedDictionary(int capacity)
        : this(capacity, (IEqualityComparer<TKey>)null)
    {
    }

    public OrderedDictionary(IEqualityComparer<TKey> comparer)
        : this(0, comparer)
    {
    }

    public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer)
    {
        if (0 > capacity)
        {
            throw new ArgumentOutOfRangeException("capacity", "'capacity' must be non-negative");
        }

        _initialCapacity = capacity;
        _comparer = comparer;
    }

    public OrderedDictionary(OrderedDictionary<TKey, TValue> initialData)
    {
        foreach (KeyValuePair<TKey, TValue> initialDatum in initialData)
        {
            this[initialDatum.Key] = initialDatum.Value;
        }
    }

    [OnDeserialized]
    protected void OnDeserializedMethod(StreamingContext context)
    {
        if (listSer == null)
        {
            return;
        }

        foreach (KeyValuePair<TKey, TValue> item in listSer)
        {
            this[item.Key] = item.Value;
        }

        listSer = null;
    }

    [ProtoBeforeSerialization]
    protected void BeforeSerialization()
    {
        listSer = new List<KeyValuePair<TKey, TValue>>();
        listSer.AddRange(_list);
    }

    public OrderedDictionary(Dictionary<TKey, TValue> initialData)
    {
        foreach (KeyValuePair<TKey, TValue> initialDatum in initialData)
        {
            this[initialDatum.Key] = initialDatum.Value;
        }
    }

    public void Insert(int index, TKey key, TValue value)
    {
        if (index > Count || index < 0)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        Dictionary.Add(key, value);
        List.Insert(index, new KeyValuePair<TKey, TValue>(key, value));
    }

    public void InsertBefore(TKey Atkey, TKey key, TValue value)
    {
        for (int i = 0; i < List.Count; i++)
        {
            if (List[i].Key.Equals(Atkey))
            {
                Insert(i, key, value);
                break;
            }
        }
    }

    public void RemoveAt(int index)
    {
        if (index >= Count || index < 0)
        {
            throw new ArgumentOutOfRangeException("index", "'index' must be non-negative and less than the size of the collection");
        }

        TKey key = List[index].Key;
        List.RemoveAt(index);
        Dictionary.Remove(key);
    }

    public TValue GetValueAtIndex(int index)
    {
        return List[index].Value;
    }

    public TKey GetKeyAtIndex(int index)
    {
        return List[index].Key;
    }

    public void SetAtIndex(int index, TValue value)
    {
        if (index >= Count || index < 0)
        {
            throw new ArgumentOutOfRangeException("index", "'index' must be non-negative and less than the size of the collection");
        }

        TKey key = List[index].Key;
        List[index] = new KeyValuePair<TKey, TValue>(key, value);
        Dictionary[key] = value;
    }

    public int Add(TKey key, TValue value)
    {
        Dictionary.Add(key, value);
        List.Add(new KeyValuePair<TKey, TValue>(key, value));
        return Count - 1;
    }

    public void Clear()
    {
        Dictionary.Clear();
        List.Clear();
    }

    public bool ContainsKey(TKey key)
    {
        return Dictionary.ContainsKey(key);
    }

    //
    // Summary:
    //     Returns -1 if the key was not found
    //
    // Parameters:
    //   key:
    public int IndexOfKey(TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        for (int i = 0; i < List.Count; i++)
        {
            TKey key2 = List[i].Key;
            if (_comparer != null)
            {
                if (_comparer.Equals(key2, key))
                {
                    return i;
                }
            }
            else if (key2.Equals(key))
            {
                return i;
            }
        }

        return -1;
    }

    public bool Remove(TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        int num = IndexOfKey(key);
        if (num >= 0 && Dictionary.Remove(key))
        {
            List.RemoveAt(num);
            return true;
        }

        return false;
    }

    private void CopyTo(Array array, int index)
    {
        ((ICollection)List).CopyTo(array, index);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return Dictionary.TryGetValue(key, out value);
    }

    public TValue TryGetValue(TKey key)
    {
        Dictionary.TryGetValue(key, out var value);
        return value;
    }

    private void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    private bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).Contains(item);
    }

    private void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)Dictionary).CopyTo(array, arrayIndex);
    }

    private bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return List.GetEnumerator();
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return List.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return List.GetEnumerator();
    }

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
    {
        Add(key, value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        return ContainsKey(item.Key);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        int num = 0;
        foreach (KeyValuePair<TKey, TValue> item in _dictionary)
        {
            array[arrayIndex + num] = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
            num++;
        }
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    public bool ContainsValue(TValue value)
    {
        return Dictionary.ContainsValue(value);
    }

    //
    // Summary:
    //     Adds values, but for performance does not replace any existing values which have
    //     the same key; that's fine for asset loading
    //
    // Parameters:
    //   src:
    //
    //   logger:
    internal void AddRange(Dictionary<TKey, TValue> src, ILogger logger)
    {
        foreach (KeyValuePair<TKey, TValue> item in src)
        {
            TKey key = item.Key;
            if (!Dictionary.ContainsKey(key))
            {
                Dictionary.Add(key, item.Value);
                List.Add(item);
            }
        }
    }
}
#if false // Decompilation log
'181' items in cache
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
