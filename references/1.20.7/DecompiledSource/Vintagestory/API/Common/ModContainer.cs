#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Mono.Cecil;
using Newtonsoft.Json;
using ProperVersion;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class ModContainer : Mod
{
    public List<string> MissingDependencies;

    private string selectedAssemblyFile;

    private static HashAlgorithm fileHasher = SHA1.Create();

    public bool Enabled => Status == ModStatus.Enabled;

    public ModStatus Status { get; set; } = ModStatus.Enabled;


    public ModError? Error { get; set; }

    public string FolderPath { get; private set; }

    public List<string> SourceFiles { get; } = new List<string>();


    public List<string> AssemblyFiles { get; } = new List<string>();


    public bool RequiresCompilation => SourceFiles.Count > 0;

    public Assembly Assembly { get; private set; }

    public ModContainer(FileSystemInfo fsInfo, ILogger parentLogger, bool logDebug)
    {
        base.SourceType = GetSourceType(fsInfo).Value;
        base.FileName = fsInfo.Name;
        base.SourcePath = fsInfo.FullName;
        base.Logger = new ModLogger(parentLogger, this);
        base.Logger.TraceLog = logDebug;
        switch (base.SourceType)
        {
            case EnumModSourceType.CS:
                SourceFiles.Add(base.SourcePath);
                break;
            case EnumModSourceType.DLL:
                AssemblyFiles.Add(base.SourcePath);
                break;
            case EnumModSourceType.Folder:
                FolderPath = base.SourcePath;
                break;
            case EnumModSourceType.ZIP:
                break;
        }
    }

    public static EnumModSourceType? GetSourceType(FileSystemInfo fsInfo)
    {
        if (fsInfo is DirectoryInfo)
        {
            return EnumModSourceType.Folder;
        }

        return GetSourceTypeFromExtension(fsInfo.Name);
    }

    private static EnumModSourceType? GetSourceTypeFromExtension(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            return null;
        }

        extension = extension.Substring(1).ToUpperInvariant();
        if (!Enum.TryParse<EnumModSourceType>(extension, out var result))
        {
            return null;
        }

        return result;
    }

    public void SetError(ModError error)
    {
        Status = ModStatus.Errored;
        Error = error;
    }

    public void Unpack(string unpackPath)
    {
        //IL_00e3: Unknown result type (might be due to invalid IL or missing references)
        //IL_00ea: Expected O, but got Unknown
        //IL_00ff: Unknown result type (might be due to invalid IL or missing references)
        //IL_0106: Expected O, but got Unknown
        if (!Enabled || SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
        {
            return;
        }

        if (base.SourceType == EnumModSourceType.ZIP)
        {
            using (FileStream inputStream = File.OpenRead(base.SourcePath))
            {
                byte[] source = fileHasher.ComputeHash(inputStream);
                StringBuilder stringBuilder = new StringBuilder(12);
                foreach (byte item in source.Take(6))
                {
                    stringBuilder.Append(item.ToString("x2"));
                }

                FolderPath = Path.Combine(unpackPath, base.FileName + "_" + stringBuilder.ToString());
            }

            if (!Directory.Exists(FolderPath))
            {
                try
                {
                    Directory.CreateDirectory(FolderPath);
                    ZipFile val = new ZipFile(base.SourcePath, (StringCodec)null);
                    try
                    {
                        foreach (ZipEntry item2 in val)
                        {
                            ZipEntry val2 = item2;
                            string path = Path.Combine(FolderPath, val2.Name);
                            if (val2.IsDirectory)
                            {
                                Directory.CreateDirectory(path);
                                continue;
                            }

                            string directoryName = Path.GetDirectoryName(path);
                            if (!Directory.Exists(directoryName))
                            {
                                Directory.CreateDirectory(directoryName);
                            }

                            using Stream stream = val.GetInputStream(val2);
                            using FileStream destination = new FileStream(path, FileMode.Create);
                            stream.CopyTo(destination);
                        }
                    }
                    finally
                    {
                        ((IDisposable)val)?.Dispose();
                    }
                }
                catch (Exception e)
                {
                    base.Logger.Error("An exception was thrown when trying to extract the mod archive to '{0}':", FolderPath);
                    base.Logger.Error(e);
                    SetError(ModError.Loading);
                    try
                    {
                        Directory.Delete(FolderPath, recursive: true);
                        return;
                    }
                    catch (Exception)
                    {
                        base.Logger.Error("Additionally, there was an exception when deleting cached mod folder path '{0}':", FolderPath);
                        base.Logger.Error(e);
                        return;
                    }
                }
            }
        }

        string text = Path.Combine(FolderPath, ".ignore");
        IgnoreFile ignoreFile = (File.Exists(text) ? new IgnoreFile(text, FolderPath) : null);
        foreach (string item3 in Directory.EnumerateFiles(FolderPath, "*", SearchOption.AllDirectories))
        {
            if (ignoreFile != null && !ignoreFile.Available(item3))
            {
                continue;
            }

            EnumModSourceType? sourceTypeFromExtension = GetSourceTypeFromExtension(item3);
            string text2 = item3.Substring(FolderPath.Length + 1);
            int num = text2.IndexOfAny(new char[2] { '/', '\\' });
            string text3 = ((num >= 0) ? text2.Substring(0, num) : null);
            switch (sourceTypeFromExtension)
            {
                case EnumModSourceType.CS:
                    if (text3 != "src")
                    {
                        base.Logger.Error("File '{0}' is not in the 'src/' subfolder.", Path.GetFileName(item3));
                        if (base.SourceType != EnumModSourceType.Folder)
                        {
                            SetError(ModError.Loading);
                            return;
                        }
                    }
                    else
                    {
                        SourceFiles.Add(item3);
                    }

                    break;
                case EnumModSourceType.DLL:
                    if (text3 == "native")
                    {
                        break;
                    }

                    if (text3 != null)
                    {
                        base.Logger.Error("File '{0}' is not in the mod's root folder. Won't load this mod. If you need to ship unmanaged dlls, put them in the native/ folder.", Path.GetFileName(item3));
                        if (base.SourceType != EnumModSourceType.Folder)
                        {
                            SetError(ModError.Loading);
                            return;
                        }
                    }
                    else
                    {
                        AssemblyFiles.Add(item3);
                    }

                    break;
            }
        }
    }

    public void LoadModInfo(ModCompilationContext compilationContext, ModAssemblyLoader loader)
    {
        //IL_00e8: Unknown result type (might be due to invalid IL or missing references)
        //IL_00ef: Expected O, but got Unknown
        if (!Enabled || base.Info != null)
        {
            return;
        }

        try
        {
            if (base.SourceType == EnumModSourceType.ZIP || base.SourceType == EnumModSourceType.Folder)
            {
                if (FolderPath != null)
                {
                    string path = Path.Combine(FolderPath, "modinfo.json");
                    if (File.Exists(path))
                    {
                        string value = File.ReadAllText(path);
                        base.Info = JsonConvert.DeserializeObject<ModInfo>(value);
                        base.Info?.Init();
                    }

                    path = Path.Combine(FolderPath, "worldconfig.json");
                    if (File.Exists(path))
                    {
                        string value2 = File.ReadAllText(path);
                        base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(value2);
                    }

                    string text = Path.Combine(FolderPath, "modicon.png");
                    if (!File.Exists(text))
                    {
                        text = Path.Combine(FolderPath, "textures/gui/3rdpartymodicon.png");
                    }

                    if (File.Exists(text))
                    {
                        base.Icon = new BitmapExternal(text);
                    }
                }
                else
                {
                    ZipFile val = new ZipFile(base.SourcePath, (StringCodec)null);
                    try
                    {
                        ZipEntry entry = val.GetEntry("modinfo.json");
                        if (entry != null)
                        {
                            using StreamReader streamReader = new StreamReader(val.GetInputStream(entry));
                            string value3 = streamReader.ReadToEnd();
                            base.Info = JsonConvert.DeserializeObject<ModInfo>(value3);
                            base.Info?.Init();
                        }

                        entry = val.GetEntry("worldconfig.json");
                        if (entry != null)
                        {
                            using StreamReader streamReader2 = new StreamReader(val.GetInputStream(entry));
                            string value4 = streamReader2.ReadToEnd();
                            base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(value4);
                        }

                        entry = val.GetEntry("modicon.png");
                        if (entry != null)
                        {
                            base.Icon = new BitmapExternal(val.GetInputStream(entry));
                        }
                        else
                        {
                            string text2 = Path.Combine(GamePaths.AssetsPath, "game", "textures", "gui", "3rdpartymodicon.png");
                            if (File.Exists(text2))
                            {
                                base.Icon = new BitmapExternal(text2);
                            }
                        }
                    }
                    finally
                    {
                        ((IDisposable)val)?.Dispose();
                    }
                }

                if (base.Info == null)
                {
                    base.Logger.Error("Missing modinfo.json");
                    SetError(ModError.Loading);
                }

                if (SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
                {
                    base.Logger.Warning("Is a {0} mod, but .cs or .dll files were found. These will be ignored.", base.SourceType);
                }
            }
            else
            {
                base.Info = LoadModInfoFromCode(compilationContext, loader, out var modWorldConfig, out var iconPath);
                base.Info?.Init();
                base.WorldConfig = modWorldConfig;
                if (iconPath != null)
                {
                    string text3 = Path.Combine(GamePaths.AssetsPath, iconPath);
                    if (File.Exists(text3))
                    {
                        base.Icon = new BitmapExternal(text3);
                    }
                }

                if (base.Info == null)
                {
                    base.Logger.Error("Missing ModInfoAttribute");
                    SetError(ModError.Loading);
                }
            }

            if (base.Info != null)
            {
                CheckProperVersions();
            }
        }
        catch (Exception e)
        {
            base.Logger.Error("An exception was thrown trying to to load the ModInfo:");
            base.Logger.Error(e);
            SetError(ModError.Loading);
        }
    }

    private ModInfo LoadModInfoFromCode(ModCompilationContext compilationContext, ModAssemblyLoader loader, out ModWorldConfiguration modWorldConfig, out string iconPath)
    {
        ModInfo result;
        if (RequiresCompilation)
        {
            if (AssemblyFiles.Count > 0)
            {
                throw new Exception("Found both .cs and .dll files, this is not supported");
            }

            Assembly = compilationContext.CompileFromFiles(this);
            base.Logger.Notification("Successfully compiled {0} source files", SourceFiles.Count);
            base.Logger.VerboseDebug("Successfully compiled {0} source files", SourceFiles.Count);
            result = LoadModInfoFromAssembly(Assembly, out modWorldConfig, out iconPath);
        }
        else
        {
            base.Logger.VerboseDebug("Check for mod systems in mod {0}", string.Join(", ", AssemblyFiles));
            List<string> list = AssemblyFiles.Where((string file) => isEligible(file)).ToList();
            if (list.Count == 0)
            {
                throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", AssemblyFiles)));
            }

            if (list.Count >= 2)
            {
                throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
            }

            selectedAssemblyFile = list[0];
            base.Logger.VerboseDebug("Selected assembly {0}", selectedAssemblyFile);
            result = LoadModInfoFromAssemblyDefinition(loader.LoadAssemblyDefinition(selectedAssemblyFile), out modWorldConfig, out iconPath);
        }

        if (iconPath == null)
        {
            iconPath = "game/textures/gui/3rdpartymodicon.png";
        }

        return result;
        bool isEligible(string path)
        {
            AssemblyDefinition val2 = loader.LoadAssemblyDefinition(path);
            if (((IEnumerable<CustomAttribute>)val2.CustomAttributes).Any((CustomAttribute attribute) => ((MemberReference)attribute.AttributeType).Name == "ModInfoAttribute"))
            {
                return ((IEnumerable<ModuleDefinition>)val2.Modules).SelectMany((ModuleDefinition module) => (IEnumerable<TypeDefinition>)module.Types).Any((TypeDefinition type) => !type.IsAbstract && isModSystem(type));
            }

            return false;
        }

        static bool isModSystem(TypeDefinition typeDefinition)
        {
            TypeReference val = typeDefinition.BaseType;
            while (val != null)
            {
                if (((MemberReference)val).FullName == typeof(ModSystem).FullName)
                {
                    return true;
                }

                TypeDefinition obj = val.Resolve();
                val = ((obj != null) ? obj.BaseType : null);
            }

            return false;
        }
    }

    public void LoadAssembly(ModCompilationContext compilationContext, ModAssemblyLoader loader)
    {
        EnumModType enumModType = base.Info?.Type ?? EnumModType.Code;
        if (!Enabled || Assembly != null)
        {
            return;
        }

        if (enumModType != EnumModType.Code)
        {
            if (SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
            {
                base.Logger.Warning("Is a {0} mod, but .cs or .dll files were found. These will be ignored.", enumModType);
            }

            return;
        }

        try
        {
            if (RequiresCompilation)
            {
                Assembly = compilationContext.CompileFromFiles(this);
                base.Logger.Notification("Successfully compiled {0} source files", SourceFiles.Count);
                base.Logger.VerboseDebug("Successfully compiled {0} source files", SourceFiles.Count);
                return;
            }

            if (selectedAssemblyFile != null)
            {
                Assembly = loader.LoadFrom(selectedAssemblyFile);
                return;
            }

            base.Logger.VerboseDebug("Check for mod systems in mod {0}", string.Join(", ", AssemblyFiles));
            List<Assembly> list = (from path in AssemblyFiles
                                   select loader.LoadFrom(path) into ass
                                   where ass.GetCustomAttribute<ModInfoAttribute>() != null || GetModSystems(ass).Any()
                                   select ass).ToList();
            if (list.Count == 0)
            {
                throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", AssemblyFiles)));
            }

            if (list.Count >= 2)
            {
                throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
            }

            Assembly = list[0];
            base.Logger.VerboseDebug("Loaded assembly {0}", Assembly.Location);
        }
        catch (Exception e)
        {
            base.Logger.Error("An exception was thrown when trying to load assembly:");
            base.Logger.Error(e);
            SetError(ModError.Loading);
        }
    }

    public void InstantiateModSystems(EnumAppSide side)
    {
        if (!Enabled || Assembly == null || base.Systems.Count > 0)
        {
            return;
        }

        if (base.Info == null)
        {
            throw new InvalidOperationException("LoadModInfo was not called before InstantiateModSystems");
        }

        if (!base.Info.Side.Is(side))
        {
            Status = ModStatus.Unused;
            return;
        }

        List<ModSystem> list = new List<ModSystem>();
        foreach (Type modSystem2 in GetModSystems(Assembly))
        {
            try
            {
                ModSystem modSystem = (ModSystem)Activator.CreateInstance(modSystem2);
                modSystem.Mod = this;
                list.Add(modSystem);
            }
            catch (Exception e)
            {
                base.Logger.Error("Exception thrown when trying to create an instance of ModSystem {0}:", modSystem2);
                base.Logger.Error(e);
            }
        }

        base.Systems = list.AsReadOnly();
        if (base.Systems.Count == 0 && FolderPath == null)
        {
            base.Logger.Warning("Is a Code mod, but no ModSystems found");
        }
    }

    private IEnumerable<Type> GetModSystems(Assembly assembly)
    {
        try
        {
            return from type in assembly.GetTypes()
                   where typeof(ModSystem).IsAssignableFrom(type) && !type.IsAbstract
                   select type;
        }
        catch (Exception ex)
        {
            if (ex is ReflectionTypeLoadException)
            {
                Exception[] loaderExceptions = (ex as ReflectionTypeLoadException).LoaderExceptions;
                base.Logger.Error("Exception thrown when attempting to retrieve all types of the assembly {0}. Will ignore asssembly. Loader exceptions:", assembly.FullName);
                base.Logger.Error(ex);
                if (ex.InnerException != null)
                {
                    base.Logger.Error("InnerException:");
                    base.Logger.Error(ex.InnerException);
                }

                for (int i = 0; i < loaderExceptions.Length; i++)
                {
                    base.Logger.Error(loaderExceptions[i]);
                }
            }
            else
            {
                base.Logger.Error("Exception thrown when attempting to retrieve all types of the assembly {0}: {1}, InnerException: {2}. Will ignore asssembly.", assembly.FullName, ex, ex.InnerException);
            }

            return Enumerable.Empty<Type>();
        }
    }

    private ModInfo LoadModInfoFromAssembly(Assembly assembly, out ModWorldConfiguration modWorldConfig, out string iconPath)
    {
        ModInfoAttribute customAttribute = assembly.GetCustomAttribute<ModInfoAttribute>();
        if (customAttribute == null)
        {
            modWorldConfig = null;
            iconPath = null;
            return null;
        }

        List<ModDependency> dependencies = (from attr in assembly.GetCustomAttributes<ModDependencyAttribute>()
                                            select new ModDependency(attr.ModID, attr.Version)).ToList();
        return LoadModInfoFromModInfoAttribute(customAttribute, dependencies, out modWorldConfig, out iconPath);
    }

    private ModInfo LoadModInfoFromAssemblyDefinition(AssemblyDefinition assemblyDefinition, out ModWorldConfiguration modWorldConfig, out string iconPath)
    {
        //IL_003d: Unknown result type (might be due to invalid IL or missing references)
        //IL_0042: Unknown result type (might be due to invalid IL or missing references)
        //IL_0058: Unknown result type (might be due to invalid IL or missing references)
        //IL_005d: Unknown result type (might be due to invalid IL or missing references)
        //IL_00ac: Unknown result type (might be due to invalid IL or missing references)
        //IL_00b1: Unknown result type (might be due to invalid IL or missing references)
        //IL_00c9: Unknown result type (might be due to invalid IL or missing references)
        //IL_00ce: Unknown result type (might be due to invalid IL or missing references)
        //IL_011c: Unknown result type (might be due to invalid IL or missing references)
        //IL_0121: Unknown result type (might be due to invalid IL or missing references)
        CustomAttribute val = ((IEnumerable<CustomAttribute>)assemblyDefinition.CustomAttributes).SingleOrDefault((System.Func<CustomAttribute, bool>)((CustomAttribute attribute) => ((MemberReference)attribute.AttributeType).Name == "ModInfoAttribute"));
        if (val == null)
        {
            modWorldConfig = null;
            iconPath = null;
            return null;
        }

        CustomAttributeArgument val2 = val.ConstructorArguments[0];
        string name = ((CustomAttributeArgument)(ref val2)).Value as string;
        val2 = val.ConstructorArguments[1];
        string modID = ((CustomAttributeArgument)(ref val2)).Value as string;
        ModInfoAttribute modInfoAttribute = new ModInfoAttribute(name, modID);
        foreach (CustomAttributeNamedArgument item in ((IEnumerable<CustomAttributeNamedArgument>)val.Properties).Where((CustomAttributeNamedArgument p) => ((CustomAttributeNamedArgument)(ref p)).Name != "Name" && ((CustomAttributeNamedArgument)(ref p)).Name != "ModID"))
        {
            CustomAttributeNamedArgument current = item;
            PropertyInfo property = modInfoAttribute.GetType().GetProperty(((CustomAttributeNamedArgument)(ref current)).Name);
            val2 = ((CustomAttributeNamedArgument)(ref current)).Argument;
            if (((CustomAttributeArgument)(ref val2)).Value is CustomAttributeArgument[] source)
            {
                property.SetValue(modInfoAttribute, source.Select((CustomAttributeArgument item) => ((CustomAttributeArgument)(ref item)).Value as string).ToArray());
            }
            else
            {
                val2 = ((CustomAttributeNamedArgument)(ref current)).Argument;
                property.SetValue(modInfoAttribute, ((CustomAttributeArgument)(ref val2)).Value);
            }
        }

        List<ModDependency> dependencies = ((IEnumerable<CustomAttribute>)assemblyDefinition.CustomAttributes).Where((CustomAttribute attribute) => ((MemberReference)attribute.AttributeType).Name == "ModDependencyAttribute").Select(delegate (CustomAttribute attribute)
        {
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_000c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0020: Unknown result type (might be due to invalid IL or missing references)
            //IL_0025: Unknown result type (might be due to invalid IL or missing references)
            CustomAttributeArgument val3 = attribute.ConstructorArguments[0];
            string modID2 = (string)((CustomAttributeArgument)(ref val3)).Value;
            val3 = attribute.ConstructorArguments[1];
            return new ModDependency(modID2, ((CustomAttributeArgument)(ref val3)).Value as string);
        }).ToList();
        return LoadModInfoFromModInfoAttribute(modInfoAttribute, dependencies, out modWorldConfig, out iconPath);
    }

    private ModInfo LoadModInfoFromModInfoAttribute(ModInfoAttribute modInfoAttr, List<ModDependency> dependencies, out ModWorldConfiguration modWorldConfig, out string iconPath)
    {
        if (!Enum.TryParse<EnumAppSide>(modInfoAttr.Side, ignoreCase: true, out var result))
        {
            base.Logger.Warning("Cannot parse '{0}', must be either 'Client', 'Server' or 'Universal'. Defaulting to 'Universal'.", modInfoAttr.Side);
            result = EnumAppSide.Universal;
        }

        if (modInfoAttr.WorldConfig != null)
        {
            modWorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(modInfoAttr.WorldConfig);
        }
        else
        {
            modWorldConfig = null;
        }

        ModInfo result2 = new ModInfo(EnumModType.Code, modInfoAttr.Name, modInfoAttr.ModID, modInfoAttr.Version, modInfoAttr.Description, modInfoAttr.Authors, modInfoAttr.Contributors, modInfoAttr.Website, result, modInfoAttr.RequiredOnClient, modInfoAttr.RequiredOnServer, dependencies)
        {
            NetworkVersion = modInfoAttr.NetworkVersion,
            CoreMod = modInfoAttr.CoreMod
        };
        iconPath = modInfoAttr.IconPath;
        return result2;
    }

    private void CheckProperVersions()
    {
        if (!string.IsNullOrEmpty(base.Info.Version) && !SemVer.TryParse(base.Info.Version, out var result, out var error))
        {
            base.Logger.Warning("{0} (best guess: {1})", error, result);
        }

        foreach (ModDependency dependency in base.Info.Dependencies)
        {
            if (!(dependency.Version == "*") && !string.IsNullOrEmpty(dependency.Version) && !SemVer.TryParse(dependency.Version, out result, out error))
            {
                base.Logger.Warning("Dependency '{0}': {1} (best guess: {2})", dependency.ModID, error, result);
            }
        }
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
Resolve: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
------------------
Resolve: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
Could not find by name: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
Could not find by name: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
Could not find by name: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Sockets.dll'
------------------
Resolve: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
Could not find by name: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
------------------
Resolve: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\gwlar\.nuget\packages\system.collections.immutable\8.0.0\lib\net7.0\System.Collections.Immutable.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
Could not find by name: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Compression.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\0Harmony.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Audio.OpenAL.dll'
------------------
Resolve: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Mathematics.dll'
------------------
Resolve: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Common.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
Could not find by name: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
------------------
Resolve: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Pipes.dll'
------------------
Resolve: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Graphics.dll'
------------------
Resolve: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.FileVersionInfo.dll'
------------------
Resolve: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.dll'
------------------
Resolve: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
Could not find by name: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
------------------
Resolve: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
Could not find by name: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
