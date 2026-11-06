namespace Fsp.Interop;

internal struct FspFileSystemOperationContext
{
	internal unsafe FspFsctlTransactReq* Request;

	internal unsafe FspFsctlTransactRsp* Response;
}
