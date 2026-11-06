namespace Fsp.Interop;

internal struct CreteRequest
{
	internal uint CreateOptions;

	internal uint FileAttributes;

	internal FspFsctlTransactBuf SecurityDescriptor;

	internal ulong AllocationSize;

	internal ulong AccessToken;

	internal uint DesiredAccess;

	internal uint GrantedAccess;

	internal uint ShareAccess;
}
