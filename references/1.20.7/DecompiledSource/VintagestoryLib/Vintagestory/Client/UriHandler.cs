using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Vintagestory.Client;

public class UriHandler
{
	private const string IPC_CHANNEL_NAME = "SingleInstanceVintageStoryWithUriScheme";

	private NamedPipeServerStream _namedPipeServerStream;

	private StreamReader _serverStringReader;

	private CancellationTokenSource _pipeServerCts;

	private NamedPipeClientStream _namedPipeClientStream;

	private StreamWriter _clientStreamWriter;

	private static UriHandler _instance;

	public static UriHandler Instance => _instance ?? (_instance = new UriHandler());

	private UriHandler()
	{
	}

	public void StartPipeServer()
	{
		try
		{
			if (_namedPipeServerStream == null)
			{
				_namedPipeServerStream = new NamedPipeServerStream("SingleInstanceVintageStoryWithUriScheme", PipeDirection.In);
				_pipeServerCts = new CancellationTokenSource();
				_serverStringReader = new StreamReader(_namedPipeServerStream);
			}
			Task.Run(async delegate
			{
				while (!_pipeServerCts.IsCancellationRequested)
				{
					try
					{
						await _namedPipeServerStream.WaitForConnectionAsync(_pipeServerCts.Token);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					string readLineAsync = await _serverStringReader.ReadLineAsync();
					if (readLineAsync != null)
					{
						ClientProgramArgs clientProgramArgs = Parser.Default.ParseArguments<ClientProgramArgs>(readLineAsync.Split(" ")).Value;
						if (clientProgramArgs?.InstallModId != null)
						{
							HandleModInstall(clientProgramArgs.InstallModId);
						}
						else if (clientProgramArgs?.ConnectServerAddress != null)
						{
							HandleConnect(clientProgramArgs.ConnectServerAddress);
						}
					}
					_namedPipeServerStream.Disconnect();
				}
			}, _pipeServerCts.Token);
		}
		catch
		{
			Console.WriteLine("Couldn't start NamedPipeServer.");
		}
	}

	public bool TryConnectClientPipe()
	{
		NamedPipeClientStream namedPipeClientStream = _namedPipeClientStream;
		if (namedPipeClientStream != null && namedPipeClientStream.IsConnected)
		{
			Console.WriteLine("Client pipe already connected");
			return true;
		}
		try
		{
			_namedPipeClientStream = new NamedPipeClientStream(".", "SingleInstanceVintageStoryWithUriScheme", PipeDirection.Out);
			_namedPipeClientStream.Connect(1000);
			_clientStreamWriter = new StreamWriter(_namedPipeClientStream);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public void Dispose()
	{
		_pipeServerCts?.Cancel();
		_serverStringReader?.Dispose();
		_namedPipeServerStream?.Close();
		_namedPipeServerStream?.Dispose();
		_clientStreamWriter?.Dispose();
		_namedPipeClientStream?.Close();
		_namedPipeClientStream?.Dispose();
	}

	private void HandleConnect(string uri)
	{
		ScreenManager.EnqueueMainThreadTask(delegate
		{
			ClientProgram.screenManager.GamePlatform.XPlatInterface.FocusWindow();
			ClientProgram.screenManager.ConnectToMultiplayer(uri, null);
		});
	}

	private void HandleModInstall(string modId)
	{
		ScreenManager.EnqueueMainThreadTask(delegate
		{
			ClientProgram.screenManager.GamePlatform.XPlatInterface.FocusWindow();
			ClientProgram.screenManager.InstallMod(modId);
		});
	}

	public void SendModInstall(string argsInstallModId)
	{
		if (_clientStreamWriter == null || _namedPipeClientStream == null)
		{
			Console.WriteLine("ClientPipeStream seems not initialized did you forget to call ConnectClientPipe first?");
			return;
		}
		_clientStreamWriter.WriteLine("-i " + argsInstallModId);
		_clientStreamWriter.Flush();
	}

	public void SendConnect(string argsConnectServerAddress)
	{
		if (_clientStreamWriter == null || _namedPipeClientStream == null)
		{
			Console.WriteLine("ClientPipeStream seems not initialized did you forget to call ConnectClientPipe first?");
			return;
		}
		_clientStreamWriter.WriteLine("-c " + argsConnectServerAddress);
		_clientStreamWriter.Flush();
	}
}
