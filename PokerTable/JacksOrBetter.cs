using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace PokerTable
{
    [RegisterTypeInIl2Cpp]
    public class JacksOrBetter : MonoBehaviour
    {
        public enum PayoutResult : int
        {
            Nothing,
            JacksOrBetter,
            TwoPair,
            ThreeOfAKind,
            Straight,
            Flush,
            FullHouse,
            FourOfAKind,
            StraightFlush,
            RoyalFlush
        }

        public enum CardSuits : int
        {
            Spades,
            Hearts,
            Diamonds,
            Clubs
        }

        private static string[] payoutsString = { "Nothing",
            "Jacks or Better",
            "Two Pair",
            "Three of a Kind",
            "Straight",
            "Flush",
            "Full House",
            "Four of a Kind",
            "Straight Flush",
            "Royal Flush" };
        private static float[] payoutsAmounts = { 0f, 1f, 2f, 3f, 4f, 6f, 9f, 25f, 50f, 976f };
        private static int betAmount = 1;
        private static bool gameLoopRunning = false;
        private static TextMeshPro betTextComponent = null;
        private static GameObject storedBetsMenu = null, storedOptionsMenu = null;
        private static bool betAccepted = false, userQuits = false;
        private static List<int> deck, hand;
        public static object gameLoopCoroutine = null;
        private static Transform storedGamePartsTransform = null, activeGamePartsTransform = null;

        public static void Log(string msg, bool sendMsg = true)
        {
            if (sendMsg)
            {
                Table.Log($"JacksOrBetter - {msg}", sendMsg);
            }
        }

        public static void Warn(string msg, bool sendMsg = true)
        {
            if (sendMsg)
            {
                Table.Warn($"JacksOrBetter - {msg}", sendMsg);
            }
        }

        public static void Error(string msg)
        {
            Table.Error($"JacksOrBetter - {msg}");
        }

        public static void ShowSplash()
        {
            storedGamePartsTransform = Main.loadedTable.transform.GetChild(1);
            activeGamePartsTransform = Main.loadedTable.transform.GetChild(2);
            GameObject splashScreen = new GameObject("SplashScreen");
            splashScreen.transform.SetParent(activeGamePartsTransform);
            splashScreen.transform.localPosition = new Vector3(0, 0.9454f, 0);
            splashScreen.transform.localRotation = Quaternion.identity;
            splashScreen.transform.localScale = Vector3.one;
            GameObject splashScreenText = Table.SpawnText(splashScreen.transform,
                /*Title*/"Jacks or Better",
                /*position*/ new Vector3(0f, 0f, 0.4f),
                /*rotation*/Quaternion.Euler(90f, 180f, 0f),
                /*scale*/ new Vector3(1.25f, 1.25f, 1.25f));
            TextMeshPro splashScreenTextTMP = splashScreenText.GetComponent<TextMeshPro>();
            splashScreenTextTMP.alignment = TextAlignmentOptions.Center;
            splashScreenTextTMP.enableWordWrapping = false;
        }

        public static IEnumerator Run()
        {
            Log("Run Running", (bool)Main.debugging.SavedValue);
            SetupStart();
            gameLoopCoroutine = MelonCoroutines.Start(GameLoop());
            yield return gameLoopCoroutine;
            Log("Run Completed", (bool)Main.debugging.SavedValue);
        }

        private static void SetupStart()
        {
            Log("SetupStart Running", (bool)Main.debugging.SavedValue);
            gameLoopRunning = false;
            RandomizeDeck();
            SetupBetMenu();
            SetupOptionsMenu();
            Log("SetupStart Completed", (bool)Main.debugging.SavedValue);
        }

        private static GameObject cardSpots = null;
        private static void SetupCardSpots()
        {
            Log("SetupStart Started", (bool)Main.debugging.SavedValue);
            cardSpots = GameObject.Instantiate(storedGamePartsTransform.GetChild(2).gameObject);
            cardSpots.name = "CardSpots";
            cardSpots.transform.SetParent(activeGamePartsTransform);
            cardSpots.transform.localPosition = Vector3.zero;
            cardSpots.transform.localRotation = Quaternion.identity;
            cardSpots.transform.localScale = Vector3.one;
            Log("SetupStart Completed", (bool)Main.debugging.SavedValue);
        }

        private static int coinsGained = 0;
        private static IEnumerator GameLoop()
        {
            Log("GameLoop Running", (bool)Main.debugging.SavedValue);
            gameLoopRunning = true;
            while (gameLoopRunning)
            {
                //reshuffle if needed
                if (deck.Count <= 26 * (int)Main.deckCount.SavedValue)
                {
                    RandomizeDeck(false);
                    yield return new WaitForSeconds(1f);
                }
                //check for coins
                betAmount = 1;
                if (!Table.freePlay && Main.GetPlayerCoinCount() < betAmount)
                {
                    GameObject youBrokeAsFuck = Table.SpawnText(activeGamePartsTransform.transform,
                        /*Title*/$"Too Few Coins. Try Free Play or{Environment.NewLine}Complete More Poses to Earn Coins",
                        /*position*/ new Vector3(0f, Table.TABLEHEIGHT - 0.025f, 0.65f),
                        /*rotation*/Quaternion.Euler(90, 180, 0),
                        /*scale*/ new Vector3(1f, 1f, 1f));
                    youBrokeAsFuck.name = "You Broke As Fuck";
                    TextMeshPro endGameTextTMP = youBrokeAsFuck.GetComponent<TextMeshPro>();
                    endGameTextTMP.alignment = TextAlignmentOptions.Center;
                    endGameTextTMP.enableWordWrapping = false;
                    continueShuffling = false;
                    Log("Not Enough Coins, Exiting Jacks Or Better", (bool)Main.debugging.SavedValue);
                    yield return new WaitForSeconds(1.5f);
                    gameLoopRunning = false;
                    break;
                }
                //starting bet
                Table.ClearActiveObjects();
                yield return MelonCoroutines.Start(RunBetMenu());
                Table.ClearActiveObjects();
                yield return new WaitForSeconds(1.5f);
                //-user quits
                if (userQuits)
                {
                    gameLoopRunning = false;
                }
                //bet accepted
                if (betAccepted)
                {
                    Log("Player Bet Accepted: " + betAmount, true);
                    Main.Payout(-betAmount, 1f);
                    coinsGained = 0;
                    yield return MelonCoroutines.Start(PlayHand());
                    yield return new WaitForSeconds(0.5f);
                    //check hand for win
                    yield return MelonCoroutines.Start(CheckPayout());
                    //clear active cards
                    ClearCardSpots();
                    bool continuePressed = false;
                    //load end game text
                    GameObject endGameText = Table.SpawnText(activeGamePartsTransform.transform,
                        /*Title*/$"End Game Text",
                        /*position*/ new Vector3(0f, Table.TABLEHEIGHT - 0.025f, 0.5f),
                        /*rotation*/Quaternion.Euler(90, 180, 0),
                        /*scale*/ new Vector3(1f, 1f, 1f));
                    endGameText.name = "End Game Text";
                    TextMeshPro endGameTextTMP = endGameText.GetComponent<TextMeshPro>();
                    endGameTextTMP.alignment = TextAlignmentOptions.Center;
                    endGameTextTMP.enableWordWrapping = false;
                    endGameTextTMP.text = $"Hand Contains: {payoutsString[(int)thisHandsPayout]}{Environment.NewLine}Total Coins Gained: {coinsGained}";
                    //load continue button
                    GameObject continueButton = Table.instance.LoadMenuButton("Continue",
                        /*position*/ new Vector3(0f, Table.TABLEHEIGHT - 0.025f, 0.96f),
                        /*rotation*/Quaternion.Euler(0, 0, 0),
                        /*scale*/ new Vector3(0.5f, 0.5f, 0.5f),
                        () => { continuePressed = true; });
                    continueButton.name = "Continue";
                    //wait for continue to be pressed
                    while (!continuePressed)
                    {
                        yield return new WaitForFixedUpdate();
                    }
                }
                Table.ClearActiveObjects();
            }
            Log("GameLoop Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static PayoutResult thisHandsPayout = PayoutResult.Nothing;
        private static IEnumerator CheckPayout()
        {
            Log("CheckPayout Started", (bool)Main.debugging.SavedValue);
            thisHandsPayout = GetWinType();
            coinsGained = Main.Payout(betAmount, payoutsAmounts[(int)thisHandsPayout]);
            Log($"Hand Contains: {payoutsString[(int)thisHandsPayout]}, Paying Out: {coinsGained}", true);
            object payoutCoroutine = null;
            if (thisHandsPayout == PayoutResult.Nothing)
            {
                payoutCoroutine = MelonCoroutines.Start(PlayPlayerLose());
            }
            else
            {
                payoutCoroutine = MelonCoroutines.Start(PlayPlayerWin());
            }
            yield return payoutCoroutine;
            Log("CheckPayout Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator PlayPlayerWin()
        {
            Log("PlayPlayerWin Started", (bool)Main.debugging.SavedValue);
            List<object> coroutines = new List<object>();
            yield return new WaitForSeconds(0.25f);
            for (int i = 0; i < 5; i++)
            {
                coroutines.Add(MelonCoroutines.Start(PlayCardWinCoroutine(cardSpots.transform.GetChild(i).GetChild(0).gameObject, true)));
                yield return new WaitForSeconds(0.1f);
            }
            foreach (object coroutine in coroutines) { yield return coroutine; }
            yield return new WaitForSeconds(0.25f);
            Log("PlayPlayerWin Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator PlayPlayerLose()
        {
            Log("PlayPlayerLose Started", (bool)Main.debugging.SavedValue);
            List<object> coroutines = new List<object>();
            yield return new WaitForSeconds(0.25f);
            for (int i = 0; i < 5; i++)
            {
                coroutines.Add(MelonCoroutines.Start(PlayCardShrinkCoroutine(cardSpots.transform.GetChild(i).GetChild(0).gameObject, true)));
            }
            foreach(object coroutine in coroutines) { yield return coroutine; }
            yield return new WaitForSeconds(0.25f);
            Log("PlayPlayerLose Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static PayoutResult GetWinType()
        {
            if (HasRoyalFlush())
            {
                return PayoutResult.RoyalFlush;
            }
            else if (HasStraightFlush())
            {
                return PayoutResult.StraightFlush;
            }
            else if (HasFourOfAKind())
            {
                return PayoutResult.FourOfAKind;
            }
            else if (HasFullHouse())
            {
                return PayoutResult.FullHouse;
            }
            else if (HasFlush())
            {
                return PayoutResult.Flush;
            }
            else if (HasStraight())
            {
                return PayoutResult.Straight;
            }
            else if (HasThreeOfAKind())
            {
                return PayoutResult.ThreeOfAKind;
            }
            else if (HasTwoPair())
            {
                return PayoutResult.TwoPair;
            }
            else if (HasJacksOrBetter())
            {
                return PayoutResult.JacksOrBetter;
            }
            //if no win
            else { return PayoutResult.Nothing; }
        }

        private static bool HasJacksOrBetter()
        {
            List<int> tempHand = new List<int>();
            foreach (int i in hand) { tempHand.Add(getCardValue(i)); }
            List<int> cardCountList = new List<int>();
            for (int i = 0; i < 13; i++) { cardCountList.Add(0); }
            foreach (int card in tempHand) { cardCountList[card - 1]++; }
            return ((cardCountList[0] >= 2) || (cardCountList[10] >= 2) || (cardCountList[11] >= 2) || (cardCountList[12] >= 2));
        }

        private static bool HasTwoPair()
        {
            List<int> tempHand = new List<int>();
            foreach (int i in hand) { tempHand.Add(getCardValue(i)); }
            List<int> cardCountList = new List<int>();
            for (int i = 0; i < 13; i++) { cardCountList.Add(0); }
            foreach (int card in tempHand) { cardCountList[card - 1]++; }
            bool hasTwoPair = false;
            if (cardCountList.Contains(2))
            {
                cardCountList.Remove(2);
                if (cardCountList.Contains(2)) { hasTwoPair = true; }
            }
            return hasTwoPair;
        }

        private static bool HasThreeOfAKind() { return HasXOfAKind(3); }

        private static bool HasFourOfAKind() { return HasXOfAKind(4); }

        private static bool HasFullHouse()
        {
            List<int> tempHand = new List<int>();
            foreach (int i in hand) { tempHand.Add(getCardValue(i)); }
            List<int> cardCountList = new List<int>();
            for (int i = 0; i < 13; i++) { cardCountList.Add(0); }
            foreach (int card in tempHand) { cardCountList[card - 1]++; }
            cardCountList.Sort();
            return (cardCountList[12] == 3) && (cardCountList[11] == 2);
        }

        private static bool HasStraightFlush()
        {
            return (HasFlush()
                && HasStraight());
        }

        private static bool HasRoyalFlush()
        {
            return (HasFlush()
                && DoesHandContainCardValue(1)
                && DoesHandContainCardValue(10)
                && DoesHandContainCardValue(11)
                && DoesHandContainCardValue(12)
                && DoesHandContainCardValue(13));
        }

        private static bool HasStraight()
        {
            List<int> tempHand = new List<int>();
            foreach (int i in hand) { tempHand.Add(getCardValue(i)); }
            tempHand.Sort();
            bool hasAce = false;
            bool usedAce = false;
            if (tempHand[0] == 1)
            {
                hasAce = true;
                tempHand.Add(tempHand[0]);
            }
            bool inLine = true;
            for (int i = 0; i < tempHand.Count - 1; i++)
            {
                if (tempHand[i] + 1 != tempHand[i + 1])
                {
                    if (hasAce && !usedAce) { usedAce = true; }
                    else { inLine = false; break; }
                }
            }
            return inLine;
        }

        private static bool HasFlush()
        {
            CardSuits[] suits = new CardSuits[5];
            suits[0] = getCardSuit(hand[0]);
            suits[1] = getCardSuit(hand[1]);
            suits[2] = getCardSuit(hand[2]);
            suits[3] = getCardSuit(hand[3]);
            suits[4] = getCardSuit(hand[4]);
            return ((suits[0] == suits[1]) && (suits[0] == suits[2]) && (suits[0] == suits[3]) && (suits[0] == suits[4]));
        }

        private static bool DoesHandContainCardValue(int cardNumberToFind)
        {
            return ((getCardValue(hand[0]) == cardNumberToFind)
                || (getCardValue(hand[1]) == cardNumberToFind)
                || (getCardValue(hand[2]) == cardNumberToFind)
                || (getCardValue(hand[3]) == cardNumberToFind)
                || (getCardValue(hand[4]) == cardNumberToFind));
        }

        private static bool HasXOfAKind(int x)
        {
            List<int> tempHand = new List<int>();
            foreach (int i in hand) { tempHand.Add(getCardValue(i)); }
            List<int> cardCountList = new List<int>();
            for (int i = 0; i < 13; i++) { cardCountList.Add(0); }
            foreach(int card in tempHand) { cardCountList[card - 1]++; }
            cardCountList.Sort();
            return (x <= cardCountList[12]);
        }

        private static int getCardValue(int rawCard)
        {
            int card = rawCard;
            while (card >= 13) { card -= 13; }
            return card + 1;
        }

        private static CardSuits getCardSuit(int rawCard)
        {
            int card = rawCard;
            CardSuits suit = 0;
            while (card >= 13)
            {
                card -= 13;
                suit++;
            }
            return suit;
        }

        private static IEnumerator PlayCardShrinkCoroutine(GameObject card, bool destroyAfter = false)
        {
            Vector3 startingScale = card.transform.localScale;
            Vector3 scalePerTick = startingScale / 50f;
            for (int i = 0; i < 50; i++)
            {
                card.transform.localScale -= scalePerTick;
                yield return new WaitForFixedUpdate();
            }
            if (destroyAfter) { GameObject.Destroy(card); }
            yield break;
        }

        private static IEnumerator PlayCardWinCoroutine(GameObject card, bool destroyAfter = false)
        {
            Vector3 startingPosition = card.transform.localPosition;
            Vector3 positionPerTick = Vector3.up * 0.004f;
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition += positionPerTick;
                yield return new WaitForFixedUpdate();
            }
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition -= positionPerTick;
                yield return new WaitForFixedUpdate();
            }
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition += positionPerTick;
                yield return new WaitForFixedUpdate();
            }
            object winShrinkCoroutine = MelonCoroutines.Start(PlayCardShrinkCoroutine(card));
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition -= positionPerTick;
                yield return new WaitForFixedUpdate();
            }
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition += positionPerTick;
                yield return new WaitForFixedUpdate();
            }
            yield return winShrinkCoroutine;
            if (destroyAfter) { GameObject.Destroy(card); }
            yield break;
        }

        private static IEnumerator PlayHand()
        {
            Log("PlayHand Running", (bool)Main.debugging.SavedValue);
            hand = new List<int>();
            SetupCardSpots();
            //deal hand
            yield return MelonCoroutines.Start(DrawStartingHand());
            //offer options
            yield return MelonCoroutines.Start(RunOptionsMenu());
            //replace removed cards
            yield return MelonCoroutines.Start(ReplaceMissingCardsInHand());
            Log("PlayHand Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator DrawStartingHand()
        {
            Log("DrawStartingHand Started", (bool)Main.debugging.SavedValue);
            hand.Add(DrawCard());
            Log("Player Card 1: " + Table.CardString[hand[0]], (bool)Main.debugging.SavedValue);
            yield return PlayDrawCardAnimation(hand[0], cardSpots.transform.GetChild(0), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));

            hand.Add(DrawCard());
            Log("Player Card 2: " + Table.CardString[hand[1]], (bool)Main.debugging.SavedValue);
            yield return PlayDrawCardAnimation(hand[1], cardSpots.transform.GetChild(1), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));

            hand.Add(DrawCard());
            Log("Player Card 3: " + Table.CardString[hand[2]], (bool)Main.debugging.SavedValue);
            yield return PlayDrawCardAnimation(hand[2], cardSpots.transform.GetChild(2), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));

            hand.Add(DrawCard());
            Log("Player Card 4: " + Table.CardString[hand[3]], (bool)Main.debugging.SavedValue);
            yield return PlayDrawCardAnimation(hand[3], cardSpots.transform.GetChild(3), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));

            hand.Add(DrawCard());
            Log("Player Card 5: " + Table.CardString[hand[4]], (bool)Main.debugging.SavedValue);
            yield return PlayDrawCardAnimation(hand[4], cardSpots.transform.GetChild(4), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
            Log("DrawStartingHand Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator ReplaceMissingCardsInHand()
        {
            Log("ReplaceMissingCardsInHand Started", (bool)Main.debugging.SavedValue);
            for (int i = 0; i < 5; i++)
            {
                if (hand[i] == -1)
                {
                    hand[i] = DrawCard();
                    yield return PlayDrawCardAnimation(hand[i], cardSpots.transform.GetChild(i), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
                    yield return new WaitForSeconds(0.25f);
                }
            }
            Log("ReplaceMissingCardsInHand Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        //Note: position, not localPosition. This sets the linear path to localPosition 0,0 in animation
        private static object PlayDrawCardAnimation(int CardToDraw, Transform parent, Vector3 position, Quaternion localRotation, bool playRotate = true)
        {
            Log("PlayDrawCardAnimation Started", (bool)Main.debugging.SavedValue);
            GameObject card = GameObject.Instantiate(Table.storedDeckOfCards.transform.GetChild(CardToDraw).gameObject);
            card.transform.SetParent(parent);
            card.transform.position = position;
            card.transform.localRotation = localRotation;
            Log("PlayDrawCardAnimation Completed", (bool)Main.debugging.SavedValue);
            return MelonCoroutines.Start(PlayDrawAnimation(card, 25, playRotate));
        }

        private static int DrawCard()
        {
            int card = deck[0];
            deck.RemoveAt(0);
            Table.dealerDeck.transform.localScale = new Vector3(1, ((float)deck.Count) / 52f, 1);
            return card;
        }

        //moves it to localPosition 0. flips 180 unless specified (dealer card 2)
        private static IEnumerator PlayDrawAnimation(GameObject card, int ticks = 25, bool playRotate = true)
        {
            Log("PlayDrawAnimation Started", (bool)Main.debugging.SavedValue);
            Vector3 distancePerTick = (card.transform.localPosition) / ticks;
            float rotationPerTick = 180f / ((float)ticks);
            float currentRotation = card.transform.localRotation.eulerAngles.y;
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition -= distancePerTick;
                if (playRotate)
                {
                    currentRotation -= rotationPerTick;
                    card.transform.localRotation = Quaternion.Euler(card.transform.localRotation.eulerAngles.x, currentRotation, card.transform.localRotation.eulerAngles.z);
                }
                yield return new WaitForFixedUpdate();
            }
            Log("PlayDrawAnimation Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        //spawns bet menu and waits for an option to be pressed
        private static IEnumerator RunBetMenu()
        {
            Log("RunBetMenu Started", (bool)Main.debugging.SavedValue);
            betAmount = 1;
            GameObject betsMenu = SpawnBetsMenu();
            Log("Spawned Bets Menu, Waiting for Accepted Bet", (bool)Main.debugging.SavedValue);
            while (!betAccepted && !userQuits)
            {
                yield return new WaitForFixedUpdate();
            }
            Log("RunBetMenu Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static GameObject SpawnBetsMenu()
        {
            Log("SpawnBetsMenu Started", (bool)Main.debugging.SavedValue);
            GameObject spawnedBetsMenu = GameObject.Instantiate(storedBetsMenu);
            spawnedBetsMenu.transform.SetParent(activeGamePartsTransform);
            spawnedBetsMenu.transform.localPosition = new Vector3(0, Table.TABLEHEIGHT - 0.052f, 0);
            spawnedBetsMenu.transform.localRotation = Quaternion.identity;
            spawnedBetsMenu.transform.localScale = Vector3.one;

            betTextComponent = spawnedBetsMenu.transform.GetChild(1).GetComponent<TextMeshPro>();
            UpdateBetAmountText();
            //Bet Down
            spawnedBetsMenu.transform.GetChild(2).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Bet Down Pressed", (bool)Main.debugging.SavedValue);
                if (1 < betAmount)
                {
                    betAmount--;
                    Log("New Bet: " + betAmount, (bool)Main.debugging.SavedValue);
                    UpdateBetAmountText();
                }
            }));
            //Bet Up
            spawnedBetsMenu.transform.GetChild(3).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Bet Up Pressed", (bool)Main.debugging.SavedValue);
                if (Main.GetPlayerCoinCount() >= betAmount + 1)
                {
                    betAmount++;
                    Log("New Bet: " + betAmount, (bool)Main.debugging.SavedValue);
                    UpdateBetAmountText();
                }
            }));
            //Bet Accepted
            spawnedBetsMenu.transform.GetChild(4).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Bet Accepted: " + betAmount, (bool)Main.debugging.SavedValue);
                betAccepted = true;
                continueShuffling = false;
            }));
            //Quit
            spawnedBetsMenu.transform.GetChild(5).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("User Quit During Betting", (bool)Main.debugging.SavedValue);
                userQuits = true;
                continueShuffling = false;
            }));
            //Bet Min
            spawnedBetsMenu.transform.GetChild(6).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Bet Min Pressed", (bool)Main.debugging.SavedValue);
                betAmount = 1;
                Log("New Bet: " + betAmount, (bool)Main.debugging.SavedValue);
                UpdateBetAmountText();
            }));
            //Bet Max
            spawnedBetsMenu.transform.GetChild(7).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Bet Max Pressed", (bool)Main.debugging.SavedValue);
                betAmount = Main.GetPlayerCoinCount();
                Log("New Bet: " + betAmount, (bool)Main.debugging.SavedValue);
                UpdateBetAmountText();
            }));
            spawnedBetsMenu.transform.GetChild(7).GetChild(2).localPosition = new Vector3(0f, 0f, -0.25f); //fixes weirdness about Text not being correct
            betAccepted = false;
            userQuits = false;
            Log("SpawnBetsMenu Completed", (bool)Main.debugging.SavedValue);
            return spawnedBetsMenu;
        }

        private static void RandomizeDeck(bool continueShuffling = true)
        {
            Log("RandomizeDeck Running", (bool)Main.debugging.SavedValue);
            object[] shufflings = Table.ShuffleDealerDeck();
            if (continueShuffling)
            {
                ContinueShufflings(shufflings);
            }
            deck = new List<int>();
            List<int> tempDeck = new List<int>();
            int cardCount = 0;
            for (int i = 0; i < 52 * (int)Main.deckCount.SavedValue; i++)
            {
                deck.Add(cardCount);
                cardCount++;
                if (cardCount == 52) { cardCount = 0; }
            }
            Log("Deck Setup Done", (bool)Main.debugging.SavedValue);
            Table.dealerDeck.transform.localScale = new Vector3(1, ((float)deck.Count) / 52f, 1);
            tempDeck.Clear();
            while (deck.Count > 0)
            {
                int spot = Table.random.Next(deck.Count);
                tempDeck.Add(deck[spot]);
                deck.RemoveAt(spot);
            }
            deck.AddRange(tempDeck);
            Log("RandomizeDeck Complete", (bool)Main.debugging.SavedValue);
        }

        private static bool continueShuffling = true;
        private static void ContinueShufflings(object[] shufflings)
        {
            continueShuffling = true;
            Transform cardsParent = Table.dealerDeck.transform.GetChild(0).GetChild(0);
            for (int i = 0; i < shufflings.Length; i++)
            {
                MelonCoroutines.Start(ShufflingCard(cardsParent.transform.GetChild(i).gameObject, shufflings[i]));
            }
        }

        private static IEnumerator ShufflingCard(GameObject card, object spinCoroutine)
        {
            Transform cardsParent = Table.dealerDeck.transform.GetChild(0).GetChild(0);
            yield return spinCoroutine;
            while (continueShuffling)
            {
                spinCoroutine = MelonCoroutines.Start(Table.SpinCard(card));
                yield return spinCoroutine;
                yield return new WaitForFixedUpdate();
            }
            yield break;
        }

        private static void SetupBetMenu()
        {
            Log("SetupBetMenu Started", (bool)Main.debugging.SavedValue);
            if (storedBetsMenu != null) { GameObject.Destroy(storedBetsMenu); }
            storedBetsMenu = new GameObject("BetsMenu");
            storedBetsMenu.transform.SetParent(storedGamePartsTransform);
            storedBetsMenu.transform.localPosition = Vector3.zero;
            storedBetsMenu.transform.localRotation = Quaternion.identity;
            storedBetsMenu.transform.localScale = Vector3.one;

            GameObject betTitleText = Table.SpawnText(storedBetsMenu.transform,
                /*Title*/"How Much To Bet?",
                /*position*/ new Vector3(0f, 0f, 0.35f),
                /*rotation*/Quaternion.Euler(90f, 180f, 0f),
                /*scale*/ new Vector3(1.25f, 1.25f, 1.25f));
            TextMeshPro betTitleTextTMP = betTitleText.GetComponent<TextMeshPro>();
            betTitleTextTMP.alignment = TextAlignmentOptions.Center;
            betTitleTextTMP.enableWordWrapping = false;

            GameObject betText = Table.SpawnText(storedBetsMenu.transform,
                /*Title*/"BetText",
                /*position*/ new Vector3(0f, 0f, 0.5f),
                /*rotation*/Quaternion.Euler(90, 180, 0),
                /*scale*/ new Vector3(1f, 1f, 1f));
            TextMeshPro betTextTMP = betText.GetComponent<TextMeshPro>();
            betTextTMP.alignment = TextAlignmentOptions.Center;
            betTextTMP.enableWordWrapping = false;
            betTextComponent = betText.GetComponent<TextMeshPro>();
            UpdateBetAmountText();

            GameObject betDownButton = Table.SpawnButton(storedBetsMenu.transform,
                /*Title*/"Down",
                /*position*/ new Vector3(0.25f, 0f, 0.9f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            GameObject betUpButton = Table.SpawnButton(storedBetsMenu.transform,
                /*Title*/"Up",
                /*position*/ new Vector3(-0.25f, 0f, 0.9f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            GameObject betAcceptButton = Table.SpawnButton(storedBetsMenu.transform,
                /*Title*/"Accept",
                /*position*/ new Vector3(0f, 0f, 0.75f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            GameObject UserQuitButton = Table.SpawnButton(storedBetsMenu.transform,
                /*Title*/"Exit",
                /*position*/ new Vector3(0.75f, 0f, 0.25f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            GameObject betMinButton = Table.SpawnButton(storedBetsMenu.transform,
                /*Title*/"Min",
                /*position*/ new Vector3(0.5f, 0f, 0.8f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            GameObject betMaxButton = Table.SpawnButton(storedBetsMenu.transform,
                /*Title*/"Max",
                /*position*/ new Vector3(-0.5f, 0f, 0.8f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));
            Log("SetupBetMenu Completed", (bool)Main.debugging.SavedValue);
        }

        private static bool pressedContinue, removeCard1 = true, removeCard2 = true, removeCard3 = true, removeCard4 = true, removeCard5 = true;
        private static GameObject SpawnOptionsMenu()
        {
            Log("SpawnOptionsMenu Started", (bool)Main.debugging.SavedValue);
            pressedContinue = false;
            removeCard1 = true;
            removeCard2 = true;
            removeCard3 = true;
            removeCard4 = true;
            removeCard5 = true;
            GameObject spawnedOptionsMenu = GameObject.Instantiate(storedOptionsMenu);
            spawnedOptionsMenu.transform.SetParent(activeGamePartsTransform);
            spawnedOptionsMenu.transform.localPosition = new Vector3(0, Table.TABLEHEIGHT - 0.052f, 0);
            spawnedOptionsMenu.transform.localRotation = Quaternion.identity;
            spawnedOptionsMenu.transform.localScale = Vector3.one;
            //card 1
            spawnedOptionsMenu.transform.GetChild(0).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Card 1 Pressed", (bool)Main.debugging.SavedValue);
                removeCard1 = !removeCard1;
                spawnedOptionsMenu.transform.GetChild(0).GetChild(2).GetComponent<TextMeshPro>().text = removeCard1 ? "Remove" : "Keep";
            }));
            //card 2
            spawnedOptionsMenu.transform.GetChild(1).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Card 2 Pressed", (bool)Main.debugging.SavedValue);
                removeCard2 = !removeCard2;
                spawnedOptionsMenu.transform.GetChild(1).GetChild(2).GetComponent<TextMeshPro>().text = removeCard2 ? "Remove" : "Keep";
            }));
            //card 3
            spawnedOptionsMenu.transform.GetChild(2).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Card 3 Pressed", (bool)Main.debugging.SavedValue);
                removeCard3 = !removeCard3;
                spawnedOptionsMenu.transform.GetChild(2).GetChild(2).GetComponent<TextMeshPro>().text = removeCard3 ? "Remove" : "Keep";
            }));
            //card 4
            spawnedOptionsMenu.transform.GetChild(3).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Card 4 Pressed", (bool)Main.debugging.SavedValue);
                removeCard4 = !removeCard4;
                spawnedOptionsMenu.transform.GetChild(3).GetChild(2).GetComponent<TextMeshPro>().text = removeCard4 ? "Remove" : "Keep";
            }));
            //card 5
            spawnedOptionsMenu.transform.GetChild(4).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Card 5 Pressed", (bool)Main.debugging.SavedValue);
                removeCard5 = !removeCard5;
                spawnedOptionsMenu.transform.GetChild(4).GetChild(2).GetComponent<TextMeshPro>().text = removeCard5 ? "Remove" : "Keep";
            }));
            //continue
            spawnedOptionsMenu.transform.GetChild(5).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Continue Pressed", (bool)Main.debugging.SavedValue);
                pressedContinue = true;
            }));
            spawnedOptionsMenu.transform.GetChild(5).GetChild(2).localPosition = new Vector3(0f, 0f, -0.2f); //fixes weirdness about Text not being correct
            Log("SpawnOptionsMenu Completed", (bool)Main.debugging.SavedValue);
            return spawnedOptionsMenu;
        }

        //spawns optionsMenu menu and waits for an option to be pressed
        private static IEnumerator RunOptionsMenu()
        {
            Log("RunOptionsMenu Started", (bool)Main.debugging.SavedValue);
            GameObject optionsMenu = SpawnOptionsMenu();
            Log("Spawned Options Menu, Waiting for Selection", (bool)Main.debugging.SavedValue);
            while (!pressedContinue)
            {
                yield return new WaitForFixedUpdate();
            }
            GameObject.Destroy(optionsMenu);
            //options done, removing cards selected for removal
            Log($"Keep / Remove List: {removeCard1} {removeCard2} {removeCard3} {removeCard4} {removeCard5}", (bool)Main.debugging.SavedValue);
            List<object> cardsRemoving = new List<object>();
            if (removeCard1)
            {
                Log("Removing Card 1", (bool)Main.debugging.SavedValue);
                cardsRemoving.Add(MelonCoroutines.Start(PlayCardShrinkCoroutine(cardSpots.transform.GetChild(0).GetChild(0).gameObject, true)));
                hand[0] = -1;
            }
            if (removeCard2)
            {
                Log("Removing Card 2", (bool)Main.debugging.SavedValue);
                cardsRemoving.Add(MelonCoroutines.Start(PlayCardShrinkCoroutine(cardSpots.transform.GetChild(1).GetChild(0).gameObject, true)));
                hand[1] = -1;
            }
            if (removeCard3)
            {
                Log("Removing Card 3", (bool)Main.debugging.SavedValue);
                cardsRemoving.Add(MelonCoroutines.Start(PlayCardShrinkCoroutine(cardSpots.transform.GetChild(2).GetChild(0).gameObject, true)));
                hand[2] = -1;
            }
            if (removeCard4)
            {
                Log("Removing Card 4", (bool)Main.debugging.SavedValue);
                cardsRemoving.Add(MelonCoroutines.Start(PlayCardShrinkCoroutine(cardSpots.transform.GetChild(3).GetChild(0).gameObject, true)));
                hand[3] = -1;
            }
            if (removeCard5)
            {
                Log("Removing Card 5", (bool)Main.debugging.SavedValue);
                cardsRemoving.Add(MelonCoroutines.Start(PlayCardShrinkCoroutine(cardSpots.transform.GetChild(4).GetChild(0).gameObject, true)));
                hand[4] = -1;
            }
            //wait for removal of cards to be complete
            foreach (object coroutine in cardsRemoving) { yield return coroutine; }
            Log("RunOptionsMenu Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static void ClearCardSpots()
        {
            Log("Clearing Card Spots", (bool)Main.debugging.SavedValue);
            for (int i = 0; i < cardSpots.transform.GetChildCount(); i++)
            {
                for (int x = cardSpots.transform.GetChild(i).childCount - 1; x >= 0; x--)
                {
                    GameObject.Destroy(cardSpots.transform.GetChild(i).GetChild(x).gameObject);
                }
            }
        }

        private static void SetupOptionsMenu()
        {
            Log("SetupOptionsMenu Started", (bool)Main.debugging.SavedValue);
            if (storedOptionsMenu != null) { GameObject.Destroy(storedOptionsMenu); }
            storedOptionsMenu = new GameObject("OptionsMenu");
            storedOptionsMenu.transform.SetParent(storedGamePartsTransform);
            storedOptionsMenu.transform.localPosition = Vector3.zero;
            storedOptionsMenu.transform.localRotation = Quaternion.identity;
            storedOptionsMenu.transform.localScale = Vector3.one;
            GameObject button;
            TextMeshPro textMeshPro;
            button = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Card1",
                /*position*/ new Vector3(0.1228f * 2f, 0f, 0.8f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));
            button.transform.GetChild(2).localPosition = new Vector3(0, 0, -0.2f);
            textMeshPro = button.transform.GetChild(2).GetComponent<TextMeshPro>();
            textMeshPro.fontSize = 0.75f;
            textMeshPro.text = (removeCard1 ? "Remove" : "Keep");

            button = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Card2",
                /*position*/ new Vector3(0.1228f, 0f, 0.8f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));
            button.transform.GetChild(2).localPosition = new Vector3(0, 0, -0.2f);
            textMeshPro = button.transform.GetChild(2).GetComponent<TextMeshPro>();
            textMeshPro.fontSize = 0.75f;
            textMeshPro.text = (removeCard2 ? "Remove" : "Keep");

            button = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Card3",
                /*position*/ new Vector3(0f, 0f, 0.8f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));
            button.transform.GetChild(2).localPosition = new Vector3(0, 0, -0.2f);
            textMeshPro = button.transform.GetChild(2).GetComponent<TextMeshPro>();
            textMeshPro.fontSize = 0.75f;
            textMeshPro.text = (removeCard3 ? "Remove" : "Keep");

            button = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Card4",
                /*position*/ new Vector3(-0.1228f, 0f, 0.8f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));
            button.transform.GetChild(2).localPosition = new Vector3(0, 0, -0.2f);
            textMeshPro = button.transform.GetChild(2).GetComponent<TextMeshPro>();
            textMeshPro.fontSize = 0.75f;
            textMeshPro.text = (removeCard4 ? "Remove" : "Keep");

            button = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Card5",
                /*position*/ new Vector3(0.1228f * -2f, 0f, 0.8f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));
            button.transform.GetChild(2).localPosition = new Vector3(0, 0, -0.2f);
            textMeshPro = button.transform.GetChild(2).GetComponent<TextMeshPro>();
            textMeshPro.fontSize = 0.75f;
            textMeshPro.text = (removeCard5 ? "Remove" : "Keep");

            Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Continue",
                /*position*/ new Vector3(-0.43f, 0f, 0.86f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            Log("SetupOptionsMenu Completed", (bool)Main.debugging.SavedValue);
        }

        private static void UpdateBetAmountText()
        {
            betTextComponent.text = betAmount.ToString() + " of " + Main.GetPlayerCoinCount();
        }
    }
}
