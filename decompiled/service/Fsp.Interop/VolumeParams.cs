using System.Runtime.CompilerServices;
using LIKCEKLPJGFLBICDCNPHGANDLBGFGNJDEDPF;

namespace Fsp.Interop;

internal struct VolumeParams
{
	internal const uint CaseSensitiveSearch = 1u;

	internal const uint CasePreservedNames = 2u;

	internal const uint UnicodeOnDisk = 4u;

	internal const uint PersistentAcls = 8u;

	internal const uint ReparsePoints = 16u;

	internal const uint ReparsePointsAccessCheck = 32u;

	internal const uint NamedStreams = 64u;

	internal const uint HardLinks = 128u;

	internal const uint ExtendedAttributes = 256u;

	internal const uint ReadOnlyVolume = 512u;

	internal const uint PostCleanupWhenModifiedOnly = 1024u;

	internal const uint PassQueryDirectoryPattern = 2048u;

	internal const uint AlwaysUseDoubleBuffering = 4096u;

	internal const uint PassQueryDirectoryFileName = 8192u;

	internal const uint FlushAndPurgeOnCleanup = 16384u;

	internal const uint DeviceControl = 32768u;

	internal const uint UmFileContextIsUserContext2 = 65536u;

	internal const uint UmFileContextIsFullContext = 131072u;

	internal const uint AllowOpenInKernelMode = 16777216u;

	internal const uint CasePreservedExtendedAttributes = 33554432u;

	internal const uint WslFeatures = 67108864u;

	internal const uint RejectIrpPriorToTransact0 = 268435456u;

	internal const uint SupportsPosixUnlinkRename = 536870912u;

	internal const uint PostDispositionWhenNecessaryOnly = 1073741824u;

	internal const int PrefixSize = 192;

	internal const int FileSystemNameSize = 16;

	internal const uint VolumeInfoTimeoutValid = 1u;

	internal const uint DirInfoTimeoutValid = 2u;

	internal const uint SecurityTimeoutValid = 4u;

	internal const uint StreamInfoTimeoutValid = 8u;

	internal const uint EaTimeoutValid = 16u;

	internal ushort Version;

	internal ushort SectorSize;

	internal ushort SectorsPerAllocationUnit;

	internal ushort MaxComponentLength;

	internal ulong VolumeCreationTime;

	internal uint VolumeSerialNumber;

	internal uint TransactTimeout;

	internal uint IrpTimeout;

	internal uint IrpCapacity;

	internal uint FileInfoTimeout;

	internal uint Flags;

	internal unsafe fixed ushort Prefix[192];

	internal unsafe fixed ushort FileSystemName[16];

	internal uint AdditionalFlags;

	internal uint VolumeInfoTimeout;

	internal uint DirInfoTimeout;

	internal uint SecurityTimeout;

	internal uint StreamInfoTimeout;

	internal uint EaTimeout;

	internal uint FsextControlCode;

	internal unsafe fixed uint Reserved32[1];

	internal unsafe fixed ulong Reserved64[2];

	private static object CMOCOAKAPMHPEHECCOBNJEALEFIDPJDHJKEF;

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal string GetPrefix()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal void SetPrefix(string Value)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal string GetFileSystemName()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal void SetFileSystemName(string Value)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal bool IsPrefixEmpty()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static VolumeParams()
	{
		GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJOLIPBMJHCEOLFMLAFNMMPLOBGECDDDOFLK();
		GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJNKIHNGHFGCNDIMBMPIOEOHDHLMEJGICCDP();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool DHDDMPMBFOFEOFJJDLELKJNIFNHJPJLKDAFM()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object HMDPOEFDIBJKGAIIKODAJBEOOKPIKBLDDPLL()
	{
		return null;
	}
}
