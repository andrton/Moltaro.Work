using System.Net;
using System.Security.Cryptography;
using Moltaro.Signals;

namespace Moltaro.Work.Hosting.Tests.Doubles;

// The options of the SignalHost that a test registers with AddSignalHost: a free port of the loopback address and a
// token made for this run.
internal static class LoopbackSignals
{
    public static void Configure(SignalHostOptions options)
    {
        options.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
        options.Token = RandomNumberGenerator.GetHexString(32);
    }
}
