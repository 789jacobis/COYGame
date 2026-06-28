using System;
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
        private CardView previewCardView;
        private readonly List<GameObject> choiceButtons = new();
        private readonly HashSet<CardRuntime> selectableCardTargets = new();
        private Image targetSelectionOverlay;
        private TMP_Text targetSelectionPrompt;
        private Action<CardRuntime> onDraggedCardTargetChosen;
        private bool selectingCardTarget;

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

            if (selectingCardTarget)
            {
                return TryChooseDraggedCardTarget(cardView, screenPosition);
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

        public void BeginDraggedCardTargetSelection(string prompt, IReadOnlyList<CardRuntime> candidates, Action<CardRuntime> onChosen)
        {
            HideCardPreview();
            selectableCardTargets.Clear();
            foreach (var candidate in candidates)
            {
                selectableCardTargets.Add(candidate);
            }

            onDraggedCardTargetChosen = onChosen;
            selectingCardTarget = true;
            EnsureTargetSelectionVisuals();
            targetSelectionPrompt.text = prompt;
            targetSelectionOverlay.gameObject.SetActive(true);
            targetSelectionPrompt.gameObject.SetActive(true);
        }

        public void EndDraggedCardTargetSelection()
        {
            selectingCardTarget = false;
            selectableCardTargets.Clear();
            onDraggedCardTargetChosen = null;
            if (targetSelectionOverlay != null)
            {
                targetSelectionOverlay.gameObject.SetActive(false);
            }

            if (targetSelectionPrompt != null)
            {
                targetSelectionPrompt.gameObject.SetActive(false);
            }
        }

        private bool TryChooseDraggedCardTarget(CardView cardView, Vector2 screenPosition)
        {
            if (!IsMiddleScreenBand(screenPosition))
            {
                return false;
            }

            if (!selectableCardTargets.Contains(cardView.Card))
            {
                SetLog("Choose a valid target card");
                return false;
            }

            cardView.ReturnToHand();
            HideCardPreview();
            ShowChoice("Discount this card?", new[] { "YES", "NO" }, index =>
            {
                if (index == 0)
                {
                    var chosenCard = cardView.Card;
                    var callback = onDraggedCardTargetChosen;
                    EndDraggedCardTargetSelection();
                    HideModal();
                    callback?.Invoke(chosenCard);
                }
                else
                {
                    HideModal();
                    if (targetSelectionOverlay != null)
                    {
                        targetSelectionOverlay.gameObject.SetActive(true);
                    }

                    if (targetSelectionPrompt != null)
                    {
                        targetSelectionPrompt.gameObject.SetActive(true);
                    }
                }
            });
            return true;
        }

        public void RenderHand(IReadOnlyList<CardRuntime> hand, int currentAp, bool revealCards = true)
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
                view.Bind(card, this, revealCards);
                view.Refresh(revealCards && card.CurrentCost <= currentAp);
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
            ClearChoiceButtons();
            if (modalButton != null)
            {
                modalButton.gameObject.SetActive(true);
            }

            modalText.text = message;
            modalPanel.SetActive(true);
            modalPanel.transform.SetAsLastSibling();
        }

        public void ShowChoice(string message, IReadOnlyList<string> choices, Action<int> onChosen)
        {
            HideCardPreview();
            ClearChoiceButtons();
            modalText.text = message;
            modalPanel.SetActive(true);
            modalPanel.transform.SetAsLastSibling();
            if (modalButton != null)
            {
                modalButton.gameObject.SetActive(false);
            }

            var parent = modalPanel.transform;
            for (var i = 0; i < choices.Count; i++)
            {
                var index = i;
                var buttonObject = new GameObject($"ChoiceButton{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = (RectTransform)buttonObject.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(260f, 48f);
                rect.anchoredPosition = choices.Count == 2
                    ? new Vector2(index == 0 ? -78f : 78f, -76f)
                    : new Vector2(0f, -52f - index * 58f);

                var image = buttonObject.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.95f);

                var button = buttonObject.GetComponent<Button>();
                button.onClick.AddListener(() => onChosen?.Invoke(index));

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 4f);
                labelRect.offsetMax = new Vector2(-8f, -4f);

                var label = labelObject.GetComponent<TextMeshProUGUI>();
                label.text = choices[index];
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 22f;
                label.color = Color.black;
                label.raycastTarget = false;

                choiceButtons.Add(buttonObject);
            }
        }

        public void HideModal()
        {
            ClearChoiceButtons();
            if (modalButton != null)
            {
                modalButton.gameObject.SetActive(true);
            }

            modalPanel.SetActive(false);
        }

        public void ShowCardPreview(CardRuntime card)
        {
            EnsureCardPrefab();
            EnsureCardPreviewView();
            SetPreviewTextVisible(false);
            previewCardView.Bind(card, this);
            previewCardView.Refresh(true);
            cardPreview.gameObject.SetActive(true);
        }

        public void HideCardPreview()
        {
            if (cardPreview != null)
            {
                cardPreview.gameObject.SetActive(false);
            }
        }

        private void EnsureCardPreviewView()
        {
            if (previewCardView != null || cardPreview == null || cardViewPrefab == null)
            {
                return;
            }

            previewCardView = Instantiate(cardViewPrefab, cardPreview);
            var rect = (RectTransform)previewCardView.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 1.55f;

            foreach (var graphic in previewCardView.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private void EnsureTargetSelectionVisuals()
        {
            if (targetSelectionOverlay == null)
            {
                var overlayObject = new GameObject("TargetSelectionOverlay", typeof(RectTransform), typeof(Image));
                var overlayRect = (RectTransform)overlayObject.transform;
                overlayRect.SetParent(GetCanvasRoot(), false);
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                targetSelectionOverlay = overlayObject.GetComponent<Image>();
                targetSelectionOverlay.color = new Color(0f, 0f, 0f, 0.38f);
                targetSelectionOverlay.raycastTarget = false;

                overlayRect.SetAsLastSibling();
            }
            else if (targetSelectionOverlay.transform.parent != GetCanvasRoot())
            {
                targetSelectionOverlay.rectTransform.SetParent(GetCanvasRoot(), false);
            }

            if (targetSelectionPrompt == null)
            {
                var promptObject = new GameObject("TargetSelectionPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
                var promptRect = (RectTransform)promptObject.transform;
                promptRect.SetParent(GetCanvasRoot(), false);
                promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 1f);
                promptRect.pivot = new Vector2(0.5f, 1f);
                promptRect.anchoredPosition = new Vector2(0f, -96f);
                promptRect.sizeDelta = new Vector2(760f, 64f);
                targetSelectionPrompt = promptObject.GetComponent<TextMeshProUGUI>();
                targetSelectionPrompt.alignment = TextAlignmentOptions.Center;
                targetSelectionPrompt.fontSize = 32f;
                targetSelectionPrompt.color = Color.white;
                targetSelectionPrompt.raycastTarget = false;
            }
            else if (targetSelectionPrompt.transform.parent != GetCanvasRoot())
            {
                targetSelectionPrompt.rectTransform.SetParent(GetCanvasRoot(), false);
            }

            targetSelectionOverlay.transform.SetAsLastSibling();
            if (handRoot != null)
            {
                handRoot.SetAsLastSibling();
            }

            if (dragLayer != null)
            {
                dragLayer.SetAsLastSibling();
            }

            targetSelectionPrompt.transform.SetAsLastSibling();
            if (modalPanel != null)
            {
                modalPanel.transform.SetAsLastSibling();
            }
        }

        private Transform GetCanvasRoot()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform : transform;
        }

        private void ClearChoiceButtons()
        {
            foreach (var button in choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }

            choiceButtons.Clear();
        }

        private void SetPreviewTextVisible(bool visible)
        {
            if (previewTitle != null)
            {
                previewTitle.gameObject.SetActive(visible);
            }

            if (previewBody != null)
            {
                previewBody.gameObject.SetActive(visible);
            }

            if (previewCost != null)
            {
                previewCost.gameObject.SetActive(visible);
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

            var previewImage = cardPreview.GetComponent<Image>();
            if (previewImage != null)
            {
                previewImage.color = new Color(1f, 1f, 1f, 0f);
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
