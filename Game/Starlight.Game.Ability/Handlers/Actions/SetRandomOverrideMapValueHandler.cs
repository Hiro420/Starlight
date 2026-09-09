using Serilog;
using Starlight.Game.Ability.DynamicProps;
using Starlight.Protobuf.Registry;
using Starlight.Protocol;

namespace Starlight.Game.Ability.Handlers.Actions;

public sealed class SetRandomOverrideMapValueHandler(ProtocolRegistry protocol) : AbilityActionHandler
{
    public override ValueTask HandleAsync(AbilityContext context)
    {
        if (context.Action is null || context.Ability is null ||
            !AbilityInvokeDecode.Try<AbilityActionSetRandomOverrideMapValue>(
                protocol,
                context.Invoke.AbilityData,
                out var random))
            return ValueTask.CompletedTask;

        var owner = GlobalValueActionHelpers.EvaluationOwner(context);
        var min = AbilityDynamicFloat.Get(context, "valueRangeMin", owner);
        var max = AbilityDynamicFloat.Get(context, "valueRangeMax", owner);
        var value = random.RandomValue;

        if (value < min || value > max)
        {
            if (context.LogAbilitiesEnabled)
            {
                Log.Warning(
                    "SetRandomOverrideMapValue rejected {Value}; expected [{Min}, {Max}]",
                    value,
                    min,
                    max);
            }
            return ValueTask.CompletedTask;
        }

        var key = GlobalValueActionHelpers.GetString(context.Action, "overrideMapKey");
        GlobalValueActionHelpers.SetAbilitySpecial(context, key, value);
        return ValueTask.CompletedTask;
    }
}
