using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectS.Combat.View
{
    /// <summary>Gắn trên mỗi thẻ ở tay bài, chuyển thao tác kéo thả về CombatScreen.</summary>
    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public CombatScreen Screen;
        public int HandIndex;

        public void OnBeginDrag(PointerEventData eventData)
        {
            Screen.BeginCardDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Screen.UpdateCardDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Screen.EndCardDrag(eventData);
        }
    }
}
