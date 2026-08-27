namespace Starlight.Protocol;

/// <summary>Property ids, which the protocol carries as bare integers rather than an enum.</summary>
public enum PlayerProperty : uint
{
    None = 0,
    Exp = 1001,
    BreakLevel = 1002,
    SatiationVal = 1003,
    SatiationPenaltyTime = 1004,
    GearStartVal = 2001,
    GearStopVal = 2002,
    Level = 4001,
    LastChangeAvatarTime = 10001,
    MaxSpringVolume = 10002,
    CurSpringVolume = 10003,
    IsSpringAutoUse = 10004,
    SpringAutoUsePercent = 10005,
    IsFlyable = 10006,
    IsWeatherLocked = 10007,
    IsGameTimeLocked = 10008,
    IsTransferable = 10009,
    MaxStamina = 10010,
    CurPersistStamina = 10011,
    CurTemporaryStamina = 10012,
    PlayerLevel = 10013,
    PlayerExp = 10014,
    PlayerHcoin = 10015,
    PlayerScoin = 10016,
    PlayerMpSettingType = 10017,
    IsMpModeAvailable = 10018,
    PlayerWorldLevel = 10019,
    PlayerResin = 10020,
    PlayerWaitSubHcoin = 10022,
    PlayerWaitSubScoin = 10023,
    IsOnlyMpWithPsPlayer = 10024,
    PlayerMcoin = 10025,
    PlayerWaitSubMcoin = 10026,
    PlayerLegendaryKey = 10027,
    IsHasFirstShare = 10028,
    PlayerForgePoint = 10029,
    CurClimateMeter = 10035,
    CurClimateType = 10036,
    CurClimateAreaId = 10037,
    CurClimateAreaClimateType = 10038,
    PlayerWorldLevelLimit = 10039,
    PlayerWorldLevelAdjustCd = 10040,
    PlayerLegendaryDailyTaskNum = 10041,
    PlayerHomeCoin = 10042,
    PlayerWaitSubHomeCoin = 10043,
    IsAutoUnlockSpecificEquip = 10044,
    PlayerGcgCoin = 10045,
    PlayerWaitSubGcgCoin = 10046,
    PlayerOnlineTime = 10047,
    IsDiveable = 10048,
    MaxDiveStamina = 10049,
    CurPersistDiveStamina = 10050,
    IsCanPutFiveStarReliquary = 10051,
    IsAutoLockFiveStarReliquary = 10052,
    PlayerRoleCombatCoin = 10053,
    CurPhlogiston = 10054,
    ReliquaryTemporaryExp = 10055
}

public static class PlayerPropertyExtensions
{
    /// Serializes
    public static PropValue Value(this PlayerProperty type, long value) => new() { Type = (uint)type, Val = value, Ival = value };
}
