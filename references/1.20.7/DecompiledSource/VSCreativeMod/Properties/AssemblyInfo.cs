using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Vintagestory.API.Common;

[assembly: AssemblyTitle("Vintagestory World Edit Mod")]
[assembly: AssemblyDescription("www.vintagestory.at")]
[assembly: AssemblyCompany("Tyron Madlener (Anego Studios)")]
[assembly: AssemblyProduct("Vintage Story")]
[assembly: AssemblyCopyright("Copyright © 2016-2024 Anego Studios")]
[assembly: AssemblyTrademark("")]
[assembly: ComVisible(false)]
[assembly: Guid("203dfbf1-3599-43fd-8487-e1c79c2b788f")]
[assembly: AssemblyFileVersion("1.20.7")]
[assembly: ModInfo("Creative Mode", "creative", Version = "1.20.7", NetworkVersion = "1.20.8", CoreMod = true, Description = "World editing, worldedit GUI and super flat world generation", IconPath = "game/textures/gui/modicon.png", Authors = new string[] { "Tyron" }, WorldConfig = "\r\n    {\r\n\t    playstyles: [\r\n\t\t    {\r\n\t\t\t    code: \"creativebuilding\",\r\n\t\t\t\tplayListCode: \"creative\",\r\n                langcode: \"creativebuilding\",\r\n                listOrder: 10,\r\n\t\t\t    mods: [\"game\", \"creative\"],\r\n\t\t\t    worldType: \"superflat\",\r\n\t\t\t    worldConfig: {\r\n\t\t\t\t    worldClimate: \"superflat\",\r\n\t\t\t\t    gameMode: \"creative\",\r\n\t\t\t\t    hoursPerDay: \"2400\",\r\n\t\t\t\t\tcloudypos: \"0.5\",\r\n\t\t\t\t\ttemporalStability: \"false\",\r\n                    temporalStorms: \"off\",\r\n\t\t\t\t\tsnowAccum: \"false\",\r\n\t\t\t\t\tcolorAccurateWorldmap: \"true\",\r\n\t\t\t\t\ttemporalRifts: \"off\",\r\n                    loreContent: \"false\"\r\n\t\t\t    }\r\n\t\t    }\r\n\t    ],\r\n\t    worldConfigAttributes: [\r\n\r\n\t    ]\r\n    }\r\n")]
[assembly: ModDependency("game", "")]
[assembly: AssemblyVersion("1.0.0.0")]
[module: System.Runtime.CompilerServices.RefSafetyRules(11)]
