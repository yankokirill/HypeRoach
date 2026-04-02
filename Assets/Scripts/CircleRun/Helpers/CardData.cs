using UnityEngine;

namespace Game.CircleRun
{
    [CreateAssetMenu(fileName = "NewRunCard", menuName = "CircleRun/Card")]
    public class CardData : ScriptableObject
    {
        public string cardTitle = "Способность";
        [TextArea] public string description = "Описание";
        public int mana = 1;

        [HideInInspector] public int level = 1;

        public Sprite art;

        [SerializeReference, SubclassSelector]
        public IEffect effect;
    }

    public interface IEffect
    {
        // Теперь мы передаем уровень карты прямо в эффект!
        void Apply(PlayerCockroach player, EnemyCockroach[] enemies, int level);
        bool Check(PlayerCockroach player, EnemyCockroach[] enemies);
    }
}
