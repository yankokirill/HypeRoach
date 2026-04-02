namespace Game.Race.Effects
{
    [System.Serializable]
    public class BackEffect : IEffect
    {
        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            // Карту можно разыграть всегда, даже если история пуста (просто не сработает)
            return true;
        }

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies, int level)
        {
            // Отматываем игрока
            player.RewindLane();

            // Отматываем всех врагов
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.RewindLane();
                    // Кратковременная блокировка AI, чтобы не начал сразу менять линию обратно
                    enemy.LockLaneChanging(1.0f);
                }
            }
        }
    }
}
