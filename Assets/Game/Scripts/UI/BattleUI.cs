using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace COYGame
{
    public sealed class BattleUI : MonoBehaviour
    {
        [Header("Top")]
        public TMP_Text phaseText;
        public TMP_Text roundText;
        public TMP_Text scoreText;
        public TMP_Text logText;

        [Header("Hoops")]
        public TMP_Text playerHoopText;
        public TMP_Text enemyHoopText;
        public Image playerHpFill;
        public Image playerShieldFill;
        public Image enemyHpFill;
        public Image enemyShieldFill;

        [Header("Cards")]
        public TMP_Text apText;
        public TMP_Text deckText;
        public TMP_Text discardText;
        public RectTransform handRoot;
        public RectTransform dragLayer;
        public RectTransform playArea;
        public RectTransform cardPreview;
        public TMP_Text previewTitle;
        public TMP_Text previewBody;
        public TMP_Text previewCost;
        public CardView cardViewPrefab;
        public BattleController controller;

        [Header("Buttons")]
        public Button confirmButton;
        public Button twoPointButton;
        public Button threePointButton;
        public GameObject strategyPanel;
        public GameObject modalPanel;
        public TMP_Text modalText;
        public Button modalButton;

        public Transform DragLayer => dragLayer;

        private void Awake()
        {
            DisablePreviewRaycasts();
        }

        public void BindController(BattleController owner)
        {
            controller = owner;
        }

        public void BindButtons(UnityAction onConfirm, UnityAction onTwoPoint, UnityAction onThreePoint, UnityAction onModal)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(onConfirm);
            twoPointButton.onClick.RemoveAllListeners();
            twoPointButton.onClick.AddListener(onTwoPoint);
            threePointButton.onClick.RemoveAllListeners();
            threePointButton.onClick.AddListener(onThreePoint);
            modalButton.onClick.RemoveAllListeners();
            modalButton.onClick.AddListener(onModal);
        }

        public bool TryPlayDraggedCard(CardView cardView, Vector2 screenPosition)
        {
            EnsurePlayArea();
            if (playArea == null || controller == null)
            {
                return false;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(playArea, screenPosition, null))
            {
                return false;
            }

            controller.TryPlayPlayerCard(cardView);
            return true;
        }

        public void RenderHand(IReadOnlyList<CardRuntime> hand, int currentAp)
        {
            EnsureCardPrefab();
            for (var i = handRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(handRoot.GetChild(i).gameObject);
            }

            foreach (var card in hand)
            {
                var view = Instantiate(cardViewPrefab, handRoot);
                view.Bind(card, this);
                view.Refresh(card.Data.apCost <= currentAp);
            }
        }

        public void RefreshHandInteractivity(int currentAp)
        {
            foreach (Transform child in handRoot)
            {
                var view = child.GetComponent<CardView>();
                if (view != null)
                {
                    view.Refresh(view.Card.Data.apCost <= currentAp);
                }
            }
        }

        public void SetStrategyVisible(bool visible)
        {
            strategyPanel.SetActive(visible);
        }

        public void ShowModal(string message)
        {
            HideCardPreview();
            modalText.text = message;
            modalPanel.SetActive(true);
        }

        public void HideModal()
        {
            modalPanel.SetActive(false);
        }

        public void ShowCardPreview(CardRuntime card)
        {
            previewTitle.text = card.Data.cardName;
            previewBody.text = $"{card.Owner.playerName}\n{card.Data.rulesText}";
            previewCost.text = $"{card.Data.apCost} AP";
            cardPreview.gameObject.SetActive(true);
        }

        public void HideCardPreview()
        {
            if (cardPreview != null)
            {
                cardPreview.gameObject.SetActive(false);
            }
        }

        public void SetLog(string message)
        {
            logText.text = message;
        }

        public void Refresh(BattleController owner)
        {
            phaseText.text = owner.PhaseLabel;
            roundText.text = $"Round\n{owner.Round}";
            scoreText.text = $"{owner.PlayerTeam.Score}   {owner.EnemyTeam.Score}";
            apText.text = $"AP {owner.CurrentAp}/{owner.MaxApThisPhase}";
            deckText.text = $"Deck\n{owner.ActiveDeckCount}";
            discardText.text = $"Discard\n{owner.ActiveDiscardCount}";

            RefreshHoop(owner.PlayerHoop, playerHoopText, playerHpFill, playerShieldFill);
            RefreshHoop(owner.EnemyHoop, enemyHoopText, enemyHpFill, enemyShieldFill);
            RefreshHandInteractivity(owner.CurrentAp);
        }

        private void EnsureCardPrefab()
        {
            if (cardViewPrefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            cardViewPrefab = AssetDatabase.LoadAssetAtPath<CardView>("Assets/Game/UI/CardView.prefab");
#endif
            if (cardViewPrefab == null)
            {
                Debug.LogError("BattleUI.cardViewPrefab is missing. Run Tools > COY > Build MVP Scene again.");
            }
        }

        private void EnsurePlayArea()
        {
            if (playArea != null)
            {
                return;
            }

            var court = GameObject.Find("Court");
            if (court != null)
            {
                playArea = court.GetComponent<RectTransform>();
            }
        }

        private void DisablePreviewRaycasts()
        {
            if (cardPreview == null)
            {
                return;
            }

            foreach (var graphic in cardPreview.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private static void RefreshHoop(HoopState hoop, TMP_Text text, Image hpFill, Image shieldFill)
        {
            text.text = $"{hoop.Hp}/{hoop.BaseHp}\nShield {hoop.Shield}";
            var max = Mathf.Max(1, hoop.MaxTarget);
            hpFill.fillAmount = Mathf.Clamp01((float)hoop.Hp / max);
            shieldFill.fillAmount = Mathf.Clamp01((float)hoop.Shield / max);
        }
    }
}
