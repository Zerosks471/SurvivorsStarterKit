using Godot;

public enum EnemyClass
{
    Minion = 0,
    Warrior = 1,
    Archer = 2,
    Mage = 3,

    Boss = 100
}

public partial class Enemy : AnimatableBody3D
{
    public const uint MaxLifepoints = 10;
    public const float AttackRange = 2f;

    /// <summary>Enemies closer than this animate every other frame. Farther ones every 4th.</summary>
    public const float NearAnimationRange = 20f;

    [Export]
    public int Lifepoints { get; set; } = (int)MaxLifepoints;

    [Export]
    public uint Damages { get; set; } = 5;

    [Export]
    public float MovementSpeed = 4;

    [Export]
    public int Experience { get; private set; } = 1;

    /// <summary>Position kept on the C# side so weapons and the grid avoid engine calls.</summary>
    public Vector3 CachedPosition { get; private set; }

    public EnemyClass Class { get; internal set; }

    /// <summary>False while the enemy sits hidden in EnemyManager's pool.</summary>
    public bool IsActive { get; private set; } = true;

    public bool IsAlive => IsActive && Lifepoints > 0 && !IsQueuedForDeletion();

    /// <summary>Stats from the scene file, kept so a pooled enemy can be reset.</summary>
    internal int BaseLifepoints;
    internal uint BaseDamages;
    internal float BaseMovementSpeed;

    /// <summary>Set by EnemyManager. Called when HP hits 0 instead of freeing the node.</summary>
    internal System.Action<Enemy> OnDied;

    /// <summary>Radius of the enemy's collision sphere in world units. Weapons use it for hit checks.</summary>
    public float BodyRadius { get; private set; } = 0.75f;

    private GameManager _gameManager;
    private GpuParticles3D _damageParticles;

    private ProgressBar _lifebar;
    private Camera3D _camera;
    private Label _username;

    private float _attackCooldown = 1f;
    private float _attackTimeLeft = 0f;

    /// <summary>Shortest gap between two hit particle bursts on the same enemy.</summary>
    private const ulong HitParticleGapMs = 300;
    private ulong _lastParticleMs;

    private AnimationTree _animationTree;
    private float _animationDelta = 0f;
    private int _animationFrame;

    [Signal]
    public delegate void OnEnemyHitEventHandler(Enemy enemy, int damages);

    public bool IsPlayerInAttackRange =>
        (_gameManager.Player.GlobalPosition - CachedPosition).LengthSquared() <= AttackRange * AttackRange;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");

        // The attack timer node is only read for its duration now.
        // The cooldown is counted in Tick() to skip a Timer per enemy.
        var attackTimer = GetNode<Timer>("AttackCooldown");
        _attackCooldown = (float)attackTimer.WaitTime;
        attackTimer.Stop();

        _damageParticles = GetNode<GpuParticles3D>("DamageParticles");
        _camera = GetNode<Camera3D>("../Player/Camera3D");

        CachedPosition = GlobalPosition;

        // Hits are found through EnemyManager's grid now, so the physics engine
        // no longer needs to track hundreds of overlapping enemy bodies.
        var shape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (shape?.Shape is SphereShape3D sphere)
            BodyRadius = sphere.Radius * shape.GlobalTransform.Basis.Scale.X;
        CollisionLayer = 0;
        CollisionMask = 0;

        // Animation is advanced from Tick() at a reduced rate, staggered per enemy
        _animationTree = GetNodeOrNull<AnimationTree>("AnimationTree");
        if (_animationTree != null)
            _animationTree.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
        _animationFrame = (int)(GD.Randi() % 4);

        // EnemyManager drives every enemy from one loop. Turning off per-node
        // callbacks saves two engine-to-C# calls per enemy per frame.
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    /// <summary>Puts the enemy into play with fresh stats. Used for new and pooled enemies.</summary>
    internal void Activate(Vector3 position, int lifepoints, uint damages, float movementSpeed)
    {
        Lifepoints = lifepoints;
        Damages = damages;
        MovementSpeed = movementSpeed;
        _attackTimeLeft = 0f;
        _lastParticleMs = 0;
        IsActive = true;
        Visible = true;

        if (IsInsideTree()) GlobalPosition = position;
        else Position = position;
        CachedPosition = position;
    }

    /// <summary>Hides the enemy so EnemyManager can reuse it.</summary>
    internal void Deactivate()
    {
        IsActive = false;
        Visible = false;
    }

    /// <summary>Called by EnemyManager once per physics frame.</summary>
    internal void Tick(float delta, Vector3 playerPos)
    {
        if (!IsAlive) return;

        var direction = (playerPos - CachedPosition).LimitLength();
        var newPos = CachedPosition + delta * MovementSpeed * direction;

        // One transform write instead of a position write plus a LookAt call
        var flatDir = new Vector3(playerPos.X - newPos.X, 0, playerPos.Z - newPos.Z);
        var basis = flatDir.LengthSquared() > Mathf.Epsilon
            ? Basis.LookingAt(flatDir, Vector3.Up, true)
            : GlobalTransform.Basis;
        GlobalTransform = new Transform3D(basis, newPos);
        CachedPosition = newPos;

        _attackTimeLeft -= delta;
        if (_attackTimeLeft <= 0f) Attack();

        AdvanceAnimation(delta, playerPos);

        UpdateLabels();
    }

    private void AdvanceAnimation(float delta, Vector3 playerPos)
    {
        if (_animationTree == null) return;

        _animationDelta += delta;
        _animationFrame++;
        bool near = (playerPos - CachedPosition).LengthSquared() < NearAnimationRange * NearAnimationRange;
        int stride = near ? 2 : 4;
        if (_animationFrame % stride != 0) return;

        _animationTree.Advance(_animationDelta);
        _animationDelta = 0f;
    }

    private void UpdateLabels()
    {
        if (_lifebar != null)
        {
            Vector2 lifeBarPos = _camera.UnprojectPosition(CachedPosition);
            lifeBarPos -= _lifebar.Size / 2;
            lifeBarPos.Y -= 50;
            _lifebar.Position = lifeBarPos;
            _lifebar.Value = Lifepoints;
        }

        if (_username == null) return;

        Vector2 usernamePos = _camera.UnprojectPosition(CachedPosition);
        usernamePos -= _username.Size / 2;
        usernamePos.Y -= 40;
        _username.Position = usernamePos;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        _lifebar?.QueueFree();
        _username?.QueueFree();
    }

    private void Attack()
    {
        if (!IsPlayerInAttackRange) return;
        _attackTimeLeft = _attackCooldown;
        _gameManager.Player.TakeDamages(Damages);
    }

    internal void SetName(string username)
    {
        _username = new() { Text = username };
        GetNode<Control>("../HUD").AddChild(_username);
    }

    internal void TakeDamages(uint damages = 1)
    {
        if (!IsAlive) return;

        EmitParticles();

        // Direct call instead of EmitSignal: signals cost an engine round trip per hit,
        // and area weapons can land hundreds of hits in one frame
        _gameManager.EnemyHit(this, (int)damages);
        Lifepoints -= (int)damages;
        if (Lifepoints > 0) return;

        if (OnDied != null) OnDied(this);
        else QueueFree();
    }

    internal void EmitParticles()
    {
        // Replays the one-shot burst instead of duplicating a particle node per hit.
        // Rapid hits share one burst.
        ulong now = Time.GetTicksMsec();
        if (now - _lastParticleMs < HitParticleGapMs) return;
        _lastParticleMs = now;
        _damageParticles.Restart();
    }
}
