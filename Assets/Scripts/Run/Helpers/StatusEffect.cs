using UnityEngine;

namespace Game.Run
{
    public abstract class StatusEffect
    {
        public float duration;
        protected Cockroach owner;

        public virtual void Initialize(Cockroach owner, float duration)
        {
            this.owner = owner;
            this.duration = duration;
        }

        // Обновление таймера. Возвращает true, если эффект закончился
        public virtual bool Update(float deltaTime)
        {
            duration -= deltaTime;
            return duration <= 0;
        }

        // Переопределяем эти методы в наследниках, если нужно
        public virtual float ModifySpeed(float currentSpeed) => currentSpeed;
        public virtual bool ProvidesInvulnerability() => false;

        public virtual void OnRemove() { }
    }
}
