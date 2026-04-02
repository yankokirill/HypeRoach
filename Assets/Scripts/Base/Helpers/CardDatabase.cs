using System.Collections.Generic;
using UnityEngine;

namespace Game.Base
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Base/Database")]
    public class CardDatabase : ScriptableObject
    {
        public List<CardData> allCards;

        public CardData GetRandomCard()
        {
            if (allCards == null || allCards.Count == 0) return null;
            return allCards[Random.Range(0, allCards.Count)];
        }
    }
}
