using Il2CppTMPro;
using MelonLoader;
using RumbleModdingAPI;
using System.Collections;
using UnityEngine;
using Random = System.Random;

namespace PokerTable
{
    public enum Games : int
    {
        BlackJack,
        JacksOrBetter
    }

    [RegisterTypeInIl2Cpp]
    public class Table : MonoBehaviour
    {
        public static string[] CardString = {
        "AS", "2S", "3S", "4S", "5S", "6S", "7S", "8S", "9S", "10S", "JS", "QS", "KS",
        "AH", "2H", "3H", "4H", "5H", "6H", "7H", "8H", "9H", "10H", "JH", "QH", "KH",
        "AD", "2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "JD", "QD", "KD",
        "AC", "2C", "3C", "4C", "5C", "6C", "7C", "8C", "9C", "10C", "JC", "QC", "KC" };

        public static Table instance = null;
        public static float TABLEHEIGHT;
        public static Random random;
        public static int seed;
        public static bool freePlay = false;
        public static GameObject dealerDeck, storedDeckOfCards;

        public static BlackJack blackJackInstance;
        public static JacksOrBetter jacksOrBetterInstance;

        public static void Log(string msg, bool sendMsg = true)
        {
            if (sendMsg)
            {
                Main.Log($"Table - {msg}", sendMsg);
            }
        }

        public static void Warn(string msg, bool sendMsg = true)
        {
            if (sendMsg)
            {
                Main.Warn($"Table - {msg}", sendMsg);
            }
        }

        public static void Error(string msg)
        {
            Main.Error($"Table - {msg}");
        }

        void Start()
        {
            Log("Start Started", (bool)Main.debugging.SavedValue);
            if (instance != null) { GameObject.Destroy(instance.gameObject); }
            instance = this;
            TABLEHEIGHT = this.transform.GetChild(0).GetChild(15).localPosition.y;
            dealerDeck = this.transform.GetChild(0).GetChild(16).gameObject;
            storedDeckOfCards = this.transform.GetChild(1).GetChild(0).gameObject;
            SetupRandom();
            SetupStart();
            Log("Start Completed", (bool)Main.debugging.SavedValue);
        }

        void OnDestroy()
        {
            Log("OnDestroy Started", (bool)Main.debugging.SavedValue);
            Component.Destroy(blackJackInstance);
            Component.Destroy(jacksOrBetterInstance);
            Log("OnDestroy Completed", (bool)Main.debugging.SavedValue);
        }

        public static void SetupRandom()
        {
            Log("SetupRandom Started", (bool)Main.debugging.SavedValue);
            Log("0", true);
            string seedString = "0123456789";
            if (!(bool)Main.useSeed.SavedValue)
            {
                Log("1", true);
                Random randomSeed = new Random();
                int randomInt = randomSeed.Next(1, 10);
                string seedCrafted = seedString[randomInt].ToString();
                Log("2", true);
                for (int i = 1; i <= 8; i++)
                {
                    seedCrafted += seedString[randomSeed.Next(0, 10)];
                }
                Log("3", true);
                seed = int.Parse(seedCrafted);
                Log("4", true);
            }
            else
            {
                Log("5", true); seed = Math.Min((int)Main.seed.SavedValue, 0);
            }
            Log("6", true);
            Main.seed.Value = seed;
            Main.seed.SavedValue = seed;
            random = new Random(seed);
            Log("7", true);
            Log("SetupRandom Complete", (bool)Main.debugging.SavedValue);
        }

        public static GameObject freePlayButton;
        private void SetupStart()
        {
            Log("SetupStart Started", (bool)Main.debugging.SavedValue);
            if (!(bool)Main.useSeed.SavedValue)
            {
                freePlayButton = LoadMenuButton("FreePlay",
                    /*position*/ new Vector3(0f, TABLEHEIGHT - 0.045f, 0.6f),
                    /*rotation*/Quaternion.Euler(0, 0, 0),
                    /*scale*/ new Vector3(0.5f, 0.5f, 0.5f),
                    () => {
                        freePlay = !freePlay;
                        this.gameObject.transform.GetChild(2).FindChild("FreePlay Button/FreePlayText").GetComponent<TextMeshPro>().color = freePlay ? Color.green : Color.red;
                    });
                freePlayButton.transform.GetChild(2).GetComponent<TextMeshPro>().color = freePlay ? Color.green : Color.red;
            }
            LoadMenuButton("BlackJack",
                /*position*/ new Vector3(0.55f, TABLEHEIGHT - 0.05f, 0.6f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(1, 1, 1),
                () => { LoadGame(Games.BlackJack); });
            LoadMenuButton("Jacks Or Better",
                /*position*/ new Vector3(-0.55f, TABLEHEIGHT - 0.05f, 0.6f), 
                /*rotation*/Quaternion.Euler(0, 0, 0), 
                /*scale*/ new Vector3(1, 1, 1), 
                () => { LoadGame(Games.JacksOrBetter); });
            Log("SetupStart Complete", (bool)Main.debugging.SavedValue);
        }

        private void LoadGame(Games game)
        {
            Log("Loading Game: " + game, (bool)Main.debugging.SavedValue);
            ClearActiveObjects();
            switch (game)
            {
                case Games.BlackJack:
                    BlackJack.ShowSplash();
                    break;
                case Games.JacksOrBetter:
                    JacksOrBetter.ShowSplash();
                    break;
                default:
                    return;
            }
            MelonCoroutines.Start(LoadGameCoroutine(game));
        }

        private IEnumerator LoadGameCoroutine(Games game)
        {
            yield return new WaitForSeconds(0.5f);
            object gameRunningCoroutine = null;
            switch (game)
            {
                case Games.BlackJack:
                    Log("Game BlackJack Started!", (bool)Main.debugging.SavedValue);
                    blackJackInstance = new BlackJack();
                    gameRunningCoroutine = MelonCoroutines.Start(BlackJack.Run()); //load game
                    break;
                case Games.JacksOrBetter:
                    Log("Game JacksOrBetter Started!", (bool)Main.debugging.SavedValue);
                    jacksOrBetterInstance = new JacksOrBetter();
                    gameRunningCoroutine = MelonCoroutines.Start(JacksOrBetter.Run()); //load game
                    break;
                default:
                    yield break;
            }
            yield return gameRunningCoroutine;
            ClearActiveObjects();
            Log("Game Completed!", (bool)Main.debugging.SavedValue);
            SetupStart();
        }

        public static void ClearActiveObjects()
        {
            Log("Clearing Active Objects", (bool)Main.debugging.SavedValue);
            Transform activeObjectsSpot = instance.transform.GetChild(2);
            for (int i = activeObjectsSpot.GetChildCount() - 1; i >= 0; i--)
            {
                Log("Clearing Active Object: " + activeObjectsSpot.GetChild(i).gameObject.name, (bool)Main.debugging.SavedValue);
                GameObject.Destroy(activeObjectsSpot.GetChild(i).gameObject);
            }
            Log("Done Clearing Active Objects", (bool)Main.debugging.SavedValue);
        }

        public GameObject LoadMenuButton(string title, Vector3 position, Quaternion rotation, Vector3 localScale, Action listener = null)
        {
            Log("Loading Menu Button: " + title, (bool)Main.debugging.SavedValue);
            GameObject button = (listener != null ? Calls.Create.NewButton(listener) : Calls.Create.NewButton());
            button.name = title + " Button";
            button.transform.SetParent(this.transform.GetChild(2));
            button.transform.localPosition = new Vector3(position.x, position.y, position.z);
            button.transform.localRotation = rotation;
            button.transform.localScale = localScale;
            LoadText(button, title);
            Log("Done Loading Menu Button: " + title, (bool)Main.debugging.SavedValue);
            return button;
        }

        public static object[] ShuffleDealerDeck()
        {
            Transform cardsParent = dealerDeck.transform.GetChild(0).GetChild(0);
            object[] shufflings = new object[cardsParent.GetChildCount()];
            for (int i = 0; i < cardsParent.GetChildCount(); i++)
            {
                shufflings[i] = MelonCoroutines.Start(SpinCard(cardsParent.GetChild(i).gameObject));
            }
            return shufflings;
        }

        public static IEnumerator SpinCard(GameObject card)
        {
            yield return new WaitForSeconds(((float)Table.random.Next(10, 50)) / 100); //adds variation to shuffle start time
            int fixedUpdatesToCompleteSpin = Table.random.Next(25, 50); //adds variation to spin speed
            int updatesLeft = fixedUpdatesToCompleteSpin;
            float amountToSpin = 360f / ((float)fixedUpdatesToCompleteSpin);
            while (updatesLeft >= 0)
            {
                try
                {
                    card.transform.localRotation = Quaternion.Euler(card.transform.localRotation.x, card.transform.localRotation.y, amountToSpin * (fixedUpdatesToCompleteSpin - updatesLeft));
                }
                catch { yield break; }
                updatesLeft--;
                yield return new WaitForFixedUpdate();
            }
            yield break;
        }

        public static GameObject SpawnButton(Transform parent, string title, Vector3 position, Quaternion rotation, Vector3 localScale, Action listener = null)
        {
            Log("Loading Button: " + title, (bool)Main.debugging.SavedValue);
            GameObject button = (listener != null ? Calls.Create.NewButton(listener) : Calls.Create.NewButton());
            button.name = title + " Button";
            button.transform.SetParent(parent);
            button.transform.localPosition = position;
            button.transform.localRotation = rotation;
            button.transform.localScale = localScale;
            LoadText(button, title);
            Log("Done Loading Button: " + title, (bool)Main.debugging.SavedValue);
            return button;
        }

        public static GameObject SpawnText(Transform parent, string title, Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Log("Loading Text: " + title, (bool)Main.debugging.SavedValue);
            GameObject text = Calls.Create.NewText();
            text.name = title + " Text";
            text.transform.SetParent(parent);
            text.transform.localPosition = position;
            text.transform.localRotation = rotation;
            text.transform.localScale = localScale;
            text.GetComponent<TextMeshPro>().text = title;
            Log("Done Loading Text: " + title, (bool)Main.debugging.SavedValue);
            return text;
        }

        public static void LoadText(GameObject button, string title)
        {
            Log("Loading Menu Button Text: " + title, (bool)Main.debugging.SavedValue);
            GameObject text = Calls.Create.NewText();
            text.name = title + "Text";
            text.transform.SetParent(button.transform);
            text.transform.localPosition = new Vector3(0f, 0f, -0.25f);
            text.transform.localRotation = Quaternion.Euler(90, 180, 0);
            text.transform.localScale = Vector3.one;
            TextMeshPro textTMP = text.GetComponent<TextMeshPro>();
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.enableWordWrapping = false;
            textTMP.text = title;
            Log("Done Loading Menu Button Text: " + title, (bool)Main.debugging.SavedValue);
        }
    }
}
