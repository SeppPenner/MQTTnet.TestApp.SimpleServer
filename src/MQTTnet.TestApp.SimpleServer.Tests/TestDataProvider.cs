// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestDataProvider.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to provide the test data used in the tests.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.SimpleServer.Tests;

/// <summary>
/// A class to provide the test data used in the tests.
/// </summary>
public static class TestDataProvider
{
    /// <summary>
    /// The topic used in all tests. It is the topic the application starts with.
    /// </summary>
    public const string Topic = "brand/type/group/code";

    /// <summary>
    /// The payload used in all tests. It has the shape the Random button of the application generates.
    /// </summary>
    public const string Payload = "{\"dt\":\"13.08.2026 20:15\"}";

    /// <summary>
    /// The time a test waits for a message before it gives up.
    /// </summary>
    public static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The time a test waits to make sure that no message arrives at all.
    /// </summary>
    public static readonly TimeSpan SilenceTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets a port that is free at this moment. Every test uses its own port, so that a server that is torn
    /// down slowly cannot make the next test fail. The port is free when it is returned, and nothing keeps
    /// another process from taking it in the meantime.
    /// </summary>
    /// <returns>A free port as an <see cref="int"/>.</returns>
    public static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
