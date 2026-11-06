using System;

namespace Fsp.Interop;

[Flags]
internal enum NotifyFilter : uint
{
	None = 0u,
	ChangeFileName = 1u,
	ChangeDirName = 2u,
	ChangeName = 3u,
	ChangeAttributes = 4u,
	ChangeSize = 8u,
	ChangeLastWrite = 0x10u,
	ChangeLastAccess = 0x20u,
	ChangeCreation = 0x40u,
	ChangeEa = 0x80u,
	ChangeSecurity = 0x100u,
	ChangeStreamName = 0x200u,
	ChangeStreamSize = 0x400u,
	ChangeStreamWrite = 0x800u
}
