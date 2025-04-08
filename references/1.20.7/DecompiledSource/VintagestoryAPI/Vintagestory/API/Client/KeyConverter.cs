namespace Vintagestory.API.Client;

/// <summary>
/// Converts key code from OpenTK 4 to GlKeys
/// </summary>
public static class KeyConverter
{
	public static readonly int[] NewKeysToGlKeys;

	public static readonly int[] GlKeysToNew;

	static KeyConverter()
	{
		NewKeysToGlKeys = new int[349];
		GlKeysToNew = new int[131];
		NewKeysToGlKeys[32] = 51;
		NewKeysToGlKeys[39] = 125;
		NewKeysToGlKeys[44] = 126;
		NewKeysToGlKeys[45] = 120;
		NewKeysToGlKeys[46] = 127;
		NewKeysToGlKeys[47] = 128;
		NewKeysToGlKeys[48] = 109;
		NewKeysToGlKeys[49] = 110;
		NewKeysToGlKeys[50] = 111;
		NewKeysToGlKeys[51] = 112;
		NewKeysToGlKeys[52] = 113;
		NewKeysToGlKeys[53] = 114;
		NewKeysToGlKeys[54] = 115;
		NewKeysToGlKeys[55] = 116;
		NewKeysToGlKeys[56] = 117;
		NewKeysToGlKeys[57] = 118;
		NewKeysToGlKeys[59] = 124;
		NewKeysToGlKeys[61] = 121;
		NewKeysToGlKeys[65] = 83;
		NewKeysToGlKeys[66] = 84;
		NewKeysToGlKeys[67] = 85;
		NewKeysToGlKeys[68] = 86;
		NewKeysToGlKeys[69] = 87;
		NewKeysToGlKeys[70] = 88;
		NewKeysToGlKeys[71] = 89;
		NewKeysToGlKeys[72] = 90;
		NewKeysToGlKeys[73] = 91;
		NewKeysToGlKeys[74] = 92;
		NewKeysToGlKeys[75] = 93;
		NewKeysToGlKeys[76] = 94;
		NewKeysToGlKeys[77] = 95;
		NewKeysToGlKeys[78] = 96;
		NewKeysToGlKeys[79] = 97;
		NewKeysToGlKeys[80] = 98;
		NewKeysToGlKeys[81] = 99;
		NewKeysToGlKeys[82] = 100;
		NewKeysToGlKeys[83] = 101;
		NewKeysToGlKeys[84] = 102;
		NewKeysToGlKeys[85] = 103;
		NewKeysToGlKeys[86] = 104;
		NewKeysToGlKeys[87] = 105;
		NewKeysToGlKeys[88] = 106;
		NewKeysToGlKeys[89] = 107;
		NewKeysToGlKeys[90] = 108;
		NewKeysToGlKeys[91] = 122;
		NewKeysToGlKeys[92] = 129;
		NewKeysToGlKeys[93] = 123;
		NewKeysToGlKeys[96] = 119;
		NewKeysToGlKeys[256] = 50;
		NewKeysToGlKeys[257] = 49;
		NewKeysToGlKeys[258] = 52;
		NewKeysToGlKeys[259] = 53;
		NewKeysToGlKeys[260] = 54;
		NewKeysToGlKeys[261] = 55;
		NewKeysToGlKeys[262] = 48;
		NewKeysToGlKeys[263] = 47;
		NewKeysToGlKeys[264] = 46;
		NewKeysToGlKeys[265] = 45;
		NewKeysToGlKeys[266] = 56;
		NewKeysToGlKeys[267] = 57;
		NewKeysToGlKeys[268] = 58;
		NewKeysToGlKeys[269] = 59;
		NewKeysToGlKeys[280] = 60;
		NewKeysToGlKeys[281] = 61;
		NewKeysToGlKeys[282] = 64;
		NewKeysToGlKeys[283] = 62;
		NewKeysToGlKeys[284] = 63;
		NewKeysToGlKeys[290] = 10;
		NewKeysToGlKeys[291] = 11;
		NewKeysToGlKeys[292] = 12;
		NewKeysToGlKeys[293] = 13;
		NewKeysToGlKeys[294] = 14;
		NewKeysToGlKeys[295] = 15;
		NewKeysToGlKeys[296] = 16;
		NewKeysToGlKeys[297] = 17;
		NewKeysToGlKeys[298] = 18;
		NewKeysToGlKeys[299] = 19;
		NewKeysToGlKeys[300] = 20;
		NewKeysToGlKeys[301] = 21;
		NewKeysToGlKeys[302] = 22;
		NewKeysToGlKeys[303] = 23;
		NewKeysToGlKeys[304] = 24;
		NewKeysToGlKeys[305] = 25;
		NewKeysToGlKeys[306] = 26;
		NewKeysToGlKeys[307] = 27;
		NewKeysToGlKeys[308] = 28;
		NewKeysToGlKeys[309] = 29;
		NewKeysToGlKeys[310] = 30;
		NewKeysToGlKeys[311] = 31;
		NewKeysToGlKeys[312] = 32;
		NewKeysToGlKeys[313] = 33;
		NewKeysToGlKeys[314] = 34;
		NewKeysToGlKeys[320] = 67;
		NewKeysToGlKeys[321] = 68;
		NewKeysToGlKeys[322] = 69;
		NewKeysToGlKeys[323] = 70;
		NewKeysToGlKeys[324] = 71;
		NewKeysToGlKeys[325] = 72;
		NewKeysToGlKeys[326] = 73;
		NewKeysToGlKeys[327] = 74;
		NewKeysToGlKeys[328] = 75;
		NewKeysToGlKeys[329] = 76;
		NewKeysToGlKeys[330] = 81;
		NewKeysToGlKeys[331] = 77;
		NewKeysToGlKeys[332] = 78;
		NewKeysToGlKeys[333] = 79;
		NewKeysToGlKeys[334] = 80;
		NewKeysToGlKeys[335] = 82;
		NewKeysToGlKeys[340] = 1;
		NewKeysToGlKeys[341] = 3;
		NewKeysToGlKeys[342] = 5;
		NewKeysToGlKeys[343] = 7;
		NewKeysToGlKeys[344] = 2;
		NewKeysToGlKeys[345] = 4;
		NewKeysToGlKeys[346] = 6;
		NewKeysToGlKeys[347] = 8;
		NewKeysToGlKeys[348] = 9;
		for (int j = 0; j < GlKeysToNew.Length; j++)
		{
			GlKeysToNew[j] = -1;
		}
		for (int i = 0; i < NewKeysToGlKeys.Length; i++)
		{
			if (NewKeysToGlKeys[i] != 0)
			{
				GlKeysToNew[NewKeysToGlKeys[i]] = i;
			}
		}
	}
}
