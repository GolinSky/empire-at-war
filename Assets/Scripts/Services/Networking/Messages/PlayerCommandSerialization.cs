using System;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.ShipAbilities;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>
    /// Mirror reader/writer extensions for the read-only command structs (the weaver only handles public fields).
    /// <see cref="PlayerCommandMessage"/> writes its kind and then only that command.
    /// </summary>
    public static class PlayerCommandSerialization
    {
        public static void WritePlayerId(this NetworkWriter writer, PlayerId value) =>
            writer.WriteInt(value.IsNone ? -1 : value.Index);

        public static PlayerId ReadPlayerId(this NetworkReader reader)
        {
            int index = reader.ReadInt();
            return index < 0 ? PlayerId.None : new PlayerId(index);
        }

        public static void WritePlayerCommandMessage(this NetworkWriter writer, PlayerCommandMessage message)
        {
            writer.WriteByte((byte)message.Kind);
            writer.WriteUInt(message.LastReceivedTick);
            switch (message.Kind)
            {
                case PlayerCommandKind.UnitOrder: writer.WriteUnitOrderCommand(message.UnitOrder); break;
                case PlayerCommandKind.Purchase: writer.WritePurchaseUnitCommand(message.Purchase); break;
                case PlayerCommandKind.CancelPurchase: writer.WriteCancelPurchaseCommand(message.CancelPurchase); break;
                case PlayerCommandKind.BuildSiteFacility: writer.WriteBuildSiteFacilityCommand(message.BuildSiteFacility); break;
                case PlayerCommandKind.FireSuperWeapon: writer.WriteFireSuperWeaponCommand(message.FireSuperWeapon); break;
                case PlayerCommandKind.ShipAbility: writer.WriteShipAbilityCommand(message.ShipAbility); break;
                case PlayerCommandKind.DeployReinforcement: writer.WriteDeployReinforcementCommand(message.DeployReinforcement); break;
                default: throw new ArgumentOutOfRangeException(nameof(message), message.Kind, "Unknown command kind.");
            }
        }

        public static PlayerCommandMessage ReadPlayerCommandMessage(this NetworkReader reader)
        {
            PlayerCommandMessage message = new PlayerCommandMessage
            {
                Kind = (PlayerCommandKind)reader.ReadByte(),
                LastReceivedTick = reader.ReadUInt()
            };
            switch (message.Kind)
            {
                case PlayerCommandKind.UnitOrder: message.UnitOrder = reader.ReadUnitOrderCommand(); break;
                case PlayerCommandKind.Purchase: message.Purchase = reader.ReadPurchaseUnitCommand(); break;
                case PlayerCommandKind.CancelPurchase: message.CancelPurchase = reader.ReadCancelPurchaseCommand(); break;
                case PlayerCommandKind.BuildSiteFacility: message.BuildSiteFacility = reader.ReadBuildSiteFacilityCommand(); break;
                case PlayerCommandKind.FireSuperWeapon: message.FireSuperWeapon = reader.ReadFireSuperWeaponCommand(); break;
                case PlayerCommandKind.ShipAbility: message.ShipAbility = reader.ReadShipAbilityCommand(); break;
                case PlayerCommandKind.DeployReinforcement: message.DeployReinforcement = reader.ReadDeployReinforcementCommand(); break;
                default: throw new ArgumentOutOfRangeException(nameof(reader), message.Kind, "Unknown command kind.");
            }

            return message;
        }

        public static void WriteUnitOrderCommand(this NetworkWriter writer, UnitOrderCommand command)
        {
            writer.WritePlayerId(command.Issuer);
            writer.WriteByte((byte)command.Action);
            writer.WriteArray(command.ReceiverIds);
            writer.WriteVector3(command.Point);
            writer.WriteLong(command.TargetId);
            writer.WriteInt(command.TargetHardPointId);
            writer.WriteArray(command.Waypoints);
        }

        public static UnitOrderCommand ReadUnitOrderCommand(this NetworkReader reader) => new UnitOrderCommand(
            reader.ReadPlayerId(), (UnitActionId)reader.ReadByte(), reader.ReadArray<long>(), reader.ReadVector3(),
            reader.ReadLong(), reader.ReadInt(), reader.ReadArray<Vector3>());

        public static void WritePurchaseUnitCommand(this NetworkWriter writer, PurchaseUnitCommand command)
        {
            writer.WritePlayerId(command.Issuer);
            writer.WriteByte((byte)command.Kind);
            writer.WriteInt(command.Key);
            writer.WriteInt(command.Tier);
        }

        public static PurchaseUnitCommand ReadPurchaseUnitCommand(this NetworkReader reader) => new PurchaseUnitCommand(
            reader.ReadPlayerId(), (UnitRequestKind)reader.ReadByte(), reader.ReadInt(), reader.ReadInt());

        public static void WriteCancelPurchaseCommand(this NetworkWriter writer, CancelPurchaseCommand command)
        {
            writer.WritePlayerId(command.Issuer);
            writer.WriteString(command.RequestId);
        }

        public static CancelPurchaseCommand ReadCancelPurchaseCommand(this NetworkReader reader) =>
            new CancelPurchaseCommand(reader.ReadPlayerId(), reader.ReadString());

        public static void WriteBuildSiteFacilityCommand(this NetworkWriter writer, BuildSiteFacilityCommand command)
        {
            writer.WritePlayerId(command.Issuer);
            writer.WriteInt(command.SiteIndex);
            writer.WriteInt((int)command.FacilityType);
        }

        public static BuildSiteFacilityCommand ReadBuildSiteFacilityCommand(this NetworkReader reader) =>
            new BuildSiteFacilityCommand(reader.ReadPlayerId(), reader.ReadInt(), (SiteFacilityType)reader.ReadInt());

        public static void WriteFireSuperWeaponCommand(this NetworkWriter writer, FireSuperWeaponCommand command)
        {
            writer.WritePlayerId(command.Issuer);
            writer.WriteInt((int)command.Type);
            writer.WriteLong(command.TargetId);
        }

        public static FireSuperWeaponCommand ReadFireSuperWeaponCommand(this NetworkReader reader) =>
            new FireSuperWeaponCommand(reader.ReadPlayerId(), (SuperWeaponType)reader.ReadInt(), reader.ReadLong());

        public static void WriteShipAbilityCommand(this NetworkWriter writer, ShipAbilityCommand command)
        {
            writer.WritePlayerId(command.Issuer);
            writer.WriteInt((int)command.AbilityId);
            writer.WriteArray(command.CasterIds);
            writer.WriteLong(command.TargetId);
            writer.WriteBool(command.Cancel);
        }

        public static ShipAbilityCommand ReadShipAbilityCommand(this NetworkReader reader) => new ShipAbilityCommand(
            reader.ReadPlayerId(), (ShipAbilityId)reader.ReadInt(), reader.ReadArray<long>(), reader.ReadLong(),
            reader.ReadBool());

        public static void WriteDeployReinforcementCommand(this NetworkWriter writer, DeployReinforcementCommand command)
        {
            writer.WritePlayerId(command.Issuer);
            writer.WriteByte((byte)command.Kind);
            writer.WriteInt(command.Key);
            writer.WriteArray(command.Slots);
        }

        public static DeployReinforcementCommand ReadDeployReinforcementCommand(this NetworkReader reader) =>
            new DeployReinforcementCommand(reader.ReadPlayerId(), (UnitRequestKind)reader.ReadByte(), reader.ReadInt(),
                reader.ReadArray<Vector3>());
    }
}
