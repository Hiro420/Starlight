using System.Text.Json.Serialization;

namespace Starlight.Game.Resources.Excel;

[GameResource("AvatarExcelConfigData.json")]
public sealed class AvatarData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("iconName")]
    public string IconName { get; set; } = string.Empty;

    [JsonPropertyName("initialWeapon")]
    public uint InitialWeapon { get; set; }

    [JsonPropertyName("avatarPromoteId")]
    public uint AvatarPromoteId { get; set; }

    [JsonPropertyName("skillDepotId")]
    public uint SkillDepotId { get; set; }

    [JsonPropertyName("candSkillDepotIds")]
    public List<uint> CandSkillDepotIds { get; set; } = [];

    [JsonPropertyName("hpBase")]
    public float HpBase { get; set; }

    [JsonPropertyName("attackBase")]
    public float AttackBase { get; set; }

    [JsonPropertyName("defenseBase")]
    public float DefenseBase { get; set; }

    [JsonPropertyName("critical")]
    public float CritChanceBase { get; set; }

    [JsonPropertyName("criticalHurt")]
    public float CritDamageBase { get; set; }

    [JsonPropertyName("elementMastery")]
    public float ElementMasteryBase { get; set; }

    [JsonPropertyName("chargeEfficiency")]
    public float ChargeEfficiencyBase { get; set; }

    [JsonPropertyName("propGrowCurves")]
    public List<PropGrowCurveData> PropGrowCurves { get; set; } = [];

    public string AvatarName => IconName.Split('_').Last();

    public float GetBaseStat(FightProperty property, uint level, IReadOnlyDictionary<uint, AvatarCurveData> curves)
    {
        var baseValue = property switch {
            FightProperty.FIGHT_PROP_BASE_HP => HpBase,
            FightProperty.FIGHT_PROP_BASE_ATTACK => AttackBase,
            FightProperty.FIGHT_PROP_BASE_DEFENSE => DefenseBase,
            _ => 0f
        };

        var curveName = PropGrowCurves.FirstOrDefault(curve =>
            FightPropertyExtensions.TryParse(curve.Type, out var curveProperty) && curveProperty == property)?.GrowCurve;

        return curves.TryGetValue(level, out var curve) ? baseValue * curve.GetMultiplier(curveName ?? string.Empty) : baseValue;
    }
}

[GameResource("AvatarSkillDepotExcelConfigData.json")]
public sealed class AvatarSkillDepotData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("skills")]
    public List<uint> Skills { get; set; } = [];

    [JsonPropertyName("energySkill")]
    public uint EnergySkill { get; set; }

    [JsonPropertyName("talents")]
    public List<uint> Talents { get; set; } = [];

    [JsonPropertyName("talentStarName")]
    public string TalentStarName { get; set; } = string.Empty;

    [JsonPropertyName("skillDepotAbilityGroup")]
    public string SkillDepotAbilityGroup { get; set; } = string.Empty;

    [JsonPropertyName("extraAbilities")]
    public List<string> ExtraAbilities { get; set; } = [];

    [JsonPropertyName("inherentProudSkillOpens")]
    public List<InherentProudSkillOpenData> InherentProudSkillOpens { get; set; } = [];

    [JsonPropertyName("specialProudSkillOpens")]
    public List<SpecialProudSkillOpenData> SpecialProudSkillOpens { get; set; } = [];

    public IEnumerable<uint> SkillsAndEnergySkill => Skills.Append(EnergySkill).Where(skill => skill != 0);
}

[GameResource("AvatarSkillExcelConfigData.json", Priority = LoadPriority.Highest)]
public sealed class AvatarSkillData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("proudSkillGroupId")]
    public uint ProudSkillGroupId { get; set; }

    [JsonPropertyName("maxChargeNum")]
    public uint MaxChargeNum { get; set; }

    [JsonPropertyName("costElemVal")]
    public uint CostElemVal { get; set; }

    [JsonPropertyName("costElemType")]
    public string CostElemType { get; set; } = string.Empty;

    [JsonPropertyName("specialEnergyCostType")]
    public string SpecialEnergyCostType { get; set; } = string.Empty;

    [JsonPropertyName("specialEnergyCostStart")]
    public uint SpecialEnergyCostStart { get; set; }

    [JsonPropertyName("specialEnergyCostMin")]
    public uint SpecialEnergyCostMin { get; set; }

    [JsonPropertyName("specialEnergyCostMax")]
    public uint SpecialEnergyCostMax { get; set; }

    public bool HasSpecialEnergyRequirement =>
        !string.IsNullOrEmpty(SpecialEnergyCostType) &&
        !string.Equals(SpecialEnergyCostType, "SPECIAL_ENERGY_NONE", StringComparison.Ordinal);

    public uint EnergyMaximum => HasSpecialEnergyRequirement ? SpecialEnergyCostMax : CostElemVal;
    public uint EnergyStart => HasSpecialEnergyRequirement ? Math.Max(SpecialEnergyCostStart, SpecialEnergyCostMin) : 0;
}

[GameResource("AvatarTalentExcelConfigData.json")]
public sealed class AvatarTalentData : Data
{
    [JsonPropertyName("talentId")]
    public new uint Id { get; set; }

    [JsonPropertyName("openConfig")]
    public string ConfigName { get; set; } = string.Empty;

    [JsonPropertyName("paramList")]
    public List<float> ParamList { get; set; } = [];
}

public sealed class InherentProudSkillOpenData
{
    [JsonPropertyName("proudSkillGroupId")]
    public uint ProudSkillGroupId { get; set; }

    [JsonPropertyName("needAvatarPromoteLevel")]
    public uint NeedAvatarPromoteLevel { get; set; }
}

public sealed class SpecialProudSkillOpenData
{
    [JsonPropertyName("proudSkillGroupId")]
    public uint ProudSkillGroupId { get; set; }
}
