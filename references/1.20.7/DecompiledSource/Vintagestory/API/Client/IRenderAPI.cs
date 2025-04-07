#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

//
// Summary:
//     The main api component to assist you in rendering pretty stuff onto the screen
public interface IRenderAPI
{
    WireframeModes WireframeDebugRender { get; }

    PerceptionEffects PerceptionEffects { get; }

    Stack<ElementBounds> ScissorStack { get; }

    int TextureSize { get; }

    FrustumCulling DefaultFrustumCuller { get; }

    //
    // Summary:
    //     List of all loaded frame buffers. To get the god rays frame buffer for exampple,
    //     do
    //
    //     Framebuffers[(int)EnumFrameBuffer.GodRays]
    List<FrameBufferRef> FrameBuffers { get; }

    //
    // Summary:
    //     Set the current framebuffer
    FrameBufferRef FrameBuffer { set; }

    //
    // Summary:
    //     A number of default shader uniforms
    DefaultShaderUniforms ShaderUniforms { get; }

    //
    // Summary:
    //     Can be used to offset the position of the player camera
    ModelTransform CameraOffset { get; }

    //
    // Summary:
    //     The render stage the engine is currently at
    EnumRenderStage CurrentRenderStage { get; }

    //
    // Summary:
    //     The default view matrix used during perspective rendering. Is refreshed before
    //     EnumRenderStage.Opaque. Useful for doing projections in the Ortho stage via MatrixToolsd.Project()
    double[] PerspectiveViewMat { get; }

    //
    // Summary:
    //     The default projection matrix used during perspective rendering. Is refreshed
    //     before EnumRenderStage.Opaque. Useful for doing projections in the Ortho stage
    //     via MatrixToolsd.Project()
    double[] PerspectiveProjectionMat { get; }

    //
    // Summary:
    //     The name of the font used during this render (if it exists).
    [Obsolete("Please use ElementGeometrics.DecorativeFontName instead")]
    string DecorativeFontName { get; }

    //
    // Summary:
    //     The standard font used during this render (if it exists).
    [Obsolete("Please use ElementGeometrics.StandardFontName instead.")]
    string StandardFontName { get; }

    //
    // Summary:
    //     Width of the primary render framebuffer
    int FrameWidth { get; }

    //
    // Summary:
    //     Height of the primary render framebuffer
    int FrameHeight { get; }

    //
    // Summary:
    //     The camera type.
    EnumCameraMode CameraType { get; }

    //
    // Summary:
    //     True if when in IFP mode the camera would end up inside blocks
    bool CameraStuck { get; }

    //
    // Summary:
    //     The current modelview matrix stack
    StackMatrix4 MvMatrix { get; }

    //
    // Summary:
    //     The current projection matrix stack
    StackMatrix4 PMatrix { get; }

    float LineWidth { set; }

    //
    // Summary:
    //     The current top most matrix in the model view matrix stack.
    float[] CurrentModelviewMatrix { get; }

    //
    // Summary:
    //     Player camera matrix with player positioned at 0,0,0. You can use this matrix
    //     instead of Vintagestory.API.Client.IRenderAPI.CurrentModelviewMatrix for high
    //     precision rendering.
    double[] CameraMatrixOrigin { get; }

    //
    // Summary:
    //     Player camera matrix with player positioned at 0,0,0. You can use this matrix
    //     instead of Vintagestory.API.Client.IRenderAPI.CurrentModelviewMatrix for high
    //     precision rendering.
    float[] CameraMatrixOriginf { get; }

    //
    // Summary:
    //     The current top most matrix in the projection matrix stack
    float[] CurrentProjectionMatrix { get; }

    //
    // Summary:
    //     The current projection matrix for shadow rendering (renders the scene from the
    //     viewpoint of the sun)
    float[] CurrentShadowProjectionMatrix { get; }

    //
    // Summary:
    //     Gives you a reference to the "standard" shader, a general purpose shader for
    //     normal shading work
    IStandardShaderProgram StandardShader { get; }

    //
    // Summary:
    //     Gives you a reference to the currently active shader, or null if none is active
    //     right now
    IShaderProgram CurrentActiveShader { get; }

    //
    // Summary:
    //     The current ambient color (e.g. will return a blue tint when player is under
    //     water)
    Vec3f AmbientColor { get; }

    //
    // Summary:
    //     The current fog color (e.g. will return a blue tint when player is under water)
    Vec4f FogColor { get; }

    //
    // Summary:
    //     Current minimum fog value
    float FogMin { get; }

    //
    // Summary:
    //     Density of the current fog. Fog is calculated as followed in the shaders: clamp(fogMin
    //     + 1 - 1 / exp(gl_FragDepth * fogDensity), 0, 1)
    float FogDensity { get; }

    //
    // Summary:
    //     Returns you a render info object of given item stack. Can be used to render held
    //     items onto a creature.
    //
    // Parameters:
    //   inSlot:
    //
    //   ground:
    //
    //   dt:
    ItemRenderInfo GetItemStackRenderInfo(ItemSlot inSlot, EnumItemRenderTarget ground, float dt);

    void Reset3DProjection();

    void Set3DProjection(float zfar, float fov);

    //
    // Summary:
    //     Returns null if no OpenGL Error happened, otherwise one of the official opengl
    //     error codes
    string GlGetError();

    //
    // Summary:
    //     If opengl debug mode is enabled and an opengl error is found this method will
    //     throw an exception. It is recommended to use this methods in a few spots during
    //     render code to track down rendering issues in time.
    //
    // Parameters:
    //   message:
    void CheckGlError(string message = "");

    //
    // Summary:
    //     The current model view.
    void GlMatrixModeModelView();

    //
    // Summary:
    //     Pushes a copy of the current matrix onto the games matrix stack
    void GlPushMatrix();

    //
    // Summary:
    //     Pops the top most matrix from the games matrix stack
    void GlPopMatrix();

    //
    // Summary:
    //     Replaces the top most matrix with given one
    //
    // Parameters:
    //   matrix:
    void GlLoadMatrix(double[] matrix);

    //
    // Summary:
    //     Translates top most matrix in the games matrix stack
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    void GlTranslate(float x, float y, float z);

    //
    // Summary:
    //     Translates top most matrix in the games matrix stack
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    void GlTranslate(double x, double y, double z);

    //
    // Summary:
    //     Scales top most matrix in the games matrix stack
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    void GlScale(float x, float y, float z);

    //
    // Summary:
    //     Rotates top most matrix in the games matrix stack
    //
    // Parameters:
    //   angle:
    //
    //   x:
    //
    //   y:
    //
    //   z:
    void GlRotate(float angle, float x, float y, float z);

    //
    // Summary:
    //     Enables the Culling faces.
    void GlEnableCullFace();

    //
    // Summary:
    //     Disables the culling faces.
    void GlDisableCullFace();

    //
    // Summary:
    //     Enables the Depth Test.
    void GLEnableDepthTest();

    //
    // Summary:
    //     Disables the Depth Test.
    void GLDisableDepthTest();

    void GlViewport(int x, int y, int width, int height);

    //
    // Summary:
    //     Toggle writing to the depth buffer
    //
    // Parameters:
    //   on:
    void GLDepthMask(bool on);

    //
    // Summary:
    //     Regenerates the mip maps for the currently bound texture
    void GlGenerateTex2DMipmaps();

    //
    // Summary:
    //     To enable/disable various blending modes
    //
    // Parameters:
    //   blend:
    //
    //   blendMode:
    void GlToggleBlend(bool blend, EnumBlendMode blendMode = EnumBlendMode.Standard);

    //
    // Summary:
    //     Convenience method for GlScissor(). Tells the graphics card to not render anything
    //     outside supplied bounds. Can be turned of again with PopScissor(). Any previously
    //     applied scissor will be restored after calling PopScissor().
    //
    // Parameters:
    //   bounds:
    //
    //   stacking:
    //     If true, also applies scissoring from the previous call to PushScissor, otherwise
    //     replaces the scissor bounds
    void PushScissor(ElementBounds bounds, bool stacking = false);

    //
    // Summary:
    //     End scissor mode. Disable any previously set render constraints
    void PopScissor();

    //
    // Summary:
    //     Tells the graphics card to not render anything outside supplied bounds. Only
    //     sets the boundaries. Can be turned on/off with GlScissorFlag(true/false)
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   width:
    //
    //   height:
    void GlScissor(int x, int y, int width, int height);

    //
    // Summary:
    //     Whether scissor mode should be active or not
    //
    // Parameters:
    //   enable:
    void GlScissorFlag(bool enable);

    //
    // Summary:
    //     Creates a bitmap from a given PNG.
    //
    // Parameters:
    //   pngdata:
    //     the PNG data passed in.
    //
    // Returns:
    //     A bitmap object.
    BitmapExternal BitmapCreateFromPng(byte[] pngdata);

    //
    // Summary:
    //     Loads texture from Pixels in BGRA format.
    //
    // Parameters:
    //   bgraPixels:
    //     The pixel array
    //
    //   width:
    //     the width of the final texture
    //
    //   height:
    //     the height of the final texture
    //
    //   linearMag:
    //     Enable/Disable Linear rendering or use Nearest rendering.
    //
    //   clampMode:
    //     The current clamp mode
    //
    // Returns:
    //     The GLID for the resulting texture.
    [Obsolete("Use LoadOrUpdateTextureFromBgra(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture); instead. This method cannot warn you of memory leaks when the texture is not properly disposed.")]
    int LoadTextureFromBgra(int[] bgraPixels, int width, int height, bool linearMag, int clampMode);

    //
    // Summary:
    //     Loads texture from Pixels in RGBA format.
    //
    // Parameters:
    //   rgbaPixels:
    //     The pixel array
    //
    //   width:
    //     the width of the final texture
    //
    //   height:
    //     the height of the final texture
    //
    //   linearMag:
    //     Enable/Disable Linear rendering or use Nearest rendering.
    //
    //   clampMode:
    //     The current clamp mode
    //
    // Returns:
    //     The OpenGL Identifier for the resulting texture.
    [Obsolete("Use LoadOrUpdateTextureFromRgba(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture); instead. This method cannot warn you of memory leaks when the texture is not properly disposed.")]
    int LoadTextureFromRgba(int[] rgbaPixels, int width, int height, bool linearMag, int clampMode);

    //
    // Summary:
    //     Loads texture from Pixels in BGRA format.
    //
    // Parameters:
    //   bgraPixels:
    //     The pixel array
    //
    //   linearMag:
    //     Enable/Disable Linear rendering or use Nearest rendering.
    //
    //   clampMode:
    //     The current clamp mode
    //
    //   intoTexture:
    //     The target texture space it should load the pixels into. Must have width/height
    //     set accordingly. Will set the opengl textureid upon successful load
    void LoadOrUpdateTextureFromBgra(int[] bgraPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture);

    //
    // Summary:
    //     Loads texture from Pixels in RGBA format.
    //
    // Parameters:
    //   rgbaPixels:
    //     The pixel array
    //
    //   linearMag:
    //     Enable/Disable Linear rendering or use Nearest rendering.
    //
    //   clampMode:
    //     The current clamp mode
    //
    //   intoTexture:
    //     The target texture space it should load the pixels into. Must have width/height
    //     set accordingly. Will set the opengl textureid upon successful load.
    void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture);

    void LoadTexture(IBitmap bmp, ref LoadedTexture intoTexture, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false);

    //
    // Summary:
    //     Deletes given texture
    //
    // Parameters:
    //   textureId:
    //     the OpenGL Identifier for the target Texture.
    void GLDeleteTexture(int textureId);

    //
    // Summary:
    //     Max size a texture can have on the current graphics card
    //
    // Returns:
    //     The maximum size a texture can have on the current graphics card in Pixels.
    int GlGetMaxTextureSize();

    //
    // Summary:
    //     Binds given texture. For use with shaders - you should assign the texture directly
    //     though shader uniforms.
    //
    // Parameters:
    //   textureid:
    //     The OpenGL Identifier ID for the target texture to bind.
    void BindTexture2d(int textureid);

    //
    // Summary:
    //     Loads given texture through the assets managers and loads it onto the graphics
    //     card. Will return a cached version on every subsequent call to this method.
    //
    // Parameters:
    //   name:
    //     the location of the texture as it exists within the game or mod directory.
    //
    // Returns:
    //     The texture id
    int GetOrLoadTexture(AssetLocation name);

    //
    // Summary:
    //     Loads given texture through the assets managers and loads it onto the graphics
    //     card. Will return a cached version on every subsequent call to this method.
    //
    // Parameters:
    //   name:
    //     the location of the texture as it exists within the game or mod directory.
    //
    //   intoTexture:
    //     the texture object to be populated. If it already is populated it will be disposed
    //     first
    void GetOrLoadTexture(AssetLocation name, ref LoadedTexture intoTexture);

    //
    // Summary:
    //     Loads the texture supplied by the bitmap, uploads it to the graphics card and
    //     keeps a cached version under given name. Will return that cached version on every
    //     subsequent call to this method.
    //
    // Parameters:
    //   name:
    //     the location of the texture as it exists within the game or mod directory.
    //
    //   bmp:
    //     The referenced bitmap
    //
    //   intoTexture:
    //     the texture object to be populated. If it already is populated it will be disposed
    //     first
    void GetOrLoadTexture(AssetLocation name, BitmapRef bmp, ref LoadedTexture intoTexture);

    //
    // Summary:
    //     Removes given texture from the cache and from graphics card memory
    //
    // Parameters:
    //   name:
    //     the location of the texture as it exists within the game or mod directory.
    //
    // Returns:
    //     whether the operation was successful or not.
    bool RemoveTexture(AssetLocation name);

    //
    // Summary:
    //     Gets you the uniform location of given uniform for given shader
    //
    // Parameters:
    //   shaderProgramNumber:
    //
    //   name:
    int GetUniformLocation(int shaderProgramNumber, string name);

    //
    // Summary:
    //     Gives you access to all of the vanilla shaders
    //
    // Parameters:
    //   program:
    IShaderProgram GetEngineShader(EnumShaderProgram program);

    //
    // Summary:
    //     Gives you access to all currently registered shaders identified by their number
    //
    //
    // Parameters:
    //   shaderProgramNumber:
    IShaderProgram GetShader(int shaderProgramNumber);

    //
    // Summary:
    //     Populates the uniforms and light values for given positions and calls shader.Use().
    //
    //
    // Parameters:
    //   posX:
    //     The position for light level reading
    //
    //   posY:
    //     The position for light level reading
    //
    //   posZ:
    //     The position for light level reading
    //
    //   colorMul:
    IStandardShaderProgram PreparedStandardShader(int posX, int posY, int posZ, Vec4f colorMul = null);

    //
    // Summary:
    //     Allocates memory on the graphics card. Can use UpdateMesh() to populate it with
    //     data. The custom mesh data parts may be null. Sizes are in bytes.
    //
    // Parameters:
    //   xyzSize:
    //     the squared size of the texture.
    //
    //   normalSize:
    //     the size of the normals
    //
    //   uvSize:
    //     the size of the UV map.
    //
    //   rgbaSize:
    //     size of the RGBA colors.
    //
    //   flagsSize:
    //     Size of the render flags.
    //
    //   indicesSize:
    //     Size of the indices
    //
    //   customFloats:
    //     Float values of the mesh
    //
    //   customInts:
    //     Float values of the mesh
    //
    //   customShorts:
    //
    //   customBytes:
    //     Byte values of the mesh
    //
    //   drawMode:
    //     The current draw mode
    //
    //   staticDraw:
    //     whether the draw should be static or dynamic.
    //
    // Returns:
    //     the reference to the mesh
    MeshRef AllocateEmptyMesh(int xyzSize, int normalSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true);

    //
    // Summary:
    //     Uploads your mesh onto the graphics card for rendering (= load into a VAO).
    //
    //     If you use a custom shader, these are the VBO locations: xyz=0, uv=1, rgba=2,
    //     rgba2=3, flags=4, customFloats=5, customInts=6, customBytes=7 (indices do not
    //     get their own data location)
    //     If any of them are null, the vbo location is not consumed and all used location
    //     numbers shift by -1
    //
    // Parameters:
    //   data:
    MeshRef UploadMesh(MeshData data);

    //
    // Summary:
    //     Same as Vintagestory.API.Client.IRenderAPI.UploadMesh(Vintagestory.API.Client.MeshData)
    //     but splits it into multiple MeshRefs, one for each texture
    //
    // Parameters:
    //   data:
    MultiTextureMeshRef UploadMultiTextureMesh(MeshData data);

    //
    // Summary:
    //     Updates the existing mesh. Updates any non null data from updatedata
    //
    // Parameters:
    //   meshRef:
    //
    //   updatedata:
    void UpdateMesh(MeshRef meshRef, MeshData updatedata);

    //
    // Summary:
    //     Frees up the memory on the graphics card. Should always be called at the end
    //     of a meshes lifetime to prevent memory leaks. Equivalent to calling Dispose on
    //     the meshref itself
    //
    // Parameters:
    //   vao:
    void DeleteMesh(MeshRef vao);

    UBORef CreateUBO(IShaderProgram shaderProgram, int bindingPoint, string blockName, int size);

    //
    // Summary:
    //     Renders given mesh onto the screen
    //
    // Parameters:
    //   meshRef:
    void RenderMesh(MeshRef meshRef);

    //
    // Summary:
    //     Renders given mesh onto the screen, with the mesh requiring multiple render calls
    //     for each texture, asigns the associated texture each call
    //
    // Parameters:
    //   mmr:
    //
    //   textureSampleName:
    //
    //   textureNumber:
    void RenderMultiTextureMesh(MultiTextureMeshRef mmr, string textureSampleName, int textureNumber = 0);

    //
    // Summary:
    //     Uses the graphics instanced rendering methods to efficiently render the same
    //     mesh multiple times. Use the custom mesh data parts with instanced flag on to
    //     supply custom data to each mesh.
    //
    // Parameters:
    //   meshRef:
    //
    //   quantity:
    void RenderMeshInstanced(MeshRef meshRef, int quantity = 1);

    //
    // Summary:
    //     Draws only a part of the mesh
    //
    // Parameters:
    //   meshRef:
    //
    //   indicesStarts:
    //
    //   indicesSizes:
    //
    //   groupCount:
    void RenderMesh(MeshRef meshRef, int[] indicesStarts, int[] indicesSizes, int groupCount);

    //
    // Summary:
    //     Renders given texture into another texture. If you use the resulting texture
    //     for in-world rendering, remember to recreate the mipmaps via Vintagestory.API.Client.IRenderAPI.GlGenerateTex2DMipmaps
    //
    //
    // Parameters:
    //   fromTexture:
    //
    //   sourceX:
    //
    //   sourceY:
    //
    //   sourceWidth:
    //
    //   sourceHeight:
    //
    //   intoTexture:
    //
    //   targetX:
    //
    //   targetY:
    //
    //   alphaTest:
    //     If below given threshold, the pixel is not drawn into the target texture. (Default:
    //     0.05)
    void RenderTextureIntoTexture(LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, float targetY, float alphaTest = 0.005f);

    //
    // Summary:
    //     Renders given itemstack at given position (gui/orthographic mode)
    //
    // Parameters:
    //   itemstack:
    //
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    //
    //   size:
    //
    //   color:
    //     Set to Vintagestory.API.MathTools.ColorUtil.WhiteArgb for normal rendering
    //
    //   shading:
    //     Unused.
    //
    //   rotate:
    //     If true, will slowly rotate the itemstack around the Y-Axis
    //
    //   showStackSize:
    //     If true, will render a number depicting how many blocks/item are in the stack
    [Obsolete("Use RenderItemstackToGui(inSlot, ....) instead")]
    void RenderItemstackToGui(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true);

    //
    // Summary:
    //     Renders given itemstack in slot at given position (gui/orthographic mode)
    //
    // Parameters:
    //   inSlot:
    //
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    //
    //   size:
    //
    //   color:
    //
    //   shading:
    //
    //   rotate:
    //
    //   showStackSize:
    void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, bool shading = true, bool rotate = false, bool showStackSize = true);

    //
    // Summary:
    //     Renders given itemstack in slot at given position (gui/orthographic mode)
    //
    // Parameters:
    //   inSlot:
    //
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    //
    //   size:
    //
    //   color:
    //
    //   dt:
    //
    //   shading:
    //
    //   rotate:
    //
    //   showStackSize:
    void RenderItemstackToGui(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, bool shading = true, bool rotate = false, bool showStackSize = true);

    //
    // Summary:
    //     Renders given itemstack into supplied texture atlas. This is a rather costly
    //     operation. Also be sure to cache the results, as each call to this method consumes
    //     more space in your texture atlas. If you call this method outside the ortho render
    //     stage, it will enqueue a render task for next frame. Rather exceptionally, this
    //     method is also thread safe. If called from another thread, the render task always
    //     gets enqueued. The call back will always be run on the main thread.
    //
    // Parameters:
    //   stack:
    //
    //   atlas:
    //
    //   size:
    //
    //   onComplete:
    //     Once rendered, this returns a texture subid, which you can use to retrieve the
    //     textureAtlasPosition from the atlas
    //
    //   color:
    //
    //   sepiaLevel:
    //
    //   scale:
    //
    // Returns:
    //     True if the render could complete immediatly, false if it has to wait until the
    //     next ortho render stage
    bool RenderItemStackToAtlas(ItemStack stack, ITextureAtlasAPI atlas, int size, Action<int> onComplete, int color = -1, float sepiaLevel = 0f, float scale = 1f);

    //
    // Summary:
    //     Returns the first TextureAtlasPosition it can find for given block or item texture
    //     in itemstack.
    TextureAtlasPosition GetTextureAtlasPosition(ItemStack itemstack);

    //
    // Summary:
    //     Renders given entity at given position (gui/orthographic mode)
    //
    // Parameters:
    //   dt:
    //
    //   entity:
    //
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    //
    //   yawDelta:
    //     For rotating the entity around its y-axis
    //
    //   size:
    //
    //   color:
    void RenderEntityToGui(float dt, Entity entity, double posX, double posY, double posZ, float yawDelta, float size, int color);

    //
    // Summary:
    //     Renders given texture onto the screen, uses a simple quad for rendering (gui
    //     mode). Assumes the texture to use a premultiplied alpha channel
    //
    // Parameters:
    //   textureid:
    //
    //   posX:
    //
    //   posY:
    //
    //   width:
    //
    //   height:
    //
    //   z:
    //
    //   color:
    void Render2DTexturePremultipliedAlpha(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null);

    //
    // Summary:
    //     Renders given texture onto the screen, uses a simple quad for rendering (gui
    //     mode). Assumes the texture to use a premultiplied alpha channel
    //
    // Parameters:
    //   textureid:
    //
    //   posX:
    //
    //   posY:
    //
    //   width:
    //
    //   height:
    //
    //   z:
    //
    //   color:
    void Render2DTexturePremultipliedAlpha(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null);

    //
    // Summary:
    //     Renders given texture onto the screen, uses a simple quad for rendering (gui
    //     mode). Assumes the texture to use a premultiplied alpha channel
    //
    // Parameters:
    //   textureid:
    //
    //   bounds:
    //
    //   z:
    //
    //   color:
    void Render2DTexturePremultipliedAlpha(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null);

    //
    // Summary:
    //     Renders given texture onto the screen, uses a simple quad for rendering (gui
    //     mode)
    //
    // Parameters:
    //   textureid:
    //
    //   posX:
    //
    //   posY:
    //
    //   width:
    //
    //   height:
    //
    //   z:
    //
    //   color:
    void RenderTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null);

    //
    // Summary:
    //     Renders given texture onto the screen, uses a simple quad for rendering (gui
    //     mode)
    //
    // Parameters:
    //   textureid:
    //
    //   posX:
    //
    //   posY:
    //
    //   width:
    //
    //   height:
    //
    //   z:
    //
    //   color:
    void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null);

    //
    // Summary:
    //     Renders given texture onto the screen, uses supplied quad for rendering (gui
    //     mode)
    //
    // Parameters:
    //   quadModel:
    //
    //   textureid:
    //
    //   posX:
    //
    //   posY:
    //
    //   width:
    //
    //   height:
    //
    //   z:
    void Render2DTexture(MeshRef quadModel, int textureid, float posX, float posY, float width, float height, float z = 50f);

    //
    // Summary:
    //     Renders given texture onto the screen, uses supplied quad for rendering (gui
    //     mode)
    //
    // Parameters:
    //   quadModel:
    //
    //   textureid:
    //
    //   posX:
    //
    //   posY:
    //
    //   width:
    //
    //   height:
    //
    //   z:
    void Render2DTexture(MultiTextureMeshRef quadModel, float posX, float posY, float width, float height, float z = 50f);

    //
    // Summary:
    //     Renders given texture onto the screen, uses a simple quad for rendering (gui
    //     mode)
    //
    // Parameters:
    //   textureid:
    //
    //   bounds:
    //
    //   z:
    //
    //   color:
    void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null);

    //
    // Summary:
    //     Renders given texture onto the screen, uses a simple quad for rendering (gui
    //     mode)
    //
    // Parameters:
    //   textTexture:
    //
    //   posX:
    //
    //   posY:
    //
    //   z:
    void Render2DLoadedTexture(LoadedTexture textTexture, float posX, float posY, float z = 50f);

    //
    // Summary:
    //     Renders a rectangle outline at given position
    //
    // Parameters:
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    //
    //   width:
    //
    //   height:
    //
    //   color:
    void RenderRectangle(float posX, float posY, float posZ, float width, float height, int color);

    //
    // Summary:
    //     Inefficiently renders a line between 2 points
    //
    // Parameters:
    //   origin:
    //
    //   posX1:
    //
    //   posY1:
    //
    //   posZ1:
    //
    //   posX2:
    //
    //   posY2:
    //
    //   posZ2:
    //
    //   color:
    void RenderLine(BlockPos origin, float posX1, float posY1, float posZ1, float posX2, float posY2, float posZ2, int color);

    //
    // Summary:
    //     Adds a dynamic light source to the scene. Will not be rendered if the current
    //     point light count exceeds max dynamic lights in the graphics settings
    //
    // Parameters:
    //   pointlight:
    void AddPointLight(IPointLight pointlight);

    //
    // Summary:
    //     Removes a dynamic light source from the scene
    //
    // Parameters:
    //   pointlight:
    void RemovePointLight(IPointLight pointlight);
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
