namespace Fsp.Interop;

internal enum NotifyAction : uint
{
	Added = 1u,
	Removed,
	Modified,
	RenamedOldName,
	RenamedNewName,
	AddedStream,
	RemovedStream,
	ModifiedStream,
	RemovedByDelete,
	IdNotTunnelled,
	TunnelledIdCollision
}
