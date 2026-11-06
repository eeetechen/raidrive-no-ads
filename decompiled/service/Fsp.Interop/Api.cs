using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using ICJOPEKMCHLGNAOFOAPFCIHMKDAHIGMICIAE;
using LIKCEKLPJGFLBICDCNPHGANDLBGFGNJDEDPF;

namespace Fsp.Interop;

[SuppressUnmanagedCodeSecurity]
internal static class Api
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct Proto
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemPreflight([MarshalAs(UnmanagedType.LPWStr)] string DevicePath, [MarshalAs(UnmanagedType.LPWStr)] string MountPoint);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemCreate([MarshalAs(UnmanagedType.LPWStr)] string DevicePath, ref VolumeParams VolumeParams, nint Interface, out nint PFileSystem);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemDelete(nint FileSystem);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemSetMountPoint(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string MountPoint);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemSetMountPointEx(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string MountPoint, nint SecurityDescriptor);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemRemoveMountPoint(nint FileSystem);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemStartDispatcher(nint FileSystem, uint ThreadCount);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemStopDispatcher(nint FileSystem);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemSendResponse(nint FileSystem, ref FspFsctlTransactRsp Response);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemNotifyBegin(nint FileSystem, uint Timeout);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemNotifyEnd(nint FileSystem);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemNotify(nint FileSystem, nint NotifyInfo, nuint Size);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal unsafe delegate FspFileSystemOperationContext* FspFileSystemGetOperationContext();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate nint FspFileSystemMountPointF(nint FileSystem);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemSetOperationGuardStrategyF(nint FileSystem, int GuardStrategy);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemSetDebugLogF(nint FileSystem, uint DebugLog);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint FspFileSystemOperationProcessIdF();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool FspFileSystemAddDirInfo(nint DirInfo, nint Buffer, uint Length, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool FspFileSystemFindReparsePoint(nint FileSystem, GetReparsePointByName GetReparsePointByName, nint Context, [MarshalAs(UnmanagedType.LPWStr)] string FileName, out uint PReparsePointIndex);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemResolveReparsePoints(nint FileSystem, GetReparsePointByName GetReparsePointByName, nint Context, [MarshalAs(UnmanagedType.LPWStr)] string FileName, uint ReparsePointIndex, [MarshalAs(UnmanagedType.U1)] bool ResolveLastPathComponent, out IoStatusBlock PIoStatus, nint Buffer, nint PSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspFileSystemCanReplaceReparsePoint(nint CurrentReparseData, nuint CurrentReparseDataSize, nint ReplaceReparseData, nuint ReplaceReparseDataSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool FspFileSystemAddStreamInfo(nint StreamInfo, nint Buffer, uint Length, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool FspFileSystemAddEa(nint SingleEa, nint Ea, uint EaLength, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool FspFileSystemAddNotifyInfo(nint NotifyInfo, nint Buffer, uint Length, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemStopServiceIfNecessary(nint FileSystem, [MarshalAs(UnmanagedType.U1)] bool Normally);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool FspFileSystemAcquireDirectoryBuffer(ref nint PDirBuffer, [MarshalAs(UnmanagedType.U1)] bool Reset, out int PResult);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool FspFileSystemFillDirectoryBuffer(ref nint PDirBuffer, ref DirInfo DirInfo, out int PResult);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemReleaseDirectoryBuffer(ref nint PDirBuffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemReadDirectoryBuffer(ref nint PDirBuffer, [MarshalAs(UnmanagedType.LPWStr)] string Marker, nint Buffer, uint Length, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspFileSystemDeleteDirectoryBuffer(ref nint PDirBuffer);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspSetSecurityDescriptor(nint InputDescriptor, uint SecurityInformation, nint ModificationDescriptor, out nint PSecurityDescriptor);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspDeleteSecurityDescriptor(nint SecurityDescriptor, nint CreateFunc);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspServiceCreate([MarshalAs(UnmanagedType.LPWStr)] string ServiceName, ServiceStart OnStart, ServiceStop OnStop, ServiceControl OnControl, out nint PService);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspServiceDelete(nint Service);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspServiceAllowConsoleMode(nint Service);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspServiceRequestTime(nint Service, uint Time);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspServiceSetExitCode(nint Service, uint ExitCode);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint FspServiceGetExitCode(nint Service);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspServiceLoop(nint Service);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspServiceStop(nint Service);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspServiceLog(uint Type, [MarshalAs(UnmanagedType.LPWStr)] string Format, [MarshalAs(UnmanagedType.LPWStr)] string Message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspVersion(out uint PVersion);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int FspNtStatusFromWin32(uint Error);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate uint FspWin32FromNtStatus(int Status);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspDebugLog([MarshalAs(UnmanagedType.LPStr)] string Format, [MarshalAs(UnmanagedType.LPStr)] string Message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void FspDebugLogSetHandle(nint Handle);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetReparsePointByName(nint FileSystem, nint Context, [MarshalAs(UnmanagedType.LPWStr)] string FileName, [MarshalAs(UnmanagedType.U1)] bool IsDirectory, nint Buffer, nint PSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int ServiceStart(nint Service, uint Argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] Argv);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int ServiceStop(nint Service);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int ServiceControl(nint Service, uint Control, uint EventType, nint EventData);
	}

	internal delegate int EnumerateEa(long FileDesc, ref object Context, string EaName, byte[] EaValue, bool NeedEa);

	private struct SYSTEM_INFO
	{
		internal const uint PROCESSOR_ARCHITECTURE_INTEL = 0u;

		internal const uint PROCESSOR_ARCHITECTURE_MIPS = 1u;

		internal const uint PROCESSOR_ARCHITECTURE_ALPHA = 2u;

		internal const uint PROCESSOR_ARCHITECTURE_PPC = 3u;

		internal const uint PROCESSOR_ARCHITECTURE_SHX = 4u;

		internal const uint PROCESSOR_ARCHITECTURE_ARM = 5u;

		internal const uint PROCESSOR_ARCHITECTURE_IA64 = 6u;

		internal const uint PROCESSOR_ARCHITECTURE_ALPHA64 = 7u;

		internal const uint PROCESSOR_ARCHITECTURE_MSIL = 8u;

		internal const uint PROCESSOR_ARCHITECTURE_AMD64 = 9u;

		internal const uint PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10u;

		internal const uint PROCESSOR_ARCHITECTURE_NEUTRAL = 11u;

		internal const uint PROCESSOR_ARCHITECTURE_ARM64 = 12u;

		internal const uint PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13u;

		internal const uint PROCESSOR_ARCHITECTURE_IA32_ON_ARM64 = 14u;

		internal const uint PROCESSOR_ARCHITECTURE_UNKNOWN = 65535u;

		internal ushort wProcessorArchitecture;

		internal ushort wReserved;

		internal uint dwPageSize;

		internal nint lpMinimumApplicationAddress;

		internal nint lpMaximumApplicationAddress;

		internal nint dwActiveProcessorMask;

		internal uint dwNumberOfProcessors;

		internal uint dwProcessorType;

		internal uint dwAllocationGranularity;

		internal ushort wProcessorLevel;

		internal ushort wProcessorRevision;
	}

	internal static object FspFileSystemPreflight;

	internal static object FspFileSystemCreate;

	internal static object FspFileSystemDelete;

	internal static object FspFileSystemSetMountPoint;

	internal static object _FspFileSystemSetMountPointEx;

	internal static object FspFileSystemRemoveMountPoint;

	internal static object FspFileSystemStartDispatcher;

	internal static object FspFileSystemStopDispatcher;

	internal static object FspFileSystemSendResponse;

	internal static object FspFileSystemNotifyBegin;

	internal static object FspFileSystemNotifyEnd;

	internal static object _FspFileSystemNotify;

	internal static object FspFileSystemGetOperationContext;

	internal static object FspFileSystemMountPoint;

	internal static object FspFileSystemSetOperationGuardStrategy;

	internal static object FspFileSystemSetDebugLog;

	internal static object FspFileSystemOperationProcessId;

	internal static object _FspFileSystemAddDirInfo;

	internal static object FspFileSystemFindReparsePoint;

	internal static object FspFileSystemResolveReparsePoints;

	internal static object _FspFileSystemCanReplaceReparsePoint;

	internal static object _FspFileSystemAddStreamInfo;

	internal static object _FspFileSystemAddEa;

	internal static object _FspFileSystemAddNotifyInfo;

	internal static object FspFileSystemStopServiceIfNecessary;

	internal static object FspFileSystemAcquireDirectoryBuffer;

	internal static object FspFileSystemFillDirectoryBuffer;

	internal static object FspFileSystemReleaseDirectoryBuffer;

	internal static object FspFileSystemReadDirectoryBuffer;

	internal static object FspFileSystemDeleteDirectoryBuffer;

	internal static object FspSetSecurityDescriptor;

	internal static nint _FspSetSecurityDescriptorPtr;

	internal static object FspDeleteSecurityDescriptor;

	internal static object FspServiceCreate;

	internal static object FspServiceDelete;

	internal static object FspServiceAllowConsoleMode;

	internal static object FspServiceRequestTime;

	internal static object FspServiceSetExitCode;

	internal static object FspServiceGetExitCode;

	internal static object FspServiceLoop;

	internal static object FspServiceStop;

	internal static object FspServiceLog;

	internal static object FspVersion;

	internal static object FspNtStatusFromWin32;

	internal static object FspWin32FromNtStatus;

	internal static object FspDebugLog;

	internal static object FspDebugLogSetHandle;

	internal static object ProductName;

	internal static object ProductFileName;

	private static object CMOCOAKAPMHPEHECCOBNJEALEFIDPJDHJKEF;

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int FspFileSystemSetMountPointEx(nint FileSystem, object MountPoint, object SecurityDescriptor)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static ulong FspFileSystemGetOperationRequestHint()
	{
		return 0uL;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static uint FspFileSystemGetOperationRequesShareAccess()
	{
		return 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static uint FspFileSystemGetOperationRequesFileAttributes()
	{
		return 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool FspFileSystemAddDirInfo(ref DirInfo DirInfo, nint Buffer, uint Length, out uint PBytesTransferred)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool FspFileSystemEndDirInfo(nint Buffer, uint Length, out uint PBytesTransferred)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool FspFileSystemAddStreamInfo(ref StreamInfo StreamInfo, nint Buffer, uint Length, out uint PBytesTransferred)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool FspFileSystemEndStreamInfo(nint Buffer, uint Length, out uint PBytesTransferred)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool FspFileSystemAddNotifyInfo(ref NotifyInfoInternal NotifyInfo, nint Buffer, uint Length, out uint PBytesTransferred)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int FspFileSystemEnumerateEa(long FileDesc, object EnumerateEa, nint Ea, uint EaLength)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool FspFileSystemAddEa(ref FullEaInformation EaInfo, nint Buffer, uint Length, out uint PBytesTransferred)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool FspFileSystemEndEa(nint Buffer, uint Length, out uint PBytesTransferred)
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int FspFileSystemNotify(nint FileSystem, object NotifyInfoArray)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object GetUserContext(nint NativePtr)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void SetUserContext(nint NativePtr, object Obj)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void DisposeUserContext(nint NativePtr)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int CopySecurityDescriptor(object SecurityDescriptorBytes, nint SecurityDescriptor, nint PSecurityDescriptorSize)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static byte[] MakeSecurityDescriptor(nint SecurityDescriptor)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static byte[] ModifySecurityDescriptor(object SecurityDescriptorBytes, uint SecurityInformation, object ModificationDescriptorBytes)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int ModifySecurityDescriptorEx(object SecurityDescriptorBytes, uint SecurityInformation, object ModificationDescriptorBytes, ref byte[] ModifiedDescriptorBytes)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int CopyReparsePoint(object ReparseData, nint Buffer, nint PSize)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static byte[] MakeReparsePoint(nint Buffer, nuint Size)
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int FspFileSystemCanReplaceReparsePoint(object CurrentReparseData, object ReplaceReparseData)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static int SetDebugLogFile(object FileName)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Version GetVersion()
	{
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static nint LoadDll()
	{
		return (nint)(object)null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static nint GetEntryPointPtr(nint Module, object Name)
	{
		return (nint)(object)null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T GetEntryPoint<T>(nint Module, object Name)
	{
		return (T)null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void LoadProto(nint Module)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void CheckVersion()
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static Api()
	{
		GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJOLIPBMJHCEOLFMLAFNMMPLOBGECDDDOFLK();
		int num = 4;
		while (true)
		{
			int num2 = num;
			while (true)
			{
				int num3 = num2;
				while (true)
				{
					switch (num3)
					{
					default:
						if (num2 == 13)
						{
							return;
						}
						goto IL_0041;
					case 4:
						GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJNKIHNGHFGCNDIMBMPIOEOHDHLMEJGICCDP();
						num3 = 3;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_4c52378a420f42a9a525fb2b1bdc3488 != 0)
						{
							num3 = 0;
						}
						continue;
					case 6:
						ProductFileName = GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.LGFICOJCEDNCDBGHPGKOJPBMAGHFKCMNGIEM(0x2F511CEA ^ _003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_8d406889785c4cedab6dc17966e6c1ef);
						num3 = 5;
						continue;
					case 0:
						goto end_IL_000e;
					case 2:
						GCKJPIMLLCGALLONMOJGBOFOCIIOGJMOFLEI.JNMLINCDJFDFJOCKHAEFNDGOGEFAEBOBFIHD();
						num3 = 1;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_3094742966a548ee93ff30b44e505ad6 == 0)
						{
							num3 = 7;
						}
						continue;
					case 1:
						ProductName = GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.LGFICOJCEDNCDBGHPGKOJPBMAGHFKCMNGIEM(0x6DD3DB6B ^ _003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_b9be7489726e46359a70038e102e1e43);
						num3 = 6;
						continue;
					case 5:
						LoadProto(LoadDll());
						num3 = 0;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_7c5d647723a942fda881944281ba4b17 != 0)
						{
							num3 = 8;
						}
						continue;
					case 3:
						break;
					}
					goto IL_0129;
					IL_0041:
					if (num2 == 994)
					{
						break;
					}
					goto IL_0129;
					IL_0129:
					GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.DLAMFDPKJOBMPKPOENJIKMDJJANMNLIGPAPJ();
					num3 = 2;
					if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_06489ccc250a4aefa6dd80dff4dfbbcf != 0)
					{
						num3 = 2;
					}
				}
				continue;
				end_IL_000e:
				break;
			}
			CheckVersion();
			num = 13;
		}
	}

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
	private static extern nint LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string DllName);

	[DllImport("kernel32.dll", BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true, ThrowOnUnmappableChar = true)]
	private static extern nint GetProcAddress(nint hModule, string lpProcName);

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
	private static extern void GetSystemInfo(out SYSTEM_INFO SystemInfo);

	[DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall)]
	private static extern uint GetSecurityDescriptorLength(nint SecurityDescriptor);

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
	private static extern nint GetStdHandle(uint nStdHandle);

	[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
	private static extern nint CreateFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, uint dwDesiredAccess, uint dwShareMode, nint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool DHDDMPMBFOFEOFJJDLELKJNIFNHJPJLKDAFM()
	{
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static Api HMDPOEFDIBJKGAIIKODAJBEOOKPIKBLDDPLL()
	{
		return null;
	}
}
