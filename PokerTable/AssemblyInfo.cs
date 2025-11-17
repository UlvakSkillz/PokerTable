using MelonLoader;
using BuildInfo = GamblingMod.BuildInfo;

[assembly: MelonInfo(typeof(GamblingMod.Main), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonColor(255, 195, 0, 255)]
[assembly: MelonAuthorColor(255, 195, 0, 255)]
[assembly: VerifyLoaderVersion(0, 6, 6, true)]

