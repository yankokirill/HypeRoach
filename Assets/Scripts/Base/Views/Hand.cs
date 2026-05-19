using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Base
{
    public class Hand : MonoBehaviour
    {
        [Header("Configuration")]
        public RectTransform handParent;
        public int maxHandSize = 7;

        [Header("Layout Settings")]
        public float cardSpacing = 220f;

        [Header("Start Deal Animation")]
        public float dealDuration = 1.5f;

        public AnimationCurve spacingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float startSpacing = -20f;
        public AnimationCurve yPositionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float startYOffset = -600f;

        private float dealTimer = 0f;
        private float currentSpacing;
        private float currentYOffset;

        private List<CardView> cards = new List<CardView>();
        private CardView _draggedCard;

        public List<CardView> GetCards() => cards;

        private void Start() => dealTimer = 0f;

        private void Update()
        {
            if (dealTimer < dealDuration)
            {
                dealTimer += Time.deltaTime;
                float t = Mathf.Clamp01(dealTimer / dealDuration);
                currentSpacing = Mathf.Lerp(startSpacing, cardSpacing, spacingCurve.Evaluate(t));
                currentYOffset = Mathf.Lerp(startYOffset, 0f, yPositionCurve.Evaluate(t));
            }
            else
            {
                currentSpacing = cardSpacing;
                currentYOffset = 0f;
            }
            UpdateCardPositions();
        }

        public void AddCard(CardView c)
        {
            if (cards.Count >= maxHandSize) return;
            cards.Add(c);
            c.currentHand = this;
            c.transform.SetParent(handParent, false);

            UpdateSiblingIndices(); // ИСХРАВЛЕНИЕ: Безопасное присвоение индексов рендера

            if (dealTimer < dealDuration)
                c.transform.localPosition = new Vector3(0, startYOffset, 0);
        }

        public void RemoveCard(CardView c)
        {
            cards.Remove(c);
            c.currentHand = null;
        }

        public int Count => cards.Count;
        public int GetCardIndex(CardView c) => cards.IndexOf(c);

        public void BeginDrag(CardView card) => _draggedCard = card;

        public void EndDrag()
        {
            _draggedCard = null;
            UpdateSiblingIndices(); // Пересчитываем порядок после возвращения в руку
            UpdateCardPositions();
        }

        // ИСПРАВЛЕНИЕ: Централизованный и надежный метод установки Sibling Indices
        // Идем с конца, чтобы левые карты гарантированно ложились "сверху", без коллизий.
        public void UpdateSiblingIndices()
        {
            int siblingIndex = 0;
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                // Не трогаем карту, если она на Canvas (draggedCard)
                if (cards[i].transform.parent == handParent)
                {
                    cards[i].transform.SetSiblingIndex(siblingIndex);
                    siblingIndex++;
                }
            }
        }

        private void UpdateCardPositions()
        {
            if (cards.Count == 0) return;

            var visibleCards = new List<CardView>(cards.Count);
            foreach (var c in cards)
            {
                if (c.currentSlot != null) continue;
                if (c == _draggedCard) continue;
                visibleCards.Add(c);
            }

            float totalWidth = (visibleCards.Count - 1) * currentSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < visibleCards.Count; i++)
                visibleCards[i].targetLocalPosition =
                    new Vector3(startX + i * currentSpacing, currentYOffset, 0);
        }

        public void UpdateCardOrder(CardView draggedCard, PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handParent, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

            int oldIndex = cards.IndexOf(draggedCard);
            if (oldIndex == -1) return;

            var visibleCards = new List<CardView>(cards.Count);
            foreach (var c in cards)
            {
                if (c.currentSlot != null) continue;
                if (c == draggedCard) continue;
                visibleCards.Add(c);
            }

            int insertIndex = visibleCards.Count;
            float totalWidth = (visibleCards.Count - 1) * currentSpacing;
            float startX = visibleCards.Count > 0 ? -totalWidth / 2f : 0f;

            for (int i = 0; i < visibleCards.Count; i++)
            {
                float slotX = startX + i * currentSpacing;
                if (localPos.x < slotX + currentSpacing * 0.5f)
                {
                    insertIndex = i;
                    break;
                }
            }

            int newIndex;
            if (insertIndex >= visibleCards.Count)
            {
                newIndex = visibleCards.Count > 0
                    ? cards.IndexOf(visibleCards[visibleCards.Count - 1]) + 1
                    : cards.Count;
            }
            else
            {
                newIndex = cards.IndexOf(visibleCards[insertIndex]);
            }

            newIndex = Mathf.Clamp(newIndex, 0, cards.Count);

            if (newIndex != oldIndex)
            {
                cards.RemoveAt(oldIndex);
                if (newIndex > oldIndex) newIndex--;
                cards.Insert(newIndex, draggedCard);

                UpdateSiblingIndices();
                UpdateCardPositions();
            }
        }
    }
}
