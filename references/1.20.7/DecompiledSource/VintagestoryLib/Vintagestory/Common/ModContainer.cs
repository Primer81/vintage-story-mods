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
		string ext = Path.GetExtension(fileName);
		if (string.IsNullOrEmpty(ext))
		{
			return null;
		}
		ext = ext.Substring(1).ToUpperInvariant();
		if (!Enum.TryParse<EnumModSourceType>(ext, out var type))
		{
			return null;
		}
		return type;
	}

	public void SetError(ModError error)
	{
		Status = ModStatus.Errored;
		Error = error;
	}

	public void Unpack(string unpackPath)
	{
		if (!Enabled || SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
		{
			return;
		}
		if (base.SourceType == EnumModSourceType.ZIP)
		{
			using (FileStream stream = File.OpenRead(base.SourcePath))
			{
				byte[] source = fileHasher.ComputeHash(stream);
				StringBuilder sb = new StringBuilder(12);
				foreach (byte item in source.Take(6))
				{
					sb.Append(item.ToString("x2"));
				}
				FolderPath = Path.Combine(unpackPath, base.FileName + "_" + sb.ToString());
			}
			if (!Directory.Exists(FolderPath))
			{
				try
				{
					Directory.CreateDirectory(FolderPath);
					using ZipFile zipFile = new ZipFile(base.SourcePath);
					foreach (ZipEntry entry in zipFile)
					{
						string outputPath = Path.Combine(FolderPath, entry.Name);
						if (entry.IsDirectory)
						{
							Directory.CreateDirectory(outputPath);
							continue;
						}
						string outputDirectory = Path.GetDirectoryName(outputPath);
						if (!Directory.Exists(outputDirectory))
						{
							Directory.CreateDirectory(outputDirectory);
						}
						using Stream inputStream = zipFile.GetInputStream(entry);
						using FileStream outputStream = new FileStream(outputPath, FileMode.Create);
						inputStream.CopyTo(outputStream);
					}
				}
				catch (Exception ex)
				{
					base.Logger.Error("An exception was thrown when trying to extract the mod archive to '{0}':", FolderPath);
					base.Logger.Error(ex);
					SetError(ModError.Loading);
					try
					{
						Directory.Delete(FolderPath, recursive: true);
						return;
					}
					catch (Exception)
					{
						base.Logger.Error("Additionally, there was an exception when deleting cached mod folder path '{0}':", FolderPath);
						base.Logger.Error(ex);
						return;
					}
				}
			}
		}
		string ignoreFilename = Path.Combine(FolderPath, ".ignore");
		IgnoreFile ignoreFile = (File.Exists(ignoreFilename) ? new IgnoreFile(ignoreFilename, FolderPath) : null);
		foreach (string path in Directory.EnumerateFiles(FolderPath, "*", SearchOption.AllDirectories))
		{
			if (ignoreFile != null && !ignoreFile.Available(path))
			{
				continue;
			}
			EnumModSourceType? type = GetSourceTypeFromExtension(path);
			string relativePath = path.Substring(FolderPath.Length + 1);
			int slashIndex = relativePath.IndexOfAny(new char[2] { '/', '\\' });
			string topFolderName = ((slashIndex >= 0) ? relativePath.Substring(0, slashIndex) : null);
			switch (type)
			{
			case EnumModSourceType.CS:
				if (topFolderName != "src")
				{
					base.Logger.Error("File '{0}' is not in the 'src/' subfolder.", Path.GetFileName(path));
					if (base.SourceType != EnumModSourceType.Folder)
					{
						SetError(ModError.Loading);
						return;
					}
				}
				else
				{
					SourceFiles.Add(path);
				}
				break;
			case EnumModSourceType.DLL:
				if (topFolderName == "native")
				{
					break;
				}
				if (topFolderName != null)
				{
					base.Logger.Error("File '{0}' is not in the mod's root folder. Won't load this mod. If you need to ship unmanaged dlls, put them in the native/ folder.", Path.GetFileName(path));
					if (base.SourceType != EnumModSourceType.Folder)
					{
						SetError(ModError.Loading);
						return;
					}
				}
				else
				{
					AssemblyFiles.Add(path);
				}
				break;
			}
		}
	}

	public void LoadModInfo(ModCompilationContext compilationContext, ModAssemblyLoader loader)
	{
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
						string content2 = File.ReadAllText(path);
						base.Info = JsonConvert.DeserializeObject<ModInfo>(content2);
						base.Info?.Init();
					}
					path = Path.Combine(FolderPath, "worldconfig.json");
					if (File.Exists(path))
					{
						string content = File.ReadAllText(path);
						base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(content);
					}
					string iconpath = Path.Combine(FolderPath, "modicon.png");
					if (!File.Exists(iconpath))
					{
						iconpath = Path.Combine(FolderPath, "textures/gui/3rdpartymodicon.png");
					}
					if (File.Exists(iconpath))
					{
						base.Icon = new BitmapExternal(iconpath);
					}
				}
				else
				{
					using ZipFile zip = new ZipFile(base.SourcePath);
					ZipEntry entry = zip.GetEntry("modinfo.json");
					if (entry != null)
					{
						using StreamReader streamReader = new StreamReader(zip.GetInputStream(entry));
						string content4 = streamReader.ReadToEnd();
						base.Info = JsonConvert.DeserializeObject<ModInfo>(content4);
						base.Info?.Init();
					}
					entry = zip.GetEntry("worldconfig.json");
					if (entry != null)
					{
						using StreamReader reader = new StreamReader(zip.GetInputStream(entry));
						string content3 = reader.ReadToEnd();
						base.WorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(content3);
					}
					entry = zip.GetEntry("modicon.png");
					if (entry != null)
					{
						base.Icon = new BitmapExternal(zip.GetInputStream(entry));
					}
					else
					{
						string iconpath2 = Path.Combine(GamePaths.AssetsPath, "game", "textures", "gui", "3rdpartymodicon.png");
						if (File.Exists(iconpath2))
						{
							base.Icon = new BitmapExternal(iconpath2);
						}
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
				base.Info = LoadModInfoFromCode(compilationContext, loader, out var worldConfig, out var iconPath);
				base.Info?.Init();
				base.WorldConfig = worldConfig;
				if (iconPath != null)
				{
					string filePath = Path.Combine(GamePaths.AssetsPath, iconPath);
					if (File.Exists(filePath))
					{
						base.Icon = new BitmapExternal(filePath);
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
		catch (Exception ex)
		{
			base.Logger.Error("An exception was thrown trying to to load the ModInfo:");
			base.Logger.Error(ex);
			SetError(ModError.Loading);
		}
	}

	private ModInfo LoadModInfoFromCode(ModCompilationContext compilationContext, ModAssemblyLoader loader, out ModWorldConfiguration modWorldConfig, out string iconPath)
	{
		ModInfo modInfo;
		if (RequiresCompilation)
		{
			if (AssemblyFiles.Count > 0)
			{
				throw new Exception("Found both .cs and .dll files, this is not supported");
			}
			Assembly = compilationContext.CompileFromFiles(this);
			base.Logger.Notification("Successfully compiled {0} source files", SourceFiles.Count);
			base.Logger.VerboseDebug("Successfully compiled {0} source files", SourceFiles.Count);
			modInfo = LoadModInfoFromAssembly(Assembly, out modWorldConfig, out iconPath);
		}
		else
		{
			base.Logger.VerboseDebug("Check for mod systems in mod {0}", string.Join(", ", AssemblyFiles));
			List<string> assemblyCandidates = AssemblyFiles.Where((string file) => isEligible(file)).ToList();
			if (assemblyCandidates.Count == 0)
			{
				throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", AssemblyFiles)));
			}
			if (assemblyCandidates.Count >= 2)
			{
				throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
			}
			selectedAssemblyFile = assemblyCandidates[0];
			base.Logger.VerboseDebug("Selected assembly {0}", selectedAssemblyFile);
			modInfo = LoadModInfoFromAssemblyDefinition(loader.LoadAssemblyDefinition(selectedAssemblyFile), out modWorldConfig, out iconPath);
		}
		if (iconPath == null)
		{
			iconPath = "game/textures/gui/3rdpartymodicon.png";
		}
		return modInfo;
		bool isEligible(string path)
		{
			AssemblyDefinition assemblyDefinition = loader.LoadAssemblyDefinition(path);
			if (assemblyDefinition.CustomAttributes.Any((CustomAttribute attribute) => attribute.AttributeType.Name == "ModInfoAttribute"))
			{
				return assemblyDefinition.Modules.SelectMany((ModuleDefinition module) => module.Types).Any((TypeDefinition type) => !type.IsAbstract && isModSystem(type));
			}
			return false;
		}
		static bool isModSystem(TypeDefinition typeDefinition)
		{
			for (TypeReference baseType = typeDefinition.BaseType; baseType != null; baseType = baseType.Resolve()?.BaseType)
			{
				if (baseType.FullName == typeof(ModSystem).FullName)
				{
					return true;
				}
			}
			return false;
		}
	}

	public void LoadAssembly(ModCompilationContext compilationContext, ModAssemblyLoader loader)
	{
		EnumModType modType = base.Info?.Type ?? EnumModType.Code;
		if (!Enabled || Assembly != null)
		{
			return;
		}
		if (modType != EnumModType.Code)
		{
			if (SourceFiles.Count > 0 || AssemblyFiles.Count > 0)
			{
				base.Logger.Warning("Is a {0} mod, but .cs or .dll files were found. These will be ignored.", modType);
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
			List<Assembly> assemblyCandidates = (from path in AssemblyFiles
				select loader.LoadFrom(path) into ass
				where ass.GetCustomAttribute<ModInfoAttribute>() != null || GetModSystems(ass).Any()
				select ass).ToList();
			if (assemblyCandidates.Count == 0)
			{
				throw new Exception(string.Format("{0} declared as code mod, but there are no .dll files that contain at least one ModSystem or has a ModInfo attribute", string.Join(", ", AssemblyFiles)));
			}
			if (assemblyCandidates.Count >= 2)
			{
				throw new Exception("Found multiple .dll files with ModSystems and/or ModInfo attributes");
			}
			Assembly = assemblyCandidates[0];
			base.Logger.VerboseDebug("Loaded assembly {0}", Assembly.Location);
		}
		catch (Exception ex)
		{
			base.Logger.Error("An exception was thrown when trying to load assembly:");
			base.Logger.Error(ex);
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
		List<ModSystem> systems = new List<ModSystem>();
		foreach (Type systemType in GetModSystems(Assembly))
		{
			try
			{
				ModSystem system = (ModSystem)Activator.CreateInstance(systemType);
				system.Mod = this;
				systems.Add(system);
			}
			catch (Exception ex)
			{
				base.Logger.Error("Exception thrown when trying to create an instance of ModSystem {0}:", systemType);
				base.Logger.Error(ex);
			}
		}
		base.Systems = systems.AsReadOnly();
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
				Exception[] es = (ex as ReflectionTypeLoadException).LoaderExceptions;
				base.Logger.Error("Exception thrown when attempting to retrieve all types of the assembly {0}. Will ignore asssembly. Loader exceptions:", assembly.FullName);
				base.Logger.Error(ex);
				if (ex.InnerException != null)
				{
					base.Logger.Error("InnerException:");
					base.Logger.Error(ex.InnerException);
				}
				for (int i = 0; i < es.Length; i++)
				{
					base.Logger.Error(es[i]);
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
		ModInfoAttribute modInfoAttr = assembly.GetCustomAttribute<ModInfoAttribute>();
		if (modInfoAttr == null)
		{
			modWorldConfig = null;
			iconPath = null;
			return null;
		}
		List<ModDependency> dependencies = (from attr in assembly.GetCustomAttributes<ModDependencyAttribute>()
			select new ModDependency(attr.ModID, attr.Version)).ToList();
		return LoadModInfoFromModInfoAttribute(modInfoAttr, dependencies, out modWorldConfig, out iconPath);
	}

	private ModInfo LoadModInfoFromAssemblyDefinition(AssemblyDefinition assemblyDefinition, out ModWorldConfiguration modWorldConfig, out string iconPath)
	{
		CustomAttribute modInfoAttribute = assemblyDefinition.CustomAttributes.SingleOrDefault((CustomAttribute attribute) => attribute.AttributeType.Name == "ModInfoAttribute");
		if (modInfoAttribute == null)
		{
			modWorldConfig = null;
			iconPath = null;
			return null;
		}
		string name = modInfoAttribute.ConstructorArguments[0].Value as string;
		string modID = modInfoAttribute.ConstructorArguments[1].Value as string;
		ModInfoAttribute modInfo = new ModInfoAttribute(name, modID);
		foreach (Mono.Cecil.CustomAttributeNamedArgument property in modInfoAttribute.Properties.Where((Mono.Cecil.CustomAttributeNamedArgument p) => p.Name != "Name" && p.Name != "ModID"))
		{
			PropertyInfo propertySetter = modInfo.GetType().GetProperty(property.Name);
			if (property.Argument.Value is CustomAttributeArgument[] array)
			{
				propertySetter.SetValue(modInfo, array.Select((CustomAttributeArgument item) => item.Value as string).ToArray());
			}
			else
			{
				propertySetter.SetValue(modInfo, property.Argument.Value);
			}
		}
		List<ModDependency> dependencies = (from attribute in assemblyDefinition.CustomAttributes
			where attribute.AttributeType.Name == "ModDependencyAttribute"
			select new ModDependency((string)attribute.ConstructorArguments[0].Value, attribute.ConstructorArguments[1].Value as string)).ToList();
		return LoadModInfoFromModInfoAttribute(modInfo, dependencies, out modWorldConfig, out iconPath);
	}

	private ModInfo LoadModInfoFromModInfoAttribute(ModInfoAttribute modInfoAttr, List<ModDependency> dependencies, out ModWorldConfiguration modWorldConfig, out string iconPath)
	{
		if (!Enum.TryParse<EnumAppSide>(modInfoAttr.Side, ignoreCase: true, out var side))
		{
			base.Logger.Warning("Cannot parse '{0}', must be either 'Client', 'Server' or 'Universal'. Defaulting to 'Universal'.", modInfoAttr.Side);
			side = EnumAppSide.Universal;
		}
		if (modInfoAttr.WorldConfig != null)
		{
			modWorldConfig = JsonConvert.DeserializeObject<ModWorldConfiguration>(modInfoAttr.WorldConfig);
		}
		else
		{
			modWorldConfig = null;
		}
		ModInfo result = new ModInfo(EnumModType.Code, modInfoAttr.Name, modInfoAttr.ModID, modInfoAttr.Version, modInfoAttr.Description, modInfoAttr.Authors, modInfoAttr.Contributors, modInfoAttr.Website, side, modInfoAttr.RequiredOnClient, modInfoAttr.RequiredOnServer, dependencies)
		{
			NetworkVersion = modInfoAttr.NetworkVersion,
			CoreMod = modInfoAttr.CoreMod
		};
		iconPath = modInfoAttr.IconPath;
		return result;
	}

	private void CheckProperVersions()
	{
		if (!string.IsNullOrEmpty(base.Info.Version) && !SemVer.TryParse(base.Info.Version, out var guess, out var error))
		{
			base.Logger.Warning("{0} (best guess: {1})", error, guess);
		}
		foreach (ModDependency dep in base.Info.Dependencies)
		{
			if (!(dep.Version == "*") && !string.IsNullOrEmpty(dep.Version) && !SemVer.TryParse(dep.Version, out guess, out error))
			{
				base.Logger.Warning("Dependency '{0}': {1} (best guess: {2})", dep.ModID, error, guess);
			}
		}
	}
}
