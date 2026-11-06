using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ICJOPEKMCHLGNAOFOAPFCIHMKDAHIGMICIAE;
using LIKCEKLPJGFLBICDCNPHGANDLBGFGNJDEDPF;

namespace Fsp.Interop;

internal struct FSPFileInfo
{
	public uint FileAttributes;

	public uint ReparseTag;

	public ulong AllocationSize;

	public ulong FileSize;

	public ulong CreationTime;

	public ulong LastAccessTime;

	public ulong LastWriteTime;

	public ulong ChangeTime;

	public ulong IndexNumber;

	public uint HardLinks;

	internal static int EaSizeOffset;

	internal static object CMOCOAKAPMHPEHECCOBNJEALEFIDPJDHJKEF;

	public uint EaSize
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		get
		{
			return 0u;
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		set
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal uint GetEaSize()
	{
		return 0u;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal void SetEaSize(uint value)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static FSPFileInfo()
	{
		GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJOLIPBMJHCEOLFMLAFNMMPLOBGECDDDOFLK();
		int num = 2;
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
					case 3:
						return;
					case 0:
						goto end_IL_000e;
					case 2:
						GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJNKIHNGHFGCNDIMBMPIOEOHDHLMEJGICCDP();
						num3 = 4;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_94a880ee802c4e7b9cc062fe6a9bccdc == 0)
						{
							num3 = 1;
						}
						continue;
					case 1:
						GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.DLAMFDPKJOBMPKPOENJIKMDJJANMNLIGPAPJ();
						num3 = 0;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_8fedf413cef7473da85b8f49a8a532e5 != 0)
						{
							num3 = 1;
						}
						continue;
					}
					switch (num2)
					{
					case 991:
						break;
					default:
						return;
					case 10:
						EaSizeOffset = (int)Marshal.OffsetOf<FSPFileInfo>(GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.LGFICOJCEDNCDBGHPGKOJPBMAGHFKCMNGIEM(-1957886778 ^ -1892245555 ^ _003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_d417d8ff94d54435b6855cb9c5f8fd17)) + 4;
						num3 = 3;
						continue;
					}
					break;
				}
				continue;
				end_IL_000e:
				break;
			}
			GCKJPIMLLCGALLONMOJGBOFOCIIOGJMOFLEI.JNMLINCDJFDFJOCKHAEFNDGOGEFAEBOBFIHD();
			num = 10;
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
