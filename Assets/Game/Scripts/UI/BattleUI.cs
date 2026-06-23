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
            ConfigureRuntimeLayout();
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

            if (!IsMiddleScreenBand(screenPosition))
            {
                return false;
            }

            controller.TryPlayPlayerCard(cardView);
            return true;
        }

        private static bool IsMiddleScreenBand(Vector2 screenPosition)
        {
            var bottom = Screen.height / 3f;
            var top = Screen.height * 2f / 3f;
            return screenPosition.y >= bottom && screenPosition.y <= top;
        }

        public void RenderHand(IReadOnlyList<CardRuntime> hand, int currentAp)
        {
            EnsureCardPrefab();
            for (var i = handRoot.childCount - 1; i >= 0; i--)
            {
                var child = handRoot.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            foreach (var card in hand)
            {
                var view = Instantiate(cardViewPrefab, handRoot);
                view.Bind(card, this);
                view.Refresh(card.CurrentCost <= currentAp);
            }

            ApplyFanHandLayout();
        }

        public void RefreshHandInteractivity(int currentAp)
        {
            foreach (Transform child in handRoot)
            {
                var view = child.GetComponent<CardView>();
                if (view != null)
                {
                    view.Refresh(view.Card.CurrentCost <= currentAp);
                }
            }

            ApplyFanHandLayout();
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
            var ownerName = card.Owner != null ? card.Owner.playerName : "[Item]";
            previewBody.text = $"{ownerName}\n{card.Data.rulesText}";
            previewCost.text = $"{card.CurrentCost} AP";
            previewCost.color = card.CurrentCost < card.Data.apCost ? Color.red : Color.black;
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

            RefreshHoop(owner.PlayerHoop, playerHoopText, playerHpFill, playerShieldFill, owner.PlayerHoopDimmed);
            RefreshHoop(owner.EnemyHoop, enemyHoopText, enemyHpFill, enemyShieldFill, owner.EnemyHoopDimmed);
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

        private void ConfigureRuntimeLayout()
        {
            StretchToCanvas(playArea);
            DisableHandLayoutGroup();
            MoveRect(handRoot, new Vector2(0, -405), new Vector2(980, 300));
            MoveRect(apText?.rectTransform, new Vector2(0, -510), new Vector2(230, 64));
            MoveRect(deckText?.rectTransform, new Vector2(-760, -330), new Vector2(110, 90));
            MoveRect(discardText?.rectTransform, new Vector2(760, -330), new Vector2(120, 90));
            MoveRect(confirmButton != null ? (RectTransform)confirmButton.transform : null, new Vector2(820, -450), new Vector2(120, 76));
            MoveRect(logText?.rectTransform, new Vector2(0, 250), new Vector2(820, 60));
            AddWhiteBacking(deckText);
            AddWhiteBacking(discardText);
        }

        private void DisableHandLayoutGroup()
        {
            if (handRoot == null)
            {
                return;
            }

            foreach (var layout in handRoot.GetComponents<LayoutGroup>())
            {
                layout.enabled = false;
            }
        }

        private void ApplyFanHandLayout()
        {
            if (handRoot == null)
            {
                return;
            }

            var count = handRoot.childCount;
            if (count == 0)
            {
                return;
            }

            var spacing = Mathf.Clamp(760f / Mathf.Max(1, count - 1), 82f, 130f);
            var center = (count - 1) * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var child = (RectTransform)handRoot.GetChild(i);
                var offset = i - center;
                var normalized = count == 1 ? 0f : offset / center;
                var x = offset * spacing;
                var y = -Mathf.Abs(normalized) * 42f + Mathf.Cos(normalized * Mathf.PI * 0.5f) * 18f;
                var rotation = -normalized * 13f;
                child.anchoredPosition = new Vector2(x, y);
                child.localRotation = Quaternion.Euler(0f, 0f, rotation);
                child.localScale = Vector3.one;
            }
        }

        private static void StretchToCanvas(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void MoveRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void AddWhiteBacking(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            var rect = text.rectTransform;
            var backingName = text.gameObject.name + "Backing";
            var backing = rect.parent.Find(backingName) as RectTransform;
            if (backing == null)
            {
                var go = new GameObject(backingName, typeof(RectTransform), typeof(Image));
                backing = (RectTransform)go.transform;
                backing.SetParent(rect.parent, false);
            }

            backing.anchorMin = rect.anchorMin;
            backing.anchorMax = rect.anchorMax;
            backing.pivot = rect.pivot;
            backing.anchoredPosition = rect.anchoredPosition;
            backing.sizeDelta = rect.sizeDelta;
            backing.SetSiblingIndex(rect.GetSiblingIndex());
            var image = backing.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);
            image.raycastTarget = false;
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

        private static void RefreshHoop(HoopState hoop, TMP_Text text, Image hpFill, Image shieldFill, bool dimmed)
        {
            text.text = $"{hoop.Hp}/{hoop.BaseHp}\nShield {hoop.Shield}";
            var max = Mathf.Max(1, hoop.MaxTarget);
            hpFill.fillAmount = Mathf.Clamp01((float)hoop.Hp / max);
            shieldFill.fillAmount = Mathf.Clamp01((float)hoop.Shield / max);
            var alpha = dimmed ? 0.28f : 1f;
            SetAlpha(text, alpha);
            SetAlpha(hpFill, alpha);
            SetAlpha(shieldFill, alpha);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
