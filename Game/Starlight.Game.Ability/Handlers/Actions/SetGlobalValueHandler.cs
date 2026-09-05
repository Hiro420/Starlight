using Starlight.Game.Ability.DynamicProps;

namespace Starlight.Game.Ability.Handlers.Actions;

public sealed class SetGlobalValueHandler : AbilityActionHandler
{
    public override ValueTask HandleAsync(AbilityContext context)
    {
        if (context.Action is null)
            return ValueTask.CompletedTask;

        var key = GlobalValueActionHelpers.GetString(context.Action, "key");

        if (key.Length == 0)
            return ValueTask.CompletedTask;

        var value = AbilityDynamicFloat.Get(
            context,
            "value",
            GlobalValueActionHelpers.EvaluationOwner(context));

        GlobalValueActionHelpers.SetGlobal(GlobalValueActionHelpers.DefaultTarget(context), key, value);
        return ValueTask.CompletedTask;
    }
}
