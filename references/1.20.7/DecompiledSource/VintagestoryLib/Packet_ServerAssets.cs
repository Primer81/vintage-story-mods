public class Packet_ServerAssets
{
	public Packet_BlockType[] Blocks;

	public int BlocksCount;

	public int BlocksLength;

	public Packet_ItemType[] Items;

	public int ItemsCount;

	public int ItemsLength;

	public Packet_EntityType[] Entities;

	public int EntitiesCount;

	public int EntitiesLength;

	public Packet_Recipes[] Recipes;

	public int RecipesCount;

	public int RecipesLength;

	public const int BlocksFieldID = 1;

	public const int ItemsFieldID = 2;

	public const int EntitiesFieldID = 3;

	public const int RecipesFieldID = 4;

	public Packet_BlockType[] GetBlocks()
	{
		return Blocks;
	}

	public void SetBlocks(Packet_BlockType[] value, int count, int length)
	{
		Blocks = value;
		BlocksCount = count;
		BlocksLength = length;
	}

	public void SetBlocks(Packet_BlockType[] value)
	{
		Blocks = value;
		BlocksCount = value.Length;
		BlocksLength = value.Length;
	}

	public int GetBlocksCount()
	{
		return BlocksCount;
	}

	public void BlocksAdd(Packet_BlockType value)
	{
		if (BlocksCount >= BlocksLength)
		{
			if ((BlocksLength *= 2) == 0)
			{
				BlocksLength = 1;
			}
			Packet_BlockType[] newArray = new Packet_BlockType[BlocksLength];
			for (int i = 0; i < BlocksCount; i++)
			{
				newArray[i] = Blocks[i];
			}
			Blocks = newArray;
		}
		Blocks[BlocksCount++] = value;
	}

	public Packet_ItemType[] GetItems()
	{
		return Items;
	}

	public void SetItems(Packet_ItemType[] value, int count, int length)
	{
		Items = value;
		ItemsCount = count;
		ItemsLength = length;
	}

	public void SetItems(Packet_ItemType[] value)
	{
		Items = value;
		ItemsCount = value.Length;
		ItemsLength = value.Length;
	}

	public int GetItemsCount()
	{
		return ItemsCount;
	}

	public void ItemsAdd(Packet_ItemType value)
	{
		if (ItemsCount >= ItemsLength)
		{
			if ((ItemsLength *= 2) == 0)
			{
				ItemsLength = 1;
			}
			Packet_ItemType[] newArray = new Packet_ItemType[ItemsLength];
			for (int i = 0; i < ItemsCount; i++)
			{
				newArray[i] = Items[i];
			}
			Items = newArray;
		}
		Items[ItemsCount++] = value;
	}

	public Packet_EntityType[] GetEntities()
	{
		return Entities;
	}

	public void SetEntities(Packet_EntityType[] value, int count, int length)
	{
		Entities = value;
		EntitiesCount = count;
		EntitiesLength = length;
	}

	public void SetEntities(Packet_EntityType[] value)
	{
		Entities = value;
		EntitiesCount = value.Length;
		EntitiesLength = value.Length;
	}

	public int GetEntitiesCount()
	{
		return EntitiesCount;
	}

	public void EntitiesAdd(Packet_EntityType value)
	{
		if (EntitiesCount >= EntitiesLength)
		{
			if ((EntitiesLength *= 2) == 0)
			{
				EntitiesLength = 1;
			}
			Packet_EntityType[] newArray = new Packet_EntityType[EntitiesLength];
			for (int i = 0; i < EntitiesCount; i++)
			{
				newArray[i] = Entities[i];
			}
			Entities = newArray;
		}
		Entities[EntitiesCount++] = value;
	}

	public Packet_Recipes[] GetRecipes()
	{
		return Recipes;
	}

	public void SetRecipes(Packet_Recipes[] value, int count, int length)
	{
		Recipes = value;
		RecipesCount = count;
		RecipesLength = length;
	}

	public void SetRecipes(Packet_Recipes[] value)
	{
		Recipes = value;
		RecipesCount = value.Length;
		RecipesLength = value.Length;
	}

	public int GetRecipesCount()
	{
		return RecipesCount;
	}

	public void RecipesAdd(Packet_Recipes value)
	{
		if (RecipesCount >= RecipesLength)
		{
			if ((RecipesLength *= 2) == 0)
			{
				RecipesLength = 1;
			}
			Packet_Recipes[] newArray = new Packet_Recipes[RecipesLength];
			for (int i = 0; i < RecipesCount; i++)
			{
				newArray[i] = Recipes[i];
			}
			Recipes = newArray;
		}
		Recipes[RecipesCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
