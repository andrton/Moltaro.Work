using System.Net;
using System.Security.Cryptography;
using Moltaro.Signals;

namespace Moltaro.Work.Tests.Doubles;

// The ISignals of a client in the tests: an in-process host on a free port of the loopback address, with a token made
// for this run. Its in-process ISignals works without Start, so it opens no socket. The test disposes it.
internal static class LoopbackSignalHost
{
    public static SignalHost Create()
    {
        var options = new SignalHostOptions
        {
            ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
            Token = RandomNumberGenerator.GetHexString(32),
        };

        return new SignalHost(options);
    }
}
