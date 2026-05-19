// HandCardData.cs
using UnityEngine;

namespace Game.Base
{
    public enum CardType { Building, Upgrade }

    public enum UpgradeType
    {
        None,
        DedInside,
        Baryga,
        Berserk,
        Tusovshchik
    }

    public static class UpgradeTypeExtensions
    {
        public static bool IsLegendary(this UpgradeType t) =>
            t == UpgradeType.Berserk || t == UpgradeType.Tusovshchik;
    }

    [CreateAssetMenu(fileName = "NewHandCard", menuName = "Base/HandCard")]
    public class HandCardData : ScriptableObject
    {
        [Header("Идентификация")]
        public string cardID;
        public string cardName;
        public CardType type;

        [Header("Спрайты")]
        public Sprite art;
        public Sprite background;

        [Header("Базовые характеристики (только для Building)")]
        public int baseHype;
        public int baseEvasion;

        [Header("Кружочки (только для Building)")]
        public int greenCount;
        public int whiteCount;
        public int yellowCount;

        [Header("Тип Upgrade и его описание")]
        public UpgradeType upgradeType = UpgradeType.None;

        [Header("Описание")]
        [TextArea(2, 3)] public string effectText;
        [TextArea(2, 3)] public string description;
    }
}
