namespace Game.CircleRun.Effects
{
    [System.Serializable]
    public class SwapEffect : IEffect
    {
        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies)
            => enemies != null && enemies.Length >= 2;

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies, int level)
        {
            if (enemies.Length < 2) return;

            // Берем первых двух врагов (или можно рандомных)
            int lane0 = enemies[0].currentLane;
            int lane1 = enemies[1].currentLane;

            enemies[0].ChangeLane(lane1);
            enemies[1].ChangeLane(lane0);

            // Блокируем им смену линии, чтобы они не вернулись сразу назад
            enemies[0].LockLaneChanging(1.0f);
            enemies[1].LockLaneChanging(1.0f);
        }
    }
}