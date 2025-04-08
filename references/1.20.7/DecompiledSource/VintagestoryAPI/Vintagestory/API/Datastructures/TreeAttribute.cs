using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.API.Datastructures;

/// <summary>
/// A datastructure to hold generic data for most primitives (int, string, float, etc.). But can also hold generic data using the ByteArrayAttribute + class serialization
/// </summary>
public class TreeAttribute : ITreeAttribute, IAttribute, IEnumerable<KeyValuePair<string, IAttribute>>, IEnumerable
{
	public static Dictionary<int, Type> AttributeIdMapping;

	protected int depth;

	internal IDictionary<string, IAttribute> attributes = new ConcurrentSmallDictionary<string, IAttribute>(0);

	/// <summary>
	/// Will return null if given attribute does not exist
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public IAttribute this[string key]
	{
		get
		{
			return attributes.TryGetValue(key);
		}
		set
		{
			attributes[key] = value;
		}
	}

	/// <summary>
	/// Amount of elements in this Tree attribute
	/// </summary>
	public int Count => attributes.Count;

	/// <summary>
	/// Returns all values inside this tree attributes
	/// </summary>
	public IAttribute[] Values => (IAttribute[])attributes.Values;

	public string[] Keys => (string[])attributes.Keys;

	static TreeAttribute()
	{
		AttributeIdMapping = new Dictionary<int, Type>();
		RegisterAttribute(1, typeof(IntAttribute));
		RegisterAttribute(2, typeof(LongAttribute));
		RegisterAttribute(3, typeof(DoubleAttribute));
		RegisterAttribute(4, typeof(FloatAttribute));
		RegisterAttribute(5, typeof(StringAttribute));
		RegisterAttribute(6, typeof(TreeAttribute));
		RegisterAttribute(7, typeof(ItemstackAttribute));
		RegisterAttribute(8, typeof(ByteArrayAttribute));
		RegisterAttribute(9, typeof(BoolAttribute));
		RegisterAttribute(10, typeof(StringArrayAttribute));
		RegisterAttribute(11, typeof(IntArrayAttribute));
		RegisterAttribute(12, typeof(FloatArrayAttribute));
		RegisterAttribute(13, typeof(DoubleArrayAttribute));
		RegisterAttribute(14, typeof(TreeArrayAttribute));
		RegisterAttribute(15, typeof(LongArrayAttribute));
		RegisterAttribute(16, typeof(BoolArrayAttribute));
	}

	public static void RegisterAttribute(int attrId, Type type)
	{
		AttributeIdMapping[attrId] = type;
	}

	public static TreeAttribute CreateFromBytes(byte[] blockEntityData)
	{
		TreeAttribute tree = new TreeAttribute();
		using MemoryStream ms = new MemoryStream(blockEntityData);
		using BinaryReader reader = new BinaryReader(ms);
		tree.FromBytes(reader);
		return tree;
	}

	public virtual void FromBytes(BinaryReader stream)
	{
		if (depth > 30)
		{
			Console.WriteLine("Can't fully decode AttributeTree, beyond 30 depth limit");
			return;
		}
		attributes.Clear();
		byte attrId;
		while ((attrId = stream.ReadByte()) != 0)
		{
			string key = stream.ReadString();
			IAttribute attr = (IAttribute)Activator.CreateInstance(AttributeIdMapping[attrId]);
			if (attr is TreeAttribute)
			{
				((TreeAttribute)attr).depth = depth + 1;
			}
			attr.FromBytes(stream);
			attributes[key] = attr;
		}
	}

	public virtual byte[] ToBytes()
	{
		using MemoryStream ms = new MemoryStream();
		using (BinaryWriter writer = new BinaryWriter(ms))
		{
			ToBytes(writer);
		}
		return ms.ToArray();
	}

	public virtual void FromBytes(byte[] data)
	{
		using MemoryStream ms = new MemoryStream(data);
		using BinaryReader reader = new BinaryReader(ms);
		FromBytes(reader);
	}

	public virtual void ToBytes(BinaryWriter stream)
	{
		foreach (KeyValuePair<string, IAttribute> val in attributes)
		{
			stream.Write((byte)val.Value.GetAttributeId());
			stream.Write(val.Key);
			val.Value.ToBytes(stream);
		}
		stream.Write((byte)0);
	}

	public int GetAttributeId()
	{
		return 6;
	}

	[Obsolete("May not return consistent results if the TreeAttribute changes between calls")]
	public int IndexOf(string key)
	{
		_ = attributes.Keys;
		int i = 0;
		foreach (string key2 in attributes.Keys)
		{
			if (key2 == key)
			{
				return i;
			}
			i++;
		}
		return -1;
	}

	public IEnumerator<KeyValuePair<string, IAttribute>> GetEnumerator()
	{
		return attributes.GetEnumerator();
	}

	IEnumerator<KeyValuePair<string, IAttribute>> IEnumerable<KeyValuePair<string, IAttribute>>.GetEnumerator()
	{
		return attributes.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return attributes.GetEnumerator();
	}

	/// <summary>
	/// Set a value. Returns itself for method chaining
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public TreeAttribute Set(string key, IAttribute value)
	{
		attributes[key] = value;
		return this;
	}

	public IAttribute GetAttribute(string key)
	{
		return attributes.TryGetValue(key);
	}

	/// <summary>
	/// True if this attribute exists
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public bool HasAttribute(string key)
	{
		return attributes.ContainsKey(key);
	}

	public bool TryGetAttribute(string key, out IAttribute value)
	{
		return attributes.TryGetValue(key, out value);
	}

	public IAttribute GetAttributeByPath(string path)
	{
		if (!path.Contains('/'))
		{
			return this[path];
		}
		string[] parts = path.Split('/');
		ITreeAttribute treeAttr = this;
		for (int i = 0; i < parts.Length - 1; i++)
		{
			IAttribute attr = treeAttr[parts[i]];
			if (attr is ITreeAttribute)
			{
				treeAttr = (ITreeAttribute)attr;
				continue;
			}
			return null;
		}
		return treeAttr[parts[^1]];
	}

	public void DeleteAttributeByPath(string path)
	{
		string[] parts = path.Split('/');
		for (int i = 0; i < parts.Length - 1; i++)
		{
			IAttribute attr = ((ITreeAttribute)this)[parts[i]];
			if (attr is ITreeAttribute)
			{
				attr = (ITreeAttribute)attr;
				continue;
			}
			return;
		}
		((ITreeAttribute)this)?.RemoveAttribute(parts[^1]);
	}

	/// <summary>
	/// Removes an attribute
	/// </summary>
	/// <param name="key"></param>
	public virtual void RemoveAttribute(string key)
	{
		attributes.Remove(key);
	}

	/// <summary>
	/// Creates a bool attribute with given key and value
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public virtual void SetBool(string key, bool value)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<bool> sattr)
		{
			sattr.SetValue(value);
		}
		else
		{
			attributes[key] = new BoolAttribute(value);
		}
	}

	/// <summary>
	/// Creates an int attribute with given key and value<br />
	/// Side note: If you need this attribute to be compatible with deserialized json - use SetLong()
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public virtual void SetInt(string key, int value)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<int> sattr)
		{
			sattr.SetValue(value);
		}
		else
		{
			attributes[key] = new IntAttribute(value);
		}
	}

	/// <summary>
	/// Creates a long attribute with given key and value
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public virtual void SetLong(string key, long value)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<long> sattr)
		{
			sattr.SetValue(value);
		}
		else
		{
			attributes[key] = new LongAttribute(value);
		}
	}

	/// <summary>
	/// Creates a double attribute with given key and value
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public virtual void SetDouble(string key, double value)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<double> sattr)
		{
			sattr.SetValue(value);
		}
		else
		{
			attributes[key] = new DoubleAttribute(value);
		}
	}

	/// <summary>
	/// Creates a float attribute with given key and value<br />
	/// Side note: If you need this attribute to be compatible with deserialized json - use SetDouble()
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public virtual void SetFloat(string key, float value)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<float> sattr)
		{
			sattr.SetValue(value);
		}
		else
		{
			attributes[key] = new FloatAttribute(value);
		}
	}

	public virtual void SetString(string key, string value)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<string> sattr)
		{
			sattr.SetValue(value);
		}
		else
		{
			attributes[key] = new StringAttribute(value);
		}
	}

	public virtual void SetStringArray(string key, string[] values)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<string[]> sattr)
		{
			sattr.SetValue(values);
		}
		else
		{
			attributes[key] = new StringArrayAttribute(values);
		}
	}

	/// <summary>
	/// Creates a byte[] attribute with given key and value
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public virtual void SetBytes(string key, byte[] value)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ScalarAttribute<byte[]> sattr)
		{
			sattr.SetValue(value);
		}
		else
		{
			attributes[key] = new ByteArrayAttribute(value);
		}
	}

	public virtual void SetAttribute(string key, IAttribute value)
	{
		attributes[key] = value;
	}

	/// <summary>
	/// Sets given item stack with given key
	/// </summary>
	/// <param name="key"></param>
	/// <param name="itemstack"></param>
	public void SetItemstack(string key, ItemStack itemstack)
	{
		if (attributes.TryGetValue(key, out var attr) && attr is ItemstackAttribute sattr)
		{
			sattr.SetValue(itemstack);
		}
		else
		{
			attributes[key] = new ItemstackAttribute(itemstack);
		}
	}

	/// <summary>
	/// Retrieves a bool or null if the key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual bool? TryGetBool(string key)
	{
		return (attributes.TryGetValue(key) as BoolAttribute)?.value;
	}

	public virtual int? TryGetInt(string key)
	{
		return (attributes.TryGetValue(key) as IntAttribute)?.value;
	}

	/// <summary>
	/// Retrieves a double or null if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual double? TryGetDouble(string key)
	{
		return (attributes.TryGetValue(key) as DoubleAttribute)?.value;
	}

	/// <summary>
	/// Retrieves a float or null if the key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual float? TryGetFloat(string key)
	{
		return (attributes.TryGetValue(key) as FloatAttribute)?.value;
	}

	/// <summary>
	/// Retrieves a bool or default value if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual bool GetBool(string key, bool defaultValue = false)
	{
		if (attributes.TryGetValue(key) is BoolAttribute attr)
		{
			return attr.value;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves an int or default value if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual int GetInt(string key, int defaultValue = 0)
	{
		if (attributes.TryGetValue(key) is IntAttribute attr)
		{
			return attr.value;
		}
		return defaultValue;
	}

	/// <summary>
	/// Same as (int)GetDecimal(key, defValue);
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual int GetAsInt(string key, int defaultValue = 0)
	{
		return (int)GetDecimal(key, defaultValue);
	}

	public virtual bool GetAsBool(string key, bool defaultValue = false)
	{
		IAttribute attr = attributes.TryGetValue(key);
		if (attr is IntAttribute)
		{
			return (int)attr.GetValue() > 0;
		}
		if (attr is FloatAttribute)
		{
			return (float)attr.GetValue() > 0f;
		}
		if (attr is DoubleAttribute)
		{
			return (double)attr.GetValue() > 0.0;
		}
		if (attr is LongAttribute)
		{
			return (long)attr.GetValue() > 0;
		}
		if (attr is StringAttribute)
		{
			if (!((string)attr.GetValue() == "true"))
			{
				return (string)attr.GetValue() == "1";
			}
			return true;
		}
		if (attr is BoolAttribute)
		{
			return (bool)attr.GetValue();
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves an int, float, long or double value. Whatever attribute is found for given key, in aformentioned order. If its a string its converted to double
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual double GetDecimal(string key, double defaultValue = 0.0)
	{
		IAttribute attr = attributes.TryGetValue(key);
		if (attr is IntAttribute)
		{
			return (int)attr.GetValue();
		}
		if (attr is FloatAttribute)
		{
			return (float)attr.GetValue();
		}
		if (attr is DoubleAttribute)
		{
			return (double)attr.GetValue();
		}
		if (attr is LongAttribute)
		{
			return (long)attr.GetValue();
		}
		if (attr is StringAttribute)
		{
			return ((string)attr.GetValue()).ToDouble();
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves a double or defaultValue if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual double GetDouble(string key, double defaultValue = 0.0)
	{
		if (attributes.TryGetValue(key) is DoubleAttribute attr)
		{
			return attr.value;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves a float or defaultvalue if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual float GetFloat(string key, float defaultValue = 0f)
	{
		if (attributes.TryGetValue(key) is FloatAttribute attr)
		{
			return attr.value;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves a string attribute or defaultValue if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual string GetString(string key, string defaultValue = null)
	{
		string val = (attributes.TryGetValue(key) as StringAttribute)?.value;
		if (val != null)
		{
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves the value of given attribute, independent of attribute type
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual string GetAsString(string key, string defaultValue = null)
	{
		string val = attributes.TryGetValue(key)?.GetValue().ToString();
		if (val != null)
		{
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves a string or defaultValue if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual string[] GetStringArray(string key, string[] defaultValue = null)
	{
		string[] val = (attributes.TryGetValue(key) as StringArrayAttribute)?.value;
		if (val != null)
		{
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves a byte array or defaultValue if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual byte[] GetBytes(string key, byte[] defaultValue = null)
	{
		byte[] val = (attributes.TryGetValue(key) as ByteArrayAttribute)?.value;
		if (val != null)
		{
			return val;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves an attribute tree or null if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual ITreeAttribute GetTreeAttribute(string key)
	{
		return attributes.TryGetValue(key) as ITreeAttribute;
	}

	/// <summary>
	/// Retrieves an attribute tree or adds it if key is not found.
	/// Throws an exception if the key does exist but is not a tree.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual ITreeAttribute GetOrAddTreeAttribute(string key)
	{
		IAttribute attr = attributes.TryGetValue(key);
		if (attr == null)
		{
			TreeAttribute result = new TreeAttribute();
			SetAttribute(key, result);
			return result;
		}
		if (attr is ITreeAttribute result2)
		{
			return result2;
		}
		throw new InvalidOperationException($"The attribute with key '{key}' is a {attr.GetType().Name}, not TreeAttribute.");
	}

	/// <summary>
	/// Retrieves an itemstack or defaultValue if key is not found. Be sure to call stack.ResolveBlockOrItem() after retrieving it.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public ItemStack GetItemstack(string key, ItemStack defaultValue = null)
	{
		ItemStack stack = ((ItemstackAttribute)attributes.TryGetValue(key))?.value;
		if (stack != null)
		{
			return stack;
		}
		return defaultValue;
	}

	/// <summary>
	/// Retrieves a long or default value if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public virtual long GetLong(string key, long defaultValue = 0L)
	{
		return ((LongAttribute)attributes.TryGetValue(key))?.value ?? defaultValue;
	}

	/// <summary>
	/// Retrieves a long or null value if key is not found
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public virtual long? TryGetLong(string key)
	{
		return ((LongAttribute)attributes.TryGetValue(key))?.value;
	}

	public virtual ModelTransform GetModelTransform(string key)
	{
		ITreeAttribute attr = GetTreeAttribute(key);
		if (attr == null)
		{
			return null;
		}
		ITreeAttribute torigin = attr.GetTreeAttribute("origin");
		ITreeAttribute trotation = attr.GetTreeAttribute("rotation");
		ITreeAttribute ttranslation = attr.GetTreeAttribute("translation");
		float scale = attr.GetFloat("scale", 1f);
		Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
		if (torigin != null)
		{
			origin.X = torigin.GetFloat("x");
			origin.Y = torigin.GetFloat("y");
			origin.Z = torigin.GetFloat("z");
		}
		Vec3f rotation = new Vec3f();
		if (trotation != null)
		{
			rotation.X = trotation.GetFloat("x");
			rotation.Y = trotation.GetFloat("y");
			rotation.Z = trotation.GetFloat("z");
		}
		Vec3f translation = new Vec3f();
		if (ttranslation != null)
		{
			translation.X = ttranslation.GetFloat("x");
			translation.Y = ttranslation.GetFloat("y");
			translation.Z = ttranslation.GetFloat("z");
		}
		return new ModelTransform
		{
			Scale = scale,
			Origin = origin,
			Translation = translation,
			Rotation = rotation
		};
	}

	public object GetValue()
	{
		return this;
	}

	/// <summary>
	/// Creates a deep copy of the attribute tree
	/// </summary>
	/// <returns></returns>
	public virtual ITreeAttribute Clone()
	{
		TreeAttribute tree = new TreeAttribute();
		foreach (KeyValuePair<string, IAttribute> val in attributes)
		{
			tree[val.Key] = val.Value.Clone();
		}
		return tree;
	}

	IAttribute IAttribute.Clone()
	{
		return Clone();
	}

	/// <summary>
	/// Returns true if given tree contains all of elements of this one, but given tree may contain also more elements. Individual node values are exactly matched.
	/// </summary>
	/// <param name="worldForResolve"></param>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool IsSubSetOf(IWorldAccessor worldForResolve, IAttribute other)
	{
		if (!(other is TreeAttribute))
		{
			return false;
		}
		TreeAttribute otherTree = (TreeAttribute)other;
		if (attributes.Count > otherTree.attributes.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, IAttribute> val in attributes)
		{
			if (GlobalConstants.IgnoredStackAttributes.Contains(val.Key))
			{
				continue;
			}
			if (!otherTree.attributes.ContainsKey(val.Key))
			{
				return false;
			}
			if (val.Value is TreeAttribute)
			{
				if (!(otherTree.attributes[val.Key] as TreeAttribute).IsSubSetOf(worldForResolve, val.Value))
				{
					return false;
				}
			}
			else if (!otherTree.attributes[val.Key].Equals(worldForResolve, val.Value))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Returns true if given tree exactly matches this one
	/// </summary>
	/// <param name="worldForResolve"></param>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(IWorldAccessor worldForResolve, IAttribute other)
	{
		if (!(other is TreeAttribute))
		{
			return false;
		}
		TreeAttribute otherTree = (TreeAttribute)other;
		if (attributes.Count != otherTree.attributes.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, IAttribute> val in attributes)
		{
			if (!otherTree.attributes.ContainsKey(val.Key))
			{
				return false;
			}
			if (!otherTree.attributes[val.Key].Equals(worldForResolve, val.Value))
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals(IWorldAccessor worldForResolve, IAttribute other, params string[] ignorePaths)
	{
		return Equals(worldForResolve, other, "", ignorePaths);
	}

	public bool Equals(IWorldAccessor worldForResolve, IAttribute other, string currentPath, params string[] ignorePaths)
	{
		if (!(other is TreeAttribute))
		{
			return false;
		}
		TreeAttribute otherTree = (TreeAttribute)other;
		if ((ignorePaths == null || ignorePaths.Length == 0) && attributes.Count != otherTree.attributes.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, IAttribute> val2 in attributes)
		{
			string curPath2 = currentPath + ((currentPath.Length > 0) ? "/" : "") + val2.Key;
			if (ignorePaths != null && ignorePaths.Contains(curPath2))
			{
				continue;
			}
			if (!otherTree.attributes.ContainsKey(val2.Key))
			{
				return false;
			}
			IAttribute otherAttr = otherTree.attributes[val2.Key];
			if (otherAttr is TreeAttribute)
			{
				if (!((TreeAttribute)otherAttr).Equals(worldForResolve, val2.Value, currentPath, ignorePaths))
				{
					return false;
				}
			}
			else if (otherAttr is ItemstackAttribute)
			{
				if (!(otherAttr as ItemstackAttribute).Equals(worldForResolve, val2.Value, ignorePaths))
				{
					return false;
				}
			}
			else if (!otherAttr.Equals(worldForResolve, val2.Value))
			{
				return false;
			}
		}
		foreach (KeyValuePair<string, IAttribute> val in otherTree.attributes)
		{
			string curPath = currentPath + ((currentPath.Length > 0) ? "/" : "") + val.Key;
			if ((ignorePaths == null || !ignorePaths.Contains(curPath)) && !attributes.ContainsKey(val.Key))
			{
				return false;
			}
		}
		return true;
	}

	public string ToJsonToken()
	{
		return ToJsonToken(attributes);
	}

	public static IAttribute FromJson(string json)
	{
		return new JsonObject(JToken.Parse(json)).ToAttribute();
	}

	public static string ToJsonToken(IEnumerable<KeyValuePair<string, IAttribute>> attributes)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("{ ");
		int i = 0;
		foreach (KeyValuePair<string, IAttribute> val in attributes)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}
			i++;
			sb.Append("\"" + val.Key + "\": " + val.Value.ToJsonToken());
		}
		sb.Append(" }");
		return sb.ToString();
	}

	/// <summary>
	/// Merges the sourceTree into the current one
	/// </summary>
	/// <param name="sourceTree"></param>
	public virtual void MergeTree(ITreeAttribute sourceTree)
	{
		if (sourceTree is TreeAttribute srcTree)
		{
			MergeTree(this, srcTree);
			return;
		}
		throw new ArgumentException("Expected TreeAttribute but got " + sourceTree.GetType().Name + "! " + sourceTree.ToString());
	}

	protected static void MergeTree(TreeAttribute dstTree, TreeAttribute srcTree)
	{
		foreach (KeyValuePair<string, IAttribute> srcVal in srcTree.attributes)
		{
			MergeAttribute(dstTree, srcVal.Key, srcVal.Value);
		}
	}

	protected static void MergeAttribute(TreeAttribute dstTree, string srcKey, IAttribute srcAttr)
	{
		IAttribute dstAttr = dstTree.attributes.TryGetValue(srcKey);
		if (dstAttr == null)
		{
			dstTree.attributes[srcKey] = srcAttr.Clone();
			return;
		}
		if (dstAttr.GetAttributeId() != srcAttr.GetAttributeId())
		{
			throw new Exception("Cannot merge attributes! Expected attributeId " + dstAttr.GetAttributeId() + " instead of " + srcAttr.GetAttributeId() + "! Existing: " + dstAttr.ToString() + ", new: " + srcAttr.ToString());
		}
		if (srcAttr is ITreeAttribute)
		{
			MergeTree(dstAttr as TreeAttribute, srcAttr as TreeAttribute);
		}
		else
		{
			dstTree.attributes[srcKey] = srcAttr.Clone();
		}
	}

	public OrderedDictionary<string, IAttribute> SortedCopy(bool recursive = false)
	{
		IOrderedEnumerable<KeyValuePair<string, IAttribute>> orderedEnumerable = attributes.OrderBy((KeyValuePair<string, IAttribute> x) => x.Key);
		OrderedDictionary<string, IAttribute> sortedTree = new OrderedDictionary<string, IAttribute>();
		foreach (KeyValuePair<string, IAttribute> entry in orderedEnumerable)
		{
			IAttribute attribute = entry.Value;
			TreeAttribute tree = attribute as TreeAttribute;
			if (tree != null && recursive)
			{
				attribute = tree.ConsistentlyOrderedCopy();
			}
			sortedTree.Add(entry.Key, attribute);
		}
		return sortedTree;
	}

	private IAttribute ConsistentlyOrderedCopy()
	{
		Dictionary<string, IAttribute> sorted = attributes.OrderBy((KeyValuePair<string, IAttribute> x) => x.Key).ToDictionary((KeyValuePair<string, IAttribute> pair) => pair.Key, (KeyValuePair<string, IAttribute> pair) => pair.Value);
		foreach (var (key, attribute2) in sorted)
		{
			if (attribute2 is TreeAttribute tree)
			{
				sorted[key] = tree.ConsistentlyOrderedCopy();
			}
		}
		TreeAttribute treeAttribute = new TreeAttribute();
		treeAttribute.attributes.AddRange(sorted);
		return treeAttribute;
	}

	public override int GetHashCode()
	{
		return GetHashCode(null);
	}

	public int GetHashCode(string[] ignoredAttributes)
	{
		int hashcode = 0;
		int i = 0;
		foreach (KeyValuePair<string, IAttribute> val in attributes)
		{
			if (ignoredAttributes == null || !ignoredAttributes.Contains(val.Key))
			{
				hashcode = ((!(val.Value is ITreeAttribute tree)) ? ((i != 0) ? (hashcode ^ (val.Key.GetHashCode() ^ val.Value.GetHashCode())) : (val.Key.GetHashCode() ^ val.Value.GetHashCode())) : ((i != 0) ? (hashcode ^ (val.Key.GetHashCode() ^ tree.GetHashCode(ignoredAttributes))) : (val.Key.GetHashCode() ^ tree.GetHashCode(ignoredAttributes))));
				i++;
			}
		}
		return hashcode;
	}

	Type IAttribute.GetType()
	{
		return GetType();
	}
}
