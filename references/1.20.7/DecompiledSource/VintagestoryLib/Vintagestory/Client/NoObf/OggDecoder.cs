using System;
using System.IO;
using Vintagestory.API.Common;
using csogg;
using csvorbis;

namespace Vintagestory.Client.NoObf;

public class OggDecoder
{
	private const int buffersize = 8192;

	[ThreadStatic]
	private static byte[] convbuffer;

	public AudioMetaData OggToWav(Stream ogg, IAsset asset)
	{
		AudioMetaData sample = new AudioMetaData(asset);
		sample.Loaded = 1;
		TextWriter s_err = new StringWriter();
		Stream input = null;
		MemoryStream output = null;
		input = ogg;
		output = new MemoryStream();
		SyncState oy = new SyncState();
		StreamState os = new StreamState();
		Page og = new OggPage();
		Packet op = new Packet();
		Info vi = new Info();
		Comment vc = new Comment();
		DspState vd = new DspState();
		csvorbis.Block vb = new csvorbis.Block(vd);
		int bytes = 0;
		oy.init();
		int eos = 0;
		int index = oy.buffer(4096);
		byte[] buffer = oy.data;
		try
		{
			bytes = input.Read(buffer, index, 4096);
		}
		catch (Exception e3)
		{
			s_err.WriteLine(LoggerBase.CleanStackTrace(e3.ToString()));
		}
		oy.wrote(bytes);
		if (oy.pageout(og) != 1)
		{
			if (bytes < 4096)
			{
				goto IL_04fd;
			}
			s_err.WriteLine("Input does not appear to be an Ogg bitstream.");
		}
		os.init(og.serialno());
		vi.init();
		vc.init();
		if (os.pagein(og) < 0)
		{
			s_err.WriteLine("Error reading first page of Ogg bitstream data.");
		}
		if (os.packetout(op) != 1)
		{
			s_err.WriteLine("Error reading initial header packet.");
		}
		if (vi.synthesis_headerin(vc, op) < 0)
		{
			s_err.WriteLine("This Ogg bitstream does not contain Vorbis audio data.");
		}
		int i = 0;
		while (i < 2)
		{
			while (i < 2)
			{
				switch (oy.pageout(og))
				{
				case 1:
					os.pagein(og);
					for (; i < 2; vi.synthesis_headerin(vc, op), i++)
					{
						switch (os.packetout(op))
						{
						case -1:
							s_err.WriteLine("Corrupt secondary header.  Exiting.");
							continue;
						default:
							continue;
						case 0:
							break;
						}
						break;
					}
					continue;
				default:
					continue;
				case 0:
					break;
				}
				break;
			}
			index = oy.buffer(4096);
			buffer = oy.data;
			try
			{
				bytes = input.Read(buffer, index, 4096);
			}
			catch (Exception e2)
			{
				s_err.WriteLine(LoggerBase.CleanStackTrace(e2.ToString()));
			}
			if (bytes == 0 && i < 2)
			{
				s_err.WriteLine("End of file before finding all Vorbis headers!");
			}
			oy.wrote(bytes);
		}
		byte[][] ptr2 = vc.user_comments;
		for (int k = 0; k < vc.user_comments.Length && ptr2[k] != null; k++)
		{
			s_err.WriteLine(vc.getComment(k));
		}
		s_err.WriteLine("\nBitstream is " + vi.channels + " channel, " + vi.rate + "Hz");
		s_err.WriteLine("Encoded by: " + vc.getVendor() + "\n");
		sample.Channels = vi.channels;
		sample.Rate = vi.rate;
		int convsize = 4096 / vi.channels;
		vd.synthesis_init(vi);
		vb.init(vd);
		float[][][] _pcm = new float[1][][];
		int[] _index = new int[vi.channels];
		if (convbuffer == null)
		{
			convbuffer = new byte[8192];
		}
		while (eos == 0)
		{
			while (eos == 0)
			{
				switch (oy.pageout(og))
				{
				case -1:
					s_err.WriteLine("Corrupt or missing data in bitstream; continuing...");
					continue;
				default:
					os.pagein(og);
					while (true)
					{
						switch (os.packetout(op))
						{
						case -1:
							continue;
						default:
						{
							if (vb.synthesis(op) == 0)
							{
								vd.synthesis_blockin(vb);
							}
							int samples;
							while ((samples = vd.synthesis_pcmout(_pcm, _index)) > 0)
							{
								float[][] pcm = _pcm[0];
								bool clipflag = false;
								int bout = ((samples < convsize) ? samples : convsize);
								for (i = 0; i < vi.channels; i++)
								{
									int ptr = i * 2;
									int mono = _index[i];
									for (int j = 0; j < bout; j++)
									{
										int val = (int)((double)pcm[i][mono + j] * 32767.0);
										if (val > 32767)
										{
											val = 32767;
											clipflag = true;
										}
										if (val < -32768)
										{
											val = -32768;
											clipflag = true;
										}
										if (val < 0)
										{
											val |= 0x8000;
										}
										convbuffer[ptr] = (byte)val;
										convbuffer[ptr + 1] = (byte)((uint)val >> 8);
										ptr += 2 * vi.channels;
									}
								}
								output.Write(convbuffer, 0, 2 * vi.channels * bout);
								vd.synthesis_read(bout);
							}
							continue;
						}
						case 0:
							break;
						}
						break;
					}
					if (og.eos() != 0)
					{
						eos = 1;
					}
					continue;
				case 0:
					break;
				}
				break;
			}
			if (eos == 0)
			{
				index = oy.buffer(4096);
				buffer = oy.data;
				try
				{
					bytes = input.Read(buffer, index, 4096);
				}
				catch (Exception e)
				{
					s_err.WriteLine(LoggerBase.CleanStackTrace(e.ToString()));
				}
				oy.wrote(bytes);
				if (bytes == 0)
				{
					eos = 1;
				}
			}
		}
		os.clear();
		vb.clear();
		vd.clear();
		vi.clear();
		goto IL_04fd;
		IL_04fd:
		oy.clear();
		input.Close();
		sample.Pcm = output.ToArray();
		return sample;
	}
}
