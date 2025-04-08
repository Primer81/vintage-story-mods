using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class VSWebClient : HttpClient
{
	public delegate void PostCompleteHandler(CompletedArgs args);

	public static readonly VSWebClient Inst = new VSWebClient
	{
		Timeout = TimeSpan.FromSeconds(ClientSettings.WebRequestTimeout)
	};

	public void PostAsync(Uri uri, FormUrlEncodedContent postData, PostCompleteHandler onFinished)
	{
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				HttpResponseMessage res = await PostAsync(uri, postData);
				string data = await res.Content.ReadAsStringAsync();
				CompletedArgs args2 = new CompletedArgs
				{
					State = ((!res.IsSuccessStatusCode) ? CompletionState.Error : CompletionState.Good),
					StatusCode = (int)res.StatusCode,
					Response = data,
					ErrorMessage = res.ReasonPhrase
				};
				onFinished(args2);
			}
			catch (Exception e)
			{
				CompletedArgs args = new CompletedArgs
				{
					State = CompletionState.Error,
					ErrorMessage = e.Message
				};
				onFinished(args);
			}
		});
	}

	public string Post(Uri uri, FormUrlEncodedContent postData)
	{
		try
		{
			return PostAsync(uri, postData).Result.Content.ReadAsStringAsync().Result;
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	public async Task DownloadAsync(string requestUri, Stream destination, IProgress<Tuple<int, long>> progress = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		using HttpResponseMessage response = await GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		long? contentLength = response.Content.Headers.ContentLength;
		Stream download = await response.Content.ReadAsStreamAsync(cancellationToken);
		try
		{
			if (progress == null || !contentLength.HasValue)
			{
				await download.CopyToAsync(destination, cancellationToken);
				return;
			}
			Progress<int> relativeProgress = new Progress<int>(delegate(int totalBytes)
			{
				progress.Report(new Tuple<int, long>(totalBytes, contentLength.Value));
			});
			await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
			download.Close();
		}
		finally
		{
			if (download != null)
			{
				await download.DisposeAsync();
			}
		}
	}
}
