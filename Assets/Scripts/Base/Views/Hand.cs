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

        [Tooltip("Curve controlling the spacing over time (0 to 1)")]
        public AnimationCurve spacingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float startSpacing = -20f;

        [Tooltip("Curve controlling the vertical movement over time (0 to 1)")]
        public AnimationCurve yPositionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("How far down off-screen the cards start")]
        public float startYOffset = -600f;

        // Internal animation state
        private float dealTimer = 0f;
        private float currentSpacing;
        private float currentYOffset;

        private List<CardView> cards = new List<CardView>();

        private void Start()
        {
            // Reset timer on start to trigger the initial deal animation
            dealTimer = 0f;
        }

        private void Update()
        {
            // --- Deal Animation Logic ---
            if (dealTimer < dealDuration)
            {
                dealTimer += Time.deltaTime;

                // Calculate percentage of completion (0.0 to 1.0)
                float t = Mathf.Clamp01(dealTimer / dealDuration);

                // Evaluate curves
                float spaceT = spacingCurve.Evaluate(t);
                float yPosT = yPositionCurve.Evaluate(t);

                // Apply curves to spacing and Y offset
                currentSpacing = Mathf.Lerp(startSpacing, cardSpacing, spaceT);
                currentYOffset = Mathf.Lerp(startYOffset, 0f, yPosT);
            }
            else
            {
                // Ensure values lock to final state when animation finishes
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

            // If we are currently in the middle of the deal animation, 
            // instantly teleport the card to the start location so it doesn't flash on screen.
            if (dealTimer < dealDuration)
            {
                c.transform.localPosition = new Vector3(0, startYOffset, 0);
            }
        }

        public void RemoveCard(CardView c)
        {
            cards.Remove(c);
            c.currentHand = null;
        }

        public int Count => cards.Count;

        public int GetCardIndex(CardView c)
        {
            return cards.IndexOf(c);
        }

        private void UpdateCardPositions()
        {
            if (cards.Count == 0) return;

            // Calculate width using animated spacing
            float totalWidth = (cards.Count - 1) * currentSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < cards.Count; i++)
            {
                CardView card = cards[i];

                if (card.currentSlot != null) continue;

                // Set the target position using the animated Spacing (X) and animated Offset (Y)
                Vector3 targetPos = new Vector3(startX + (i * currentSpacing), currentYOffset, 0);

                card.targetLocalPosition = targetPos;
            }
        }

        public void UpdateCardOrder(CardView draggedCard, PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handParent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPointerPosition
            );

            int newIndex = 0;
            float minDistance = float.MaxValue;

            float totalWidth = (cards.Count - 1) * currentSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < cards.Count; i++)
            {
                float slotX = startX + (i * currentSpacing);
                float distance = Mathf.Abs(localPointerPosition.x - slotX);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    newIndex = i;
                }
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
