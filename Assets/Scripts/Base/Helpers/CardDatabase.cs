// CardDatabase.cs
using System.Collections.Generic;
using UnityEngine;

namespace Game.Base
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Base/Database")]
    public class CardDatabase : ScriptableObject
    {
        public List<HandCardData> buildings;
        public List<HandCardData> upgradeCards;

        public HandCardData GetWorker()
        {
            return buildings[0];
        }

        public HandCardData GetRandomBuilding()
        {
            if (buildings == null || buildings.Count == 0) return null;
            return buildings[Random.Range(0, buildings.Count)];
        }

        public HandCardData GetRandomUpgradeCard()
        {
            if (upgradeCards == null || upgradeCards.Count == 0) return null;
            return upgradeCards[Random.Range(0, upgradeCards.Count)];
        }

        public HandCardData FindBuilding(string id) =>
            buildings?.Find(c => c.cardID == id);

        public HandCardData FindCard(string id) =>
            buildings?.Find(c => c.cardID == id)
            ?? upgradeCards?.Find(c => c.cardID == id);
    }
}
