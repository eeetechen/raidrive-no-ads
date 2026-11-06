using System.Runtime.InteropServices;

namespace Fsp.Interop;

[StructLayout(LayoutKind.Explicit)]
internal struct FspFsctlTransactReq
{
	[FieldOffset(0)]
	internal ushort Version;

	[FieldOffset(2)]
	internal ushort Size;

	[FieldOffset(4)]
	internal uint Kind;

	[FieldOffset(8)]
	internal ulong Hint;

	[FieldOffset(0)]
	internal unsafe fixed byte Padding[88];

	[FieldOffset(16)]
	internal CreteRequest Create;
}
