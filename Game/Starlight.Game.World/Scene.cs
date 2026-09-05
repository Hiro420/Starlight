namespace Starlight.Game.World;

/// <summary>One loaded scene inside a <see cref="World"/>.</summary>
public sealed class Scene(World world, uint sceneId)
{
    private readonly Dictionary<uint, MonsterEntity> _monsters = [];

    /// The world that loaded this scene and allocates its entity IDs.
    public World World => world;

    public uint Id => sceneId;
    public IReadOnlyDictionary<uint, MonsterEntity> Monsters => _monsters;

    public void AddMonster(MonsterEntity monster) => _monsters[monster.EntityId] = monster;
}
