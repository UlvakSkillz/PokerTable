using MelonLoader;
using BuildInfo = PokerTable.BuildInfo;

[assembly: MelonInfo(typeof(PokerTable.Main), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonColor(255, 195, 0, 255)]
[assembly: MelonAuthorColor(255, 195, 0, 255)]
[assembly: VerifyLoaderVersion(0, 6, 6, true)]

