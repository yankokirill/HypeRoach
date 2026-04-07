using UnityEngine;

namespace Game.Race.Effects
{
    public class SpeedBoostModifier : StatusEffect
    {
        private float boostAmount;

        public SpeedBoostModifier(float amount)
        {
            this.boostAmount = amount;
        }

        public override float ModifySpeed(float currentSpeed)
        {
            // Простое и постоянное добавление скорости на время действия эффекта
            return currentSpeed + boostAmount;
        }

        public override bool ProvidesInvulnerability() => false; // Нет неуязвимости!

        public override void OnRemove()
        {
            // Эффект закончился
        }
    }
}
