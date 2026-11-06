namespace RaiDrive.Service.Upload;

internal enum AsyncCancleReason
{
	WriteFile,
	ChangeSize,
	MergeRename
}
