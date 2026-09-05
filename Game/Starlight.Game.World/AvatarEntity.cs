using Starlight.Game.Player;
using Starlight.Game.Resources;
using Starlight.Protocol;

namespace Starlight.Game.World;

public sealed record AvatarEntity(SceneEntityInfo Info, uint WeaponEntityId)
{
    public uint EntityId => Info.EntityId;

    public static AvatarEntity Create(
        World world,
        uint uid,
        uint peerId,
        Avatar avatar,
        Vector position,
        Vector? rotation = null,
        Vector? refPos = null
    )
    {
        var weaponEntityId = world.NextEntityId(ProtEntityType.PROT_ENTITY_TYPE_WEAPON);
        var inventory = world.Owner.Module<InventoryModule>();

        var weaponItem = inventory.Weapons.FirstOrDefault(w => w.Guid == avatar.WeaponGuid)
                         ?? throw new InvalidOperationException(
                             $"Weapon {avatar.WeaponItemId} with GUID {avatar.WeaponGuid} not found in inventory");

        var sceneAvatar = new SceneAvatarInfo {
            Uid = uid,
            AvatarId = avatar.AvatarId,
            Guid = avatar.Guid,
            PeerId = peerId,
            SkillDepotId = avatar.SkillDepotId,
            BornTime = avatar.BornTime,
            WearingFlycloakId = Avatar.DefaultFlycloak,
            EquipIdList = [avatar.WeaponItemId],
            Weapon = new SceneWeaponInfo {
                EntityId = weaponEntityId,
                GadgetId = weaponItem.GadgetId,
                ItemId = weaponItem.ItemId,
                Guid = avatar.WeaponGuid,
                Level = weaponItem.Level,
                PromoteLevel = weaponItem.PromoteLevel
            }
        };

        avatar.PopulateSceneProgression(sceneAvatar);

        var info = new SceneEntityInfo {
            EntityType = ProtEntityType.PROT_ENTITY_TYPE_AVATAR,
            EntityId = world.NextEntityId(ProtEntityType.PROT_ENTITY_TYPE_AVATAR),
            LifeState = avatar.GetFightProperty(FightProperty.FIGHT_PROP_CUR_HP) <= 0 ? 0u : 1u,
            MotionInfo = new MotionInfo {
                Pos = position,
                Rot = rotation ?? new Vector(),
                Speed = new Vector(),
                RefPos = refPos ?? new Vector(),
                State = MotionState.MOTION_STATE_STANDBY
            },
            EntityClientData = new EntityClientData(),
            EntityAuthorityInfo = new EntityAuthorityInfo {
                AbilityInfo = new AbilitySyncStateInfo(),
                BornPos = new Vector(),
                ClientExtraInfo = new EntityClientExtraInfo { SkillAnchorPosition = new Vector() }
            },
            PropList = [
                new PropPair { Type = (uint)PlayerProperty.Level, PropValue = PlayerProperty.Level.Value(avatar.Level) }
            ],
            Avatar = sceneAvatar
        };

        foreach (var (property, value) in avatar.FightProps)
        {
            info.FightPropList.Add(new FightPropPair { PropType = (uint)property, PropValue = value });
        }

        return new AvatarEntity(info, weaponEntityId);
    }
}
