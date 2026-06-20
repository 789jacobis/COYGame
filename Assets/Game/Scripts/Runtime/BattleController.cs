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

            if (card.Data.apCost > context.Ap)
            {
                ui.SetLog("Not enough AP");
                ui.HideCardPreview();
                cardView.ReturnToHand();
                return;
            }

            PlayCard(card);
            cardView.MarkPlayed();
            ui.HideCardPreview();
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
                DiscardRemainingHand();
                var scored = EnemyHoop.IsBroken;
                if (scored)
                {
                    PlayerTeam.Score += (int)context.Strategy;
                }

                phase = BattlePhase.PlayerAttackResult;
                waitingForModal = true;
                ui.ShowModal(scored ? $"Player scored {(int)context.Strategy} points!" : "Player did not score");
                RefreshAll();
                return;
            }

            if (phase == BattlePhase.PlayerDefense)
            {
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
            }

            DiscardRemainingHand();
            var scored = PlayerHoop.IsBroken;
            if (scored)
            {
                EnemyTeam.Score += (int)context.Strategy;
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
            return new TurnContext
            {
                Phase = phase,
                ActingTeam = acting,
                OpposingTeam = opposing,
                TargetHoop = targetHoop,
                Ap = ap,
                MaxAp = ap,
                DrawCount = drawCount,
                Hand = deck.Draw(drawCount)
            };
        }

        private void PlayCard(CardRuntime card)
        {
            context.Ap -= card.Data.apCost;
            context.Hand.Remove(card);
            card.WasPlayed = true;
            var message = CardEffectResolver.Resolve(card, context);
            GetActiveDeck()?.DiscardCard(card);
            ui.SetLog(message);
        }

        private void DiscardRemainingHand()
        {
            var deck = GetActiveDeck();
            if (deck != null && context != null)
            {
                deck.DiscardMany(context.Hand);
                context.Hand.Clear();
            }

            ui.RenderHand(context?.Hand ?? new List<CardRuntime>(), 0);
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
