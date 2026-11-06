using System;

namespace RaiDrive.Service.Revision;

[Flags]
internal enum CommonRevisionButtons
{
	Delete = 1,
	Rollback = 2,
	Download = 4,
	Custom = 8
}
