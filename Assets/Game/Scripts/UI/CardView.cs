using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace COYGame
{
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image background = null;
        [SerializeField] private TMP_Text titleText = null;
        [SerializeField] private TMP_Text bodyText = null;
        [SerializeField] private TMP_Text costText = null;
        [SerializeField] private CanvasGroup canvasGroup = null;

        private BattleUI battleUI;
        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private int originalSiblingIndex;
        private bool dragging;
        private bool played;

        public CardRuntime Card { get; private set; }

        public void Bind(CardRuntime card, BattleUI ui)
        {
            Card = card;
            battleUI = ui;
            rectTransform = (RectTransform)transform;
            canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            ConfigureCostPosition();
            Refresh(true);
        }

        public void Refresh(bool playable)
        {
            titleText.text = Card.Data.cardName;
            bodyText.text = $"{Card.Owner.playerName}\n{Card.Data.rulesText}";
            costText.text = Card.CurrentCost.ToString();
            costText.color = Card.CurrentCost < Card.Data.apCost ? Color.red : Color.black;
            background.color = playable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.9f);
        }

        private void ConfigureCostPosition()
        {
            if (costText == null)
            {
                return;
            }

            var costRect = costText.rectTransform;
            costRect.anchorMin = costRect.anchorMax = new Vector2(0f, 1f);
            costRect.pivot = new Vector2(0f, 1f);
            costRect.anchoredPosition = new Vector2(8f, -8f);
            costRect.sizeDelta = new Vector2(42f, 34f);
            costText.alignment = TextAlignmentOptions.Center;
            costText.fontSize = 24f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            battleUI.ShowCardPreview(Card);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!dragging)
            {
                battleUI.HideCardPreview();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalRotation = rectTransform.localRotation;
            originalScale = rectTransform.localScale;
            transform.SetParent(battleUI.DragLayer, true);
            rectTransform.localRotation = Quaternion.identity;
            canvasGroup.blocksRaycasts = false;
            battleUI.ShowCardPreview(Card);
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            canvasGroup.blocksRaycasts = true;

            if (played)
            {
                battleUI.HideCardPreview();
                return;
            }

            if (battleUI.TryPlayDraggedCard(this, eventData.position))
            {
                return;
            }

            battleUI.HideCardPreview();

            if (transform.parent == battleUI.DragLayer)
            {
                ReturnToHand();
            }
        }

        public void ReturnToHand()
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.anchoredPosition = originalAnchoredPosition;
            rectTransform.localRotation = originalRotation;
            rectTransform.localScale = originalScale;
        }

        public void MarkPlayed()
        {
            played = true;
            battleUI.HideCardPreview();
            Destroy(gameObject);
        }
    }
}
