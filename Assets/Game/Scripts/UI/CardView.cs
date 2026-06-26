using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace COYGame
{
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image background = null;
        [SerializeField] private Image artworkImage = null;
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
        private bool faceDown;

        public CardRuntime Card { get; private set; }

        public void Bind(CardRuntime card, BattleUI ui, bool showFace = true)
        {
            Card = card;
            battleUI = ui;
            faceDown = !showFace;
            rectTransform = (RectTransform)transform;
            canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            ConfigureCostPosition();
            Refresh(true);
        }

        public void Refresh(bool playable)
        {
            if (faceDown)
            {
                RefreshFaceDown();
                return;
            }

            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
            }

            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(true);
            }

            if (costText != null)
            {
                costText.gameObject.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            titleText.text = Card.Data.cardName;
            var ownerName = Card.Owner != null ? Card.Owner.playerName : "[Item]";
            bodyText.text = $"{ownerName}\n{Card.Data.rulesText}";
            costText.text = Card.CurrentCost.ToString();
            costText.color = Card.CurrentCost < Card.Data.apCost ? Color.red : Color.black;
            RefreshArtwork();
            background.color = playable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.9f);
        }

        private void RefreshFaceDown()
        {
            if (titleText != null)
            {
                titleText.gameObject.SetActive(false);
            }

            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(false);
            }

            if (costText != null)
            {
                costText.gameObject.SetActive(false);
            }

            if (artworkImage != null)
            {
                artworkImage.enabled = false;
            }

            if (background != null)
            {
                background.sprite = null;
                background.color = new Color(0.12f, 0.16f, 0.22f, 1f);
                background.raycastTarget = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void RefreshArtwork()
        {
            var sprite = Card.Data.artwork != null
                ? Card.Data.artwork
                : Card.Owner != null ? Card.Owner.cardArtwork : null;
            if (background != null)
            {
                background.sprite = sprite;
                background.preserveAspect = false;
                background.raycastTarget = true;
            }

            if (artworkImage != null)
            {
                artworkImage.enabled = false;
            }

            if (titleText != null)
            {
                titleText.transform.SetAsLastSibling();
            }

            if (bodyText != null)
            {
                var bodyRect = bodyText.rectTransform;
                bodyRect.anchoredPosition = new Vector2(0f, -42f);
                bodyRect.sizeDelta = new Vector2(138f, 88f);
                bodyText.transform.SetAsLastSibling();
            }

            if (costText != null)
            {
                costText.transform.SetAsLastSibling();
            }
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
            if (faceDown)
            {
                return;
            }

            battleUI.ShowCardPreview(Card);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (faceDown)
            {
                return;
            }

            if (!dragging)
            {
                battleUI.HideCardPreview();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (faceDown)
            {
                return;
            }

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
            if (faceDown)
            {
                return;
            }

            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (faceDown)
            {
                return;
            }

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
