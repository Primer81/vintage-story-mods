using System;

namespace CompactExifLib;

public class ExifException : Exception
{
	public ExifErrCode ErrorCode;

	public override string Message => ErrorCode switch
	{
		ExifErrCode.ImageTypeIsNotSupported => "Image type is not supported!", 
		ExifErrCode.ImageHasUnsupportedFeatures => "Image has unsupported features!", 
		ExifErrCode.InternalImageStructureIsWrong => "Internal image structure is wrong!", 
		ExifErrCode.ExifBlockHasIllegalContent => "EXIF block has an illegal content!", 
		ExifErrCode.ExifDataAreTooLarge => "EXIF data are too large: 64 kB for JPEG files, 2 GB for TIFF files!", 
		ExifErrCode.ImageTypesDoNotMatch => "Image types do not match!", 
		_ => "Internal error!", 
	};

	public ExifException(ExifErrCode _ErrorCode)
	{
		ErrorCode = _ErrorCode;
	}
}
