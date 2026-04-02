using Game.Run;
using UnityEngine;

namespace Game.CircleRun.Effects
{
    public class DashModifier : StatusEffect
    {
        private float dashForce;
        private float initialDuration;

        public DashModifier(float force)
        {
            this.dashForce = force;
        }

        public override void Initialize(Cockroach owner, float duration)
        {
            base.Initialize(owner, duration);
            this.initialDuration = duration;
        }

        public override float ModifySpeed(float currentSpeed)
        {
            // progress идет от 1.0 (старт) до 0.0 (конец)
            float progress = duration / initialDuration;

            // Используем возведение в степень (например, ^2), чтобы затухание было резким в начале
            // и плавным в конце. Это создает эффект "пинка".
            float curve = Mathf.Pow(progress, 2f);

            float extraSpeed = dashForce * curve;

            return currentSpeed + extraSpeed;
        }

        // Прохождение сквозь объекты во время рывка
        public override bool ProvidesInvulnerability() => true;

        public override void OnRemove()
        {
            Debug.Log("Рывок завершен.");
        }
    }

    [System.Serializable]
    public class DashEffect : IEffect
    {
        public float duration = 0.5f;
        public float dashForce = 15f;

        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies) => true;

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies, int level)
        {
            var dash = new DashModifier(dashForce);
            dash.Initialize(player, duration);
            player.AddEffect(dash);
        }
    }
}
