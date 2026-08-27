namespace Starlight.Game.World;

/// <summary>One loaded scene inside a <see cref="World"/>.</summary>
public sealed class Scene(World world, uint sceneId)
{
    /// The world that loaded this scene and allocates its entity IDs.
    public World World => world;

    public uint Id => sceneId;
}
