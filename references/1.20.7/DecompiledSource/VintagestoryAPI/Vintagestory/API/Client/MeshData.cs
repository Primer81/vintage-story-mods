using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

/// <summary>
/// A data structure that can be used to upload mesh information onto the graphics card
/// Please note, all arrays are used as a buffer. They do not tightly fit the data but are always sized as a multiple of 2 from the initial size.
/// </summary>
public class MeshData
{
	public delegate bool MeshDataFilterDelegate(int faceIndex);

	public static MeshDataRecycler Recycler;

	public const int StandardVerticesPerFace = 4;

	public const int StandardIndicesPerFace = 6;

	public const int BaseSizeInBytes = 34;

	public int[] TextureIds = new int[0];

	/// <summary>
	/// The x/y/z coordinates buffer. This should hold VerticesCount*3 values.
	/// </summary>
	public float[] xyz;

	/// <summary>
	/// The render flags buffer. This should hold VerticesCount*1 values.
	/// </summary>
	public int[] Flags;

	/// <summary>
	/// True if the flags array contains any wind mode
	/// </summary>
	public bool HasAnyWindModeSet;

	/// <summary>
	/// The normals buffer. This should hold VerticesCount*1 values. Currently unused by the engine.
	/// GL_INT_2_10_10_10_REV Format
	/// x: bits 0-9    (10 bit signed int)
	/// y: bits 10-19  (10 bit signed int)
	/// z: bits 20-29  (10 bit signed int) 
	/// w: bits 30-31
	/// </summary>
	public int[] Normals;

	/// <summary>
	/// The uv buffer for texture coordinates. This should hold VerticesCount*2 values.
	/// </summary>
	public float[] Uv;

	/// <summary>
	/// The vertex color buffer. This should hold VerticesCount*4 values.
	/// </summary>
	public byte[] Rgba;

	/// <summary>
	/// The indices buffer. This should hold IndicesCount values.
	/// </summary>
	public int[] Indices;

	/// <summary>
	/// Texture index per face, references to and index in the TextureIds array
	/// </summary>
	public byte[] TextureIndices;

	/// <summary>
	/// Custom floats buffer. Can be used to upload arbitrary amounts of float values onto the graphics card
	/// </summary>
	public CustomMeshDataPartFloat CustomFloats;

	/// <summary>
	/// Custom ints buffer. Can be used to upload arbitrary amounts of int values onto the graphics card
	/// </summary>
	public CustomMeshDataPartInt CustomInts;

	/// <summary>
	/// Custom shorts buffer. Can be used to upload arbitrary amounts of short values onto the graphics card
	/// </summary>
	public CustomMeshDataPartShort CustomShorts;

	/// <summary>
	/// Custom bytes buffer. Can be used to upload arbitrary amounts of byte values onto the graphics card
	/// </summary>
	public CustomMeshDataPartByte CustomBytes;

	/// <summary>
	/// When using instanced rendering, set this flag to have the xyz values instanced.
	/// </summary>
	public bool XyzInstanced;

	/// <summary>
	/// When using instanced rendering, set this flag to have the uv values instanced.
	/// </summary>
	public bool UvInstanced;

	/// <summary>
	/// When using instanced rendering, set this flag to have the rgba values instanced.
	/// </summary>
	public bool RgbaInstanced;

	/// <summary>
	/// When using instanced rendering, set this flag to have the rgba2 values instanced.
	/// </summary>
	public bool Rgba2Instanced;

	/// <summary>
	/// When using instanced rendering, set this flag to have the indices instanced.
	/// </summary>
	public bool IndicesInstanced;

	/// <summary>
	/// When using instanced rendering, set this flag to have the flags instanced.
	/// </summary>
	public bool FlagsInstanced;

	/// <summary>
	/// xyz vbo usage hints for the graphics card. Recommended to be set to false when this section of data changes often.
	/// </summary>
	public bool XyzStatic = true;

	/// <summary>
	/// uv vbo usage hints for the graphics card. Recommended to be set to false when this section of data changes often.
	/// </summary>
	public bool UvStatic = true;

	/// <summary>
	/// rgab vbo usage hints for the graphics card. Recommended to be set to false when this section of data changes often.
	/// </summary>
	public bool RgbaStatic = true;

	/// <summary>
	/// rgba2 vbo usage hints for the graphics card. Recommended to be set to false when this section of data changes often.
	/// </summary>
	public bool Rgba2Static = true;

	/// <summary>
	/// indices vbo usage hints for the graphics card. Recommended to be set to false when this section of data changes often.
	/// </summary>
	public bool IndicesStatic = true;

	/// <summary>
	/// flags vbo usage hints for the graphics card. Recommended to be set to false when this section of data changes often.
	/// </summary>
	public bool FlagsStatic = true;

	/// <summary>
	/// For offseting the data in the VBO. This field is used when updating a mesh.
	/// </summary>
	public int XyzOffset;

	/// <summary>
	/// For offseting the data in the VBO. This field is used when updating a mesh.
	/// </summary>
	public int UvOffset;

	/// <summary>
	/// For offseting the data in the VBO. This field is used when updating a mesh.
	/// </summary>
	public int RgbaOffset;

	/// <summary>
	/// For offseting the data in the VBO. This field is used when updating a mesh.
	/// </summary>
	public int Rgba2Offset;

	/// <summary>
	/// For offseting the data in the VBO. This field is used when updating a mesh.
	/// </summary>
	public int FlagsOffset;

	/// <summary>
	/// For offseting the data in the VBO. This field is used when updating a mesh.
	/// </summary>
	public int NormalsOffset;

	/// <summary>
	/// For offseting the data in the VBO. This field is used when updating a mesh.
	/// </summary>
	public int IndicesOffset;

	/// <summary>
	/// The meshes draw mode
	/// </summary>
	public EnumDrawMode mode;

	/// <summary>
	/// Amount of currently assigned normals
	/// </summary>
	public int NormalsCount;

	/// <summary>
	/// Amount of currently assigned vertices
	/// </summary>
	public int VerticesCount;

	/// <summary>
	/// Amount of currently assigned indices
	/// </summary>
	public int IndicesCount;

	/// <summary>
	/// Vertex buffer size
	/// </summary>
	public int VerticesMax;

	/// <summary>
	/// Index buffer size
	/// </summary>
	public int IndicesMax;

	/// <summary>
	/// BlockShapeTesselator xyz faces. Required by TerrainChunkTesselator to determine vertex lightness. Should hold VerticesCount / 4 values. Set to 0 for no face, set to 1..8 for faces 0..7
	/// </summary>
	public byte[] XyzFaces;

	/// <summary>
	/// Amount of assigned xyz face values
	/// </summary>
	public int XyzFacesCount;

	public int TextureIndicesCount;

	public int IndicesPerFace = 6;

	public int VerticesPerFace = 4;

	/// <summary>
	/// BlockShapeTesselator climate colormap ids. Required by TerrainChunkTesselator to determine whether to color a vertex by a color map or not. Should hold VerticesCount / 4 values. Set to 0 for no color mapping, set 1..n for color map 0..n-1
	/// </summary>
	public byte[] ClimateColorMapIds;

	/// <summary>
	/// BlockShapeTesselator season colormap ids. Required by TerrainChunkTesselator to determine whether to color a vertex by a color map or not. Should hold VerticesCount / 4 values. Set to 0 for no color mapping, set 1..n for color map 0..n-1
	/// </summary>
	public byte[] SeasonColorMapIds;

	public bool[] FrostableBits;

	/// <summary>
	/// BlockShapeTesselator renderpass. Required by TerrainChunkTesselator to determine in which mesh data pool each quad should land in. Should hold VerticesCount / 4 values.<br />
	/// Lower 10 bits = render pass<br />
	/// Upper 6 bits = extra bits for tesselators<br />
	///    Bit 10: DisableRandomDrawOffset
	/// </summary>
	public short[] RenderPassesAndExtraBits;

	/// <summary>
	/// Amount of assigned tint values
	/// </summary>
	public int ColorMapIdsCount;

	/// <summary>
	/// Amount of assigned render pass values
	/// </summary>
	public int RenderPassCount;

	/// <summary>
	/// If true, this MeshData was constructed from MeshDataRecycler
	/// </summary>
	public bool Recyclable;

	/// <summary>
	/// The time this MeshData most recently entered the recycling system; the oldest may be garbage collected
	/// </summary>
	public long RecyclingTime;

	/// <summary>
	/// The size of the position values.
	/// </summary>
	public const int XyzSize = 12;

	/// <summary>
	/// The size of the normals.
	/// </summary>
	public const int NormalSize = 4;

	/// <summary>
	/// The size of the color.
	/// </summary>
	public const int RgbaSize = 4;

	/// <summary>
	/// The size of the Uv.
	/// </summary>
	public const int UvSize = 8;

	/// <summary>
	/// the size of the index.
	/// </summary>
	public const int IndexSize = 4;

	/// <summary>
	/// the size of the flags.
	/// </summary>
	public const int FlagsSize = 4;

	[Obsolete("Use RenderPassesAndExtraBits instead")]
	public short[] RenderPasses => RenderPassesAndExtraBits;

	/// <summary>
	/// returns VerticesCount * 3
	/// </summary>
	public int XyzCount => VerticesCount * 3;

	/// <summary>
	/// returns VerticesCount * 4
	/// </summary>
	public int RgbaCount => VerticesCount * 4;

	/// <summary>
	/// returns VerticesCount * 4
	/// </summary>
	public int Rgba2Count => VerticesCount * 4;

	/// <summary>
	/// returns VerticesCount
	/// </summary>
	public int FlagsCount => VerticesCount;

	/// <summary>
	/// returns VerticesCount * 2
	/// </summary>
	public int UvCount => VerticesCount * 2;

	/// <summary>
	/// Gets the number of verticies in the the mesh.
	/// </summary>
	/// <returns>The number of verticies in this mesh.</returns>
	/// <remarks>..Shouldn't this be a property?</remarks>
	public int GetVerticesCount()
	{
		return VerticesCount;
	}

	/// <summary>
	/// Sets the number of verticies in this mesh.
	/// </summary>
	/// <param name="value">The number of verticies in this mesh</param>
	/// <remarks>..Shouldn't this be a property?</remarks>
	public void SetVerticesCount(int value)
	{
		VerticesCount = value;
	}

	/// <summary>
	/// Gets the number of Indicices in this mesh.
	/// </summary>
	/// <returns>The number of indicies in the mesh.</returns>
	/// <remarks>..Shouldn't this be a property?</remarks>
	public int GetIndicesCount()
	{
		return IndicesCount;
	}

	/// <summary>
	/// Sets the number of indices in this mesh.
	/// </summary>
	/// <param name="value">The number of indices in this mesh.</param>
	public void SetIndicesCount(int value)
	{
		IndicesCount = value;
	}

	public float[] GetXyz()
	{
		return xyz;
	}

	public void SetXyz(float[] p)
	{
		xyz = p;
	}

	public byte[] GetRgba()
	{
		return Rgba;
	}

	public void SetRgba(byte[] p)
	{
		Rgba = p;
	}

	public float[] GetUv()
	{
		return Uv;
	}

	public void SetUv(float[] p)
	{
		Uv = p;
	}

	public int[] GetIndices()
	{
		return Indices;
	}

	public void SetIndices(int[] p)
	{
		Indices = p;
	}

	public EnumDrawMode GetMode()
	{
		return mode;
	}

	public void SetMode(EnumDrawMode p)
	{
		mode = p;
	}

	/// <summary>
	/// Offset the mesh by given values
	/// </summary>
	/// <param name="offset"></param>
	public MeshData Translate(Vec3f offset)
	{
		Translate(offset.X, offset.Y, offset.Z);
		return this;
	}

	/// <summary>
	/// Offset the mesh by given values
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	public MeshData Translate(float x, float y, float z)
	{
		for (int i = 0; i < VerticesCount; i++)
		{
			xyz[i * 3] += x;
			xyz[i * 3 + 1] += y;
			xyz[i * 3 + 2] += z;
		}
		return this;
	}

	/// <summary>
	/// Rotate the mesh by given angles around given origin
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="radX"></param>
	/// <param name="radY"></param>
	/// <param name="radZ"></param>
	public MeshData Rotate(Vec3f origin, float radX, float radY, float radZ)
	{
		Span<float> matrix = stackalloc float[16];
		Mat4f.RotateXYZ(matrix, radX, radY, radZ);
		return MatrixTransform(matrix, new float[4], origin);
	}

	/// <summary>
	/// Scale the mesh by given values around given origin
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="scaleX"></param>
	/// <param name="scaleY"></param>
	/// <param name="scaleZ"></param>
	public MeshData Scale(Vec3f origin, float scaleX, float scaleY, float scaleZ)
	{
		for (int i = 0; i < VerticesCount; i++)
		{
			int offset = i * 3;
			float vx = xyz[offset] - origin.X;
			float vy = xyz[offset + 1] - origin.Y;
			float vz = xyz[offset + 2] - origin.Z;
			xyz[offset] = origin.X + scaleX * vx;
			xyz[offset + 1] = origin.Y + scaleY * vy;
			xyz[offset + 2] = origin.Z + scaleZ * vz;
		}
		return this;
	}

	/// <summary>
	/// Apply given transformation on the mesh
	/// </summary>
	/// <param name="transform"></param>        
	public MeshData ModelTransform(ModelTransform transform)
	{
		float[] matrix = Mat4f.Create();
		float dx = transform.Translation.X + transform.Origin.X;
		float dy = transform.Translation.Y + transform.Origin.Y;
		float dz = transform.Translation.Z + transform.Origin.Z;
		Mat4f.Translate(matrix, matrix, dx, dy, dz);
		Mat4f.RotateX(matrix, matrix, transform.Rotation.X * ((float)Math.PI / 180f));
		Mat4f.RotateY(matrix, matrix, transform.Rotation.Y * ((float)Math.PI / 180f));
		Mat4f.RotateZ(matrix, matrix, transform.Rotation.Z * ((float)Math.PI / 180f));
		Mat4f.Scale(matrix, matrix, transform.ScaleXYZ.X, transform.ScaleXYZ.Y, transform.ScaleXYZ.Z);
		Mat4f.Translate(matrix, matrix, 0f - transform.Origin.X, 0f - transform.Origin.Y, 0f - transform.Origin.Z);
		MatrixTransform(matrix);
		return this;
	}

	/// <summary>
	/// Apply given transformation on the mesh
	/// </summary>
	/// <param name="matrix"></param>
	public MeshData MatrixTransform(float[] matrix)
	{
		return MatrixTransform(matrix, new float[4]);
	}

	/// <summary>
	/// Apply given transformation on the mesh - specifying two temporary vectors to work in (these can then be re-used for performance reasons)
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="vec">a re-usable float[4], values unimportant</param>
	/// <param name="origin">origin point</param>
	public MeshData MatrixTransform(float[] matrix, float[] vec, Vec3f origin = null)
	{
		return MatrixTransform((Span<float>)matrix, vec, origin);
	}

	public MeshData MatrixTransform(Span<float> matrix, float[] vec, Vec3f origin = null)
	{
		if (origin == null)
		{
			for (int l = 0; l < VerticesCount; l++)
			{
				Mat4f.MulWithVec3_Position(matrix, xyz, xyz, l * 3);
			}
		}
		else
		{
			for (int m = 0; m < VerticesCount; m++)
			{
				Mat4f.MulWithVec3_Position_WithOrigin(matrix, xyz, xyz, m * 3, origin);
			}
		}
		if (Normals != null)
		{
			for (int k = 0; k < VerticesCount; k++)
			{
				NormalUtil.FromPackedNormal(Normals[k], ref vec);
				Mat4f.MulWithVec4(matrix, vec, vec);
				Normals[k] = NormalUtil.PackNormal(vec);
			}
		}
		if (XyzFaces != null)
		{
			for (int j = 0; j < XyzFaces.Length; j++)
			{
				byte meshFaceIndex = XyzFaces[j];
				if (meshFaceIndex != 0)
				{
					Vec3f normalfv = BlockFacing.ALLFACES[meshFaceIndex - 1].Normalf;
					XyzFaces[j] = Mat4f.MulWithVec3_BlockFacing(matrix, normalfv).MeshDataIndex;
				}
			}
		}
		if (Flags != null)
		{
			for (int i = 0; i < Flags.Length; i++)
			{
				VertexFlags.UnpackNormal(Flags[i], vec);
				Mat4f.MulWithVec3(matrix, vec, vec);
				float len = GameMath.RootSumOfSquares(vec[0], vec[1], vec[2]);
				Flags[i] = (Flags[i] & -33546241) | VertexFlags.PackNormal(vec[0] / len, vec[1] / len, vec[2] / len);
			}
		}
		return this;
	}

	/// <summary>
	/// Apply given transformation on the mesh
	/// </summary>
	/// <param name="matrix"></param>
	public MeshData MatrixTransform(double[] matrix)
	{
		if (Mat4d.IsTranslationOnly(matrix))
		{
			Translate((float)matrix[12], (float)matrix[13], (float)matrix[14]);
			return this;
		}
		double[] inVec = new double[4];
		for (int k = 0; k < VerticesCount; k++)
		{
			float x = xyz[k * 3];
			float y = xyz[k * 3 + 1];
			float z = xyz[k * 3 + 2];
			xyz[k * 3] = (float)(matrix[0] * (double)x + matrix[4] * (double)y + matrix[8] * (double)z + matrix[12]);
			xyz[k * 3 + 1] = (float)(matrix[1] * (double)x + matrix[5] * (double)y + matrix[9] * (double)z + matrix[13]);
			xyz[k * 3 + 2] = (float)(matrix[2] * (double)x + matrix[6] * (double)y + matrix[10] * (double)z + matrix[14]);
			if (Normals != null)
			{
				NormalUtil.FromPackedNormal(Normals[k], ref inVec);
				double[] outVec = Mat4d.MulWithVec4(matrix, inVec);
				Normals[k] = NormalUtil.PackNormal(outVec);
			}
		}
		if (XyzFaces != null)
		{
			for (int j = 0; j < XyzFaces.Length; j++)
			{
				byte meshFaceIndex = XyzFaces[j];
				if (meshFaceIndex != 0)
				{
					Vec3d normald = BlockFacing.ALLFACES[meshFaceIndex - 1].Normald;
					double x2 = matrix[0] * normald.X + matrix[4] * normald.Y + matrix[8] * normald.Z;
					double oy2 = matrix[1] * normald.X + matrix[5] * normald.Y + matrix[9] * normald.Z;
					double oz2 = matrix[2] * normald.X + matrix[6] * normald.Y + matrix[10] * normald.Z;
					BlockFacing rotatedFacing = BlockFacing.FromVector(x2, oy2, oz2);
					XyzFaces[j] = rotatedFacing.MeshDataIndex;
				}
			}
		}
		if (Flags != null)
		{
			for (int i = 0; i < Flags.Length; i++)
			{
				VertexFlags.UnpackNormal(Flags[i], inVec);
				double ox = matrix[0] * inVec[0] + matrix[4] * inVec[1] + matrix[8] * inVec[2];
				double oy = matrix[1] * inVec[0] + matrix[5] * inVec[1] + matrix[9] * inVec[2];
				double oz = matrix[2] * inVec[0] + matrix[6] * inVec[1] + matrix[10] * inVec[2];
				Flags[i] = (Flags[i] & -33546241) | VertexFlags.PackNormal(ox, oy, oz);
			}
		}
		return this;
	}

	/// <summary>
	/// Creates a new mesh data instance with no components initialized.
	/// </summary>
	public MeshData(bool initialiseArrays = true)
	{
		if (initialiseArrays)
		{
			XyzFaces = new byte[0];
			ClimateColorMapIds = new byte[0];
			SeasonColorMapIds = new byte[0];
			RenderPassesAndExtraBits = new short[0];
		}
	}

	/// <summary>
	/// Creates a new mesh data instance with given components, but you can also freely nullify or set individual components after initialization
	/// Any component that is null is ignored by UploadModel/UpdateModel
	/// </summary>
	/// <param name="capacityVertices"></param>
	/// <param name="capacityIndices"></param>
	/// <param name="withUv"></param>
	/// <param name="withNormals"></param>
	/// <param name="withRgba"></param>
	/// <param name="withFlags"></param>
	public MeshData(int capacityVertices, int capacityIndices, bool withNormals = false, bool withUv = true, bool withRgba = true, bool withFlags = true)
	{
		XyzFaces = new byte[0];
		ClimateColorMapIds = new byte[0];
		SeasonColorMapIds = new byte[0];
		RenderPassesAndExtraBits = new short[0];
		xyz = new float[capacityVertices * 3];
		if (withNormals)
		{
			Normals = new int[capacityVertices];
		}
		if (withUv)
		{
			Uv = new float[capacityVertices * 2];
			TextureIndices = new byte[capacityVertices / VerticesPerFace];
		}
		if (withRgba)
		{
			Rgba = new byte[capacityVertices * 4];
		}
		if (withFlags)
		{
			Flags = new int[capacityVertices];
		}
		Indices = new int[capacityIndices];
		IndicesMax = capacityIndices;
		VerticesMax = capacityVertices;
	}

	/// <summary>
	/// This constructor creates a basic MeshData with xyz, Uv, Rgba, Flags and Indices only; Indices to Vertices ratio is the default 6:4
	/// </summary>
	/// <param name="capacity"></param>
	public MeshData(int capacity)
	{
		xyz = new float[capacity * 3];
		Uv = new float[capacity * 2];
		Rgba = new byte[capacity * 4];
		Flags = new int[capacity];
		VerticesMax = capacity;
		int capacityIndices = capacity * 6 / 4;
		Indices = new int[capacityIndices];
		IndicesMax = capacityIndices;
	}

	/// <summary>
	/// Sets up the tints array for holding tint info
	/// </summary>
	/// <returns></returns>
	public MeshData WithColorMaps()
	{
		SeasonColorMapIds = new byte[VerticesMax / 4];
		ClimateColorMapIds = new byte[VerticesMax / 4];
		return this;
	}

	/// <summary>
	/// Sets up the xyzfaces array for holding xyzfaces info
	/// </summary>
	/// <returns></returns>
	public MeshData WithXyzFaces()
	{
		XyzFaces = new byte[VerticesMax / 4];
		return this;
	}

	/// <summary>
	/// Sets up the renderPasses array for holding render pass info
	/// </summary>
	/// <returns></returns>
	public MeshData WithRenderpasses()
	{
		RenderPassesAndExtraBits = new short[VerticesMax / 4];
		return this;
	}

	/// <summary>
	/// Sets up the renderPasses array for holding render pass info
	/// </summary>
	/// <returns></returns>
	public MeshData WithNormals()
	{
		Normals = new int[VerticesMax];
		return this;
	}

	/// <summary>
	/// Add supplied mesh data to this mesh. If a given dataset is not set, it is not copied from the sourceMesh. Automatically adjusts the indices for you.
	/// Is filtered to only add mesh data for given render pass.
	/// A negative render pass value defaults to EnumChunkRenderPass.Opaque
	/// </summary>
	/// <param name="data"></param>
	/// <param name="filterByRenderPass"></param>
	public void AddMeshData(MeshData data, EnumChunkRenderPass filterByRenderPass)
	{
		int renderPassInt = (int)filterByRenderPass;
		AddMeshData(data, (int i) => data.RenderPassesAndExtraBits[i] != renderPassInt && (data.RenderPassesAndExtraBits[i] != -1 || filterByRenderPass != EnumChunkRenderPass.Opaque));
	}

	public void AddMeshData(MeshData data, MeshDataFilterDelegate dele = null)
	{
		int di = 0;
		int verticesPerFace = VerticesPerFace;
		int indicesPerFace = IndicesPerFace;
		for (int i = 0; i < data.VerticesCount / verticesPerFace; i++)
		{
			if (dele != null && !dele(i))
			{
				di += indicesPerFace;
				continue;
			}
			int lastelement = VerticesCount;
			if (Uv != null)
			{
				AddTextureId(data.TextureIds[data.TextureIndices[i]]);
			}
			for (int k2 = 0; k2 < verticesPerFace; k2++)
			{
				int vertexNum = i * verticesPerFace + k2;
				if (VerticesCount >= VerticesMax)
				{
					GrowVertexBuffer();
					GrowNormalsBuffer();
				}
				xyz[XyzCount] = data.xyz[vertexNum * 3];
				xyz[XyzCount + 1] = data.xyz[vertexNum * 3 + 1];
				xyz[XyzCount + 2] = data.xyz[vertexNum * 3 + 2];
				if (Normals != null)
				{
					Normals[VerticesCount] = data.Normals[vertexNum];
				}
				if (Uv != null)
				{
					Uv[UvCount] = data.Uv[vertexNum * 2];
					Uv[UvCount + 1] = data.Uv[vertexNum * 2 + 1];
				}
				if (Rgba != null)
				{
					Rgba[RgbaCount] = data.Rgba[vertexNum * 4];
					Rgba[RgbaCount + 1] = data.Rgba[vertexNum * 4 + 1];
					Rgba[RgbaCount + 2] = data.Rgba[vertexNum * 4 + 2];
					Rgba[RgbaCount + 3] = data.Rgba[vertexNum * 4 + 3];
				}
				if (Flags != null)
				{
					Flags[FlagsCount] = data.Flags[vertexNum];
				}
				if (CustomInts != null && data.CustomInts != null)
				{
					int valsPerVertex4 = ((data.CustomInts.InterleaveStride == 0) ? data.CustomInts.InterleaveSizes[0] : data.CustomInts.InterleaveStride);
					for (int m = 0; m < valsPerVertex4; m++)
					{
						CustomInts.Add(data.CustomInts.Values[vertexNum / valsPerVertex4 + m]);
					}
				}
				if (CustomFloats != null && data.CustomFloats != null)
				{
					int valsPerVertex3 = ((data.CustomFloats.InterleaveStride == 0) ? data.CustomFloats.InterleaveSizes[0] : data.CustomFloats.InterleaveStride);
					for (int l = 0; l < valsPerVertex3; l++)
					{
						CustomFloats.Add(data.CustomFloats.Values[vertexNum / valsPerVertex3 + l]);
					}
				}
				if (CustomShorts != null && data.CustomShorts != null)
				{
					int valsPerVertex2 = ((data.CustomShorts.InterleaveStride == 0) ? data.CustomShorts.InterleaveSizes[0] : data.CustomShorts.InterleaveStride);
					for (int k = 0; k < valsPerVertex2; k++)
					{
						CustomShorts.Add(data.CustomShorts.Values[vertexNum / valsPerVertex2 + k]);
					}
				}
				if (CustomBytes != null && data.CustomBytes != null)
				{
					int valsPerVertex = ((data.CustomBytes.InterleaveStride == 0) ? data.CustomBytes.InterleaveSizes[0] : data.CustomBytes.InterleaveStride);
					for (int j = 0; j < valsPerVertex; j++)
					{
						CustomBytes.Add(data.CustomBytes.Values[vertexNum / valsPerVertex + j]);
					}
				}
				VerticesCount++;
			}
			for (int n = 0; n < indicesPerFace; n++)
			{
				int indexNum = i * indicesPerFace + n;
				AddIndex(lastelement - (i - di / indicesPerFace) * verticesPerFace + data.Indices[indexNum] - 2 * di / 3);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte getTextureIndex(int textureId)
	{
		for (byte i = 0; i < TextureIds.Length; i++)
		{
			if (TextureIds[i] == textureId)
			{
				return i;
			}
		}
		TextureIds = TextureIds.Append(textureId);
		return (byte)(TextureIds.Length - 1);
	}

	/// <summary>
	/// Add supplied mesh data to this mesh. If a given dataset is not set, it is not copied from the sourceMesh. Automatically adjusts the indices for you.
	/// </summary>
	/// <param name="sourceMesh"></param>
	public void AddMeshData(MeshData sourceMesh)
	{
		for (int i = 0; i < sourceMesh.VerticesCount; i++)
		{
			if (VerticesCount >= VerticesMax)
			{
				GrowVertexBuffer();
				GrowNormalsBuffer();
			}
			xyz[XyzCount] = sourceMesh.xyz[i * 3];
			xyz[XyzCount + 1] = sourceMesh.xyz[i * 3 + 1];
			xyz[XyzCount + 2] = sourceMesh.xyz[i * 3 + 2];
			if (Normals != null)
			{
				Normals[VerticesCount] = sourceMesh.Normals[i];
			}
			if (Uv != null)
			{
				Uv[UvCount] = sourceMesh.Uv[i * 2];
				Uv[UvCount + 1] = sourceMesh.Uv[i * 2 + 1];
			}
			if (Rgba != null)
			{
				Rgba[RgbaCount] = sourceMesh.Rgba[i * 4];
				Rgba[RgbaCount + 1] = sourceMesh.Rgba[i * 4 + 1];
				Rgba[RgbaCount + 2] = sourceMesh.Rgba[i * 4 + 2];
				Rgba[RgbaCount + 3] = sourceMesh.Rgba[i * 4 + 3];
			}
			if (Flags != null && sourceMesh.Flags != null)
			{
				Flags[VerticesCount] = sourceMesh.Flags[i];
			}
			VerticesCount++;
		}
		addMeshDataEtc(sourceMesh);
	}

	public void AddMeshData(MeshData sourceMesh, float xOffset, float yOffset, float zOffset)
	{
		for (int i = 0; i < sourceMesh.VerticesCount; i++)
		{
			if (VerticesCount >= VerticesMax)
			{
				GrowVertexBuffer();
				GrowNormalsBuffer();
			}
			xyz[XyzCount] = sourceMesh.xyz[i * 3] + xOffset;
			xyz[XyzCount + 1] = sourceMesh.xyz[i * 3 + 1] + yOffset;
			xyz[XyzCount + 2] = sourceMesh.xyz[i * 3 + 2] + zOffset;
			if (Normals != null)
			{
				Normals[VerticesCount] = sourceMesh.Normals[i];
			}
			if (Uv != null)
			{
				Uv[UvCount] = sourceMesh.Uv[i * 2];
				Uv[UvCount + 1] = sourceMesh.Uv[i * 2 + 1];
			}
			if (Rgba != null)
			{
				Rgba[RgbaCount] = sourceMesh.Rgba[i * 4];
				Rgba[RgbaCount + 1] = sourceMesh.Rgba[i * 4 + 1];
				Rgba[RgbaCount + 2] = sourceMesh.Rgba[i * 4 + 2];
				Rgba[RgbaCount + 3] = sourceMesh.Rgba[i * 4 + 3];
			}
			if (Flags != null && sourceMesh.Flags != null)
			{
				Flags[VerticesCount] = sourceMesh.Flags[i];
			}
			VerticesCount++;
		}
		addMeshDataEtc(sourceMesh);
	}

	private void addMeshDataEtc(MeshData sourceMesh)
	{
		for (int i4 = 0; i4 < sourceMesh.XyzFacesCount; i4++)
		{
			AddXyzFace(sourceMesh.XyzFaces[i4]);
		}
		for (int i3 = 0; i3 < sourceMesh.TextureIndicesCount; i3++)
		{
			AddTextureId(sourceMesh.TextureIds[sourceMesh.TextureIndices[i3]]);
		}
		int start = ((IndicesCount > 0) ? ((mode == EnumDrawMode.Triangles) ? (Indices[IndicesCount - 1] + 1) : (Indices[IndicesCount - 2] + 1)) : 0);
		for (int i2 = 0; i2 < sourceMesh.IndicesCount; i2++)
		{
			AddIndex(start + sourceMesh.Indices[i2]);
		}
		for (int n = 0; n < sourceMesh.ColorMapIdsCount; n++)
		{
			AddColorMapIndex(sourceMesh.ClimateColorMapIds[n], sourceMesh.SeasonColorMapIds[n]);
		}
		for (int m = 0; m < sourceMesh.RenderPassCount; m++)
		{
			AddRenderPass(sourceMesh.RenderPassesAndExtraBits[m]);
		}
		if (CustomInts != null && sourceMesh.CustomInts != null)
		{
			for (int l = 0; l < sourceMesh.CustomInts.Count; l++)
			{
				CustomInts.Add(sourceMesh.CustomInts.Values[l]);
			}
		}
		if (CustomFloats != null && sourceMesh.CustomFloats != null)
		{
			for (int k = 0; k < sourceMesh.CustomFloats.Count; k++)
			{
				CustomFloats.Add(sourceMesh.CustomFloats.Values[k]);
			}
		}
		if (CustomShorts != null && sourceMesh.CustomShorts != null)
		{
			for (int j = 0; j < sourceMesh.CustomShorts.Count; j++)
			{
				CustomShorts.Add(sourceMesh.CustomShorts.Values[j]);
			}
		}
		if (CustomBytes != null && sourceMesh.CustomBytes != null)
		{
			for (int i = 0; i < sourceMesh.CustomBytes.Count; i++)
			{
				CustomBytes.Add(sourceMesh.CustomBytes.Values[i]);
			}
		}
	}

	/// <summary>
	/// Removes the last index in the indices array
	/// </summary>
	public void RemoveIndex()
	{
		if (IndicesCount > 0)
		{
			IndicesCount--;
		}
	}

	/// <summary>
	/// Removes the last vertex in the vertices array
	/// </summary>
	public void RemoveVertex()
	{
		if (VerticesCount > 0)
		{
			VerticesCount--;
		}
	}

	/// <summary>
	/// Removes the last "count" vertices from the vertex array
	/// </summary>
	/// <param name="count"></param>
	public void RemoveVertices(int count)
	{
		VerticesCount = Math.Max(0, VerticesCount - count);
	}

	/// <summary>
	/// Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="color"></param>
	public unsafe void AddVertexSkipTex(float x, float y, float z, int color = -1)
	{
		int count = VerticesCount;
		if (count >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		int xyzCount = count * 3;
		array[xyzCount] = x;
		array[xyzCount + 1] = y;
		array[xyzCount + 2] = z;
		fixed (byte* rgbaByte = Rgba)
		{
			int* rgbaInt = (int*)rgbaByte;
			rgbaInt[count] = color;
		}
		VerticesCount = count + 1;
	}

	/// <summary>
	/// Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="u"></param>
	/// <param name="v"></param>
	public void AddVertex(float x, float y, float z, float u, float v)
	{
		int count = VerticesCount;
		if (count >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] Uv = this.Uv;
		int xyzCount = count * 3;
		array[xyzCount] = x;
		array[xyzCount + 1] = y;
		array[xyzCount + 2] = z;
		int uvCount = count * 2;
		Uv[uvCount] = u;
		Uv[uvCount + 1] = v;
		VerticesCount = count + 1;
	}

	/// <summary>
	/// Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="u"></param>
	/// <param name="v"></param>
	/// <param name="color"></param>
	public void AddVertex(float x, float y, float z, float u, float v, int color)
	{
		AddWithFlagsVertex(x, y, z, u, v, color, 0);
	}

	/// <summary>
	/// Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="u"></param>
	/// <param name="v"></param>
	/// <param name="color"></param>
	public void AddVertex(float x, float y, float z, float u, float v, byte[] color)
	{
		int count = VerticesCount;
		if (count >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] Uv = this.Uv;
		byte[] Rgba = this.Rgba;
		int xyzCount = count * 3;
		array[xyzCount] = x;
		array[xyzCount + 1] = y;
		array[xyzCount + 2] = z;
		int uvCount = count * 2;
		Uv[uvCount] = u;
		Uv[uvCount + 1] = v;
		int rgbaCount = count * 4;
		Rgba[rgbaCount] = color[0];
		Rgba[rgbaCount + 1] = color[1];
		Rgba[rgbaCount + 2] = color[2];
		Rgba[rgbaCount + 3] = color[3];
		VerticesCount = count + 1;
	}

	/// <summary>
	/// Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="u"></param>
	/// <param name="v"></param>
	/// <param name="color"></param>
	/// <param name="flags"></param>
	public unsafe void AddWithFlagsVertex(float x, float y, float z, float u, float v, int color, int flags)
	{
		int count = VerticesCount;
		if (count >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] Uv = this.Uv;
		int xyzCount = count * 3;
		array[xyzCount] = x;
		array[xyzCount + 1] = y;
		array[xyzCount + 2] = z;
		int uvCount = count * 2;
		Uv[uvCount] = u;
		Uv[uvCount + 1] = v;
		if (Flags != null)
		{
			Flags[count] = flags;
		}
		fixed (byte* rgbaByte = Rgba)
		{
			int* rgbaInt = (int*)rgbaByte;
			rgbaInt[count] = color;
		}
		VerticesCount = count + 1;
	}

	/// <summary>
	/// Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="u"></param>
	/// <param name="v"></param>
	/// <param name="color"></param>
	/// <param name="flags"></param>
	public unsafe void AddVertexWithFlags(float x, float y, float z, float u, float v, int color, int flags)
	{
		AddVertexWithFlagsSkipColor(x, y, z, u, v, flags);
		fixed (byte* rgbaByte = Rgba)
		{
			int* rgbaInt = (int*)rgbaByte;
			rgbaInt[VerticesCount - 1] = color;
		}
	}

	/// <summary>
	/// Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="u"></param>
	/// <param name="v"></param>
	/// <param name="flags"></param>
	public void AddVertexWithFlagsSkipColor(float x, float y, float z, float u, float v, int flags)
	{
		int count = VerticesCount;
		if (count >= VerticesMax)
		{
			GrowVertexBuffer();
		}
		float[] array = xyz;
		float[] Uv = this.Uv;
		int xyzCount = count * 3;
		array[xyzCount] = x;
		array[xyzCount + 1] = y;
		array[xyzCount + 2] = z;
		int uvCount = count * 2;
		Uv[uvCount] = u;
		Uv[uvCount + 1] = v;
		if (Flags != null)
		{
			Flags[count] = flags;
		}
		VerticesCount = count + 1;
	}

	/// <summary>
	/// Applies a vertex flag to an existing MeshData (uses binary OR)
	/// </summary>
	public void SetVertexFlags(int flag)
	{
		if (Flags != null)
		{
			int count = VerticesCount;
			for (int i = 0; i < count; i++)
			{
				Flags[i] |= flag;
			}
		}
	}

	/// <summary>
	/// Adds a new normal to the mesh. Grows the normal buffer if necessary.
	/// </summary>
	/// <param name="normalizedX"></param>
	/// <param name="normalizedY"></param>
	/// <param name="normalizedZ"></param>
	public void AddNormal(float normalizedX, float normalizedY, float normalizedZ)
	{
		if (NormalsCount >= Normals.Length)
		{
			GrowNormalsBuffer();
		}
		Normals[NormalsCount++] = NormalUtil.PackNormal(normalizedX, normalizedY, normalizedZ);
	}

	/// <summary>
	/// Adds a new normal to the mesh. Grows the normal buffer if necessary.
	/// </summary>
	/// <param name="facing"></param>
	public void AddNormal(BlockFacing facing)
	{
		if (NormalsCount >= Normals.Length)
		{
			GrowNormalsBuffer();
		}
		Normals[NormalsCount++] = facing.NormalPacked;
	}

	public void AddColorMapIndex(byte climateMapIndex, byte seasonMapIndex)
	{
		if (ColorMapIdsCount >= SeasonColorMapIds.Length)
		{
			Array.Resize(ref SeasonColorMapIds, SeasonColorMapIds.Length + 32);
			Array.Resize(ref ClimateColorMapIds, ClimateColorMapIds.Length + 32);
		}
		ClimateColorMapIds[ColorMapIdsCount] = climateMapIndex;
		SeasonColorMapIds[ColorMapIdsCount++] = seasonMapIndex;
	}

	public void AddColorMapIndex(byte climateMapIndex, byte seasonMapIndex, bool frostableBit)
	{
		if (FrostableBits == null)
		{
			FrostableBits = new bool[ClimateColorMapIds.Length];
		}
		if (ColorMapIdsCount >= SeasonColorMapIds.Length)
		{
			Array.Resize(ref SeasonColorMapIds, SeasonColorMapIds.Length + 32);
			Array.Resize(ref ClimateColorMapIds, ClimateColorMapIds.Length + 32);
			Array.Resize(ref FrostableBits, FrostableBits.Length + 32);
		}
		FrostableBits[ColorMapIdsCount] = frostableBit;
		ClimateColorMapIds[ColorMapIdsCount] = climateMapIndex;
		SeasonColorMapIds[ColorMapIdsCount++] = seasonMapIndex;
	}

	public void AddRenderPass(short renderPass)
	{
		if (RenderPassCount >= RenderPassesAndExtraBits.Length)
		{
			Array.Resize(ref RenderPassesAndExtraBits, RenderPassesAndExtraBits.Length + 32);
		}
		RenderPassesAndExtraBits[RenderPassCount++] = renderPass;
	}

	public void AddXyzFace(byte faceIndex)
	{
		if (XyzFacesCount >= XyzFaces.Length)
		{
			Array.Resize(ref XyzFaces, XyzFaces.Length + 32);
		}
		XyzFaces[XyzFacesCount++] = faceIndex;
	}

	public void AddTextureId(int textureId)
	{
		if (TextureIndicesCount >= TextureIndices.Length)
		{
			Array.Resize(ref TextureIndices, TextureIndices.Length + 32);
		}
		TextureIndices[TextureIndicesCount++] = getTextureIndex(textureId);
	}

	public void AddIndex(int index)
	{
		if (IndicesCount >= IndicesMax)
		{
			GrowIndexBuffer();
		}
		Indices[IndicesCount++] = index;
	}

	/// <summary>
	/// Add 6 indices
	/// </summary>
	/// <param name="i1"></param>
	/// <param name="i2"></param>
	/// <param name="i3"></param>
	/// <param name="i4"></param>
	/// <param name="i5"></param>
	/// <param name="i6"></param>
	public void AddIndices(int i1, int i2, int i3, int i4, int i5, int i6)
	{
		int count = IndicesCount;
		if (count + 6 > IndicesMax)
		{
			GrowIndexBuffer(6);
		}
		int[] indices = Indices;
		indices[count++] = i1;
		indices[count++] = i2;
		indices[count++] = i3;
		indices[count++] = i4;
		indices[count++] = i5;
		indices[count++] = i6;
		IndicesCount = count;
	}

	public void AddIndices(int[] indices)
	{
		int length = indices.Length;
		int count = IndicesCount;
		if (count + length > IndicesMax)
		{
			GrowIndexBuffer(length);
		}
		int[] currentIndices = Indices;
		for (int i = 0; i < length; i++)
		{
			currentIndices[count++] = indices[i];
		}
		IndicesCount = count;
	}

	public void GrowIndexBuffer()
	{
		int i = IndicesCount;
		int[] largerIndices = new int[IndicesMax = i * 2];
		int[] currentIndices = Indices;
		while (--i >= 0)
		{
			largerIndices[i] = currentIndices[i];
		}
		Indices = largerIndices;
	}

	public void GrowIndexBuffer(int byAtLeastQuantity)
	{
		int newSize = Math.Max(IndicesCount * 2, IndicesCount + byAtLeastQuantity);
		int[] largerIndices = new int[IndicesMax = newSize];
		int[] currentIndices = Indices;
		int i = IndicesCount;
		while (--i >= 0)
		{
			largerIndices[i] = currentIndices[i];
		}
		Indices = largerIndices;
	}

	public void GrowNormalsBuffer()
	{
		if (Normals != null)
		{
			int i = Normals.Length;
			int[] largerNormals = new int[i * 2];
			int[] currentNormals = Normals;
			while (--i >= 0)
			{
				largerNormals[i] = currentNormals[i];
			}
			Normals = largerNormals;
		}
	}

	/// <summary>
	/// Doubles the size of the xyz, uv, rgba, rgba2 and flags arrays
	/// </summary>
	public void GrowVertexBuffer()
	{
		if (xyz != null)
		{
			float[] largerXyz = new float[XyzCount * 2];
			float[] currentXyz = xyz;
			int l = currentXyz.Length;
			while (--l >= 0)
			{
				largerXyz[l] = currentXyz[l];
			}
			xyz = largerXyz;
		}
		if (Uv != null)
		{
			float[] largerUv = new float[UvCount * 2];
			float[] currentUv = Uv;
			int k = currentUv.Length;
			while (--k >= 0)
			{
				largerUv[k] = currentUv[k];
			}
			Uv = largerUv;
		}
		if (Rgba != null)
		{
			byte[] largerRgba = new byte[RgbaCount * 2];
			byte[] currentRgba = Rgba;
			int j = currentRgba.Length;
			while (--j >= 0)
			{
				largerRgba[j] = currentRgba[j];
			}
			Rgba = largerRgba;
		}
		if (Flags != null)
		{
			int[] largerFlags = new int[FlagsCount * 2];
			int[] currentFlags = Flags;
			int i = currentFlags.Length;
			while (--i >= 0)
			{
				largerFlags[i] = currentFlags[i];
			}
			Flags = largerFlags;
		}
		VerticesMax *= 2;
	}

	/// <summary>
	/// Resizes all buffers to tightly fit the data. Recommended to run this method for long-term in-memory storage of meshdata for meshes that won't get any new vertices added
	/// </summary>
	public void CompactBuffers()
	{
		if (xyz != null)
		{
			int cnt4 = XyzCount;
			float[] tightXyz = new float[cnt4 + 1];
			Array.Copy(xyz, 0, tightXyz, 0, cnt4);
			xyz = tightXyz;
		}
		if (Uv != null)
		{
			int cnt3 = UvCount;
			float[] tightUv = new float[cnt3 + 1];
			Array.Copy(Uv, 0, tightUv, 0, cnt3);
			Uv = tightUv;
		}
		if (Rgba != null)
		{
			int cnt2 = RgbaCount;
			byte[] tightRgba = new byte[cnt2 + 1];
			Array.Copy(Rgba, 0, tightRgba, 0, cnt2);
			Rgba = tightRgba;
		}
		if (Flags != null)
		{
			int cnt = FlagsCount;
			int[] tightFlags = new int[cnt + 1];
			Array.Copy(Flags, 0, tightFlags, 0, cnt);
			Flags = tightFlags;
		}
		VerticesMax = VerticesCount;
	}

	/// <summary>
	/// Creates a compact, deep copy of the mesh
	/// </summary>
	/// <returns></returns>
	public MeshData Clone()
	{
		MeshData newMesh = CloneBasicData();
		CloneExtraData(newMesh);
		return newMesh;
	}

	/// <summary>
	/// Clone the basic xyz, uv, rgba and flags arrays, which are common to every chunk mesh (though not necessarily all used by individual block/item/entity models)
	/// </summary>
	/// <returns></returns>
	private MeshData CloneBasicData()
	{
		MeshData dest = new MeshData(initialiseArrays: false);
		dest.VerticesPerFace = VerticesPerFace;
		dest.IndicesPerFace = IndicesPerFace;
		dest.SetVerticesCount(VerticesCount);
		dest.xyz = xyz.FastCopy(XyzCount);
		if (Uv != null)
		{
			dest.Uv = Uv.FastCopy(UvCount);
		}
		if (Rgba != null)
		{
			dest.Rgba = Rgba.FastCopy(RgbaCount);
		}
		if (Flags != null)
		{
			dest.Flags = Flags.FastCopy(FlagsCount);
		}
		dest.Indices = Indices.FastCopy(IndicesCount);
		dest.SetIndicesCount(IndicesCount);
		dest.VerticesMax = VerticesCount;
		dest.IndicesMax = dest.Indices.Length;
		return dest;
	}

	private void CopyBasicData(MeshData dest)
	{
		dest.SetVerticesCount(VerticesCount);
		dest.SetIndicesCount(IndicesCount);
		Array.Copy(xyz, dest.xyz, XyzCount);
		Array.Copy(Uv, dest.Uv, UvCount);
		Array.Copy(Rgba, dest.Rgba, RgbaCount);
		Array.Copy(Flags, dest.Flags, FlagsCount);
		Array.Copy(Indices, dest.Indices, IndicesCount);
	}

	public void DisposeBasicData()
	{
		xyz = null;
		Uv = null;
		Rgba = null;
		Flags = null;
		Indices = null;
	}

	/// <summary>
	/// Clone the extra mesh data fields. Some of these fields are used only by block/item meshes (some only be Microblocks). Some others are not used by all chunk meshes, though may be used by certain meshes in certain renderpasses (e.g. CustomInts). Either way, cannot sensibly be retained within the MeshDataRecycler system, must be cloned every time
	/// </summary>
	/// <returns></returns>
	private void CloneExtraData(MeshData dest)
	{
		if (Normals != null)
		{
			dest.Normals = Normals.FastCopy(NormalsCount);
		}
		if (XyzFaces != null)
		{
			dest.XyzFaces = XyzFaces.FastCopy(XyzFacesCount);
			dest.XyzFacesCount = XyzFacesCount;
		}
		if (TextureIndices != null)
		{
			dest.TextureIndices = TextureIndices.FastCopy(TextureIndicesCount);
			dest.TextureIndicesCount = TextureIndicesCount;
			dest.TextureIds = (int[])TextureIds.Clone();
		}
		if (ClimateColorMapIds != null)
		{
			dest.ClimateColorMapIds = ClimateColorMapIds.FastCopy(ColorMapIdsCount);
			dest.ColorMapIdsCount = ColorMapIdsCount;
		}
		if (SeasonColorMapIds != null)
		{
			dest.SeasonColorMapIds = SeasonColorMapIds.FastCopy(ColorMapIdsCount);
			dest.ColorMapIdsCount = ColorMapIdsCount;
		}
		if (RenderPassesAndExtraBits != null)
		{
			dest.RenderPassesAndExtraBits = RenderPassesAndExtraBits.FastCopy(RenderPassCount);
			dest.RenderPassCount = RenderPassCount;
		}
		if (CustomFloats != null)
		{
			dest.CustomFloats = CustomFloats.Clone();
		}
		if (CustomShorts != null)
		{
			dest.CustomShorts = CustomShorts.Clone();
		}
		if (CustomBytes != null)
		{
			dest.CustomBytes = CustomBytes.Clone();
		}
		if (CustomInts != null)
		{
			dest.CustomInts = CustomInts.Clone();
		}
	}

	private void DisposeExtraData()
	{
		Normals = null;
		NormalsCount = 0;
		XyzFaces = null;
		XyzFacesCount = 0;
		TextureIndices = null;
		TextureIndicesCount = 0;
		TextureIds = null;
		ClimateColorMapIds = null;
		SeasonColorMapIds = null;
		ColorMapIdsCount = 0;
		RenderPassesAndExtraBits = null;
		RenderPassCount = 0;
		CustomFloats = null;
		CustomShorts = null;
		CustomBytes = null;
		CustomInts = null;
	}

	public MeshData CloneUsingRecycler()
	{
		if (VerticesCount * 34 < 4096 || Uv == null || Rgba == null || Flags == null || VerticesPerFace != 4 || IndicesPerFace != 6)
		{
			return Clone();
		}
		int requiredSize = Math.Max(VerticesCount, (IndicesCount + 6 - 1) / 6 * 4);
		if ((float)requiredSize > (float)VerticesCount * 1.05f || (float)(requiredSize * 6 / 4) > (float)IndicesCount * 1.2f)
		{
			return Clone();
		}
		MeshData newMesh = Recycler.GetOrCreateMesh(requiredSize);
		CopyBasicData(newMesh);
		CloneExtraData(newMesh);
		return newMesh;
	}

	/// <summary>
	/// Allows meshdata object to be returned to the recycler
	/// </summary>
	public void Dispose()
	{
		DisposeExtraData();
		if (Recyclable)
		{
			Recyclable = false;
			Recycler.Recycle(this);
		}
		else
		{
			DisposeBasicData();
		}
	}

	/// <summary>
	/// Creates an empty copy of the mesh
	/// </summary>
	/// <returns></returns>
	public MeshData EmptyClone()
	{
		MeshData dest = new MeshData(initialiseArrays: false);
		dest.VerticesPerFace = VerticesPerFace;
		dest.IndicesPerFace = IndicesPerFace;
		dest.xyz = new float[XyzCount];
		if (Normals != null)
		{
			dest.Normals = new int[Normals.Length];
		}
		if (XyzFaces != null)
		{
			dest.XyzFaces = new byte[XyzFaces.Length];
		}
		if (TextureIndices != null)
		{
			dest.TextureIndices = new byte[TextureIndices.Length];
		}
		if (ClimateColorMapIds != null)
		{
			dest.ClimateColorMapIds = new byte[ClimateColorMapIds.Length];
		}
		if (SeasonColorMapIds != null)
		{
			dest.SeasonColorMapIds = new byte[SeasonColorMapIds.Length];
		}
		if (RenderPassesAndExtraBits != null)
		{
			dest.RenderPassesAndExtraBits = new short[RenderPassesAndExtraBits.Length];
		}
		if (Uv != null)
		{
			dest.Uv = new float[UvCount];
		}
		if (Rgba != null)
		{
			dest.Rgba = new byte[RgbaCount];
		}
		if (Flags != null)
		{
			dest.Flags = new int[FlagsCount];
		}
		dest.Indices = new int[GetIndicesCount()];
		if (CustomFloats != null)
		{
			dest.CustomFloats = CustomFloats.EmptyClone();
		}
		if (CustomShorts != null)
		{
			dest.CustomShorts = CustomShorts.EmptyClone();
		}
		if (CustomBytes != null)
		{
			dest.CustomBytes = CustomBytes.EmptyClone();
		}
		if (CustomInts != null)
		{
			dest.CustomInts = CustomInts.EmptyClone();
		}
		dest.VerticesMax = XyzCount / 3;
		dest.IndicesMax = dest.Indices.Length;
		return dest;
	}

	/// <summary>
	/// Sets the counts of all data to 0
	/// </summary>
	public MeshData Clear()
	{
		IndicesCount = 0;
		VerticesCount = 0;
		ColorMapIdsCount = 0;
		RenderPassCount = 0;
		XyzFacesCount = 0;
		NormalsCount = 0;
		TextureIndicesCount = 0;
		if (CustomBytes != null)
		{
			CustomBytes.Count = 0;
		}
		if (CustomFloats != null)
		{
			CustomFloats.Count = 0;
		}
		if (CustomShorts != null)
		{
			CustomShorts.Count = 0;
		}
		if (CustomInts != null)
		{
			CustomInts.Count = 0;
		}
		return this;
	}

	public int SizeInBytes()
	{
		return ((xyz != null) ? (xyz.Length * 4) : 0) + ((Indices != null) ? (Indices.Length * 4) : 0) + ((Rgba != null) ? Rgba.Length : 0) + ((ClimateColorMapIds != null) ? ClimateColorMapIds.Length : 0) + ((SeasonColorMapIds != null) ? SeasonColorMapIds.Length : 0) + ((XyzFaces != null) ? XyzFaces.Length : 0) + ((RenderPassesAndExtraBits != null) ? (RenderPassesAndExtraBits.Length * 2) : 0) + ((Normals != null) ? (Normals.Length * 4) : 0) + ((Flags != null) ? (Flags.Length * 4) : 0) + ((Uv != null) ? (Uv.Length * 4) : 0) + ((CustomBytes?.Values != null) ? CustomBytes.Values.Length : 0) + ((CustomFloats?.Values != null) ? (CustomFloats.Values.Length * 4) : 0) + ((CustomShorts?.Values != null) ? (CustomShorts.Values.Length * 2) : 0) + ((CustomInts?.Values != null) ? (CustomInts.Values.Length * 4) : 0);
	}

	/// <summary>
	/// Returns a copy of this mesh with the uvs set to the specified TextureAtlasPosition
	/// </summary>
	public MeshData WithTexPos(TextureAtlasPosition texPos)
	{
		MeshData meshData = Clone();
		meshData.SetTexPos(texPos);
		return meshData;
	}

	/// <summary>
	/// Sets the uvs of this mesh to the specified TextureAtlasPosition, assuming the initial UVs range from 0..1, as they will be scaled by the texPos
	/// </summary>
	public void SetTexPos(TextureAtlasPosition texPos)
	{
		float wdt = texPos.x2 - texPos.x1;
		float hgt = texPos.y2 - texPos.y1;
		for (int j = 0; j < Uv.Length; j++)
		{
			Uv[j] = ((j % 2 == 0) ? (Uv[j] * wdt + texPos.x1) : (Uv[j] * hgt + texPos.y1));
		}
		byte texIndex = getTextureIndex(texPos.atlasTextureId);
		for (int i = 0; i < TextureIndices.Length; i++)
		{
			TextureIndices[i] = texIndex;
		}
	}

	public MeshData[] SplitByTextureId()
	{
		MeshData[] meshes = new MeshData[TextureIds.Length];
		int i;
		for (i = 0; i < meshes.Length; i++)
		{
			MeshData obj = (meshes[i] = EmptyClone());
			obj.AddMeshData(this, (int faceindex) => TextureIndices[faceindex] == i);
			obj.CompactBuffers();
		}
		return meshes;
	}
}
