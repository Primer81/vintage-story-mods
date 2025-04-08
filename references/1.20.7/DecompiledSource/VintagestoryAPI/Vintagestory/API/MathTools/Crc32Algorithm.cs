using System;
using System.Security.Cryptography;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Implementation of CRC-32.
/// This class supports several convenient static methods returning the CRC as UInt32.
/// From https://github.com/force-net/Crc32.NET
/// </summary>
public class Crc32Algorithm : HashAlgorithm
{
	private uint _currentCrc;

	private static readonly SafeProxy _proxy = new SafeProxy();

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Vintagestory.API.MathTools.Crc32Algorithm" /> class. 
	/// </summary>
	public Crc32Algorithm()
	{
		HashSizeValue = 32;
	}

	/// <summary>
	/// Computes CRC-32 from multiple buffers.
	/// Call this method multiple times to chain multiple buffers.
	/// </summary>
	/// <param name="initial">
	/// Initial CRC value for the algorithm. It is zero for the first buffer.
	/// Subsequent buffers should have their initial value set to CRC value returned by previous call to this method.
	/// </param>
	/// <param name="input">Input buffer with data to be checksummed.</param>
	/// <param name="offset">Offset of the input data within the buffer.</param>
	/// <param name="length">Length of the input data in the buffer.</param>
	/// <returns>Accumulated CRC-32 of all buffers processed so far.</returns>
	public static uint Append(uint initial, byte[] input, int offset, int length)
	{
		if (input == null)
		{
			throw new ArgumentNullException();
		}
		if (offset < 0 || length < 0 || offset + length > input.Length)
		{
			throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the input array");
		}
		return AppendInternal(initial, input, offset, length);
	}

	/// <summary>
	/// Computes CRC-3C from multiple buffers.
	/// Call this method multiple times to chain multiple buffers.
	/// </summary>
	/// <param name="initial">
	/// Initial CRC value for the algorithm. It is zero for the first buffer.
	/// Subsequent buffers should have their initial value set to CRC value returned by previous call to this method.
	/// </param>
	/// <param name="input">Input buffer containing data to be checksummed.</param>
	/// <returns>Accumulated CRC-32 of all buffers processed so far.</returns>
	public static uint Append(uint initial, byte[] input)
	{
		if (input == null)
		{
			throw new ArgumentNullException();
		}
		return AppendInternal(initial, input, 0, input.Length);
	}

	/// <summary>
	/// Computes CRC-32 from input buffer.
	/// </summary>
	/// <param name="input">Input buffer with data to be checksummed.</param>
	/// <param name="offset">Offset of the input data within the buffer.</param>
	/// <param name="length">Length of the input data in the buffer.</param>
	/// <returns>CRC-32 of the data in the buffer.</returns>
	public static uint Compute(byte[] input, int offset, int length)
	{
		return Append(0u, input, offset, length);
	}

	/// <summary>
	/// Computes CRC-32 from input buffer.
	/// </summary>
	/// <param name="input">Input buffer containing data to be checksummed.</param>
	/// <returns>CRC-32 of the buffer.</returns>
	public static uint Compute(byte[] input)
	{
		return Append(0u, input);
	}

	/// <summary>
	/// Resets internal state of the algorithm. Used internally.
	/// </summary>
	public override void Initialize()
	{
		_currentCrc = 0u;
	}

	/// <summary>
	/// Appends CRC-32 from given buffer
	/// </summary>
	protected override void HashCore(byte[] input, int offset, int length)
	{
		_currentCrc = AppendInternal(_currentCrc, input, offset, length);
	}

	/// <summary>
	/// Computes CRC-32 from <see cref="M:Vintagestory.API.MathTools.Crc32Algorithm.HashCore(System.Byte[],System.Int32,System.Int32)" />
	/// </summary>
	protected override byte[] HashFinal()
	{
		return new byte[4]
		{
			(byte)(_currentCrc >> 24),
			(byte)(_currentCrc >> 16),
			(byte)(_currentCrc >> 8),
			(byte)_currentCrc
		};
	}

	private static uint AppendInternal(uint initial, byte[] input, int offset, int length)
	{
		if (length > 0)
		{
			return _proxy.Append(initial, input, offset, length);
		}
		return initial;
	}
}
