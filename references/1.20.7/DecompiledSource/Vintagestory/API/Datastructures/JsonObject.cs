#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Datastructures;

//
// Summary:
//     Elegant, yet somewhat inefficently designed (because wasteful with heap objects)
//     wrapper class to abstract away the type-casting nightmare of JToken O.O
public class JsonObject : IReadOnlyCollection<JsonObject>, IEnumerable<JsonObject>, IEnumerable
{
    [CompilerGenerated]
    private sealed class _003CGetEnumerator_003Ed__36 : IEnumerator<JsonObject>, IEnumerator, IDisposable
    {
        private int _003C_003E1__state;

        private JsonObject _003C_003E2__current;

        public JsonObject _003C_003E4__this;

        private IEnumerator<KeyValuePair<string, JToken?>> _003C_003E7__wrap1;

        private JArray _003Cjarr_003E5__3;

        private int _003Ci_003E5__4;

        JsonObject IEnumerator<JsonObject>.Current
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
        public _003CGetEnumerator_003Ed__36(int _003C_003E1__state)
        {
            this._003C_003E1__state = _003C_003E1__state;
        }

        [DebuggerHidden]
        void IDisposable.Dispose()
        {
            int num = _003C_003E1__state;
            if (num == -3 || num == 1)
            {
                try
                {
                }
                finally
                {
                    _003C_003Em__Finally1();
                }
            }

            _003C_003E7__wrap1 = null;
            _003Cjarr_003E5__3 = null;
            _003C_003E1__state = -2;
        }

        private bool MoveNext()
        {
            try
            {
                int num = _003C_003E1__state;
                JsonObject jsonObject = _003C_003E4__this;
                switch (num)
                {
                    default:
                        return false;
                    case 0:
                        {
                            _003C_003E1__state = -1;
                            if (jsonObject.token == null)
                            {
                                throw new InvalidOperationException("Cannot iterate over a null token");
                            }

                            if (jsonObject.token is JObject jObject)
                            {
                                _003C_003E7__wrap1 = jObject.GetEnumerator();
                                _003C_003E1__state = -3;
                                goto IL_00a0;
                            }

                            JToken token = jsonObject.token;
                            _003Cjarr_003E5__3 = token as JArray;
                            if (_003Cjarr_003E5__3 != null)
                            {
                                _003Ci_003E5__4 = 0;
                                goto IL_0125;
                            }

                            throw new InvalidOperationException("Can iterate only over a JObject or JArray, this token is of type " + jsonObject.token.Type);
                        }
                    case 1:
                        _003C_003E1__state = -3;
                        goto IL_00a0;
                    case 2:
                        {
                            _003C_003E1__state = -1;
                            _003Ci_003E5__4++;
                            goto IL_0125;
                        }

                    IL_00a0:
                        if (_003C_003E7__wrap1.MoveNext())
                        {
                            _003C_003E2__current = new JsonObject(_003C_003E7__wrap1.Current.Key);
                            _003C_003E1__state = 1;
                            return true;
                        }

                        _003C_003Em__Finally1();
                        _003C_003E7__wrap1 = null;
                        break;
                    IL_0125:
                        if (_003Ci_003E5__4 < jsonObject.Count)
                        {
                            _003C_003E2__current = new JsonObject(_003Cjarr_003E5__3[_003Ci_003E5__4]);
                            _003C_003E1__state = 2;
                            return true;
                        }

                        _003Cjarr_003E5__3 = null;
                        break;
                }

                return false;
            }
            catch
            {
                //try-fault
                ((IDisposable)this).Dispose();
                throw;
            }
        }

        bool IEnumerator.MoveNext()
        {
            //ILSpy generated this explicit interface implementation from .override directive in MoveNext
            return this.MoveNext();
        }

        private void _003C_003Em__Finally1()
        {
            _003C_003E1__state = -1;
            if (_003C_003E7__wrap1 != null)
            {
                _003C_003E7__wrap1.Dispose();
            }
        }

        [DebuggerHidden]
        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }

    private JToken token;

    //
    // Summary:
    //     Access a tokens element with given key
    //
    // Parameters:
    //   key:
    public JsonObject this[string key]
    {
        get
        {
            if (token == null || !(token is JObject))
            {
                return new JsonObject(null);
            }

            (token as JObject).TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken value);
            return new JsonObject(value);
        }
    }

    //
    // Summary:
    //     True if the token is not null
    public bool Exists => token != null;

    public virtual JToken Token
    {
        get
        {
            return token;
        }
        set
        {
            token = value;
        }
    }

    public int Count
    {
        get
        {
            if (token == null)
            {
                throw new InvalidOperationException("Cannot count a null token");
            }

            if (token is JObject jObject)
            {
                return jObject.Count;
            }

            if (token is JArray jArray)
            {
                return jArray.Count;
            }

            throw new InvalidOperationException("Can iterate only over a JObject or JArray, this token is of type " + token.Type);
        }
    }

    public static JsonObject FromJson(string jsonCode)
    {
        return new JsonObject(JToken.Parse(jsonCode));
    }

    //
    // Summary:
    //     Create a new instance of a JsonObject
    //
    // Parameters:
    //   token:
    public JsonObject(JToken token)
    {
        this.token = token;
    }

    //
    // Summary:
    //     Create a new instance of a JsonObject
    //
    // Parameters:
    //   original:
    //
    //   unused:
    //     Only present so that the Constructor with a sole null parameter has an unambiguous
    //     signature
    public JsonObject(JsonObject original, bool unused)
    {
        token = original.token;
    }

    //
    // Summary:
    //     True if the token has an element with given key
    //
    // Parameters:
    //   key:
    public bool KeyExists(string key)
    {
        return token[key] != null;
    }

    //
    // Summary:
    //     Deserialize the token to an object of the specified type T
    //
    // Type parameters:
    //   T:
    public T AsObject<T>(T defaultValue = default(T))
    {
        JsonSerializerSettings settings = null;
        if (token != null)
        {
            return JsonConvert.DeserializeObject<T>(token.ToString(), settings);
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Deserialize the token to an object of the specified type T, with the specified
    //     domain for any AssetLocation which needs to be parsed
    //
    // Type parameters:
    //   T:
    public T AsObject<T>(T defaultValue, string domain)
    {
        JsonSerializerSettings jsonSerializerSettings = null;
        if (domain != "game")
        {
            jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(new AssetLocationJsonParser(domain));
        }

        if (token != null)
        {
            return JsonConvert.DeserializeObject<T>(token.ToString(), jsonSerializerSettings);
        }

        return defaultValue;
    }

    public T AsObject<T>(JsonSerializerSettings settings, T defaultValue, string domain = "game")
    {
        if (domain != "game")
        {
            if (settings == null)
            {
                settings = new JsonSerializerSettings();
            }

            settings.Converters.Add(new AssetLocationJsonParser(domain));
        }

        if (token != null)
        {
            return JsonConvert.DeserializeObject<T>(token.ToString(), settings);
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Turn the token into an array of JsonObjects
    public JsonObject[] AsArray()
    {
        if (!(token is JArray))
        {
            return null;
        }

        JArray jArray = (JArray)token;
        JsonObject[] array = new JsonObject[jArray.Count];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = new JsonObject(jArray[i]);
        }

        return array;
    }

    //
    // Summary:
    //     Turn the token into a string
    //
    // Parameters:
    //   defaultValue:
    //     If the conversion fails, this value is used instead
    public string AsString(string defaultValue = null)
    {
        return GetValue(defaultValue);
    }

    [Obsolete("Use AsArray<string>() instead")]
    public string[] AsStringArray(string[] defaultValue = null, string defaultDomain = null)
    {
        return AsArray(defaultValue, defaultDomain);
    }

    [Obsolete("Use AsArray<float>() instead")]
    public float[] AsFloatArray(float[] defaultValue = null)
    {
        return AsArray(defaultValue);
    }

    //
    // Summary:
    //     Turn the token into an array
    //
    // Parameters:
    //   defaultValue:
    //     If the conversion fails, this value is used instead
    //
    //   defaultDomain:
    public T[] AsArray<T>(T[] defaultValue = null, string defaultDomain = null)
    {
        if (!(token is JArray))
        {
            return defaultValue;
        }

        JArray jArray = (JArray)token;
        T[] array = new T[jArray.Count];
        for (int i = 0; i < array.Length; i++)
        {
            JToken jToken = jArray[i];
            if (jToken is JValue || jToken is JObject)
            {
                array[i] = jToken.ToObject<T>(defaultDomain);
                continue;
            }

            return defaultValue;
        }

        return array;
    }

    //
    // Summary:
    //     Turn the token into a boolean
    //
    // Parameters:
    //   defaultValue:
    //     If the conversion fails, this value is used instead
    public bool AsBool(bool defaultValue = false)
    {
        if (!(token is JValue))
        {
            return defaultValue;
        }

        object value = ((JValue)token).Value;
        if (value is bool)
        {
            return (bool)value;
        }

        if (value is string)
        {
            if (!bool.TryParse(value?.ToString() ?? "", out var result))
            {
                return defaultValue;
            }

            return result;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Turn the token into an integer
    //
    // Parameters:
    //   defaultValue:
    //     If the conversion fails, this value is used instead
    public int AsInt(int defaultValue = 0)
    {
        if (!(token is JValue))
        {
            return defaultValue;
        }

        object value = ((JValue)token).Value;
        if (value is long)
        {
            return (int)(long)value;
        }

        if (value is int)
        {
            return (int)value;
        }

        if (value is float)
        {
            return (int)(float)value;
        }

        if (value is double)
        {
            return (int)(double)value;
        }

        if (value is string)
        {
            if (!int.TryParse(value?.ToString() ?? "", out var result))
            {
                return defaultValue;
            }

            return result;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Turn the token into a float
    //
    // Parameters:
    //   defaultValue:
    //     If the conversion fails, this value is used instead
    public float AsFloat(float defaultValue = 0f)
    {
        if (!(token is JValue))
        {
            return defaultValue;
        }

        object value = ((JValue)token).Value;
        if (value is int)
        {
            return (int)value;
        }

        if (value is float)
        {
            return (float)value;
        }

        if (value is long)
        {
            return (long)value;
        }

        if (value is double)
        {
            return (float)(double)value;
        }

        if (value is string)
        {
            if (!float.TryParse(value?.ToString() ?? "", NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
            {
                return defaultValue;
            }

            return result;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Turn the token into a double
    //
    // Parameters:
    //   defaultValue:
    //     If the conversion fails, this value is used instead
    public double AsDouble(double defaultValue = 0.0)
    {
        if (!(token is JValue))
        {
            return defaultValue;
        }

        object value = ((JValue)token).Value;
        if (value is int)
        {
            return (int)value;
        }

        if (value is long)
        {
            return (long)value;
        }

        if (value is double)
        {
            return (double)value;
        }

        if (value is string)
        {
            if (!double.TryParse(value?.ToString() ?? "", out var result))
            {
                return defaultValue;
            }

            return result;
        }

        return defaultValue;
    }

    private T GetValue<T>(T defaultValue = default(T))
    {
        if (!(token is JValue))
        {
            return defaultValue;
        }

        if (!(((JValue)token).Value is T))
        {
            return defaultValue;
        }

        return token.ToObject<T>();
    }

    //
    // Summary:
    //     Calls token.ToString()
    public override string ToString()
    {
        return (token?.ToString()).DeDuplicate();
    }

    //
    // Summary:
    //     True if the token is a JArray
    public bool IsArray()
    {
        return token is JArray;
    }

    //
    // Summary:
    //     Turns the token into an IAttribute with all its child elements, if it has any.
    //
    //     Note: If you converting this to a tree attribute, a subsequent call to tree.GetInt()
    //     might not work because Newtonsoft.JSON seems to load integers as long, so use
    //     GetDecimal() or GetLong() instead. Similar things might happen with float<->double
    public IAttribute ToAttribute()
    {
        return ToAttribute(token);
    }

    public virtual void FillPlaceHolder(string key, string value)
    {
        FillPlaceHolder(token, key, value);
    }

    internal static void FillPlaceHolder(JToken token, string key, string value)
    {
        if (token is JValue jValue && jValue.Value is string)
        {
            jValue.Value = (jValue.Value as string).Replace("{" + key + "}", value);
        }

        if (token is JArray jArray)
        {
            foreach (JToken item in jArray)
            {
                FillPlaceHolder(item, key, value);
            }
        }

        if (!(token is JObject jObject))
        {
            return;
        }

        foreach (KeyValuePair<string, JToken> item2 in jObject)
        {
            FillPlaceHolder(item2.Value, key, value);
        }
    }

    private static IAttribute ToAttribute(JToken token)
    {
        if (token is JValue jValue)
        {
            if (jValue.Value is int)
            {
                return new IntAttribute((int)jValue.Value);
            }

            if (jValue.Value is long)
            {
                return new LongAttribute((long)jValue.Value);
            }

            if (jValue.Value is float)
            {
                return new FloatAttribute((float)jValue.Value);
            }

            if (jValue.Value is double)
            {
                return new DoubleAttribute((double)jValue.Value);
            }

            if (jValue.Value is bool)
            {
                return new BoolAttribute((bool)jValue.Value);
            }

            if (jValue.Value is string)
            {
                return new StringAttribute((string)jValue.Value);
            }
        }

        if (token is JObject jObject)
        {
            TreeAttribute treeAttribute = new TreeAttribute();
            {
                foreach (KeyValuePair<string, JToken> item in jObject)
                {
                    treeAttribute[item.Key] = ToAttribute(item.Value);
                }

                return treeAttribute;
            }
        }

        if (token is JArray jArray)
        {
            if (!jArray.HasValues)
            {
                return new TreeArrayAttribute(new TreeAttribute[0]);
            }

            if (jArray[0] is JValue jValue2)
            {
                if (jValue2.Value is int)
                {
                    return new IntArrayAttribute(ToPrimitiveArray<int>(jArray));
                }

                if (jValue2.Value is long)
                {
                    return new LongArrayAttribute(ToPrimitiveArray<long>(jArray));
                }

                if (jValue2.Value is float)
                {
                    return new FloatArrayAttribute(ToPrimitiveArray<float>(jArray));
                }

                if (jValue2.Value is double)
                {
                    return new DoubleArrayAttribute(ToPrimitiveArray<double>(jArray));
                }

                if (jValue2.Value is bool)
                {
                    return new BoolArrayAttribute(ToPrimitiveArray<bool>(jArray));
                }

                if (jValue2.Value is string)
                {
                    return new StringArrayAttribute(ToPrimitiveArray<string>(jArray));
                }

                return null;
            }

            TreeAttribute[] array = new TreeAttribute[jArray.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (TreeAttribute)ToAttribute(jArray[i]);
            }

            return new TreeArrayAttribute(array);
        }

        return null;
    }

    //
    // Summary:
    //     Turn a JArray into a primitive array
    //
    // Parameters:
    //   array:
    //
    // Type parameters:
    //   T:
    public static T[] ToPrimitiveArray<T>(JArray array)
    {
        T[] array2 = new T[array.Count];
        for (int i = 0; i < array2.Length; i++)
        {
            _ = array[i];
            array2[i] = array[i].ToObject<T>();
        }

        return array2;
    }

    //
    // Summary:
    //     Returns a deep clone
    public JsonObject Clone()
    {
        return new JsonObject(token.DeepClone());
    }

    //
    // Summary:
    //     Returns true if this object has the named bool attribute, and it is true
    public bool IsTrue(string attrName)
    {
        if (token == null || !(token is JObject))
        {
            return false;
        }

        if (token[attrName] is JValue jValue)
        {
            object value = jValue.Value;
            if (value is bool)
            {
                return (bool)value;
            }

            if (jValue.Value is string value2)
            {
                bool.TryParse(value2, out var result);
                return result;
            }
        }

        return false;
    }

    [IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__36))]
    public IEnumerator<JsonObject> GetEnumerator()
    {
        //yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
        return new _003CGetEnumerator_003Ed__36(0)
        {
            _003C_003E4__this = this
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
