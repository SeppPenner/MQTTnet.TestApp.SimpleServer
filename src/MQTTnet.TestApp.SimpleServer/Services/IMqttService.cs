// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMqttService.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A service to run the MQTT server, the publisher client and the subscriber client of the test bench.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.SimpleServer.Services;

/// <inheritdoc cref="IDisposable"/>
/// <summary>
/// A service to run the MQTT server, the publisher client and the subscriber client of the test bench.
/// All three talk to localhost, only the port is given by the caller.
/// </summary>
/// <seealso cref="IDisposable"/>
public interface IMqttService : IDisposable
{
    /// <summary>
    /// Occurs when the subscriber client has received an application message. The event is raised on a
    /// background thread of the MQTT client, a caller that touches the user interface has to marshal it.
    /// </summary>
    event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Occurs when the publisher client has connected. The event is raised on a background thread of the MQTT
    /// client, a caller that touches the user interface has to marshal it.
    /// </summary>
    event EventHandler? PublisherConnected;

    /// <summary>
    /// Occurs when the publisher client has disconnected, no matter whether it was asked to or lost the
    /// connection. The event is raised on a background thread of the MQTT client, a caller that touches the
    /// user interface has to marshal it.
    /// </summary>
    event EventHandler? PublisherDisconnected;

    /// <summary>
    /// Gets a value indicating whether the server is started.
    /// </summary>
    bool IsServerStarted { get; }

    /// <summary>
    /// Gets a value indicating whether the publisher client is started. A started client that has lost its
    /// connection still counts as started until it is stopped.
    /// </summary>
    bool IsPublisherStarted { get; }

    /// <summary>
    /// Gets a value indicating whether the subscriber client is started. A started client that has lost its
    /// connection still counts as started until it is stopped.
    /// </summary>
    bool IsSubscriberStarted { get; }

    /// <summary>
    /// Starts the server on the given port. Does nothing if the server is already started.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is not a valid port number.</exception>
    Task StartServerAsync(int port);

    /// <summary>
    /// Stops the server. Does nothing if the server is not started.
    /// </summary>
    Task StopServerAsync();

    /// <summary>
    /// Starts the publisher client and connects it to the server on the given port. Does nothing if the
    /// publisher client is already started.
    /// </summary>
    /// <param name="port">The port to connect to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is not a valid port number.</exception>
    Task StartPublisherAsync(int port);

    /// <summary>
    /// Stops the publisher client. Does nothing if the publisher client is not started.
    /// </summary>
    Task StopPublisherAsync();

    /// <summary>
    /// Publishes the given payload to the given topic, retained and with quality of service level at least
    /// once.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The payload to publish.</param>
    /// <exception cref="ArgumentException">Thrown if the topic is null, empty or white space.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the publisher client is not started.</exception>
    Task PublishAsync(string topic, string payload);

    /// <summary>
    /// Starts the subscriber client, connects it to the server on the given port and subscribes it to the
    /// given topic. Does nothing if the subscriber client is already started.
    /// </summary>
    /// <param name="port">The port to connect to.</param>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is not a valid port number.</exception>
    /// <exception cref="ArgumentException">Thrown if the topic is null, empty or white space.</exception>
    Task StartSubscriberAsync(int port, string topic);

    /// <summary>
    /// Stops the subscriber client. Does nothing if the subscriber client is not started.
    /// </summary>
    Task StopSubscriberAsync();
}
