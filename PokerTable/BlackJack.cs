using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using UnityEngine;

namespace PokerTable
{

    [RegisterTypeInIl2Cpp]
    public class BlackJack : MonoBehaviour
    {
        private static int betAmount = 1, betHeldAmount = 0;
        private static bool gameLoopRunning = false;
        private static TextMeshPro betTextComponent = null;
        private static GameObject storedBetsMenu = null, storedOptionsMenu = null;
        private static bool betAccepted = false, userQuits = false;
        private static List<int> deck, dealerHand;
        private static List<List<int>> hand;
        public static object gameLoopCoroutine = null;
        private static Transform storedGamePartsTransform = null, activeGamePartsTransform = null;

        public static void Log(string msg, bool sendMsg = true)
        {
            if (sendMsg)
            {
                Table.Log($"BlackJack - {msg}", sendMsg);
            }
        }

        public static void Warn(string msg, bool sendMsg = true)
        {
            if (sendMsg)
            {
                Table.Warn($"BlackJack - {msg}", sendMsg);
            }
        }

        public static void Error(string msg)
        {
            Table.Error($"BlackJack - {msg}");
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
                /*Title*/"BlackJack",
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
            cardSpots = GameObject.Instantiate(storedGamePartsTransform.GetChild(1).gameObject);
            cardSpots.name = "CardSpots";
            cardSpots.transform.SetParent(activeGamePartsTransform);
            cardSpots.transform.localPosition = Vector3.zero;
            cardSpots.transform.localRotation = Quaternion.identity;
            cardSpots.transform.localScale = Vector3.one;
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
                    Log("Not Enough Coins, Exiting BlackJack", (bool)Main.debugging.SavedValue);
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
                    betHeldAmount = betAmount;
                    coinsGained = 0;
                    //players turn
                    yield return MelonCoroutines.Start(PlayHands());
                    //dealers turn
                    yield return MelonCoroutines.Start(DealersTurn());
                    //check hands compared to dealer
                    List<int> playerHandTotals = GetPlayerHandTotal();
                    int dealerHandTotal = GetDealerHandTotal();
                    for (int i = playerHandTotals.Count - 1; i >= 0; i--)
                    {
                        //clear active cards
                        ClearVisualCardsFromAHand(true);
                        ClearVisualCardsFromAHand(false);
                        //spawn that hands cards
                        List<GameObject> handCards = SpawnHandCards(i);
                        List<GameObject> dealerCards = SpawnDealerCards();
                        yield return new WaitForSeconds(0.25f);
                        object endGameCoroutine0 = null;
                        object endGameCoroutine1 = null;
                        string cardString0 = "";
                        foreach (int card in hand[i])
                        {
                            cardString0 += Table.CardString[card] + " ";
                        }
                        string cardString1 = "";
                        foreach (int card in dealerHand)
                        {
                            cardString1 += Table.CardString[card] + " ";
                        }
                        Log($"Checking Hand {i}: Player {playerHandTotals[i]} ( {cardString0}) vs Dealer {dealerHandTotal} ( {cardString1})", true);
                        yield return new WaitForSeconds(0.5f);
                        //if blackjack
                        if (has21Immediately)
                        {
                            Log($"Player Got 21 Immediately! Gained {betAmount * 2} Coins", true);
                            coinsGained += Main.Payout(betAmount, 2f);
                            endGameCoroutine0 = MelonCoroutines.Start(PlayPlayerWin(handCards, dealerCards));
                        }
                        //player Bust
                        else if (playerHandTotals[i] > 21)
                        {
                            Log($"Player Bust, Lost {betAmount} Coins", true);
                            betHeldAmount -= betAmount;
                            endGameCoroutine0 = MelonCoroutines.Start(PlayPlayerBust(handCards));
                        }
                        //if player beat dealer or if dealer bust
                        else if ((playerHandTotals[i] > dealerHandTotal) || ((dealerHandTotal > 21)))
                        {
                            Log($"Player Beat Dealer! Gained {(int)(((float)betAmount) * 1.5f)} Coins", true);
                            coinsGained += Main.Payout(betAmount, 1.5f);
                            endGameCoroutine0 = MelonCoroutines.Start(PlayPlayerWin(handCards, dealerCards));
                        }
                        //if player didnt beat dealer
                        else
                        {
                            Log($"Player Didn't Beat Dealer, Lost {betAmount} Coins", true);
                            betHeldAmount -= betAmount;
                            endGameCoroutine0 = MelonCoroutines.Start(PlayDealerWin(handCards, dealerCards));
                        }
                        if (dealerHandTotal > 21)
                        {
                            endGameCoroutine1 = MelonCoroutines.Start(PlayDealerBust(dealerCards));
                        }
                        yield return endGameCoroutine0;
                        yield return endGameCoroutine1;
                        yield return new WaitForSeconds(0.25f);
                        //destroy the cards
                        foreach (GameObject handCard in handCards) { GameObject.Destroy(handCard); }
                        foreach (GameObject dealerCard in dealerCards) { GameObject.Destroy(dealerCard); }
                    }
                    Main.Payout(betHeldAmount, 1f);
                    bool continuePressed = false;
                    //load end game text
                    GameObject endGameText = Table.SpawnText(activeGamePartsTransform.transform,
                        /*Title*/$"Total Coins Gained: {coinsGained}",
                        /*position*/ new Vector3(0f, Table.TABLEHEIGHT - 0.025f, 0.5f),
                        /*rotation*/Quaternion.Euler(90, 180, 0),
                        /*scale*/ new Vector3(1f, 1f, 1f));
                    endGameText.name = "End Game Text";
                    TextMeshPro endGameTextTMP = endGameText.GetComponent<TextMeshPro>();
                    endGameTextTMP.alignment = TextAlignmentOptions.Center;
                    endGameTextTMP.enableWordWrapping = false;
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

        private static IEnumerator PlayPlayerWin(List<GameObject> handCards, List<GameObject> dealerCards)
        {
            Log("PlayPlayerWin Started", (bool)Main.debugging.SavedValue);
            List<object> cardsReacting = new List<object>();
            for (int i = 0; i < handCards.Count; i++)
            {
                cardsReacting.Add(PlayCardReact(handCards[i], true, true));
            }
            for (int i = 0; i < dealerCards.Count; i++)
            {
                cardsReacting.Add(PlayCardReact(dealerCards[i], false, false));
            }
            foreach (object card in cardsReacting)
            {
                yield return card;
            }
            Log("PlayPlayerWin Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static object PlayCardReact(GameObject card, bool win, bool isPlayer)
        {
            return MelonCoroutines.Start(PlayCardReactCoroutine(card, win, isPlayer));
        }

        private static IEnumerator PlayCardReactCoroutine(GameObject card, bool win, bool isPlayer)
        {
            Vector3 startingPostion = card.transform.localPosition;
            Vector3 movePerTick = ((isPlayer ? Vector3.right : Vector3.left) / 50f) * (win ? 0.5f : -0.25f);
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition += movePerTick;
                yield return new WaitForFixedUpdate();
            }
            for (int i = 0; i < 25; i++)
            {
                card.transform.localPosition -= movePerTick;
                yield return new WaitForFixedUpdate();
            }
            card.transform.localPosition = startingPostion;
        }

        private static IEnumerator PlayPlayerBust(List<GameObject> handCards)
        {
            Log("PlayPlayerBust Started", (bool)Main.debugging.SavedValue);
            List<object> cardsReacting = new List<object>();
            for (int i = 0; i < handCards.Count; i++)
            {
                cardsReacting.Add(MelonCoroutines.Start(PlayCardBustCoroutine(handCards[i])));
            }
            foreach (object card in cardsReacting)
            {
                yield return card;
            }
            Log("PlayPlayerBust Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator PlayDealerBust(List<GameObject> handCards)
        {
            Log("PlayDealerBust Started", (bool)Main.debugging.SavedValue);
            List<object> cardsReacting = new List<object>();
            for (int i = 0; i < handCards.Count; i++)
            {
                cardsReacting.Add(MelonCoroutines.Start(PlayCardBustCoroutine(handCards[i])));
            }
            foreach (object card in cardsReacting)
            {
                yield return card;
            }
            Log("PlayDealerBust Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator PlayCardBustCoroutine(GameObject card)
        {
            Log("PlayCardBustCoroutine Started", (bool)Main.debugging.SavedValue);
            Vector3 startingScale = card.transform.localScale;
            Vector3 scalePerTick = startingScale / 50f;
            for (int i = 0; i < 50; i++)
            {
                card.transform.localScale -= scalePerTick;
                yield return new WaitForFixedUpdate();
            }
            Log("PlayCardBustCoroutine Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator PlayDealerWin(List<GameObject> handCards, List<GameObject> dealerCards)
        {
            Log("PlayDealerWin Started", (bool)Main.debugging.SavedValue);
            List<object> cardsReacting = new List<object>();
            for (int i = 0; i < handCards.Count; i++)
            {
                cardsReacting.Add(PlayCardReact(handCards[i], false, true));
            }
            for (int i = 0; i < dealerCards.Count; i++)
            {
                cardsReacting.Add(PlayCardReact(dealerCards[i], true, false));
            }
            foreach (object card in cardsReacting)
            {
                yield return card;
            }
            Log("PlayDealerWin Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static List<GameObject> SpawnHandCards(int thisHand)
        {
            List<GameObject> handsCards = new List<GameObject>();
            for (int i = 0; i < hand[thisHand].Count; i++)
            {
                GameObject aCard = GameObject.Instantiate(Table.storedDeckOfCards.transform.GetChild(hand[thisHand][i]).gameObject);
                aCard.transform.SetParent(cardSpots.transform.GetChild(0).GetChild(i));
                aCard.transform.localPosition = Vector3.zero;
                aCard.transform.localRotation = Quaternion.identity;
                handsCards.Add(aCard);
            }
            return handsCards;
        }

        private static List<GameObject> SpawnDealerCards()
        {
            List<GameObject> dealerCards = new List<GameObject>();
            for (int i = 0; i < dealerHand.Count; i++)
            {
                GameObject aCard = GameObject.Instantiate(Table.storedDeckOfCards.transform.GetChild(dealerHand[i]).gameObject);
                aCard.transform.SetParent(cardSpots.transform.GetChild(1).GetChild(i));
                aCard.transform.localPosition = Vector3.zero;
                aCard.transform.localRotation = Quaternion.identity;
                dealerCards.Add(aCard);
            }
            return dealerCards;
        }

        private static IEnumerator RevealDealerCard()
        {
            Log("RevealDealerCard Started", (bool)Main.debugging.SavedValue);
            float rotationPerTick = 180f / 25f;
            Transform dealerCard = cardSpots.transform.GetChild(1).GetChild(1).GetChild(0);
            GameObject newCard = GameObject.Instantiate(Table.storedDeckOfCards.transform.GetChild(dealerHand[1]).gameObject);
            newCard.transform.SetParent(dealerCard.transform.parent);
            newCard.transform.position = dealerCard.transform.position;
            newCard.transform.rotation = dealerCard.transform.rotation;
            newCard.transform.localScale = dealerCard.transform.localScale;
            float currentRotation = newCard.transform.localRotation.eulerAngles.y;
            GameObject.Destroy(dealerCard.gameObject);
            for (int i = 0; i < 25; i++)
            {
                currentRotation -= rotationPerTick;
                newCard.transform.localRotation = Quaternion.Euler(newCard.transform.localRotation.x, currentRotation, newCard.transform.localRotation.z);
                yield return new WaitForFixedUpdate();
            }
            Log("Completed Rotation", (bool)Main.debugging.SavedValue);
            Log("RevealDealerCard Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static List<int> GetPlayerHandTotal()
        {
            Log("GetPlayerHandTotal Started", (bool)Main.debugging.SavedValue);
            List<int> hands = new List<int>();
            int total = 0;
            bool hasAce = false;
            foreach (List<int> i in hand)
            {
                foreach (int j in i)
                {
                    int card = j;
                    while (card >= 13)
                    {
                        card -= 13;
                    }
                    card = Math.Clamp(card + 1, 1, 10);
                    if (card == 1)
                    {
                        hasAce = true;
                    }
                    total += card;
                }
                if (hasAce && total <= 11)
                {
                    total += 10;
                }
                hands.Add(total);
                total = 0;
            }
            Log("GetPlayerHandTotal Completed", (bool)Main.debugging.SavedValue);
            return hands;
        }

        private static int GetDealerHandTotal()
        {
            Log("GetDealerHandTotal Started", (bool)Main.debugging.SavedValue);
            int total = 0;
            bool hasAce = false;
            foreach (int i in dealerHand)
            {
                int card = i;
                while (card >= 13)
                {
                    card -= 13;
                }
                card = Math.Clamp(card + 1, 1, 10);
                if (card == 1)
                {
                    hasAce = true;
                }
                total += card;
            }
            if (hasAce && total <= 11)
            {
                total += 10;
            }
            Log("GetDealerHandTotal Completed", (bool)Main.debugging.SavedValue);
            return total;
        }

        private static int GetCardsTotal(List<int> cards)
        {
            Log("GetCardsTotal Started", (bool)Main.debugging.SavedValue);
            int total = 0;
            bool hasAce = false;
            foreach (int i in cards)
            {
                int card = i;
                while (card >= 13)
                {
                    card -= 13;
                }
                card = Math.Clamp(card + 1, 1, 10);
                if (card == 1)
                {
                    hasAce = true;
                }
                total += card;
                Log($"Card {Table.CardString[i]}: {card}", (bool)Main.debugging.SavedValue);
            }
            if (hasAce && total <= 11)
            {
                total += 10;
                Log($"Ace able to be 11", (bool)Main.debugging.SavedValue);
            }
            Log("GetCardsTotal Completed: " + total, (bool)Main.debugging.SavedValue);
            return total;
        }

        private static bool has21Immediately;
        private static IEnumerator PlayHands()
        {
            Log("PlayHands Running", (bool)Main.debugging.SavedValue);
            hand = new List<List<int>>();
            dealerHand = new List<int>();
            has21Immediately = false;
            SetupCardSpots();
            int thisHand = hand.Count;
            hand.Add(new List<int>());
            hand[hand.Count - 1].Add(DrawCard());
            Log("Player Card 1: " + Table.CardString[hand[0][0]], (bool)Main.debugging.SavedValue);
            object playDrawAnimationCoroutine = PlayDrawCardAnimation(hand[hand.Count - 1][0], cardSpots.transform.GetChild(0).GetChild(0), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
            yield return playDrawAnimationCoroutine;

            dealerHand.Add(DrawCard());
            Log("Dealer Card 1: " + Table.CardString[dealerHand[0]], (bool)Main.debugging.SavedValue);
            object playDrawAnimationCoroutine2 = PlayDrawCardAnimation(dealerHand[0], cardSpots.transform.GetChild(1).GetChild(0), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
            yield return playDrawAnimationCoroutine2;

            hand[0].Add(DrawCard());
            Log("Player Card 2: " + Table.CardString[hand[0][1]], (bool)Main.debugging.SavedValue);
            object playDrawAnimationCoroutine3 = PlayDrawCardAnimation(hand[0][1], cardSpots.transform.GetChild(0).GetChild(1), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
            yield return playDrawAnimationCoroutine3;
            
            dealerHand.Add(DrawCard());
            Log("Dealer Card 2: -_-", (bool)Main.debugging.SavedValue);
            object playDrawAnimationCoroutine4 = PlayDrawCardAnimation(52, cardSpots.transform.GetChild(1).GetChild(1), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0), false);
            yield return playDrawAnimationCoroutine4;
            //starting hands have been dealt
            if (GetCardsTotal(hand[thisHand]) == 21)
            {
                has21Immediately = true;
            }
            else
            {
                //offer options
                object optionsMenuCoroutine = MelonCoroutines.Start(RunOptionsMenu(thisHand));
                yield return optionsMenuCoroutine;
            }
            Log("PlayHands Completed", (bool)Main.debugging.SavedValue);
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
            if (betAccepted)
            {
                betHeldAmount = betAmount;
                Log("Bet Accepted, Bet: " + betAmount, (bool)Main.debugging.SavedValue);
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

        private static bool pressedStay;
        private static bool pressedHit;
        private static bool pressedSplit;
        private static GameObject SpawnOptionsMenu(int thisHand)
        {
            Log("SpawnOptionsMenu Started", (bool)Main.debugging.SavedValue);
            pressedStay = false;
            pressedHit = false;
            pressedSplit = false;
            GameObject spawnedOptionsMenu = GameObject.Instantiate(storedOptionsMenu);
            spawnedOptionsMenu.transform.SetParent(activeGamePartsTransform);
            spawnedOptionsMenu.transform.localPosition = new Vector3(0, Table.TABLEHEIGHT - 0.052f, 0);
            spawnedOptionsMenu.transform.localRotation = Quaternion.identity;
            spawnedOptionsMenu.transform.localScale = Vector3.one;
            //Stay
            spawnedOptionsMenu.transform.GetChild(0).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Stay Pressed", (bool)Main.debugging.SavedValue);
                pressedStay = true;
            }));
            //Hit
            spawnedOptionsMenu.transform.GetChild(1).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                Log("Hit Pressed", (bool)Main.debugging.SavedValue);
                pressedHit = true;
            }));
            spawnedOptionsMenu.transform.GetChild(1).GetChild(2).localPosition = new Vector3(0f, 0f, -0.25f); //fixes weirdness about Text not being correct
            //Split
            if (SplitIsPossible(hand[thisHand]))
            {
                spawnedOptionsMenu.transform.GetChild(2).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((Action)(() => {
                    Log("Split Pressed", (bool)Main.debugging.SavedValue);
                    pressedSplit = true;
                }));
                spawnedOptionsMenu.transform.GetChild(2).GetChild(2).localPosition = new Vector3(0f, 0f, -0.25f); //fixes weirdness about Text not being correct
                spawnedOptionsMenu.transform.GetChild(2).gameObject.SetActive(true);
            }
            Log("SpawnOptionsMenu Completed", (bool)Main.debugging.SavedValue);
            return spawnedOptionsMenu;
        }

        //spawns optionsMenu menu and waits for an option to be pressed
        private static IEnumerator RunOptionsMenu(int thisHand)
        {
            Log("RunOptionsMenu Started", (bool)Main.debugging.SavedValue);
            GameObject optionsMenu = SpawnOptionsMenu(thisHand);
            Log("Spawned Options Menu, Waiting for Selection", (bool)Main.debugging.SavedValue);
            while (!pressedStay && !pressedHit && !pressedSplit)
            {
                yield return new WaitForFixedUpdate();
            }
            GameObject.Destroy(optionsMenu);
            if (pressedHit)
            {
                Log("Pressed Hit", (bool)Main.debugging.SavedValue);
                hand[thisHand].Add(DrawCard());
                //add drawing card visual
                Log($"Player Card {hand[thisHand].Count}: {Table.CardString[hand[thisHand][hand[thisHand].Count - 1]]}", (bool)Main.debugging.SavedValue);
                object playDrawAnimationCoroutine = PlayDrawCardAnimation(hand[thisHand][hand[thisHand].Count - 1], cardSpots.transform.GetChild(0).GetChild(hand[thisHand].Count - 1), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
                yield return playDrawAnimationCoroutine;
                if (GetCardsTotal(hand[thisHand]) < 21)
                {
                    object optionsInOptionsMenuCoroutine = MelonCoroutines.Start(RunOptionsMenu(thisHand));
                    yield return optionsInOptionsMenuCoroutine;
                }
                else
                {
                    if (GetCardsTotal(hand[thisHand]) > 21) { Log("Player Bust, Total: " + GetCardsTotal(hand[thisHand]), (bool)Main.debugging.SavedValue); }
                    else { Log("Player Got 21, Stopping Hand", (bool)Main.debugging.SavedValue); }
                }
            }
            else if (pressedStay)
            {
                Log("Pressed Stay", (bool)Main.debugging.SavedValue);
                //all hands need to conclude first
            }
            else if (pressedSplit)
            {
                Log("Pressed Split", (bool)Main.debugging.SavedValue);
                Log("Player Split Bet Accepted: " + betAmount, true);
                Main.Payout(-betAmount, 1f);
                betHeldAmount += betAmount;
                //split 1 hand in 2, then play options for each hand
                List<int> secondHand = new List<int>();
                secondHand.Add(-1);
                secondHand.Add(hand[thisHand][1]);
                hand[thisHand][1] = -1;
                string cardsString = "";
                foreach (int i in hand[thisHand])
                {
                    if (i != -1)
                    {
                        cardsString += Table.CardString[i];
                    }
                    else
                    {
                        cardsString += "-1";
                    }
                    cardsString += " ";
                }
                Log($"Hand {thisHand}: {cardsString}", (bool)Main.debugging.SavedValue);
                cardsString = "";
                foreach (int i in secondHand)
                {
                    if (i != -1)
                    {
                        cardsString += Table.CardString[i];
                    }
                    else
                    {
                        cardsString += "-1";
                    }
                    cardsString += " ";
                }
                Log($"Next Hand: {cardsString}", (bool)Main.debugging.SavedValue);
                ClearVisualCardsFromAHand(true);
                //hand 1
                Log($"Playing First Hand", (bool)Main.debugging.SavedValue);
                object drawSplitCoroutine = MelonCoroutines.Start(DrawSplit(thisHand));
                yield return drawSplitCoroutine;
                if (GetCardsTotal(hand[thisHand]) <= 21)
                {
                    object runOptionsMenuCoroutine = MelonCoroutines.Start(RunOptionsMenu(thisHand));
                    yield return runOptionsMenuCoroutine;
                }
                else
                {
                    Log("Player Bust, Total: " + GetCardsTotal(hand[thisHand]), (bool)Main.debugging.SavedValue);
                }
                yield return new WaitForSeconds(1f);
                //setup for next hand
                Log($"Setting Up For Next Hand", (bool)Main.debugging.SavedValue);
                ClearVisualCardsFromAHand(true);
                hand.Add(secondHand);
                int secondHandSpot = hand.Count - 1;
                //next hand
                Log($"Playing Next Hand", (bool)Main.debugging.SavedValue);
                drawSplitCoroutine = MelonCoroutines.Start(DrawSplit(secondHandSpot, true));
                yield return drawSplitCoroutine;
                if (GetCardsTotal(hand[secondHandSpot]) <= 21)
                {
                    object runOptionsMenuCoroutine = MelonCoroutines.Start(RunOptionsMenu(secondHandSpot));
                    yield return runOptionsMenuCoroutine;
                }
                yield return new WaitForSeconds(1f);
            }
            Log("RunOptionsMenu Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static void ClearVisualCardsFromAHand(bool isPlayer)
        {
            Log($"Clearing {(isPlayer ? "Player's" : "Dealer's")} Cards", (bool)Main.debugging.SavedValue);
            int spot = isPlayer ? 0 : 1;
            for (int i = 0; i < cardSpots.transform.GetChild(spot).GetChildCount(); i++)
            {
                for (int x = cardSpots.transform.GetChild(spot).GetChild(i).childCount - 1; x >= 0; x--)
                {
                    GameObject.Destroy(cardSpots.transform.GetChild(spot).GetChild(i).GetChild(x).gameObject);
                }
            }
        }

        private static IEnumerator DrawSplit(int thisHand, bool recreateOriginal = false)
        {
            bool drawFirstSpot = false;
            if (hand[thisHand][0] == -1)
            {
                hand[thisHand][0] = DrawCard();
                drawFirstSpot = true;
            }
            else
            {
                PlayDrawCardAnimation(hand[thisHand][0], cardSpots.transform.GetChild(0).GetChild(0), cardSpots.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 0, 0), false);
            }
            if (hand[thisHand][1] == -1)
            {
                hand[thisHand][1] = DrawCard();
                drawFirstSpot = false;
            }
            else
            {
                PlayDrawCardAnimation(hand[thisHand][1], cardSpots.transform.GetChild(0).GetChild(1), cardSpots.transform.GetChild(0).GetChild(1).position, Quaternion.Euler(0, 0, 0), false);
            }
            object playDrawAnimationCoroutine = PlayDrawCardAnimation(hand[thisHand][drawFirstSpot ? 0 : 1], cardSpots.transform.GetChild(0).GetChild(drawFirstSpot ? 0 : 1), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
            yield return playDrawAnimationCoroutine;
            yield break;
        }

        private static IEnumerator DealersTurn()
        {
            Log("DealersTurn Started", (bool)Main.debugging.SavedValue);
            //flip card
            object revealDealerCardCoroutine = MelonCoroutines.Start(RevealDealerCard());
            yield return revealDealerCardCoroutine;
            //check total
            bool hasAce = false;
            bool aceIs11 = false;
            int dealerTotal = GetDealerHandTotal();
            bool hasSoft17 = (dealerTotal == 17) && hasAce && aceIs11;
            //hit on 16 or below or soft 17 (has ace as 11)
            while ((dealerTotal < 17) || hasSoft17)
            {
                object dealerDrawCorooutine = MelonCoroutines.Start(DealerDraw());
                yield return dealerDrawCorooutine;
                dealerTotal = GetDealerHandTotal();
                hasSoft17 = (dealerTotal == 17) && hasAce && aceIs11;
                yield return new WaitForSeconds(0.25f);
            }
            Log("DealersTurn Completed", (bool)Main.debugging.SavedValue);
            yield break;
        }

        private static IEnumerator DealerDraw()
        {
            dealerHand.Add(DrawCard());
            Log($"Dealer Card {dealerHand.Count}: " + Table.CardString[dealerHand[dealerHand.Count - 1]], (bool)Main.debugging.SavedValue);
            object playDrawAnimationCoroutine = PlayDrawCardAnimation(dealerHand[dealerHand.Count - 1], cardSpots.transform.GetChild(1).GetChild(dealerHand.Count - 1), Table.dealerDeck.transform.GetChild(0).GetChild(0).position, Quaternion.Euler(0, 180, 0));
            yield return playDrawAnimationCoroutine;
            yield break;
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

            GameObject stayButton = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Stay",
                /*position*/ new Vector3(0.25f, 0f, 0.9f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            GameObject hitButton = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Hit",
                /*position*/ new Vector3(-0.25f, 0f, 0.9f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));

            GameObject SplitButton = Table.SpawnButton(storedOptionsMenu.transform,
                /*Title*/"Split",
                /*position*/ new Vector3(0f, 0f, 0.95f),
                /*rotation*/Quaternion.Euler(0, 0, 0),
                /*scale*/ new Vector3(0.5f, 0.5f, 0.5f));
            SplitButton.SetActive(false);
            Log("SetupOptionsMenu Completed", (bool)Main.debugging.SavedValue);
        }

        private static bool SplitIsPossible(List<int> thisHand)
        {
            int card1 = thisHand[0];
            int card2 = thisHand[1];
            while (card1 >= 13) { card1 -= 13; }
            while (card2 >= 13) { card2 -= 13; }
            Log("Is Split Possible: " + ((card1 == card2) && (Table.freePlay || (Main.GetPlayerCoinCount() >= (betAmount + betHeldAmount)))), (bool)Main.debugging.SavedValue);
            return ((card1 == card2) && (Main.GetPlayerCoinCount() >= (betAmount + betHeldAmount)));
        }

        private static void UpdateBetAmountText()
        {
            betTextComponent.text = betAmount.ToString() + " of " + Main.GetPlayerCoinCount();
        }
    }
}
