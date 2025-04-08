using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Vintagestory.API.Config;

namespace Vintagestory.Common.Convert;

public static class ZstdNative
{
	internal sealed class NativeTypeNameAttribute : Attribute
	{
		public string Name { get; }

		public NativeTypeNameAttribute(string name)
		{
			Name = name;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ZstdCCtx
	{
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ZstdDCtx
	{
	}

	public enum ZstdCParameter
	{
		ZSTD_c_compressionLevel = 100,
		ZSTD_c_windowLog = 101,
		ZSTD_c_hashLog = 102,
		ZSTD_c_chainLog = 103,
		ZSTD_c_searchLog = 104,
		ZSTD_c_minMatch = 105,
		ZSTD_c_targetLength = 106,
		ZSTD_c_strategy = 107,
		ZSTD_c_enableLongDistanceMatching = 160,
		ZSTD_c_ldmHashLog = 161,
		ZSTD_c_ldmMinMatch = 162,
		ZSTD_c_ldmBucketSizeLog = 163,
		ZSTD_c_ldmHashRateLog = 164,
		ZSTD_c_contentSizeFlag = 200,
		ZSTD_c_checksumFlag = 201,
		ZSTD_c_dictIDFlag = 202,
		ZSTD_c_nbWorkers = 400,
		ZSTD_c_jobSize = 401,
		ZSTD_c_overlapLog = 402
	}

	private const string DllName = "libzstd";

	public static Version Version
	{
		get
		{
			int version = (int)ZSTD_versionNumber();
			return new Version(version / 10000 % 100, version / 100 % 100, version % 100);
		}
	}

	static ZstdNative()
	{
		NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
	}

	private static nint DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		string suffix = RuntimeEnv.OS switch
		{
			OS.Windows => ".dll", 
			OS.Mac => ".dylib", 
			OS.Linux => ".so", 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		nint handle;
		if (RuntimeEnv.OS == OS.Linux)
		{
			if (NativeLibrary.TryLoad(libraryName + suffix + ".1", assembly, searchPath, out handle))
			{
				return handle;
			}
			if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out handle))
			{
				return handle;
			}
		}
		if (!NativeLibrary.TryLoad(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib", libraryName + suffix), assembly, searchPath, out handle))
		{
			return IntPtr.Zero;
		}
		return handle;
	}

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	public unsafe static extern ZstdCCtx* ZSTD_createCCtx();

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	public unsafe static extern nuint ZSTD_CCtx_setParameter(ZstdCCtx* cctx, ZstdCParameter param, int value);

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	[return: NativeTypeName("size_t")]
	public static extern nuint ZSTD_compressBound([NativeTypeName("size_t")] nuint srcSize);

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	[return: NativeTypeName("unsigned long long")]
	public unsafe static extern ulong ZSTD_getFrameContentSize([NativeTypeName("const void *")] void* src, [NativeTypeName("size_t")] nuint srcSize);

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	[return: NativeTypeName("size_t")]
	public unsafe static extern nuint ZSTD_compressCCtx(ZstdCCtx* cctx, void* dst, [NativeTypeName("size_t")] nuint dstCapacity, [NativeTypeName("const void *")] void* src, [NativeTypeName("size_t")] nuint srcSize, int compressionLevel);

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	public unsafe static extern ZstdDCtx* ZSTD_createDCtx();

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	[return: NativeTypeName("size_t")]
	public unsafe static extern nuint ZSTD_decompressDCtx(ZstdDCtx* dctx, void* dst, [NativeTypeName("size_t")] nuint dstCapacity, [NativeTypeName("const void *")] void* src, [NativeTypeName("size_t")] nuint srcSize);

	[DllImport("libzstd", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
	public static extern uint ZSTD_versionNumber();
}
