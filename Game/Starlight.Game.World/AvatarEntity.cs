using Starlight.Game.Player;
using Starlight.Game.Resources;
using Starlight.Protobuf.Core;
using Starlight.Protocol;

namespace Starlight.Game.World;

public sealed record AvatarEntity(
    SceneEntityInfo Info,
    uint WeaponEntityId,
    Avatar Avatar,
    uint Uid,
    uint PeerId
)
{
    public uint EntityId => Info.EntityId;

    public SceneEntityInfo ToProtocol()
    {
        // Info owns scene state only. Mutable avatar state is always materialized from Avatar.
        var info = Info.Clone();

        info.LifeState = Avatar.GetFightProperty(FightProperty.FIGHT_PROP_CUR_HP) <= 0 ? 0u : 1u;
        info.Avatar = Avatar.SceneInfo(Uid, PeerId, WeaponEntityId);

        info.PropList.Clear();

        info.PropList.Add(new PropPair {
            Type = (uint)PlayerProperty.Level,
            PropValue = PlayerProperty.Level.Value(Avatar.Level)
        });

        info.FightPropList.Clear();

        foreach (var (property, value) in Avatar.FightProps)
        {
            info.FightPropList.Add(new FightPropPair {
                PropType = (uint)property,
                PropValue = value
            });
        }

        return info;
    }

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

        var info = new SceneEntityInfo {
            EntityType = ProtEntityType.PROT_ENTITY_TYPE_AVATAR,
            EntityId = world.NextEntityId(ProtEntityType.PROT_ENTITY_TYPE_AVATAR),
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
            }
        };

        return new AvatarEntity(info, weaponEntityId, avatar, uid, peerId);
    }
}
