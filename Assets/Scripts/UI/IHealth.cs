// Interface sederhana agar HealthBar bisa membaca health dari script apa pun
// (PlayerHealth, EnemyBTController, EnemyUtilityController, EnemyHybridController).
public interface IHealth
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
}
