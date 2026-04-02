using UnityEngine;

namespace Game.Base
{
    public enum CardType { Breeding, Training, Sponsor, Artifact }

    [System.Serializable]
    public struct LevelStats
    {
        public int populationIncome;
        public int iqBuff;
        public int charismaBuff;

        [Header("Тексты")]
        [TextArea(2, 3)] public string effectText;  // Строго механика (для руки и драфта)
        [TextArea(2, 3)] public string description; // Лор (для панели построенной карты)
    }

    [CreateAssetMenu(fileName = "NewBaseCard", menuName = "Base/Card")]
    public class CardData : ScriptableObject
    {
        public string cardID;
        public string cardName;
        public CardType type;
        public Sprite art;

        public int level = 1;
        public int maxLevel = 3;

        public LevelStats[] statsPerLevel = new LevelStats[3];

        public CircleRun.CardData runCardReward;

        public int CurrentPopulationIncome => statsPerLevel[Mathf.Clamp(level - 1, 0, 2)].populationIncome;
        public int CurrentIQBuff => statsPerLevel[Mathf.Clamp(level - 1, 0, 2)].iqBuff;
        public int CurrentCharismaBuff => statsPerLevel[Mathf.Clamp(level - 1, 0, 2)].charismaBuff;

        public string CurrentEffectText => statsPerLevel[Mathf.Clamp(level - 1, 0, 2)].effectText;
        public string CurrentDescription => statsPerLevel[Mathf.Clamp(level - 1, 0, 2)].description;

        public CardData Clone()
        {
            var clone = Instantiate(this);
            clone.level = 1;
            return clone;
        }
    }
}
