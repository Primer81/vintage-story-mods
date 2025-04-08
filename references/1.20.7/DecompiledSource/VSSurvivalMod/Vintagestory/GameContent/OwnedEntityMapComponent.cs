using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class OwnedEntityMapComponent : MapComponent
{
	private EntityOwnership entity;

	internal MeshRef quadModel;

	public LoadedTexture Texture;

	private Vec2f viewPos = new Vec2f();

	private Matrixf mvMat = new Matrixf();

	private int color;

	public OwnedEntityMapComponent(ICoreClientAPI capi, LoadedTexture texture, EntityOwnership entity, string color = null)
		: base(capi)
	{
		quadModel = capi.Render.UploadMesh(QuadMeshUtil.GetQuad());
		Texture = texture;
		this.entity = entity;
		this.color = ((color != null) ? (ColorUtil.Hex2Int(color) | -16777216) : 0);
	}

	public override void Render(GuiElementMap map, float dt)
	{
		bool pinned = true;
		EntityPos pos = capi.World.GetEntityById(entity.EntityId)?.Pos ?? entity.Pos;
		if (!(pos.DistanceTo(capi.World.Player.Entity.Pos.XYZ) < 2.0))
		{
			map.TranslateWorldPosToViewPos(pos.XYZ, ref viewPos);
			if (pinned)
			{
				map.Api.Render.PushScissor(null);
				map.ClampButPreserveAngle(ref viewPos, 2);
			}
			else if (viewPos.X < -10f || viewPos.Y < -10f || (double)viewPos.X > map.Bounds.OuterWidth + 10.0 || (double)viewPos.Y > map.Bounds.OuterHeight + 10.0)
			{
				return;
			}
			float x = (float)(map.Bounds.renderX + (double)viewPos.X);
			float y = (float)(map.Bounds.renderY + (double)viewPos.Y);
			ICoreClientAPI api = map.Api;
			if (Texture.Disposed)
			{
				throw new Exception("Fatal. Trying to render a disposed texture");
			}
			if (quadModel.Disposed)
			{
				throw new Exception("Fatal. Trying to render a disposed texture");
			}
			capi.Render.GlToggleBlend(blend: true);
			IShaderProgram prog = api.Render.GetEngineShader(EnumShaderProgram.Gui);
			if (color == 0)
			{
				prog.Uniform("rgbaIn", ColorUtil.WhiteArgbVec);
			}
			else
			{
				Vec4f vec = new Vec4f();
				ColorUtil.ToRGBAVec4f(color, ref vec);
				prog.Uniform("rgbaIn", vec);
			}
			prog.Uniform("applyColor", 0);
			prog.Uniform("extraGlow", 0);
			prog.Uniform("noTexture", 0f);
			prog.BindTexture2D("tex2d", Texture.TextureId, 0);
			mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale(Texture.Width, Texture.Height, 0f)
				.Scale(0.5f, 0.5f, 0f)
				.RotateZ(0f - pos.Yaw + (float)Math.PI);
			prog.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
			prog.UniformMatrix("modelViewMatrix", mvMat.Values);
			api.Render.RenderMesh(quadModel);
			if (pinned)
			{
				map.Api.Render.PopScissor();
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		quadModel.Dispose();
	}

	public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
	{
		Vec3d pos = capi.World.GetEntityById(entity.EntityId)?.Pos?.XYZ ?? entity.Pos.XYZ;
		Vec2f viewPos = new Vec2f();
		mapElem.TranslateWorldPosToViewPos(pos, ref viewPos);
		double mouseX = (double)args.X - mapElem.Bounds.renderX;
		double mouseY = (double)args.Y - mapElem.Bounds.renderY;
		double sc = GuiElement.scaled(5.0);
		if (Math.Abs((double)viewPos.X - mouseX) < sc && Math.Abs((double)viewPos.Y - mouseY) < sc)
		{
			hoverText.AppendLine(entity.Name);
			hoverText.AppendLine("Owned by you");
		}
	}
}
