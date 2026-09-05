using Starlight.Game;
using Starlight.Game.Player;
using Starlight.Game.Resources;
using Starlight.Protocol;

namespace Starlight.Game.World;

public sealed class AvatarEntity : SceneEntity
{
    private AvatarEntity(
        Scene scene,
        SceneEntityInfo info,
        FightPropertyStore fightProperties,
        Avatar avatar,
        uint weaponEntityId
    ) : base(scene, info, fightProperties)
    {
        Avatar = avatar;
        WeaponEntityId = weaponEntityId;
    }

    public Avatar Avatar { get; }
    public uint WeaponEntityId { get; }
    public override uint AuthorityPeerId => Info.Avatar?.PeerId ?? 0;
    public override bool RemoveFromSceneOnDeath => false;

    public static AvatarEntity Create(
        Scene scene,
        uint uid,
        uint peerId,
        Avatar avatar,
        Vector position,
        Vector? rotation = null,
        Vector? refPos = null
    )
    {
        var world = scene.World;
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
            LifeState = avatar.GetFightProperty(FightProperty.FIGHT_PROP_CUR_HP) <= 0 ? 2u : 1u,
            MotionInfo = new MotionInfo {
                Pos = CopyVector(position),
                Rot = CopyVector(rotation),
                Speed = new Vector(),
                RefPos = CopyVector(refPos),
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

        return new AvatarEntity(scene, info, avatar.FightPropertyStore, avatar, weaponEntityId);
    }

    private static Vector CopyVector(Vector? source) =>
        source is null ? new Vector() : new Vector { X = source.X, Y = source.Y, Z = source.Z };
}
