using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Host-authoritative stand-in for the battle sim: variable-dt Update, fixed 20 Hz network tick.
    public sealed class SpikeHostSimulation
    {
        public const float TICK_INTERVAL = 1f / 20f;
        public const byte OBSERVER_SIDE = byte.MaxValue;

        private const float SPAWN_DISTANCE = 1500f;
        private const float WANDER_RADIUS = 400f;
        private const float CAPITAL_FRACTION = 0.2f;
        private const float MOVING_REFRESH_SECONDS = 1f;
        private const float IDLE_REFRESH_SECONDS = 3f;
        private const float HEADING_THRESHOLD = 5f;
        private const int CHUNK_BYTE_BUDGET = 1000;
        private const int CHUNK_HEADER_BYTES = 32;
        private const int UNIT_STATE_BYTES = 10;

        private readonly List<SpikeUnit> _units = new List<SpikeUnit>();
        private readonly Vector2[] _sideAnchors = new Vector2[2];
        private readonly SpikeKinematics[] _lastSent;
        private readonly double[] _lastSentTime;
        private readonly Queue<SpikeQueuedCommand> _commands = new Queue<SpikeQueuedCommand>();
        private readonly Dictionary<int, byte> _connectionSides = new Dictionary<int, byte>();
        private readonly List<SpikeUnitState> _pending = new List<SpikeUnitState>();
        private readonly System.Random _random = new System.Random(1234);
        private readonly SpikeSyncMode _mode;
        private readonly float _errorThreshold;
        private readonly int _unitsPerChunk;

        private float _accumulator;
        private uint _tick;

        public SpikeHostSimulation(int unitsPerSide, SpikeSyncMode mode, float errorThreshold)
        {
            _mode = mode;
            _errorThreshold = errorThreshold;
            int maxPacket = Transport.active.GetMaxPacketSize(Channels.Unreliable);
            int budget = Mathf.Min(CHUNK_BYTE_BUDGET, maxPacket - CHUNK_HEADER_BYTES);
            _unitsPerChunk = (budget - CHUNK_HEADER_BYTES) / UNIT_STATE_BYTES;

            for (byte side = 0; side < 2; side++)
            {
                _sideAnchors[side] = new Vector2(0f, side == 0 ? -SPAWN_DISTANCE : SPAWN_DISTANCE);
                int capitals = Mathf.RoundToInt(unitsPerSide * CAPITAL_FRACTION);
                for (int i = 0; i < unitsPerSide; i++)
                {
                    SpikeUnitKind kind = i < capitals ? SpikeUnitKind.Capital : SpikeUnitKind.Squadron;
                    Vector2 position = _sideAnchors[side] + RandomOffset(WANDER_RADIUS);
                    SpikeUnit unit = new SpikeUnit(side, kind, position, side == 0 ? 0f : 180f);
                    unit.Target = _sideAnchors[side] + RandomOffset(WANDER_RADIUS);
                    _units.Add(unit);
                }
            }

            _lastSent = new SpikeKinematics[_units.Count];
            _lastSentTime = new double[_units.Count];
            ForceFullResend();
        }

        public SpikeSyncMode Mode => _mode;
        public int UnitCount => _units.Count;
        public int UnitsPerChunk => _unitsPerChunk;
        public uint CurrentTick => _tick;
        public SpikeDeadReckoningStats Stats { get; } = new SpikeDeadReckoningStats();

        public byte AssignSide(NetworkConnectionToClient connection)
        {
            byte side = OBSERVER_SIDE;
            for (byte candidate = 0; candidate < 2 && side == OBSERVER_SIDE; candidate++)
            {
                if (!_connectionSides.ContainsValue(candidate))
                    side = candidate;
            }

            _connectionSides[connection.connectionId] = side;
            return side;
        }

        public void ReleaseSide(NetworkConnectionToClient connection) => _connectionSides.Remove(connection.connectionId);

        public SpikeWelcomeMessage CreateWelcome(byte side)
        {
            byte[] sides = new byte[_units.Count];
            SpikeUnitKind[] kinds = new SpikeUnitKind[_units.Count];
            for (int i = 0; i < _units.Count; i++)
            {
                sides[i] = _units[i].Side;
                kinds[i] = _units[i].Kind;
            }

            return new SpikeWelcomeMessage { Side = side, Mode = _mode, UnitSides = sides, UnitKinds = kinds };
        }

        public void EnqueueCommand(NetworkConnectionToClient connection, SpikeCommandMessage command) =>
            _commands.Enqueue(new SpikeQueuedCommand(connection, command));

        // A joining client needs every unit once: the next tick carries a keyframe for every unit.
        public void ForceFullResend()
        {
            for (int i = 0; i < _lastSentTime.Length; i++)
                _lastSentTime[i] = double.MinValue;
        }

        public void Update(float deltaTime)
        {
            foreach (SpikeUnit unit in _units)
            {
                if (unit.HasArrived)
                    unit.Target = _sideAnchors[unit.Side] + RandomOffset(WANDER_RADIUS);
                unit.Tick(deltaTime);
            }

            _accumulator += deltaTime;
            while (_accumulator >= TICK_INTERVAL)
            {
                _accumulator -= TICK_INTERVAL;
                RunNetworkTick();
            }
        }

        private void RunNetworkTick()
        {
            _tick++;
            ApplyCommands();
            CollectChanges(NetworkTime.time);
            SendChunks(NetworkTime.time);
        }

        private void ApplyCommands()
        {
            while (_commands.Count > 0)
            {
                SpikeQueuedCommand queued = _commands.Dequeue();
                SpikeCommandMessage command = queued.Command;
                bool accepted = _connectionSides.TryGetValue(queued.Connection.connectionId, out byte side)
                    && side != OBSERVER_SIDE
                    && side == command.Side;
                if (accepted)
                {
                    _sideAnchors[side] = new Vector2(
                        SpikeQuantization.DecodePosition(command.TargetX),
                        SpikeQuantization.DecodePosition(command.TargetZ));
                    foreach (SpikeUnit unit in _units)
                    {
                        if (unit.Side == side)
                            unit.Target = _sideAnchors[side] + RandomOffset(WANDER_RADIUS * 0.5f);
                    }
                }

                queued.Connection.Send(new SpikeCommandAckMessage { Sequence = command.Sequence, AppliedTick = _tick, Accepted = accepted });
            }
        }

        private void CollectChanges(double now)
        {
            _pending.Clear();
            for (int i = 0; i < _units.Count; i++)
            {
                SpikeKinematics actual = _units[i].Kinematics;
                bool send = _lastSentTime[i] == double.MinValue || _mode == SpikeSyncMode.FullSnapshot;
                if (!send)
                {
                    float age = (float)(now - _lastSentTime[i]);
                    SpikeKinematics predicted = _lastSent[i].Extrapolate(age);
                    float error = Vector2.Distance(predicted.Position, actual.Position);
                    float headingError = Mathf.Abs(Mathf.DeltaAngle(predicted.Heading, actual.Heading));
                    bool moving = actual.Speed > 0f || _lastSent[i].Speed > 0f;
                    float refresh = moving ? MOVING_REFRESH_SECONDS : IDLE_REFRESH_SECONDS;
                    Stats.RecordError(_units[i].Kind, error);
                    send = error > _errorThreshold || headingError > HEADING_THRESHOLD || age >= refresh;
                }

                if (!send)
                    continue;

                SpikeUnitState state = SpikeQuantization.Encode((ushort)i, actual);
                _pending.Add(state);
                _lastSent[i] = SpikeQuantization.Decode(state);
                _lastSentTime[i] = now;
            }

            Stats.RecordKeyframes(_pending.Count);
        }

        // Always at least one (possibly empty) chunk per tick so clients keep a steady interpolation timeline.
        private void SendChunks(double now)
        {
            int chunkCount = Mathf.Max(1, Mathf.CeilToInt(_pending.Count / (float)_unitsPerChunk));
            for (int chunk = 0; chunk < chunkCount; chunk++)
            {
                int start = chunk * _unitsPerChunk;
                int count = Mathf.Clamp(_pending.Count - start, 0, _unitsPerChunk);
                SpikeUnitState[] units = new SpikeUnitState[count];
                _pending.CopyTo(start, units, 0, count);

                NetworkServer.SendToAll(new SpikeTickMessage
                {
                    Tick = _tick,
                    Time = now,
                    ChunkIndex = (byte)chunk,
                    ChunkCount = (byte)chunkCount,
                    Units = units
                }, Channels.Unreliable);
            }
        }

        private Vector2 RandomOffset(float radius)
        {
            double angle = _random.NextDouble() * System.Math.PI * 2.0;
            float distance = (float)System.Math.Sqrt(_random.NextDouble()) * radius;
            return new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * distance;
        }
    }
}
