using Mirror;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    public readonly struct SpikeQueuedCommand
    {
        public readonly NetworkConnectionToClient Connection;
        public readonly SpikeCommandMessage Command;

        public SpikeQueuedCommand(NetworkConnectionToClient connection, SpikeCommandMessage command)
        {
            Connection = connection;
            Command = command;
        }
    }
}
