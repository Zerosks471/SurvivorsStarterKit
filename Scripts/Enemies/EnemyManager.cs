using Godot;
using System.Collections.Generic;
using System.Linq;

internal class EnemyManager
{
    public const int EnemySpawnRange = 30;

    public float SpawnRate { get; private set; } = 1;

    public float SpawnDelay => 1f / SpawnRate;

    private readonly GameManager _gameManager;
    private readonly Dictionary<EnemyClass, PackedScene> _enemyPrefabs;
    private readonly List<Enemy> _enemies = new();
    public List<Enemy> Enemies => _enemies;

    // Dead enemies wait here, hidden, to be reused. Creating a new skeleton
    // enemy costs about 8 ms, so reuse avoids a hitch on every spawn.
    private readonly Dictionary<EnemyClass, Stack<Enemy>> _pools = new();

    // BONUSES
    private List<EnemyClass> _enemyClasses = new() { EnemyClass.Minion };
    private int _lifepointsBonus = 0;
    private uint _damageBonus = 0;
    private float _movespeedBonus = 0;

    public EnemyManager(GameManager gameManager)
    {
        _gameManager = gameManager;

        _enemyPrefabs = new()
        {
            { EnemyClass.Minion, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_minion.tscn") },
            { EnemyClass.Warrior, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_warrior.tscn") },
            { EnemyClass.Archer, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_archer.tscn") },
            { EnemyClass.Mage, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_mage.tscn") },
            { EnemyClass.Boss, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_boss.tscn") },
        };
    }

    private readonly EnemyGrid _grid = new(4f);

    /// <summary>Moves every enemy and rebuilds the spatial grid. Called once per physics frame.</summary>
    public void Tick(double delta)
    {
        if (_gameManager.Player == null) return;

        var playerPos = _gameManager.Player.GlobalPosition;
        float dt = (float)delta;

        // Index loop: no LINQ allocations. Enemies never die during this loop.
        for (int i = 0; i < _enemies.Count; i++)
            _enemies[i].Tick(dt, playerPos);

        _grid.Rebuild(_enemies);
    }

    internal Enemy GetNearestEnemy(Vector3 origin, float maxRange) => _grid.FindNearest(origin, maxRange);

    internal Enemy GetEnemyInRadius(Vector3 origin, float radius) => _grid.FindFirstInRadius(origin, radius);

    internal void GetEnemiesInRadius(Vector3 origin, float radius, List<Enemy> results) => _grid.CollectInRadius(origin, radius, results);

    internal Enemy SpawnEnemy() => SpawnEnemy(_enemyClasses[GD.RandRange(0, _enemyClasses.Count - 1)]);

    internal Enemy SpawnEnemy(EnemyClass enemyClass)
    {
        bool isNew = !_pools.TryGetValue(enemyClass, out var pool) || pool.Count == 0;
        var enemy = isNew ? CreateEnemy(enemyClass) : pool.Pop();

        enemy.Activate(
            GetRandomPos(),
            enemy.BaseLifepoints * _gameManager.GetMaxEnemyLifepoints() + _lifepointsBonus,
            enemy.BaseDamages + _damageBonus,
            enemy.BaseMovementSpeed + _movespeedBonus);

        if (isNew) _gameManager.GetNode("/root/MainScene").AddChild(enemy);
        _enemies.Add(enemy);
        return enemy;
    }

    private Enemy CreateEnemy(EnemyClass enemyClass)
    {
        var enemy = _enemyPrefabs[enemyClass].Instantiate<Enemy>();
        enemy.Name = enemyClass.ToString();
        enemy.Class = enemyClass;
        enemy.BaseLifepoints = enemy.Lifepoints;
        enemy.BaseDamages = enemy.Damages;
        enemy.BaseMovementSpeed = enemy.MovementSpeed;
        enemy.OnDied = KillEnemy;
        enemy.TreeExiting += () => ForgetEnemy(enemy);
        return enemy;
    }

    internal Enemy SpawnBoss() => SpawnEnemy(EnemyClass.Boss);

    private void KillEnemy(Enemy enemy)
    {
        if (!_enemies.Remove(enemy)) return;
        enemy.Deactivate();

        if (!_pools.TryGetValue(enemy.Class, out var pool))
            _pools[enemy.Class] = pool = new Stack<Enemy>();
        pool.Push(enemy);

        _gameManager.EnemyKilled(enemy.Experience);
    }

    /// <summary>Scene is closing. Drop references without granting XP.</summary>
    private void ForgetEnemy(Enemy enemy)
    {
        _enemies.Remove(enemy);
    }

    private Vector3 GetRandomPos() => _gameManager.GetRandomPosAroundPlayer(EnemySpawnRange);

    internal void Upgrade(EnemyPowerup enemyPowerup)
    {
        switch (enemyPowerup.Type)
        {
            case EnemyPowerupType.UnlockClassWarrior:
                _enemyClasses.Add(EnemyClass.Warrior);
                break;
            case EnemyPowerupType.UnlockClassMage:
                _enemyClasses.Add(EnemyClass.Mage);
                break;
            case EnemyPowerupType.UnlockClassArcher:
                _enemyClasses.Add(EnemyClass.Archer);
                break;
            case EnemyPowerupType.BossSpawn:
                SpawnBoss();
                break;
            case EnemyPowerupType.Lifepoints:
                _lifepointsBonus += (int)((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.Damages:
                _damageBonus += (uint)((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.Movespeed:
                _movespeedBonus += ((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.SpawnRate:
                SpawnRate += ((StatEnemyPowerup)enemyPowerup).Value;
                break;
            default:
                GD.PrintErr($"{enemyPowerup.Type} is not handled");
                break;
        }
    }

    internal double GetFinalValue(EnemyPowerup enemyPowerup) => enemyPowerup.Type switch
    {
        EnemyPowerupType.Lifepoints => _lifepointsBonus + (int)((StatEnemyPowerup)enemyPowerup).Value,
        EnemyPowerupType.Damages => _damageBonus + (uint)((StatEnemyPowerup)enemyPowerup).Value,
        EnemyPowerupType.Movespeed => (double)(_movespeedBonus + ((StatEnemyPowerup)enemyPowerup).Value),
        EnemyPowerupType.SpawnRate => (double)(1f / (SpawnRate + ((StatEnemyPowerup)enemyPowerup).Value)),
        _ => default,
    };
}
