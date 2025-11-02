using HarmonyLib;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Poses;
using MelonLoader;
using RumbleModdingAPI;
using RumbleModUI;
using System.Security.Cryptography;
using UnityEngine;

namespace PokerTable
{
    public static class BuildInfo
    {
        public const string ModName = "PokerTable";
        public const string ModVersion = "1.0.2";
        public const string Author = "UlvakSkillz";
    }

    [HarmonyPatch(typeof(PlayerPoseSystem), "OnPoseSetCompleted", new Type[] { typeof(PoseSet) })]
    public static class PosePatch
    {
        private static void Postfix(PoseSet set)
        {
            Main.posesCompleted++;
            if (Main.posesCompleted >= 100)
            {
                Main.posesCompleted -= 100;
                Main.Payout(1, 1f);
                Main.Log("Player Earned 1 Coin from Completing 100 Poses", true);
            }
        }
    }

    public class Main : MelonMod
    {
        public static Main instance;
        private static MelonLogger.Instance logger;
        private static int playerCoins = -1;
        public static int posesCompleted = -1;

        private string currentScene = "Loader";
        private Mod PokerTable = new Mod();
        private ModSetting<bool> enabled;
        public static ModSetting<int> deckCount;
        public static ModSetting<int> seed;
        public static ModSetting<bool> useSeed;
        public static ModSetting<bool> debugging;
        private GameObject storedTable;
        public static GameObject loadedTable;
        private static bool flatLandFound = false, voidLandFound = false;

        public static void Log(string msg, bool sendMsg = true)
        {
            if (sendMsg) { logger.Msg(msg); }
        }

        public static void Warn(string msg, bool sendMsg = true)
        {
            if (sendMsg) { logger.Warning(msg); }
        }

        public static void Error(string msg) { logger.Error(msg); }

        public override void OnLateInitializeMelon()
        {
            instance = this;
            ModUIInit();
            logger = LoggerInstance;
            Log("OnLateInitializeMelon Started", (bool)debugging.SavedValue);
            Calls.onMapInitialized += MapInit;
            LoadSaveFile();
            LoadAssetBundle();
            if (Calls.Mods.findOwnMod("FlatLand", "1.0.0", false))
            {
                Log("FlatLand Found, Adding to PokerTable to Dont Disable List", (bool)debugging.SavedValue);
                flatLandFound = true;
                FlatLand.main.dontDisableGameObject.Add("PokerTable");
            }
            if (Calls.Mods.findOwnMod("VoidLand", "1.0.0", false))
            {
                Log("VoidLand Found, Adding to PokerTable to Dont Disable List", (bool)debugging.SavedValue);
                voidLandFound = true;
                VoidLand.main.dontDisableGameObject.Add("PokerTable");
            }
            Log("OnLateInitializeMelon Completed", (bool)debugging.SavedValue);
        }

        private void LoadSaveFile()
        {
            Log("LoadSaveFile Started", (bool)debugging.SavedValue);
            if (!Directory.Exists(@"UserData\" + BuildInfo.ModName))
            {
                Directory.CreateDirectory(@"UserData\" + BuildInfo.ModName);
            }
            if (!File.Exists(@"UserData\" + BuildInfo.ModName + @"\Save.save"))
            {
                EncryptStringToFile("0|0", @"UserData\" + BuildInfo.ModName + @"\Save.save", "UlvakSkillz");
            }
            try
            {
                string decodedText = DecryptStringFromFile(@"UserData\" + BuildInfo.ModName + @"\Save.save", "UlvakSkillz");
                Log("Save.save File Text: " + decodedText, (bool)debugging.SavedValue);
                string[] fileText = decodedText.Split("|");
                playerCoins = int.Parse(fileText[0]);
                posesCompleted = int.Parse(fileText[1]);
            }
            catch (Exception e)
            {
                Error("Error Loading Save File:");
                Error(e.Message);
                if (playerCoins == -1) { Log("Failed to Load Player Coins, Setting to 0", true); playerCoins = 0; }
                if (posesCompleted == -1) { Log("Failed to Load Poses Completed Count, Setting to 0", true); posesCompleted = 0; }
            }
            Log("LoadSaveFile Completed", (bool)debugging.SavedValue);
        }

        private void LoadAssetBundle()
        {
            Log("LoadAssetBundle Started", (bool)debugging.SavedValue);
            storedTable = GameObject.Instantiate(Calls.LoadAssetFromStream<GameObject>(this, "PokerTable.blackjacktable", "BlackjackTable"));
            storedTable.name = "PokerTable";
            storedTable.SetActive(false);
            GameObject.DontDestroyOnLoad(storedTable);
            Log("LoadAssetBundle Completed", (bool)debugging.SavedValue);
        }

        public void ModUIInit()
        {
            PokerTable.ModName = BuildInfo.ModName;
            PokerTable.ModVersion = BuildInfo.ModVersion;
            PokerTable.SetFolder(BuildInfo.ModName);
            enabled = PokerTable.AddToList("Enabled", true, 0, "Enables THE Table.", new Tags());
            deckCount = PokerTable.AddToList("Deck Count", 1, "Sets How Many Complete Decks should be in the Dealer Deck.", new Tags());
            seed = PokerTable.AddToList("Seed", -1, "Sets the Seed in the Randomizer if 'Use Seed' is Toggled On.", new Tags { DoNotSave = true });
            useSeed = PokerTable.AddToList("Use Seed", false, 0, "If Enabled, Sets the Table to FreePlay Mode and Uses the Seed.", new Tags { DoNotSave = true });
            debugging = PokerTable.AddToList("Debugging", false, 0, "Enables Debugging Logs.", new Tags());
            PokerTable.GetFromFile();
            lastUseSeed = (bool)useSeed.SavedValue;
            UI.instance.UI_Initialized += UIInit;
            PokerTable.ModSaved += Save;
        }

        private bool lastUseSeed = false;
        private int lastdeckCount = 1;
        private void Save()
        {
            if (lastdeckCount != (int)deckCount.SavedValue)
            {
                int decks = Math.Max((int)deckCount.SavedValue, 0);
                deckCount.SavedValue = decks;
                deckCount.Value = decks;
            }
            int clampedValue = Math.Clamp((int)seed.SavedValue, 0, 999999999);
            if (clampedValue != (int)seed.SavedValue)
            {
                seed.SavedValue = clampedValue;
                seed.Value = clampedValue;
            }
            if (lastUseSeed != (bool)useSeed.SavedValue)
            {
                lastUseSeed = (bool)useSeed.SavedValue;
                if (lastUseSeed)
                {
                    if (Table.freePlayButton != null) { GameObject.Destroy(Table.freePlayButton); }
                    Table.freePlay =  true;
                }
                Table.SetupRandom();
            }
        }

        private void UIInit()
        {
            Log("UIInit Started", (bool)debugging.SavedValue);
            UI.instance.AddMod(PokerTable);
            Log("UIInit Completed", (bool)debugging.SavedValue);
        }

        public void MapInit()
        {
            Log("MapInit Started", (bool)debugging.SavedValue);
            if (logger == null)
            {
                logger = LoggerInstance;
            }
            currentScene = Calls.Scene.GetSceneName();
            if ((bool)enabled.SavedValue)
            {
                LoadTable();
            }
            Log("MapInit Completed", (bool)debugging.SavedValue);
        }

        private void LoadTable()
        {
            Log("LoadTable Started", (bool)debugging.SavedValue);
            if (currentScene != "Gym") { return; }
            GameObject table = GameObject.Instantiate(storedTable);
            table.name = "PokerTable";
            switch (currentScene)
            {
                case "Gym":
                    table.transform.position = new Vector3(4.1736f, -3.5255f, -10.3336f);
                    table.transform.rotation = Quaternion.Euler(0, 170.9995f, 0);
                    if (flatLandFound)
                    {
                        GameObject.Find("/FlatLand/FlatLandButton/").transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new System.Action(() =>
                        {
                            loadedTable.transform.position = new Vector3(loadedTable.transform.position.x, 0, loadedTable.transform.position.z);
                        }));
                    }
                    if (voidLandFound)
                    {
                        GameObject.Find("/VoidLand/VoidLandButton/").transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new System.Action(() =>
                        {
                            loadedTable.transform.position = new Vector3(loadedTable.transform.position.x, 0, loadedTable.transform.position.z);
                        }));
                    }
                    break;
                case "Park":
                    table.transform.position = new Vector3(12.83f, -2.6873f, -1.1611f);
                    table.transform.rotation = Quaternion.Euler(0, 186.289f, 0);
                    break;
                default:
                    return;
            }
            table.SetActive(true);
            loadedTable = table;
            table.AddComponent<Table>();
            Log("LoadTable Completed", (bool)debugging.SavedValue);
        }

        public static int GetPlayerCoinCount() { return playerCoins; }

        public static int Payout(int bet, float multiplyer)
        {
            float betF = bet;
            if (!Table.freePlay)
            {
                playerCoins += (int)(betF * multiplyer);
            }
            EncryptStringToFile(playerCoins.ToString() + "|" + posesCompleted, @"UserData\" + BuildInfo.ModName + @"\Save.save", "UlvakSkillz");
            return (int)(betF * multiplyer);
        }

        public static void EncryptStringToFile(string plainText, string filePath, string password)
        {
            // Generate a random salt
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using (var aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;
                aesAlg.Padding = PaddingMode.PKCS7;
                // Derive key and IV from password + salt
                using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100000))
                {
                    aesAlg.Key = deriveBytes.GetBytes(32);
                    aesAlg.IV = deriveBytes.GetBytes(16);
                }
                using (FileStream fsOutput = new FileStream(filePath, FileMode.Create))
                {
                    // Write salt and IV to the file header
                    fsOutput.Write(salt, 0, salt.Length);
                    fsOutput.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (ICryptoTransform encryptor = aesAlg.CreateEncryptor())
                    using (CryptoStream csEncrypt = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                }
            }
        }

        public static string DecryptStringFromFile(string filePath, string password)
        {
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            using (FileStream fsInput = new FileStream(filePath, FileMode.Open))
            {
                // Read salt and IV from file
                fsInput.Read(salt, 0, salt.Length);
                fsInput.Read(iv, 0, iv.Length);
                using (var aesAlg = Aes.Create())
                {
                    aesAlg.KeySize = 256;
                    aesAlg.BlockSize = 128;
                    aesAlg.Padding = PaddingMode.PKCS7;
                    // Derive key from password + stored salt
                    using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100000))
                    {
                        aesAlg.Key = deriveBytes.GetBytes(32);
                    }
                    aesAlg.IV = iv;
                    using (ICryptoTransform decryptor = aesAlg.CreateDecryptor())
                    using (CryptoStream csDecrypt = new CryptoStream(fsInput, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        string plainText = srDecrypt.ReadToEnd();
                        return plainText;
                    }
                }
            }
        }
    }
}
