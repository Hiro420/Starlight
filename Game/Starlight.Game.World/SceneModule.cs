using Serilog;
using Starlight.Game.Ability;
using Starlight.Game.Modules;
using Starlight.Game.Player;
using Starlight.Game.Resources;
using Starlight.Protobuf.Core;
using Starlight.Protobuf.Registry;
using Starlight.Protocol;
using Starlight.Rpc.Proto;

namespace Starlight.Game.World;

public sealed class SceneModule(
    IPlayer player,
    IInvokeForwarder forwarder,
    ProtocolRegistry protocol,
    GameData? data = null
) : IModule
{
    #region Beach Simulator

    // These constants are here until we get a permanent solution
    // scaffolded out and properly implemented.

    private const uint SpawnSceneId = 3;
    private static readonly Vector SpawnPosition = new() { X = 2747, Y = 194, Z = -1719 };

    private readonly List<SceneEntityInfo> _spawned = [];
    private readonly Dictionary<ulong, AvatarEntity> _teamEntities = [];
    private ulong _currentAvatarGuid;

    private MotionInfo? _lastCurrentMotion;

    #endregion

    [Lifecycle(LifecycleEvent.PlayerLogin)]
    public PlayerEnterSceneNotify? OnLogin()
        => player.State.BornState == NetPlayerState.Types.PlayerBornState.Pending ? null : EnterScene();

    [Lifecycle(LifecycleEvent.PlayerBorn)]
    public PlayerEnterSceneNotify OnBorn()
    {
        player.Module<WorldModule>().EnterOwnWorld();
        return EnterScene();
    }

    [Lifecycle(LifecycleEvent.PlayerTeamChanged)]
    public IEnumerable<IMessage> OnTeamChanged()
    {
        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null)
            yield break;

        var teams = player.Module<TeamModule>();
        var team = teams.Current;
        var avatarSwitch = teams.ConsumePendingAvatarSwitch();

        if (avatarSwitch is not null)
        {
            foreach (var message in SwitchAvatar(team, avatarSwitch))
            {
                yield return message;
            }

            yield break;
        }

        var abilities = player.Module<AbilityModule>();
        var inventory = player.Module<InventoryModule>();
        var notification = new SceneTeamUpdateNotify();
        var nextEntities = new Dictionary<ulong, AvatarEntity>();

        var outgoing = _teamEntities.GetValueOrDefault(_currentAvatarGuid);
        var outgoingPos = outgoing?.Info.MotionInfo?.Pos ?? _lastCurrentMotion?.Pos ?? SpawnPosition;
        var outgoingRot = outgoing?.Info.MotionInfo?.Rot ?? _lastCurrentMotion?.Rot ?? new Vector();
        var outgoingRef = outgoing?.Info.MotionInfo?.RefPos ?? _lastCurrentMotion?.RefPos ?? new Vector();

        foreach (var avatar in team.Avatars)
        {
            var isIncomingCurrent = avatar.Guid == team.CurrentAvatarGuid;

            if (!_teamEntities.TryGetValue(avatar.Guid, out var entity))
            {
                var position = isIncomingCurrent ? outgoingPos : SpawnPosition;
                var rotation = isIncomingCurrent ? outgoingRot : new Vector();
                var refPos = isIncomingCurrent ? outgoingRef : new Vector();

                entity = AvatarEntity.Create(
                    scene,
                    player.Uid,
                    module.PeerId,
                    avatar,
                    position,
                    rotation,
                    refPos);
            } else if (isIncomingCurrent && entity.Info.MotionInfo is {} motion)
            {
                motion.Pos = outgoingPos;
                motion.Rot = outgoingRot;
                motion.RefPos = outgoingRef;
            }

            if (entity.Info.MotionInfo is {} standbyMotion)
                standbyMotion.State = MotionState.MOTION_STATE_STANDBY;

            scene.AddEntity(entity);
            nextEntities.Add(avatar.Guid, entity);

            if (!abilities.TryGetComponent(entity.EntityId, out var avatarAbilities))
            {
                avatarAbilities = abilities.RegisterAvatar(
                    module.World.Abilities,
                    new AbilityOwner(entity.EntityId, AbilityOwnerType.Avatar, module.PeerId, player.Uid),
                    avatar.AvatarId,
                    avatar.SkillDepotId,
                    scene.Id,
                    AbilitySources(avatar, inventory),
                    fightProperties: entity.FightProperties);
            } else
            {
                avatarAbilities.BindFightProperties(entity.FightProperties);
            }

            if (!abilities.TryGetComponent(entity.WeaponEntityId, out var weaponAbilities))
            {
                weaponAbilities = abilities.RegisterWeapon(
                    module.World.Abilities,
                    new AbilityOwner(entity.WeaponEntityId, AbilityOwnerType.Weapon, module.PeerId, player.Uid),
                    avatar.WeaponGadgetId);
            }
            entity.Info.EntityAuthorityInfo!.AbilityInfo = AbilityProtocol.ToSyncState(avatarAbilities);

            var isCurrent = avatar.Guid == team.CurrentAvatarGuid;

            notification.SceneTeamAvatarList.Add(new SceneTeamAvatar {
                PlayerUid = player.Uid,
                SceneId = scene.Id,
                AvatarGuid = avatar.Guid,
                EntityId = entity.EntityId,
                WeaponGuid = avatar.WeaponGuid,
                WeaponEntityId = entity.WeaponEntityId,
                AbilityControlBlock = AbilityProtocol.ToControlBlock(avatarAbilities),
                AvatarAbilityInfo = AbilityProtocol.ToSyncState(avatarAbilities),
                WeaponAbilityInfo = AbilityProtocol.ToSyncState(weaponAbilities),
                SceneEntityInfo = entity.Info,
                IsOnScene = isCurrent,
                IsPlayerCurAvatar = isCurrent,
                AvatarInfo = avatar.Info()
            });
        }

        _teamEntities.TryGetValue(_currentAvatarGuid, out var previous);
        nextEntities.TryGetValue(team.CurrentAvatarGuid, out var current);
        var currentChanged = _currentAvatarGuid != team.CurrentAvatarGuid;

        if (current is not null && current.Info.MotionInfo is {} curMotion)
            _lastCurrentMotion = curMotion;

        foreach (var stale in _teamEntities
                     .Where(pair => !nextEntities.ContainsKey(pair.Key))
                     .Select(pair => pair.Value)
                     .ToArray())
        {
            scene.RemoveEntity(stale.EntityId);
            module.World.Abilities.Remove(stale.EntityId);
        }

        _teamEntities.Clear();

        foreach (var (guid, entity) in nextEntities)
        {
            _teamEntities.Add(guid, entity);
        }

        _currentAvatarGuid = team.CurrentAvatarGuid;

        yield return notification;

        if (currentChanged && previous is not null)
        {
            yield return new SceneEntityDisappearNotify {
                DisappearType = VisionType.VISION_TYPE_REPLACE,
                EntityList = { previous.EntityId }
            };
        }

        if (currentChanged && previous is not null && current is not null)
        {
            yield return new SceneEntityAppearNotify {
                AppearType = VisionType.VISION_TYPE_REPLACE,
                Param = previous.EntityId,
                EntityList = { current.Info }
            };
        }
    }

    [Opcode]
    public IEnumerable<IMessage> OnEnterSceneReady(EnterSceneReadyReq msg)
    {
        // TODO: Validate `enter_scene_token`.

        var module = player.Module<WorldModule>();

        // TODO: Fetch player's last scene.
        var scene = module.Scene = module.World.GetScene(SpawnSceneId);

        yield return new EnterScenePeerNotify {
            DestSceneId = scene.Id,
            HostPeerId = module.World.HostPeerId,
            PeerId = module.PeerId,
            EnterSceneToken = msg.EnterSceneToken
        };

        yield return new EnterSceneReadyRsp { EnterSceneToken = msg.EnterSceneToken };
    }

    [Opcode]
    public IEnumerable<IMessage> OnSceneInit(SceneInitFinishReq msg)
    {
        var module = player.Module<WorldModule>();
        var world = module.World;
        var scene = module.Scene!;
        var abilities = player.Module<AbilityModule>();
        var inventory = player.Module<InventoryModule>();
        var teamEntityId = world.TeamEntityIdOf(player);
        var levelEntityId = world.LevelEntityId;

        abilities.RegisterScene(
            world.Abilities,
            scene.Id,
            new AbilityOwner(AbilityEntityIds.Scene, AbilityOwnerType.Scene));

        var teamAbilities = abilities.RegisterTeam(
            world.Abilities,
            new AbilityOwner(teamEntityId, AbilityOwnerType.Team, module.PeerId, player.Uid),
            scene.Id);

        var level = abilities.RegisterMpLevel(
            world.Abilities,
            new AbilityOwner(levelEntityId, AbilityOwnerType.MpLevel, world.HostPeerId));

        var enterInfo = new PlayerEnterSceneInfoNotify {
            EnterSceneToken = msg.EnterSceneToken,
            TeamEnterInfo = new TeamEnterSceneInfo {
                AbilityControlBlock = AbilityProtocol.ToControlBlock(teamAbilities),
                TeamAbilityInfo = AbilityProtocol.ToSyncState(teamAbilities),
                TeamEntityId = teamEntityId
            },
            MpLevelEntityInfo = new MPLevelEntityInfo {
                EntityId = levelEntityId,
                AbilityInfo = AbilityProtocol.ToSyncState(level),
                AuthorityPeerId = world.HostPeerId
            }
        };

        var teamUpdate = new SceneTeamUpdateNotify();
        var team = player.Module<TeamModule>().Current;

        _spawned.Clear();

        foreach (var existing in _teamEntities.Values)
        {
            scene.RemoveEntity(existing.EntityId);
        }

        _teamEntities.Clear();
        _currentAvatarGuid = team.CurrentAvatarGuid;
        _lastCurrentMotion = null;

        foreach (var avatar in team.Avatars)
        {
            var entity = AvatarEntity.Create(scene, player.Uid, module.PeerId, avatar, SpawnPosition);
            scene.AddEntity(entity);
            _teamEntities.Add(avatar.Guid, entity);

            var avatarAbilities = abilities.RegisterAvatar(
                world.Abilities,
                new AbilityOwner(entity.EntityId, AbilityOwnerType.Avatar, module.PeerId, player.Uid),
                avatar.AvatarId,
                avatar.SkillDepotId,
                scene.Id,
                AbilitySources(avatar, inventory),
                fightProperties: entity.FightProperties);

            var weaponAbilities = abilities.RegisterWeapon(
                world.Abilities,
                new AbilityOwner(entity.WeaponEntityId, AbilityOwnerType.Weapon, module.PeerId, player.Uid),
                avatar.WeaponGadgetId);
            entity.Info.EntityAuthorityInfo!.AbilityInfo = AbilityProtocol.ToSyncState(avatarAbilities);

            var isCurrent = avatar.Guid == team.CurrentAvatarGuid;

            if (isCurrent)
            {
                enterInfo.CurAvatarEntityId = entity.EntityId;
                _spawned.Add(entity.Info);
                _lastCurrentMotion = entity.Info.MotionInfo;
            }

            enterInfo.AvatarEnterInfo.Add(new AvatarEnterSceneInfo {
                AvatarGuid = avatar.Guid,
                AvatarEntityId = entity.EntityId,
                WeaponGuid = avatar.WeaponGuid,
                WeaponEntityId = entity.WeaponEntityId,
                AvatarAbilityInfo = AbilityProtocol.ToSyncState(avatarAbilities),
                WeaponAbilityInfo = AbilityProtocol.ToSyncState(weaponAbilities)
            });

            teamUpdate.SceneTeamAvatarList.Add(new SceneTeamAvatar {
                PlayerUid = player.Uid,
                SceneId = scene.Id,
                AvatarGuid = avatar.Guid,
                EntityId = entity.EntityId,
                WeaponGuid = avatar.WeaponGuid,
                WeaponEntityId = entity.WeaponEntityId,
                AbilityControlBlock = AbilityProtocol.ToControlBlock(avatarAbilities),
                AvatarAbilityInfo = AbilityProtocol.ToSyncState(avatarAbilities),
                WeaponAbilityInfo = AbilityProtocol.ToSyncState(weaponAbilities),
                SceneEntityInfo = entity.Info,
                IsOnScene = isCurrent,
                IsPlayerCurAvatar = isCurrent
            });
        }

        yield return enterInfo;
        yield return teamUpdate;
        yield return new SceneInitFinishRsp { EnterSceneToken = msg.EnterSceneToken };
    }

    [Opcode]
    public IEnumerable<IMessage> OnEnterSceneDone(EnterSceneDoneReq msg)
    {
        // TODO: Validate `enter_scene_token`.

        var scene = player.Module<WorldModule>().Scene;
        IEnumerable<SceneEntityInfo> entities = _spawned;

        if (scene is not null)
            entities = entities.Concat(scene.Monsters.Values.Select(monster => monster.Info));

        yield return new SceneEntityAppearNotify {
            AppearType = VisionType.VISION_TYPE_BORN,
            EntityList = [.. entities]
        };

        yield return new EnterSceneDoneRsp { EnterSceneToken = msg.EnterSceneToken };
    }

    [Opcode]
    public async Task OnCombatInvocations(CombatInvocationsNotify notify)
    {
        var pendingHits = new List<AttackResult>();

        foreach (var invoke in notify.InvokeList)
        {
            switch (invoke.ArgumentType)
            {
                case CombatTypeArgument.COMBAT_TYPE_ARGUMENT_ENTITY_MOVE:
                    HandleEntityMove(invoke.CombatData);
                    break;
                case CombatTypeArgument.COMBAT_TYPE_ARGUMENT_EVT_BEING_HIT:
                    if (TryDecode(invoke.CombatData, out EvtBeingHitInfo hit) && hit.AttackResult is not null)
                        pendingHits.Add(hit.AttackResult);
                    break;
                case CombatTypeArgument.COMBAT_TYPE_ARGUMENT_SET_ATTACK_TARGET:
                    HandleSetAttackTarget(invoke.CombatData);
                    break;
                case CombatTypeArgument.COMBAT_TYPE_ARGUMENT_ANIMATOR_PARAMETER_CHANGED:
                    HandleAnimatorParameter(invoke.CombatData);
                    break;
                case CombatTypeArgument.COMBAT_TYPE_ARGUMENT_BEING_HEALED_NTF:
                    HandleBeingHealed(invoke.CombatData);
                    break;
                case CombatTypeArgument.COMBAT_TYPE_ARGUMENT_SKILL_ANCHOR_POSITION_NTF:
                    HandleSkillAnchorPosition(invoke.CombatData);
                    break;
                default:
                    Log.Debug("Unhandled combat invoke: ArgumentType={ArgumentType}", invoke.ArgumentType);
                    break;
            }
        }

        foreach (var group in notify.InvokeList.GroupBy(invoke => invoke.ForwardType))
        {
            // TODO: CombatInvokeEntry carries no forward_peer. With co-op we'll need to
            // map the targeted peer here for FORWARD_TYPE_TO_PEER / FORWARD_TYPE_TO_PEERS.
            await forwarder.Forward(
                player,
                group.Key,
                new CombatInvocationsNotify { InvokeList = [.. group] },
                forwardPeer: 0);
        }

        foreach (var attack in pendingHits)
        {
            await HandleAttack(attack);
        }
    }

    private void HandleEntityMove(Google.Protobuf.ByteString data)
    {
        if (!TryDecode(data, out EntityMoveInfo move))
            return;

        TryApplyEntityMove(move, requireAuthority: true, out _);
    }

    private IEnumerable<IMessage> SwitchAvatar(PlayerTeam team, AvatarSwitchContext avatarSwitch)
    {
        if (avatarSwitch.Guid != team.CurrentAvatarGuid || _currentAvatarGuid == team.CurrentAvatarGuid)
            yield break;

        if (!_teamEntities.TryGetValue(_currentAvatarGuid, out var previous) ||
            !_teamEntities.TryGetValue(team.CurrentAvatarGuid, out var current))
        {
            // The scene is not fully materialized yet
            _currentAvatarGuid = team.CurrentAvatarGuid;
            yield break;
        }

        if (previous.Info.MotionInfo is not {} previousMotion ||
            current.Info.MotionInfo is not {} currentMotion)
            yield break;

        previousMotion.State = MotionState.MOTION_STATE_STANDBY;

        currentMotion.Pos = avatarSwitch.IsMove && avatarSwitch.MovePos is {} movePos ? CopyVector(movePos) : CopyVector(previousMotion.Pos);
        currentMotion.Rot = CopyVector(previousMotion.Rot);
        currentMotion.Speed = new Vector();
        currentMotion.RefPos = new Vector();

        _lastCurrentMotion = currentMotion;
        _currentAvatarGuid = team.CurrentAvatarGuid;

        yield return new SceneEntityDisappearNotify {
            DisappearType = VisionType.VISION_TYPE_REPLACE,
            EntityList = { previous.EntityId }
        };

        yield return new SceneEntityAppearNotify {
            AppearType = VisionType.VISION_TYPE_REPLACE,
            Param = previous.EntityId,
            EntityList = { current.Info }
        };
    }

    private static Vector CopyVector(Vector? source) =>
        source is null ? new Vector() : new Vector { X = source.X, Y = source.Y, Z = source.Z };

    private void HandleSetAttackTarget(Google.Protobuf.ByteString data)
    {
        if (!TryDecode(data, out EvtSetAttackTargetInfo target))
            return;

        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null || !scene.TryGetEntity(target.EntityId, out var entity))
            return;

        // Attack-target state lives on GameEntity in Grasscutter. Keep it generic here too;
        // authority still has to belong to the peer sending the invoke.
        if (entity.AuthorityPeerId != 0 && entity.AuthorityPeerId != module.PeerId)
            return;

        entity.AttackTargetId = target.AttackTargetId;
    }

    [Opcode]
    public async Task OnSceneEntityMove(SceneEntityMoveReq msg)
    {
        var move = new EntityMoveInfo {
            EntityId = msg.EntityId,
            MotionInfo = msg.MotionInfo,
            SceneTime = msg.SceneTime,
            ReliableSeq = msg.ReliableSeq,
            IsReliable = msg.ReliableSeq != 0
        };

        if (!TryApplyEntityMove(move, requireAuthority: true, out var currentMotion))
        {
            await player.Send(new SceneEntityMoveRsp {
                EntityId = msg.EntityId,
                SceneTime = msg.SceneTime,
                ReliableSeq = msg.ReliableSeq,
                Retcode = -1,
                FailMotion = currentMotion
            });
            return;
        }

        await BroadcastSceneExceptPlayer(new SceneEntityMoveNotify {
            EntityId = msg.EntityId,
            SceneTime = msg.SceneTime,
            ReliableSeq = msg.ReliableSeq,
            MotionInfo = CopyMotion(msg.MotionInfo)
        });
    }

    [Opcode]
    public async Task<SceneEntitiesMovesRsp> OnSceneEntitiesMoves(SceneEntitiesMovesReq msg)
    {
        var response = new SceneEntitiesMovesRsp();
        var accepted = new List<EntityMoveInfo>();

        foreach (var move in msg.EntityMoveInfoList)
        {
            if (TryApplyEntityMove(move, requireAuthority: true, out var currentMotion))
            {
                accepted.Add(move);
                continue;
            }

            response.EntityMoveFailInfoList.Add(new EntityMoveFailInfo {
                EntityId = move.EntityId,
                SceneTime = move.SceneTime,
                ReliableSeq = move.ReliableSeq,
                Retcode = -1,
                FailMotion = currentMotion
            });
        }

        foreach (var move in accepted)
        {
            await BroadcastSceneExceptPlayer(new SceneEntityMoveNotify {
                EntityId = move.EntityId,
                SceneTime = move.SceneTime,
                ReliableSeq = move.ReliableSeq,
                MotionInfo = CopyMotion(move.MotionInfo)
            });
        }

        return response;
    }

    [Opcode]
    public void OnEvtAiSyncSkillCd(EvtAiSyncSkillCdNotify notify)
    {
        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null || module.PeerId != module.World.HostPeerId)
            return;

        foreach (var (entityId, cdInfo) in notify.AiCdMap)
        {
            if (scene.Monsters.TryGetValue(entityId, out var monster))
                monster.SyncAiSkillCooldowns(cdInfo);
        }
    }

    [Opcode]
    public void OnEvtAiSyncCombatThreat(EvtAiSyncCombatThreatInfoNotify notify)
    {
        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null || module.PeerId != module.World.HostPeerId)
            return;

        foreach (var (entityId, threatInfo) in notify.CombatThreatInfoMap)
        {
            if (scene.Monsters.TryGetValue(entityId, out var monster))
                monster.SyncAiThreat(threatInfo);
        }
    }

    [Opcode]
    public void OnMonsterAlertChange(MonsterAlertChangeNotify notify)
    {
        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null || module.PeerId != module.World.HostPeerId)
            return;

        var alert = notify.IsAlert != 0;

        foreach (var entityId in notify.MonsterEntityList)
        {
            if (!scene.Monsters.TryGetValue(entityId, out var monster))
                continue;

            monster.SetAlert(alert);

            if (alert && notify.AvatarEntityId != 0)
                monster.AttackTargetId = notify.AvatarEntityId;
            else if (!alert)
                monster.AttackTargetId = 0;
        }
    }

    private async Task HandleAttack(AttackResult attack)
    {
        var scene = player.Module<WorldModule>().Scene;

        if (scene is null)
            return;

        var result = scene.HandleAttack(attack);

        if (!result.Handled || result.Target is not {} target)
            return;

        var damage = result.Damage;

        await BroadcastScene(new EntityFightPropUpdateNotify {
            EntityId = target.EntityId,
            FightPropMap = {
                [(uint)FightProperty.FIGHT_PROP_CUR_HP] = damage.CurrentHp
            }
        });

        if (damage.ClearedHpDebt != 0f)
        {
            await BroadcastScene(new EntityFightPropUpdateNotify {
                EntityId = target.EntityId,
                FightPropMap = {
                    [(uint)FightProperty.FIGHT_PROP_CUR_HP_DEBTS] = 0f
                }
            });
        }

        if (damage.Died)
            await KillEntity(scene, target, result.AttackerId);
    }

    private async Task KillEntity(Scene scene, SceneEntity target, uint sourceEntityId)
    {
        // Damage() already transitions the entity to dead. Keep this method usable by
        // future non-damage kill paths as well.
        target.Info.LifeState = 2;

        if (scene.World.Abilities.TryGet(target.EntityId, out var component))
            component.SetKilled(true);

        await BroadcastScene(new LifeStateChangeNotify {
            EntityId = target.EntityId,
            LifeState = 2,
            SourceEntityId = sourceEntityId,
            MoveReliableSeq = target.LastMoveReliableSeq
        });

        target.OnDeath(sourceEntityId);

        // Grasscutter keeps dead avatars registered and removes non-avatar entities.
        if (!target.RemoveFromSceneOnDeath)
            return;

        await BroadcastScene(new SceneEntityDisappearNotify {
            DisappearType = VisionType.VISION_TYPE_DIE,
            EntityList = { target.EntityId }
        });

        scene.RemoveEntity(target.EntityId);
        scene.World.Abilities.Remove(target.EntityId);

        if (target is MonsterEntity { WeaponEntityId: not 0 } monster)
            scene.World.Abilities.Remove(monster.WeaponEntityId);
    }

    private bool TryApplyEntityMove(EntityMoveInfo move, bool requireAuthority, out MotionInfo currentMotion)
    {
        currentMotion = new MotionInfo();

        if (move.EntityId == 0 || move.MotionInfo is not {} incoming)
            return false;

        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null || !scene.TryGetEntity(move.EntityId, out var entity) ||
            entity.MotionInfo is not {} motion)
            return false;

        currentMotion = CopyMotion(motion);

        if (requireAuthority && entity.AuthorityPeerId != 0 && entity.AuthorityPeerId != module.PeerId)
            return false;

        CopyMotionInto(motion, incoming);
        entity.LastMoveReliableSeq = move.ReliableSeq;
        entity.LastMoveSceneTimeMs = move.SceneTime;
        currentMotion = CopyMotion(motion);

        if (entity is AvatarEntity avatar && avatar.EntityId == _teamEntities.GetValueOrDefault(_currentAvatarGuid)?.EntityId)
            _lastCurrentMotion = motion;

        return true;
    }

    private Task BroadcastScene(IMessage message)
    {
        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null)
            return Task.CompletedTask;

        return Task.WhenAll(
            module.World.Peers.Values
                .Where(peer => peer.Module<WorldModule>().Scene?.Id == scene.Id)
                .Select(peer => peer.Send(message)));
    }

    private Task BroadcastSceneExceptPlayer(IMessage message)
    {
        var module = player.Module<WorldModule>();
        var scene = module.Scene;

        if (scene is null)
            return Task.CompletedTask;

        return Task.WhenAll(
            module.World.Peers.Values
                .Where(peer => !ReferenceEquals(peer, player) && peer.Module<WorldModule>().Scene?.Id == scene.Id)
                .Select(peer => peer.Send(message)));
    }

    private static MotionInfo CopyMotion(MotionInfo? source)
    {
        if (source is null)
            return new MotionInfo();

        var copy = new MotionInfo {
            Pos = CopyVector(source.Pos),
            Rot = CopyVector(source.Rot),
            Speed = CopyVector(source.Speed),
            RefPos = CopyVector(source.RefPos),
            RefId = source.RefId,
            State = source.State,
            SceneTime = source.SceneTime,
            IntervalVelocity = source.IntervalVelocity
        };

        foreach (var param in source.Params)
        {
            copy.Params.Add(CopyVector(param));
        }

        return copy;
    }

    private static void CopyMotionInto(MotionInfo target, MotionInfo source)
    {
        target.Pos = CopyVector(source.Pos);
        target.Rot = CopyVector(source.Rot);
        target.Speed = CopyVector(source.Speed);
        target.RefPos = CopyVector(source.RefPos);
        target.RefId = source.RefId;
        target.State = source.State;
        target.SceneTime = source.SceneTime;
        target.IntervalVelocity = source.IntervalVelocity;
        target.Params.Clear();

        foreach (var param in source.Params)
        {
            target.Params.Add(CopyVector(param));
        }
    }

    private void HandleAnimatorParameter(Google.Protobuf.ByteString data)
    {
        // TODO: Handle EvtAnimatorParameterInfo.
    }

    private void HandleBeingHealed(Google.Protobuf.ByteString data)
    {
        // TODO: Handle EvtBeingHealedNotify.
    }

    private void HandleSkillAnchorPosition(Google.Protobuf.ByteString data)
    {
        // TODO: Handle EvtSyncSkillAnchorPosition.
    }

    private bool TryDecode<T>(Google.Protobuf.ByteString data, out T message)
        where T : class, ISelfSerializable<T>, new()
    {
        message = new T();

        try
        {
            using var input = data.CreateCodedInput();

            if (protocol.GetDescriptor(typeof(T)) is not null)
                protocol.Deserialize(message, input);
            else
                T.Serializer.Deserialize(message, input);

            return true;
        }
        catch (Exception)
        {
            message = null!;
            return false;
        }
    }

    public async Task<MonsterEntity> SpawnMonster(uint monsterId, uint level)
    {
        var gameData = data ?? throw new InvalidOperationException("Game data is unavailable.");
        var module = player.Module<WorldModule>();
        var scene = module.Scene ?? throw new InvalidOperationException("Player is not in a scene.");

        if (!gameData.MonsterData.TryGetValue(monsterId, out var monster))
            throw new KeyNotFoundException($"Monster {monsterId} does not exist in resources.");

        if (!gameData.MonsterCurveData.ContainsKey(level))
            throw new ArgumentOutOfRangeException(nameof(level), level, "Monster level curve does not exist.");

        var current = _teamEntities.GetValueOrDefault(_currentAvatarGuid);

        var motion = current?.Info.MotionInfo ?? _lastCurrentMotion
            ?? throw new InvalidOperationException("Current avatar is not materialized in the scene.");

        var position = CopyVector(motion.Pos);
        position.Y += 3f;
        var rotation = CopyVector(motion.Rot);

        var entity = MonsterEntity.Create(scene, gameData, monster, level, position, rotation);
        var abilities = player.Module<AbilityModule>();

        var monsterAbilities = abilities.RegisterMonster(
            module.World.Abilities,
            new AbilityOwner(entity.EntityId, AbilityOwnerType.Monster, module.World.HostPeerId),
            monsterId,
            scene.Id,
            fightProperties: entity.FightProperties);
        entity.Info.EntityAuthorityInfo!.AbilityInfo = AbilityProtocol.ToSyncState(monsterAbilities);

        if (entity.WeaponEntityId != 0 && entity.WeaponGadgetId != 0)
        {
            var weaponAbilities = abilities.RegisterWeapon(
                module.World.Abilities,
                new AbilityOwner(entity.WeaponEntityId, AbilityOwnerType.Weapon, module.World.HostPeerId),
                entity.WeaponGadgetId);

            if (entity.Info.Monster?.WeaponList.Count > 0)
                entity.Info.Monster.WeaponList[0].AbilityInfo = AbilityProtocol.ToSyncState(weaponAbilities);
        }

        scene.AddEntity(entity);

        var notification = new SceneEntityAppearNotify {
            AppearType = VisionType.VISION_TYPE_BORN,
            EntityList = { entity.Info }
        };

        var recipients = module.World.Peers.Values
            .Where(peer => peer.Module<WorldModule>().Scene?.Id == scene.Id)
            .Select(peer => peer.Send(notification));

        await Task.WhenAll(recipients);
        return entity;
    }

    [Opcode]
    public PostEnterSceneRsp OnPostEnterScene(PostEnterSceneReq msg) =>
        // TODO: Validate `enter_scene_token`.
        new() { EnterSceneToken = msg.EnterSceneToken };

    private static AvatarAbilitySources AbilitySources(Avatar avatar, InventoryModule inventory)
    {
        inventory.TryGetWeapon(avatar.WeaponGuid, out var weapon);

        return new AvatarAbilitySources(
            avatar.Talents,
            avatar.PromoteLevel,
            weapon?.AffixId ?? 0,
            weapon?.Refinement ?? 1);
    }

    private static PlayerEnterSceneNotify EnterScene() => new() {
        Type = EnterType.ENTER_TYPE_ENTER_SELF,
        SceneId = SpawnSceneId,
        Pos = SpawnPosition
    };
}
