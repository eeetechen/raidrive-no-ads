using System;

namespace RaiDrive.Service;

[Flags]
internal enum AddUploadWhere
{
	None = 0,
	OpenFile = 1,
	ChangeFileSize = 2,
	UploadWriteFile = 4,
	FinalUpload = 8
}
