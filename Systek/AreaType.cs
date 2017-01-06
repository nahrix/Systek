namespace Systek
{
    /// <summary>
    /// Code implementation of the area types defined in tblAreaType
    /// </summary>
    public enum AreaType
    {
            SERVER_INITIALIZATION = 1000,
            SERVER_TCP_LISTENER = 1001,
            SERVER_MACHINE = 1002,
            SERVER_MESSAGE_HANDLER = 1003,
            AGENT_MESSAGE_HANDLER = 2000,
            AGENT_INITIALIZATION = 2001,
            NET_LIB = 3000,
            UNIT_TEST = 4000
    };
}
