using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public record Choice(Powerup Powerup, EnemyPowerup EnemyPowerup, double EnemyValue);

public partial class GameManager : Node
{
    private const float VoteTime = 30;

    /// <summary>Spawn cap. The original kit stopped at 200 because of physics cost.</summary>
    public const int MaxEnemies = 500;

    /// <summary>How far weapons look for a target.</summary>
    public const float TargetSearchRange = 60f;

    // Debug: F3 shows the performance overlay, F4 toggles stress-test spawning
    private bool _stressTest = false;
    private bool _f3WasDown = false;
    private bool _f4WasDown = false;
    private Label _perfLabel;

    public Player Player;

    private double _enemySpawnTimeLeft = 1;

    private EnemyManager _enemyManager;

    private ProgressBar _playerXpBar;
    private float _playerXp = 0;
    private int _playerLevel = 1;
    private int _maxPlayerXP = 5;

    private bool _isVotePhase = false;
    private UpgradeView _upgradeView;
    private List<Choice> _currentVotes;
    private List<Powerup> _powerups = new();
    private List<EnemyPowerup> _enemyPowerups = new();
    private Dictionary<PowerupType, int> _powerupsCount = new();
    private Dictionary<EnemyPowerupType, int> _enemyPowerupsCount = new();

    private Label _gameTimeLabel;
    public double GameTime { get; private set; } = 0;

    public event Action<Enemy, int> OnEnemyHit;

    public override void _Ready()
    {
        base._Ready();

        _enemyManager = new(this);
        _enemySpawnTimeLeft = _enemyManager.SpawnDelay;

        ProcessMode = ProcessModeEnum.Always;

        _maxPlayerXP = GetMaxXPPerLevel(1);

        _gameTimeLabel = GetNode<Label>("/root/MainScene/HUD/GameTime");
        _playerXpBar = GetNode<ProgressBar>("/root/MainScene/HUD/PlayerXPBar");
        _playerXpBar.MaxValue = _maxPlayerXP;

        _upgradeView = GetNode<UpgradeView>("/root/MainScene/HUD/UpgradeContainer");
        _upgradeView.OnChoose += OnChoose;

        LoadPowerups();
        LoadEnemyPowerups();

        CreatePerfOverlay();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // GameManager keeps running while paused (ProcessMode.Always), so check here
        if (_isVotePhase || GetTree().Paused) return;

        _enemyManager.Tick(delta);
    }

    private void CreatePerfOverlay()
    {
        _perfLabel = new Label
        {
            Position = new Vector2(12, 12),
            Visible = OS.IsDebugBuild(),
            ProcessMode = ProcessModeEnum.Always,
        };
        _perfLabel.AddThemeColorOverride("font_color", Colors.White);
        _perfLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _perfLabel.AddThemeConstantOverride("outline_size", 4);
        GetNode("/root/MainScene/HUD").CallDeferred(Node.MethodName.AddChild, _perfLabel);
    }

    private void UpdateDebugInput()
    {
        bool f3 = Input.IsPhysicalKeyPressed(Key.F3);
        if (f3 && !_f3WasDown) _perfLabel.Visible = !_perfLabel.Visible;
        _f3WasDown = f3;

        bool f4 = Input.IsPhysicalKeyPressed(Key.F4);
        if (f4 && !_f4WasDown) _stressTest = !_stressTest;
        _f4WasDown = f4;

        if (!_perfLabel.Visible) return;
        double physicsMs = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000;
        double processMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000;
        _perfLabel.Text =
            $"FPS {Engine.GetFramesPerSecond()}  |  Enemies {_enemyManager.Enemies.Count}/{MaxEnemies}\n" +
            $"Physics {physicsMs:0.0} ms  |  Process {processMs:0.0} ms\n" +
            $"F3 overlay  |  F4 stress test: {(_stressTest ? "ON" : "off")}";
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        UpdateDebugInput();

        if (_isVotePhase) return;

        // Debug thing
        if (Input.IsActionJustPressed("SpawnBoss"))
        {
            _enemyManager.SpawnBoss();
        }

        GameTime += delta;
        _gameTimeLabel.Text = $"{Mathf.FloorToInt(GameTime / 60):00}:{Mathf.FloorToInt(GameTime % 60):00}";

        if (Player == null) return;

        _enemySpawnTimeLeft -= delta;
        if (_enemySpawnTimeLeft > 0) return;
        // Stress test spawns 15x faster, one enemy per tick, so spawns never pile into one frame
        _enemySpawnTimeLeft = _stressTest ? _enemyManager.SpawnDelay / 15f : _enemyManager.SpawnDelay;

        if (_enemyManager.Enemies.Count < MaxEnemies)
            _enemyManager.SpawnEnemy();
    }

    private void LoadPowerups()
    {
        foreach (var path in PowerupPaths.PlayerPowerups)
        {
            Powerup powerup = GD.Load<Powerup>(path);
            _powerups.Add(powerup);
            _powerupsCount.Add(powerup.Type, 0);
        }
    }

    private void LoadEnemyPowerups()
    {
        foreach (var path in PowerupPaths.EnemyPowerups)
        {
            EnemyPowerup powerup = GD.Load<EnemyPowerup>(path);
            _enemyPowerups.Add(powerup);
            _enemyPowerupsCount.Add(powerup.Type, 0);
        }
    }

    internal void EnemyHit(Enemy enemy, int damages)
    {
        OnEnemyHit?.Invoke(enemy, damages);
    }

    internal void EnemyKilled(float enemyXP)
    {
        _playerXp += enemyXP;
        _playerXpBar.Value = _playerXp;
        if (_playerXp >= _maxPlayerXP)
        {
            _playerLevel++;
            _maxPlayerXP = GetMaxXPPerLevel(_playerLevel);
            _playerXpBar.MaxValue = _maxPlayerXP;

            GetTree().Paused = true;
            DisplayPowerups();
        }
    }

    private void DisplayPowerups()
    {
        _isVotePhase = true;

        List<Powerup> powerupChoices = _powerups.Where(p => _powerupsCount[p.Type] < p.MaxCumul)
            .OrderBy(_ => GD.Randf())
            .Take(3)
            .ToList();
        List<EnemyPowerup> enemyPowerupsChoices = _enemyPowerups.Where(p => _enemyPowerupsCount[p.Type] < p.MaxStack)
            .OrderBy(_ => GD.Randf())
            .Take(3)
            .ToList();

        _currentVotes = new List<Choice>{
            new(powerupChoices[0], enemyPowerupsChoices[0], _enemyManager.GetFinalValue(enemyPowerupsChoices[0])),
            new(powerupChoices[1], enemyPowerupsChoices[1], _enemyManager.GetFinalValue(enemyPowerupsChoices[1])),
            new(powerupChoices[2], enemyPowerupsChoices[2], _enemyManager.GetFinalValue(enemyPowerupsChoices[2])),
        };
        _upgradeView.SetChoices(_currentVotes);
    }

    private async void OnChoose(Choice choice)
    {
        _upgradeView.DisplayChoicePicked(_currentVotes.IndexOf(choice) + 1);

        await Task.Delay(3000);

        var upgradables = Player.GetChildren()
            .Where(child => child is IUpgradable)
            .Select(child => child as IUpgradable);
        foreach (var upgradable in upgradables) upgradable.Upgrade(choice.Powerup.Type);

        _enemyManager.Upgrade(choice.EnemyPowerup);

        _powerupsCount[choice.Powerup.Type]++;
        _enemyPowerupsCount[choice.EnemyPowerup.Type]++;

        _isVotePhase = false;

        GetTree().Paused = false;
        _upgradeView.Clear();
        _playerXp = 0;
    }

    public Vector3 GetRandomPosAroundPlayer(float range) => Player.Position + range * new Vector3(
            (float)GD.RandRange(-1f, 1f),
            0,
            (float)GD.RandRange(-1f, 1f)
            ).Normalized();

    internal Enemy GetNearestEnemy() => Player == null
        ? null
        : _enemyManager.GetNearestEnemy(Player.GlobalPosition, TargetSearchRange);

    internal Enemy GetNearestEnemy(Vector3 origin, float maxRange) => _enemyManager.GetNearestEnemy(origin, maxRange);

    internal Enemy GetEnemyInRadius(Vector3 origin, float radius) => _enemyManager.GetEnemyInRadius(origin, radius);

    /// <summary>Fills results with every enemy touching the circle. Clear the list first.</summary>
    internal void GetEnemiesInRadius(Vector3 origin, float radius, List<Enemy> results) => _enemyManager.GetEnemiesInRadius(origin, radius, results);

    internal int GetMaxXPPerLevel(int level) => Mathf.RoundToInt(Math.Log10(Math.Pow(level, 10) * 10) * 5);

    internal int GetMaxEnemyLifepoints(int level) => Mathf.RoundToInt(Math.Log10(level * 10));

    internal int GetMaxEnemyLifepoints() => GetMaxEnemyLifepoints(_playerLevel);
}
