namespace EmpireAtWar.Models.Factions
{
    //todo: not scalable - refactor
    public enum ShipType
    {
        //republic
        Venator = 0,
        Acclamator = 1,
        Arquitens = 2,
        HeavyDreadnought = 5,
        Thranta = 6,
        Rothana = 7,
        Resolute = 8,
        Mandator = 9,
        Imperator = 10,
        StealthCorvette = 11,

        //separatist
        Providence = 100,
        Recusant = 101,
        Munificent = 102,
        Lucrehulk = 103,
        Malevolence = 104,
        Captor = 105,
        PatrolFrigate = 106,
        C9979 = 107,
        Dispatcher = 108,

        //empire
        Victory = 200,
        ISDI = 201,
        ISDII = 205,
        ISDIII = 206,
        VictoryI = 202,
        VictoryII = 203,
        ArquitensImperialCruiser = 204,
        RaiderCorvette = 208,

        //rebellion
        NebulonB = 300,
        CorellianCorvette = 301,
        MonCalCruiser = 302,
        HomeOne = 303,
        MC75Profundity = 304,

    }
}
