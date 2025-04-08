namespace Vintagestory.API.Common;

public class FastParticlePool
{
	public delegate ParticleBase CreateParticleDelegate();

	public ParticleBase FirstAlive;

	public ParticleBase FirstDead;

	public int PoolSize;

	public int AliveCount;

	public FastParticlePool(int poolSize, CreateParticleDelegate createParticle)
	{
		if (poolSize != 0)
		{
			PoolSize = poolSize;
			ParticleBase elem = (FirstDead = createParticle());
			poolSize--;
			while (poolSize-- > 0)
			{
				elem.Next = createParticle();
				elem.Next.Prev = elem;
				elem = elem.Next;
			}
		}
	}

	public void Kill(ParticleBase elem)
	{
		if (elem == FirstAlive)
		{
			FirstAlive = elem.Next;
			if (FirstAlive != null)
			{
				FirstAlive.Prev = null;
			}
		}
		else
		{
			elem.Prev.Next = elem.Next;
			if (elem.Next != null)
			{
				elem.Next.Prev = elem.Prev;
			}
		}
		if (FirstDead == null)
		{
			elem.Prev = null;
			elem.Next = null;
		}
		else
		{
			FirstDead.Prev = elem;
			elem.Next = FirstDead;
			elem.Prev = null;
		}
		FirstDead = elem;
		AliveCount--;
	}

	public ParticleBase ReviveOne()
	{
		if (FirstDead == null)
		{
			return null;
		}
		ParticleBase elem = FirstDead;
		FirstDead = elem.Next;
		if (FirstAlive == null)
		{
			elem.Prev = null;
			elem.Next = null;
		}
		else
		{
			FirstAlive.Prev = elem;
			elem.Next = FirstAlive;
			elem.Prev = null;
		}
		FirstAlive = elem;
		AliveCount++;
		return elem;
	}
}
