#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public abstract class ShaderProgramBase : IShaderProgram, IDisposable
{
    public static int shadowmapQuality;

    public static ShaderProgramBase CurrentShaderProgram;

    public int PassId;

    public int ProgramId;

    public string PassName;

    public Shader VertexShader;

    public Shader GeometryShader;

    public Shader FragmentShader;

    public Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

    public Dictionary<string, int> textureLocations = new Dictionary<string, int>();

    public OrderedDictionary<string, UBORef> ubos = new OrderedDictionary<string, UBORef>();

    public bool clampTToEdge;

    public HashSet<string> includes = new HashSet<string>();

    public Dictionary<string, int> customSamplers = new Dictionary<string, int>();

    private bool disposed;

    public bool Disposed => disposed;

    int IShaderProgram.PassId => PassId;

    string IShaderProgram.PassName => PassName;

    public bool ClampTexturesToEdge
    {
        get
        {
            return clampTToEdge;
        }
        set
        {
            clampTToEdge = value;
        }
    }

    IShader IShaderProgram.VertexShader
    {
        get
        {
            return VertexShader;
        }
        set
        {
            VertexShader = (Shader)value;
        }
    }

    IShader IShaderProgram.FragmentShader
    {
        get
        {
            return FragmentShader;
        }
        set
        {
            FragmentShader = (Shader)value;
        }
    }

    IShader IShaderProgram.GeometryShader
    {
        get
        {
            return GeometryShader;
        }
        set
        {
            GeometryShader = (Shader)value;
        }
    }

    public bool LoadError { get; set; }

    public OrderedDictionary<string, UBORef> UBOs => ubos;

    public string AssetDomain { get; set; }

    public void SetCustomSampler(string uniformName, bool isLinear)
    {
        int value = ScreenManager.Platform.GenSampler(isLinear);
        customSamplers.Add(uniformName, value);
    }

    public void Uniform(string uniformName, float value)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform1(uniformLocations[uniformName], value);
    }

    public void Uniform(string uniformName, int count, float[] value)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform1(uniformLocations[uniformName], count, value);
    }

    public void Uniform(string uniformName, int value)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform1(uniformLocations[uniformName], value);
    }

    public void Uniform(string uniformName, Vec2f value)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform2(uniformLocations[uniformName], value.X, value.Y);
    }

    public void Uniform(string uniformName, Vec3f value)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform3(uniformLocations[uniformName], value.X, value.Y, value.Z);
    }

    public void Uniform(string uniformName, Vec3i value)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform3(uniformLocations[uniformName], value.X, value.Y, value.Z);
    }

    public void Uniforms2(string uniformName, int count, float[] values)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform2(uniformLocations[uniformName], count, values);
    }

    public void Uniforms3(string uniformName, int count, float[] values)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform3(uniformLocations[uniformName], count, values);
    }

    public void Uniform(string uniformName, Vec4f value)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform4(uniformLocations[uniformName], value.X, value.Y, value.Z, value.W);
    }

    public void Uniforms4(string uniformName, int count, float[] values)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.Uniform4(uniformLocations[uniformName], count, values);
    }

    public void UniformMatrix(string uniformName, float[] matrix)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.UniformMatrix4(uniformLocations[uniformName], 1, transpose: false, matrix);
    }

    public void UniformMatrix(string uniformName, ref Matrix4 matrix)
    {
        if (CurrentShaderProgram?.ProgramId != ProgramId)
        {
            throw new InvalidOperationException("Can't set uniform on not active shader " + PassName + "!");
        }

        GL.UniformMatrix4(uniformLocations[uniformName], transpose: false, ref matrix);
    }

    public bool HasUniform(string uniformName)
    {
        return uniformLocations.ContainsKey(uniformName);
    }

    public void BindTexture2D(string samplerName, int textureId, int textureNumber)
    {
        GL.Uniform1(uniformLocations[samplerName], textureNumber);
        GL.ActiveTexture((TextureUnit)(33984 + textureNumber));
        GL.BindTexture(TextureTarget.Texture2D, textureId);
        if (customSamplers.TryGetValue(samplerName, out var value))
        {
            GL.BindSampler(textureNumber, value);
        }

        if (clampTToEdge)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
        }
    }

    public void BindTexture2D(string samplerName, int textureId)
    {
        BindTexture2D(samplerName, textureId, textureLocations[samplerName]);
    }

    public void BindTextureCube(string samplerName, int textureId, int textureNumber)
    {
        GL.Uniform1(uniformLocations[samplerName], textureNumber);
        GL.ActiveTexture((TextureUnit)(33984 + textureNumber));
        GL.BindTexture(TextureTarget.TextureCubeMap, textureId);
        if (clampTToEdge)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
        }
    }

    public void UniformMatrices4x3(string uniformName, int count, float[] matrix)
    {
        GL.UniformMatrix4x3(uniformLocations[uniformName], count, transpose: false, matrix);
    }

    public void UniformMatrices(string uniformName, int count, float[] matrix)
    {
        GL.UniformMatrix4(uniformLocations[uniformName], count, transpose: false, matrix);
    }

    public void Use()
    {
        if (CurrentShaderProgram != null && CurrentShaderProgram != this)
        {
            throw new InvalidOperationException("Already a different shader (" + CurrentShaderProgram.PassName + ") in use!");
        }

        if (disposed)
        {
            throw new InvalidOperationException("Can't use a disposed shader!");
        }

        GL.UseProgram(ProgramId);
        CurrentShaderProgram = this;
        DefaultShaderUniforms shaderUniforms = ScreenManager.Platform.ShaderUniforms;
        if (includes.Contains("fogandlight.fsh"))
        {
            Uniform("zNear", shaderUniforms.ZNear);
            Uniform("zFar", shaderUniforms.ZFar);
            Uniform("lightPosition", shaderUniforms.LightPosition3D);
            Uniform("shadowIntensity", shaderUniforms.DropShadowIntensity);
            Uniform("glitchStrength", shaderUniforms.GlitchStrength);
            if (shadowmapQuality > 0)
            {
                FrameBufferRef frameBufferRef = ScreenManager.Platform.FrameBuffers[11];
                FrameBufferRef frameBufferRef2 = ScreenManager.Platform.FrameBuffers[12];
                BindTexture2D("shadowMapFar", frameBufferRef.DepthTextureId);
                BindTexture2D("shadowMapNear", frameBufferRef2.DepthTextureId);
                Uniform("shadowMapWidthInv", 1f / (float)frameBufferRef.Width);
                Uniform("shadowMapHeightInv", 1f / (float)frameBufferRef.Height);
                Uniform("viewDistance", (float)ClientSettings.ViewDistance);
                Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
            }
        }

        if (includes.Contains("fogandlight.vsh"))
        {
            int fogSphereQuantity = shaderUniforms.FogSphereQuantity;
            Uniform("fogSphereQuantity", fogSphereQuantity);
            Uniform("fogSpheres", fogSphereQuantity * 8, shaderUniforms.FogSpheres);
            int pointLightsCount = shaderUniforms.PointLightsCount;
            Uniform("pointLightQuantity", pointLightsCount);
            Uniforms3("pointLights", pointLightsCount, shaderUniforms.PointLights3);
            Uniforms3("pointLightColors", pointLightsCount, shaderUniforms.PointLightColors3);
            Uniform("flatFogDensity", shaderUniforms.FlagFogDensity);
            Uniform("flatFogStart", shaderUniforms.FlatFogStartYPos - shaderUniforms.PlayerPos.Y);
            Uniform("glitchStrengthFL", shaderUniforms.GlitchStrength);
            Uniform("viewDistance", (float)ClientSettings.ViewDistance);
            Uniform("viewDistanceLod0", (float)Math.Min(640, ClientSettings.ViewDistance) * ClientSettings.LodBias);
            Uniform("nightVisionStrength", shaderUniforms.NightVisionStrength);
        }

        if (includes.Contains("shadowcoords.vsh"))
        {
            Uniform("shadowRangeNear", shaderUniforms.ShadowRangeNear);
            Uniform("shadowRangeFar", shaderUniforms.ShadowRangeFar);
            UniformMatrix("toShadowMapSpaceMatrixNear", shaderUniforms.ToShadowMapSpaceMatrixNear);
            UniformMatrix("toShadowMapSpaceMatrixFar", shaderUniforms.ToShadowMapSpaceMatrixFar);
        }

        if (includes.Contains("vertexwarp.vsh"))
        {
            Uniform("timeCounter", shaderUniforms.TimeCounter);
            Uniform("windWaveCounter", shaderUniforms.WindWaveCounter);
            Uniform("windWaveCounterHighFreq", shaderUniforms.WindWaveCounterHighFreq);
            Uniform("windSpeed", shaderUniforms.WindSpeed);
            Uniform("waterWaveCounter", shaderUniforms.WaterWaveCounter);
            Uniform("playerpos", shaderUniforms.PlayerPos);
            Uniform("globalWarpIntensity", shaderUniforms.GlobalWorldWarp);
            Uniform("glitchWaviness", shaderUniforms.GlitchWaviness);
            Uniform("windWaveIntensity", shaderUniforms.WindWaveIntensity);
            Uniform("waterWaveIntensity", shaderUniforms.WaterWaveIntensity);
            Uniform("perceptionEffectId", shaderUniforms.PerceptionEffectId);
            Uniform("perceptionEffectIntensity", shaderUniforms.PerceptionEffectIntensity);
        }

        if (includes.Contains("skycolor.fsh"))
        {
            Uniform("fogWaveCounter", shaderUniforms.FogWaveCounter);
            BindTexture2D("sky", shaderUniforms.SkyTextureId);
            BindTexture2D("glow", shaderUniforms.GlowTextureId);
            Uniform("sunsetMod", shaderUniforms.SunsetMod);
            Uniform("ditherSeed", shaderUniforms.DitherSeed);
            Uniform("horizontalResolution", shaderUniforms.FrameWidth);
            Uniform("playerToSealevelOffset", shaderUniforms.PlayerToSealevelOffset);
        }

        if (includes.Contains("colormap.vsh"))
        {
            Uniforms4("colorMapRects", 40, shaderUniforms.ColorMapRects4);
            Uniform("seasonRel", shaderUniforms.SeasonRel);
            Uniform("seaLevel", shaderUniforms.SeaLevel);
            Uniform("atlasHeight", shaderUniforms.BlockAtlasHeight);
            Uniform("seasonTemperature", shaderUniforms.SeasonTemperature);
        }

        if (includes.Contains("underwatereffects.fsh"))
        {
            FrameBufferRef frameBufferRef3 = ScreenManager.Platform.FrameBuffers[5];
            BindTexture2D("liquidDepth", frameBufferRef3.DepthTextureId);
            Uniform("cameraUnderwater", shaderUniforms.CameraUnderwater);
            Uniform("waterMurkColor", shaderUniforms.WaterMurkColor);
            FrameBufferRef frameBufferRef4 = ScreenManager.Platform.FrameBuffers[0];
            Uniform("frameSize", new Vec2f(frameBufferRef4.Width, frameBufferRef4.Height));
        }

        if (this == ShaderPrograms.Gui)
        {
            ShaderPrograms.Gui.LightPosition = new Vec3f(1f, -1f, 0f).Normalize();
        }

        foreach (KeyValuePair<string, UBORef> ubo in ubos)
        {
            ubo.Value.Bind();
        }
    }

    public void Stop()
    {
        GL.UseProgram(0);
        for (int i = 0; i < customSamplers.Count; i++)
        {
            GL.BindSampler(i, 0);
        }

        foreach (KeyValuePair<string, UBORef> ubo in ubos)
        {
            ubo.Value.Unbind();
        }

        CurrentShaderProgram = null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (VertexShader != null)
        {
            GL.DetachShader(ProgramId, VertexShader.ShaderId);
            GL.DeleteShader(VertexShader.ShaderId);
        }

        if (FragmentShader != null)
        {
            GL.DetachShader(ProgramId, FragmentShader.ShaderId);
            GL.DeleteShader(FragmentShader.ShaderId);
        }

        if (GeometryShader != null)
        {
            GL.DetachShader(ProgramId, GeometryShader.ShaderId);
            GL.DeleteShader(GeometryShader.ShaderId);
        }

        foreach (KeyValuePair<string, int> customSampler in customSamplers)
        {
            GL.DeleteSampler(customSampler.Value);
        }

        GL.DeleteProgram(ProgramId);
    }

    public abstract bool Compile();
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
