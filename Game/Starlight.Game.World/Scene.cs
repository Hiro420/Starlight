using Starlight.Protocol;

namespace Starlight.Game.World;

/// <summary>One loaded scene inside a <see cref="World"/>.</summary>
public sealed class Scene(World world, uint sceneId)
{
    private readonly Dictionary<uint, SceneEntity> _entities = [];
    private readonly Dictionary<uint, MonsterEntity> _monsters = [];

    /// The world that loaded this scene and allocates its entity IDs.
    public World World => world;

    public uint Id => sceneId;
    public IReadOnlyDictionary<uint, SceneEntity> Entities => _entities;
    public IReadOnlyDictionary<uint, MonsterEntity> Monsters => _monsters;

    public void AddEntity(SceneEntity entity)
    {
        if (!ReferenceEquals(entity.Scene, this))
            throw new ArgumentException("Entity belongs to a different scene.", nameof(entity));

        if (_entities.TryGetValue(entity.EntityId, out var previous) && !ReferenceEquals(previous, entity))
        {
            previous.Detach();

            if (previous is MonsterEntity)
                _monsters.Remove(entity.EntityId);
        }

        _entities[entity.EntityId] = entity;

        if (entity is MonsterEntity monster)
            _monsters[entity.EntityId] = monster;
    }

    public void AddMonster(MonsterEntity monster) => AddEntity(monster);

    public bool TryGetEntity(uint entityId, out SceneEntity entity) =>
        _entities.TryGetValue(entityId, out entity!);

    public SceneEntity? GetEntity(uint entityId) =>
        _entities.GetValueOrDefault(entityId);

    public SceneAttackResult HandleAttack(AttackResult attack)
    {
        if (attack.DefenseId == 0 || !TryGetEntity(attack.DefenseId, out var target))
            return default;

        var damage = target.Damage(attack.Damage);
        return new SceneAttackResult(target, damage, attack.AttackerId);
    }

    public bool RemoveEntity(uint entityId)
    {
        _monsters.Remove(entityId);

        if (!_entities.Remove(entityId, out var entity))
            return false;

        entity.Detach();
        return true;
    }

    public bool RemoveMonster(uint entityId) => RemoveEntity(entityId);
}

public readonly record struct SceneAttackResult(
    SceneEntity? Target,
    EntityDamageResult Damage,
    uint AttackerId
)
{
    public bool Handled => Target is not null && Damage.Applied;
}
