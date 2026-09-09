using Starlight.Game.Resources;

namespace Starlight.Game.Ability.DynamicProps;

internal static class AbilityFightProperty
{
    public const uint HealAdd = (uint)FightProperty.FIGHT_PROP_HEAL_ADD;
    public const uint HealedAdd = (uint)FightProperty.FIGHT_PROP_HEALED_ADD;
    public const uint CurHp = (uint)FightProperty.FIGHT_PROP_CUR_HP;
    public const uint MaxHp = (uint)FightProperty.FIGHT_PROP_MAX_HP;
    public const uint CurAttack = (uint)FightProperty.FIGHT_PROP_CUR_ATTACK;
    public const uint CurHpDebts = (uint)FightProperty.FIGHT_PROP_CUR_HP_DEBTS;
    public const uint CurHpPaidDebts = (uint)FightProperty.FIGHT_PROP_CUR_HP_PAID_DEBTS;

    public static bool TryGetId(string name, out uint id)
    {
        if (FightPropertyExtensions.TryParse(name, out var property) && property != FightProperty.FIGHT_PROP_NONE)
        {
            id = (uint)property;
            return true;
        }

        id = 0;
        return false;
    }
}
