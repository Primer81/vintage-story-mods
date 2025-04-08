using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class ModCompilationContext
{
	private readonly string[] references;

	public ModCompilationContext()
	{
		references = new string[17]
		{
			"System.dll",
			"System.Core.dll",
			"System.Data.dll",
			"System.Runtime.dll",
			"System.Private.CoreLib.dll",
			"SkiaSharp.dll",
			"System.Xml.dll",
			"System.Xml.Linq.dll",
			"System.Net.Http.dll",
			"VintagestoryAPI.dll",
			"Newtonsoft.Json.dll",
			"protobuf-net.dll",
			"Tavis.JsonPatch.dll",
			"cairo-sharp.dll",
			Path.Combine("Mods", "VSCreativeMod.dll"),
			Path.Combine("Mods", "VSEssentials.dll"),
			Path.Combine("Mods", "VSSurvivalMod.dll")
		};
		string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
		if (assemblyPath == null)
		{
			throw new Exception("Could not find core/system assembly path for mod compilation.");
		}
		for (int i = 0; i < references.Length; i++)
		{
			if (File.Exists(Path.Combine(GamePaths.Binaries, references[i])))
			{
				references[i] = Path.Combine(GamePaths.Binaries, references[i]);
				continue;
			}
			if (File.Exists(Path.Combine(GamePaths.Binaries, "Lib", references[i])))
			{
				references[i] = Path.Combine(GamePaths.Binaries, "Lib", references[i]);
				continue;
			}
			if (File.Exists(Path.Combine(assemblyPath, references[i])))
			{
				references[i] = Path.Combine(assemblyPath, references[i]);
				continue;
			}
			throw new Exception("Referenced library not found: " + references[i]);
		}
	}

	public Assembly CompileFromFiles(ModContainer mod)
	{
		List<PortableExecutableReference> refsMetadata = (from sourceFile in mod.SourceFiles
			where sourceFile.EndsWithOrdinal(".dll")
			select MetadataReference.CreateFromFile(sourceFile)).ToList();
		refsMetadata.AddRange(references.Select((string dlls) => MetadataReference.CreateFromFile(dlls)));
		IEnumerable<SyntaxTree> syntaxTrees = mod.SourceFiles.Select((string file) => CSharpSyntaxTree.ParseText(File.ReadAllText(file)));
		CSharpCompilation compilation = CSharpCompilation.Create(mod.FileName + Guid.NewGuid(), syntaxTrees, refsMetadata, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream memoryStream = new MemoryStream();
		EmitResult result = compilation.Emit(memoryStream);
		if (!result.Success)
		{
			foreach (Diagnostic error in result.Diagnostics.Where((Diagnostic d) => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error))
			{
				mod.Logger.Error("{0}: {1}", error.Id, error.GetMessage());
			}
			return null;
		}
		memoryStream.Seek(0L, SeekOrigin.Begin);
		mod.Logger.Debug("Successfully compiled mod with Roslyn");
		return Assembly.Load(memoryStream.ToArray());
	}
}
