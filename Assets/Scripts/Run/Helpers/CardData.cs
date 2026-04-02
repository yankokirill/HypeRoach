using System.Collections.Generic;
using UnityEngine;

namespace Game.Run
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Run/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Данные карточки")]
        public string cardTitle = "Статистика";
        public string description = "Описание";
        public int mana;

        [Header("Визуал")]
        public Sprite art;

        [Header("Механика")]    
        [SerializeReference, SubclassSelector]
        public IEffect effect;
    }

    public interface IEffect
    {
        void Apply(PlayerCockroach player, EnemyCockroach[] enemies);
        bool Check(PlayerCockroach player, EnemyCockroach[] enemies);
    }

    public enum Target { None, Player, Enemy1, Enemy2 }
}
