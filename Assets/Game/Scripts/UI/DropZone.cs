using UnityEngine;
using UnityEngine.EventSystems;

namespace COYGame
{
    public sealed class DropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField] private BattleController battleController;

        public void Bind(BattleController controller)
        {
            battleController = controller;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (battleController == null || eventData.pointerDrag == null)
            {
                return;
            }

            var cardView = eventData.pointerDrag.GetComponent<CardView>();
            if (cardView != null)
            {
                battleController.TryPlayPlayerCard(cardView);
            }
        }
    }
}
