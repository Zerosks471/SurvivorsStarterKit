using Godot;
using System.Collections.Generic;

public partial class SpiritWater : Node3D, IUpgradable
{
    [Export]
    public uint Damages = 1;

    private uint _damagesBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;

    [Export]
    public float Duration = 4f;

    private float _durationBonus = 0;

    public float TotalDuration => Duration + _durationBonus;

    [Export]
    public float Cooldown = 5f;

    private float _cooldownBonus = 0;

    public float TotalCooldown => Cooldown - _cooldownBonus;

    [Export]
    public float ProjectileRange = 2;

    [Export]
    public PackedScene ProjectilePrefab;
    private Timer _projectileCooldown;
    private Timer _damageCooldown;

    /// <summary>A pool of water on the ground that damages enemies standing in it.</summary>
    private class Zone
    {
        public Vector3 Position;
        public float Radius;
        public bool Active = true;
    }

    private readonly List<Zone> _zones = new();
    private readonly List<Enemy> _hits = new();
    private GameManager _gameManager;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _projectileCooldown = GetNode<Timer>("ProjectileCooldown");
        _projectileCooldown.WaitTime = TotalCooldown;
        _projectileCooldown.Start();
        _projectileCooldown.Timeout += OnAttackReady;

        _damageCooldown = GetNode<Timer>("DamageCooldown");
        _damageCooldown.Start();
        _damageCooldown.Timeout += OnDamageReady;
    }

    private void OnAttackReady()
    {
        _projectileCooldown.WaitTime = TotalCooldown;
        _projectileCooldown.Start();

        var projectile = ProjectilePrefab.Instantiate<Area3D>();
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = _gameManager.GetRandomPosAroundPlayer(ProjectileRange) + new Vector3(0, 0.1f, 0);

        // The Area3D only supplies the zone size now. Hits come from the enemy grid.
        projectile.Monitoring = false;
        projectile.Monitorable = false;
        var zone = new Zone
        {
            Position = projectile.GlobalPosition,
            Radius = WeaponShapes.WorldRadius(projectile, 3f),
        };
        _zones.Add(zone);

        var tweener = GetTree().CreateTween();
        tweener.TweenProperty(projectile.GetNode("Visual"), "scale", new Vector3(0.01f, 0.01f, 0.01f), 1).SetDelay(TotalDuration);
        tweener.Parallel().TweenCallback(Callable.From(() => zone.Active = false)).SetDelay(TotalDuration);
        tweener.TweenCallback(Callable.From(() =>
        {
            _zones.Remove(zone);
            projectile.QueueFree();
        }));
    }

    private void OnDamageReady()
    {
        _damageCooldown.Start();

        foreach (var zone in _zones)
        {
            if (!zone.Active) continue;

            _hits.Clear();
            _gameManager.GetEnemiesInRadius(zone.Position, zone.Radius, _hits);
            foreach (var enemy in _hits)
                enemy.TakeDamages(TotalDamages);
        }
    }

    public void Upgrade(PowerupType powerupType)
    {
        switch (powerupType)
        {
            case PowerupType.SpiritWaterDamages: _damagesBonus += 1; break;
            case PowerupType.SpiritWaterDuration: _durationBonus += 0.2f; break;
            case PowerupType.SpiritWaterCooldown: _cooldownBonus += 0.25f; break;
            default: break;
        }
    }
}
