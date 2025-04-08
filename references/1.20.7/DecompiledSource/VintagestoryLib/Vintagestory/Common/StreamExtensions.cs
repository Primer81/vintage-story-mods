using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vintagestory.Common;

public static class StreamExtensions
{
	public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<int> progress = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (!source.CanRead)
		{
			throw new ArgumentException("Has to be readable", "source");
		}
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		if (!destination.CanWrite)
		{
			throw new ArgumentException("Has to be writable", "destination");
		}
		if (bufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize");
		}
		byte[] buffer = new byte[bufferSize];
		int totalBytesRead = 0;
		while (true)
		{
			int num;
			int bytesRead = (num = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
			if (num == 0)
			{
				break;
			}
			await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			totalBytesRead += bytesRead;
			progress?.Report(totalBytesRead);
		}
	}
}
