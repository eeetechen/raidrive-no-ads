using System;

namespace RaiDrive.Service.Revision;

[Flags]
internal enum RevisionActions
{
	None = 0,
	Delete = 1,
	DeleteAll = 2,
	Rollback = 4,
	Download = 8,
	Custom1 = 0x10,
	Custom2 = 0x20
}
