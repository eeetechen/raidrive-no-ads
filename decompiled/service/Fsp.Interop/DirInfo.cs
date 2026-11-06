using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ICJOPEKMCHLGNAOFOAPFCIHMKDAHIGMICIAE;
using LIKCEKLPJGFLBICDCNPHGANDLBGFGNJDEDPF;

namespace Fsp.Interop;

internal struct DirInfo
{
	internal const int FileNameBufSize = 255;

	internal static int FileNameBufOffset;

	internal ushort Size;

	internal FSPFileInfo FileInfo;

	internal unsafe fixed byte Padding[24];

	internal unsafe fixed ushort FileNameBuf[255];

	private static object CMOCOAKAPMHPEHECCOBNJEALEFIDPJDHJKEF;

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal void SetFileNameBuf(string Value)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static DirInfo()
	{
		GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJOLIPBMJHCEOLFMLAFNMMPLOBGECDDDOFLK();
		int num = 3;
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
						num3 = 2;
						continue;
					case 3:
						break;
					case 0:
						FileNameBufOffset = (int)Marshal.OffsetOf<DirInfo>(GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.LGFICOJCEDNCDBGHPGKOJPBMAGHFKCMNGIEM(0x5026FF8D ^ _003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_629d193b5de045ef86d8ca38add76205));
						num3 = 1;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_242f2247264346bb9579565286634d74 == 0)
						{
							num3 = 5;
						}
						continue;
					case 2:
						GCKJPIMLLCGALLONMOJGBOFOCIIOGJMOFLEI.JNMLINCDJFDFJOCKHAEFNDGOGEFAEBOBFIHD();
						num3 = 0;
						if (_003CModule_003E_007B25e48f6e_002Df4cb_002D4048_002D8b27_002Dc61c82e409b6_007D.m_6095ed59f511470f8858a81603566f5e.m_8e68b59f293540cabf634e96f80a39d8 != 0)
						{
							num3 = 8;
						}
						continue;
					case 1:
						return;
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
			GFBGAMFPLEDLAABJKOIPOFFMKLLKJJDPOJLM.PJNKIHNGHFGCNDIMBMPIOEOHDHLMEJGICCDP();
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
