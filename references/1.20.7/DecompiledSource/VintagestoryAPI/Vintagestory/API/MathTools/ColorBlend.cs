using System;

namespace Vintagestory.API.MathTools;

public static class ColorBlend
{
	public delegate int ColorBlendDelegate(int col1, int col2);

	private static ColorBlendDelegate[] Blenders;

	private static readonly uint[] masTable;

	static ColorBlend()
	{
		masTable = new uint[768]
		{
			0u, 0u, 0u, 1u, 0u, 0u, 1u, 0u, 1u, 2863311531u,
			0u, 33u, 1u, 0u, 2u, 3435973837u, 0u, 34u, 2863311531u, 0u,
			34u, 1227133513u, 1227133513u, 33u, 1u, 0u, 3u, 954437177u, 0u, 33u,
			3435973837u, 0u, 35u, 3123612579u, 0u, 35u, 2863311531u, 0u, 35u, 1321528399u,
			0u, 34u, 1227133513u, 1227133513u, 34u, 2290649225u, 0u, 35u, 1u, 0u,
			4u, 4042322161u, 0u, 36u, 954437177u, 0u, 34u, 3616814565u, 3616814565u, 36u,
			3435973837u, 0u, 36u, 3272356035u, 3272356035u, 36u, 3123612579u, 0u, 36u, 2987803337u,
			0u, 36u, 2863311531u, 0u, 36u, 1374389535u, 0u, 35u, 1321528399u, 0u,
			35u, 2545165805u, 2545165805u, 36u, 1227133513u, 1227133513u, 35u, 2369637129u, 0u, 36u,
			2290649225u, 0u, 36u, 1108378657u, 1108378657u, 35u, 1u, 0u, 5u, 1041204193u,
			0u, 35u, 4042322161u, 0u, 37u, 1963413621u, 1963413621u, 36u, 954437177u, 0u,
			35u, 1857283155u, 1857283155u, 36u, 3616814565u, 3616814565u, 37u, 1762037865u, 1762037865u, 36u,
			3435973837u, 0u, 37u, 3352169597u, 0u, 37u, 3272356035u, 3272356035u, 37u, 799063683u,
			0u, 35u, 3123612579u, 0u, 37u, 1527099483u, 1527099483u, 36u, 2987803337u, 0u,
			37u, 2924233053u, 0u, 37u, 2863311531u, 0u, 37u, 1402438301u, 0u, 36u,
			1374389535u, 0u, 36u, 2694881441u, 0u, 37u, 1321528399u, 0u, 36u, 2593187801u,
			2593187801u, 37u, 2545165805u, 2545165805u, 37u, 2498890063u, 2498890063u, 37u, 1227133513u, 1227133513u,
			36u, 1205604855u, 1205604855u, 36u, 2369637129u, 0u, 37u, 582368447u, 0u, 35u,
			2290649225u, 0u, 37u, 1126548799u, 0u, 36u, 1108378657u, 1108378657u, 36u, 1090785345u,
			1090785345u, 36u, 1u, 0u, 6u, 4228890877u, 0u, 38u, 1041204193u, 0u,
			36u, 128207979u, 0u, 33u, 4042322161u, 0u, 38u, 1991868891u, 0u, 37u,
			1963413621u, 1963413621u, 37u, 3871519817u, 0u, 38u, 954437177u, 0u, 36u, 941362695u,
			941362695u, 36u, 1857283155u, 1857283155u, 37u, 458129845u, 0u, 35u, 3616814565u, 3616814565u,
			38u, 892460737u, 0u, 36u, 1762037865u, 1762037865u, 37u, 3479467177u, 0u, 38u,
			3435973837u, 0u, 38u, 3393554407u, 0u, 38u, 3352169597u, 0u, 38u, 827945503u,
			0u, 36u, 3272356035u, 3272356035u, 38u, 3233857729u, 0u, 38u, 799063683u, 0u,
			36u, 789879043u, 0u, 36u, 3123612579u, 0u, 38u, 3088515809u, 0u, 38u,
			1527099483u, 1527099483u, 37u, 755159085u, 755159085u, 36u, 2987803337u, 0u, 38u, 2955676419u,
			0u, 38u, 2924233053u, 0u, 38u, 723362913u, 723362913u, 36u, 2863311531u, 0u,
			38u, 2833792855u, 2833792855u, 38u, 1402438301u, 0u, 37u, 2776544515u, 0u, 38u,
			1374389535u, 0u, 37u, 2721563435u, 2721563435u, 38u, 2694881441u, 0u, 38u, 2668717543u,
			2668717543u, 38u, 1321528399u, 0u, 37u, 654471207u, 654471207u, 36u, 2593187801u, 2593187801u,
			38u, 2568952401u, 2568952401u, 38u, 2545165805u, 2545165805u, 38u, 630453915u, 630453915u, 36u,
			2498890063u, 2498890063u, 38u, 619094385u, 619094385u, 36u, 1227133513u, 1227133513u, 37u, 2432547849u,
			2432547849u, 38u, 1205604855u, 1205604855u, 37u, 2390242669u, 2390242669u, 38u, 2369637129u, 0u,
			38u, 587345955u, 587345955u, 36u, 582368447u, 0u, 36u, 1154949189u, 0u, 37u,
			2290649225u, 0u, 38u, 2271718239u, 2271718239u, 38u, 1126548799u, 0u, 37u, 2234779731u,
			2234779731u, 38u, 1108378657u, 1108378657u, 37u, 274877907u, 0u, 35u, 1090785345u, 1090785345u,
			37u, 270549121u, 270549121u, 35u, 1u, 0u, 7u, 266354561u, 0u, 35u,
			4228890877u, 0u, 39u, 4196609267u, 0u, 39u, 1041204193u, 0u, 37u, 4133502361u,
			0u, 39u, 128207979u, 0u, 34u, 4072265289u, 0u, 39u, 4042322161u, 0u,
			39u, 125400505u, 0u, 34u, 1991868891u, 0u, 38u, 1977538899u, 0u, 38u,
			1963413621u, 1963413621u, 38u, 974744351u, 0u, 37u, 3871519817u, 0u, 39u, 3844446251u,
			0u, 39u, 954437177u, 0u, 37u, 3791419407u, 0u, 39u, 941362695u, 941362695u,
			37u, 3739835469u, 0u, 39u, 1857283155u, 1857283155u, 38u, 3689636335u, 0u, 39u,
			458129845u, 0u, 36u, 910191745u, 0u, 37u, 3616814565u, 3616814565u, 39u, 3593175255u,
			0u, 39u, 892460737u, 0u, 37u, 3546811703u, 0u, 39u, 1762037865u, 1762037865u,
			38u, 875407347u, 0u, 37u, 3479467177u, 0u, 39u, 3457583735u, 3457583735u, 39u,
			3435973837u, 0u, 39u, 3414632385u, 0u, 39u, 3393554407u, 0u, 39u, 3372735055u,
			0u, 39u, 3352169597u, 0u, 39u, 1665926709u, 0u, 38u, 827945503u, 0u,
			37u, 1645975491u, 0u, 38u, 3272356035u, 3272356035u, 39u, 1626496491u, 0u, 38u,
			3233857729u, 0u, 39u, 401868285u, 401868285u, 36u, 799063683u, 0u, 37u, 3177779271u,
			3177779271u, 39u, 789879043u, 0u, 37u, 1570730897u, 0u, 38u, 3123612579u, 0u,
			39u, 1552982525u, 1552982525u, 38u, 3088515809u, 0u, 39u, 1535630765u, 1535630765u, 38u,
			1527099483u, 1527099483u, 38u, 3037324939u, 0u, 39u, 755159085u, 755159085u, 37u, 3004130131u,
			0u, 39u, 2987803337u, 0u, 39u, 371456631u, 371456631u, 36u, 2955676419u, 0u,
			39u, 2939870663u, 0u, 39u, 2924233053u, 0u, 39u, 363595115u, 363595115u, 36u,
			723362913u, 723362913u, 37u, 2878302691u, 0u, 39u, 2863311531u, 0u, 39u, 356059465u,
			0u, 36u, 2833792855u, 2833792855u, 39u, 352407573u, 352407573u, 36u, 1402438301u, 0u,
			38u, 2790638649u, 2790638649u, 39u, 2776544515u, 0u, 39u, 1381296015u, 0u, 38u,
			1374389535u, 0u, 38u, 42735993u, 0u, 33u, 2721563435u, 2721563435u, 39u, 2708156719u,
			0u, 39u, 2694881441u, 0u, 39u, 1340867839u, 0u, 38u, 2668717543u, 2668717543u,
			39u, 663956297u, 0u, 37u, 1321528399u, 0u, 38u, 2630410593u, 0u, 39u,
			654471207u, 654471207u, 37u, 2605477791u, 0u, 39u, 2593187801u, 2593187801u, 39u, 2581013211u,
			0u, 39u, 2568952401u, 2568952401u, 39u, 1278501893u, 0u, 38u, 2545165805u, 2545165805u,
			39u, 1266718465u, 1266718465u, 38u, 630453915u, 630453915u, 37u, 313787565u, 313787565u, 36u,
			2498890063u, 2498890063u, 39u, 621895717u, 621895717u, 37u, 619094385u, 619094385u, 37u, 616318177u,
			616318177u, 37u, 1227133513u, 1227133513u, 38u, 2443359173u, 0u, 39u, 2432547849u, 2432547849u,
			39u, 2421831779u, 2421831779u, 39u, 1205604855u, 1205604855u, 38u, 1200340205u, 0u, 38u,
			2390242669u, 2390242669u, 39u, 1189947649u, 1189947649u, 38u, 2369637129u, 0u, 39u, 589866753u,
			589866753u, 37u, 587345955u, 587345955u, 37u, 1169693221u, 1169693221u, 38u, 582368447u, 0u,
			37u, 144977799u, 144977799u, 35u, 1154949189u, 0u, 38u, 2300233531u, 0u, 39u,
			2290649225u, 0u, 39u, 285143057u, 0u, 36u, 2271718239u, 2271718239u, 39u, 2262369605u,
			0u, 39u, 1126548799u, 0u, 38u, 2243901281u, 2243901281u, 39u, 2234779731u, 2234779731u,
			39u, 278216505u, 278216505u, 36u, 1108378657u, 1108378657u, 38u, 1103927337u, 1103927337u, 38u,
			274877907u, 0u, 36u, 2190262207u, 0u, 39u, 1090785345u, 1090785345u, 38u, 2172947881u,
			0u, 39u, 270549121u, 270549121u, 36u, 2155905153u, 0u, 39u
		};
		Blenders = new ColorBlendDelegate[9] { Normal, Darken, Lighten, Multiply, Screen, ColorDodge, ColorBurn, Overlay, OverlayCutout };
	}

	public static int Blend(EnumColorBlendMode blendMode, int colorBase, int colorOver)
	{
		return Blenders[(int)blendMode](colorBase, colorOver);
	}

	public static int Normal(int rgb1, int rgb2)
	{
		return ColorUtil.ColorOver(rgb2, rgb1);
	}

	public static int Overlay(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		VSColor rhs = new VSColor(rgb2);
		rhs.Rn *= rhs.An;
		rhs.Gn *= rhs.An;
		rhs.Bn *= rhs.An;
		int lhsA = lhs.A;
		int rhsA = rhs.A;
		int y = lhsA * (255 - rhsA) + 128;
		y = (y >> 8) + y >> 8;
		int totalA = y + rhsA;
		if (totalA == 0)
		{
			return 0;
		}
		int fB;
		if (lhs.B < 128)
		{
			fB = 2 * lhs.B * rhs.B + 128;
			fB = (fB >> 8) + fB >> 8;
		}
		else
		{
			fB = 2 * (255 - lhs.B) * (255 - rhs.B) + 128;
			fB = (fB >> 8) + fB >> 8;
			fB = 255 - fB;
		}
		int fG;
		if (lhs.G < 128)
		{
			fG = 2 * lhs.G * rhs.G + 128;
			fG = (fG >> 8) + fG >> 8;
		}
		else
		{
			fG = 2 * (255 - lhs.G) * (255 - rhs.G) + 128;
			fG = (fG >> 8) + fG >> 8;
			fG = 255 - fG;
		}
		int fR;
		if (lhs.R < 128)
		{
			fR = 2 * lhs.R * rhs.R + 128;
			fR = (fR >> 8) + fR >> 8;
		}
		else
		{
			fR = 2 * (255 - lhs.R) * (255 - rhs.R) + 128;
			fR = (fR >> 8) + fR >> 8;
			fR = 255 - fR;
		}
		int x = lhsA * rhsA + 128;
		x = (x >> 8) + x >> 8;
		int z = rhsA - x;
		int masIndex = totalA * 3;
		uint taM = masTable[masIndex];
		uint taA = masTable[masIndex + 1];
		uint taS = masTable[masIndex + 2];
		uint b = (uint)((lhs.B * y + rhs.B * z + fB * x) * taM + taA >> (int)taS);
		uint g = (uint)((lhs.G * y + rhs.G * z + fG * x) * taM + taA >> (int)taS);
		int num = (int)((lhs.R * y + rhs.R * z + fR * x) * taM + taA >> (int)taS);
		int a = lhsA * (255 - rhsA) + 128;
		a = (a >> 8) + a >> 8;
		a += rhsA;
		return num + (int)(g << 8) + (int)(b << 16) + (a << 24);
	}

	public static int Darken(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		VSColor rhs = new VSColor(rgb2);
		int lhsA = lhs.A;
		int rhsA = rhs.A;
		int y = lhsA * (255 - rhsA) + 128;
		y = (y >> 8) + y >> 8;
		int totalA = y + rhsA;
		if (totalA == 0)
		{
			return 0;
		}
		int fB = Math.Min(lhs.B, rhs.B);
		int fG = Math.Min(lhs.G, rhs.G);
		int fR = Math.Min(lhs.R, rhs.R);
		int x = lhsA * rhsA + 128;
		x = (x >> 8) + x >> 8;
		int z = rhsA - x;
		int masIndex = totalA * 3;
		uint taM = masTable[masIndex];
		uint taA = masTable[masIndex + 1];
		uint taS = masTable[masIndex + 2];
		uint b = (uint)((lhs.B * y + rhs.B * z + fB * x) * taM + taA >> (int)taS);
		uint g = (uint)((lhs.G * y + rhs.G * z + fG * x) * taM + taA >> (int)taS);
		int num = (int)((lhs.R * y + rhs.R * z + fR * x) * taM + taA >> (int)taS);
		int a = lhsA * (255 - rhsA) + 128;
		a = (a >> 8) + a >> 8;
		a += rhsA;
		return num + (int)(g << 8) + (int)(b << 16) + (a << 24);
	}

	public static int Lighten(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		VSColor rhs = new VSColor(rgb2);
		int lhsA = lhs.A;
		int rhsA = rhs.A;
		int y = lhsA * (255 - rhsA) + 128;
		y = (y >> 8) + y >> 8;
		int totalA = y + rhsA;
		if (totalA == 0)
		{
			return 0;
		}
		int fB = Math.Max(lhs.B, rhs.B);
		int fG = Math.Max(lhs.G, rhs.G);
		int fR = Math.Max(lhs.R, rhs.R);
		int x = lhsA * rhsA + 128;
		x = (x >> 8) + x >> 8;
		int z = rhsA - x;
		int masIndex = totalA * 3;
		uint taM = masTable[masIndex];
		uint taA = masTable[masIndex + 1];
		uint taS = masTable[masIndex + 2];
		uint b = (uint)((lhs.B * y + rhs.B * z + fB * x) * taM + taA >> (int)taS);
		uint g = (uint)((lhs.G * y + rhs.G * z + fG * x) * taM + taA >> (int)taS);
		int num = (int)((lhs.R * y + rhs.R * z + fR * x) * taM + taA >> (int)taS);
		int a = lhsA * (255 - rhsA) + 128;
		a = (a >> 8) + a >> 8;
		a += rhsA;
		return num + (int)(g << 8) + (int)(b << 16) + (a << 24);
	}

	public static int Multiply(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		VSColor rhs = new VSColor(rgb2);
		int lhsA = lhs.A;
		int rhsA = rhs.A;
		int y = lhsA * (255 - rhsA) + 128;
		y = (y >> 8) + y >> 8;
		int totalA = y + rhsA;
		if (totalA == 0)
		{
			return 0;
		}
		int fB = lhs.B * rhs.B + 128;
		fB = (fB >> 8) + fB >> 8;
		int fG = lhs.G * rhs.G + 128;
		fG = (fG >> 8) + fG >> 8;
		int fR = lhs.R * rhs.R + 128;
		fR = (fR >> 8) + fR >> 8;
		int x = lhsA * rhsA + 128;
		x = (x >> 8) + x >> 8;
		int z = rhsA - x;
		int masIndex = totalA * 3;
		uint taM = masTable[masIndex];
		uint taA = masTable[masIndex + 1];
		uint taS = masTable[masIndex + 2];
		uint b = (uint)((lhs.B * y + rhs.B * z + fB * x) * taM + taA >> (int)taS);
		uint g = (uint)((lhs.G * y + rhs.G * z + fG * x) * taM + taA >> (int)taS);
		int num = (int)((lhs.R * y + rhs.R * z + fR * x) * taM + taA >> (int)taS);
		int a = lhsA * (255 - rhsA) + 128;
		a = (a >> 8) + a >> 8;
		a += rhsA;
		return num + (int)(g << 8) + (int)(b << 16) + (a << 24);
	}

	public static int Screen(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		VSColor rhs = new VSColor(rgb2);
		int lhsA = lhs.A;
		int rhsA = rhs.A;
		int y = lhsA * (255 - rhsA) + 128;
		y = (y >> 8) + y >> 8;
		int totalA = y + rhsA;
		if (totalA == 0)
		{
			return 0;
		}
		int fB = rhs.B * lhs.B + 128;
		fB = (fB >> 8) + fB >> 8;
		fB = rhs.B + lhs.B - fB;
		int fG = rhs.G * lhs.G + 128;
		fG = (fG >> 8) + fG >> 8;
		fG = rhs.G + lhs.G - fG;
		int fR = rhs.R * lhs.R + 128;
		fR = (fR >> 8) + fR >> 8;
		fR = rhs.R + lhs.R - fR;
		int x = lhsA * rhsA + 128;
		x = (x >> 8) + x >> 8;
		int z = rhsA - x;
		int masIndex = totalA * 3;
		uint taM = masTable[masIndex];
		uint taA = masTable[masIndex + 1];
		uint taS = masTable[masIndex + 2];
		uint b = (uint)((lhs.B * y + rhs.B * z + fB * x) * taM + taA >> (int)taS);
		uint g = (uint)((lhs.G * y + rhs.G * z + fG * x) * taM + taA >> (int)taS);
		int num = (int)((lhs.R * y + rhs.R * z + fR * x) * taM + taA >> (int)taS);
		int a = lhsA * (255 - rhsA) + 128;
		a = (a >> 8) + a >> 8;
		a += rhsA;
		return num + (int)(g << 8) + (int)(b << 16) + (a << 24);
	}

	public static int ColorDodge(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		VSColor rhs = new VSColor(rgb2);
		int lhsA = lhs.A;
		int rhsA = rhs.A;
		int y = lhsA * (255 - rhsA) + 128;
		y = (y >> 8) + y >> 8;
		int totalA = y + rhsA;
		if (totalA == 0)
		{
			return 0;
		}
		int fB;
		if (rhs.B == byte.MaxValue)
		{
			fB = 255;
		}
		else
		{
			int k = (255 - rhs.B) * 3;
			uint M3 = masTable[k];
			uint A3 = masTable[k + 1];
			uint S3 = masTable[k + 2];
			fB = (int)(lhs.B * 255 * M3 + A3 >> (int)S3);
			fB = Math.Min(255, fB);
		}
		int fG;
		if (rhs.G == byte.MaxValue)
		{
			fG = 255;
		}
		else
		{
			int j = (255 - rhs.G) * 3;
			uint M2 = masTable[j];
			uint A2 = masTable[j + 1];
			uint S2 = masTable[j + 2];
			fG = (int)(lhs.G * 255 * M2 + A2 >> (int)S2);
			fG = Math.Min(255, fG);
		}
		int fR;
		if (rhs.R == byte.MaxValue)
		{
			fR = 255;
		}
		else
		{
			int i = (255 - rhs.R) * 3;
			uint M = masTable[i];
			uint A = masTable[i + 1];
			uint S = masTable[i + 2];
			fR = (int)(lhs.R * 255 * M + A >> (int)S);
			fR = Math.Min(255, fR);
		}
		int x = lhsA * rhsA + 128;
		x = (x >> 8) + x >> 8;
		int z = rhsA - x;
		int masIndex = totalA * 3;
		uint taM = masTable[masIndex];
		uint taA = masTable[masIndex + 1];
		uint taS = masTable[masIndex + 2];
		uint b = (uint)((lhs.B * y + rhs.B * z + fB * x) * taM + taA >> (int)taS);
		uint g = (uint)((lhs.G * y + rhs.G * z + fG * x) * taM + taA >> (int)taS);
		int num = (int)((lhs.R * y + rhs.R * z + fR * x) * taM + taA >> (int)taS);
		int a = lhsA * (255 - rhsA) + 128;
		a = (a >> 8) + a >> 8;
		a += rhsA;
		return num + (int)(g << 8) + (int)(b << 16) + (a << 24);
	}

	public static int ColorBurn(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		VSColor rhs = new VSColor(rgb2);
		int lhsA = lhs.A;
		int rhsA = rhs.A;
		int y = lhsA * (255 - rhsA) + 128;
		y = (y >> 8) + y >> 8;
		int totalA = y + rhsA;
		if (totalA == 0)
		{
			return 0;
		}
		int fB;
		if (rhs.B == 0)
		{
			fB = 0;
		}
		else
		{
			int k = rhs.B * 3;
			uint M3 = masTable[k];
			uint A3 = masTable[k + 1];
			uint S3 = masTable[k + 2];
			fB = (int)((255 - lhs.B) * 255 * M3 + A3 >> (int)S3);
			fB = 255 - fB;
			fB = Math.Max(0, fB);
		}
		int fG;
		if (rhs.G == 0)
		{
			fG = 0;
		}
		else
		{
			int j = rhs.G * 3;
			uint M2 = masTable[j];
			uint A2 = masTable[j + 1];
			uint S2 = masTable[j + 2];
			fG = (int)((255 - lhs.G) * 255 * M2 + A2 >> (int)S2);
			fG = 255 - fG;
			fG = Math.Max(0, fG);
		}
		int fR;
		if (rhs.R == 0)
		{
			fR = 0;
		}
		else
		{
			int i = rhs.R * 3;
			uint M = masTable[i];
			uint A = masTable[i + 1];
			uint S = masTable[i + 2];
			fR = (int)((255 - lhs.R) * 255 * M + A >> (int)S);
			fR = 255 - fR;
			fR = Math.Max(0, fR);
		}
		int x = lhsA * rhsA + 128;
		x = (x >> 8) + x >> 8;
		int z = rhsA - x;
		int masIndex = totalA * 3;
		uint taM = masTable[masIndex];
		uint taA = masTable[masIndex + 1];
		uint taS = masTable[masIndex + 2];
		uint b = (uint)((lhs.B * y + rhs.B * z + fB * x) * taM + taA >> (int)taS);
		uint g = (uint)((lhs.G * y + rhs.G * z + fG * x) * taM + taA >> (int)taS);
		int num = (int)((lhs.R * y + rhs.R * z + fR * x) * taM + taA >> (int)taS);
		int a = lhsA * (255 - rhsA) + 128;
		a = (a >> 8) + a >> 8;
		a += rhsA;
		return num + (int)(g << 8) + (int)(b << 16) + (a << 24);
	}

	public static int OverlayCutout(int rgb1, int rgb2)
	{
		VSColor lhs = new VSColor(rgb1);
		if (new VSColor(rgb2).A != 0)
		{
			lhs.A = 0;
		}
		return lhs.AsInt;
	}
}
