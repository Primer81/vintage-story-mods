using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompactExifLib;

public class ExifData
{
	private enum ImageFileBlockState
	{
		NonExistent,
		Removed,
		Existent
	}

	private class TagItem
	{
		public ExifTagId TagId;

		public ExifTagType TagType;

		public int ValueCount;

		public byte[] ValueData;

		public int ValueIndex;

		public int AllocatedByteCount;

		public int OriginalDataOffset;

		public int ByteCount => GetTagByteCount(TagType, ValueCount);

		public TagItem(ExifTag TagSpec, ExifTagType _TagType, int _ValueCount)
			: this(ExtractTagId(TagSpec), _TagType, _ValueCount)
		{
		}

		public TagItem(ExifTagId _TagId, ExifTagType _TagType, int _ValueCount)
		{
			TagId = _TagId;
			TagType = _TagType;
			ValueCount = _ValueCount;
			int RequiredByteCount = GetTagByteCount(_TagType, _ValueCount);
			ValueData = AllocTagMemory(RequiredByteCount);
			AllocatedByteCount = ValueData.Length;
			ValueIndex = 0;
		}

		public TagItem(ExifTagId _TagId, ExifTagType _TagType, int _ValueCount, byte[] _ValueArray, int _ValueIndex, int _AllocatedByteCount, int _OriginalDataOffset = 0)
		{
			TagId = _TagId;
			TagType = _TagType;
			ValueCount = _ValueCount;
			ValueData = _ValueArray;
			ValueIndex = _ValueIndex;
			AllocatedByteCount = _AllocatedByteCount;
			OriginalDataOffset = _OriginalDataOffset;
		}

		public static byte[] AllocTagMemory(int RequiredByteCount)
		{
			int NewByteCount = RequiredByteCount;
			if (NewByteCount < 32)
			{
				NewByteCount = 32;
			}
			return new byte[NewByteCount];
		}

		public void SetTagTypeAndValueCount(ExifTagType _TagType, int _ValueCount, bool KeepExistingData)
		{
			int RequiredByteCount = GetTagByteCount(_TagType, _ValueCount);
			if (AllocatedByteCount < RequiredByteCount)
			{
				uint NewByteCount = (uint)(AllocatedByteCount << 1);
				if (NewByteCount > int.MaxValue)
				{
					NewByteCount = 2147483647u;
				}
				else if (NewByteCount < (uint)RequiredByteCount)
				{
					NewByteCount = (uint)RequiredByteCount;
				}
				byte[] NewTagData = AllocTagMemory((int)NewByteCount);
				if (KeepExistingData)
				{
					Array.Copy(ValueData, ValueIndex, NewTagData, 0, AllocatedByteCount);
				}
				AllocatedByteCount = NewTagData.Length;
				ValueData = NewTagData;
				ValueIndex = 0;
			}
			TagType = _TagType;
			ValueCount = _ValueCount;
		}
	}

	private class FlexArray
	{
		private byte[] _Buffer;

		private int _Length;

		public byte this[int i]
		{
			get
			{
				return _Buffer[i];
			}
			set
			{
				_Buffer[i] = value;
			}
		}

		public byte[] Buffer => _Buffer;

		public int Length
		{
			get
			{
				return _Length;
			}
			set
			{
				if (value > _Buffer.Length)
				{
					uint i = (uint)_Buffer.Length;
					i <<= 1;
					if (i > int.MaxValue)
					{
						i = 2147483647u;
					}
					else if ((uint)value > i)
					{
						i = (uint)value;
					}
					Array.Resize(ref _Buffer, (int)i);
				}
				_Length = value;
			}
		}

		public FlexArray(int Capacity)
		{
			_Buffer = new byte[Capacity];
			_Length = 0;
		}
	}

	public ImageType ImageType;

	public const int IfdShift = 16;

	private const int MinExifBlockLen = 6;

	private const int MaxTagValueCount = 268435455;

	private byte[] SourceExifBlock;

	private Stream SourceExifStream;

	private ExifByteOrder _ByteOrder;

	private string _FileNameWithPath;

	private static readonly int[] TypeByteCount = new int[13]
	{
		0, 1, 1, 2, 4, 8, 1, 1, 2, 4,
		8, 4, 8
	};

	private const int TypeByteCountLen = 13;

	private const int IdCodeLength = 8;

	private static readonly byte[] IdCodeUtf16 = new byte[8] { 85, 78, 73, 67, 79, 68, 69, 0 };

	private static readonly byte[] IdCodeAscii = new byte[8] { 65, 83, 67, 73, 73, 0, 0, 0 };

	private static readonly byte[] IdCodeDefault = new byte[8];

	private Dictionary<ExifTagId, TagItem>[] TagTable;

	private byte[] ThumbnailImage;

	private int ThumbnailStartIndex;

	private int ThumbnailByteCount;

	private const int ExifIfdCount = 5;

	private ExifIfd ExifIfdForTagEnumeration;

	private Dictionary<ExifTagId, TagItem>.KeyCollection.Enumerator TagEnumerator;

	private ImageFileBlockState[] ImageFileBlockInfo;

	private ExifErrCode ErrCodeForIllegalExifBlock;

	private const int JpegMaxExifBlockLen = 65518;

	private const ushort JpegApp0Marker = 65504;

	private const ushort JpegApp1Marker = 65505;

	private const ushort JpegApp2Marker = 65506;

	private const ushort JpegApp13Marker = 65517;

	private const ushort JpegApp14Marker = 65518;

	private const ushort JpegCommentMarker = 65534;

	private const ushort JpegSoiMarker = 65496;

	private const ushort JpegSosMarker = 65498;

	private static readonly byte[] JpegExifSignature = new byte[6] { 69, 120, 105, 102, 0, 0 };

	private static readonly byte[] JpegXmpInitialSignature = new byte[20]
	{
		104, 116, 116, 112, 58, 47, 47, 110, 115, 46,
		97, 100, 111, 98, 101, 46, 99, 111, 109, 47
	};

	private static readonly byte[] JpegIptcSignature = new byte[14]
	{
		80, 104, 111, 116, 111, 115, 104, 111, 112, 32,
		51, 46, 48, 0
	};

	private const int TiffHeaderLen = 8;

	private const uint TiffHeaderSignatureLE = 1229531648u;

	private const uint TiffHeaderSignatureBE = 1296891946u;

	private const uint PngHeaderPart1 = 2303741511u;

	private const uint PngHeaderPart2 = 218765834u;

	private const uint PngIhdrChunk = 1229472850u;

	private const uint PngExifChunk = 1700284774u;

	private const uint PngItxtChunk = 1767135348u;

	private const uint PngTextChunk = 1950701684u;

	private const uint PngTimeChunk = 1950960965u;

	private const uint PngIendChunk = 1229278788u;

	private static readonly byte[] PngXmpSignature = new byte[18]
	{
		88, 77, 76, 58, 99, 111, 109, 46, 97, 100,
		111, 98, 101, 46, 120, 109, 112, 0
	};

	private static readonly byte[] PngIptcSignature = new byte[22]
	{
		82, 97, 119, 32, 112, 114, 111, 102, 105, 108,
		101, 32, 116, 121, 112, 101, 32, 105, 112, 116,
		99, 0
	};

	private static readonly int ImageFileBlockCount = Enum.GetValues(typeof(ImageFileBlock)).Length;

	private static readonly uint[] Crc32ChecksumTable = new uint[256]
	{
		0u, 1996959894u, 3993919788u, 2567524794u, 124634137u, 1886057615u, 3915621685u, 2657392035u, 249268274u, 2044508324u,
		3772115230u, 2547177864u, 162941995u, 2125561021u, 3887607047u, 2428444049u, 498536548u, 1789927666u, 4089016648u, 2227061214u,
		450548861u, 1843258603u, 4107580753u, 2211677639u, 325883990u, 1684777152u, 4251122042u, 2321926636u, 335633487u, 1661365465u,
		4195302755u, 2366115317u, 997073096u, 1281953886u, 3579855332u, 2724688242u, 1006888145u, 1258607687u, 3524101629u, 2768942443u,
		901097722u, 1119000684u, 3686517206u, 2898065728u, 853044451u, 1172266101u, 3705015759u, 2882616665u, 651767980u, 1373503546u,
		3369554304u, 3218104598u, 565507253u, 1454621731u, 3485111705u, 3099436303u, 671266974u, 1594198024u, 3322730930u, 2970347812u,
		795835527u, 1483230225u, 3244367275u, 3060149565u, 1994146192u, 31158534u, 2563907772u, 4023717930u, 1907459465u, 112637215u,
		2680153253u, 3904427059u, 2013776290u, 251722036u, 2517215374u, 3775830040u, 2137656763u, 141376813u, 2439277719u, 3865271297u,
		1802195444u, 476864866u, 2238001368u, 4066508878u, 1812370925u, 453092731u, 2181625025u, 4111451223u, 1706088902u, 314042704u,
		2344532202u, 4240017532u, 1658658271u, 366619977u, 2362670323u, 4224994405u, 1303535960u, 984961486u, 2747007092u, 3569037538u,
		1256170817u, 1037604311u, 2765210733u, 3554079995u, 1131014506u, 879679996u, 2909243462u, 3663771856u, 1141124467u, 855842277u,
		2852801631u, 3708648649u, 1342533948u, 654459306u, 3188396048u, 3373015174u, 1466479909u, 544179635u, 3110523913u, 3462522015u,
		1591671054u, 702138776u, 2966460450u, 3352799412u, 1504918807u, 783551873u, 3082640443u, 3233442989u, 3988292384u, 2596254646u,
		62317068u, 1957810842u, 3939845945u, 2647816111u, 81470997u, 1943803523u, 3814918930u, 2489596804u, 225274430u, 2053790376u,
		3826175755u, 2466906013u, 167816743u, 2097651377u, 4027552580u, 2265490386u, 503444072u, 1762050814u, 4150417245u, 2154129355u,
		426522225u, 1852507879u, 4275313526u, 2312317920u, 282753626u, 1742555852u, 4189708143u, 2394877945u, 397917763u, 1622183637u,
		3604390888u, 2714866558u, 953729732u, 1340076626u, 3518719985u, 2797360999u, 1068828381u, 1219638859u, 3624741850u, 2936675148u,
		906185462u, 1090812512u, 3747672003u, 2825379669u, 829329135u, 1181335161u, 3412177804u, 3160834842u, 628085408u, 1382605366u,
		3423369109u, 3138078467u, 570562233u, 1426400815u, 3317316542u, 2998733608u, 733239954u, 1555261956u, 3268935591u, 3050360625u,
		752459403u, 1541320221u, 2607071920u, 3965973030u, 1969922972u, 40735498u, 2617837225u, 3943577151u, 1913087877u, 83908371u,
		2512341634u, 3803740692u, 2075208622u, 213261112u, 2463272603u, 3855990285u, 2094854071u, 198958881u, 2262029012u, 4057260610u,
		1759359992u, 534414190u, 2176718541u, 4139329115u, 1873836001u, 414664567u, 2282248934u, 4279200368u, 1711684554u, 285281116u,
		2405801727u, 4167216745u, 1634467795u, 376229701u, 2685067896u, 3608007406u, 1308918612u, 956543938u, 2808555105u, 3495958263u,
		1231636301u, 1047427035u, 2932959818u, 3654703836u, 1088359270u, 936918000u, 2847714899u, 3736837829u, 1202900863u, 817233897u,
		3183342108u, 3401237130u, 1404277552u, 615818150u, 3134207493u, 3453421203u, 1423857449u, 601450431u, 3009837614u, 3294710456u,
		1567103746u, 711928724u, 3020668471u, 3272380065u, 1510334235u, 755167117u
	};

	private static readonly ExifTagId[] InternalTiffTags = new ExifTagId[50]
	{
		ExifTagId.ImageWidth,
		ExifTagId.ImageLength,
		ExifTagId.BitsPerSample,
		ExifTagId.Compression,
		ExifTagId.PhotometricInterpretation,
		ExifTagId.Threshholding,
		ExifTagId.CellWidth,
		ExifTagId.CellLength,
		ExifTagId.FillOrder,
		ExifTagId.StripOffsets,
		ExifTagId.SamplesPerPixel,
		ExifTagId.RowsPerStrip,
		ExifTagId.StripByteCounts,
		ExifTagId.MinSampleValue,
		ExifTagId.MaxSampleValue,
		ExifTagId.PlanarConfiguration,
		ExifTagId.FreeOffsets,
		ExifTagId.FreeByteCounts,
		ExifTagId.GrayResponseUnit,
		ExifTagId.GrayResponseCurve,
		ExifTagId.T4Options,
		ExifTagId.T6Options,
		ExifTagId.TransferFunction,
		ExifTagId.Predictor,
		ExifTagId.WhitePoint,
		ExifTagId.PrimaryChromaticities,
		ExifTagId.ColorMap,
		ExifTagId.HalftoneHints,
		ExifTagId.TileWidth,
		ExifTagId.TileLength,
		ExifTagId.TileOffsets,
		ExifTagId.TileByteCounts,
		ExifTagId.ExtraSamples,
		ExifTagId.SampleFormat,
		ExifTagId.SMinSampleValue,
		ExifTagId.SMaxSampleValue,
		ExifTagId.TransferRange,
		ExifTagId.YCbCrCoefficients,
		ExifTagId.YCbCrSubSampling,
		ExifTagId.YCbCrPositioning,
		ExifTagId.ReferenceBlackWhite,
		(ExifTagId)512,
		ExifTagId.JpegInterchangeFormat,
		ExifTagId.JpegInterchangeFormatLength,
		(ExifTagId)515,
		(ExifTagId)517,
		(ExifTagId)518,
		(ExifTagId)519,
		(ExifTagId)520,
		(ExifTagId)521
	};

	private static BitArray InternalTiffTagsBitArray;

	public int MakerNoteOriginalOffset { get; private set; }

	public ExifByteOrder ByteOrder => _ByteOrder;

	public ExifData(string FileNameWithPath, ExifLoadOptions Options = (ExifLoadOptions)0)
		: this()
	{
		_FileNameWithPath = Path.GetFullPath(FileNameWithPath);
		using FileStream ImageFile = File.OpenRead(_FileNameWithPath);
		ReadFromStream(ImageFile, Options);
	}

	public ExifData(Stream ImageStream, ExifLoadOptions Options = (ExifLoadOptions)0)
		: this()
	{
		ReadFromStream(ImageStream, Options);
	}

	public static ExifData Empty()
	{
		ExifData exifData = new ExifData();
		exifData.SetEmptyExifBlock();
		return exifData;
	}

	public void Save(string DestFileNameWithPath = null, ExifSaveOptions SaveOptions = (ExifSaveOptions)0)
	{
		bool DestFileIsTempFile = false;
		string SourceFileNameWithPath = _FileNameWithPath;
		if (DestFileNameWithPath != null)
		{
			DestFileNameWithPath = Path.GetFullPath(DestFileNameWithPath);
		}
		if (DestFileNameWithPath == null || string.Compare(SourceFileNameWithPath, DestFileNameWithPath, StringComparison.OrdinalIgnoreCase) == 0)
		{
			int BackslashPosition = SourceFileNameWithPath.LastIndexOf('\\');
			int DotPosition = SourceFileNameWithPath.LastIndexOf('.');
			if (DotPosition <= BackslashPosition)
			{
				DotPosition = SourceFileNameWithPath.Length;
			}
			DestFileNameWithPath = SourceFileNameWithPath.Insert(DotPosition, "~");
			DestFileIsTempFile = true;
		}
		bool DestFileCreated = false;
		FileStream SourceFile = null;
		FileStream DestFile = null;
		try
		{
			SourceFile = File.OpenRead(SourceFileNameWithPath);
			DestFile = File.Open(DestFileNameWithPath, FileMode.Create);
			DestFileCreated = true;
			Save(SourceFile, DestFile, SaveOptions);
			SourceFile.Dispose();
			SourceFile = null;
			DestFile.Dispose();
			DestFile = null;
			if (DestFileIsTempFile)
			{
				File.Delete(SourceFileNameWithPath);
				File.Move(DestFileNameWithPath, SourceFileNameWithPath);
			}
		}
		catch
		{
			SourceFile?.Dispose();
			DestFile?.Dispose();
			if (DestFileCreated)
			{
				File.Delete(DestFileNameWithPath);
			}
			throw;
		}
	}

	public void Save(Stream SourceStream, Stream DestStream, ExifSaveOptions SaveOptions = (ExifSaveOptions)0)
	{
		ImageType SourceImageType = CheckStreamTypeAndCompatibility(SourceStream);
		if (SourceImageType != ImageType)
		{
			throw new ExifException(ExifErrCode.ImageTypesDoNotMatch);
		}
		switch (SourceImageType)
		{
		case ImageType.Jpeg:
			SaveJpeg(SourceStream, DestStream);
			break;
		case ImageType.Tiff:
			SaveTiff(SourceStream, DestStream);
			break;
		case ImageType.Png:
			SavePng(SourceStream, DestStream);
			break;
		}
	}

	public bool GetTagValue(ExifTag TagSpec, out string Value, StrCoding Coding)
	{
		bool Success = false;
		StrCodingFormat StringTagFormat = (StrCodingFormat)(Coding & (StrCoding)(-65536));
		ushort CodePage = (ushort)Coding;
		if (StringTagFormat == StrCodingFormat.TypeUndefinedWithIdCode)
		{
			Success = GetTagValueWithIdCode(TagSpec, out Value, CodePage);
		}
		else
		{
			Value = null;
			if (GetTagItem(TagSpec, out var t))
			{
				int i = t.ValueCount;
				int j = t.ValueIndex;
				if (CodePage == 1200 || CodePage == 1201)
				{
					while (i >= 2 && t.ValueData[j + i - 2] == 0 && t.ValueData[j + i - 1] == 0)
					{
						i -= 2;
					}
				}
				else
				{
					while (i >= 1 && t.ValueData[j + i - 1] == 0)
					{
						i--;
					}
				}
				if (CodePage != 0 && ((StringTagFormat == StrCodingFormat.TypeAscii && t.TagType == ExifTagType.Ascii) || (StringTagFormat == StrCodingFormat.TypeUndefined && t.TagType == ExifTagType.Undefined) || (StringTagFormat == StrCodingFormat.TypeByte && t.TagType == ExifTagType.Byte)))
				{
					Value = Encoding.GetEncoding(CodePage).GetString(t.ValueData, j, i);
					Success = true;
				}
			}
		}
		return Success;
	}

	public bool SetTagValue(ExifTag TagSpec, string Value, StrCoding Coding)
	{
		bool Success = false;
		StrCodingFormat StringTagFormat = (StrCodingFormat)(Coding & (StrCoding)(-65536));
		ushort CodePage = (ushort)Coding;
		if (StringTagFormat == StrCodingFormat.TypeUndefinedWithIdCode)
		{
			Success = SetTagValueWithIdCode(TagSpec, Value, CodePage);
		}
		else
		{
			ExifTagType TagType = StringTagFormat switch
			{
				StrCodingFormat.TypeUndefined => ExifTagType.Undefined, 
				StrCodingFormat.TypeByte => ExifTagType.Byte, 
				_ => ExifTagType.Ascii, 
			};
			int NullTermBytes = 0;
			if (TagType != ExifTagType.Undefined)
			{
				NullTermBytes = ((CodePage != 1200 && CodePage != 1201) ? 1 : 2);
			}
			if (CodePage != 0)
			{
				byte[] StringAsByteArray = Encoding.GetEncoding(CodePage).GetBytes(Value);
				int StrByteLen = StringAsByteArray.Length;
				int TotalByteCount = StrByteLen + NullTermBytes;
				TagItem t = PrepareTagForCompleteWriting(TagSpec, TagType, TotalByteCount);
				if (t != null)
				{
					Array.Copy(StringAsByteArray, 0, t.ValueData, t.ValueIndex, StrByteLen);
					if (NullTermBytes >= 1)
					{
						t.ValueData[t.ValueIndex + StrByteLen] = 0;
					}
					if (NullTermBytes >= 2)
					{
						t.ValueData[t.ValueIndex + StrByteLen + 1] = 0;
					}
					Success = true;
				}
			}
		}
		return Success;
	}

	public bool GetTagValue(ExifTag TagSpec, out int Value, int Index = 0)
	{
		bool Success = false;
		uint TempValue = 0u;
		if (GetTagItem(TagSpec, out var t) && ReadUintElement(t, Index, out TempValue))
		{
			Success = t.TagType != ExifTagType.ULong || (int)TempValue >= 0;
			if (!Success)
			{
				TempValue = 0u;
			}
		}
		Value = (int)TempValue;
		return Success;
	}

	public bool SetTagValue(ExifTag TagSpec, int Value, ExifTagType TagType, int Index = 0)
	{
		bool Success = false;
		switch (TagType)
		{
		case ExifTagType.Byte:
			if (Value >= 0 && Value <= 255)
			{
				Success = true;
			}
			break;
		case ExifTagType.UShort:
			if (Value >= 0 && Value <= 65535)
			{
				Success = true;
			}
			break;
		case ExifTagType.ULong:
			if (Value >= 0)
			{
				Success = true;
			}
			break;
		case ExifTagType.SLong:
			Success = true;
			break;
		}
		if (Success)
		{
			TagItem t = PrepareTagForArrayItemWriting(TagSpec, TagType, Index);
			Success = WriteUintElement(t, Index, (uint)Value);
		}
		return Success;
	}

	public bool GetTagValue(ExifTag TagSpec, out uint Value, int Index = 0)
	{
		bool Success = false;
		uint TempValue = 0u;
		if (GetTagItem(TagSpec, out var t) && ReadUintElement(t, Index, out TempValue))
		{
			Success = t.TagType != ExifTagType.SLong || (int)TempValue >= 0;
			if (!Success)
			{
				TempValue = 0u;
			}
		}
		Value = TempValue;
		return Success;
	}

	public bool SetTagValue(ExifTag TagSpec, uint Value, ExifTagType TagType, int Index = 0)
	{
		bool Success = false;
		switch (TagType)
		{
		case ExifTagType.Byte:
			if (Value >= 0 && Value <= 255)
			{
				Success = true;
			}
			break;
		case ExifTagType.UShort:
			if (Value >= 0 && Value <= 65535)
			{
				Success = true;
			}
			break;
		case ExifTagType.ULong:
			Success = true;
			break;
		case ExifTagType.SLong:
			if ((int)Value >= 0)
			{
				Success = true;
			}
			break;
		}
		if (Success)
		{
			TagItem t = PrepareTagForArrayItemWriting(TagSpec, TagType, Index);
			Success = WriteUintElement(t, Index, Value);
		}
		return Success;
	}

	public bool GetTagValue(ExifTag TagSpec, out ExifRational Value, int Index = 0)
	{
		bool Success = false;
		if (GetTagItem(TagSpec, out var t) && ReadURatElement(t, Index, out var Numer, out var Denom))
		{
			if (t.TagType == ExifTagType.URational)
			{
				Value = new ExifRational(Numer, Denom);
			}
			else
			{
				Value = new ExifRational((int)Numer, (int)Denom);
			}
			Success = true;
		}
		else
		{
			Value = new ExifRational(0, 0);
		}
		return Success;
	}

	public bool SetTagValue(ExifTag TagSpec, ExifRational Value, ExifTagType TagType, int Index = 0)
	{
		bool Success = false;
		switch (TagType)
		{
		case ExifTagType.SRational:
			if (Value.Numer < 2147483648u && Value.Denom < 2147483648u)
			{
				if (Value.Sign)
				{
					Value.Numer = 0 - Value.Numer;
				}
				Success = true;
			}
			else
			{
				Success = false;
			}
			break;
		case ExifTagType.URational:
			Success = !Value.IsNegative();
			break;
		}
		if (Success)
		{
			TagItem t = PrepareTagForArrayItemWriting(TagSpec, TagType, Index);
			Success = WriteURatElement(t, Index, Value.Numer, Value.Denom);
		}
		return Success;
	}

	public bool GetTagValue(ExifTag TagSpec, out DateTime Value, ExifDateFormat Format = ExifDateFormat.DateAndTime)
	{
		bool Success = false;
		Value = DateTime.MinValue;
		if (GetTagItem(TagSpec, out var t) && t.TagType == ExifTagType.Ascii)
		{
			try
			{
				int i = t.ValueIndex;
				if (t.ValueCount >= 10 && t.ValueData[i + 4] == 58 && t.ValueData[i + 7] == 58)
				{
					int Year = CalculateTwoDigitDecNumber(t.ValueData, i) * 100 + CalculateTwoDigitDecNumber(t.ValueData, i + 2);
					int Month = CalculateTwoDigitDecNumber(t.ValueData, i + 5);
					int Day = CalculateTwoDigitDecNumber(t.ValueData, i + 8);
					if (Format == ExifDateFormat.DateAndTime && t.ValueCount == 20 && t.ValueData[i + 10] == 32 && t.ValueData[i + 13] == 58 && t.ValueData[i + 16] == 58 && t.ValueData[i + 19] == 0)
					{
						int Hour = CalculateTwoDigitDecNumber(t.ValueData, i + 11);
						int Minute = CalculateTwoDigitDecNumber(t.ValueData, i + 14);
						int Second = CalculateTwoDigitDecNumber(t.ValueData, i + 17);
						Value = new DateTime(Year, Month, Day, Hour, Minute, Second);
						Success = true;
					}
					else if (Format == ExifDateFormat.DateOnly && t.ValueCount == 11 && t.ValueData[i + 10] == 0)
					{
						Value = new DateTime(Year, Month, Day);
						Success = true;
					}
				}
			}
			catch
			{
			}
		}
		return Success;
	}

	public bool SetTagValue(ExifTag TagSpec, DateTime Value, ExifDateFormat Format = ExifDateFormat.DateAndTime)
	{
		bool Success = false;
		int sByteCount = 0;
		switch (Format)
		{
		case ExifDateFormat.DateAndTime:
			sByteCount = 20;
			break;
		case ExifDateFormat.DateOnly:
			sByteCount = 11;
			break;
		}
		if (sByteCount != 0)
		{
			TagItem t = PrepareTagForCompleteWriting(TagSpec, ExifTagType.Ascii, sByteCount);
			if (t != null)
			{
				int i = t.ValueIndex;
				int Year = Value.Year;
				ConvertTwoDigitNumberToByteArr(t.ValueData, ref i, Year / 100);
				ConvertTwoDigitNumberToByteArr(t.ValueData, ref i, Year % 100);
				t.ValueData[i] = 58;
				i++;
				ConvertTwoDigitNumberToByteArr(t.ValueData, ref i, Value.Month);
				t.ValueData[i] = 58;
				i++;
				ConvertTwoDigitNumberToByteArr(t.ValueData, ref i, Value.Day);
				if (Format == ExifDateFormat.DateAndTime)
				{
					t.ValueData[i] = 32;
					i++;
					ConvertTwoDigitNumberToByteArr(t.ValueData, ref i, Value.Hour);
					t.ValueData[i] = 58;
					i++;
					ConvertTwoDigitNumberToByteArr(t.ValueData, ref i, Value.Minute);
					t.ValueData[i] = 58;
					i++;
					ConvertTwoDigitNumberToByteArr(t.ValueData, ref i, Value.Second);
				}
				t.ValueData[i] = 0;
				i++;
				Success = true;
			}
		}
		return Success;
	}

	public bool GetTagRawData(ExifTag TagSpec, out ExifTagType TagType, out int ValueCount, out byte[] RawData, out int RawDataIndex)
	{
		bool Success = false;
		if (GetTagItem(TagSpec, out var t))
		{
			TagType = t.TagType;
			ValueCount = t.ValueCount;
			RawData = t.ValueData;
			RawDataIndex = t.ValueIndex;
			Success = true;
		}
		else
		{
			TagType = (ExifTagType)0;
			ValueCount = 0;
			RawData = null;
			RawDataIndex = 0;
		}
		return Success;
	}

	public bool GetTagRawData(ExifTag TagSpec, out ExifTagType TagType, out int ValueCount, out byte[] RawData)
	{
		bool Success = false;
		if (GetTagItem(TagSpec, out var t))
		{
			TagType = t.TagType;
			ValueCount = t.ValueCount;
			int i = t.ByteCount;
			RawData = new byte[i];
			Array.Copy(t.ValueData, t.ValueIndex, RawData, 0, i);
			Success = true;
		}
		else
		{
			TagType = (ExifTagType)0;
			ValueCount = 0;
			RawData = null;
		}
		return Success;
	}

	public bool SetTagRawData(ExifTag TagSpec, ExifTagType TagType, int ValueCount, byte[] RawData, int RawDataIndex = 0)
	{
		bool Success = false;
		ExifIfd Ifd = ExtractIfd(TagSpec);
		if ((uint)Ifd < 5u)
		{
			int RawDataByteCount = GetTagByteCount(TagType, ValueCount);
			Dictionary<ExifTagId, TagItem> IfdTagTable = TagTable[(int)Ifd];
			if (IfdTagTable.TryGetValue(ExtractTagId(TagSpec), out var t))
			{
				t.TagType = TagType;
				t.ValueCount = ValueCount;
				t.ValueData = RawData;
				t.ValueIndex = RawDataIndex;
				t.AllocatedByteCount = RawDataByteCount;
			}
			else
			{
				t = new TagItem(ExtractTagId(TagSpec), TagType, ValueCount, RawData, RawDataIndex, RawDataByteCount);
				IfdTagTable.Add(t.TagId, t);
			}
			Success = true;
		}
		return Success;
	}

	public bool GetTagValueCount(ExifTag TagSpec, out int ValueCount)
	{
		if (GetTagItem(TagSpec, out var t))
		{
			ValueCount = t.ValueCount;
			return true;
		}
		ValueCount = 0;
		return false;
	}

	public bool SetTagValueCount(ExifTag TagSpec, int ValueCount)
	{
		if (GetTagItem(TagSpec, out var t) && (uint)ValueCount <= 268435455u)
		{
			t.SetTagTypeAndValueCount(t.TagType, ValueCount, KeepExistingData: true);
			return true;
		}
		return false;
	}

	public bool SetTagValueCount(ExifTag TagSpec, int ValueCount, ExifTagType TagType)
	{
		return PrepareTagForArrayItemWriting(TagSpec, TagType, ValueCount - 1) != null;
	}

	public bool GetTagType(ExifTag TagSpec, out ExifTagType TagType)
	{
		if (GetTagItem(TagSpec, out var t))
		{
			TagType = t.TagType;
			return true;
		}
		TagType = (ExifTagType)0;
		return false;
	}

	public bool TagExists(ExifTag TagSpec)
	{
		ExifIfd Ifd = ExtractIfd(TagSpec);
		if ((uint)Ifd < 5u)
		{
			return TagTable[(int)Ifd].ContainsKey(ExtractTagId(TagSpec));
		}
		return false;
	}

	public bool IfdExists(ExifIfd Ifd)
	{
		if ((uint)Ifd < 5u)
		{
			return TagTable[(int)Ifd].Count > 0;
		}
		return false;
	}

	public bool ImageFileBlockExists(ImageFileBlock BlockType)
	{
		return ImageFileBlockInfo[(int)BlockType] == ImageFileBlockState.Existent;
	}

	public bool RemoveTag(ExifTag TagSpec)
	{
		ExifIfd Ifd = ExtractIfd(TagSpec);
		if ((uint)Ifd < 5u)
		{
			return TagTable[(int)Ifd].Remove(ExtractTagId(TagSpec));
		}
		return false;
	}

	public bool RemoveAllTagsFromIfd(ExifIfd Ifd)
	{
		bool Removed = false;
		switch (Ifd)
		{
		case ExifIfd.PrimaryData:
			RemoveAllTagsFromIfdPrimaryData();
			Removed = true;
			break;
		case ExifIfd.PrivateData:
		case ExifIfd.GpsInfoData:
		case ExifIfd.Interoperability:
		case ExifIfd.ThumbnailData:
			ClearIfd_Unchecked(Ifd);
			if (Ifd == ExifIfd.ThumbnailData)
			{
				RemoveThumbnailImage(RemoveAlsoThumbnailTags: false);
			}
			Removed = true;
			break;
		}
		return Removed;
	}

	public void RemoveAllTags()
	{
		RemoveAllTagsFromIfdPrimaryData();
		for (ExifIfd Ifd = ExifIfd.PrivateData; Ifd < (ExifIfd)5; Ifd++)
		{
			ClearIfd_Unchecked(Ifd);
		}
		RemoveThumbnailImage(RemoveAlsoThumbnailTags: false);
		ImageFileBlockInfo[1] = ImageFileBlockState.Removed;
	}

	public void RemoveImageFileBlock(ImageFileBlock BlockType)
	{
		switch (BlockType)
		{
		case ImageFileBlock.Exif:
			RemoveAllTags();
			return;
		case ImageFileBlock.Unknown:
			return;
		}
		if (ImageType == ImageType.Tiff)
		{
			switch (BlockType)
			{
			case ImageFileBlock.Xmp:
				RemoveTag(ExifTag.XmpMetadata);
				break;
			case ImageFileBlock.Iptc:
				RemoveTag(ExifTag.IptcMetadata);
				break;
			}
		}
		ImageFileBlockInfo[(int)BlockType] = ImageFileBlockState.Removed;
	}

	public void ReplaceAllTagsBy(ExifData SourceExifData)
	{
		bool SwapByteOrder = false;
		if (ImageType == ImageType.Tiff)
		{
			if (_ByteOrder != SourceExifData._ByteOrder)
			{
				SwapByteOrder = true;
			}
		}
		else
		{
			_ByteOrder = SourceExifData._ByteOrder;
		}
		RemoveAllTags();
		Dictionary<ExifTagId, TagItem> obj = SourceExifData.TagTable[0];
		Dictionary<ExifTagId, TagItem> DestIfdTagList = TagTable[0];
		bool IsTiffImagePresent = ImageType == ImageType.Tiff || SourceExifData.ImageType == ImageType.Tiff;
		bool CopyTag = true;
		foreach (TagItem SourceTag3 in obj.Values)
		{
			if (IsTiffImagePresent)
			{
				CopyTag = !IsInternalTiffTag(SourceTag3.TagId) && !IsMetaDataTiffTag(SourceTag3.TagId);
			}
			if (CopyTag)
			{
				TagItem DestTag3 = CopyTagItemDeeply(ExifIfd.PrimaryData, SourceTag3, SwapByteOrder);
				AddIfNotExists(DestIfdTagList, DestTag3);
			}
		}
		ExifIfd[] array = new ExifIfd[3]
		{
			ExifIfd.PrivateData,
			ExifIfd.GpsInfoData,
			ExifIfd.Interoperability
		};
		foreach (ExifIfd Ifd in array)
		{
			Dictionary<ExifTagId, TagItem> obj2 = SourceExifData.TagTable[(int)Ifd];
			DestIfdTagList = TagTable[(int)Ifd];
			foreach (TagItem SourceTag2 in obj2.Values)
			{
				TagItem DestTag2 = CopyTagItemDeeply(Ifd, SourceTag2, SwapByteOrder);
				AddIfNotExists(DestIfdTagList, DestTag2);
			}
		}
		if (ImageType == ImageType.Jpeg)
		{
			Dictionary<ExifTagId, TagItem> obj3 = SourceExifData.TagTable[4];
			DestIfdTagList = TagTable[4];
			foreach (TagItem SourceTag in obj3.Values)
			{
				TagItem DestTag = CopyTagItemDeeply(ExifIfd.ThumbnailData, SourceTag, SwapByteOrder);
				AddIfNotExists(DestIfdTagList, DestTag);
			}
			if (SourceExifData.ThumbnailImageExists())
			{
				ThumbnailStartIndex = 0;
				ThumbnailByteCount = SourceExifData.ThumbnailByteCount;
				ThumbnailImage = new byte[ThumbnailByteCount];
				Array.Copy(SourceExifData.ThumbnailImage, SourceExifData.ThumbnailStartIndex, ThumbnailImage, 0, ThumbnailByteCount);
			}
			else
			{
				RemoveThumbnailImage_Internal();
			}
		}
		UpdateImageFileBlockInfo();
		MakerNoteOriginalOffset = SourceExifData.MakerNoteOriginalOffset;
	}

	public bool ThumbnailImageExists()
	{
		return ThumbnailImage != null;
	}

	public bool GetThumbnailImage(out byte[] ThumbnailData, out int ThumbnailIndex, out int ThumbnailByteCount)
	{
		ThumbnailData = ThumbnailImage;
		ThumbnailIndex = ThumbnailStartIndex;
		ThumbnailByteCount = this.ThumbnailByteCount;
		return ThumbnailImageExists();
	}

	public bool SetThumbnailImage(byte[] ThumbnailData, int ThumbnailIndex = 0, int ThumbnailByteCount = -1)
	{
		bool Success = false;
		if (ThumbnailData != null)
		{
			ThumbnailImage = ThumbnailData;
			ThumbnailStartIndex = ThumbnailIndex;
			if (ThumbnailByteCount < 0)
			{
				this.ThumbnailByteCount = ThumbnailData.Length - ThumbnailIndex;
			}
			else
			{
				this.ThumbnailByteCount = ThumbnailByteCount;
			}
			SetTagValue(ExifTag.JpegInterchangeFormat, 0, ExifTagType.ULong);
			SetTagValue(ExifTag.JpegInterchangeFormatLength, this.ThumbnailByteCount, ExifTagType.ULong);
			Success = true;
		}
		return Success;
	}

	public void RemoveThumbnailImage(bool RemoveAlsoThumbnailTags)
	{
		RemoveThumbnailImage_Internal();
		if (RemoveAlsoThumbnailTags)
		{
			ClearIfd_Unchecked(ExifIfd.ThumbnailData);
			return;
		}
		RemoveTag(ExifTag.JpegInterchangeFormat);
		RemoveTag(ExifTag.JpegInterchangeFormatLength);
	}

	public static ExifIfd ExtractIfd(ExifTag TagSpec)
	{
		return (ExifIfd)((uint)TagSpec >> 16);
	}

	public static ExifTagId ExtractTagId(ExifTag TagSpec)
	{
		return (ExifTagId)(ushort)TagSpec;
	}

	public static ExifTag ComposeTagSpec(ExifIfd Ifd, ExifTagId TagId)
	{
		return (ExifTag)(((int)Ifd << 16) | (int)TagId);
	}

	public static int GetTagByteCount(ExifTagType TagType, int ValueCount)
	{
		if ((uint)TagType < 13u)
		{
			return TypeByteCount[(int)TagType] * ValueCount;
		}
		return 0;
	}

	public ushort ExifReadUInt16(byte[] Data, int StartIndex)
	{
		if (_ByteOrder == ExifByteOrder.BigEndian)
		{
			return (ushort)((Data[StartIndex] << 8) | Data[StartIndex + 1]);
		}
		return (ushort)((Data[StartIndex + 1] << 8) | Data[StartIndex]);
	}

	public void ExifWriteUInt16(byte[] Data, int StartIndex, ushort Value)
	{
		if (_ByteOrder == ExifByteOrder.BigEndian)
		{
			Data[StartIndex] = (byte)(Value >> 8);
			Data[StartIndex + 1] = (byte)Value;
		}
		else
		{
			Data[StartIndex + 1] = (byte)(Value >> 8);
			Data[StartIndex] = (byte)Value;
		}
	}

	public uint ExifReadUInt32(byte[] Data, int StartIndex)
	{
		if (_ByteOrder == ExifByteOrder.BigEndian)
		{
			return (uint)((Data[StartIndex] << 24) | (Data[StartIndex + 1] << 16) | (Data[StartIndex + 2] << 8) | Data[StartIndex + 3]);
		}
		return (uint)((Data[StartIndex + 3] << 24) | (Data[StartIndex + 2] << 16) | (Data[StartIndex + 1] << 8) | Data[StartIndex]);
	}

	public void ExifWriteUInt32(byte[] Data, int StartIndex, uint Value)
	{
		if (_ByteOrder == ExifByteOrder.BigEndian)
		{
			Data[StartIndex] = (byte)(Value >> 24);
			Data[StartIndex + 1] = (byte)(Value >> 16);
			Data[StartIndex + 2] = (byte)(Value >> 8);
			Data[StartIndex + 3] = (byte)Value;
		}
		else
		{
			Data[StartIndex + 3] = (byte)(Value >> 24);
			Data[StartIndex + 2] = (byte)(Value >> 16);
			Data[StartIndex + 1] = (byte)(Value >> 8);
			Data[StartIndex] = (byte)Value;
		}
	}

	private ushort ReadUInt16BE(byte[] Data, int StartIndex)
	{
		return (ushort)((Data[StartIndex] << 8) | Data[StartIndex + 1]);
	}

	private uint ReadUInt32BE(byte[] Data, int StartIndex)
	{
		return (uint)((Data[StartIndex] << 24) | (Data[StartIndex + 1] << 16) | (Data[StartIndex + 2] << 8) | Data[StartIndex + 3]);
	}

	private void WriteUInt16BE(byte[] Data, int StartIndex, ushort Value)
	{
		Data[StartIndex] = (byte)(Value >> 8);
		Data[StartIndex + 1] = (byte)Value;
	}

	private void WriteUInt32BE(byte[] Data, int StartIndex, uint Value)
	{
		Data[StartIndex] = (byte)(Value >> 24);
		Data[StartIndex + 1] = (byte)(Value >> 16);
		Data[StartIndex + 2] = (byte)(Value >> 8);
		Data[StartIndex + 3] = (byte)Value;
	}

	public bool InitTagEnumeration(ExifIfd Ifd)
	{
		if ((uint)Ifd < 5u)
		{
			ExifIfdForTagEnumeration = Ifd;
			TagEnumerator = TagTable[(int)Ifd].Keys.GetEnumerator();
			return true;
		}
		TagEnumerator = default(Dictionary<ExifTagId, TagItem>.KeyCollection.Enumerator);
		return false;
	}

	public bool EnumerateNextTag(out ExifTag TagSpec)
	{
		bool Success = false;
		if (TagEnumerator.MoveNext())
		{
			ExifTagId TagId = TagEnumerator.Current;
			TagSpec = ComposeTagSpec(ExifIfdForTagEnumeration, TagId);
			Success = true;
		}
		else
		{
			TagSpec = (ExifTag)0;
		}
		return Success;
	}

	public bool GetDateTaken(out DateTime Value)
	{
		return GetDateAndTimeWithMillisecHelper(out Value, ExifTag.DateTimeOriginal, ExifTag.SubsecTimeOriginal);
	}

	public bool SetDateTaken(DateTime Value)
	{
		return SetDateAndTimeWithMillisecHelper(Value, ExifTag.DateTimeOriginal, ExifTag.SubsecTimeOriginal);
	}

	public void RemoveDateTaken()
	{
		RemoveTag(ExifTag.DateTimeOriginal);
		RemoveTag(ExifTag.SubsecTimeOriginal);
	}

	public bool GetDateDigitized(out DateTime Value)
	{
		return GetDateAndTimeWithMillisecHelper(out Value, ExifTag.DateTimeDigitized, ExifTag.SubsecTimeDigitized);
	}

	public bool SetDateDigitized(DateTime Value)
	{
		return SetDateAndTimeWithMillisecHelper(Value, ExifTag.DateTimeDigitized, ExifTag.SubsecTimeDigitized);
	}

	public void RemoveDateDigitized()
	{
		RemoveTag(ExifTag.DateTimeDigitized);
		RemoveTag(ExifTag.SubsecTimeDigitized);
	}

	public bool GetDateChanged(out DateTime Value)
	{
		return GetDateAndTimeWithMillisecHelper(out Value, ExifTag.DateTime, ExifTag.SubsecTime);
	}

	public bool SetDateChanged(DateTime Value)
	{
		return SetDateAndTimeWithMillisecHelper(Value, ExifTag.DateTime, ExifTag.SubsecTime);
	}

	public void RemoveDateChanged()
	{
		RemoveTag(ExifTag.DateTime);
		RemoveTag(ExifTag.SubsecTime);
	}

	public bool GetGpsLongitude(out GeoCoordinate Value)
	{
		return GetGpsCoordinateHelper(out Value, ExifTag.GpsLongitude, ExifTag.GpsLongitudeRef, 'W', 'E');
	}

	public bool SetGpsLongitude(GeoCoordinate Value)
	{
		return SetGpsCoordinateHelper(Value, ExifTag.GpsLongitude, ExifTag.GpsLongitudeRef, 'W', 'E');
	}

	public void RemoveGpsLongitude()
	{
		RemoveTag(ExifTag.GpsLongitude);
		RemoveTag(ExifTag.GpsLongitudeRef);
	}

	public bool GetGpsLatitude(out GeoCoordinate Value)
	{
		return GetGpsCoordinateHelper(out Value, ExifTag.GpsLatitude, ExifTag.GpsLatitudeRef, 'N', 'S');
	}

	public bool SetGpsLatitude(GeoCoordinate Value)
	{
		return SetGpsCoordinateHelper(Value, ExifTag.GpsLatitude, ExifTag.GpsLatitudeRef, 'N', 'S');
	}

	public void RemoveGpsLatitude()
	{
		RemoveTag(ExifTag.GpsLatitude);
		RemoveTag(ExifTag.GpsLatitudeRef);
	}

	public bool GetGpsAltitude(out decimal Value)
	{
		bool Success = false;
		if (GetTagValue(ExifTag.GpsAltitude, out ExifRational AltitudeRat, 0) && AltitudeRat.IsValid())
		{
			Value = ExifRational.ToDecimal(AltitudeRat);
			if (GetTagValue(ExifTag.GpsAltitudeRef, out uint BelowSeaLevel, 0) && BelowSeaLevel == 1)
			{
				Value = -Value;
			}
			Success = true;
		}
		else
		{
			Value = default(decimal);
		}
		return Success;
	}

	public bool SetGpsAltitude(decimal Value)
	{
		bool Success = false;
		ExifRational AltitudeRat = ExifRational.FromDecimal(Value);
		uint BelowSeaLevel = 0u;
		if (AltitudeRat.IsNegative())
		{
			BelowSeaLevel = 1u;
			AltitudeRat.Sign = false;
		}
		if (SetTagValue(ExifTag.GpsAltitude, AltitudeRat, ExifTagType.URational) && SetTagValue(ExifTag.GpsAltitudeRef, BelowSeaLevel, ExifTagType.Byte))
		{
			Success = true;
		}
		return Success;
	}

	public void RemoveGpsAltitude()
	{
		RemoveTag(ExifTag.GpsAltitude);
		RemoveTag(ExifTag.GpsAltitudeRef);
	}

	public bool GetGpsDateTimeStamp(out DateTime Value)
	{
		bool Success = false;
		if (GetTagValue(ExifTag.GpsDateStamp, out Value, ExifDateFormat.DateOnly))
		{
			if (GetTagValue(ExifTag.GpsTimeStamp, out ExifRational Hour, 0) && !Hour.IsNegative() && Hour.IsValid() && GetTagValue(ExifTag.GpsTimeStamp, out ExifRational Min, 1) && !Min.IsNegative() && Min.IsValid() && GetTagValue(ExifTag.GpsTimeStamp, out ExifRational Sec, 2) && !Sec.IsNegative() && Sec.IsValid())
			{
				Value = Value.AddHours((double)Hour.Numer / (double)Hour.Denom);
				Value = Value.AddMinutes((double)Min.Numer / (double)Min.Denom);
				double ms = Math.Truncate((double)Sec.Numer * 1000.0 / (double)Sec.Denom);
				Value = Value.AddMilliseconds(ms);
				Success = true;
			}
			else
			{
				Value = DateTime.MinValue;
			}
		}
		else
		{
			Value = DateTime.MinValue;
		}
		return Success;
	}

	public bool SetGpsDateTimeStamp(DateTime Value)
	{
		bool Success = false;
		if (SetTagValue(ExifTag.GpsDateStamp, Value.Date, ExifDateFormat.DateOnly))
		{
			TimeSpan ts = Value.TimeOfDay;
			ExifRational Hour = new ExifRational(ts.Hours, 1);
			ExifRational Min = new ExifRational(ts.Minutes, 1);
			int ms = ts.Milliseconds;
			ExifRational Sec = ((ms != 0) ? new ExifRational(ts.Seconds * 1000 + ms, 1000) : new ExifRational(ts.Seconds, 1));
			if (SetTagValue(ExifTag.GpsTimeStamp, Hour, ExifTagType.URational) && SetTagValue(ExifTag.GpsTimeStamp, Min, ExifTagType.URational, 1) && SetTagValue(ExifTag.GpsTimeStamp, Sec, ExifTagType.URational, 2))
			{
				Success = true;
			}
		}
		return Success;
	}

	public void RemoveGpsDateTimeStamp()
	{
		RemoveTag(ExifTag.GpsDateStamp);
		RemoveTag(ExifTag.GpsTimeStamp);
	}

	private ExifData()
	{
		_ByteOrder = ExifByteOrder.BigEndian;
		ImageFileBlockInfo = new ImageFileBlockState[ImageFileBlockCount];
	}

	private void ReadFromStream(Stream ImageStream, ExifLoadOptions Options)
	{
		SourceExifStream = ImageStream;
		ErrCodeForIllegalExifBlock = ExifErrCode.ExifBlockHasIllegalContent;
		ImageType = CheckStreamTypeAndCompatibility(SourceExifStream);
		if (Options.HasFlag(ExifLoadOptions.CreateEmptyBlock))
		{
			SetEmptyExifBlock();
		}
		else
		{
			if (ImageType == ImageType.Jpeg)
			{
				ReadJepg();
			}
			else if (ImageType == ImageType.Tiff)
			{
				ReadTiff();
			}
			else if (ImageType == ImageType.Png)
			{
				ReadPng();
			}
			UpdateImageFileBlockInfo();
		}
		SourceExifBlock = null;
		SourceExifStream = null;
	}

	private void ReadJepg()
	{
		byte[] BlockContent = new byte[65536];
		bool ExifBlockFound = false;
		SourceExifStream.Position = 2L;
		ushort BlockMarker;
		do
		{
			ReadJpegBlock(SourceExifStream, BlockContent, out var BlockContentSize, out BlockMarker, out var BlockType, out var MetaDataIndex);
			if (BlockType != 0)
			{
				if (BlockType == ImageFileBlock.Exif && !ExifBlockFound)
				{
					ExifBlockFound = true;
					int TiffHeaderAndExifBlockLen = BlockContentSize - MetaDataIndex;
					SourceExifBlock = new byte[TiffHeaderAndExifBlockLen];
					Array.Copy(BlockContent, MetaDataIndex, SourceExifBlock, 0, TiffHeaderAndExifBlockLen);
					EvaluateTiffHeader(SourceExifBlock, TiffHeaderAndExifBlockLen, out var IfdPrimaryDataOffset);
					EvaluateExifBlock(IfdPrimaryDataOffset);
				}
				ImageFileBlockInfo[(int)BlockType] = ImageFileBlockState.Existent;
			}
		}
		while (BlockMarker != 65498);
		if (!ExifBlockFound)
		{
			SetEmptyExifBlock();
		}
	}

	private void ReadTiff()
	{
		byte[] BlockContent = new byte[8];
		SourceExifStream.Position = 0L;
		SourceExifBlock = null;
		ErrCodeForIllegalExifBlock = ExifErrCode.InternalImageStructureIsWrong;
		int TiffHeaderBytesRead = SourceExifStream.Read(BlockContent, 0, 8);
		EvaluateTiffHeader(BlockContent, TiffHeaderBytesRead, out var IfdPrimaryDataOffset);
		EvaluateExifBlock(IfdPrimaryDataOffset);
	}

	private void ReadPng()
	{
		byte[] TempData = new byte[65536];
		byte[] BlockContent = new byte[30];
		bool ExifBlockFound = false;
		SourceExifStream.Position = 4L;
		if (SourceExifStream.Read(TempData, 0, 4) < 4 || ReadUInt32BE(TempData, 0) != 218765834)
		{
			throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
		}
		uint ChunkType;
		do
		{
			ReadPngBlockHeader(SourceExifStream, TempData, out var DataLength, out ChunkType);
			ImageFileBlock BlockType;
			int BytesRead = DetectPngImageBlock(SourceExifStream, BlockContent, ChunkType, out BlockType);
			if (BlockType != 0)
			{
				if (BlockType == ImageFileBlock.Exif && !ExifBlockFound)
				{
					ExifBlockFound = true;
					SourceExifBlock = new byte[DataLength];
					if (SourceExifStream.Read(SourceExifBlock, 0, DataLength) < DataLength)
					{
						throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
					}
					uint Crc32Calculated = CalculateCrc32(TempData, 4, 4, Finalize: false);
					Crc32Calculated = CalculateCrc32(SourceExifBlock, 0, DataLength, Finalize: true, Crc32Calculated);
					int num = SourceExifStream.Read(TempData, 0, 4);
					uint Crc32Loaded = ReadUInt32BE(TempData, 0);
					if (num < 4 || Crc32Calculated != Crc32Loaded)
					{
						throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
					}
					EvaluateTiffHeader(SourceExifBlock, DataLength, out var IfdPrimaryDataOffset);
					EvaluateExifBlock(IfdPrimaryDataOffset);
					BytesRead = DataLength + 4;
				}
				ImageFileBlockInfo[(int)BlockType] = ImageFileBlockState.Existent;
			}
			SourceExifStream.Position += DataLength + 4 - BytesRead;
		}
		while (ChunkType != 1229278788);
		if (!ExifBlockFound)
		{
			SetEmptyExifBlock();
		}
	}

	private void ReadPngBlockHeader(Stream ImageStream, byte[] TempData, out int BlockLength, out uint ChunkType)
	{
		if (ImageStream.Read(TempData, 0, 8) < 8)
		{
			throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
		}
		BlockLength = (int)ReadUInt32BE(TempData, 0);
		if (BlockLength < 0)
		{
			throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
		}
		ChunkType = ReadUInt32BE(TempData, 4);
	}

	private int DetectPngImageBlock(Stream ImageStream, byte[] SignatureData, uint ChunkType, out ImageFileBlock BlockType)
	{
		int BytesRead = 0;
		BlockType = ImageFileBlock.Unknown;
		switch (ChunkType)
		{
		case 1700284774u:
			BlockType = ImageFileBlock.Exif;
			break;
		case 1767135348u:
		{
			int BytesToRead = PngIptcSignature.Length;
			BytesRead = ImageStream.Read(SignatureData, 0, BytesToRead);
			if (ArrayStartsWith(SignatureData, BytesRead, PngXmpSignature))
			{
				BlockType = ImageFileBlock.Xmp;
			}
			else if (ArrayStartsWith(SignatureData, BytesRead, PngIptcSignature))
			{
				BlockType = ImageFileBlock.Iptc;
			}
			break;
		}
		case 1950701684u:
			BlockType = ImageFileBlock.PngMetaData;
			break;
		case 1950960965u:
			BlockType = ImageFileBlock.PngDateChanged;
			break;
		}
		return BytesRead;
	}

	private static uint CalculateCrc32(byte[] Data, int StartIndex, int Length, bool Finalize = true, uint StartCrc = uint.MaxValue)
	{
		uint result = StartCrc;
		int EndIndexPlus1 = StartIndex + Length;
		for (int i = StartIndex; i < EndIndexPlus1; i++)
		{
			byte value = Data[i];
			result = Crc32ChecksumTable[(byte)result ^ value] ^ (result >> 8);
		}
		if (Finalize)
		{
			result = ~result;
		}
		return result;
	}

	private void UpdateImageFileBlockInfo()
	{
		ImageFileBlockState NewExifBlockState = ImageFileBlockState.NonExistent;
		Dictionary<ExifTagId, TagItem> PrimaryDataIfdTable = TagTable[0];
		foreach (ExifTagId tid in PrimaryDataIfdTable.Keys)
		{
			if (ImageType == ImageType.Tiff)
			{
				if (!IsInternalTiffTag(tid) && !IsMetaDataTiffTag(tid))
				{
					NewExifBlockState = ImageFileBlockState.Existent;
					break;
				}
				continue;
			}
			NewExifBlockState = ImageFileBlockState.Existent;
			break;
		}
		if (NewExifBlockState == ImageFileBlockState.NonExistent)
		{
			ExifIfd[] array = new ExifIfd[4]
			{
				ExifIfd.PrivateData,
				ExifIfd.GpsInfoData,
				ExifIfd.Interoperability,
				ExifIfd.ThumbnailData
			};
			foreach (ExifIfd Ifd in array)
			{
				if (TagTable[(int)Ifd].Count > 0)
				{
					NewExifBlockState = ImageFileBlockState.Existent;
					break;
				}
			}
		}
		ImageFileBlockInfo[1] = NewExifBlockState;
		if (ImageType == ImageType.Tiff)
		{
			if (PrimaryDataIfdTable.ContainsKey(ExifTagId.XmpMetadata))
			{
				ImageFileBlockInfo[3] = ImageFileBlockState.Existent;
			}
			else
			{
				ImageFileBlockInfo[3] = ImageFileBlockState.NonExistent;
			}
			if (PrimaryDataIfdTable.ContainsKey(ExifTagId.IptcMetadata))
			{
				ImageFileBlockInfo[2] = ImageFileBlockState.Existent;
			}
			else
			{
				ImageFileBlockInfo[2] = ImageFileBlockState.NonExistent;
			}
		}
	}

	private void ReadJpegBlockMarker(Stream ImageStream, out ushort BlockMarker, out int BlockContentSize)
	{
		byte[] TempBuffer = new byte[2];
		ImageStream.Read(TempBuffer, 0, 2);
		BlockMarker = ReadUInt16BE(TempBuffer, 0);
		if (BlockMarker == 65281 || (BlockMarker >= 65488 && BlockMarker <= 65498))
		{
			BlockContentSize = 0;
			return;
		}
		if (BlockMarker == ushort.MaxValue)
		{
			throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
		}
		ImageStream.Read(TempBuffer, 0, 2);
		BlockContentSize = ReadUInt16BE(TempBuffer, 0) - 2;
		if (BlockContentSize >= 0)
		{
			return;
		}
		throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
	}

	private void ReadJpegBlock(Stream ImageStream, byte[] BlockContent, out int BlockContentSize, out ushort BlockMarker, out ImageFileBlock BlockType, out int MetaDataIndex)
	{
		BlockType = ImageFileBlock.Unknown;
		MetaDataIndex = 0;
		ReadJpegBlockMarker(ImageStream, out BlockMarker, out var ContentSize);
		if (ContentSize > 0)
		{
			if (ImageStream.Read(BlockContent, 0, ContentSize) != ContentSize)
			{
				throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
			}
			if (BlockMarker == 65505)
			{
				if (ArrayStartsWith(BlockContent, ContentSize, JpegExifSignature))
				{
					BlockType = ImageFileBlock.Exif;
					MetaDataIndex = JpegExifSignature.Length;
				}
				else if (ArrayStartsWith(BlockContent, ContentSize, JpegXmpInitialSignature))
				{
					BlockType = ImageFileBlock.Xmp;
					int i;
					for (i = JpegXmpInitialSignature.Length; i < ContentSize && BlockContent[i] != 0; i++)
					{
					}
					MetaDataIndex = i + 1;
				}
			}
			else if (BlockMarker == 65517)
			{
				if (ArrayStartsWith(BlockContent, ContentSize, JpegIptcSignature))
				{
					BlockType = ImageFileBlock.Iptc;
					MetaDataIndex = JpegIptcSignature.Length;
				}
			}
			else if (BlockMarker == 65534)
			{
				BlockType = ImageFileBlock.JpegComment;
			}
		}
		BlockContentSize = ContentSize;
	}

	private ImageType CheckStreamTypeAndCompatibility(Stream StreamToBeChecked)
	{
		byte[] TempBuffer = new byte[4];
		StreamToBeChecked.Read(TempBuffer, 0, 4);
		uint ImageSignature = ReadUInt32BE(TempBuffer, 0);
		ImageType StreamImageType;
		if (ImageSignature >> 16 == 65496)
		{
			StreamImageType = ImageType.Jpeg;
		}
		else
		{
			switch (ImageSignature)
			{
			case 1229531648u:
			case 1296891946u:
				StreamImageType = ImageType.Tiff;
				break;
			case 2303741511u:
				StreamImageType = ImageType.Png;
				break;
			default:
				throw new ExifException(ExifErrCode.ImageTypeIsNotSupported);
			}
		}
		if (StreamToBeChecked.Length > int.MaxValue)
		{
			throw new ExifException(ExifErrCode.ImageHasUnsupportedFeatures);
		}
		long i = StreamToBeChecked.Position;
		StreamToBeChecked.Position += -2L;
		if (StreamToBeChecked.Position != i + -2)
		{
			throw new ExifException(ExifErrCode.ImageHasUnsupportedFeatures);
		}
		StreamToBeChecked.Position = i;
		return StreamImageType;
	}

	private void EvaluateTiffHeader(byte[] TiffHeader, int TiffHeaderBytesRead, out int IfdPrimaryDataOffset)
	{
		if (TiffHeaderBytesRead < 8)
		{
			throw new ExifException(ErrCodeForIllegalExifBlock);
		}
		switch (ReadUInt32BE(TiffHeader, 0))
		{
		case 1229531648u:
			_ByteOrder = ExifByteOrder.LittleEndian;
			break;
		case 1296891946u:
			_ByteOrder = ExifByteOrder.BigEndian;
			break;
		default:
			throw new ExifException(ErrCodeForIllegalExifBlock);
		}
		IfdPrimaryDataOffset = (int)ExifReadUInt32(TiffHeader, 4);
		if (IfdPrimaryDataOffset < 8)
		{
			throw new ExifException(ErrCodeForIllegalExifBlock);
		}
	}

	private void SaveJpeg(Stream SourceStream, Stream DestStream)
	{
		byte[] BlockContent = new byte[65536];
		byte[] TempBuffer = new byte[4];
		SourceStream.Position = 2L;
		WriteUInt16BE(TempBuffer, 0, 65496);
		DestStream.Write(TempBuffer, 0, 2);
		ushort BlockMarker;
		while (true)
		{
			ReadJpegBlockMarker(SourceStream, out BlockMarker, out var BlockContentSize2);
			if (BlockMarker == 65498)
			{
				break;
			}
			long NextBlockStartPosition = SourceStream.Position + BlockContentSize2;
			if (BlockMarker == 65504)
			{
				WriteUInt16BE(TempBuffer, 0, BlockMarker);
				WriteUInt16BE(TempBuffer, 2, (ushort)(BlockContentSize2 + 2));
				DestStream.Write(TempBuffer, 0, 4);
				if (SourceStream.Read(BlockContent, 0, BlockContentSize2) != BlockContentSize2)
				{
					throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
				}
				DestStream.Write(BlockContent, 0, BlockContentSize2);
			}
			SourceStream.Position = NextBlockStartPosition;
		}
		CreateExifBlock(out var NewExifBlock, 8);
		int NewExifBlockLen = NewExifBlock.Length;
		if (NewExifBlockLen > 65518)
		{
			throw new ExifException(ExifErrCode.ExifDataAreTooLarge);
		}
		if (NewExifBlockLen > 0)
		{
			WriteUInt16BE(TempBuffer, 0, 65505);
			WriteUInt16BE(TempBuffer, 2, (ushort)(16 + NewExifBlockLen));
			DestStream.Write(TempBuffer, 0, 4);
			DestStream.Write(JpegExifSignature, 0, JpegExifSignature.Length);
			CreateTiffHeader(out var TiffHeader, 8);
			DestStream.Write(TiffHeader, 0, TiffHeader.Length);
			DestStream.Write(NewExifBlock.Buffer, 0, NewExifBlockLen);
		}
		SourceStream.Position = 2L;
		while (true)
		{
			ReadJpegBlock(SourceStream, BlockContent, out var BlockContentSize, out BlockMarker, out var BlockType, out var _);
			if (BlockMarker == 65498)
			{
				break;
			}
			bool CopyBlockFromSourceStream = true;
			if (BlockMarker == 65504)
			{
				CopyBlockFromSourceStream = false;
			}
			else if (BlockType == ImageFileBlock.Exif)
			{
				CopyBlockFromSourceStream = false;
			}
			else if (BlockType == ImageFileBlock.Xmp && ImageFileBlockInfo[(int)BlockType] == ImageFileBlockState.Removed)
			{
				CopyBlockFromSourceStream = false;
			}
			else if (BlockType == ImageFileBlock.Iptc && ImageFileBlockInfo[(int)BlockType] == ImageFileBlockState.Removed)
			{
				CopyBlockFromSourceStream = false;
			}
			else if (BlockType == ImageFileBlock.JpegComment && ImageFileBlockInfo[(int)BlockType] == ImageFileBlockState.Removed)
			{
				CopyBlockFromSourceStream = false;
			}
			if (CopyBlockFromSourceStream)
			{
				WriteUInt16BE(TempBuffer, 0, BlockMarker);
				WriteUInt16BE(TempBuffer, 2, (ushort)(BlockContentSize + 2));
				DestStream.Write(TempBuffer, 0, 4);
				DestStream.Write(BlockContent, 0, BlockContentSize);
			}
		}
		WriteUInt16BE(TempBuffer, 0, BlockMarker);
		DestStream.Write(TempBuffer, 0, 2);
		int i;
		do
		{
			i = SourceStream.Read(BlockContent, 0, BlockContent.Length);
			DestStream.Write(BlockContent, 0, i);
		}
		while (i == BlockContent.Length);
	}

	private void SaveTiff(Stream SourceStream, Stream DestStream)
	{
		int ImageNumber = 0;
		int NextExifBlockPointerIndex = 0;
		byte[] TempBuffer = new byte[65536];
		FlexArray ExifBlockAsBinaryData = null;
		SourceStream.Position = 0L;
		byte[] TiffHeader = new byte[8];
		if (SourceStream.Read(TiffHeader, 0, 8) != 8)
		{
			throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
		}
		ExifByteOrder SourceStreamByteOrder = ExifByteOrder.LittleEndian;
		if (ReadUInt32BE(TiffHeader, 0) == 1296891946)
		{
			SourceStreamByteOrder = ExifByteOrder.BigEndian;
		}
		if (_ByteOrder != SourceStreamByteOrder)
		{
			throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
		}
		int SourceStreamExifBlockOffset = (int)ExifReadUInt32(TiffHeader, 4);
		uint CurrentDestStreamOffset = 8u;
		do
		{
			uint[] SegmentOffsetTable;
			uint[] SegmentByteCountTable;
			ExifTag SegmentOffsetsTag;
			ExifTag SegmentByteCountsTag;
			ExifData CurrentImageExif = CreateSubExifData(SourceStream, ref SourceStreamExifBlockOffset, SourceStreamByteOrder, out SegmentOffsetTable, out SegmentByteCountTable, out SegmentOffsetsTag, out SegmentByteCountsTag);
			if (ImageNumber == 0)
			{
				CurrentImageExif = this;
			}
			if (!CurrentImageExif.TagExists(ExifTag.ImageWidth) || !CurrentImageExif.TagExists(ExifTag.ImageLength))
			{
				throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
			}
			CurrentImageExif.RemoveTag(ExifTag.FreeOffsets);
			CurrentImageExif.RemoveTag(ExifTag.FreeByteCounts);
			int ImageSegmentCount = SegmentOffsetTable.Length;
			CurrentImageExif.SetTagValueCount(SegmentOffsetsTag, ImageSegmentCount, ExifTagType.ULong);
			CurrentImageExif.SetTagValueCount(SegmentByteCountsTag, ImageSegmentCount, ExifTagType.ULong);
			for (int i = 0; i < ImageSegmentCount; i++)
			{
				CurrentImageExif.SetTagValue(SegmentOffsetsTag, CurrentDestStreamOffset, ExifTagType.ULong, i);
				uint SegmentSize = SegmentByteCountTable[i];
				CurrentImageExif.SetTagValue(SegmentByteCountsTag, SegmentSize, ExifTagType.ULong, i);
				CurrentDestStreamOffset += SegmentSize;
			}
			bool HasImageMatrixFillByte = false;
			if ((CurrentDestStreamOffset & (true ? 1u : 0u)) != 0)
			{
				CurrentDestStreamOffset++;
				HasImageMatrixFillByte = true;
			}
			if (CurrentDestStreamOffset > int.MaxValue)
			{
				throw new ExifException(ExifErrCode.ExifDataAreTooLarge);
			}
			if (ImageNumber == 0)
			{
				CurrentImageExif.ExifWriteUInt32(TiffHeader, 4, CurrentDestStreamOffset);
				DestStream.Write(TiffHeader, 0, 8);
			}
			else
			{
				CurrentImageExif.ExifWriteUInt32(ExifBlockAsBinaryData.Buffer, NextExifBlockPointerIndex, CurrentDestStreamOffset);
				DestStream.Write(ExifBlockAsBinaryData.Buffer, 0, ExifBlockAsBinaryData.Length);
			}
			for (int i = 0; i < ImageSegmentCount; i++)
			{
				int RemainingByteCount = (int)SegmentByteCountTable[i];
				int BytesToRead = TempBuffer.Length;
				SourceStream.Position = SegmentOffsetTable[i];
				do
				{
					if (BytesToRead > RemainingByteCount)
					{
						BytesToRead = RemainingByteCount;
					}
					if (SourceStream.Read(TempBuffer, 0, BytesToRead) == BytesToRead)
					{
						DestStream.Write(TempBuffer, 0, BytesToRead);
						RemainingByteCount -= BytesToRead;
						continue;
					}
					throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
				}
				while (RemainingByteCount > 0);
			}
			if (HasImageMatrixFillByte)
			{
				DestStream.WriteByte(0);
			}
			NextExifBlockPointerIndex = CurrentImageExif.CreateExifBlock(out ExifBlockAsBinaryData, (int)CurrentDestStreamOffset);
			CurrentDestStreamOffset += (uint)ExifBlockAsBinaryData.Length;
			if (CurrentDestStreamOffset > int.MaxValue)
			{
				throw new ExifException(ExifErrCode.ExifDataAreTooLarge);
			}
			ImageNumber++;
		}
		while (SourceStreamExifBlockOffset != 0);
		DestStream.Write(ExifBlockAsBinaryData.Buffer, 0, ExifBlockAsBinaryData.Length);
	}

	private static ExifData CreateSubExifData(Stream TiffStream, ref int ExifBlockOffset, ExifByteOrder ByteOrder, out uint[] SegmentOffsetTable, out uint[] SegmentByteCountTable, out ExifTag SegmentOffsetsTag, out ExifTag SegmentByteCountsTag)
	{
		ExifData SubExifBlock = new ExifData();
		SubExifBlock._ByteOrder = ByteOrder;
		SubExifBlock.ImageType = ImageType.Tiff;
		SubExifBlock.SourceExifBlock = null;
		SubExifBlock.SourceExifStream = TiffStream;
		ExifBlockOffset = SubExifBlock.EvaluateExifBlock(ExifBlockOffset);
		if (SubExifBlock.GetTagItem(ExifTag.StripOffsets, out var SegmentOffsetsTagItem) && SubExifBlock.GetTagItem(ExifTag.StripByteCounts, out var SegmentByteCountsTagItem))
		{
			SegmentOffsetsTag = ExifTag.StripOffsets;
			SegmentByteCountsTag = ExifTag.StripByteCounts;
		}
		else
		{
			if (!SubExifBlock.GetTagItem(ExifTag.TileOffsets, out SegmentOffsetsTagItem) || !SubExifBlock.GetTagItem(ExifTag.TileByteCounts, out SegmentByteCountsTagItem))
			{
				throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
			}
			SegmentOffsetsTag = ExifTag.TileOffsets;
			SegmentByteCountsTag = ExifTag.TileByteCounts;
		}
		int ValueCount = SegmentOffsetsTagItem.ValueCount;
		SegmentOffsetTable = new uint[ValueCount];
		SegmentByteCountTable = new uint[ValueCount];
		bool IsOffsetTable16Bit = SegmentOffsetsTagItem.TagType == ExifTagType.UShort;
		bool ByteCountTable16Bit = SegmentByteCountsTagItem.TagType == ExifTagType.UShort;
		for (int i = 0; i < ValueCount; i++)
		{
			uint v = ((!IsOffsetTable16Bit) ? SubExifBlock.ExifReadUInt32(SegmentOffsetsTagItem.ValueData, SegmentOffsetsTagItem.ValueIndex + (i << 2)) : SubExifBlock.ExifReadUInt16(SegmentOffsetsTagItem.ValueData, SegmentOffsetsTagItem.ValueIndex + (i << 1)));
			SegmentOffsetTable[i] = v;
			v = ((!ByteCountTable16Bit) ? SubExifBlock.ExifReadUInt32(SegmentByteCountsTagItem.ValueData, SegmentByteCountsTagItem.ValueIndex + (i << 2)) : SubExifBlock.ExifReadUInt16(SegmentByteCountsTagItem.ValueData, SegmentByteCountsTagItem.ValueIndex + (i << 1)));
			SegmentByteCountTable[i] = v;
		}
		return SubExifBlock;
	}

	private void SavePng(Stream SourceStream, Stream DestStream)
	{
		byte[] TempData = new byte[65536];
		byte[] BlockContent = new byte[30];
		SourceStream.Position = 8L;
		WriteUInt32BE(TempData, 0, 2303741511u);
		WriteUInt32BE(TempData, 4, 218765834u);
		DestStream.Write(TempData, 0, 8);
		uint ChunkType;
		do
		{
			bool CopyBlockFromSourceStream = true;
			long BlockStartStreamPos = SourceStream.Position;
			ReadPngBlockHeader(SourceStream, TempData, out var DataLength, out ChunkType);
			DetectPngImageBlock(SourceStream, BlockContent, ChunkType, out var BlockType);
			if (BlockType != 0 && (BlockType == ImageFileBlock.Exif || ImageFileBlockInfo[(int)BlockType] == ImageFileBlockState.Removed))
			{
				CopyBlockFromSourceStream = false;
			}
			if (CopyBlockFromSourceStream)
			{
				SourceStream.Position = BlockStartStreamPos;
				int RemainingByteCount = DataLength + 12;
				int BytesToRead = TempData.Length;
				do
				{
					if (BytesToRead > RemainingByteCount)
					{
						BytesToRead = RemainingByteCount;
					}
					if (SourceStream.Read(TempData, 0, BytesToRead) == BytesToRead)
					{
						DestStream.Write(TempData, 0, BytesToRead);
						RemainingByteCount -= BytesToRead;
						continue;
					}
					throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
				}
				while (RemainingByteCount > 0);
			}
			else
			{
				SourceStream.Position = BlockStartStreamPos + DataLength + 12;
			}
			if (ChunkType == 1229472850)
			{
				CreateExifBlock(out var NewExifBlock, 8);
				int NewExifBlockLen = NewExifBlock.Length;
				if (NewExifBlockLen > 0)
				{
					CreateTiffHeader(out var TiffHeader, 8);
					uint BlockLength = (uint)(8 + NewExifBlockLen);
					WriteUInt32BE(TempData, 0, BlockLength);
					WriteUInt32BE(TempData, 4, 1700284774u);
					DestStream.Write(TempData, 0, 8);
					DestStream.Write(TiffHeader, 0, 8);
					DestStream.Write(NewExifBlock.Buffer, 0, NewExifBlockLen);
					uint Crc32 = CalculateCrc32(TempData, 4, 4, Finalize: false);
					Crc32 = CalculateCrc32(TiffHeader, 0, 8, Finalize: false, Crc32);
					Crc32 = CalculateCrc32(NewExifBlock.Buffer, 0, NewExifBlockLen, Finalize: true, Crc32);
					WriteUInt32BE(TempData, 0, Crc32);
					DestStream.Write(TempData, 0, 4);
				}
			}
		}
		while (ChunkType != 1229278788);
	}

	private void SetEmptyExifBlock()
	{
		SourceExifBlock = new byte[14];
		ExifWriteUInt16(SourceExifBlock, 8, 0);
		ExifWriteUInt32(SourceExifBlock, 10, 0u);
		EvaluateExifBlock(8);
	}

	private void WriteTagToFlexArray(TagItem TempTagData, FlexArray WriteData, ref int TagDataIndex, int ExifBlockOffset)
	{
		int i = TagDataIndex;
		TagDataIndex += 12;
		ExifWriteUInt16(WriteData.Buffer, i, (ushort)TempTagData.TagId);
		ExifWriteUInt16(WriteData.Buffer, i + 2, (ushort)TempTagData.TagType);
		ExifWriteUInt32(WriteData.Buffer, i + 4, (uint)TempTagData.ValueCount);
		int ByteCount = GetTagByteCount(TempTagData.TagType, TempTagData.ValueCount);
		if (ByteCount <= 4)
		{
			WriteData.Buffer[i + 8] = TempTagData.ValueData[TempTagData.ValueIndex];
			byte v = 0;
			if (ByteCount >= 2)
			{
				v = TempTagData.ValueData[TempTagData.ValueIndex + 1];
			}
			WriteData.Buffer[i + 9] = v;
			v = 0;
			if (ByteCount >= 3)
			{
				v = TempTagData.ValueData[TempTagData.ValueIndex + 2];
			}
			WriteData.Buffer[i + 10] = v;
			v = 0;
			if (ByteCount >= 4)
			{
				v = TempTagData.ValueData[TempTagData.ValueIndex + 3];
			}
			WriteData.Buffer[i + 11] = v;
		}
		else
		{
			int OutsourcedDataIndex = WriteData.Length;
			ExifWriteUInt32(WriteData.Buffer, i + 8, (uint)(ExifBlockOffset + OutsourcedDataIndex));
			WriteData.Length += ByteCount;
			Array.Copy(TempTagData.ValueData, TempTagData.ValueIndex, WriteData.Buffer, OutsourcedDataIndex, ByteCount);
			if (((uint)ByteCount & (true ? 1u : 0u)) != 0)
			{
				WriteData.Length++;
				WriteData.Buffer[WriteData.Length - 1] = 0;
			}
		}
	}

	private int CreateExifBlock(out FlexArray NewExifBlock, int CurrentExifBlockOffset)
	{
		int TiffNextExifBlockPointerIndex = 0;
		FlexArray WriteExifBlock = (NewExifBlock = new FlexArray(65518));
		int WriteIndex = 0;
		UpdateIfdPointerTags(out var PrivateDataPointerTag, out var GpsInfoDataPointerTag, out var InteroperabilityPointerTag);
		CreateIfdPrimaryData(WriteExifBlock, ref WriteIndex, CurrentExifBlockOffset, out var PrivateDataIfdPointerIndex, out var GpsInfoDataIfdPointerIndex, out var ThumbnailDataIfdPointerIndex);
		CreateIfdPrivateData(WriteExifBlock, ref WriteIndex, CurrentExifBlockOffset, PrivateDataIfdPointerIndex, PrivateDataPointerTag, out var InteroperabilityIfdPointerIndex);
		CreateIfdGpsInfoData(WriteExifBlock, ref WriteIndex, CurrentExifBlockOffset, GpsInfoDataIfdPointerIndex, GpsInfoDataPointerTag);
		CreateIfdInteroperability(WriteExifBlock, ref WriteIndex, CurrentExifBlockOffset, InteroperabilityIfdPointerIndex, InteroperabilityPointerTag);
		if (ImageType == ImageType.Tiff)
		{
			TiffNextExifBlockPointerIndex = ThumbnailDataIfdPointerIndex;
			ExifWriteUInt32(WriteExifBlock.Buffer, TiffNextExifBlockPointerIndex, 0u);
		}
		else
		{
			CreateIfdThumbnailData(WriteExifBlock, ref WriteIndex, CurrentExifBlockOffset, ThumbnailDataIfdPointerIndex);
		}
		if (WriteExifBlock.Length <= 6)
		{
			WriteExifBlock.Length = 0;
		}
		return TiffNextExifBlockPointerIndex;
	}

	private void CreateTiffHeader(out byte[] TiffHeader, int ExifBlockOffset)
	{
		TiffHeader = new byte[8];
		if (ByteOrder == ExifByteOrder.BigEndian)
		{
			WriteUInt32BE(TiffHeader, 0, 1296891946u);
		}
		else
		{
			if (ByteOrder != 0)
			{
				throw new ExifException(ExifErrCode.InternalError);
			}
			WriteUInt32BE(TiffHeader, 0, 1229531648u);
		}
		ExifWriteUInt32(TiffHeader, 4, (uint)ExifBlockOffset);
	}

	private void UpdateIfdPointerTags(out TagItem PrivateDataPointerTag, out TagItem GpsInfoDataPointerTag, out TagItem InteroperabilityPointerTag)
	{
		Dictionary<ExifTagId, TagItem> PrivateDataIfdTable = TagTable[1];
		Dictionary<ExifTagId, TagItem> obj = TagTable[3];
		PrivateDataIfdTable.TryGetValue(ExifTagId.InteroperabilityIfdPointer, out InteroperabilityPointerTag);
		if (obj.Count > 0)
		{
			if (InteroperabilityPointerTag == null)
			{
				InteroperabilityPointerTag = new TagItem(ExifTag.InteroperabilityIfdPointer, ExifTagType.ULong, 1);
				PrivateDataIfdTable.Add(ExifTagId.InteroperabilityIfdPointer, InteroperabilityPointerTag);
			}
		}
		else if (InteroperabilityPointerTag != null)
		{
			PrivateDataIfdTable.Remove(ExifTagId.InteroperabilityIfdPointer);
		}
		Dictionary<ExifTagId, TagItem> PrimaryDataIfdTable = TagTable[0];
		Dictionary<ExifTagId, TagItem> obj2 = TagTable[2];
		PrimaryDataIfdTable.TryGetValue(ExifTagId.GpsInfoIfdPointer, out GpsInfoDataPointerTag);
		if (obj2.Count > 0)
		{
			if (GpsInfoDataPointerTag == null)
			{
				GpsInfoDataPointerTag = new TagItem(ExifTag.GpsInfoIfdPointer, ExifTagType.ULong, 1);
				PrimaryDataIfdTable.Add(ExifTagId.GpsInfoIfdPointer, GpsInfoDataPointerTag);
			}
		}
		else if (GpsInfoDataPointerTag != null)
		{
			PrimaryDataIfdTable.Remove(ExifTagId.GpsInfoIfdPointer);
		}
		PrimaryDataIfdTable.TryGetValue(ExifTagId.ExifIfdPointer, out PrivateDataPointerTag);
		if (PrivateDataIfdTable.Count > 0)
		{
			if (PrivateDataPointerTag == null)
			{
				PrivateDataPointerTag = new TagItem(ExifTag.ExifIfdPointer, ExifTagType.ULong, 1);
				PrimaryDataIfdTable.Add(ExifTagId.ExifIfdPointer, PrivateDataPointerTag);
			}
		}
		else if (PrivateDataPointerTag != null)
		{
			PrimaryDataIfdTable.Remove(ExifTagId.ExifIfdPointer);
		}
	}

	private void CreateIfdPrimaryData(FlexArray WriteExifBlock, ref int WriteIndex, int ExifBlockOffset, out int PrivateDataIfdPointerIndex, out int GpsInfoDataIfdPointerIndex, out int ThumbnailDataIfdPointerIndex)
	{
		Dictionary<ExifTagId, TagItem> obj = TagTable[0];
		int PrimaryDataTagCount = obj.Count;
		PrivateDataIfdPointerIndex = -1;
		GpsInfoDataIfdPointerIndex = -1;
		int IfdFixedSize = 2 + PrimaryDataTagCount * 12 + 4;
		WriteExifBlock.Length += IfdFixedSize;
		ExifWriteUInt16(WriteExifBlock.Buffer, WriteIndex, (ushort)PrimaryDataTagCount);
		WriteIndex += 2;
		foreach (TagItem t in obj.Values)
		{
			if (t.TagId == ExifTagId.ExifIfdPointer)
			{
				t.TagType = ExifTagType.ULong;
				PrivateDataIfdPointerIndex = WriteIndex + 8;
			}
			else if (t.TagId == ExifTagId.GpsInfoIfdPointer)
			{
				t.TagType = ExifTagType.ULong;
				GpsInfoDataIfdPointerIndex = WriteIndex + 8;
			}
			WriteTagToFlexArray(t, WriteExifBlock, ref WriteIndex, ExifBlockOffset);
		}
		ThumbnailDataIfdPointerIndex = WriteIndex;
		WriteIndex = WriteExifBlock.Length;
	}

	private void CreateIfdPrivateData(FlexArray WriteExifBlock, ref int WriteIndex, int ExifBlockOffset, int PrivateDataIfdPointerIndex, TagItem PrivateDataPointerTag, out int InteroperabilityIfdPointerIndex)
	{
		Dictionary<ExifTagId, TagItem> PrivateDataIfdTable = TagTable[1];
		int PrivateDataTagCount = PrivateDataIfdTable.Count;
		InteroperabilityIfdPointerIndex = -1;
		if (PrivateDataTagCount <= 0)
		{
			return;
		}
		int iPrivateDataStart = WriteIndex;
		ExifWriteUInt32(PrivateDataPointerTag.ValueData, PrivateDataPointerTag.ValueIndex, (uint)(ExifBlockOffset + WriteIndex));
		ExifWriteUInt32(WriteExifBlock.Buffer, PrivateDataIfdPointerIndex, (uint)(ExifBlockOffset + WriteIndex));
		bool AddOffsetSchemaTag;
		do
		{
			int MakerNoteDataPointerIndex = 0;
			int OffsetSchemaValueIndex = 0;
			TagItem OffsetSchemaTag = null;
			int IfdFixedSize = 2 + PrivateDataTagCount * 12 + 4;
			WriteExifBlock.Length += IfdFixedSize;
			ExifWriteUInt16(WriteExifBlock.Buffer, WriteIndex, (ushort)PrivateDataTagCount);
			WriteIndex += 2;
			foreach (TagItem t2 in PrivateDataIfdTable.Values)
			{
				if (t2.TagId == ExifTagId.InteroperabilityIfdPointer)
				{
					t2.TagType = ExifTagType.ULong;
					InteroperabilityIfdPointerIndex = WriteIndex + 8;
				}
				else if (t2.TagId == ExifTagId.MakerNote)
				{
					MakerNoteDataPointerIndex = WriteIndex + 8;
				}
				else if (t2.TagId == ExifTagId.OffsetSchema)
				{
					t2.TagType = ExifTagType.SLong;
					OffsetSchemaValueIndex = WriteIndex + 8;
					OffsetSchemaTag = t2;
				}
				WriteTagToFlexArray(t2, WriteExifBlock, ref WriteIndex, ExifBlockOffset);
			}
			ExifWriteUInt32(WriteExifBlock.Buffer, WriteIndex, 0u);
			WriteIndex = WriteExifBlock.Length;
			AddOffsetSchemaTag = CheckIfMakerNoteTagHasMoved(WriteExifBlock.Buffer, MakerNoteDataPointerIndex, OffsetSchemaValueIndex, OffsetSchemaTag);
			if (AddOffsetSchemaTag)
			{
				TagItem t = new TagItem(ExifTag.OffsetSchema, ExifTagType.SLong, 1);
				PrivateDataIfdTable.Add(ExifTagId.OffsetSchema, t);
				PrivateDataTagCount++;
				WriteExifBlock.Length = iPrivateDataStart;
				WriteIndex = iPrivateDataStart;
			}
		}
		while (AddOffsetSchemaTag);
	}

	private void CreateIfdGpsInfoData(FlexArray WriteExifBlock, ref int WriteIndex, int ExifBlockOffset, int GpsInfoDataIfdPointerIndex, TagItem GpsInfoDataPointerTag)
	{
		Dictionary<ExifTagId, TagItem> GpsInfoDataIfdTable = TagTable[2];
		int GpsInfoDataTagCount = GpsInfoDataIfdTable.Count;
		if (GpsInfoDataTagCount <= 0)
		{
			return;
		}
		uint GpsInfoDataOffset = (uint)(ExifBlockOffset + WriteIndex);
		ExifWriteUInt32(GpsInfoDataPointerTag.ValueData, GpsInfoDataPointerTag.ValueIndex, GpsInfoDataOffset);
		ExifWriteUInt32(WriteExifBlock.Buffer, GpsInfoDataIfdPointerIndex, GpsInfoDataOffset);
		int IfdFixedSize = 2 + GpsInfoDataTagCount * 12 + 4;
		WriteExifBlock.Length += IfdFixedSize;
		ExifWriteUInt16(WriteExifBlock.Buffer, WriteIndex, (ushort)GpsInfoDataTagCount);
		WriteIndex += 2;
		foreach (TagItem t in GpsInfoDataIfdTable.Values)
		{
			WriteTagToFlexArray(t, WriteExifBlock, ref WriteIndex, ExifBlockOffset);
		}
		ExifWriteUInt32(WriteExifBlock.Buffer, WriteIndex, 0u);
		WriteIndex = WriteExifBlock.Length;
	}

	private void CreateIfdInteroperability(FlexArray WriteExifBlock, ref int WriteIndex, int ExifBlockOffset, int InteroperabilityIfdPointerIndex, TagItem InteroperabilityPointerTag)
	{
		Dictionary<ExifTagId, TagItem> InteroperabilityIfdTable = TagTable[3];
		int InteroperabilityTagCount = InteroperabilityIfdTable.Count;
		if (InteroperabilityTagCount <= 0)
		{
			return;
		}
		uint InteroperabilityOffset = (uint)(ExifBlockOffset + WriteIndex);
		ExifWriteUInt32(InteroperabilityPointerTag.ValueData, InteroperabilityPointerTag.ValueIndex, InteroperabilityOffset);
		ExifWriteUInt32(WriteExifBlock.Buffer, InteroperabilityIfdPointerIndex, InteroperabilityOffset);
		int IfdFixedSize = 2 + InteroperabilityTagCount * 12 + 4;
		WriteExifBlock.Length += IfdFixedSize;
		ExifWriteUInt16(WriteExifBlock.Buffer, WriteIndex, (ushort)InteroperabilityTagCount);
		WriteIndex += 2;
		foreach (TagItem t in InteroperabilityIfdTable.Values)
		{
			WriteTagToFlexArray(t, WriteExifBlock, ref WriteIndex, ExifBlockOffset);
		}
		ExifWriteUInt32(WriteExifBlock.Buffer, WriteIndex, 0u);
		WriteIndex = WriteExifBlock.Length;
	}

	private void CreateIfdThumbnailData(FlexArray WriteExifBlock, ref int WriteIndex, int ExifBlockOffset, int ThumbnailDataIfdPointerIndex)
	{
		Dictionary<ExifTagId, TagItem> ThumbnailDataIfdTable = TagTable[4];
		int ThumbnailDataTagCount = ThumbnailDataIfdTable.Count;
		if (ThumbnailDataTagCount > 0 || ThumbnailImageExists())
		{
			ExifWriteUInt32(WriteExifBlock.Buffer, ThumbnailDataIfdPointerIndex, (uint)(ExifBlockOffset + WriteIndex));
			int IfdFixedSize = 2 + ThumbnailDataTagCount * 12 + 4;
			WriteExifBlock.Length += IfdFixedSize;
			ExifWriteUInt16(WriteExifBlock.Buffer, WriteIndex, (ushort)ThumbnailDataTagCount);
			WriteIndex += 2;
			int ThumbnailImagePointerIndex = -1;
			TagItem ThumbnailImagePointerTag = null;
			bool ThumbnailImageSizeTagExists = false;
			foreach (TagItem t in ThumbnailDataIfdTable.Values)
			{
				if (t.TagId == ExifTagId.JpegInterchangeFormat)
				{
					t.TagType = ExifTagType.ULong;
					ThumbnailImagePointerTag = t;
					ThumbnailImagePointerIndex = WriteIndex + 8;
				}
				else if (t.TagId == ExifTagId.JpegInterchangeFormatLength)
				{
					t.TagType = ExifTagType.ULong;
					ExifWriteUInt32(t.ValueData, t.ValueIndex, (uint)ThumbnailByteCount);
					ThumbnailImageSizeTagExists = true;
				}
				WriteTagToFlexArray(t, WriteExifBlock, ref WriteIndex, ExifBlockOffset);
			}
			if (ThumbnailImage != null)
			{
				if (ThumbnailImagePointerIndex < 0 || !ThumbnailImageSizeTagExists)
				{
					throw new ExifException(ExifErrCode.InternalError);
				}
				int ThumbnailImageIndex = WriteExifBlock.Length;
				uint ThumbnailImageOffset = (uint)(ExifBlockOffset + ThumbnailImageIndex);
				ExifWriteUInt32(ThumbnailImagePointerTag.ValueData, ThumbnailImagePointerTag.ValueIndex, ThumbnailImageOffset);
				ExifWriteUInt32(WriteExifBlock.Buffer, ThumbnailImagePointerIndex, ThumbnailImageOffset);
				WriteExifBlock.Length += ThumbnailByteCount;
				Array.Copy(ThumbnailImage, ThumbnailStartIndex, WriteExifBlock.Buffer, ThumbnailImageIndex, ThumbnailByteCount);
				if (((uint)WriteExifBlock.Length & (true ? 1u : 0u)) != 0)
				{
					WriteExifBlock.Length++;
					WriteExifBlock[WriteExifBlock.Length - 1] = 0;
				}
			}
			else if (ThumbnailImagePointerIndex >= 0)
			{
				throw new ExifException(ExifErrCode.InternalError);
			}
			ExifWriteUInt32(WriteExifBlock.Buffer, WriteIndex, 0u);
		}
		else
		{
			ExifWriteUInt32(WriteExifBlock.Buffer, ThumbnailDataIfdPointerIndex, 0u);
		}
	}

	private bool CheckIfMakerNoteTagHasMoved(byte[] DataBlock, int MakerNoteDataPointerIndex, int OffsetSchemaValueIndex, TagItem OffsetSchemaTag)
	{
		bool MakerNoteHasMovedAndOffsetSchemaTagRequired = false;
		if (MakerNoteOriginalOffset > 0 && MakerNoteDataPointerIndex > 0)
		{
			int MakerNoteCurrentOffset = (int)ExifReadUInt32(DataBlock, MakerNoteDataPointerIndex);
			if (MakerNoteOriginalOffset != MakerNoteCurrentOffset)
			{
				if (OffsetSchemaValueIndex > 0)
				{
					int NewOffsetDifference = MakerNoteCurrentOffset - MakerNoteOriginalOffset;
					ExifWriteUInt32(DataBlock, OffsetSchemaValueIndex, (uint)NewOffsetDifference);
					ExifWriteUInt32(OffsetSchemaTag.ValueData, OffsetSchemaTag.ValueIndex, (uint)NewOffsetDifference);
				}
				else
				{
					MakerNoteHasMovedAndOffsetSchemaTagRequired = true;
				}
			}
			else if (OffsetSchemaValueIndex > 0)
			{
				ExifWriteUInt32(DataBlock, OffsetSchemaValueIndex, 0u);
				ExifWriteUInt32(OffsetSchemaTag.ValueData, OffsetSchemaTag.ValueIndex, 0u);
			}
		}
		else if (OffsetSchemaValueIndex > 0)
		{
			ExifWriteUInt32(DataBlock, OffsetSchemaValueIndex, 0u);
			ExifWriteUInt32(OffsetSchemaTag.ValueData, OffsetSchemaTag.ValueIndex, 0u);
		}
		return MakerNoteHasMovedAndOffsetSchemaTagRequired;
	}

	private TagItem CreateTagWithReferenceToIfdRawData(byte[] IfdRawData, int IfdRawDataIndex)
	{
		ushort tagId = ExifReadUInt16(IfdRawData, IfdRawDataIndex);
		ExifTagType TagType = (ExifTagType)ExifReadUInt16(IfdRawData, IfdRawDataIndex + 2);
		uint ValueCount = ExifReadUInt32(IfdRawData, IfdRawDataIndex + 4);
		if (ValueCount > 268435455)
		{
			throw new ExifException(ErrCodeForIllegalExifBlock);
		}
		int ValueByteCount = GetTagByteCount(TagType, (int)ValueCount);
		int ValueIndex;
		byte[] TagData;
		int AllocatedByteCount;
		int OriginalOffset;
		if (ValueByteCount <= 4)
		{
			ValueIndex = IfdRawDataIndex + 8;
			TagData = IfdRawData;
			AllocatedByteCount = 4;
			OriginalOffset = 0;
		}
		else
		{
			OriginalOffset = (int)ExifReadUInt32(IfdRawData, IfdRawDataIndex + 8);
			AllocatedByteCount = ValueByteCount;
			GetOutsourcedData(OriginalOffset, AllocatedByteCount, out TagData, out ValueIndex);
		}
		return new TagItem((ExifTagId)tagId, TagType, (int)ValueCount, TagData, ValueIndex, AllocatedByteCount, OriginalOffset);
	}

	private void GetOutsourcedData(int DataOffset, int DataLen, out byte[] TagData, out int TagDataIndex)
	{
		if (SourceExifBlock != null)
		{
			if ((uint)(DataOffset + DataLen) <= (uint)SourceExifBlock.Length)
			{
				TagData = SourceExifBlock;
				TagDataIndex = DataOffset;
				return;
			}
			throw new ExifException(ErrCodeForIllegalExifBlock);
		}
		TagData = new byte[DataLen];
		SourceExifStream.Position = DataOffset;
		if (SourceExifStream.Read(TagData, 0, DataLen) != DataLen)
		{
			throw new ExifException(ErrCodeForIllegalExifBlock);
		}
		TagDataIndex = 0;
	}

	private void InitIfdPrimaryData(byte[] IfdRawData, int IfdRawDataIndex, out int PrivateDataOffset, out int GpsInfoDataOffset, out int NextImageOffset)
	{
		PrivateDataOffset = 0;
		GpsInfoDataOffset = 0;
		int TagCount = ExifReadUInt16(IfdRawData, IfdRawDataIndex);
		IfdRawDataIndex += 2;
		Dictionary<ExifTagId, TagItem> PrimaryDataIfdTable = new Dictionary<ExifTagId, TagItem>(TagCount);
		TagTable[0] = PrimaryDataIfdTable;
		for (int i = 0; i < TagCount; i++)
		{
			TagItem TempTagData = CreateTagWithReferenceToIfdRawData(IfdRawData, IfdRawDataIndex);
			if (AddIfNotExists(PrimaryDataIfdTable, TempTagData))
			{
				if (TempTagData.TagId == ExifTagId.ExifIfdPointer)
				{
					PrivateDataOffset = (int)ExifReadUInt32(TempTagData.ValueData, TempTagData.ValueIndex);
				}
				else if (TempTagData.TagId == ExifTagId.GpsInfoIfdPointer)
				{
					GpsInfoDataOffset = (int)ExifReadUInt32(TempTagData.ValueData, TempTagData.ValueIndex);
				}
			}
			IfdRawDataIndex += 12;
		}
		NextImageOffset = (int)ExifReadUInt32(IfdRawData, IfdRawDataIndex);
	}

	private void InitIfdPrivateData(byte[] IfdRawData, int IfdRawDataIndex, out int InteroperabilityOffset)
	{
		InteroperabilityOffset = 0;
		int MakerNoteOffset = 0;
		int OffsetSchemaValue = 0;
		int TagCount = ExifReadUInt16(IfdRawData, IfdRawDataIndex);
		IfdRawDataIndex += 2;
		Dictionary<ExifTagId, TagItem> PrivateDataIfdTable = new Dictionary<ExifTagId, TagItem>(TagCount);
		TagTable[1] = PrivateDataIfdTable;
		for (int i = 0; i < TagCount; i++)
		{
			TagItem TempTagData = CreateTagWithReferenceToIfdRawData(IfdRawData, IfdRawDataIndex);
			if (AddIfNotExists(PrivateDataIfdTable, TempTagData))
			{
				if (TempTagData.TagId == ExifTagId.InteroperabilityIfdPointer)
				{
					InteroperabilityOffset = (int)ExifReadUInt32(TempTagData.ValueData, TempTagData.ValueIndex);
				}
				else if (TempTagData.TagId == ExifTagId.MakerNote)
				{
					MakerNoteOffset = TempTagData.OriginalDataOffset;
				}
				else if (TempTagData.TagId == ExifTagId.OffsetSchema)
				{
					OffsetSchemaValue = (int)ExifReadUInt32(TempTagData.ValueData, TempTagData.ValueIndex);
				}
			}
			IfdRawDataIndex += 12;
		}
		if (MakerNoteOffset > 0)
		{
			MakerNoteOriginalOffset = MakerNoteOffset - OffsetSchemaValue;
		}
		else
		{
			MakerNoteOriginalOffset = 0;
		}
	}

	private void InitIfdGpsInfoData(byte[] IfdRawData, int IfdRawDataIndex)
	{
		int TagCount = ExifReadUInt16(IfdRawData, IfdRawDataIndex);
		IfdRawDataIndex += 2;
		Dictionary<ExifTagId, TagItem> GpsInfoDataIfdTable = new Dictionary<ExifTagId, TagItem>(TagCount);
		TagTable[2] = GpsInfoDataIfdTable;
		for (int i = 0; i < TagCount; i++)
		{
			TagItem TempTagData = CreateTagWithReferenceToIfdRawData(IfdRawData, IfdRawDataIndex);
			AddIfNotExists(GpsInfoDataIfdTable, TempTagData);
			IfdRawDataIndex += 12;
		}
	}

	private void InitIfdInteroperability(byte[] IfdRawData, int IfdRawDataIndex)
	{
		int TagCount = ExifReadUInt16(IfdRawData, IfdRawDataIndex);
		IfdRawDataIndex += 2;
		Dictionary<ExifTagId, TagItem> InteroperabilityIfdTable = new Dictionary<ExifTagId, TagItem>(TagCount);
		TagTable[3] = InteroperabilityIfdTable;
		for (int i = 0; i < TagCount; i++)
		{
			TagItem TempTagData = CreateTagWithReferenceToIfdRawData(IfdRawData, IfdRawDataIndex);
			AddIfNotExists(InteroperabilityIfdTable, TempTagData);
			IfdRawDataIndex += 12;
		}
	}

	private void InitIfdThumbnailData(byte[] IfdRawData, int IfdRawDataIndex)
	{
		int ThumbnailImageDataOffset = 0;
		ThumbnailByteCount = 0;
		ThumbnailImage = null;
		int TagCount = ExifReadUInt16(IfdRawData, IfdRawDataIndex);
		IfdRawDataIndex += 2;
		Dictionary<ExifTagId, TagItem> ThumbnailDataIfdTable = new Dictionary<ExifTagId, TagItem>(TagCount);
		TagTable[4] = ThumbnailDataIfdTable;
		for (int i = 0; i < TagCount; i++)
		{
			TagItem TempTagData = CreateTagWithReferenceToIfdRawData(IfdRawData, IfdRawDataIndex);
			if (AddIfNotExists(ThumbnailDataIfdTable, TempTagData))
			{
				if (TempTagData.TagId == ExifTagId.JpegInterchangeFormat)
				{
					ThumbnailImageDataOffset = (int)ExifReadUInt32(TempTagData.ValueData, TempTagData.ValueIndex);
				}
				else if (TempTagData.TagId == ExifTagId.JpegInterchangeFormatLength)
				{
					ThumbnailByteCount = (int)ExifReadUInt32(TempTagData.ValueData, TempTagData.ValueIndex);
				}
			}
			IfdRawDataIndex += 12;
		}
		if (ThumbnailImageDataOffset > 0)
		{
			GetOutsourcedData(ThumbnailImageDataOffset, ThumbnailByteCount, out ThumbnailImage, out ThumbnailStartIndex);
		}
	}

	private void GetNextIfd(int IfdOffset, out byte[] IfdRawData, out int IfdRawDataIndex)
	{
		if (IfdOffset == 0)
		{
			IfdRawData = new byte[2];
			IfdRawDataIndex = 0;
			return;
		}
		if (SourceExifBlock != null)
		{
			IfdRawData = SourceExifBlock;
			IfdRawDataIndex = IfdOffset;
			return;
		}
		SourceExifStream.Position = IfdOffset;
		IfdRawDataIndex = 0;
		byte[] TagCountArray = new byte[2];
		if (SourceExifStream.Read(TagCountArray, 0, 2) == 2)
		{
			int IfdTagCount = ExifReadUInt16(TagCountArray, 0);
			int IfdSize = 2 + IfdTagCount * 12 + 4;
			IfdRawData = new byte[IfdSize];
			IfdRawData[0] = TagCountArray[0];
			IfdRawData[1] = TagCountArray[1];
			if (SourceExifStream.Read(IfdRawData, 2, IfdSize - 2) == IfdSize - 2)
			{
				return;
			}
			throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
		}
		throw new ExifException(ExifErrCode.InternalImageStructureIsWrong);
	}

	private int EvaluateExifBlock(int PrimaryDataOffset)
	{
		int TiffNextExifBlockOffset = 0;
		TagTable = new Dictionary<ExifTagId, TagItem>[5];
		GetNextIfd(PrimaryDataOffset, out var IfdTable, out var IfdIndex);
		InitIfdPrimaryData(IfdTable, IfdIndex, out var PrivateDataOffset, out var GpsInfoDataOffset, out var ThumbnailDataOffset);
		GetNextIfd(PrivateDataOffset, out IfdTable, out IfdIndex);
		InitIfdPrivateData(IfdTable, IfdIndex, out var InteroperabilityOffset);
		GetNextIfd(GpsInfoDataOffset, out IfdTable, out IfdIndex);
		InitIfdGpsInfoData(IfdTable, IfdIndex);
		GetNextIfd(InteroperabilityOffset, out IfdTable, out IfdIndex);
		InitIfdInteroperability(IfdTable, IfdIndex);
		if (ImageType == ImageType.Tiff)
		{
			IfdTable = new byte[2];
			InitIfdThumbnailData(IfdTable, 0);
			TiffNextExifBlockOffset = ThumbnailDataOffset;
		}
		else
		{
			GetNextIfd(ThumbnailDataOffset, out IfdTable, out IfdIndex);
			InitIfdThumbnailData(IfdTable, IfdIndex);
		}
		return TiffNextExifBlockOffset;
	}

	private bool AddIfNotExists(Dictionary<ExifTagId, TagItem> Dict, TagItem TagData)
	{
		try
		{
			Dict.Add(TagData.TagId, TagData);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool CompareArrays(byte[] Array1, int StartIndex1, byte[] Array2)
	{
		if (Array1.Length >= StartIndex1 + Array2.Length)
		{
			bool IsEqual = true;
			int i = StartIndex1;
			foreach (byte b in Array2)
			{
				if (Array1[i] != b)
				{
					IsEqual = false;
					break;
				}
				i++;
			}
			return IsEqual;
		}
		return false;
	}

	private bool ArrayStartsWith(byte[] Array1, int Array1Length, byte[] Array2)
	{
		if (Array1Length >= Array2.Length)
		{
			bool IsEqual = true;
			int i = 0;
			foreach (byte b in Array2)
			{
				if (Array1[i] != b)
				{
					IsEqual = false;
					break;
				}
				i++;
			}
			return IsEqual;
		}
		return false;
	}

	private bool ReadUintElement(TagItem t, int ElementIndex, out uint Value)
	{
		bool Success = false;
		if (ElementIndex >= 0 && ElementIndex < t.ValueCount)
		{
			switch (t.TagType)
			{
			case ExifTagType.Byte:
				Value = t.ValueData[t.ValueIndex + ElementIndex];
				Success = true;
				break;
			case ExifTagType.UShort:
				Value = ExifReadUInt16(t.ValueData, t.ValueIndex + (ElementIndex << 1));
				Success = true;
				break;
			case ExifTagType.ULong:
			case ExifTagType.SLong:
				Value = ExifReadUInt32(t.ValueData, t.ValueIndex + (ElementIndex << 2));
				Success = true;
				break;
			default:
				Value = 0u;
				break;
			}
		}
		else
		{
			Value = 0u;
		}
		return Success;
	}

	private bool WriteUintElement(TagItem t, int ElementIndex, uint Value)
	{
		bool Success = false;
		if (t != null)
		{
			if (t.TagType == ExifTagType.Byte)
			{
				t.ValueData[t.ValueIndex + ElementIndex] = (byte)Value;
			}
			else if (t.TagType == ExifTagType.UShort)
			{
				ExifWriteUInt16(t.ValueData, t.ValueIndex + (ElementIndex << 1), (ushort)Value);
			}
			else
			{
				ExifWriteUInt32(t.ValueData, t.ValueIndex + (ElementIndex << 2), Value);
			}
			Success = true;
		}
		return Success;
	}

	private bool GetTagValueWithIdCode(ExifTag TagSpec, out string Value, ushort CodePage)
	{
		bool Success = false;
		bool IsUtf16Coded = false;
		bool IsAsciiCoded = false;
		Value = null;
		if (GetTagItem(TagSpec, out var t) && t.TagType == ExifTagType.Undefined && t.ValueCount >= 8)
		{
			if (CompareArrays(t.ValueData, t.ValueIndex, IdCodeUtf16))
			{
				IsUtf16Coded = true;
			}
			else if (CompareArrays(t.ValueData, t.ValueIndex, IdCodeAscii) || CompareArrays(t.ValueData, t.ValueIndex, IdCodeDefault))
			{
				IsAsciiCoded = true;
			}
			int i = t.ValueCount - 8;
			int j = t.ValueIndex + 8;
			if (IsUtf16Coded)
			{
				while (i >= 2 && t.ValueData[j + i - 2] == 0 && t.ValueData[j + i - 1] == 0)
				{
					i -= 2;
				}
				CodePage = (ushort)((ByteOrder != ExifByteOrder.BigEndian) ? 1200 : 1201);
				Value = Encoding.GetEncoding(CodePage).GetString(t.ValueData, j, i);
				Success = true;
			}
			else if (IsAsciiCoded)
			{
				while (i >= 1 && t.ValueData[j + i - 1] == 0)
				{
					i--;
				}
				if (CodePage == 1200 || CodePage == 1201)
				{
					CodePage = 20127;
				}
				Value = Encoding.GetEncoding(CodePage).GetString(t.ValueData, j, i);
				Success = true;
			}
		}
		return Success;
	}

	private bool SetTagValueWithIdCode(ExifTag TagSpec, string Value, ushort CodePage)
	{
		bool Success = false;
		if (CodePage == 1200 && ByteOrder == ExifByteOrder.BigEndian)
		{
			CodePage = 1201;
		}
		if (CodePage == 1201 && ByteOrder == ExifByteOrder.LittleEndian)
		{
			CodePage = 1200;
		}
		byte[] StringAsByteArray = Encoding.GetEncoding(CodePage).GetBytes(Value);
		int StrByteLen = StringAsByteArray.Length;
		int TotalByteCount = 8 + StrByteLen;
		TagItem t = PrepareTagForCompleteWriting(TagSpec, ExifTagType.Undefined, TotalByteCount);
		if (t != null)
		{
			byte[] RequiredIdCode = ((CodePage != 1200 && CodePage != 1201) ? IdCodeAscii : IdCodeUtf16);
			Array.Copy(RequiredIdCode, 0, t.ValueData, t.ValueIndex, 8);
			Array.Copy(StringAsByteArray, 0, t.ValueData, t.ValueIndex + 8, StrByteLen);
			Success = true;
		}
		return Success;
	}

	private bool ReadURatElement(TagItem t, int ElementIndex, out uint Numer, out uint Denom)
	{
		bool Success = false;
		if (ElementIndex >= 0 && ElementIndex < t.ValueCount)
		{
			if (t.TagType == ExifTagType.SRational || t.TagType == ExifTagType.URational)
			{
				int i = t.ValueIndex + (ElementIndex << 3);
				Numer = ExifReadUInt32(t.ValueData, i);
				Denom = ExifReadUInt32(t.ValueData, i + 4);
				Success = true;
			}
			else
			{
				Numer = 0u;
				Denom = 0u;
			}
		}
		else
		{
			Numer = 0u;
			Denom = 0u;
		}
		return Success;
	}

	private bool WriteURatElement(TagItem t, int ElementIndex, uint Numer, uint Denom)
	{
		bool Success = false;
		if (t != null)
		{
			int i = t.ValueIndex + (ElementIndex << 3);
			ExifWriteUInt32(t.ValueData, i, Numer);
			ExifWriteUInt32(t.ValueData, i + 4, Denom);
			Success = true;
		}
		return Success;
	}

	private bool GetTagItem(ExifTag TagSpec, out TagItem t)
	{
		ExifIfd Ifd = ExtractIfd(TagSpec);
		if ((uint)Ifd < 5u)
		{
			return TagTable[(int)Ifd].TryGetValue(ExtractTagId(TagSpec), out t);
		}
		t = null;
		return false;
	}

	private TagItem PrepareTagForArrayItemWriting(ExifTag TagSpec, ExifTagType TagType, int ArrayIndex)
	{
		TagItem t = null;
		ExifIfd Ifd = ExtractIfd(TagSpec);
		if ((uint)Ifd < 5u && (uint)ArrayIndex < 268435455u)
		{
			Dictionary<ExifTagId, TagItem> IfdTagTable = TagTable[(int)Ifd];
			if (IfdTagTable.TryGetValue(ExtractTagId(TagSpec), out t))
			{
				int ValueCount = t.ValueCount;
				if (ArrayIndex >= ValueCount)
				{
					ValueCount = ArrayIndex + 1;
				}
				t.SetTagTypeAndValueCount(TagType, ValueCount, KeepExistingData: true);
			}
			else
			{
				int ValueCount2 = ArrayIndex + 1;
				t = new TagItem(TagSpec, TagType, ValueCount2);
				IfdTagTable.Add(t.TagId, t);
			}
		}
		return t;
	}

	private TagItem PrepareTagForCompleteWriting(ExifTag TagSpec, ExifTagType TagType, int ValueCount)
	{
		TagItem t = null;
		ExifIfd Ifd = ExtractIfd(TagSpec);
		if ((uint)Ifd < 5u && (uint)ValueCount <= 268435455u)
		{
			Dictionary<ExifTagId, TagItem> IfdTagTable = TagTable[(int)Ifd];
			if (IfdTagTable.TryGetValue(ExtractTagId(TagSpec), out t))
			{
				t.SetTagTypeAndValueCount(TagType, ValueCount, KeepExistingData: false);
			}
			else
			{
				t = new TagItem(TagSpec, TagType, ValueCount);
				IfdTagTable.Add(t.TagId, t);
			}
		}
		return t;
	}

	private static int CalculateTwoDigitDecNumber(byte[] ByteArr, int Index)
	{
		int Value = -1;
		int d1 = ByteArr[Index];
		int d2 = ByteArr[Index + 1];
		if (d1 >= 48 && d1 <= 57 && d2 >= 48 && d2 <= 57)
		{
			Value = (d1 - 48) * 10 + (d2 - 48);
		}
		return Value;
	}

	private static void ConvertTwoDigitNumberToByteArr(byte[] ByteArr, ref int Index, int Value)
	{
		ByteArr[Index] = (byte)(Value / 10 + 48);
		Index++;
		ByteArr[Index] = (byte)(Value % 10 + 48);
		Index++;
	}

	private void ClearIfd_Unchecked(ExifIfd Ifd)
	{
		TagTable[(int)Ifd].Clear();
	}

	private void RemoveAllTagsFromIfdPrimaryData()
	{
		if (ImageType == ImageType.Tiff)
		{
			Dictionary<ExifTagId, TagItem> PrimaryDataIfdTable = TagTable[0];
			List<ExifTagId> TagIdsToBeRemoved = new List<ExifTagId>(PrimaryDataIfdTable.Count);
			foreach (ExifTagId tid2 in PrimaryDataIfdTable.Keys)
			{
				if (!IsInternalTiffTag(tid2) && !IsMetaDataTiffTag(tid2))
				{
					TagIdsToBeRemoved.Add(tid2);
				}
			}
			{
				foreach (ExifTagId tid in TagIdsToBeRemoved)
				{
					PrimaryDataIfdTable.Remove(tid);
				}
				return;
			}
		}
		ClearIfd_Unchecked(ExifIfd.PrimaryData);
	}

	private void RemoveThumbnailImage_Internal()
	{
		ThumbnailImage = null;
		ThumbnailStartIndex = 0;
		ThumbnailByteCount = 0;
	}

	private bool GetGpsCoordinateHelper(out GeoCoordinate Value, ExifTag ValueTag, ExifTag RefTag, char Cp1, char Cp2)
	{
		bool Success = false;
		if (GetTagValue(ValueTag, out ExifRational Deg, 0) && Deg.IsValid() && GetTagValue(ValueTag, out ExifRational Min, 1) && Min.IsValid() && GetTagValue(ValueTag, out ExifRational Sec, 2) && Sec.IsValid() && GetTagValue(RefTag, out var Ref, StrCoding.Utf8) && Ref.Length == 1)
		{
			char CardinalPoint = Ref[0];
			if (CardinalPoint == Cp1 || CardinalPoint == Cp2)
			{
				Value.Degree = ExifRational.ToDecimal(Deg);
				Value.Minute = ExifRational.ToDecimal(Min);
				Value.Second = ExifRational.ToDecimal(Sec);
				Value.CardinalPoint = CardinalPoint;
				Success = true;
			}
			else
			{
				Value = default(GeoCoordinate);
			}
		}
		else
		{
			Value = default(GeoCoordinate);
		}
		return Success;
	}

	private bool SetGpsCoordinateHelper(GeoCoordinate Value, ExifTag ValueTag, ExifTag RefTag, char Cp1, char Cp2)
	{
		bool Success = false;
		ExifRational Deg = ExifRational.FromDecimal(Value.Degree);
		ExifRational Min = ExifRational.FromDecimal(Value.Minute);
		ExifRational Sec = ExifRational.FromDecimal(Value.Second);
		if (SetTagValue(ValueTag, Deg, ExifTagType.URational) && SetTagValue(ValueTag, Min, ExifTagType.URational, 1) && SetTagValue(ValueTag, Sec, ExifTagType.URational, 2) && (Value.CardinalPoint == Cp1 || Value.CardinalPoint == Cp2) && SetTagValue(RefTag, Value.CardinalPoint.ToString(), StrCoding.Utf8))
		{
			Success = true;
		}
		return Success;
	}

	private bool GetDateAndTimeWithMillisecHelper(out DateTime Value, ExifTag DateAndTimeTag, ExifTag MillisecTag)
	{
		bool Success = false;
		if (GetTagValue(DateAndTimeTag, out Value, ExifDateFormat.DateAndTime))
		{
			Success = true;
			if (GetTagValue(MillisecTag, out var SubSec, StrCoding.Utf8))
			{
				string s = SubSec;
				int len = s.Length;
				if (len > 3)
				{
					s = s.Substring(0, 3);
				}
				if (int.TryParse(s, out var MilliSec) && MilliSec >= 0)
				{
					switch (len)
					{
					case 1:
						MilliSec *= 100;
						break;
					case 2:
						MilliSec *= 10;
						break;
					}
					Value = Value.AddMilliseconds(MilliSec);
				}
			}
		}
		return Success;
	}

	private bool SetDateAndTimeWithMillisecHelper(DateTime Value, ExifTag DateAndTimeTag, ExifTag MillisecTag)
	{
		bool Success = false;
		if (SetTagValue(DateAndTimeTag, Value))
		{
			Success = true;
			int MilliSec = Value.Millisecond;
			if (MilliSec != 0 || TagExists(MillisecTag))
			{
				string s = MilliSec.ToString("000");
				Success = Success && SetTagValue(MillisecTag, s, StrCoding.Utf8);
			}
		}
		return Success;
	}

	private static void InitInternalTiffTags()
	{
		InternalTiffTagsBitArray = new BitArray(533, defaultValue: false);
		int len = InternalTiffTagsBitArray.Length;
		ExifTagId[] internalTiffTags = InternalTiffTags;
		for (int i = 0; i < internalTiffTags.Length; i++)
		{
			int TagIdInt = (int)internalTiffTags[i];
			if (TagIdInt >= len)
			{
				len <<= 1;
				if (TagIdInt >= len)
				{
					len = TagIdInt + 1;
				}
				InternalTiffTagsBitArray.Length = len;
			}
			InternalTiffTagsBitArray.Set(TagIdInt, value: true);
		}
	}

	private static bool IsInternalTiffTag(ExifTagId TagId)
	{
		if (InternalTiffTagsBitArray == null)
		{
			InitInternalTiffTags();
		}
		if ((int)TagId < InternalTiffTagsBitArray.Length)
		{
			return InternalTiffTagsBitArray.Get((int)TagId);
		}
		return false;
	}

	private static bool IsMetaDataTiffTag(ExifTagId TagId)
	{
		if (TagId != ExifTagId.XmpMetadata)
		{
			return TagId == ExifTagId.IptcMetadata;
		}
		return true;
	}

	private void SwapByteOrderOfTagData(ExifIfd Ifd, TagItem t)
	{
		switch (Ifd)
		{
		case ExifIfd.PrimaryData:
			if (IsMetaDataTiffTag(t.TagId))
			{
				return;
			}
			break;
		case ExifIfd.PrivateData:
			if (t.TagId == ExifTagId.UserComment && t.TagType == ExifTagType.Undefined)
			{
				int k2 = t.ValueIndex + 8;
				int Utf16CharCount = (t.ValueCount - 8) / 2;
				for (int m = 0; m < Utf16CharCount; m++)
				{
					Swap2ByteValue(t.ValueData, k2);
					k2 += 2;
				}
				return;
			}
			break;
		}
		switch (t.TagType)
		{
		case ExifTagType.UShort:
		case ExifTagType.SShort:
		{
			int n = t.ValueIndex;
			for (int l = 0; l < t.ValueCount; l++)
			{
				Swap2ByteValue(t.ValueData, n);
				n += 2;
			}
			break;
		}
		case ExifTagType.ULong:
		case ExifTagType.SLong:
		case ExifTagType.Float:
		{
			int n = t.ValueIndex;
			for (int j = 0; j < t.ValueCount; j++)
			{
				Swap4ByteValue(t.ValueData, n);
				n += 4;
			}
			break;
		}
		case ExifTagType.URational:
		case ExifTagType.SRational:
		{
			int n = t.ValueIndex;
			for (int k = 0; k < t.ValueCount; k++)
			{
				Swap4ByteValue(t.ValueData, n);
				n += 4;
				Swap4ByteValue(t.ValueData, n);
				n += 4;
			}
			break;
		}
		case ExifTagType.Double:
		{
			int n = t.ValueIndex;
			for (int i = 0; i < t.ValueCount; i++)
			{
				Array.Reverse(t.ValueData, n, 8);
				n += 8;
			}
			break;
		}
		case ExifTagType.SByte:
		case ExifTagType.Undefined:
			break;
		}
	}

	private void Swap2ByteValue(byte[] b, int i)
	{
		byte j = b[i];
		b[i] = b[i + 1];
		b[i + 1] = j;
	}

	private void Swap4ByteValue(byte[] b, int i)
	{
		byte j = b[i];
		b[i] = b[i + 3];
		b[i + 3] = j;
		j = b[i + 1];
		b[i + 1] = b[i + 2];
		b[i + 2] = j;
	}

	private TagItem CopyTagItemDeeply(ExifIfd Ifd, TagItem TagItemToBeCopied, bool SwapByteOrder)
	{
		TagItem NewTag = new TagItem(TagItemToBeCopied.TagId, TagItemToBeCopied.TagType, TagItemToBeCopied.ValueCount);
		Array.Copy(TagItemToBeCopied.ValueData, TagItemToBeCopied.ValueIndex, NewTag.ValueData, 0, NewTag.ByteCount);
		if (SwapByteOrder)
		{
			SwapByteOrderOfTagData(Ifd, NewTag);
		}
		return NewTag;
	}
}
