namespace Game.Run.Effects
{
    [System.Serializable]
    public class SwapEnemiesEffect : IEffect
    {
        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies)
            => enemies.Length >= 2 && enemies[0].currentLane != enemies[1].currentLane;

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            if (enemies.Length < 2) return;

            int lane_0 = enemies[0].currentLane;
            int lane_1 = enemies[1].currentLane;
            enemies[0].ChangeLane(lane_1);
            enemies[1].ChangeLane(lane_0);

            enemies[0].LockLaneChanging(1f);
            enemies[1].LockLaneChanging(1f);
        }
    }
}
