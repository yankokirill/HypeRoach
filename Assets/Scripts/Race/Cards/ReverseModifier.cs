using UnityEngine;

namespace Game.Race.Effects
{
    public class ReverseModifier : StatusEffect
    {
        public override void Initialize(Cockroach owner, float duration)
        {
            base.Initialize(owner, duration);

            // Применяем разворот при старте эффекта
            if (owner.moveDirection == 1)
            {
                owner.ReverseDirection();
            }
        }

        public override float ModifySpeed(float currentSpeed)
        {
            return currentSpeed; // Скорость не меняем, меняется только moveDirection внутри Cockroach
        }

        public override bool ProvidesInvulnerability() => false;

        public override void OnRemove()
        {
            // Возвращаем нормальное направление, когда эффект спадает
            if (owner != null && owner.moveDirection == -1)
            {
                owner.ReverseDirection();
            }
            Debug.Log("Действие стрелки НАЗАД закончилось.");
        }
    }
}
