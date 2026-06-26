using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Rules")]
        [SerializeField] private int maxRounds = 6;
        [SerializeField] private int baseHoopHp = 150;
        [SerializeField] private int baseAp = 4;
        [SerializeField] private int baseDrawCount = 4;
        [SerializeField] private float threePointMultiplier = 1.2f;
        [SerializeField] private float enemyCardDelay = 0.5f;
        [SerializeField] private CardData reboundCard = null;

        [Header("Teams")]
        [SerializeField] private List<PlayerData> playerRoster = new();
        [SerializeField] private List<PlayerData> enemyRoster = new();

        [Header("UI")]
        [SerializeField] private BattleUI ui = null;

        private readonly EnemyAI enemyAI = new();
        private TurnContext context;
        private BattlePhase phase = BattlePhase.None;
        private int maxApThisPhase;
        private bool waitingForModal;

        public TeamRuntime PlayerTeam { get; private set; }
        public TeamRuntime EnemyTeam { get; private set; }
        public HoopState PlayerHoop { get; } = new();
        public HoopState EnemyHoop { get; } = new();
        public int Round { get; private set; } = 1;
        public int CurrentAp => context?.Ap ?? 0;
        public int MaxApThisPhase => context?.MaxAp ?? maxApThisPhase;
        public int ActiveDeckCount => GetActiveDeck()?.DeckCount ?? 0;
        public int ActiveDiscardCount => GetActiveDeck()?.DiscardCount ?? 0;
        public bool PlayerHoopDimmed => phase is BattlePhase.PlayerChooseStrategy or BattlePhase.PlayerAttack or BattlePhase.PlayerAttackResult or BattlePhase.EnemyDefense;
        public bool EnemyHoopDimmed => phase is BattlePhase.PlayerDefense or BattlePhase.EnemyChooseStrategy or BattlePhase.EnemyAttack or BattlePhase.EnemyAttackResult;

        public string PhaseLabel => phase switch
        {
            BattlePhase.EnemyDefense => "Enemy Turn",
            BattlePhase.PlayerChooseStrategy => "Player Turn",
            BattlePhase.PlayerAttack => "Player Turn",
            BattlePhase.PlayerDefense => "Player Turn",
            BattlePhase.EnemyAttack => "Enemy Turn",
            BattlePhase.BattleOver => "Game Over",
            _ => "COY"
        };

        private void Awake()
        {
            ui.BindController(this);
            ui.BindButtons(OnConfirmPressed, () => ChoosePlayerStrategy(ScoreStrategy.TwoPoint), () => ChoosePlayerStrategy(ScoreStrategy.ThreePoint), OnModalConfirmed);
        }

        private void Start()
        {
            StartBattle();
        }

        public void StartBattle()
        {
            PlayerTeam = new TeamRuntime(TeamSide.Player, playerRoster, Random.Range(1, 999999));
            EnemyTeam = new TeamRuntime(TeamSide.Enemy, enemyRoster, Random.Range(1, 999999));
            Round = 1;
            PlayerHoop.Reset(baseHoopHp);
            EnemyHoop.Reset(baseHoopHp);
            ui.SetStrategyVisible(false);
            ui.HideModal();
            ui.HideCardPreview();
            StartCoroutine(PlayerOffenseSequence());
        }

        public void TryPlayPlayerCard(CardView cardView)
        {
            if (context == null || phase is not (BattlePhase.PlayerAttack or BattlePhase.PlayerDefense))
            {
                ui.HideCardPreview();
                cardView.ReturnToHand();
                return;
            }

            var card = cardView.Card;
            if (!context.Hand.Contains(card))
            {
                ui.HideCardPreview();
                cardView.ReturnToHand();
                return;
            }

            if (card.CurrentCost > context.Ap)
            {
                ui.SetLog("Not enough AP");
                ui.HideCardPreview();
                cardView.ReturnToHand();
                return;
            }

            PlayCard(card);
            cardView.MarkPlayed();
            ui.HideCardPreview();
            if (TryEndPlayerAttackOnScore())
            {
                return;
            }

            RefreshAll();
        }

        private IEnumerator PlayerOffenseSequence()
        {
            EnemyHoop.Reset(baseHoopHp);
            yield return RunEnemyDefense();
            phase = BattlePhase.PlayerChooseStrategy;
            ui.SetStrategyVisible(true);
            ui.SetLog("Choose your attack plan");
            RefreshAll();
        }

        private IEnumerator RunEnemyDefense()
        {
            phase = BattlePhase.EnemyDefense;
            context = CreateContext(EnemyTeam, PlayerTeam, EnemyHoop, EnemyTeam.DefenseDeck);
            ui.RenderHand(context.Hand, 0);
            RefreshAll();

            var cards = enemyAI.ChooseCards(context);
            foreach (var card in cards)
            {
                yield return new WaitForSeconds(enemyCardDelay);
                PlayCard(card);
                RefreshAll();
            }

            ResolvePhaseEndTriggers();
            DiscardRemainingHand();
        }

        private void ChoosePlayerStrategy(ScoreStrategy strategy)
        {
            if (phase != BattlePhase.PlayerChooseStrategy)
            {
                return;
            }

            ui.SetStrategyVisible(false);
            context = CreateContext(PlayerTeam, EnemyTeam, EnemyHoop, PlayerTeam.AttackDeck);
            context.Phase = BattlePhase.PlayerAttack;
            context.Strategy = strategy;
            phase = BattlePhase.PlayerAttack;

            if (strategy == ScoreStrategy.ThreePoint)
            {
                EnemyHoop.ApplyThreePointPressure(threePointMultiplier);
            }

            ui.RenderHand(context.Hand, context.Ap);
            ui.SetLog($"Player chose a {(int)strategy}-point attack");
            RefreshAll();
        }

        private void OnConfirmPressed()
        {
            if (waitingForModal)
            {
                return;
            }

            if (phase == BattlePhase.PlayerAttack)
            {
                ResolvePhaseEndTriggers();
                DiscardRemainingHand();
                var scored = EnemyHoop.IsBroken;
                if (scored)
                {
                    PlayerTeam.Score += (int)context.Strategy;
                }
                else
                {
                    EnemyTeam.PendingReboundCards++;
                }

                phase = BattlePhase.PlayerAttackResult;
                waitingForModal = true;
                ui.ShowModal(scored ? $"Player scored {(int)context.Strategy} points!" : "Player did not score");
                RefreshAll();
                return;
            }

            if (phase == BattlePhase.PlayerDefense)
            {
                ResolvePhaseEndTriggers();
                DiscardRemainingHand();
                StartCoroutine(EnemyAttackSequence());
            }
        }

        private void OnModalConfirmed()
        {
            ui.HideModal();
            waitingForModal = false;

            if (phase == BattlePhase.PlayerAttackResult)
            {
                StartPlayerDefense();
                return;
            }

            if (phase == BattlePhase.EnemyAttackResult)
            {
                if (Round >= maxRounds)
                {
                    EndBattle();
                }
                else
                {
                    Round++;
                    StartCoroutine(PlayerOffenseSequence());
                }
            }
        }

        private void StartPlayerDefense()
        {
            PlayerHoop.Reset(baseHoopHp);
            phase = BattlePhase.PlayerDefense;
            context = CreateContext(PlayerTeam, EnemyTeam, PlayerHoop, PlayerTeam.DefenseDeck);
            ui.RenderHand(context.Hand, context.Ap);
            ui.SetLog("Defense: play defense cards, then press OK");
            RefreshAll();
        }

        private IEnumerator EnemyAttackSequence()
        {
            phase = BattlePhase.EnemyChooseStrategy;
            var strategy = enemyAI.ChooseStrategy(Round, EnemyTeam, PlayerTeam);
            context = CreateContext(EnemyTeam, PlayerTeam, PlayerHoop, EnemyTeam.AttackDeck);
            context.Phase = BattlePhase.EnemyAttack;
            context.Strategy = strategy;
            phase = BattlePhase.EnemyAttack;

            if (strategy == ScoreStrategy.ThreePoint)
            {
                PlayerHoop.ApplyThreePointPressure(threePointMultiplier);
            }

            ui.RenderHand(context.Hand, 0);
            ui.SetLog($"Enemy chose a {(int)strategy}-point attack");
            RefreshAll();

            var cards = enemyAI.ChooseCards(context);
            foreach (var card in cards)
            {
                yield return new WaitForSeconds(enemyCardDelay);
                PlayCard(card);
                RefreshAll();
                if (PlayerHoop.IsBroken)
                {
                    break;
                }
            }

            ResolvePhaseEndTriggers();
            EndEnemyAttackResult(PlayerHoop.IsBroken);
        }

        private bool TryEndPlayerAttackOnScore()
        {
            if (phase != BattlePhase.PlayerAttack || !EnemyHoop.IsBroken)
            {
                return false;
            }

            ResolvePhaseEndTriggers();
            DiscardRemainingHand();
            PlayerTeam.Score += (int)context.Strategy;
            phase = BattlePhase.PlayerAttackResult;
            waitingForModal = true;
            ui.ShowModal($"Player scored {(int)context.Strategy} points!");
            RefreshAll();
            return true;
        }

        private void EndEnemyAttackResult(bool scored)
        {
            DiscardRemainingHand();
            if (scored)
            {
                EnemyTeam.Score += (int)context.Strategy;
            }
            else
            {
                PlayerTeam.PendingReboundCards++;
            }

            phase = BattlePhase.EnemyAttackResult;
            waitingForModal = true;
            ui.ShowModal(scored ? $"Enemy scored {(int)context.Strategy} points!" : "Enemy did not score");
            RefreshAll();
        }

        private void EndBattle()
        {
            phase = BattlePhase.BattleOver;
            var result = PlayerTeam.Score == EnemyTeam.Score
                ? "Draw"
                : PlayerTeam.Score > EnemyTeam.Score ? "Player wins!" : "Enemy wins";
            ui.ShowModal($"{result}\nFinal score {PlayerTeam.Score} : {EnemyTeam.Score}");
            RefreshAll();
        }

        private TurnContext CreateContext(TeamRuntime acting, TeamRuntime opposing, HoopState targetHoop, DeckRuntime deck)
        {
            var ap = Mathf.Max(0, baseAp + acting.NextTurnApModifier);
            acting.NextTurnApModifier = 0;
            maxApThisPhase = ap;
            var drawCount = Mathf.Max(0, baseDrawCount + acting.NextTurnDrawModifier);
            acting.NextTurnDrawModifier = 0;
            var hand = new List<CardRuntime>();
            deck.ReleaseReservedToHand(hand, card => IsCardEligibleForDeck(card, acting, deck));
            var drawn = deck.Draw(drawCount);
            hand.AddRange(drawn);
            if (deck == acting.AttackDeck)
            {
                AddPendingReboundCards(acting, hand);
            }
            var createdContext = new TurnContext
            {
                Phase = phase,
                ActingTeam = acting,
                OpposingTeam = opposing,
                TargetHoop = targetHoop,
                Ap = ap,
                MaxAp = ap,
                DrawCount = drawCount,
                Deck = deck,
                Hand = hand
            };
            createdContext.ResolveCardTriggers = (cards, trigger) => ResolveTriggersForCards(cards, createdContext, trigger);
            ResolveTriggersForCards(drawn, createdContext, CardTrigger.OnDraw);
            ResolvePhaseStartTriggers(createdContext);
            return createdContext;
        }

        private bool IsCardEligibleForDeck(CardRuntime card, TeamRuntime acting, DeckRuntime deck)
        {
            if (card == null)
            {
                return false;
            }

            if (card.Data.cardType == CardType.Universal)
            {
                return true;
            }

            if (card.Data.cardType == CardType.Item)
            {
                return card.Data.usablePhase == BattlePhase.None
                    || deck == acting.AttackDeck && card.Data.usablePhase is BattlePhase.PlayerAttack or BattlePhase.EnemyAttack
                    || deck == acting.DefenseDeck && card.Data.usablePhase is BattlePhase.PlayerDefense or BattlePhase.EnemyDefense;
            }

            return deck == acting.AttackDeck && card.Data.cardType == CardType.Attack
                || deck == acting.DefenseDeck && card.Data.cardType == CardType.Defense;
        }

        private void AddPendingReboundCards(TeamRuntime acting, List<CardRuntime> hand)
        {
            if (reboundCard == null || acting.PendingReboundCards <= 0)
            {
                return;
            }

            for (var i = 0; i < acting.PendingReboundCards; i++)
            {
                hand.Add(new CardRuntime(reboundCard, null));
            }

            acting.PendingReboundCards = 0;
        }

        private void PlayCard(CardRuntime card)
        {
            var message = ResolvePlayedCard(card, true);
            ui.SetLog(message);
            ui.RenderHand(context.Hand, phase is BattlePhase.PlayerAttack or BattlePhase.PlayerDefense ? context.Ap : 0);
        }

        private string ResolvePlayedCard(CardRuntime card, bool consumeAp)
        {
            if (consumeAp)
            {
                context.Ap -= card.CurrentCost;
            }

            context.Hand.Remove(card);
            card.WasPlayed = true;
            var messages = new List<string> { CardEffectResolver.Resolve(card, context) };
            if (card.IsCombo)
            {
                messages.Add(ResolveComboFollowUp(card));
            }

            if (card.IsRecycle && !card.IsExhaust && !card.IsOnce && card.TryConsumeRecycle())
            {
                context.Hand.Add(card);
                messages.Add($"{card.Data.cardName}: recycled to hand ({card.RemainingRecycleCount} left)");
            }
            else
            {
                messages.Add(CardEffectResolver.Resolve(card, context, CardTrigger.OnDiscard));
                GetActiveDeck()?.DiscardCard(card);
            }

            return JoinMessages(messages);
        }

        private string ResolveComboFollowUp(CardRuntime sourceCard)
        {
            var candidates = context.Hand.FindAll(card => card != sourceCard && card.IsCombo);
            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            var nextCard = candidates[Random.Range(0, candidates.Count)];
            return $"{sourceCard.Data.cardName}: combo plays {nextCard.Data.cardName}\n{ResolvePlayedCard(nextCard, false)}";
        }

        private void DiscardRemainingHand()
        {
            var deck = GetActiveDeck();
            if (deck != null && context != null)
            {
                var cards = new List<CardRuntime>(context.Hand);
                foreach (var card in cards)
                {
                    CardEffectResolver.Resolve(card, context, CardTrigger.OnDiscard);
                    deck.DiscardCard(card);
                }

                context.Hand.Clear();
            }

            ui.RenderHand(context?.Hand ?? new List<CardRuntime>(), 0);
        }

        private void ResolvePhaseStartTriggers(TurnContext targetContext)
        {
            if (targetContext == null)
            {
                return;
            }

            var messages = new List<string>
            {
                ResolveTriggersForCards(targetContext.Hand, targetContext, CardTrigger.OnAnyOwnPhaseStart),
                ResolveTriggersForCards(targetContext.Hand, targetContext, CardTrigger.OnEveryOwnPhaseStart),
                ResolveTriggersForCards(targetContext.Hand, targetContext, CardTrigger.OnNextOwnPhaseStart)
            };

            if (IsAttackPhase(targetContext.Phase))
            {
                messages.Add(ResolveTriggersForCards(targetContext.Hand, targetContext, CardTrigger.OnOwnAttackPhaseStart));
            }
            else if (IsDefensePhase(targetContext.Phase))
            {
                messages.Add(ResolveTriggersForCards(targetContext.Hand, targetContext, CardTrigger.OnOwnDefensePhaseStart));
            }

            var message = JoinMessages(messages);
            if (!string.IsNullOrWhiteSpace(message))
            {
                ui.SetLog(message);
            }
        }

        private void ResolvePhaseEndTriggers()
        {
            if (context == null)
            {
                return;
            }

            var messages = new List<string>
            {
                ResolveTriggersForCards(context.Hand, context, CardTrigger.OnAnyOwnPhaseEnd),
                ResolveTriggersForCards(context.Hand, context, CardTrigger.OnPhaseEnd)
            };

            if (IsAttackPhase(context.Phase))
            {
                messages.Add(ResolveTriggersForCards(context.Hand, context, CardTrigger.OnOwnAttackPhaseEnd));
            }
            else if (IsDefensePhase(context.Phase))
            {
                messages.Add(ResolveTriggersForCards(context.Hand, context, CardTrigger.OnOwnDefensePhaseEnd));
            }

            var message = JoinMessages(messages);
            if (!string.IsNullOrWhiteSpace(message))
            {
                ui.SetLog(message);
            }
        }

        private static bool IsAttackPhase(BattlePhase targetPhase)
        {
            return targetPhase is BattlePhase.PlayerAttack or BattlePhase.EnemyAttack;
        }

        private static bool IsDefensePhase(BattlePhase targetPhase)
        {
            return targetPhase is BattlePhase.PlayerDefense or BattlePhase.EnemyDefense;
        }

        private static string ResolveTriggersForCards(IReadOnlyList<CardRuntime> cards, TurnContext targetContext, CardTrigger trigger)
        {
            if (cards == null || targetContext == null)
            {
                return string.Empty;
            }

            var messages = new List<string>();
            foreach (var card in new List<CardRuntime>(cards))
            {
                var message = CardEffectResolver.Resolve(card, targetContext, trigger);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    messages.Add(message);
                }
            }

            return JoinMessages(messages);
        }

        private static string JoinMessages(params string[] messages)
        {
            return JoinMessages((IEnumerable<string>)messages);
        }

        private static string JoinMessages(IEnumerable<string> messages)
        {
            var results = new List<string>();
            foreach (var message in messages)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    results.Add(message);
                }
            }

            return string.Join("\n", results);
        }

        private DeckRuntime GetActiveDeck()
        {
            if (context == null)
            {
                return null;
            }

            return context.Phase is BattlePhase.PlayerAttack or BattlePhase.EnemyAttack
                ? context.ActingTeam.AttackDeck
                : context.ActingTeam.DefenseDeck;
        }

        private void RefreshAll()
        {
            ui.Refresh(this);
        }
    }
}
