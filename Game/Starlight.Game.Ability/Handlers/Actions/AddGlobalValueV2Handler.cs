using Starlight.Game.Ability.DynamicProps;

namespace Starlight.Game.Ability.Handlers.Actions;

public sealed class AddGlobalValueV2Handler : AbilityActionHandler
{
    public override ValueTask HandleAsync(AbilityContext context)
    {
        if (context.Action is null)
            return ValueTask.CompletedTask;

        var target = GlobalValueActionHelpers.DefaultTarget(context);
        var owner = GlobalValueActionHelpers.EvaluationOwner(context);
        var key = GlobalValueActionHelpers.GetString(context.Action, "key");

        if (key.Length == 0)
            return ValueTask.CompletedTask;

        var delta = AbilityDynamicFloat.Get(context, "value", owner);
        var minValue = AbilityDynamicFloat.Get(context, "minValue", owner);
        var maxValue = AbilityDynamicFloat.Get(context, "maxValue", owner);
        var current = GlobalValueActionHelpers.GetGlobal(target, key);

        var next = GlobalValueActionHelpers.AddWithGrasscutterLimit(
            current,
            delta,
            minValue,
            maxValue,
            GlobalValueActionHelpers.GetBool(context.Action, "useLimitRange"));

        GlobalValueActionHelpers.SetGlobal(target, key, next);

        GlobalValueActionHelpers.SetAbilitySpecial(context, key, next);
        return ValueTask.CompletedTask;
    }
}
