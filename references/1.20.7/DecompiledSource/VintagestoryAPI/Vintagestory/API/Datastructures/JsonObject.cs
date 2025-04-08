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

/// <summary>
/// Elegant, yet somewhat inefficently designed (because wasteful with heap objects) wrapper class to abstract away the type-casting nightmare of JToken O.O
/// </summary>
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
					if (jsonObject.token is JObject jobj)
					{
						_003C_003E7__wrap1 = jobj.GetEnumerator();
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

	/// <summary>
	/// Access a tokens element with given key
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
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

	/// <summary>
	/// True if the token is not null
	/// </summary>
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
			if (token is JObject jobj)
			{
				return jobj.Count;
			}
			if (token is JArray jarr)
			{
				return jarr.Count;
			}
			throw new InvalidOperationException("Can iterate only over a JObject or JArray, this token is of type " + token.Type);
		}
	}

	public static JsonObject FromJson(string jsonCode)
	{
		return new JsonObject(JToken.Parse(jsonCode));
	}

	/// <summary>
	/// Create a new instance of a JsonObject
	/// </summary>
	/// <param name="token"></param>
	public JsonObject(JToken token)
	{
		this.token = token;
	}

	/// <summary>
	/// Create a new instance of a JsonObject
	/// </summary>
	/// <param name="original"></param>
	/// <param name="unused">Only present so that the Constructor with a sole null parameter has an unambiguous signature</param>
	public JsonObject(JsonObject original, bool unused)
	{
		token = original.token;
	}

	/// <summary>
	/// True if the token has an element with given key
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public bool KeyExists(string key)
	{
		return token[key] != null;
	}

	/// <summary>
	/// Deserialize the token to an object of the specified type T
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public T AsObject<T>(T defaultValue = default(T))
	{
		JsonSerializerSettings settings = null;
		if (token != null)
		{
			return JsonConvert.DeserializeObject<T>(token.ToString(), settings);
		}
		return defaultValue;
	}

	/// <summary>
	/// Deserialize the token to an object of the specified type T, with the specified domain for any AssetLocation which needs to be parsed
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public T AsObject<T>(T defaultValue, string domain)
	{
		JsonSerializerSettings settings = null;
		if (domain != "game")
		{
			settings = new JsonSerializerSettings();
			settings.Converters.Add(new AssetLocationJsonParser(domain));
		}
		if (token != null)
		{
			return JsonConvert.DeserializeObject<T>(token.ToString(), settings);
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

	/// <summary>
	/// Turn the token into an array of JsonObjects
	/// </summary>
	/// <returns></returns>
	public JsonObject[] AsArray()
	{
		if (!(token is JArray))
		{
			return null;
		}
		JArray arr = (JArray)token;
		JsonObject[] objs = new JsonObject[arr.Count];
		for (int i = 0; i < objs.Length; i++)
		{
			objs[i] = new JsonObject(arr[i]);
		}
		return objs;
	}

	/// <summary>
	/// Turn the token into a string
	/// </summary>
	/// <param name="defaultValue">If the conversion fails, this value is used instead</param>
	/// <returns></returns>
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

	/// <summary>
	/// Turn the token into an array
	/// </summary>
	/// <param name="defaultValue">If the conversion fails, this value is used instead</param>
	/// <param name="defaultDomain"></param>
	/// <returns></returns>
	public T[] AsArray<T>(T[] defaultValue = null, string defaultDomain = null)
	{
		if (!(this.token is JArray))
		{
			return defaultValue;
		}
		JArray arr = (JArray)this.token;
		T[] objs = new T[arr.Count];
		for (int i = 0; i < objs.Length; i++)
		{
			JToken token = arr[i];
			if (token is JValue || token is JObject)
			{
				objs[i] = token.ToObject<T>(defaultDomain);
				continue;
			}
			return defaultValue;
		}
		return objs;
	}

	/// <summary>
	/// Turn the token into a boolean
	/// </summary>
	/// <param name="defaultValue">If the conversion fails, this value is used instead</param>
	/// <returns></returns>
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
			if (!bool.TryParse(value?.ToString() ?? "", out var val))
			{
				return defaultValue;
			}
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Turn the token into an integer
	/// </summary>
	/// <param name="defaultValue">If the conversion fails, this value is used instead</param>
	/// <returns></returns>
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
			if (!int.TryParse(value?.ToString() ?? "", out var val))
			{
				return defaultValue;
			}
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Turn the token into a float
	/// </summary>
	/// <param name="defaultValue">If the conversion fails, this value is used instead</param>
	/// <returns></returns>
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
			if (!float.TryParse(value?.ToString() ?? "", NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var val))
			{
				return defaultValue;
			}
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Turn the token into a double
	/// </summary>
	/// <param name="defaultValue">If the conversion fails, this value is used instead</param>
	/// <returns></returns>
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
			if (!double.TryParse(value?.ToString() ?? "", out var val))
			{
				return defaultValue;
			}
			return val;
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

	/// <summary>
	/// Calls token.ToString()
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return (token?.ToString()).DeDuplicate();
	}

	/// <summary>
	/// True if the token is a JArray
	/// </summary>
	/// <returns></returns>
	public bool IsArray()
	{
		return token is JArray;
	}

	/// <summary>
	/// Turns the token into an IAttribute with all its child elements, if it has any.<br />
	/// Note: If you converting this to a tree attribute, a subsequent call to tree.GetInt() might not work because Newtonsoft.JSON seems to load integers as long, so use GetDecimal() or GetLong() instead. Similar things might happen with float&lt;-&gt;double
	/// </summary>
	/// <returns></returns>
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
		if (token is JValue jval && jval.Value is string)
		{
			jval.Value = (jval.Value as string).Replace("{" + key + "}", value);
		}
		if (token is JArray jarr)
		{
			foreach (JToken item in jarr)
			{
				FillPlaceHolder(item, key, value);
			}
		}
		if (!(token is JObject jobj))
		{
			return;
		}
		foreach (KeyValuePair<string, JToken> item2 in jobj)
		{
			FillPlaceHolder(item2.Value, key, value);
		}
	}

	private static IAttribute ToAttribute(JToken token)
	{
		if (token is JValue jval)
		{
			if (jval.Value is int)
			{
				return new IntAttribute((int)jval.Value);
			}
			if (jval.Value is long)
			{
				return new LongAttribute((long)jval.Value);
			}
			if (jval.Value is float)
			{
				return new FloatAttribute((float)jval.Value);
			}
			if (jval.Value is double)
			{
				return new DoubleAttribute((double)jval.Value);
			}
			if (jval.Value is bool)
			{
				return new BoolAttribute((bool)jval.Value);
			}
			if (jval.Value is string)
			{
				return new StringAttribute((string)jval.Value);
			}
		}
		if (token is JObject jobj)
		{
			TreeAttribute tree = new TreeAttribute();
			{
				foreach (KeyValuePair<string, JToken> val in jobj)
				{
					tree[val.Key] = ToAttribute(val.Value);
				}
				return tree;
			}
		}
		if (token is JArray jarr)
		{
			if (!jarr.HasValues)
			{
				return new TreeArrayAttribute(new TreeAttribute[0]);
			}
			if (jarr[0] is JValue jvalFirst)
			{
				if (jvalFirst.Value is int)
				{
					return new IntArrayAttribute(ToPrimitiveArray<int>(jarr));
				}
				if (jvalFirst.Value is long)
				{
					return new LongArrayAttribute(ToPrimitiveArray<long>(jarr));
				}
				if (jvalFirst.Value is float)
				{
					return new FloatArrayAttribute(ToPrimitiveArray<float>(jarr));
				}
				if (jvalFirst.Value is double)
				{
					return new DoubleArrayAttribute(ToPrimitiveArray<double>(jarr));
				}
				if (jvalFirst.Value is bool)
				{
					return new BoolArrayAttribute(ToPrimitiveArray<bool>(jarr));
				}
				if (jvalFirst.Value is string)
				{
					return new StringArrayAttribute(ToPrimitiveArray<string>(jarr));
				}
				return null;
			}
			TreeAttribute[] attrs = new TreeAttribute[jarr.Count];
			for (int i = 0; i < attrs.Length; i++)
			{
				attrs[i] = (TreeAttribute)ToAttribute(jarr[i]);
			}
			return new TreeArrayAttribute(attrs);
		}
		return null;
	}

	/// <summary>
	/// Turn a JArray into a primitive array
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="array"></param>
	/// <returns></returns>
	public static T[] ToPrimitiveArray<T>(JArray array)
	{
		T[] values = new T[array.Count];
		for (int i = 0; i < values.Length; i++)
		{
			_ = array[i];
			values[i] = array[i].ToObject<T>();
		}
		return values;
	}

	/// <summary>
	/// Returns a deep clone
	/// </summary>
	/// <returns></returns>
	public JsonObject Clone()
	{
		return new JsonObject(token.DeepClone());
	}

	/// <summary>
	/// Returns true if this object has the named bool attribute, and it is true
	/// </summary>
	public bool IsTrue(string attrName)
	{
		if (token == null || !(token is JObject))
		{
			return false;
		}
		if (token[attrName] is JValue jvalue)
		{
			object value = jvalue.Value;
			if (value is bool)
			{
				return (bool)value;
			}
			if (jvalue.Value is string boolString)
			{
				bool.TryParse(boolString, out var val);
				return val;
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
