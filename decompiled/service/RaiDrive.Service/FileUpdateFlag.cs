using System;

namespace RaiDrive.Service;

[Flags]
internal enum FileUpdateFlag
{
	None = 0,
	UpdateFileMeta = 1,
	Delete = 2,
	ClearCache = 4,
	SendNotify = 8
}
