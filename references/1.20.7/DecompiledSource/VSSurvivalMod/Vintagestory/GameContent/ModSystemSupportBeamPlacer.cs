using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemSupportBeamPlacer : ModSystem, IRenderer, IDisposable
{
	protected Dictionary<string, MeshData[]> origBeamMeshes = new Dictionary<string, MeshData[]>();

	private Dictionary<string, BeamPlacerWorkSpace> workspaceByPlayer = new Dictionary<string, BeamPlacerWorkSpace>();

	private ICoreAPI api;

	private ICoreClientAPI capi;

	public Matrixf ModelMat = new Matrixf();

	public double RenderOrder => 0.5;

	public int RenderRange => 12;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "beamplacer");
	}

	public bool CancelPlace(BlockSupportBeam blockSupportBeam, EntityAgent byEntity)
	{
		BeamPlacerWorkSpace ws = getWorkSpace(byEntity);
		if (ws.nowBuilding)
		{
			ws.nowBuilding = false;
			ws.startOffset = null;
			ws.endOffset = null;
			return true;
		}
		return false;
	}

	public Vec3f snapToGrid(IVec3 pos, int gridSize)
	{
		return new Vec3f((float)(int)Math.Round(pos.XAsFloat * (float)gridSize) / (float)gridSize, (float)(int)Math.Round(pos.YAsFloat * (float)gridSize) / (float)gridSize, (float)(int)Math.Round(pos.ZAsFloat * (float)gridSize) / (float)gridSize);
	}

	public void OnInteract(Block block, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, bool partialEnds)
	{
		if (blockSel != null)
		{
			BeamPlacerWorkSpace ws = getWorkSpace(byEntity);
			if (!ws.nowBuilding)
			{
				beginPlace(ws, block, byEntity, blockSel, partialEnds);
			}
			else
			{
				completePlace(ws, byEntity, slot);
			}
		}
	}

	private void beginPlace(BeamPlacerWorkSpace ws, Block block, EntityAgent byEntity, BlockSelection blockSel, bool partialEnds)
	{
		ws.GridSize = (byEntity.Controls.CtrlKey ? 16 : 4);
		ws.currentMeshes = getOrCreateBeamMeshes(block, (block as BlockSupportBeam)?.PartialEnds ?? false);
		if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BEBehaviorSupportBeam>() == null)
		{
			Block startPosBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
			if (startPosBlock.Replaceable >= 6000)
			{
				ws.startPos = blockSel.Position.Copy();
				ws.startOffset = snapToGrid(blockSel.HitPosition, ws.GridSize);
			}
			else
			{
				BlockFacing blockSelFace = blockSel.Face;
				BlockPos wsStartPos = blockSel.Position.AddCopy(blockSelFace);
				startPosBlock = api.World.BlockAccessor.GetBlock(wsStartPos);
				if (api.World.BlockAccessor.GetBlockEntity(wsStartPos)?.GetBehavior<BEBehaviorSupportBeam>() == null && startPosBlock.Replaceable < 6000)
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "notplaceablehere", Lang.Get("Cannot place here, a block is in the way"));
					return;
				}
				ws.startPos = wsStartPos;
				ws.startOffset = snapToGrid(blockSel.HitPosition, ws.GridSize).Sub(blockSel.Face.Normali);
			}
		}
		else
		{
			ws.startPos = blockSel.Position.Copy();
			ws.startOffset = snapToGrid(blockSel.HitPosition, ws.GridSize);
		}
		ws.endOffset = null;
		ws.nowBuilding = true;
		ws.block = block;
		ws.onFacing = blockSel.Face;
	}

	private void completePlace(BeamPlacerWorkSpace ws, EntityAgent byEntity, ItemSlot slot)
	{
		ws.nowBuilding = false;
		BlockEntity be = api.World.BlockAccessor.GetBlockEntity(ws.startPos);
		BEBehaviorSupportBeam beh = be?.GetBehavior<BEBehaviorSupportBeam>();
		EntityPlayer eplr = byEntity as EntityPlayer;
		Vec3f nowEndOffset = getEndOffset(eplr.Player, ws);
		if (nowEndOffset.DistanceTo(ws.startOffset) < 0.01f)
		{
			return;
		}
		if (beh == null)
		{
			if (api.World.BlockAccessor.GetBlock(ws.startPos).Replaceable < 6000)
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "notplaceablehere", Lang.Get("Cannot place here, a block is in the way"));
				return;
			}
			IPlayer player = (byEntity as EntityPlayer)?.Player;
			if (!api.World.Claims.TryAccess(player, ws.startPos, EnumBlockAccessFlags.BuildOrBreak))
			{
				player.InventoryManager.ActiveHotbarSlot.MarkDirty();
				return;
			}
			if (eplr.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				int len2 = (int)Math.Ceiling(nowEndOffset.DistanceTo(ws.startOffset));
				if (slot.StackSize < len2)
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "notenoughitems", Lang.Get("You need {0} beams to place a beam at this lenth", len2));
					return;
				}
			}
			api.World.BlockAccessor.SetBlock(ws.block.Id, ws.startPos);
			be = api.World.BlockAccessor.GetBlockEntity(ws.startPos);
			beh = be?.GetBehavior<BEBehaviorSupportBeam>();
		}
		if (eplr.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			int len = (int)Math.Ceiling(nowEndOffset.DistanceTo(ws.startOffset));
			if (slot.StackSize < len)
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "notenoughitems", Lang.Get("You need {0} beams to place a beam at this lenth", len));
				return;
			}
			slot.TakeOut(len);
			slot.MarkDirty();
		}
		beh.AddBeam(ws.startOffset, nowEndOffset, ws.onFacing, ws.block);
		be.MarkDirty(redrawOnClient: true);
	}

	public MeshData[] getOrCreateBeamMeshes(Block block, bool partialEnds, ITexPositionSource texSource = null, string texSourceKey = null)
	{
		if (capi == null)
		{
			return null;
		}
		if (texSource != null)
		{
			capi.Tesselator.TesselateShape(texSourceKey, capi.TesselatorManager.GetCachedShape(block.Shape.Base), out var cmeshData, texSource, null, 0, 0, 0);
			return new MeshData[1] { cmeshData };
		}
		string key = string.Concat(block.Code, "-", partialEnds.ToString());
		if (!origBeamMeshes.TryGetValue(key, out var meshdatas))
		{
			if (partialEnds)
			{
				meshdatas = (origBeamMeshes[key] = new MeshData[4]);
				for (int i = 0; i < 4; i++)
				{
					AssetLocation loc = block.Shape.Base.Clone().WithFilename(((i + 1) * 4).ToString() ?? "");
					Shape shape = capi.Assets.Get(loc.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")).ToObject<Shape>();
					capi.Tesselator.TesselateShape(block, shape, out var meshData);
					meshdatas[i] = meshData;
				}
			}
			else
			{
				meshdatas = (origBeamMeshes[key] = new MeshData[1]);
				capi.Tesselator.TesselateShape(block, capi.TesselatorManager.GetCachedShape(block.Shape.Base), out var meshData2);
				meshdatas[0] = meshData2;
			}
		}
		return meshdatas;
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		BeamPlacerWorkSpace ws = getWorkSpace(capi.World.Player.PlayerUID);
		if (!ws.nowBuilding)
		{
			return;
		}
		Vec3f nowEndOffset = getEndOffset(capi.World.Player, ws);
		if (!((double)ws.startOffset.DistanceTo(nowEndOffset) < 0.1))
		{
			if (ws.endOffset != nowEndOffset)
			{
				ws.endOffset = nowEndOffset;
				reloadMeshRef();
			}
			if (ws.currentMeshRef != null)
			{
				IShaderProgram currentActiveShader = capi.Render.CurrentActiveShader;
				currentActiveShader?.Stop();
				IStandardShaderProgram standardShaderProgram = capi.Render.PreparedStandardShader(ws.startPos.X, ws.startPos.InternalY, ws.startPos.Z);
				Vec3d camPos = capi.World.Player.Entity.CameraPos;
				standardShaderProgram.Use();
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((double)ws.startPos.X - camPos.X, (double)ws.startPos.InternalY - camPos.Y, (double)ws.startPos.Z - camPos.Z).Values;
				standardShaderProgram.ViewMatrix = capi.Render.CameraMatrixOriginf;
				standardShaderProgram.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;
				capi.Render.RenderMultiTextureMesh(ws.currentMeshRef, "tex");
				standardShaderProgram.Stop();
				currentActiveShader?.Use();
			}
		}
	}

	protected Vec3f getEndOffset(IPlayer player, BeamPlacerWorkSpace ws)
	{
		Vec3d vec;
		if (player.CurrentBlockSelection != null)
		{
			BlockSelection blockSel = player.CurrentBlockSelection;
			vec = blockSel.Position.ToVec3d().Sub(ws.startPos).Add(blockSel.HitPosition);
		}
		else
		{
			vec = player.Entity.SidedPos.AheadCopy(2.0).XYZ.Add(player.Entity.LocalEyePos).Sub(ws.startPos);
		}
		Vec3f vec3f = snapToGrid(vec, ws.GridSize);
		double dX = vec3f.X - ws.startOffset.X;
		double dY = vec3f.Y - ws.startOffset.Y;
		double dZ = vec3f.Z - ws.startOffset.Z;
		double len = Math.Sqrt(dZ * dZ + dY * dY + dX * dX);
		double y = Math.Sqrt(dZ * dZ + dX * dX);
		float yaw = -(float)Math.PI / 2f - (float)Math.Atan2(0.0 - dX, 0.0 - dZ);
		float pitch = (float)Math.Atan2(y, dY);
		if (player.Entity.Controls.ShiftKey)
		{
			float rotSnap = 15f;
			yaw = (float)Math.Round(yaw * (180f / (float)Math.PI) / rotSnap) * rotSnap * ((float)Math.PI / 180f);
			pitch = (float)Math.Round(pitch * (180f / (float)Math.PI) / rotSnap) * rotSnap * ((float)Math.PI / 180f);
		}
		double cosYaw = Math.Cos(yaw);
		double sinYaw = Math.Sin(yaw);
		double cosPitch = Math.Cos(pitch);
		double sinPitch = Math.Sin(pitch);
		len = Math.Min(len, 20.0);
		return new Vec3f(ws.startOffset.X + (float)(len * sinPitch * cosYaw), ws.startOffset.Y + (float)(len * cosPitch), ws.startOffset.Z + (float)(len * sinPitch * sinYaw));
	}

	private void reloadMeshRef()
	{
		BeamPlacerWorkSpace ws = getWorkSpace(capi.World.Player.PlayerUID);
		ws.currentMeshRef?.Dispose();
		MeshData mesh = generateMesh(ws.startOffset, ws.endOffset, ws.onFacing, ws.currentMeshes, ws.block.Attributes?["slumpPerMeter"].AsFloat() ?? 0f);
		ws.currentMeshRef = capi.Render.UploadMultiTextureMesh(mesh);
	}

	public static MeshData generateMesh(Vec3f start, Vec3f end, BlockFacing facing, MeshData[] origMeshes, float slumpPerMeter)
	{
		MeshData outMesh = new MeshData(4, 6).WithRenderpasses().WithXyzFaces().WithColorMaps();
		float[] mat = new float[16];
		double dX = end.X - start.X;
		double dY = end.Y - start.Y;
		double dZ = end.Z - start.Z;
		double len = Math.Sqrt(dZ * dZ + dY * dY + dX * dX);
		double y = Math.Sqrt(dZ * dZ + dX * dX);
		double normalize = 1.0 / Math.Max(1.0, len);
		Vec3f dir = new Vec3f((float)(dX * normalize), (float)(dY * normalize), (float)(dZ * normalize));
		float yaw = (float)Math.Atan2(0.0 - dX, 0.0 - dZ) + (float)Math.PI / 2f;
		float basepitch = (float)Math.Atan2(y, 0.0 - dY) + (float)Math.PI / 2f;
		float val = Math.Abs((float)(Math.Sin(yaw) * Math.Cos(yaw)));
		float pitchExtend = Math.Abs((float)(Math.Sin(basepitch) * Math.Cos(basepitch)));
		float distTo45Deg = Math.Max(val, pitchExtend);
		float extend = 0.0625f * distTo45Deg * 4f;
		float slump = 0f;
		len += (double)extend;
		for (float r = 0f - extend; (double)r < len; r += 1f)
		{
			double sectionLen = Math.Min(1.0, len - (double)r);
			if (!(sectionLen < 0.01))
			{
				Vec3f sectionStart = start + r * dir;
				float distance = (float)((double)r - len / 2.0);
				float pitch = basepitch + distance * slumpPerMeter;
				slump += (float)Math.Sin(distance * slumpPerMeter);
				if (origMeshes.Length > 1 && len < 1.125)
				{
					sectionLen = len;
					r += 1f;
				}
				int index = GameMath.Clamp((int)Math.Round((sectionLen - 0.25) * (double)origMeshes.Length), 0, origMeshes.Length - 1);
				float modelLen = (float)(index + 1) / 4f;
				float xscale = ((origMeshes.Length == 1) ? ((float)sectionLen) : ((float)sectionLen / modelLen));
				Mat4f.Identity(mat);
				Mat4f.Translate(mat, mat, sectionStart.X, sectionStart.Y + slump, sectionStart.Z);
				Mat4f.RotateY(mat, mat, yaw);
				Mat4f.RotateZ(mat, mat, pitch);
				Mat4f.Scale(mat, mat, new float[3] { xscale, 1f, 1f });
				Mat4f.Translate(mat, mat, -1f, -0.125f, -0.5f);
				MeshData mesh = origMeshes[index].Clone();
				mesh.MatrixTransform(mat);
				outMesh.AddMeshData(mesh);
			}
		}
		return outMesh;
	}

	public static float[] GetAlignMatrix(IVec3 startPos, IVec3 endPos, BlockFacing facing)
	{
		double dX = startPos.XAsDouble - endPos.XAsDouble;
		double dY = startPos.YAsDouble - endPos.YAsDouble;
		double dZ = startPos.ZAsDouble - endPos.ZAsDouble;
		double len = Math.Sqrt(dZ * dZ + dY * dY + dX * dX);
		double y = Math.Sqrt(dZ * dZ + dX * dX);
		float yaw = (float)Math.Atan2(dX, dZ) + (float)Math.PI / 2f;
		float pitch = (float)Math.Atan2(y, dY) + (float)Math.PI / 2f;
		float[] mat = new float[16];
		Mat4f.Identity(mat);
		Mat4f.Translate(mat, mat, (float)startPos.XAsDouble, (float)startPos.YAsDouble, (float)startPos.ZAsDouble);
		Mat4f.RotateY(mat, mat, yaw);
		Mat4f.RotateZ(mat, mat, pitch);
		Mat4f.Scale(mat, mat, new float[3]
		{
			(float)len,
			1f,
			1f
		});
		Mat4f.Translate(mat, mat, -1f, -0.125f, -0.5f);
		return mat;
	}

	private BeamPlacerWorkSpace getWorkSpace(EntityAgent forEntity)
	{
		return getWorkSpace((forEntity as EntityPlayer)?.PlayerUID);
	}

	private BeamPlacerWorkSpace getWorkSpace(string playerUID)
	{
		if (workspaceByPlayer.TryGetValue(playerUID, out var ws))
		{
			return ws;
		}
		return workspaceByPlayer[playerUID] = new BeamPlacerWorkSpace();
	}

	public void OnBeamRemoved(Vec3d start, Vec3d end)
	{
		StartEnd startend = new StartEnd
		{
			Start = start,
			End = end
		};
		chunkremove(start.AsBlockPos, startend);
		chunkremove(end.AsBlockPos, startend);
	}

	public void OnBeamAdded(Vec3d start, Vec3d end)
	{
		StartEnd startend = new StartEnd
		{
			Start = start,
			End = end
		};
		chunkadd(start.AsBlockPos, startend);
		chunkadd(end.AsBlockPos, startend);
	}

	private void chunkadd(BlockPos blockpos, StartEnd startend)
	{
		GetSbData(blockpos)?.Beams.Add(startend);
	}

	private void chunkremove(BlockPos blockpos, StartEnd startend)
	{
		GetSbData(blockpos)?.Beams.Remove(startend);
	}

	public double GetStableMostBeam(BlockPos blockpos, out StartEnd beamstartend)
	{
		SupportBeamsData sbdata = GetSbData(blockpos);
		if (sbdata.Beams == null || sbdata.Beams.Count == 0)
		{
			beamstartend = null;
			return 99999.0;
		}
		double minDistance = 99999.0;
		StartEnd nearestBeam = null;
		Vec3d point = blockpos.ToVec3d();
		foreach (StartEnd beam in sbdata.Beams)
		{
			bool num;
			if (!((beam.Start - beam.End).Length() * 1.5 < Math.Abs(beam.End.Y - beam.Start.Y)))
			{
				if (!isBeamStableAt(beam.Start))
				{
					continue;
				}
				num = isBeamStableAt(beam.End);
			}
			else
			{
				if (isBeamStableAt(beam.Start))
				{
					goto IL_00de;
				}
				num = isBeamStableAt(beam.End);
			}
			if (!num)
			{
				continue;
			}
			goto IL_00de;
			IL_00de:
			double dist = DistanceToLine(point, beam.Start, beam.End);
			if (dist < minDistance)
			{
				minDistance = dist;
			}
		}
		beamstartend = nearestBeam;
		return minDistance;
	}

	private static double DistanceToLine(Vec3d point, Vec3d start, Vec3d end)
	{
		Vec3d bc = end - start;
		double length = bc.Length();
		double param = 0.0;
		if (length != 0.0)
		{
			param = Math.Clamp((point - start).Dot(bc) / (length * length), 0.0, 1.0);
		}
		return point.DistanceTo(start + bc * param);
	}

	private bool isBeamStableAt(Vec3d start)
	{
		if (BlockBehaviorUnstableRock.getVerticalSupportStrength(api.World, start.AsBlockPos) <= 0 && BlockBehaviorUnstableRock.getVerticalSupportStrength(api.World, start.Add(-0.0625, 0.0, -0.0625).AsBlockPos) <= 0)
		{
			return BlockBehaviorUnstableRock.getVerticalSupportStrength(api.World, start.Add(0.0625, 0.0, 0.0625).AsBlockPos) > 0;
		}
		return true;
	}

	public SupportBeamsData GetSbData(BlockPos pos)
	{
		return GetSbData(pos.X / 32, pos.Y / 32, pos.Z / 32);
	}

	public SupportBeamsData GetSbData(int chunkx, int chunky, int chunkz)
	{
		IWorldChunk chunk = api.World.BlockAccessor.GetChunk(chunkx, chunky, chunkz);
		if (chunk == null)
		{
			return null;
		}
		object data;
		SupportBeamsData sbdata = (SupportBeamsData)(chunk.LiveModData.TryGetValue("supportbeams", out data) ? ((SupportBeamsData)data) : (chunk.LiveModData["supportbeams"] = chunk.GetModdata<SupportBeamsData>("supportbeams")));
		if (sbdata == null)
		{
			sbdata = (SupportBeamsData)(chunk.LiveModData["supportbeams"] = new SupportBeamsData());
		}
		return sbdata;
	}
}
