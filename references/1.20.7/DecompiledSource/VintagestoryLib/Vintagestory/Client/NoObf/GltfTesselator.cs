using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GltfTesselator
{
	private List<Vec3f> temp_vertices = new List<Vec3f>();

	private List<float> temp_uvs = new List<float>();

	private List<int> temp_normals = new List<int>();

	private List<int> temp_indices = new List<int>();

	private List<Vec3f> temp_material = new List<Vec3f>();

	private List<Vec4us> temp_vertexcolor = new List<Vec4us>();

	private List<int> temp_flags = new List<int>();

	private List<MeshData> meshPieces = new List<MeshData>();

	private int[] capacities;

	private VertexFlags tempFlag = new VertexFlags();

	public void Load(GltfType asset, out MeshData mesh, TextureAtlasPosition pos, int generalGlowLevel, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, out byte[][][] bakedTextures)
	{
		meshPieces.Clear();
		capacities = new int[2];
		ParseGltf(asset, pos, generalGlowLevel, climateColorMapIndex, seasonColorMapIndex, renderPass, out bakedTextures);
		mesh = new MeshData(capacities[0] + 32, capacities[1] + 32).WithXyzFaces().WithRenderpasses().WithColorMaps();
		mesh.IndicesPerFace = 3;
		mesh.VerticesPerFace = 3;
		mesh.CustomFloats = new CustomMeshDataPartFloat
		{
			Values = new float[meshPieces.Count * 5],
			InterleaveSizes = new int[2] { 3, 2 },
			InterleaveStride = 20,
			InterleaveOffsets = new int[2] { 0, 12 },
			Count = 5
		};
		mesh.CustomInts = new CustomMeshDataPartInt
		{
			Values = new int[meshPieces.Count],
			InterleaveSizes = new int[meshPieces.Count].Fill(1),
			InterleaveStride = 4,
			InterleaveOffsets = new int[meshPieces.Count],
			Count = 0
		};
		for (int j = 0; j < mesh.CustomInts.Values.Length; j++)
		{
			mesh.CustomInts.InterleaveOffsets[j] = 4 * j;
		}
		for (int i = 0; i < meshPieces.Count; i++)
		{
			MeshData piece = meshPieces[i];
			mesh.CustomFloats.Values[i * 5] = piece.CustomFloats.Values[0];
			mesh.CustomFloats.Values[i * 5 + 1] = piece.CustomFloats.Values[1];
			mesh.CustomFloats.Values[i * 5 + 2] = piece.CustomFloats.Values[2];
			mesh.CustomFloats.Values[i * 5 + 3] = piece.CustomFloats.Values[3];
			mesh.CustomFloats.Values[i * 5 + 4] = piece.CustomFloats.Values[4];
			mesh.AddMeshData(piece);
		}
	}

	public void ParseGltf(GltfType gltf, TextureAtlasPosition pos, int generalGlowLevel, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, out byte[][][] bakedTextures)
	{
		GltfBuffer[] buffers = gltf.Buffers;
		GltfBufferView[] bufferViews = gltf.BufferViews;
		int? materials = gltf.Materials?.Length;
		bakedTextures = (materials.HasValue ? new byte[materials.Value][][] : null);
		long matIndex = 0L;
		long[] nodes = gltf.Scenes[gltf.Scene].Nodes;
		foreach (long node in nodes)
		{
			GltfNode gltfNode = gltf.Nodes[node];
			GltfPrimitive[] primitives = gltf.Meshes[gltf.Nodes[node].Mesh].Primitives;
			foreach (GltfPrimitive primitive in primitives)
			{
				Dictionary<string, long> accvalues = new Dictionary<string, long>();
				Dictionary<string, byte[]> buffdat = new Dictionary<string, byte[]>();
				float[] colorFactor = new float[3] { 1f, 1f, 1f };
				float[] pbrFactor = new float[2] { 0f, 1f };
				long? vtIndex = primitive.Attributes.Position;
				long? uvIndex = primitive.Attributes.Texcoord0;
				long? nmIndex = primitive.Attributes.Normal;
				long? vcIndex = primitive.Attributes.VertexColor;
				long? vgIndex = primitive.Attributes.GlowLevel;
				long? vrIndex = primitive.Attributes.Reflective;
				long? bmwlIndex = primitive.Attributes.BMWindLeaves;
				long? bmwlwbIndex = primitive.Attributes.BMWindLeavesWeakBend;
				long? bmwnIndex = primitive.Attributes.BMWindNormal;
				long? bmwwIndex = primitive.Attributes.BMWindWater;
				long? bmwwbIndex = primitive.Attributes.BMWindWeakBend;
				long? bmwwwIndex = primitive.Attributes.BMWindWeakWind;
				long? idIndex = primitive.Indices;
				long? mtIndex = primitive.Material;
				if (vtIndex.HasValue)
				{
					accvalues.Add("vtx", vtIndex.Value);
				}
				if (uvIndex.HasValue)
				{
					accvalues.Add("uvs", uvIndex.Value);
				}
				if (vcIndex.HasValue)
				{
					accvalues.Add("vtc", vcIndex.Value);
				}
				if (vgIndex.HasValue)
				{
					accvalues.Add("vtg", vgIndex.Value);
				}
				if (vrIndex.HasValue)
				{
					accvalues.Add("vtr", vrIndex.Value);
				}
				if (bmwlIndex.HasValue)
				{
					accvalues.Add("wa", bmwlIndex.Value);
				}
				if (bmwlwbIndex.HasValue)
				{
					accvalues.Add("wb", bmwlwbIndex.Value);
				}
				if (bmwnIndex.HasValue)
				{
					accvalues.Add("wc", bmwnIndex.Value);
				}
				if (bmwwIndex.HasValue)
				{
					accvalues.Add("wd", bmwwIndex.Value);
				}
				if (bmwwbIndex.HasValue)
				{
					accvalues.Add("we", bmwwbIndex.Value);
				}
				if (bmwwwIndex.HasValue)
				{
					accvalues.Add("wf", bmwwwIndex.Value);
				}
				if (nmIndex.HasValue)
				{
					accvalues.Add("nrm", nmIndex.Value);
				}
				if (idIndex.HasValue)
				{
					accvalues.Add("ind", idIndex.Value);
				}
				if (mtIndex.HasValue)
				{
					accvalues.Add("mat", mtIndex.Value);
				}
				GltfMaterial[] materials2 = gltf.Materials;
				GltfMaterial mat = ((materials2 != null) ? materials2[mtIndex.Value] : null);
				if (mat != null)
				{
					new Dictionary<string, long>();
					if (mat?.PbrMetallicRoughness != null)
					{
						GltfPbrMetallicRoughness pbr = mat.PbrMetallicRoughness;
						colorFactor = mat.PbrMetallicRoughness.BaseColorFactor ?? colorFactor;
						pbrFactor = mat.PbrMetallicRoughness.PbrFactor ?? pbrFactor;
						GltfMatTexture baseColorTexture = pbr.BaseColorTexture;
						if (baseColorTexture != null)
						{
							_ = baseColorTexture.Index;
							if (true)
							{
								accvalues.Add("bcr", gltf.Images[pbr.BaseColorTexture.Index].BufferView);
							}
						}
						GltfMatTexture metallicRoughnessTexture = pbr.MetallicRoughnessTexture;
						if (metallicRoughnessTexture != null)
						{
							_ = metallicRoughnessTexture.Index;
							if (true)
							{
								accvalues.Add("pbr", gltf.Images[pbr.MetallicRoughnessTexture.Index].BufferView);
							}
						}
					}
					if (mat != null)
					{
						GltfMatTexture normalTexture = mat.NormalTexture;
						if (normalTexture != null)
						{
							_ = normalTexture.Index;
							if (true)
							{
								accvalues.Add("ntx", gltf.Images[mat.NormalTexture.Index].BufferView);
							}
						}
					}
				}
				foreach (KeyValuePair<string, long> dict in accvalues)
				{
					GltfBufferView bufferview = bufferViews[dict.Value];
					GltfBuffer obj = buffers[bufferview.Buffer];
					if (!buffdat.TryGetValue(dict.Key, out var bytes))
					{
						buffdat.Add(dict.Key, new byte[bufferview.ByteLength]);
					}
					bytes = buffdat[dict.Key];
					byte[] bufferdat = Convert.FromBase64String(obj.Uri.Replace("data:application/octet-stream;base64,", "")).Copy(bufferview.ByteOffset, bufferview.ByteLength);
					for (int j = 0; j < bufferdat.Length; j++)
					{
						bytes[j] = bufferdat[j];
					}
				}
				if (buffdat.TryGetValue("vtx", out var vtBytes))
				{
					temp_vertices.AddRange(vtBytes.ToVec3fs());
				}
				if (buffdat.TryGetValue("uvs", out var uvBytes))
				{
					temp_uvs.AddRange(uvBytes.ToFloats());
				}
				if (buffdat.TryGetValue("nrm", out var nmBytes))
				{
					Vec3f[] array = nmBytes.ToVec3fs();
					foreach (Vec3f val in array)
					{
						temp_normals.Add(VertexFlags.PackNormal(val) + generalGlowLevel);
					}
				}
				if (buffdat.TryGetValue("ind", out var idBytes))
				{
					temp_indices.AddRange(idBytes.ToUShorts().ToInts());
				}
				if (buffdat.TryGetValue("mat", out var mtBytes))
				{
					temp_material.AddRange(mtBytes.ToVec3fs());
				}
				if (buffdat.TryGetValue("vtc", out var vcBytes))
				{
					temp_vertexcolor.AddRange(vcBytes.ToVec4uss());
				}
				buffdat.TryGetValue("vtg", out var datBytes);
				ulong[] vgLongs = datBytes?.BytesToULongs();
				buffdat.TryGetValue("vtr", out datBytes);
				ulong[] vrLongs = datBytes?.BytesToULongs();
				buffdat.TryGetValue("wa", out datBytes);
				ulong[] waLongs = datBytes?.BytesToULongs();
				buffdat.TryGetValue("wb", out datBytes);
				ulong[] wbLongs = datBytes?.BytesToULongs();
				buffdat.TryGetValue("wc", out datBytes);
				ulong[] wcLongs = datBytes?.BytesToULongs();
				buffdat.TryGetValue("wd", out datBytes);
				ulong[] wdLongs = datBytes?.BytesToULongs();
				buffdat.TryGetValue("we", out datBytes);
				ulong[] weLongs = datBytes?.BytesToULongs();
				buffdat.TryGetValue("wf", out datBytes);
				ulong[] wfLongs = datBytes?.BytesToULongs();
				for (int i = 0; i < temp_vertices.Count; i++)
				{
					tempFlag.All = 0;
					tempFlag.GlowLevel = (byte)((vgLongs != null && vgLongs[i] != 0) ? ((byte)((double)(vgLongs[i] >> 16) / 281474976710655.0 * 255.0)) : 0);
					tempFlag.Reflective = ((vrLongs != null) ? vrLongs[i] : 0) >> 16 != 0;
					tempFlag.WindMode = ((((waLongs != null) ? waLongs[i] : 0) >> 16 != 0) ? EnumWindBitMode.Leaves : ((((wbLongs != null) ? wbLongs[i] : 0) >> 16 != 0) ? EnumWindBitMode.TallBend : ((((wcLongs != null) ? wcLongs[i] : 0) >> 16 != 0) ? EnumWindBitMode.NormalWind : ((((wdLongs != null) ? wdLongs[i] : 0) >> 16 != 0) ? EnumWindBitMode.Water : ((((weLongs != null) ? weLongs[i] : 0) >> 16 != 0) ? EnumWindBitMode.Bend : ((((wfLongs != null) ? wfLongs[i] : 0) >> 16 != 0) ? EnumWindBitMode.WeakWind : EnumWindBitMode.NoWind))))));
					temp_flags.Add(tempFlag.All);
				}
				if (bakedTextures != null)
				{
					byte[] clrtex = (buffdat.ContainsKey("bcr") ? buffdat["bcr"] : null);
					byte[] pbrtex = (buffdat.ContainsKey("pbr") ? buffdat["pbr"] : null);
					byte[] nrmtex = (buffdat.ContainsKey("pbr") ? buffdat["pbr"] : null);
					bakedTextures[matIndex] = new byte[3][] { clrtex, pbrtex, nrmtex };
				}
				matIndex++;
				BuildMeshDataPart(gltfNode, pos, climateColorMapIndex, seasonColorMapIndex, renderPass, colorFactor, pbrFactor);
			}
		}
	}

	public void BuildMeshDataPart(GltfNode node, TextureAtlasPosition pos, byte climateColorMapIndex, byte seasonColorMapIndex, short renderPass, float[] colorFactor, float[] pbrFactor)
	{
		MeshData meshPiece = new MeshData(temp_vertices.Count, temp_vertices.Count);
		meshPiece.WithXyzFaces();
		meshPiece.WithRenderpasses();
		meshPiece.WithColorMaps();
		meshPiece.IndicesPerFace = 3;
		meshPiece.VerticesPerFace = 3;
		meshPiece.Rgba.Fill(byte.MaxValue);
		capacities[0] += temp_vertices.Count * 3;
		meshPiece.Flags = new int[temp_vertices.Count];
		for (int j = 0; j < temp_vertices.Count; j++)
		{
			meshPiece.Flags[j] = temp_flags[j];
			meshPiece.Flags[j] |= temp_normals[j];
			if (temp_vertexcolor.Count > 0)
			{
				Vec4us col = temp_vertexcolor[j];
				int intCol = ((byte)((float)(int)col.W / 65535f * 255f) << 24) | ((byte)((float)(int)col.X / 65535f * 255f) << 16) | ((byte)((float)(int)col.Y / 65535f * 255f) << 8) | (byte)((float)(int)col.Z / 65535f * 255f);
				meshPiece.AddVertexSkipTex(temp_vertices[j].X + 0.5f, temp_vertices[j].Y + 0.5f, temp_vertices[j].Z + 0.5f, intCol);
			}
			else
			{
				meshPiece.AddVertexSkipTex(temp_vertices[j].X + 0.5f, temp_vertices[j].Y + 0.5f, temp_vertices[j].Z + 0.5f);
			}
		}
		for (int i = 0; i < temp_indices.Count / 3; i++)
		{
			meshPiece.AddXyzFace(0);
			meshPiece.AddTextureId(pos.atlasTextureId);
			if (meshPiece.ClimateColorMapIds != null)
			{
				meshPiece.AddColorMapIndex(climateColorMapIndex, seasonColorMapIndex);
			}
			if (meshPiece.RenderPassesAndExtraBits != null)
			{
				meshPiece.AddRenderPass(renderPass);
			}
		}
		meshPiece.Uv = temp_uvs.ToArray();
		meshPiece.AddIndices(temp_indices.ToArray());
		capacities[1] += temp_indices.Count;
		meshPiece.VerticesCount = temp_vertices.Count;
		if (pos != null)
		{
			meshPiece.SetTexPos(pos);
		}
		meshPiece.XyzFacesCount = temp_indices.Count / 3;
		Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
		if (node.Rotation != null)
		{
			Vec3f rot = GameMath.ToEulerAngles(new Vec4f((float)node.Rotation[0], (float)node.Rotation[1], (float)node.Rotation[2], (float)node.Rotation[3]));
			meshPiece.Rotate(origin, rot.X, rot.Y, rot.Z);
		}
		if (node.Scale != null)
		{
			meshPiece.Scale(origin, (float)node.Scale[0], (float)node.Scale[1], (float)node.Scale[2]);
		}
		if (node.Translation != null)
		{
			meshPiece.Translate((float)node.Translation[0], (float)node.Translation[1], (float)node.Translation[2]);
		}
		meshPiece.CustomFloats = new CustomMeshDataPartFloat
		{
			Values = new float[5]
			{
				colorFactor[0],
				colorFactor[1],
				colorFactor[2],
				pbrFactor[0],
				pbrFactor[1]
			},
			InterleaveSizes = new int[2] { 3, 2 },
			InterleaveStride = 20,
			InterleaveOffsets = new int[2] { 0, 12 },
			Count = 5
		};
		meshPiece.CustomInts = new CustomMeshDataPartInt
		{
			Values = new int[1] { meshPieces.Count * meshPiece.XyzCount },
			InterleaveSizes = new int[1] { 1 },
			InterleaveStride = 4,
			InterleaveOffsets = new int[1],
			Count = 1
		};
		meshPieces.Add(meshPiece);
		temp_vertices.Clear();
		temp_uvs.Clear();
		temp_normals.Clear();
		temp_indices.Clear();
		temp_material.Clear();
		temp_vertexcolor.Clear();
		temp_flags.Clear();
	}
}
