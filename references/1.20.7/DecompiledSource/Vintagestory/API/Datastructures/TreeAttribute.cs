#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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

//
// Summary:
//     A datastructure to hold generic data for most primitives (int, string, float,
//     etc.). But can also hold generic data using the ByteArrayAttribute + class serialization
public class TreeAttribute : ITreeAttribute, IAttribute, IEnumerable<KeyValuePair<string, IAttribute>>, IEnumerable
{
    public static Dictionary<int, Type> AttributeIdMapping;

    protected int depth;

    internal IDictionary<string, IAttribute> attributes = new ConcurrentSmallDictionary<string, IAttribute>(0);

    //
    // Summary:
    //     Will return null if given attribute does not exist
    //
    // Parameters:
    //   key:
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

    //
    // Summary:
    //     Amount of elements in this Tree attribute
    public int Count => attributes.Count;

    //
    // Summary:
    //     Returns all values inside this tree attributes
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
        TreeAttribute treeAttribute = new TreeAttribute();
        using MemoryStream input = new MemoryStream(blockEntityData);
        using BinaryReader stream = new BinaryReader(input);
        treeAttribute.FromBytes(stream);
        return treeAttribute;
    }

    public virtual void FromBytes(BinaryReader stream)
    {
        if (depth > 30)
        {
            Console.WriteLine("Can't fully decode AttributeTree, beyond 30 depth limit");
            return;
        }

        attributes.Clear();
        byte key;
        while ((key = stream.ReadByte()) != 0)
        {
            string key2 = stream.ReadString();
            IAttribute attribute = (IAttribute)Activator.CreateInstance(AttributeIdMapping[key]);
            if (attribute is TreeAttribute)
            {
                ((TreeAttribute)attribute).depth = depth + 1;
            }

            attribute.FromBytes(stream);
            attributes[key2] = attribute;
        }
    }

    public virtual byte[] ToBytes()
    {
        using MemoryStream memoryStream = new MemoryStream();
        using (BinaryWriter stream = new BinaryWriter(memoryStream))
        {
            ToBytes(stream);
        }

        return memoryStream.ToArray();
    }

    public virtual void FromBytes(byte[] data)
    {
        using MemoryStream input = new MemoryStream(data);
        using BinaryReader stream = new BinaryReader(input);
        FromBytes(stream);
    }

    public virtual void ToBytes(BinaryWriter stream)
    {
        foreach (KeyValuePair<string, IAttribute> attribute in attributes)
        {
            stream.Write((byte)attribute.Value.GetAttributeId());
            stream.Write(attribute.Key);
            attribute.Value.ToBytes(stream);
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
        int num = 0;
        foreach (string key2 in attributes.Keys)
        {
            if (key2 == key)
            {
                return num;
            }

            num++;
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

    //
    // Summary:
    //     Set a value. Returns itself for method chaining
    //
    // Parameters:
    //   key:
    //
    //   value:
    public TreeAttribute Set(string key, IAttribute value)
    {
        attributes[key] = value;
        return this;
    }

    public IAttribute GetAttribute(string key)
    {
        return attributes.TryGetValue(key);
    }

    //
    // Summary:
    //     True if this attribute exists
    //
    // Parameters:
    //   key:
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

        string[] array = path.Split('/');
        ITreeAttribute treeAttribute = this;
        for (int i = 0; i < array.Length - 1; i++)
        {
            IAttribute attribute = treeAttribute[array[i]];
            if (attribute is ITreeAttribute)
            {
                treeAttribute = (ITreeAttribute)attribute;
                continue;
            }

            return null;
        }

        return treeAttribute[array[^1]];
    }

    public void DeleteAttributeByPath(string path)
    {
        string[] array = path.Split('/');
        for (int i = 0; i < array.Length - 1; i++)
        {
            IAttribute attribute = ((ITreeAttribute)this)[array[i]];
            if (attribute is ITreeAttribute)
            {
                attribute = (ITreeAttribute)attribute;
                continue;
            }

            return;
        }

        ((ITreeAttribute)this)?.RemoveAttribute(array[^1]);
    }

    //
    // Summary:
    //     Removes an attribute
    //
    // Parameters:
    //   key:
    public virtual void RemoveAttribute(string key)
    {
        attributes.Remove(key);
    }

    //
    // Summary:
    //     Creates a bool attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    public virtual void SetBool(string key, bool value)
    {
        if (attributes.TryGetValue(key, out var value2) && value2 is ScalarAttribute<bool> scalarAttribute)
        {
            scalarAttribute.SetValue(value);
        }
        else
        {
            attributes[key] = new BoolAttribute(value);
        }
    }

    //
    // Summary:
    //     Creates an int attribute with given key and value
    //     Side note: If you need this attribute to be compatible with deserialized json
    //     - use SetLong()
    //
    // Parameters:
    //   key:
    //
    //   value:
    public virtual void SetInt(string key, int value)
    {
        if (attributes.TryGetValue(key, out var value2) && value2 is ScalarAttribute<int> scalarAttribute)
        {
            scalarAttribute.SetValue(value);
        }
        else
        {
            attributes[key] = new IntAttribute(value);
        }
    }

    //
    // Summary:
    //     Creates a long attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    public virtual void SetLong(string key, long value)
    {
        if (attributes.TryGetValue(key, out var value2) && value2 is ScalarAttribute<long> scalarAttribute)
        {
            scalarAttribute.SetValue(value);
        }
        else
        {
            attributes[key] = new LongAttribute(value);
        }
    }

    //
    // Summary:
    //     Creates a double attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    public virtual void SetDouble(string key, double value)
    {
        if (attributes.TryGetValue(key, out var value2) && value2 is ScalarAttribute<double> scalarAttribute)
        {
            scalarAttribute.SetValue(value);
        }
        else
        {
            attributes[key] = new DoubleAttribute(value);
        }
    }

    //
    // Summary:
    //     Creates a float attribute with given key and value
    //     Side note: If you need this attribute to be compatible with deserialized json
    //     - use SetDouble()
    //
    // Parameters:
    //   key:
    //
    //   value:
    public virtual void SetFloat(string key, float value)
    {
        if (attributes.TryGetValue(key, out var value2) && value2 is ScalarAttribute<float> scalarAttribute)
        {
            scalarAttribute.SetValue(value);
        }
        else
        {
            attributes[key] = new FloatAttribute(value);
        }
    }

    public virtual void SetString(string key, string value)
    {
        if (attributes.TryGetValue(key, out var value2) && value2 is ScalarAttribute<string> scalarAttribute)
        {
            scalarAttribute.SetValue(value);
        }
        else
        {
            attributes[key] = new StringAttribute(value);
        }
    }

    public virtual void SetStringArray(string key, string[] values)
    {
        if (attributes.TryGetValue(key, out var value) && value is ScalarAttribute<string[]> scalarAttribute)
        {
            scalarAttribute.SetValue(values);
        }
        else
        {
            attributes[key] = new StringArrayAttribute(values);
        }
    }

    //
    // Summary:
    //     Creates a byte[] attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    public virtual void SetBytes(string key, byte[] value)
    {
        if (attributes.TryGetValue(key, out var value2) && value2 is ScalarAttribute<byte[]> scalarAttribute)
        {
            scalarAttribute.SetValue(value);
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

    //
    // Summary:
    //     Sets given item stack with given key
    //
    // Parameters:
    //   key:
    //
    //   itemstack:
    public void SetItemstack(string key, ItemStack itemstack)
    {
        if (attributes.TryGetValue(key, out var value) && value is ItemstackAttribute itemstackAttribute)
        {
            itemstackAttribute.SetValue(itemstack);
        }
        else
        {
            attributes[key] = new ItemstackAttribute(itemstack);
        }
    }

    //
    // Summary:
    //     Retrieves a bool or null if the key is not found
    //
    // Parameters:
    //   key:
    public virtual bool? TryGetBool(string key)
    {
        return (attributes.TryGetValue(key) as BoolAttribute)?.value;
    }

    public virtual int? TryGetInt(string key)
    {
        return (attributes.TryGetValue(key) as IntAttribute)?.value;
    }

    //
    // Summary:
    //     Retrieves a double or null if key is not found
    //
    // Parameters:
    //   key:
    public virtual double? TryGetDouble(string key)
    {
        return (attributes.TryGetValue(key) as DoubleAttribute)?.value;
    }

    //
    // Summary:
    //     Retrieves a float or null if the key is not found
    //
    // Parameters:
    //   key:
    public virtual float? TryGetFloat(string key)
    {
        return (attributes.TryGetValue(key) as FloatAttribute)?.value;
    }

    //
    // Summary:
    //     Retrieves a bool or default value if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual bool GetBool(string key, bool defaultValue = false)
    {
        if (attributes.TryGetValue(key) is BoolAttribute boolAttribute)
        {
            return boolAttribute.value;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves an int or default value if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual int GetInt(string key, int defaultValue = 0)
    {
        if (attributes.TryGetValue(key) is IntAttribute intAttribute)
        {
            return intAttribute.value;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Same as (int)GetDecimal(key, defValue);
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual int GetAsInt(string key, int defaultValue = 0)
    {
        return (int)GetDecimal(key, defaultValue);
    }

    public virtual bool GetAsBool(string key, bool defaultValue = false)
    {
        IAttribute attribute = attributes.TryGetValue(key);
        if (attribute is IntAttribute)
        {
            return (int)attribute.GetValue() > 0;
        }

        if (attribute is FloatAttribute)
        {
            return (float)attribute.GetValue() > 0f;
        }

        if (attribute is DoubleAttribute)
        {
            return (double)attribute.GetValue() > 0.0;
        }

        if (attribute is LongAttribute)
        {
            return (long)attribute.GetValue() > 0;
        }

        if (attribute is StringAttribute)
        {
            if (!((string)attribute.GetValue() == "true"))
            {
                return (string)attribute.GetValue() == "1";
            }

            return true;
        }

        if (attribute is BoolAttribute)
        {
            return (bool)attribute.GetValue();
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves an int, float, long or double value. Whatever attribute is found for
    //     given key, in aformentioned order. If its a string its converted to double
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual double GetDecimal(string key, double defaultValue = 0.0)
    {
        IAttribute attribute = attributes.TryGetValue(key);
        if (attribute is IntAttribute)
        {
            return (int)attribute.GetValue();
        }

        if (attribute is FloatAttribute)
        {
            return (float)attribute.GetValue();
        }

        if (attribute is DoubleAttribute)
        {
            return (double)attribute.GetValue();
        }

        if (attribute is LongAttribute)
        {
            return (long)attribute.GetValue();
        }

        if (attribute is StringAttribute)
        {
            return ((string)attribute.GetValue()).ToDouble();
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves a double or defaultValue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual double GetDouble(string key, double defaultValue = 0.0)
    {
        if (attributes.TryGetValue(key) is DoubleAttribute doubleAttribute)
        {
            return doubleAttribute.value;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves a float or defaultvalue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual float GetFloat(string key, float defaultValue = 0f)
    {
        if (attributes.TryGetValue(key) is FloatAttribute floatAttribute)
        {
            return floatAttribute.value;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves a string attribute or defaultValue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual string GetString(string key, string defaultValue = null)
    {
        string text = (attributes.TryGetValue(key) as StringAttribute)?.value;
        if (text != null)
        {
            return text;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves the value of given attribute, independent of attribute type
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual string GetAsString(string key, string defaultValue = null)
    {
        string text = attributes.TryGetValue(key)?.GetValue().ToString();
        if (text != null)
        {
            return text;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves a string or defaultValue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual string[] GetStringArray(string key, string[] defaultValue = null)
    {
        string[] array = (attributes.TryGetValue(key) as StringArrayAttribute)?.value;
        if (array != null)
        {
            return array;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves a byte array or defaultValue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual byte[] GetBytes(string key, byte[] defaultValue = null)
    {
        byte[] array = (attributes.TryGetValue(key) as ByteArrayAttribute)?.value;
        if (array != null)
        {
            return array;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves an attribute tree or null if key is not found
    //
    // Parameters:
    //   key:
    public virtual ITreeAttribute GetTreeAttribute(string key)
    {
        return attributes.TryGetValue(key) as ITreeAttribute;
    }

    //
    // Summary:
    //     Retrieves an attribute tree or adds it if key is not found. Throws an exception
    //     if the key does exist but is not a tree.
    //
    // Parameters:
    //   key:
    public virtual ITreeAttribute GetOrAddTreeAttribute(string key)
    {
        IAttribute attribute = attributes.TryGetValue(key);
        if (attribute == null)
        {
            TreeAttribute treeAttribute = new TreeAttribute();
            SetAttribute(key, treeAttribute);
            return treeAttribute;
        }

        if (attribute is ITreeAttribute result)
        {
            return result;
        }

        throw new InvalidOperationException($"The attribute with key '{key}' is a {attribute.GetType().Name}, not TreeAttribute.");
    }

    //
    // Summary:
    //     Retrieves an itemstack or defaultValue if key is not found. Be sure to call stack.ResolveBlockOrItem()
    //     after retrieving it.
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public ItemStack GetItemstack(string key, ItemStack defaultValue = null)
    {
        ItemStack itemStack = ((ItemstackAttribute)attributes.TryGetValue(key))?.value;
        if (itemStack != null)
        {
            return itemStack;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Retrieves a long or default value if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    public virtual long GetLong(string key, long defaultValue = 0L)
    {
        return ((LongAttribute)attributes.TryGetValue(key))?.value ?? defaultValue;
    }

    //
    // Summary:
    //     Retrieves a long or null value if key is not found
    //
    // Parameters:
    //   key:
    public virtual long? TryGetLong(string key)
    {
        return ((LongAttribute)attributes.TryGetValue(key))?.value;
    }

    public virtual ModelTransform GetModelTransform(string key)
    {
        ITreeAttribute treeAttribute = GetTreeAttribute(key);
        if (treeAttribute == null)
        {
            return null;
        }

        ITreeAttribute treeAttribute2 = treeAttribute.GetTreeAttribute("origin");
        ITreeAttribute treeAttribute3 = treeAttribute.GetTreeAttribute("rotation");
        ITreeAttribute treeAttribute4 = treeAttribute.GetTreeAttribute("translation");
        float @float = treeAttribute.GetFloat("scale", 1f);
        Vec3f vec3f = new Vec3f(0.5f, 0.5f, 0.5f);
        if (treeAttribute2 != null)
        {
            vec3f.X = treeAttribute2.GetFloat("x");
            vec3f.Y = treeAttribute2.GetFloat("y");
            vec3f.Z = treeAttribute2.GetFloat("z");
        }

        Vec3f vec3f2 = new Vec3f();
        if (treeAttribute3 != null)
        {
            vec3f2.X = treeAttribute3.GetFloat("x");
            vec3f2.Y = treeAttribute3.GetFloat("y");
            vec3f2.Z = treeAttribute3.GetFloat("z");
        }

        Vec3f vec3f3 = new Vec3f();
        if (treeAttribute4 != null)
        {
            vec3f3.X = treeAttribute4.GetFloat("x");
            vec3f3.Y = treeAttribute4.GetFloat("y");
            vec3f3.Z = treeAttribute4.GetFloat("z");
        }

        return new ModelTransform
        {
            Scale = @float,
            Origin = vec3f,
            Translation = vec3f3,
            Rotation = vec3f2
        };
    }

    public object GetValue()
    {
        return this;
    }

    //
    // Summary:
    //     Creates a deep copy of the attribute tree
    public virtual ITreeAttribute Clone()
    {
        TreeAttribute treeAttribute = new TreeAttribute();
        foreach (KeyValuePair<string, IAttribute> attribute in attributes)
        {
            treeAttribute[attribute.Key] = attribute.Value.Clone();
        }

        return treeAttribute;
    }

    IAttribute IAttribute.Clone()
    {
        return Clone();
    }

    //
    // Summary:
    //     Returns true if given tree contains all of elements of this one, but given tree
    //     may contain also more elements. Individual node values are exactly matched.
    //
    // Parameters:
    //   worldForResolve:
    //
    //   other:
    public bool IsSubSetOf(IWorldAccessor worldForResolve, IAttribute other)
    {
        if (!(other is TreeAttribute))
        {
            return false;
        }

        TreeAttribute treeAttribute = (TreeAttribute)other;
        if (attributes.Count > treeAttribute.attributes.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, IAttribute> attribute in attributes)
        {
            if (GlobalConstants.IgnoredStackAttributes.Contains(attribute.Key))
            {
                continue;
            }

            if (!treeAttribute.attributes.ContainsKey(attribute.Key))
            {
                return false;
            }

            if (attribute.Value is TreeAttribute)
            {
                if (!(treeAttribute.attributes[attribute.Key] as TreeAttribute).IsSubSetOf(worldForResolve, attribute.Value))
                {
                    return false;
                }
            }
            else if (!treeAttribute.attributes[attribute.Key].Equals(worldForResolve, attribute.Value))
            {
                return false;
            }
        }

        return true;
    }

    //
    // Summary:
    //     Returns true if given tree exactly matches this one
    //
    // Parameters:
    //   worldForResolve:
    //
    //   other:
    public bool Equals(IWorldAccessor worldForResolve, IAttribute other)
    {
        if (!(other is TreeAttribute))
        {
            return false;
        }

        TreeAttribute treeAttribute = (TreeAttribute)other;
        if (attributes.Count != treeAttribute.attributes.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, IAttribute> attribute in attributes)
        {
            if (!treeAttribute.attributes.ContainsKey(attribute.Key))
            {
                return false;
            }

            if (!treeAttribute.attributes[attribute.Key].Equals(worldForResolve, attribute.Value))
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

        TreeAttribute treeAttribute = (TreeAttribute)other;
        if ((ignorePaths == null || ignorePaths.Length == 0) && attributes.Count != treeAttribute.attributes.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, IAttribute> attribute2 in attributes)
        {
            string value = currentPath + ((currentPath.Length > 0) ? "/" : "") + attribute2.Key;
            if (ignorePaths != null && ignorePaths.Contains(value))
            {
                continue;
            }

            if (!treeAttribute.attributes.ContainsKey(attribute2.Key))
            {
                return false;
            }

            IAttribute attribute = treeAttribute.attributes[attribute2.Key];
            if (attribute is TreeAttribute)
            {
                if (!((TreeAttribute)attribute).Equals(worldForResolve, attribute2.Value, currentPath, ignorePaths))
                {
                    return false;
                }
            }
            else if (attribute is ItemstackAttribute)
            {
                if (!(attribute as ItemstackAttribute).Equals(worldForResolve, attribute2.Value, ignorePaths))
                {
                    return false;
                }
            }
            else if (!attribute.Equals(worldForResolve, attribute2.Value))
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, IAttribute> attribute3 in treeAttribute.attributes)
        {
            string value2 = currentPath + ((currentPath.Length > 0) ? "/" : "") + attribute3.Key;
            if ((ignorePaths == null || !ignorePaths.Contains(value2)) && !attributes.ContainsKey(attribute3.Key))
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
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("{ ");
        int num = 0;
        foreach (KeyValuePair<string, IAttribute> attribute in attributes)
        {
            if (num > 0)
            {
                stringBuilder.Append(", ");
            }

            num++;
            stringBuilder.Append("\"" + attribute.Key + "\": " + attribute.Value.ToJsonToken());
        }

        stringBuilder.Append(" }");
        return stringBuilder.ToString();
    }

    //
    // Summary:
    //     Merges the sourceTree into the current one
    //
    // Parameters:
    //   sourceTree:
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
        foreach (KeyValuePair<string, IAttribute> attribute in srcTree.attributes)
        {
            MergeAttribute(dstTree, attribute.Key, attribute.Value);
        }
    }

    protected static void MergeAttribute(TreeAttribute dstTree, string srcKey, IAttribute srcAttr)
    {
        IAttribute attribute = dstTree.attributes.TryGetValue(srcKey);
        if (attribute == null)
        {
            dstTree.attributes[srcKey] = srcAttr.Clone();
            return;
        }

        if (attribute.GetAttributeId() != srcAttr.GetAttributeId())
        {
            throw new Exception("Cannot merge attributes! Expected attributeId " + attribute.GetAttributeId() + " instead of " + srcAttr.GetAttributeId() + "! Existing: " + attribute.ToString() + ", new: " + srcAttr.ToString());
        }

        if (srcAttr is ITreeAttribute)
        {
            MergeTree(attribute as TreeAttribute, srcAttr as TreeAttribute);
        }
        else
        {
            dstTree.attributes[srcKey] = srcAttr.Clone();
        }
    }

    public OrderedDictionary<string, IAttribute> SortedCopy(bool recursive = false)
    {
        IOrderedEnumerable<KeyValuePair<string, IAttribute>> orderedEnumerable = attributes.OrderBy((KeyValuePair<string, IAttribute> x) => x.Key);
        OrderedDictionary<string, IAttribute> orderedDictionary = new OrderedDictionary<string, IAttribute>();
        foreach (KeyValuePair<string, IAttribute> item in orderedEnumerable)
        {
            IAttribute attribute = item.Value;
            TreeAttribute treeAttribute = attribute as TreeAttribute;
            if (treeAttribute != null && recursive)
            {
                attribute = treeAttribute.ConsistentlyOrderedCopy();
            }

            orderedDictionary.Add(item.Key, attribute);
        }

        return orderedDictionary;
    }

    private IAttribute ConsistentlyOrderedCopy()
    {
        Dictionary<string, IAttribute> dictionary = attributes.OrderBy((KeyValuePair<string, IAttribute> x) => x.Key).ToDictionary((KeyValuePair<string, IAttribute> pair) => pair.Key, (KeyValuePair<string, IAttribute> pair) => pair.Value);
        foreach (var (key, attribute2) in dictionary)
        {
            if (attribute2 is TreeAttribute treeAttribute)
            {
                dictionary[key] = treeAttribute.ConsistentlyOrderedCopy();
            }
        }

        TreeAttribute treeAttribute2 = new TreeAttribute();
        treeAttribute2.attributes.AddRange(dictionary);
        return treeAttribute2;
    }

    public override int GetHashCode()
    {
        return GetHashCode(null);
    }

    public int GetHashCode(string[] ignoredAttributes)
    {
        int num = 0;
        int num2 = 0;
        foreach (KeyValuePair<string, IAttribute> attribute in attributes)
        {
            if (ignoredAttributes == null || !ignoredAttributes.Contains(attribute.Key))
            {
                num = ((!(attribute.Value is ITreeAttribute treeAttribute)) ? ((num2 != 0) ? (num ^ (attribute.Key.GetHashCode() ^ attribute.Value.GetHashCode())) : (attribute.Key.GetHashCode() ^ attribute.Value.GetHashCode())) : ((num2 != 0) ? (num ^ (attribute.Key.GetHashCode() ^ treeAttribute.GetHashCode(ignoredAttributes))) : (attribute.Key.GetHashCode() ^ treeAttribute.GetHashCode(ignoredAttributes))));
                num2++;
            }
        }

        return num;
    }

    Type IAttribute.GetType()
    {
        return GetType();
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
