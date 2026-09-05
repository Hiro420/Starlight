using Serilog;

namespace Starlight.Game.Ability.Handlers.Actions;

public sealed class SetGlobalValueToOverrideMapHandler : AbilityActionHandler
{
    private const float DummyThrowAngle = 0.9424778f;

    public override ValueTask HandleAsync(AbilityContext context)
    {
        if (context.Action is null || context.Ability is null)
            return ValueTask.CompletedTask;

        var target = GlobalValueActionHelpers.DefaultTarget(context);
        var finalTarget = target;

        if (GlobalValueActionHelpers.GetBool(context.Action, "isFromOwner") &&
            target.Owner.Type is AbilityOwnerType.Gadget or AbilityOwnerType.ClientGadget)
        {
            finalTarget = GlobalValueActionHelpers.ResolveTargetOwner(context, target);

            if (finalTarget is null)
            {
                Log.Warning(
                    "SetGlobalValueToOverrideMap: could not resolve owner for gadget entity {EntityId}",
                    target.Owner.EntityId);
                return ValueTask.CompletedTask;
            }
        }

        var globalValueKey = GlobalValueActionHelpers.GetString(context.Action, "globalValueKey");
        var overrideMapKey = GlobalValueActionHelpers.GetString(context.Action, "overrideMapKey");

        if (globalValueKey.Length == 0 || overrideMapKey.Length == 0)
            return ValueTask.CompletedTask;

        var value = GlobalValueActionHelpers.GetGlobal(finalTarget, globalValueKey);
        var formula = GlobalValueActionHelpers.GetString(context.Action, "abilityFormula");

        if (string.Equals(formula, "DummyThrowSpeed", StringComparison.Ordinal))
            value = value * 30f / (MathF.Sin(DummyThrowAngle) * 100f) - 1f;

        // Grasscutter writes the transformed value back into the GV map as well.
        GlobalValueActionHelpers.SetGlobal(finalTarget, globalValueKey, value);
        GlobalValueActionHelpers.SetAbilitySpecial(context, overrideMapKey, value);
        return ValueTask.CompletedTask;
    }
}
