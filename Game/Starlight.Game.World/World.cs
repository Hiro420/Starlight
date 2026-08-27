using Starlight.Game.Player;
using Starlight.Protocol;

namespace Starlight.Game.World;

/// <summary>
/// One player's world, plus anyone who has joined them in co-op. Owns the scenes its players
/// occupy and hands out the entity and peer IDs that are only unique within it.
/// </summary>
public sealed class World(IPlayer owner)
{
    // The owner is always seated here, so the client can rely on where world authority sits.
    private const uint OwnerPeerId = 1;

    private readonly Dictionary<uint, Scene> _scenes = [];
    private readonly Dictionary<uint, IPlayer> _peers = [];
    private readonly Dictionary<IPlayer, uint> _peerIds = [];

    private uint _nextEntityId;

    /// The player this world belongs to; everyone else in <see cref="Peers"/> is a guest.
    public IPlayer Owner { get; } = owner;

    /// Everyone currently in this world, by peer ID.
    public IReadOnlyDictionary<uint, IPlayer> Peers => _peers;

    /// Scenes this world has loaded, by scene ID.
    public IReadOnlyDictionary<uint, Scene> Scenes => _scenes;

    /// <see cref="Owner"/>'s peer ID, or 0 while they are not in the world themselves.
    public uint HostPeerId => PeerIdOf(Owner);

    /// <summary><paramref name="player"/>'s peer ID, or 0 if they are not in this world.</summary>
    public uint PeerIdOf(IPlayer player) => _peerIds.GetValueOrDefault(player);

    /// <summary>Seats <paramref name="player"/> and returns the peer ID they hold until they leave.</summary>
    public uint Join(IPlayer player)
    {
        // A rejoin must not seat the same player twice, so give up whatever slot they held.
        Leave(player);

        var peerId = player == Owner ? OwnerPeerId : NextFreePeerId();

        // Pick the displaced guest's new slot while they still hold the old one, or the search
        // hands back the very slot that is about to be taken from them.
        if (_peers.TryGetValue(peerId, out var displaced))
            Seat(displaced, NextFreePeerId());

        Seat(player, peerId);

        return peerId;
    }

    /// <summary>Removes <paramref name="player"/>, freeing their peer ID for the next joiner.</summary>
    public void Leave(IPlayer player)
    {
        if (_peerIds.Remove(player, out var peerId))
            _peers.Remove(peerId);
    }

    /// <summary>Resolves <paramref name="sceneId"/>, loading the scene the first time someone enters it.</summary>
    public Scene GetScene(uint sceneId)
    {
        if (!_scenes.TryGetValue(sceneId, out var scene))
            _scenes[sceneId] = scene = new Scene(this, sceneId);

        return scene;
    }

    /// <summary>
    /// Allocates an entity ID unique to this world.
    /// <br/>
    /// The upper bits are used by the client for identifying the entity type. It
    /// occasionally changes between versions.
    /// <br/>
    /// TODO: Make <c>21</c> a protocol-specific constant.
    /// </summary>
    public uint NextEntityId(ProtEntityType type) => (uint)type << 21 | ++_nextEntityId & 0xFFFFFF;

    private void Seat(IPlayer player, uint peerId)
    {
        _peers[peerId] = player;
        _peerIds[player] = peerId;
    }

    /// <summary>Lowest unoccupied peer ID, so IDs freed by a leaver get handed straight back out.</summary>
    private uint NextFreePeerId()
    {
        var peerId = OwnerPeerId;

        while (_peers.ContainsKey(peerId))
        {
            peerId++;
        }

        return peerId;
    }
}
