using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ChunkCuller
{
	private ClientMain game;

	private const int chunksize = 32;

	public Ray ray = new Ray();

	private Vec3d planePosition = new Vec3d();

	private Vec3i[] cubicShellPositions;

	private Vec3i centerpos = new Vec3i();

	private bool isAboveHeightLimit;

	public Vec3i curpos = new Vec3i();

	private Vec3i toPos = new Vec3i();

	private int qCount;

	private bool nowOff;

	private const long ExtraDimensionsStart = 4503599627370496L;

	public ChunkCuller(ClientMain game)
	{
		this.game = game;
		ClientSettings.Inst.AddWatcher<int>("viewDistance", genShellVectors);
		genShellVectors(ClientSettings.ViewDistance);
	}

	private void genShellVectors(int viewDistance)
	{
		Vec2i[] points = ShapeUtil.GetOctagonPoints(0, 0, viewDistance / 32 + 1);
		int cmapheight = game.WorldMap.ChunkMapSizeY;
		HashSet<Vec3i> shellPositions = new HashSet<Vec3i>();
		foreach (Vec2i point2 in points)
		{
			for (int cy = -cmapheight; cy <= cmapheight; cy++)
			{
				shellPositions.Add(new Vec3i(point2.X, cy, point2.Y));
			}
		}
		for (int r = 0; r < viewDistance / 32 + 1; r++)
		{
			points = ShapeUtil.GetOctagonPoints(0, 0, r);
			foreach (Vec2i point in points)
			{
				shellPositions.Add(new Vec3i(point.X, -cmapheight, point.Y));
				shellPositions.Add(new Vec3i(point.X, cmapheight, point.Y));
			}
		}
		cubicShellPositions = shellPositions.ToArray();
	}

	public void CullInvisibleChunks()
	{
		if (!ClientSettings.Occlusionculling || game.WorldMap.chunks.Count < 100)
		{
			if (nowOff)
			{
				return;
			}
			ClientChunk.bufIndex = 1;
			lock (game.WorldMap.chunksLock)
			{
				foreach (KeyValuePair<long, ClientChunk> val2 in game.WorldMap.chunks)
				{
					if (val2.Key / 4503599627370496L != 1)
					{
						val2.Value.SetVisible(visible: true);
					}
				}
			}
			ClientChunk.bufIndex = 0;
			lock (game.WorldMap.chunksLock)
			{
				foreach (KeyValuePair<long, ClientChunk> val in game.WorldMap.chunks)
				{
					if (val.Key / 4503599627370496L != 1)
					{
						val.Value.SetVisible(visible: true);
					}
				}
			}
			nowOff = true;
			return;
		}
		nowOff = false;
		Vec3d camPos = game.player.Entity.CameraPos;
		if (centerpos.Equals((int)camPos.X / 32, (int)camPos.Y / 32, (int)camPos.Z / 32) && Math.Abs(game.chunkPositionsForRegenTrav.Count - qCount) < 10)
		{
			return;
		}
		qCount = game.chunkPositionsForRegenTrav.Count;
		centerpos.Set((int)(camPos.X / 32.0), (int)(camPos.Y / 32.0), (int)(camPos.Z / 32.0));
		isAboveHeightLimit = centerpos.Y >= game.WorldMap.ChunkMapSizeY;
		lock (game.WorldMap.chunksLock)
		{
			foreach (KeyValuePair<long, ClientChunk> chunk2 in game.WorldMap.chunks)
			{
				chunk2.Value.SetVisible(visible: false);
			}
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -1; dy <= 1; dy++)
				{
					for (int dz = -1; dz <= 1; dz++)
					{
						long index3d = game.WorldMap.ChunkIndex3D(dx + centerpos.X, dy + centerpos.Y, dz + centerpos.Z);
						ClientChunk chunk = null;
						if (game.WorldMap.chunks.TryGetValue(index3d, out chunk))
						{
							chunk.SetVisible(visible: true);
						}
					}
				}
			}
		}
		for (int i = 0; i < cubicShellPositions.Length; i++)
		{
			Vec3i vec = cubicShellPositions[i];
			traverseRayAndMarkVisible(centerpos, vec, 0.25);
			traverseRayAndMarkVisible(centerpos, vec, 0.75);
			traverseRayAndMarkVisible(centerpos, vec, 0.75, 0.0);
		}
		game.chunkRenderer.SwapVisibleBuffers();
	}

	private void traverseRayAndMarkVisible(Vec3i fromPos, Vec3i toPosRel, double yoffset = 0.5, double xoffset = 0.5)
	{
		ray.origin.Set((double)fromPos.X + xoffset, (double)fromPos.Y + yoffset, (double)fromPos.Z + 0.5);
		ray.dir.Set((double)toPosRel.X + xoffset, (double)toPosRel.Y + yoffset, (double)toPosRel.Z + 0.5);
		toPos.Set(fromPos.X + toPosRel.X, fromPos.Y + toPosRel.Y, fromPos.Z + toPosRel.Z);
		curpos.Set(fromPos);
		BlockFacing fromFace = null;
		int manhattenLength = fromPos.ManhattenDistanceTo(toPos);
		int curMhDist;
		while ((curMhDist = curpos.ManhattenDistanceTo(fromPos)) <= manhattenLength + 2)
		{
			BlockFacing toFace = getExitingFace(curpos);
			if (toFace == null)
			{
				break;
			}
			long index3d = ((long)curpos.Y * (long)game.WorldMap.index3dMulZ + curpos.Z) * game.WorldMap.index3dMulX + curpos.X;
			game.WorldMap.chunks.TryGetValue(index3d, out var chunk);
			if (chunk != null)
			{
				chunk.SetVisible(visible: true);
				if (curMhDist > 1 && !chunk.IsTraversable(fromFace, toFace))
				{
					break;
				}
			}
			curpos.Offset(toFace);
			fromFace = toFace.Opposite;
			if (!game.WorldMap.IsValidChunkPosFast(curpos.X, curpos.Y, curpos.Z) && (!isAboveHeightLimit || curpos.Y <= 0))
			{
				break;
			}
		}
	}

	private BlockFacing getExitingFace(Vec3i pos)
	{
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockSideFacing = BlockFacing.ALLFACES[i];
			Vec3i planeNormal = blockSideFacing.Normali;
			double demon = (double)planeNormal.X * ray.dir.X + (double)planeNormal.Y * ray.dir.Y + (double)planeNormal.Z * ray.dir.Z;
			if (!(demon <= 1E-05))
			{
				planePosition.Set(pos).Add(blockSideFacing.PlaneCenter);
				double num = planePosition.X - ray.origin.X;
				double pty = planePosition.Y - ray.origin.Y;
				double ptz = planePosition.Z - ray.origin.Z;
				double t = (num * (double)planeNormal.X + pty * (double)planeNormal.Y + ptz * (double)planeNormal.Z) / demon;
				if (t >= 0.0 && Math.Abs(ray.origin.X + ray.dir.X * t - planePosition.X) <= 0.5 && Math.Abs(ray.origin.Y + ray.dir.Y * t - planePosition.Y) <= 0.5 && Math.Abs(ray.origin.Z + ray.dir.Z * t - planePosition.Z) <= 0.5)
				{
					return blockSideFacing;
				}
			}
		}
		return null;
	}
}
