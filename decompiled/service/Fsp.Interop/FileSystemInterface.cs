using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ICJOPEKMCHLGNAOFOAPFCIHMKDAHIGMICIAE;
using LIKCEKLPJGFLBICDCNPHGANDLBGFGNJDEDPF;

namespace Fsp.Interop;

internal struct FileSystemInterface
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct Proto
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetVolumeInfo(nint FileSystem, out FSPVolumeInfo VolumeInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int SetVolumeLabel(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string VolumeLabel, out FSPVolumeInfo VolumeInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetSecurityByName(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string FileName, nint PFileAttributes, nint SecurityDescriptor, nint PSecurityDescriptorSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Create(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string FileName, uint CreateOptions, uint GrantedAccess, uint FileAttributes, nint SecurityDescriptor, ulong AllocationSize, ref FullContext FullContext, ref FSPOpenFileInfo OpenFileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Open(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string FileName, uint CreateOptions, uint GrantedAccess, ref FullContext FullContext, ref FSPOpenFileInfo OpenFileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Overwrite(nint FileSystem, ref FullContext FullContext, uint FileAttributes, [MarshalAs(UnmanagedType.U1)] bool ReplaceFileAttributes, ulong AllocationSize, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void Cleanup(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName, uint Flags);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void Close(nint FileSystem, ref FullContext FullContext);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Read(nint FileSystem, ref FullContext FullContext, nint Buffer, ulong Offset, uint Length, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Write(nint FileSystem, ref FullContext FullContext, nint Buffer, ulong Offset, uint Length, [MarshalAs(UnmanagedType.U1)] bool WriteToEndOfFile, [MarshalAs(UnmanagedType.U1)] bool ConstrainedIo, out uint PBytesTransferred, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Flush(nint FileSystem, ref FullContext FullContext, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetFileInfo(nint FileSystem, ref FullContext FullContext, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int SetBasicInfo(nint FileSystem, ref FullContext FullContext, uint FileAttributes, ulong CreationTime, ulong LastAccessTime, ulong LastWriteTime, ulong ChangeTime, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int SetFileSize(nint FileSystem, ref FullContext FullContext, ulong NewSize, [MarshalAs(UnmanagedType.U1)] bool SetAllocationSize, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int CanDelete(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Rename(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName, [MarshalAs(UnmanagedType.LPWStr)] string NewFileName, [MarshalAs(UnmanagedType.U1)] bool ReplaceIfExists);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetSecurity(nint FileSystem, ref FullContext FullContext, nint SecurityDescriptor, nint PSecurityDescriptorSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int SetSecurity(nint FileSystem, ref FullContext FullContext, uint SecurityInformation, nint ModificationDescriptor);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int ReadDirectory(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string Pattern, [MarshalAs(UnmanagedType.LPWStr)] string Marker, nint Buffer, uint Length, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int ResolveReparsePoints(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string FileName, uint ReparsePointIndex, [MarshalAs(UnmanagedType.U1)] bool ResolveLastPathComponent, out IoStatusBlock PIoStatus, nint Buffer, nint PSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetReparsePoint(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName, nint Buffer, nint PSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int SetReparsePoint(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName, nint Buffer, nuint Size);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int DeleteReparsePoint(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName, nint Buffer, nuint Size);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetStreamInfo(nint FileSystem, ref FullContext FullContext, nint Buffer, uint Length, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetDirInfoByName(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName, out DirInfo DirInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Control(nint FileSystem, ref FullContext FullContext, uint ControlCode, nint InputBuffer, uint InputBufferLength, nint OutputBuffer, uint OutputBufferLength, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int SetDelete(nint FileSystem, ref FullContext FullContext, [MarshalAs(UnmanagedType.LPWStr)] string FileName, [MarshalAs(UnmanagedType.U1)] bool DeleteFile);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int CreateEx(nint FileSystem, [MarshalAs(UnmanagedType.LPWStr)] string FileName, uint CreateOptions, uint GrantedAccess, uint FileAttributes, nint SecurityDescriptor, ulong AllocationSize, nint ExtraBuffer, uint ExtraLength, [MarshalAs(UnmanagedType.U1)] bool ExtraBufferIsReparsePoint, ref FullContext FullContext, ref FSPOpenFileInfo OpenFileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int OverwriteEx(nint FileSystem, ref FullContext FullContext, uint FileAttributes, [MarshalAs(UnmanagedType.U1)] bool ReplaceFileAttributes, ulong AllocationSize, nint Ea, uint EaLength, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int GetEa(nint FileSystem, ref FullContext FullContext, nint Ea, uint EaLength, out uint PBytesTransferred);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int SetEa(nint FileSystem, ref FullContext FullContext, nint Ea, uint EaLength, out FSPFileInfo FileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate int Obsolete0();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void DispatcherStopped(nint FileSystem, [MarshalAs(UnmanagedType.U1)] bool Normally);
	}

	internal static int Size;

	internal Proto.GetVolumeInfo GetVolumeInfo;

	internal Proto.SetVolumeLabel SetVolumeLabel;

	internal Proto.GetSecurityByName GetSecurityByName;

	internal Proto.Create Create;

	internal Proto.Open Open;

	internal Proto.Overwrite Overwrite;

	internal Proto.Cleanup Cleanup;

	internal Proto.Close Close;

	internal Proto.Read Read;

	internal Proto.Write Write;

	internal Proto.Flush Flush;

	internal Proto.GetFileInfo GetFileInfo;

	internal Proto.SetBasicInfo SetBasicInfo;

	internal Proto.SetFileSize SetFileSize;

	internal Proto.CanDelete CanDelete;

	internal Proto.Rename Rename;

	internal Proto.GetSecurity GetSecurity;

	internal Proto.SetSecurity SetSecurity;

	internal Proto.ReadDirectory ReadDirectory;

	internal Proto.ResolveReparsePoints ResolveReparsePoints;

	internal Proto.GetReparsePoint GetReparsePoint;

	internal Proto.SetReparsePoint SetReparsePoint;

	internal Proto.DeleteReparsePoint DeleteReparsePoint;

	internal Proto.GetStreamInfo GetStreamInfo;

	internal Proto.GetDirInfoByName GetDirInfoByName;

	internal Proto.Control Control;

	internal Proto.SetDelete SetDelete;

	internal Proto.CreateEx CreateEx;

	internal Proto.OverwriteEx OverwriteEx;

	internal Proto.GetEa GetEa;

	internal Proto.SetEa SetEa;

	internal Proto.Obsolete0 Obsolete0;

	internal Proto.DispatcherStopped DispatcherStopped;

	private static object CMOCOAKAPMHPEHECCOBNJEALEFIDPJDHJKEF;

	[MethodImpl(MethodImplOptions.NoInlining)]
	static FileSystemInterface()
	{
		GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJOLIPBMJHCEOLFMLAFNMMPLOBGECDDDOFLK();
		int num = 1;
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
						if (num2 != 10)
						{
							if (num2 == 991)
							{
								goto end_IL_0012;
							}
							goto case 0;
						}
						GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.DLAMFDPKJOBMPKPOENJIKMDJJANMNLIGPAPJ();
						num = 3;
						break;
					case 2:
						return;
					case 3:
						GCKJPIMLLCGALLONMOJGBOFOCIIOGJMOFLEI.JNMLINCDJFDFJOCKHAEFNDGOGEFAEBOBFIHD();
						num3 = 4;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_9dd60c24cfcd498d8fac9a69b0e6fea9 == 0)
						{
							num3 = 0;
						}
						continue;
					case 0:
						Size = IntPtr.Size * 64;
						num3 = 2;
						continue;
					case 1:
						GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJNKIHNGHFGCNDIMBMPIOEOHDHLMEJGICCDP();
						num = 6;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_07bfa6d7389f40ca9607fec5928fdd53 != 0)
						{
							num = 10;
						}
						break;
					}
					goto end_IL_000e;
					continue;
					end_IL_0012:
					break;
				}
				continue;
				end_IL_000e:
				break;
			}
		}
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
