using System;

namespace RaiDrive.Service;

[Flags]
internal enum FinalCloseWhere
{
	None = 0,
	SetFileTime1 = 2,
	SetFileTime2 = 4,
	FlushFile = 8,
	CleanupFile = 0x10,
	CloseHandle = 0x20,
	CloseFile = 0x40,
	MergeUpload = 0x80,
	CanDeleteFile = 0x100
}
