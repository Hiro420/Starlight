using Starlight.Game.Ability.DynamicProps;
using Starlight.Protocol;

namespace Starlight.Game.Ability.Handlers.Actions;

public sealed class SetGlobalValueV2Handler(IInvokeForwarder forwarder) : AbilityActionHandler
{
    public override async ValueTask HandleAsync(AbilityContext context)
    {
        if (context.Action is null)
            return;

        var key = GlobalValueActionHelpers.GetString(context.Action, "key");

        if (key.Length == 0)
            return;

        var value = AbilityDynamicFloat.Get(
            context,
            "value",
            GlobalValueActionHelpers.EvaluationOwner(context));
        var target = GlobalValueActionHelpers.DefaultTarget(context);

        GlobalValueActionHelpers.SetGlobal(target, key, value);
        GlobalValueActionHelpers.SetServerGlobal(target, key, value);

        await forwarder.Forward(
            context.Player,
            ForwardType.FORWARD_TYPE_TO_ALL,
            new ServerUpdateGlobalValueNotify {
                EntityId = target.Owner.EntityId,
                KeyHash = AbilityHash.Compute(key),
                UpdateType = ServerUpdateGlobalValueNotify.Types.UpdateType.UPDATE_TYPE_SET,
                Value = value,
                Delta = 0f
            },
            forwardPeer: 0);
    }
}
