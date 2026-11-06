using System.Runtime.InteropServices;

namespace Fsp.Interop;

[StructLayout(LayoutKind.Explicit)]
internal struct FspFsctlTransactRsp
{
	[FieldOffset(0)]
	internal ushort Version;

	[FieldOffset(2)]
	internal ushort Size;

	[FieldOffset(4)]
	internal uint Kind;

	[FieldOffset(8)]
	internal ulong Hint;

	[FieldOffset(16)]
	internal IoStatus IoStatus;

	[FieldOffset(24)]
	internal FSPFileInfo WriteFileInfo;

	[FieldOffset(0)]
	internal unsafe fixed byte Padding[128];
}
