using System.Text.Json.Serialization;

namespace Starlight.Game.Resources.Excel;

[GameResource("GadgetExcelConfigData.json")]
public sealed class GadgetData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("jsonName")]
    public string JsonName { get; set; } = string.Empty;

    [JsonPropertyName("itemJsonName")]
    public string ItemJsonName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

[GameResource("MonsterExcelConfigData.json", Priority = LoadPriority.Low)]
public sealed class MonsterData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("monsterName")]
    public string MonsterName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("serverScript")]
    public string ServerScript { get; set; } = string.Empty;

    [JsonPropertyName("ai")]
    public string Ai { get; set; } = string.Empty;

    [JsonPropertyName("affix")]
    public List<uint> Affixes { get; set; } = [];

    [JsonPropertyName("equips")]
    public List<uint> Equips { get; set; } = [];

    [JsonPropertyName("securityLevel")]
    public string SecurityLevel { get; set; } = string.Empty;

    [JsonPropertyName("hpDrops")]
    public List<MonsterHpDropData> HpDrops { get; set; } = [];

    [JsonPropertyName("killDropId")]
    public uint KillDropId { get; set; }

    [JsonPropertyName("excludeWeathers")]
    public string ExcludeWeathers { get; set; } = string.Empty;

    [JsonPropertyName("featureTagGroupID")]
    public uint FeatureTagGroupId { get; set; }

    [JsonPropertyName("mpPropID")]
    public uint MpPropId { get; set; }

    [JsonPropertyName("skin")]
    public string Skin { get; set; } = string.Empty;

    [JsonPropertyName("describeId")]
    public uint DescribeId { get; set; }

    [JsonPropertyName("combatBGMLevel")]
    public int CombatBgmLevel { get; set; }

    [JsonPropertyName("entityBudgetLevel")]
    public int EntityBudgetLevel { get; set; }

    [JsonPropertyName("hpBase")]
    public float HpBase { get; set; }

    [JsonPropertyName("attackBase")]
    public float AttackBase { get; set; }

    [JsonPropertyName("defenseBase")]
    public float DefenseBase { get; set; }

    [JsonPropertyName("physicalSubHurt")]
    public float PhysicalSubHurt { get; set; }

    [JsonPropertyName("fireSubHurt")]
    public float FireSubHurt { get; set; }

    [JsonPropertyName("elecSubHurt")]
    public float ElecSubHurt { get; set; }

    [JsonPropertyName("waterSubHurt")]
    public float WaterSubHurt { get; set; }

    [JsonPropertyName("grassSubHurt")]
    public float GrassSubHurt { get; set; }

    [JsonPropertyName("windSubHurt")]
    public float WindSubHurt { get; set; }

    [JsonPropertyName("rockSubHurt")]
    public float RockSubHurt { get; set; }

    [JsonPropertyName("iceSubHurt")]
    public float IceSubHurt { get; set; }

    [JsonPropertyName("campID")]
    public uint CampId { get; set; }

    [JsonPropertyName("nameTextMapHash")]
    public ulong NameTextMapHash { get; set; }

    [JsonPropertyName("visionLevel")]
    public string VisionLevel { get; set; } = string.Empty;

    [JsonPropertyName("propGrowCurves")]
    public List<PropGrowCurveData> PropGrowCurves { get; set; } = [];

    public Dictionary<FightProperty, float> CalculateFightProperties(
        uint level,
        IReadOnlyDictionary<uint, MonsterCurveData> curves
    )
    {
        var props = new Dictionary<FightProperty, float> {
            [FightProperty.FIGHT_PROP_BASE_HP] = HpBase,
            [FightProperty.FIGHT_PROP_BASE_ATTACK] = AttackBase,
            [FightProperty.FIGHT_PROP_BASE_DEFENSE] = DefenseBase,
            [FightProperty.FIGHT_PROP_PHYSICAL_SUB_HURT] = PhysicalSubHurt,
            [FightProperty.FIGHT_PROP_FIRE_SUB_HURT] = FireSubHurt,
            [FightProperty.FIGHT_PROP_ELEC_SUB_HURT] = ElecSubHurt,
            [FightProperty.FIGHT_PROP_WATER_SUB_HURT] = WaterSubHurt,
            [FightProperty.FIGHT_PROP_GRASS_SUB_HURT] = GrassSubHurt,
            [FightProperty.FIGHT_PROP_WIND_SUB_HURT] = WindSubHurt,
            [FightProperty.FIGHT_PROP_ROCK_SUB_HURT] = RockSubHurt,
            [FightProperty.FIGHT_PROP_ICE_SUB_HURT] = IceSubHurt,
            [FightProperty.FIGHT_PROP_BASE_ELEM_REACT_CRITICAL] = 0f,
            [FightProperty.FIGHT_PROP_BASE_ELEM_REACT_CRITICAL_HURT] = 1f
        };

        if (curves.TryGetValue(level, out var curve))
        {
            foreach (var growCurve in PropGrowCurves)
            {
                if (!FightPropertyExtensions.TryParse(growCurve.Type, out var property) ||
                    property == FightProperty.FIGHT_PROP_NONE ||
                    !props.TryGetValue(property, out var value))
                    continue;

                props[property] = value * curve.GetMultiplier(growCurve.GrowCurve);
            }
        }

        props[FightProperty.FIGHT_PROP_MAX_HP] = Compound(
            props, FightProperty.FIGHT_PROP_BASE_HP, FightProperty.FIGHT_PROP_HP_PERCENT, FightProperty.FIGHT_PROP_HP);

        props[FightProperty.FIGHT_PROP_CUR_ATTACK] = Compound(
            props, FightProperty.FIGHT_PROP_BASE_ATTACK, FightProperty.FIGHT_PROP_ATTACK_PERCENT, FightProperty.FIGHT_PROP_ATTACK);

        props[FightProperty.FIGHT_PROP_CUR_DEFENSE] = Compound(
            props, FightProperty.FIGHT_PROP_BASE_DEFENSE, FightProperty.FIGHT_PROP_DEFENSE_PERCENT, FightProperty.FIGHT_PROP_DEFENSE);
        props[FightProperty.FIGHT_PROP_CUR_HP] = props[FightProperty.FIGHT_PROP_MAX_HP];

        return props;
    }

    private static float Compound(
        IReadOnlyDictionary<FightProperty, float> props,
        FightProperty baseProperty,
        FightProperty percentProperty,
        FightProperty flatProperty
    ) =>
        props.GetValueOrDefault(flatProperty) +
        props.GetValueOrDefault(baseProperty) * (1f + props.GetValueOrDefault(percentProperty));
}

public sealed class MonsterHpDropData
{
    [JsonPropertyName("dropId")]
    public uint DropId { get; set; }

    [JsonPropertyName("hpPercent")]
    public uint HpPercent { get; set; }
}

[GameResource("MonsterDescribeExcelConfigData.json", Priority = LoadPriority.High)]
public sealed class MonsterDescribeData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("nameTextMapHash")]
    public ulong NameTextMapHash { get; set; }

    [JsonPropertyName("titleID")]
    public uint TitleId { get; set; }

    [JsonPropertyName("specialNameLabID")]
    public uint SpecialNameLabId { get; set; }
}

[GameResource("MonsterSpecialNameExcelConfigData.json", Priority = LoadPriority.High)]
public sealed class MonsterSpecialNameData : Data
{
    [JsonPropertyName("specialNameID")]
    public new uint Id { get; set; }

    [JsonPropertyName("specialNameLabID")]
    public uint SpecialNameLabId { get; set; }

    [JsonPropertyName("specialNameTextMapHash")]
    public ulong SpecialNameTextMapHash { get; set; }
}

[GameResource("MonsterAffixExcelConfigData.json")]
public sealed class MonsterAffixData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("isPreAdd")]
    public bool IsPreAdd { get; set; }

    [JsonPropertyName("abilityName")]
    public List<string> AbilityNames { get; set; } = [];
}

[GameResource("SceneExcelConfigData.json")]
public sealed class SceneData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    [JsonPropertyName("levelEntityConfig")]
    public string LevelEntityConfig { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
