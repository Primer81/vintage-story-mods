using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemRenderInsideBlock : ClientSystem
{
	protected Block[] insideBlocks;

	protected MeshRef[] meshRefs;

	protected int atlasTextureId;

	protected Matrixf ModelMat = new Matrixf();

	protected Vec3d[] testPositions;

	public int[] lightExt;

	public Block[] blockExt;

	private int extChunkSize;

	private BlockPos tmpPos = new BlockPos();

	public override string Name => "rib";

	public SystemRenderInsideBlock(ClientMain game)
		: base(game)
	{
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, Name, 0.45);
		testPositions = new Vec3d[18]
		{
			new Vec3d(0.0, 0.0, 0.0),
			new Vec3d(0.0, 1.0, 0.0),
			new Vec3d(0.0, 0.0, -1.0),
			new Vec3d(1.0, 0.0, -1.0),
			new Vec3d(1.0, 0.0, 0.0),
			new Vec3d(1.0, 0.0, 1.0),
			new Vec3d(0.0, 0.0, 1.0),
			new Vec3d(-1.0, 0.0, 1.0),
			new Vec3d(-1.0, 0.0, 0.0),
			new Vec3d(-1.0, 0.0, -1.0),
			new Vec3d(0.0, -1.0, -1.0),
			new Vec3d(1.0, -1.0, -1.0),
			new Vec3d(1.0, -1.0, 0.0),
			new Vec3d(1.0, -1.0, 1.0),
			new Vec3d(0.0, -1.0, 1.0),
			new Vec3d(-1.0, -1.0, 1.0),
			new Vec3d(-1.0, -1.0, 0.0),
			new Vec3d(-1.0, -1.0, -1.0)
		};
		insideBlocks = new Block[testPositions.Length];
		meshRefs = new MeshRef[testPositions.Length];
	}

	internal override void OnLevelFinalize()
	{
		base.OnLevelFinalize();
		extChunkSize = 34;
		lightExt = new int[extChunkSize * extChunkSize * extChunkSize];
		blockExt = new Block[extChunkSize * extChunkSize * extChunkSize];
	}

	private void OnRenderFrame3D(float dt)
	{
		EntityPlayer entity = game.EntityPlayer;
		if (entity == null || game.player.worlddata.CurrentGameMode == EnumGameMode.Creative || game.player.worlddata.CurrentGameMode == EnumGameMode.Spectator)
		{
			return;
		}
		Vec3d camPos = game.api.World.Player.Entity.CameraPos.Clone().Add(game.api.World.Player.Entity.LocalEyePos);
		game.MainCamera.ZNear = GameMath.Clamp(0.1f - (float)ClientSettings.FieldOfView / 90f / 25f, 0.025f, 0.1f);
		for (int i = 0; i < testPositions.Length; i++)
		{
			Vec3d obj = testPositions[i];
			double offx = obj.X * (double)game.MainCamera.ZNear * 1.5;
			double offy = obj.Y * (double)game.MainCamera.ZNear * 1.5;
			double offz = obj.Z * (double)game.MainCamera.ZNear * 1.5;
			tmpPos.Set((int)(camPos.X + offx), (int)(camPos.Y + offy), (int)(camPos.Z + offz));
			Block block = game.BlockAccessor.GetBlock(tmpPos);
			if (block != null && (block.SideOpaque[0] || block.SideOpaque[1] || block.SideOpaque[2] || block.SideOpaque[3] || block.SideOpaque[4] || block.SideOpaque[5]))
			{
				if (block != insideBlocks[i])
				{
					meshRefs[i]?.Dispose();
					MeshData mesh = game.api.TesselatorManager.GetDefaultBlockMesh(block);
					int lx = tmpPos.X % 32;
					int num = tmpPos.X % 32;
					int lz = tmpPos.X % 32;
					int extIndex3d = ((num + 1) * extChunkSize + (lz + 1)) * extChunkSize + (lx + 1);
					blockExt[extIndex3d] = block;
					blockExt[extIndex3d + TileSideEnum.MoveIndex[5]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y - 1, tmpPos.Z);
					blockExt[extIndex3d + TileSideEnum.MoveIndex[4]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y + 1, tmpPos.Z);
					blockExt[extIndex3d + TileSideEnum.MoveIndex[0]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y, tmpPos.Z - 1);
					blockExt[extIndex3d + TileSideEnum.MoveIndex[1]] = game.BlockAccessor.GetBlock(tmpPos.X + 1, tmpPos.Y, tmpPos.Z);
					blockExt[extIndex3d + TileSideEnum.MoveIndex[2]] = game.BlockAccessor.GetBlock(tmpPos.X, tmpPos.Y, tmpPos.Z + 1);
					blockExt[extIndex3d + TileSideEnum.MoveIndex[3]] = game.BlockAccessor.GetBlock(tmpPos.X - 1, tmpPos.Y - 1, tmpPos.Z);
					block.OnJsonTesselation(ref mesh, ref lightExt, tmpPos, blockExt, extIndex3d);
					meshRefs[i] = game.api.Render.UploadMesh(mesh);
					insideBlocks[i] = block;
					int textureSubId = block.FirstTextureInventory.Baked.TextureSubId;
					atlasTextureId = game.api.BlockTextureAtlas.Positions[textureSubId].atlasTextureId;
				}
				IRenderAPI rapi = game.api.Render;
				rapi.GlDisableCullFace();
				rapi.GlToggleBlend(blend: true);
				IStandardShaderProgram standardShaderProgram = rapi.PreparedStandardShader((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
				standardShaderProgram.Tex2D = atlasTextureId;
				standardShaderProgram.SsaoAttn = 1f;
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)(int)(camPos.X + offx) - camPos.X + entity.LocalEyePos.X, (double)(int)(camPos.Y + offy) - camPos.Y + entity.LocalEyePos.Y, (double)(int)(camPos.Z + offz) - camPos.Z + entity.LocalEyePos.Z).Scale(0.999f, 0.999f, 0.999f)
					.Values;
				standardShaderProgram.ExtraZOffset = -0.0001f;
				standardShaderProgram.ViewMatrix = rapi.CameraMatrixOriginf;
				standardShaderProgram.ProjectionMatrix = rapi.CurrentProjectionMatrix;
				rapi.RenderMesh(meshRefs[i]);
				standardShaderProgram.SsaoAttn = 0f;
				standardShaderProgram.Stop();
			}
		}
	}

	public override void Dispose(ClientMain game)
	{
		for (int i = 0; i < meshRefs.Length; i++)
		{
			meshRefs[i]?.Dispose();
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
