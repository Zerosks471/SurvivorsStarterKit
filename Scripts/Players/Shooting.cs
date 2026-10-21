using Godot;
using System.Collections.Generic;

public partial class Shooting : Node3D, IUpgradable
{
    /// <summary>Bullet radius for hit checks. Same as the old collision sphere (0.5 x 0.2 scale).</summary>
    public const float HitRadius = 0.1f;

    /// <summary>Bullets that miss come back to the pool after this many seconds.</summary>
    public const float BulletLifetime = 4f;

    [Export]
    public uint Damages = 5;

    private uint _damagesBonus = 0;

    [Export]
    public float AttackSpeed = 1;

    private float _attackSpeedBonus = 0;

    public float TotalAttackSpeed => AttackSpeed + _attackSpeedBonus;

    [Export]
    public float BulletSpeed = 2;

    private float _bulletSpeedBonus = 0;

    public float TotalBulletSpeed => BulletSpeed + _bulletSpeedBonus;

    private class Bullet
    {
        public Node3D Node;
        public GpuParticles3D Trail;
        public Vector3 Position;
        public Vector3 Velocity;
        public float TimeLeft;
    }

    private PackedScene _bulletPrefab;
    private readonly List<Bullet> _active = new();
    private readonly Stack<Bullet> _pool = new();
    private Node _bulletParent;

    private GameManager _gameManager;

    private Timer _timer;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _bulletPrefab = (PackedScene)GD.Load("res://Prefabs/Powerups/bullet.tscn");
        _bulletParent = GetTree().CurrentScene;

        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = 1f / TotalAttackSpeed;
        _timer.Timeout += Shoot;
        _timer.Start();
    }

    private void Shoot()
    {
        _timer.Start();

        var nearestEnemy = _gameManager.GetNearestEnemy();
        if (nearestEnemy == null) return;

        var bullet = Rent();
        var origin = GlobalPosition + new Vector3(0, 0.5f, 0);
        var direction = nearestEnemy.CachedPosition - GlobalPosition;
        direction.Y = 0;

        bullet.Position = origin;
        bullet.Velocity = TotalBulletSpeed * direction.Normalized();
        bullet.TimeLeft = BulletLifetime;
        bullet.Node.GlobalPosition = origin;
        bullet.Node.Visible = true;
        bullet.Trail?.Restart();
        _active.Add(bullet);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Walk backwards so finished bullets can be removed in place
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var bullet = _active[i];
            bullet.Position += bullet.Velocity * dt;
            bullet.TimeLeft -= dt;

            var hit = _gameManager.GetEnemyInRadius(bullet.Position, HitRadius);
            if (hit != null)
            {
                hit.TakeDamages(Damages + _damagesBonus);
                Return(i);
                continue;
            }

            if (bullet.TimeLeft <= 0f)
            {
                Return(i);
                continue;
            }

            bullet.Node.GlobalPosition = bullet.Position;
        }
    }

    private Bullet Rent()
    {
        if (_pool.Count > 0) return _pool.Pop();

        var node = _bulletPrefab.Instantiate<Node3D>();
        _bulletParent.AddChild(node);
        return new Bullet
        {
            Node = node,
            Trail = node.GetNodeOrNull<GpuParticles3D>("GPUParticles3D"),
        };
    }

    private void Return(int activeIndex)
    {
        var bullet = _active[activeIndex];
        int last = _active.Count - 1;
        _active[activeIndex] = _active[last];
        _active.RemoveAt(last);

        bullet.Node.Visible = false;
        if (bullet.Trail != null) bullet.Trail.Emitting = false;
        _pool.Push(bullet);
    }

    public void Upgrade(PowerupType powerupType)
    {
        switch (powerupType)
        {
            case PowerupType.ShootingDamages:
                _damagesBonus += 1;
                break;
            case PowerupType.ShootingAttackSpeed:
                _attackSpeedBonus += 0.1f;
                _timer.WaitTime = 1f / TotalAttackSpeed;
                break;
            case PowerupType.ShootingBulletSpeed:
                _bulletSpeedBonus += 0.1f;
                break;
            default: break;
        }
    }
}
