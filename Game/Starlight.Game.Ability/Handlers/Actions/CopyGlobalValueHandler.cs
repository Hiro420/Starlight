using Serilog;

namespace Starlight.Game.Ability.Handlers.Actions;

public sealed class CopyGlobalValueHandler : AbilityActionHandler
{
    public override ValueTask HandleAsync(AbilityContext context)
    {
        if (context.Action is null)
            return ValueTask.CompletedTask;

        var srcTarget = GlobalValueActionHelpers.GetString(context.Action, "srcTarget", "HDCHFNKNGKN");
        var dstTarget = GlobalValueActionHelpers.GetString(context.Action, "dstTarget", "HIPHJPGIHKO");
        var source = GlobalValueActionHelpers.ResolveTarget(context, srcTarget);
        var destination = GlobalValueActionHelpers.ResolveTarget(context, dstTarget);

        if (source is null || destination is null)
        {
            Log.Debug(
                "CopyGlobalValue: source or destination is null (srcTarget={SrcTarget}, dstTarget={DstTarget})",
                srcTarget,
                dstTarget);
            return ValueTask.CompletedTask;
        }

        var srcKey = GlobalValueActionHelpers.GetString(context.Action, "srcKey");
        var dstKey = GlobalValueActionHelpers.GetString(context.Action, "dstKey");

        if (srcKey.Length == 0 || dstKey.Length == 0)
            return ValueTask.CompletedTask;

        var value = GlobalValueActionHelpers.GetGlobal(source, srcKey);
        GlobalValueActionHelpers.SetGlobal(destination, dstKey, value);
        GlobalValueActionHelpers.SetAbilitySpecial(context, dstKey, value);
        return ValueTask.CompletedTask;
    }
}
