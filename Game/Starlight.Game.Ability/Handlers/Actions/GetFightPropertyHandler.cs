using Serilog;
using Starlight.Protocol;

namespace Starlight.Game.Ability.Handlers.Actions;

public sealed class GetFightPropertyHandler(IInvokeForwarder forwarder) : AbilityActionHandler
{
    public override async ValueTask HandleAsync(AbilityContext context)
    {
        if (context.Action is null ||
            !GlobalValueActionHelpers.TryGetFightProperty(context.Action, out var property))
            return;

        var target = GlobalValueActionHelpers.DefaultTarget(context);
        var key = GlobalValueActionHelpers.GetString(context.Action, "globalValueKey");

        if (key.Length == 0)
            return;

        var value = target.GetFightProperty(property);

        if (context.LogAbilitiesEnabled)
        {
            Log.Information(
                "[GetFightProperty] Get {FightProperty} = {Value} for {EntityId} | {Ability}",
                property,
                value,
                target.Owner.EntityId,
                context.Definition?.AbilityName ?? context.Ability?.Name.ToString());
        }

        GlobalValueActionHelpers.SetGlobal(target, key, value);
        GlobalValueActionHelpers.SetAbilitySpecial(context, key, value);

        GlobalValueActionHelpers.SetServerGlobal(target, key, value);

        await forwarder.Forward(
            context.Player,
            ForwardType.FORWARD_TYPE_TO_ALL,
            new ServerGlobalValueChangeNotify {
                EntityId = target.Owner.EntityId,
                KeyHash = AbilityHash.Compute(key),
                Value = value
            },
            forwardPeer: 0);
    }
}
