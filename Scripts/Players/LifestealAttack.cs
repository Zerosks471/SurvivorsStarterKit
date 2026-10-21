using Godot;
using System.Collections.Generic;

public partial class LifestealAttack : Node3D, IUpgradable
{
    [Export]
    public uint Damages = 1;

    private uint _damagesBonus = 0;

    [Export]
    public float Cooldown = 5;

    private float _cooldownBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;
    public float TotalCooldown => Cooldown + _cooldownBonus;

    private Player _player;
    private GameManager _gameManager;
    private Timer _stealCooldown;
    private float _radius = 4.8f;
    private readonly List<Enemy> _hits = new();

    public override void _Ready()
    {
        _player = GetParent<Player>();
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _stealCooldown = GetNode<Timer>("Timer");
        _stealCooldown.WaitTime = 1f / TotalCooldown;
        _stealCooldown.Start();

        // The Area3D only supplies the zone size now. Hits come from the enemy grid.
        var area = GetNode<Area3D>("Area3D");
        _radius = WeaponShapes.WorldRadius(area, _radius);
        area.Monitoring = false;
        area.Monitorable = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_stealCooldown.TimeLeft > 0) return;

        _stealCooldown.Start();

        _hits.Clear();
        _gameManager.GetEnemiesInRadius(GlobalPosition, _radius, _hits);
        foreach (var enemy in _hits)
        {
            enemy.TakeDamages(TotalDamages);
            _player.Heal(TotalDamages);
        }
    }

    public void Upgrade(PowerupType powerupType)
    {
        switch (powerupType)
        {
            case PowerupType.LifestealDamages:
                _damagesBonus += 1;
                break;
            case PowerupType.LifestealCooldown:
                _cooldownBonus += 0.5f;
                _stealCooldown.WaitTime = 1f / TotalCooldown;
                break;
            default: break;
        }
    }
}
