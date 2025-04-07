#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

//
// Summary:
//     A data structure that can be used to upload mesh information onto the graphics
//     card Please note, all arrays are used as a buffer. They do not tightly fit the
//     data but are always sized as a multiple of 2 from the initial size.
public class MeshData
{
    public delegate bool MeshDataFilterDelegate(int faceIndex);

    public static MeshDataRecycler Recycler;

    public const int StandardVerticesPerFace = 4;

    public const int StandardIndicesPerFace = 6;

    public const int BaseSizeInBytes = 34;

    public int[] TextureIds = new int[0];

    //
    // Summary:
    //     The x/y/z coordinates buffer. This should hold VerticesCount*3 values.
    public float[] xyz;

    //
    // Summary:
    //     The render flags buffer. This should hold VerticesCount*1 values.
    public int[] Flags;

    //
    // Summary:
    //     True if the flags array contains any wind mode
    public bool HasAnyWindModeSet;

    //
    // Summary:
    //     The normals buffer. This should hold VerticesCount*1 values. Currently unused
    //     by the engine. GL_INT_2_10_10_10_REV Format x: bits 0-9 (10 bit signed int) y:
    //     bits 10-19 (10 bit signed int) z: bits 20-29 (10 bit signed int) w: bits 30-31
    public int[] Normals;

    //
    // Summary:
    //     The uv buffer for texture coordinates. This should hold VerticesCount*2 values.
    public float[] Uv;

    //
    // Summary:
    //     The vertex color buffer. This should hold VerticesCount*4 values.
    public byte[] Rgba;

    //
    // Summary:
    //     The indices buffer. This should hold IndicesCount values.
    public int[] Indices;

    //
    // Summary:
    //     Texture index per face, references to and index in the TextureIds array
    public byte[] TextureIndices;

    //
    // Summary:
    //     Custom floats buffer. Can be used to upload arbitrary amounts of float values
    //     onto the graphics card
    public CustomMeshDataPartFloat CustomFloats;

    //
    // Summary:
    //     Custom ints buffer. Can be used to upload arbitrary amounts of int values onto
    //     the graphics card
    public CustomMeshDataPartInt CustomInts;

    //
    // Summary:
    //     Custom shorts buffer. Can be used to upload arbitrary amounts of short values
    //     onto the graphics card
    public CustomMeshDataPartShort CustomShorts;

    //
    // Summary:
    //     Custom bytes buffer. Can be used to upload arbitrary amounts of byte values onto
    //     the graphics card
    public CustomMeshDataPartByte CustomBytes;

    //
    // Summary:
    //     When using instanced rendering, set this flag to have the xyz values instanced.
    public bool XyzInstanced;

    //
    // Summary:
    //     When using instanced rendering, set this flag to have the uv values instanced.
    public bool UvInstanced;

    //
    // Summary:
    //     When using instanced rendering, set this flag to have the rgba values instanced.
    public bool RgbaInstanced;

    //
    // Summary:
    //     When using instanced rendering, set this flag to have the rgba2 values instanced.
    public bool Rgba2Instanced;

    //
    // Summary:
    //     When using instanced rendering, set this flag to have the indices instanced.
    public bool IndicesInstanced;

    //
    // Summary:
    //     When using instanced rendering, set this flag to have the flags instanced.
    public bool FlagsInstanced;

    //
    // Summary:
    //     xyz vbo usage hints for the graphics card. Recommended to be set to false when
    //     this section of data changes often.
    public bool XyzStatic = true;

    //
    // Summary:
    //     uv vbo usage hints for the graphics card. Recommended to be set to false when
    //     this section of data changes often.
    public bool UvStatic = true;

    //
    // Summary:
    //     rgab vbo usage hints for the graphics card. Recommended to be set to false when
    //     this section of data changes often.
    public bool RgbaStatic = true;

    //
    // Summary:
    //     rgba2 vbo usage hints for the graphics card. Recommended to be set to false when
    //     this section of data changes often.
    public bool Rgba2Static = true;

    //
    // Summary:
    //     indices vbo usage hints for the graphics card. Recommended to be set to false
    //     when this section of data changes often.
    public bool IndicesStatic = true;

    //
    // Summary:
    //     flags vbo usage hints for the graphics card. Recommended to be set to false when
    //     this section of data changes often.
    public bool FlagsStatic = true;

    //
    // Summary:
    //     For offseting the data in the VBO. This field is used when updating a mesh.
    public int XyzOffset;

    //
    // Summary:
    //     For offseting the data in the VBO. This field is used when updating a mesh.
    public int UvOffset;

    //
    // Summary:
    //     For offseting the data in the VBO. This field is used when updating a mesh.
    public int RgbaOffset;

    //
    // Summary:
    //     For offseting the data in the VBO. This field is used when updating a mesh.
    public int Rgba2Offset;

    //
    // Summary:
    //     For offseting the data in the VBO. This field is used when updating a mesh.
    public int FlagsOffset;

    //
    // Summary:
    //     For offseting the data in the VBO. This field is used when updating a mesh.
    public int NormalsOffset;

    //
    // Summary:
    //     For offseting the data in the VBO. This field is used when updating a mesh.
    public int IndicesOffset;

    //
    // Summary:
    //     The meshes draw mode
    public EnumDrawMode mode;

    //
    // Summary:
    //     Amount of currently assigned normals
    public int NormalsCount;

    //
    // Summary:
    //     Amount of currently assigned vertices
    public int VerticesCount;

    //
    // Summary:
    //     Amount of currently assigned indices
    public int IndicesCount;

    //
    // Summary:
    //     Vertex buffer size
    public int VerticesMax;

    //
    // Summary:
    //     Index buffer size
    public int IndicesMax;

    //
    // Summary:
    //     BlockShapeTesselator xyz faces. Required by TerrainChunkTesselator to determine
    //     vertex lightness. Should hold VerticesCount / 4 values. Set to 0 for no face,
    //     set to 1..8 for faces 0..7
    public byte[] XyzFaces;

    //
    // Summary:
    //     Amount of assigned xyz face values
    public int XyzFacesCount;

    public int TextureIndicesCount;

    public int IndicesPerFace = 6;

    public int VerticesPerFace = 4;

    //
    // Summary:
    //     BlockShapeTesselator climate colormap ids. Required by TerrainChunkTesselator
    //     to determine whether to color a vertex by a color map or not. Should hold VerticesCount
    //     / 4 values. Set to 0 for no color mapping, set 1..n for color map 0..n-1
    public byte[] ClimateColorMapIds;

    //
    // Summary:
    //     BlockShapeTesselator season colormap ids. Required by TerrainChunkTesselator
    //     to determine whether to color a vertex by a color map or not. Should hold VerticesCount
    //     / 4 values. Set to 0 for no color mapping, set 1..n for color map 0..n-1
    public byte[] SeasonColorMapIds;

    public bool[] FrostableBits;

    //
    // Summary:
    //     BlockShapeTesselator renderpass. Required by TerrainChunkTesselator to determine
    //     in which mesh data pool each quad should land in. Should hold VerticesCount /
    //     4 values.
    //     Lower 10 bits = render pass
    //     Upper 6 bits = extra bits for tesselators
    //     Bit 10: DisableRandomDrawOffset
    public short[] RenderPassesAndExtraBits;

    //
    // Summary:
    //     Amount of assigned tint values
    public int ColorMapIdsCount;

    //
    // Summary:
    //     Amount of assigned render pass values
    public int RenderPassCount;

    //
    // Summary:
    //     If true, this MeshData was constructed from MeshDataRecycler
    public bool Recyclable;

    //
    // Summary:
    //     The time this MeshData most recently entered the recycling system; the oldest
    //     may be garbage collected
    public long RecyclingTime;

    //
    // Summary:
    //     The size of the position values.
    public const int XyzSize = 12;

    //
    // Summary:
    //     The size of the normals.
    public const int NormalSize = 4;

    //
    // Summary:
    //     The size of the color.
    public const int RgbaSize = 4;

    //
    // Summary:
    //     The size of the Uv.
    public const int UvSize = 8;

    //
    // Summary:
    //     the size of the index.
    public const int IndexSize = 4;

    //
    // Summary:
    //     the size of the flags.
    public const int FlagsSize = 4;

    [Obsolete("Use RenderPassesAndExtraBits instead")]
    public short[] RenderPasses => RenderPassesAndExtraBits;

    //
    // Summary:
    //     returns VerticesCount * 3
    public int XyzCount => VerticesCount * 3;

    //
    // Summary:
    //     returns VerticesCount * 4
    public int RgbaCount => VerticesCount * 4;

    //
    // Summary:
    //     returns VerticesCount * 4
    public int Rgba2Count => VerticesCount * 4;

    //
    // Summary:
    //     returns VerticesCount
    public int FlagsCount => VerticesCount;

    //
    // Summary:
    //     returns VerticesCount * 2
    public int UvCount => VerticesCount * 2;

    //
    // Summary:
    //     Gets the number of verticies in the the mesh.
    //
    // Returns:
    //     The number of verticies in this mesh.
    //
    // Remarks:
    //     ..Shouldn't this be a property?
    public int GetVerticesCount()
    {
        return VerticesCount;
    }

    //
    // Summary:
    //     Sets the number of verticies in this mesh.
    //
    // Parameters:
    //   value:
    //     The number of verticies in this mesh
    //
    // Remarks:
    //     ..Shouldn't this be a property?
    public void SetVerticesCount(int value)
    {
        VerticesCount = value;
    }

    //
    // Summary:
    //     Gets the number of Indicices in this mesh.
    //
    // Returns:
    //     The number of indicies in the mesh.
    //
    // Remarks:
    //     ..Shouldn't this be a property?
    public int GetIndicesCount()
    {
        return IndicesCount;
    }

    //
    // Summary:
    //     Sets the number of indices in this mesh.
    //
    // Parameters:
    //   value:
    //     The number of indices in this mesh.
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

    //
    // Summary:
    //     Offset the mesh by given values
    //
    // Parameters:
    //   offset:
    public MeshData Translate(Vec3f offset)
    {
        Translate(offset.X, offset.Y, offset.Z);
        return this;
    }

    //
    // Summary:
    //     Offset the mesh by given values
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
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

    //
    // Summary:
    //     Rotate the mesh by given angles around given origin
    //
    // Parameters:
    //   origin:
    //
    //   radX:
    //
    //   radY:
    //
    //   radZ:
    public MeshData Rotate(Vec3f origin, float radX, float radY, float radZ)
    {
        Span<float> matrix = stackalloc float[16];
        Mat4f.RotateXYZ(matrix, radX, radY, radZ);
        return MatrixTransform(matrix, new float[4], origin);
    }

    //
    // Summary:
    //     Scale the mesh by given values around given origin
    //
    // Parameters:
    //   origin:
    //
    //   scaleX:
    //
    //   scaleY:
    //
    //   scaleZ:
    public MeshData Scale(Vec3f origin, float scaleX, float scaleY, float scaleZ)
    {
        for (int i = 0; i < VerticesCount; i++)
        {
            int num = i * 3;
            float num2 = xyz[num] - origin.X;
            float num3 = xyz[num + 1] - origin.Y;
            float num4 = xyz[num + 2] - origin.Z;
            xyz[num] = origin.X + scaleX * num2;
            xyz[num + 1] = origin.Y + scaleY * num3;
            xyz[num + 2] = origin.Z + scaleZ * num4;
        }

        return this;
    }

    //
    // Summary:
    //     Apply given transformation on the mesh
    //
    // Parameters:
    //   transform:
    public MeshData ModelTransform(ModelTransform transform)
    {
        float[] array = Mat4f.Create();
        float x = transform.Translation.X + transform.Origin.X;
        float y = transform.Translation.Y + transform.Origin.Y;
        float z = transform.Translation.Z + transform.Origin.Z;
        Mat4f.Translate(array, array, x, y, z);
        Mat4f.RotateX(array, array, transform.Rotation.X * (MathF.PI / 180f));
        Mat4f.RotateY(array, array, transform.Rotation.Y * (MathF.PI / 180f));
        Mat4f.RotateZ(array, array, transform.Rotation.Z * (MathF.PI / 180f));
        Mat4f.Scale(array, array, transform.ScaleXYZ.X, transform.ScaleXYZ.Y, transform.ScaleXYZ.Z);
        Mat4f.Translate(array, array, 0f - transform.Origin.X, 0f - transform.Origin.Y, 0f - transform.Origin.Z);
        MatrixTransform(array);
        return this;
    }

    //
    // Summary:
    //     Apply given transformation on the mesh
    //
    // Parameters:
    //   matrix:
    public MeshData MatrixTransform(float[] matrix)
    {
        return MatrixTransform(matrix, new float[4]);
    }

    //
    // Summary:
    //     Apply given transformation on the mesh - specifying two temporary vectors to
    //     work in (these can then be re-used for performance reasons)
    //
    // Parameters:
    //   matrix:
    //
    //   vec:
    //     a re-usable float[4], values unimportant
    //
    //   origin:
    //     origin point
    public MeshData MatrixTransform(float[] matrix, float[] vec, Vec3f origin = null)
    {
        return MatrixTransform((Span<float>)matrix, vec, origin);
    }

    public MeshData MatrixTransform(Span<float> matrix, float[] vec, Vec3f origin = null)
    {
        if (origin == null)
        {
            for (int i = 0; i < VerticesCount; i++)
            {
                Mat4f.MulWithVec3_Position(matrix, xyz, xyz, i * 3);
            }
        }
        else
        {
            for (int j = 0; j < VerticesCount; j++)
            {
                Mat4f.MulWithVec3_Position_WithOrigin(matrix, xyz, xyz, j * 3, origin);
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
            for (int l = 0; l < XyzFaces.Length; l++)
            {
                byte b = XyzFaces[l];
                if (b != 0)
                {
                    Vec3f normalf = BlockFacing.ALLFACES[b - 1].Normalf;
                    XyzFaces[l] = Mat4f.MulWithVec3_BlockFacing(matrix, normalf).MeshDataIndex;
                }
            }
        }

        if (Flags != null)
        {
            for (int m = 0; m < Flags.Length; m++)
            {
                VertexFlags.UnpackNormal(Flags[m], vec);
                Mat4f.MulWithVec3(matrix, vec, vec);
                float num = GameMath.RootSumOfSquares(vec[0], vec[1], vec[2]);
                Flags[m] = (Flags[m] & -33546241) | VertexFlags.PackNormal(vec[0] / num, vec[1] / num, vec[2] / num);
            }
        }

        return this;
    }

    //
    // Summary:
    //     Apply given transformation on the mesh
    //
    // Parameters:
    //   matrix:
    public MeshData MatrixTransform(double[] matrix)
    {
        if (Mat4d.IsTranslationOnly(matrix))
        {
            Translate((float)matrix[12], (float)matrix[13], (float)matrix[14]);
            return this;
        }

        double[] toFill = new double[4];
        for (int i = 0; i < VerticesCount; i++)
        {
            float num = xyz[i * 3];
            float num2 = xyz[i * 3 + 1];
            float num3 = xyz[i * 3 + 2];
            xyz[i * 3] = (float)(matrix[0] * (double)num + matrix[4] * (double)num2 + matrix[8] * (double)num3 + matrix[12]);
            xyz[i * 3 + 1] = (float)(matrix[1] * (double)num + matrix[5] * (double)num2 + matrix[9] * (double)num3 + matrix[13]);
            xyz[i * 3 + 2] = (float)(matrix[2] * (double)num + matrix[6] * (double)num2 + matrix[10] * (double)num3 + matrix[14]);
            if (Normals != null)
            {
                NormalUtil.FromPackedNormal(Normals[i], ref toFill);
                double[] normal = Mat4d.MulWithVec4(matrix, toFill);
                Normals[i] = NormalUtil.PackNormal(normal);
            }
        }

        if (XyzFaces != null)
        {
            for (int j = 0; j < XyzFaces.Length; j++)
            {
                byte b = XyzFaces[j];
                if (b != 0)
                {
                    Vec3d normald = BlockFacing.ALLFACES[b - 1].Normald;
                    double x = matrix[0] * normald.X + matrix[4] * normald.Y + matrix[8] * normald.Z;
                    double y = matrix[1] * normald.X + matrix[5] * normald.Y + matrix[9] * normald.Z;
                    double z = matrix[2] * normald.X + matrix[6] * normald.Y + matrix[10] * normald.Z;
                    BlockFacing blockFacing = BlockFacing.FromVector(x, y, z);
                    XyzFaces[j] = blockFacing.MeshDataIndex;
                }
            }
        }

        if (Flags != null)
        {
            for (int k = 0; k < Flags.Length; k++)
            {
                VertexFlags.UnpackNormal(Flags[k], toFill);
                double x2 = matrix[0] * toFill[0] + matrix[4] * toFill[1] + matrix[8] * toFill[2];
                double y2 = matrix[1] * toFill[0] + matrix[5] * toFill[1] + matrix[9] * toFill[2];
                double z2 = matrix[2] * toFill[0] + matrix[6] * toFill[1] + matrix[10] * toFill[2];
                Flags[k] = (Flags[k] & -33546241) | VertexFlags.PackNormal(x2, y2, z2);
            }
        }

        return this;
    }

    //
    // Summary:
    //     Creates a new mesh data instance with no components initialized.
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

    //
    // Summary:
    //     Creates a new mesh data instance with given components, but you can also freely
    //     nullify or set individual components after initialization Any component that
    //     is null is ignored by UploadModel/UpdateModel
    //
    // Parameters:
    //   capacityVertices:
    //
    //   capacityIndices:
    //
    //   withUv:
    //
    //   withNormals:
    //
    //   withRgba:
    //
    //   withFlags:
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

    //
    // Summary:
    //     This constructor creates a basic MeshData with xyz, Uv, Rgba, Flags and Indices
    //     only; Indices to Vertices ratio is the default 6:4
    //
    // Parameters:
    //   capacity:
    public MeshData(int capacity)
    {
        xyz = new float[capacity * 3];
        Uv = new float[capacity * 2];
        Rgba = new byte[capacity * 4];
        Flags = new int[capacity];
        VerticesMax = capacity;
        int num = capacity * 6 / 4;
        Indices = new int[num];
        IndicesMax = num;
    }

    //
    // Summary:
    //     Sets up the tints array for holding tint info
    public MeshData WithColorMaps()
    {
        SeasonColorMapIds = new byte[VerticesMax / 4];
        ClimateColorMapIds = new byte[VerticesMax / 4];
        return this;
    }

    //
    // Summary:
    //     Sets up the xyzfaces array for holding xyzfaces info
    public MeshData WithXyzFaces()
    {
        XyzFaces = new byte[VerticesMax / 4];
        return this;
    }

    //
    // Summary:
    //     Sets up the renderPasses array for holding render pass info
    public MeshData WithRenderpasses()
    {
        RenderPassesAndExtraBits = new short[VerticesMax / 4];
        return this;
    }

    //
    // Summary:
    //     Sets up the renderPasses array for holding render pass info
    public MeshData WithNormals()
    {
        Normals = new int[VerticesMax];
        return this;
    }

    //
    // Summary:
    //     Add supplied mesh data to this mesh. If a given dataset is not set, it is not
    //     copied from the sourceMesh. Automatically adjusts the indices for you. Is filtered
    //     to only add mesh data for given render pass. A negative render pass value defaults
    //     to EnumChunkRenderPass.Opaque
    //
    // Parameters:
    //   data:
    //
    //   filterByRenderPass:
    public void AddMeshData(MeshData data, EnumChunkRenderPass filterByRenderPass)
    {
        int renderPassInt = (int)filterByRenderPass;
        AddMeshData(data, (int i) => data.RenderPassesAndExtraBits[i] != renderPassInt && (data.RenderPassesAndExtraBits[i] != -1 || filterByRenderPass != EnumChunkRenderPass.Opaque));
    }

    public void AddMeshData(MeshData data, MeshDataFilterDelegate dele = null)
    {
        int num = 0;
        int verticesPerFace = VerticesPerFace;
        int indicesPerFace = IndicesPerFace;
        for (int i = 0; i < data.VerticesCount / verticesPerFace; i++)
        {
            if (dele != null && !dele(i))
            {
                num += indicesPerFace;
                continue;
            }

            int verticesCount = VerticesCount;
            if (Uv != null)
            {
                AddTextureId(data.TextureIds[data.TextureIndices[i]]);
            }

            for (int j = 0; j < verticesPerFace; j++)
            {
                int num2 = i * verticesPerFace + j;
                if (VerticesCount >= VerticesMax)
                {
                    GrowVertexBuffer();
                    GrowNormalsBuffer();
                }

                xyz[XyzCount] = data.xyz[num2 * 3];
                xyz[XyzCount + 1] = data.xyz[num2 * 3 + 1];
                xyz[XyzCount + 2] = data.xyz[num2 * 3 + 2];
                if (Normals != null)
                {
                    Normals[VerticesCount] = data.Normals[num2];
                }

                if (Uv != null)
                {
                    Uv[UvCount] = data.Uv[num2 * 2];
                    Uv[UvCount + 1] = data.Uv[num2 * 2 + 1];
                }

                if (Rgba != null)
                {
                    Rgba[RgbaCount] = data.Rgba[num2 * 4];
                    Rgba[RgbaCount + 1] = data.Rgba[num2 * 4 + 1];
                    Rgba[RgbaCount + 2] = data.Rgba[num2 * 4 + 2];
                    Rgba[RgbaCount + 3] = data.Rgba[num2 * 4 + 3];
                }

                if (Flags != null)
                {
                    Flags[FlagsCount] = data.Flags[num2];
                }

                if (CustomInts != null && data.CustomInts != null)
                {
                    int num3 = ((data.CustomInts.InterleaveStride == 0) ? data.CustomInts.InterleaveSizes[0] : data.CustomInts.InterleaveStride);
                    for (int k = 0; k < num3; k++)
                    {
                        CustomInts.Add(data.CustomInts.Values[num2 / num3 + k]);
                    }
                }

                if (CustomFloats != null && data.CustomFloats != null)
                {
                    int num4 = ((data.CustomFloats.InterleaveStride == 0) ? data.CustomFloats.InterleaveSizes[0] : data.CustomFloats.InterleaveStride);
                    for (int l = 0; l < num4; l++)
                    {
                        CustomFloats.Add(data.CustomFloats.Values[num2 / num4 + l]);
                    }
                }

                if (CustomShorts != null && data.CustomShorts != null)
                {
                    int num5 = ((data.CustomShorts.InterleaveStride == 0) ? data.CustomShorts.InterleaveSizes[0] : data.CustomShorts.InterleaveStride);
                    for (int m = 0; m < num5; m++)
                    {
                        CustomShorts.Add(data.CustomShorts.Values[num2 / num5 + m]);
                    }
                }

                if (CustomBytes != null && data.CustomBytes != null)
                {
                    int num6 = ((data.CustomBytes.InterleaveStride == 0) ? data.CustomBytes.InterleaveSizes[0] : data.CustomBytes.InterleaveStride);
                    for (int n = 0; n < num6; n++)
                    {
                        CustomBytes.Add(data.CustomBytes.Values[num2 / num6 + n]);
                    }
                }

                VerticesCount++;
            }

            for (int num7 = 0; num7 < indicesPerFace; num7++)
            {
                int num8 = i * indicesPerFace + num7;
                AddIndex(verticesCount - (i - num / indicesPerFace) * verticesPerFace + data.Indices[num8] - 2 * num / 3);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getTextureIndex(int textureId)
    {
        for (byte b = 0; b < TextureIds.Length; b++)
        {
            if (TextureIds[b] == textureId)
            {
                return b;
            }
        }

        TextureIds = TextureIds.Append(textureId);
        return (byte)(TextureIds.Length - 1);
    }

    //
    // Summary:
    //     Add supplied mesh data to this mesh. If a given dataset is not set, it is not
    //     copied from the sourceMesh. Automatically adjusts the indices for you.
    //
    // Parameters:
    //   sourceMesh:
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
        for (int i = 0; i < sourceMesh.XyzFacesCount; i++)
        {
            AddXyzFace(sourceMesh.XyzFaces[i]);
        }

        for (int j = 0; j < sourceMesh.TextureIndicesCount; j++)
        {
            AddTextureId(sourceMesh.TextureIds[sourceMesh.TextureIndices[j]]);
        }

        int num = ((IndicesCount > 0) ? ((mode == EnumDrawMode.Triangles) ? (Indices[IndicesCount - 1] + 1) : (Indices[IndicesCount - 2] + 1)) : 0);
        for (int k = 0; k < sourceMesh.IndicesCount; k++)
        {
            AddIndex(num + sourceMesh.Indices[k]);
        }

        for (int l = 0; l < sourceMesh.ColorMapIdsCount; l++)
        {
            AddColorMapIndex(sourceMesh.ClimateColorMapIds[l], sourceMesh.SeasonColorMapIds[l]);
        }

        for (int m = 0; m < sourceMesh.RenderPassCount; m++)
        {
            AddRenderPass(sourceMesh.RenderPassesAndExtraBits[m]);
        }

        if (CustomInts != null && sourceMesh.CustomInts != null)
        {
            for (int n = 0; n < sourceMesh.CustomInts.Count; n++)
            {
                CustomInts.Add(sourceMesh.CustomInts.Values[n]);
            }
        }

        if (CustomFloats != null && sourceMesh.CustomFloats != null)
        {
            for (int num2 = 0; num2 < sourceMesh.CustomFloats.Count; num2++)
            {
                CustomFloats.Add(sourceMesh.CustomFloats.Values[num2]);
            }
        }

        if (CustomShorts != null && sourceMesh.CustomShorts != null)
        {
            for (int num3 = 0; num3 < sourceMesh.CustomShorts.Count; num3++)
            {
                CustomShorts.Add(sourceMesh.CustomShorts.Values[num3]);
            }
        }

        if (CustomBytes != null && sourceMesh.CustomBytes != null)
        {
            for (int num4 = 0; num4 < sourceMesh.CustomBytes.Count; num4++)
            {
                CustomBytes.Add(sourceMesh.CustomBytes.Values[num4]);
            }
        }
    }

    //
    // Summary:
    //     Removes the last index in the indices array
    public void RemoveIndex()
    {
        if (IndicesCount > 0)
        {
            IndicesCount--;
        }
    }

    //
    // Summary:
    //     Removes the last vertex in the vertices array
    public void RemoveVertex()
    {
        if (VerticesCount > 0)
        {
            VerticesCount--;
        }
    }

    //
    // Summary:
    //     Removes the last "count" vertices from the vertex array
    //
    // Parameters:
    //   count:
    public void RemoveVertices(int count)
    {
        VerticesCount = Math.Max(0, VerticesCount - count);
    }

    //
    // Summary:
    //     Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   color:
    public unsafe void AddVertexSkipTex(float x, float y, float z, int color = -1)
    {
        int verticesCount = VerticesCount;
        if (verticesCount >= VerticesMax)
        {
            GrowVertexBuffer();
        }

        float[] array = xyz;
        int num = verticesCount * 3;
        array[num] = x;
        array[num + 1] = y;
        array[num + 2] = z;
        fixed (byte* ptr = Rgba)
        {
            int* ptr2 = (int*)ptr;
            ptr2[verticesCount] = color;
        }

        VerticesCount = verticesCount + 1;
    }

    //
    // Summary:
    //     Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   u:
    //
    //   v:
    public void AddVertex(float x, float y, float z, float u, float v)
    {
        int verticesCount = VerticesCount;
        if (verticesCount >= VerticesMax)
        {
            GrowVertexBuffer();
        }

        float[] array = xyz;
        float[] uv = Uv;
        int num = verticesCount * 3;
        array[num] = x;
        array[num + 1] = y;
        array[num + 2] = z;
        int num2 = verticesCount * 2;
        uv[num2] = u;
        uv[num2 + 1] = v;
        VerticesCount = verticesCount + 1;
    }

    //
    // Summary:
    //     Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   u:
    //
    //   v:
    //
    //   color:
    public void AddVertex(float x, float y, float z, float u, float v, int color)
    {
        AddWithFlagsVertex(x, y, z, u, v, color, 0);
    }

    //
    // Summary:
    //     Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   u:
    //
    //   v:
    //
    //   color:
    public void AddVertex(float x, float y, float z, float u, float v, byte[] color)
    {
        int verticesCount = VerticesCount;
        if (verticesCount >= VerticesMax)
        {
            GrowVertexBuffer();
        }

        float[] array = xyz;
        float[] uv = Uv;
        byte[] rgba = Rgba;
        int num = verticesCount * 3;
        array[num] = x;
        array[num + 1] = y;
        array[num + 2] = z;
        int num2 = verticesCount * 2;
        uv[num2] = u;
        uv[num2 + 1] = v;
        int num3 = verticesCount * 4;
        rgba[num3] = color[0];
        rgba[num3 + 1] = color[1];
        rgba[num3 + 2] = color[2];
        rgba[num3 + 3] = color[3];
        VerticesCount = verticesCount + 1;
    }

    //
    // Summary:
    //     Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   u:
    //
    //   v:
    //
    //   color:
    //
    //   flags:
    public unsafe void AddWithFlagsVertex(float x, float y, float z, float u, float v, int color, int flags)
    {
        int verticesCount = VerticesCount;
        if (verticesCount >= VerticesMax)
        {
            GrowVertexBuffer();
        }

        float[] array = xyz;
        float[] uv = Uv;
        int num = verticesCount * 3;
        array[num] = x;
        array[num + 1] = y;
        array[num + 2] = z;
        int num2 = verticesCount * 2;
        uv[num2] = u;
        uv[num2 + 1] = v;
        if (Flags != null)
        {
            Flags[verticesCount] = flags;
        }

        fixed (byte* ptr = Rgba)
        {
            int* ptr2 = (int*)ptr;
            ptr2[verticesCount] = color;
        }

        VerticesCount = verticesCount + 1;
    }

    //
    // Summary:
    //     Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   u:
    //
    //   v:
    //
    //   color:
    //
    //   flags:
    public unsafe void AddVertexWithFlags(float x, float y, float z, float u, float v, int color, int flags)
    {
        AddVertexWithFlagsSkipColor(x, y, z, u, v, flags);
        fixed (byte* ptr = Rgba)
        {
            int* ptr2 = (int*)ptr;
            ptr2[VerticesCount - 1] = color;
        }
    }

    //
    // Summary:
    //     Adds a new vertex to the mesh. Grows the vertex buffer if necessary.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   u:
    //
    //   v:
    //
    //   flags:
    public void AddVertexWithFlagsSkipColor(float x, float y, float z, float u, float v, int flags)
    {
        int verticesCount = VerticesCount;
        if (verticesCount >= VerticesMax)
        {
            GrowVertexBuffer();
        }

        float[] array = xyz;
        float[] uv = Uv;
        int num = verticesCount * 3;
        array[num] = x;
        array[num + 1] = y;
        array[num + 2] = z;
        int num2 = verticesCount * 2;
        uv[num2] = u;
        uv[num2 + 1] = v;
        if (Flags != null)
        {
            Flags[verticesCount] = flags;
        }

        VerticesCount = verticesCount + 1;
    }

    //
    // Summary:
    //     Applies a vertex flag to an existing MeshData (uses binary OR)
    public void SetVertexFlags(int flag)
    {
        if (Flags != null)
        {
            int verticesCount = VerticesCount;
            for (int i = 0; i < verticesCount; i++)
            {
                Flags[i] |= flag;
            }
        }
    }

    //
    // Summary:
    //     Adds a new normal to the mesh. Grows the normal buffer if necessary.
    //
    // Parameters:
    //   normalizedX:
    //
    //   normalizedY:
    //
    //   normalizedZ:
    public void AddNormal(float normalizedX, float normalizedY, float normalizedZ)
    {
        if (NormalsCount >= Normals.Length)
        {
            GrowNormalsBuffer();
        }

        Normals[NormalsCount++] = NormalUtil.PackNormal(normalizedX, normalizedY, normalizedZ);
    }

    //
    // Summary:
    //     Adds a new normal to the mesh. Grows the normal buffer if necessary.
    //
    // Parameters:
    //   facing:
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

    //
    // Summary:
    //     Add 6 indices
    //
    // Parameters:
    //   i1:
    //
    //   i2:
    //
    //   i3:
    //
    //   i4:
    //
    //   i5:
    //
    //   i6:
    public void AddIndices(int i1, int i2, int i3, int i4, int i5, int i6)
    {
        int indicesCount = IndicesCount;
        if (indicesCount + 6 > IndicesMax)
        {
            GrowIndexBuffer(6);
        }

        int[] indices = Indices;
        indices[indicesCount++] = i1;
        indices[indicesCount++] = i2;
        indices[indicesCount++] = i3;
        indices[indicesCount++] = i4;
        indices[indicesCount++] = i5;
        indices[indicesCount++] = i6;
        IndicesCount = indicesCount;
    }

    public void AddIndices(int[] indices)
    {
        int num = indices.Length;
        int indicesCount = IndicesCount;
        if (indicesCount + num > IndicesMax)
        {
            GrowIndexBuffer(num);
        }

        int[] indices2 = Indices;
        for (int i = 0; i < num; i++)
        {
            indices2[indicesCount++] = indices[i];
        }

        IndicesCount = indicesCount;
    }

    public void GrowIndexBuffer()
    {
        int num = IndicesCount;
        int[] array = new int[IndicesMax = num * 2];
        int[] indices = Indices;
        while (--num >= 0)
        {
            array[num] = indices[num];
        }

        Indices = array;
    }

    public void GrowIndexBuffer(int byAtLeastQuantity)
    {
        int indicesMax = Math.Max(IndicesCount * 2, IndicesCount + byAtLeastQuantity);
        int[] array = new int[IndicesMax = indicesMax];
        int[] indices = Indices;
        int num = IndicesCount;
        while (--num >= 0)
        {
            array[num] = indices[num];
        }

        Indices = array;
    }

    public void GrowNormalsBuffer()
    {
        if (Normals != null)
        {
            int num = Normals.Length;
            int[] array = new int[num * 2];
            int[] normals = Normals;
            while (--num >= 0)
            {
                array[num] = normals[num];
            }

            Normals = array;
        }
    }

    //
    // Summary:
    //     Doubles the size of the xyz, uv, rgba, rgba2 and flags arrays
    public void GrowVertexBuffer()
    {
        if (xyz != null)
        {
            float[] array = new float[XyzCount * 2];
            float[] array2 = xyz;
            int num = array2.Length;
            while (--num >= 0)
            {
                array[num] = array2[num];
            }

            xyz = array;
        }

        if (Uv != null)
        {
            float[] array3 = new float[UvCount * 2];
            float[] uv = Uv;
            int num2 = uv.Length;
            while (--num2 >= 0)
            {
                array3[num2] = uv[num2];
            }

            Uv = array3;
        }

        if (Rgba != null)
        {
            byte[] array4 = new byte[RgbaCount * 2];
            byte[] rgba = Rgba;
            int num3 = rgba.Length;
            while (--num3 >= 0)
            {
                array4[num3] = rgba[num3];
            }

            Rgba = array4;
        }

        if (Flags != null)
        {
            int[] array5 = new int[FlagsCount * 2];
            int[] flags = Flags;
            int num4 = flags.Length;
            while (--num4 >= 0)
            {
                array5[num4] = flags[num4];
            }

            Flags = array5;
        }

        VerticesMax *= 2;
    }

    //
    // Summary:
    //     Resizes all buffers to tightly fit the data. Recommended to run this method for
    //     long-term in-memory storage of meshdata for meshes that won't get any new vertices
    //     added
    public void CompactBuffers()
    {
        if (xyz != null)
        {
            int xyzCount = XyzCount;
            float[] destinationArray = new float[xyzCount + 1];
            Array.Copy(xyz, 0, destinationArray, 0, xyzCount);
            xyz = destinationArray;
        }

        if (Uv != null)
        {
            int uvCount = UvCount;
            float[] array = new float[uvCount + 1];
            Array.Copy(Uv, 0, array, 0, uvCount);
            Uv = array;
        }

        if (Rgba != null)
        {
            int rgbaCount = RgbaCount;
            byte[] array2 = new byte[rgbaCount + 1];
            Array.Copy(Rgba, 0, array2, 0, rgbaCount);
            Rgba = array2;
        }

        if (Flags != null)
        {
            int flagsCount = FlagsCount;
            int[] array3 = new int[flagsCount + 1];
            Array.Copy(Flags, 0, array3, 0, flagsCount);
            Flags = array3;
        }

        VerticesMax = VerticesCount;
    }

    //
    // Summary:
    //     Creates a compact, deep copy of the mesh
    public MeshData Clone()
    {
        MeshData meshData = CloneBasicData();
        CloneExtraData(meshData);
        return meshData;
    }

    //
    // Summary:
    //     Clone the basic xyz, uv, rgba and flags arrays, which are common to every chunk
    //     mesh (though not necessarily all used by individual block/item/entity models)
    private MeshData CloneBasicData()
    {
        MeshData meshData = new MeshData(initialiseArrays: false);
        meshData.VerticesPerFace = VerticesPerFace;
        meshData.IndicesPerFace = IndicesPerFace;
        meshData.SetVerticesCount(VerticesCount);
        meshData.xyz = xyz.FastCopy(XyzCount);
        if (Uv != null)
        {
            meshData.Uv = Uv.FastCopy(UvCount);
        }

        if (Rgba != null)
        {
            meshData.Rgba = Rgba.FastCopy(RgbaCount);
        }

        if (Flags != null)
        {
            meshData.Flags = Flags.FastCopy(FlagsCount);
        }

        meshData.Indices = Indices.FastCopy(IndicesCount);
        meshData.SetIndicesCount(IndicesCount);
        meshData.VerticesMax = VerticesCount;
        meshData.IndicesMax = meshData.Indices.Length;
        return meshData;
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

    //
    // Summary:
    //     Clone the extra mesh data fields. Some of these fields are used only by block/item
    //     meshes (some only be Microblocks). Some others are not used by all chunk meshes,
    //     though may be used by certain meshes in certain renderpasses (e.g. CustomInts).
    //     Either way, cannot sensibly be retained within the MeshDataRecycler system, must
    //     be cloned every time
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

        int num = Math.Max(VerticesCount, (IndicesCount + 6 - 1) / 6 * 4);
        if ((float)num > (float)VerticesCount * 1.05f || (float)(num * 6 / 4) > (float)IndicesCount * 1.2f)
        {
            return Clone();
        }

        MeshData orCreateMesh = Recycler.GetOrCreateMesh(num);
        CopyBasicData(orCreateMesh);
        CloneExtraData(orCreateMesh);
        return orCreateMesh;
    }

    //
    // Summary:
    //     Allows meshdata object to be returned to the recycler
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

    //
    // Summary:
    //     Creates an empty copy of the mesh
    public MeshData EmptyClone()
    {
        MeshData meshData = new MeshData(initialiseArrays: false);
        meshData.VerticesPerFace = VerticesPerFace;
        meshData.IndicesPerFace = IndicesPerFace;
        meshData.xyz = new float[XyzCount];
        if (Normals != null)
        {
            meshData.Normals = new int[Normals.Length];
        }

        if (XyzFaces != null)
        {
            meshData.XyzFaces = new byte[XyzFaces.Length];
        }

        if (TextureIndices != null)
        {
            meshData.TextureIndices = new byte[TextureIndices.Length];
        }

        if (ClimateColorMapIds != null)
        {
            meshData.ClimateColorMapIds = new byte[ClimateColorMapIds.Length];
        }

        if (SeasonColorMapIds != null)
        {
            meshData.SeasonColorMapIds = new byte[SeasonColorMapIds.Length];
        }

        if (RenderPassesAndExtraBits != null)
        {
            meshData.RenderPassesAndExtraBits = new short[RenderPassesAndExtraBits.Length];
        }

        if (Uv != null)
        {
            meshData.Uv = new float[UvCount];
        }

        if (Rgba != null)
        {
            meshData.Rgba = new byte[RgbaCount];
        }

        if (Flags != null)
        {
            meshData.Flags = new int[FlagsCount];
        }

        meshData.Indices = new int[GetIndicesCount()];
        if (CustomFloats != null)
        {
            meshData.CustomFloats = CustomFloats.EmptyClone();
        }

        if (CustomShorts != null)
        {
            meshData.CustomShorts = CustomShorts.EmptyClone();
        }

        if (CustomBytes != null)
        {
            meshData.CustomBytes = CustomBytes.EmptyClone();
        }

        if (CustomInts != null)
        {
            meshData.CustomInts = CustomInts.EmptyClone();
        }

        meshData.VerticesMax = XyzCount / 3;
        meshData.IndicesMax = meshData.Indices.Length;
        return meshData;
    }

    //
    // Summary:
    //     Sets the counts of all data to 0
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

    //
    // Summary:
    //     Returns a copy of this mesh with the uvs set to the specified TextureAtlasPosition
    public MeshData WithTexPos(TextureAtlasPosition texPos)
    {
        MeshData meshData = Clone();
        meshData.SetTexPos(texPos);
        return meshData;
    }

    //
    // Summary:
    //     Sets the uvs of this mesh to the specified TextureAtlasPosition, assuming the
    //     initial UVs range from 0..1, as they will be scaled by the texPos
    public void SetTexPos(TextureAtlasPosition texPos)
    {
        float num = texPos.x2 - texPos.x1;
        float num2 = texPos.y2 - texPos.y1;
        for (int i = 0; i < Uv.Length; i++)
        {
            Uv[i] = ((i % 2 == 0) ? (Uv[i] * num + texPos.x1) : (Uv[i] * num2 + texPos.y1));
        }

        byte textureIndex = getTextureIndex(texPos.atlasTextureId);
        for (int j = 0; j < TextureIndices.Length; j++)
        {
            TextureIndices[j] = textureIndex;
        }
    }

    public MeshData[] SplitByTextureId()
    {
        MeshData[] array = new MeshData[TextureIds.Length];
        int i;
        for (i = 0; i < array.Length; i++)
        {
            MeshData obj = (array[i] = EmptyClone());
            obj.AddMeshData(this, (int faceindex) => TextureIndices[faceindex] == i);
            obj.CompactBuffers();
        }

        return array;
    }
}
#if false // Decompilation log
'181' items in cache
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
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
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
