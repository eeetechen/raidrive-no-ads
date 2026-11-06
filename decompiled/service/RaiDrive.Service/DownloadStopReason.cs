namespace RaiDrive.Service;

internal enum DownloadStopReason
{
	None,
	Start,
	Close,
	Invalidate,
	Delete,
	ChangeSize,
	NewStart,
	Complete,
	NotRunning,
	CancelAll,
	Dispose
}
