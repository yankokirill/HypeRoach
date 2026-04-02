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
            c.transform.SetAsLastSibling();

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

        private void UpdateCardPositions()
        {
            if (cards.Count == 0) return;
            float totalWidth = (cards.Count - 1) * currentSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < cards.Count; i++)
            {
                CardView card = cards[i];
                if (card.currentSlot != null) continue;
                Vector3 targetPos = new Vector3(startX + (i * currentSpacing), currentYOffset, 0);
                card.targetLocalPosition = targetPos;
            }
        }

        public void UpdateCardOrder(CardView draggedCard, PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(handParent, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
            int newIndex = 0;
            float minDistance = float.MaxValue;
            float totalWidth = (cards.Count - 1) * currentSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < cards.Count; i++)
            {
                float slotX = startX + (i * currentSpacing);
                float dist = Mathf.Abs(localPos.x - slotX);
                if (dist < minDistance) { minDistance = dist; newIndex = i; }
            }

            int oldIndex = cards.IndexOf(draggedCard);
            if (oldIndex != newIndex && oldIndex != -1)
            {
                cards.RemoveAt(oldIndex);
                cards.Insert(newIndex, draggedCard);
                draggedCard.transform.SetSiblingIndex(newIndex);
            }
        }
    }
}
