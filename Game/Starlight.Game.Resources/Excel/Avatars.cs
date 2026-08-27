using System.Text.Json.Serialization;

namespace Starlight.Game.Resources.Excel;

[GameResource("AvatarExcelConfigData.json")]
public sealed class AvatarData : Data
{
    [JsonPropertyName("id")]
    public new uint Id { get; set; }

    /// Used for extracting the avatar's internal name.
    /// Combined with avatar ability configs to look up the proper data.
    [JsonPropertyName("iconName")]
    public string IconName { get; set; } = string.Empty;

    /// The ID of the default weapon the avatar spawns with.
    [JsonPropertyName("initialWeapon")]
    public uint InitialWeapon { get; set; }

    [JsonPropertyName("skillDepotId")]
    public uint SkillDepotId { get; set; }

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

    /// The internal name of the avatar comes at the end of the string.
    /// <br/>
    /// Example: <c>UI_AvatarIcon_[name]</c>
    public string AvatarName => IconName.Split('_').Last();
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

    [JsonPropertyName("talentStarName")]
    public string TalentStarName { get; set; } = string.Empty;
}

[GameResource("AvatarTalentExcelConfigData.json")]
public sealed class AvatarTalentData : Data
{
    [JsonPropertyName("talentId")]
    public new uint Id { get; set; }

    [JsonPropertyName("openConfig")]
    public string ConfigName { get; set; } = string.Empty;
}
