using Serilog;
using Starlight.Game.Player;
using Starlight.Game.Resources;
using Starlight.Game.World;
using Starlight.Rpc.Tunnel;

namespace Starlight.Commands;

public sealed class SpawnCommand(PlayerManager players, GameData data) : ICommand
{
    public string Name => "spawn";
    public string Description => "Spawns a resource-backed monster in an online player's current scene.";
    public string Usage => "/spawn <uid> <monster-id> <level>";
    public string[] Aliases => ["s", "monster"];

    public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length != 3 ||
            !uint.TryParse(args[0], out var uid) || uid == 0 ||
            !uint.TryParse(args[1], out var monsterId) || monsterId == 0 ||
            !uint.TryParse(args[2], out var level) || level == 0)
        {
            Log.Warning("Usage: {Usage}", Usage);
            return;
        }

        if (!players.TryGet(uid, out var player))
        {
            Log.Warning("Player '{PlayerId}' is not online.", uid);
            return;
        }

        if (!data.MonsterData.ContainsKey(monsterId))
        {
            Log.Warning("Monster {MonsterId} does not exist in resources.", monsterId);
            return;
        }

        if (!data.MonsterCurveData.ContainsKey(level))
        {
            Log.Warning("Monster level {Level} has no curve data.", level);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var entity = await player.Module<SceneModule>().SpawnMonster(monsterId, level);

            Log.Information(
                "Spawned monster {MonsterId} at level {Level} as entity {EntityId} for player {PlayerId}.",
                monsterId,
                level,
                entity.EntityId,
                uid);
        }
        catch (TunnelClosedException)
        {
            Log.Warning("Spawn stopped because the target player disconnected.");
        }
        catch (InvalidOperationException exception)
        {
            Log.Warning("Cannot spawn monster for player {PlayerId}: {Reason}", uid, exception.Message);
        }
    }
}
