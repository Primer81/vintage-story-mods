using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.Essentials;

namespace Vintagestory.GameContent;

public class EntityAnimalBot : EntityAgent
{
	public string Name;

	public List<INpcCommand> Commands = new List<INpcCommand>();

	public Queue<INpcCommand> ExecutingCommands = new Queue<INpcCommand>();

	protected bool commandQueueActive;

	public bool LoopCommands;

	public PathTraverserBase linepathTraverser;

	public PathTraverserBase wppathTraverser;

	public override bool StoreWithChunk => true;

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		linepathTraverser = new StraightLineTraverser(this);
		wppathTraverser = new WaypointsTraverser(this);
	}

	public void StartExecuteCommands(bool enqueue = true)
	{
		if (enqueue)
		{
			foreach (INpcCommand val in Commands)
			{
				ExecutingCommands.Enqueue(val);
			}
		}
		if (ExecutingCommands.Count > 0)
		{
			INpcCommand cmd = ExecutingCommands.Peek();
			cmd.Start();
			WatchedAttributes.SetString("currentCommand", cmd.Type);
		}
		commandQueueActive = true;
	}

	public void StopExecuteCommands()
	{
		if (ExecutingCommands.Count > 0)
		{
			ExecutingCommands.Peek().Stop();
		}
		ExecutingCommands.Clear();
		commandQueueActive = false;
		WatchedAttributes.SetString("currentCommand", "");
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		linepathTraverser.OnGameTick(dt);
		wppathTraverser.OnGameTick(dt);
		if (commandQueueActive)
		{
			if (ExecutingCommands.Count > 0)
			{
				if (ExecutingCommands.Peek().IsFinished())
				{
					WatchedAttributes.SetString("currentCommand", "");
					ExecutingCommands.Dequeue();
					if (ExecutingCommands.Count > 0)
					{
						ExecutingCommands.Peek().Start();
					}
					else if (LoopCommands)
					{
						StartExecuteCommands();
					}
					else
					{
						commandQueueActive = false;
					}
				}
			}
			else if (LoopCommands)
			{
				StartExecuteCommands();
			}
			else
			{
				commandQueueActive = false;
			}
		}
		World.FrameProfiler.Mark("entityAnimalBot-pathfinder-and-commands");
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		if (!forClient)
		{
			ITreeAttribute ctree = new TreeAttribute();
			WatchedAttributes["commandQueue"] = ctree;
			ITreeAttribute commands = (ITreeAttribute)(ctree["commands"] = new TreeAttribute());
			int i = 0;
			foreach (INpcCommand val in Commands)
			{
				ITreeAttribute attr = new TreeAttribute();
				val.ToAttribute(attr);
				attr.SetString("type", val.Type);
				commands["cmd" + i] = attr;
				i++;
			}
			WatchedAttributes.SetBool("loop", LoopCommands);
		}
		base.ToBytes(writer, forClient);
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		if (forClient)
		{
			return;
		}
		ITreeAttribute ctree = WatchedAttributes.GetTreeAttribute("commandQueue");
		if (ctree == null)
		{
			return;
		}
		ITreeAttribute commands = ctree.GetTreeAttribute("commands");
		if (commands == null)
		{
			return;
		}
		foreach (KeyValuePair<string, IAttribute> item in commands)
		{
			ITreeAttribute attr = item.Value as ITreeAttribute;
			string type = attr.GetString("type");
			INpcCommand command = null;
			switch (type)
			{
			case "tp":
				command = new NpcTeleportCommand(this, null);
				break;
			case "goto":
				command = new NpcGotoCommand(this, null, astar: false, null, 0f);
				break;
			case "anim":
				command = new NpcPlayAnimationCommand(this, null, 1f);
				break;
			case "lookat":
				command = new NpcLookatCommand(this, 0f);
				break;
			}
			command.FromAttribute(attr);
			Commands.Add(command);
		}
		LoopCommands = WatchedAttributes.GetBool("loop");
	}
}
