using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class ObjTesselator
{
	private List<ObjFaceVertex> faceVertices = new List<ObjFaceVertex>();

	private List<float> temp_vertices = new List<float>();

	private List<float> temp_uvs = new List<float>();

	private List<float> temp_normals = new List<float>();

	private int facesCount;

	public void Load(IAsset asset, out MeshData mesh, TextureAtlasPosition pos, TesselationMetaData meta, short renderPass)
	{
		mesh = new MeshData(24, 36).WithColorMaps().WithRenderpasses();
		if (meta.WithJointIds)
		{
			mesh.CustomInts = new CustomMeshDataPartInt();
			mesh.CustomInts.InterleaveSizes = new int[1] { 1 };
			mesh.CustomInts.InterleaveOffsets = new int[1];
			mesh.CustomInts.InterleaveStride = 0;
		}
		else
		{
			mesh.CustomInts = null;
		}
		if (meta.WithDamageEffect)
		{
			mesh.CustomFloats = new CustomMeshDataPartFloat();
			mesh.CustomFloats.InterleaveSizes = new int[1] { 1 };
			mesh.CustomFloats.InterleaveOffsets = new int[1];
			mesh.CustomFloats.InterleaveStride = 0;
		}
		faceVertices.Clear();
		temp_vertices.Clear();
		temp_uvs.Clear();
		temp_normals.Clear();
		facesCount = 0;
		using (MemoryStream ms = new MemoryStream(asset.Data))
		{
			using StreamReader reader = new StreamReader(ms);
			while (!reader.EndOfStream)
			{
				parseLine(reader);
			}
		}
		mesh = new MeshData(faceVertices.Count, faceVertices.Count);
		mesh.WithXyzFaces();
		mesh.WithColorMaps();
		mesh.IndicesPerFace = 3;
		mesh.VerticesPerFace = 3;
		float uwdt = pos.x2 - pos.x1;
		float uhgt = pos.y2 - pos.y1;
		for (int i = 0; i < facesCount; i++)
		{
			mesh.AddXyzFace(0);
			mesh.AddTextureId(pos.atlasTextureId);
		}
		mesh.xyz = temp_vertices.ToArray();
		mesh.Rgba.Fill(byte.MaxValue);
		for (int vertIndex = 0; vertIndex < faceVertices.Count; vertIndex++)
		{
			ObjFaceVertex face = faceVertices[vertIndex];
			float normalx = temp_normals[3 * face.NormalIndex];
			float normaly = temp_normals[3 * face.NormalIndex + 1];
			float normalz = temp_normals[3 * face.NormalIndex + 2];
			mesh.Flags[face.VertexIndex] = VertexFlags.PackNormal(normalx, normaly, normalz) + meta.GeneralGlowLevel;
			mesh.Uv[2 * face.VertexIndex] = pos.x1 + temp_uvs[2 * face.UvIndex] * uwdt;
			mesh.Uv[2 * face.VertexIndex + 1] = pos.y1 + temp_uvs[2 * face.UvIndex + 1] * uhgt;
			mesh.AddIndex(face.VertexIndex);
			if (mesh.ClimateColorMapIds != null)
			{
				mesh.AddColorMapIndex(meta.ClimateColorMapId, meta.SeasonColorMapId);
			}
			if (mesh.RenderPassesAndExtraBits != null)
			{
				mesh.AddRenderPass(renderPass);
			}
		}
		mesh.VerticesCount = faceVertices.Count;
	}

	private void parseLine(StreamReader reader)
	{
		string currentLine = reader.ReadLine();
		if (!string.IsNullOrEmpty(currentLine) && currentLine[0] != '#')
		{
			string[] array = currentLine.Split(new char[1] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
			string keyword = array[0];
			string[] parts = array[1].Split(' ');
			switch (keyword)
			{
			case "f":
				parseFace(parts);
				break;
			case "vn":
				parseNormal(parts);
				break;
			case "vt":
				parseTextureUV(parts);
				break;
			case "v":
				parseVertex(parts);
				break;
			}
		}
	}

	private void parseFace(string[] parts)
	{
		if (parts.Length != 3)
		{
			throw new FormatException("Cannot read .obj file. The f section needs to contain 9 values");
		}
		facesCount++;
		for (int i = 0; i < 3; i++)
		{
			string[] fields = parts[i].Split(new char[1] { '/' }, StringSplitOptions.None);
			faceVertices.Add(new ObjFaceVertex
			{
				VertexIndex = fields[0].ToInt() - 1,
				UvIndex = fields[1].ToInt() - 1,
				NormalIndex = fields[2].ToInt() - 1
			});
		}
	}

	private void parseNormal(string[] parts)
	{
		temp_normals.Add(parts[0].ToFloat());
		temp_normals.Add(parts[1].ToFloat());
		temp_normals.Add(parts[2].ToFloat());
	}

	private void parseTextureUV(string[] parts)
	{
		temp_uvs.Add(parts[0].ToFloat());
		temp_uvs.Add(parts[1].ToFloat());
	}

	private void parseVertex(string[] parts)
	{
		temp_vertices.Add(parts[0].ToFloat() + 0.5f);
		temp_vertices.Add(parts[1].ToFloat() + 0.5f);
		temp_vertices.Add(parts[2].ToFloat() + 0.5f);
	}
}
