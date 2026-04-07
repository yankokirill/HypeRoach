using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Base
{
    [RequireComponent(typeof(Button))]
    public class GridSlot : MonoBehaviour, IDropHandler
    {
        [Header("Slot Coordinates")]
        public int gridX;
        public int gridY;

        [Header("UI References")]
        [SerializeField] private Transform cardHolder;

        private CardView placedCard;
        private Button slotButton;

        public bool IsEmpty => placedCard == null;

        public Transform CardHolder { get; private set; }

        private void Awake()
        {
            slotButton = GetComponent<Button>();
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        // Standard Unity Drop Event
        public void OnDrop(PointerEventData eventData)
        {
            CardView draggedCard = eventData.pointerDrag.GetComponent<CardView>();
            if (draggedCard == null) return;

            // СЦЕНАРИЙ 1: Карта пришла ИЗ РУКИ
            if (draggedCard.currentHand != null)
            {
                if (IsEmpty)
                {
                    PlaceCard(draggedCard);
                    GameManager.Instance.OnCardPlaced(draggedCard, this);
                }
                else
                {
                    Debug.Log("Нельзя ставить карту из руки на занятую клетку!");
                }
            }
            // СЦЕНАРИЙ 2: Карта пришла С ДРУГОЙ КЛЕТКИ ПОЛЯ
            else if (draggedCard.currentSlot != null)
            {
                GridSlot sourceSlot = draggedCard.currentSlot;
                if (sourceSlot == this)
                {
                    Debug.Log("Карта сброшена в тот же слот. Возвращаем на место.");
                    return;
                }

                if (IsEmpty)
                {
                    sourceSlot.RemoveCard();
                    PlaceCard(draggedCard);
                }
                // Если навели на другую карту
                else if (TryMerge(placedCard, draggedCard))
                {
                    sourceSlot.RemoveCard();
                    Destroy(draggedCard.gameObject);
                }
                else
                {
                    // Это сработает, если уровни или ID разные
                    Debug.Log("Слияние отменено: не совпадают уровни или ID. Карта возвращается назад.");
                }
            }
        }

        private bool TryMerge(CardView baseCard, CardView dragCard)
        {
            CardData baseData = baseCard.cardData;
            CardData dragData = dragCard.cardData;

            // Правила: Одинаковый ID + Одинаковый Уровень + Не Макс Уровень
            if (baseData.cardID == dragData.cardID &&
                baseData.level == dragData.level &&
                baseData.level < baseData.maxLevel)
            {
                baseData.level++; // Повышаем уровень карты на столе
                baseCard.Refresh(baseData.level); // Обновляем текст и картинку
                GameManager.Instance.NotifyStatsChanged(); // Пересчитываем общие статы колонии
                return true;
            }
            return false;
        }

        public void PlaceCard(CardView card)
        {
            // ИСПРАВЛЕНО: Раньше было просто if (!IsEmpty) return;
            // Теперь: если слот не пустой, но в нем лежит ЭТА ЖЕ карта — мы разрешаем ей встать на место!
            if (!IsEmpty && placedCard != card) return;

            placedCard = card;
            card.SetSlot(this);
            card.transform.SetParent(cardHolder, false);
            RectTransform rt = card.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            card.transform.localScale = Vector3.one;

            card.SetVisualMode(true);
        }

        public void RemoveCard()
        {
            placedCard = null;
        }

        public CardView GetCard() => placedCard;

        private void OnSlotClicked()
        {
            GameManager.Instance.OnSlotClicked(this);
        }
    }
}
