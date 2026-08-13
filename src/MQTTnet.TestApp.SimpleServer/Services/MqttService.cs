// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttService.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A service to run the MQTT server, the publisher client and the subscriber client of the test bench.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.SimpleServer.Services;

/// <inheritdoc cref="IMqttService"/>
/// <summary>
/// A service to run the MQTT server, the publisher client and the subscriber client of the test bench.
/// All three talk to localhost, only the port is given by the caller.
/// </summary>
/// <seealso cref="IMqttService"/>
public sealed class MqttService : IMqttService
{
    /// <summary>
    /// The lowest valid port number.
    /// </summary>
    private const int MinimumPort = 1;

    /// <summary>
    /// The highest valid port number.
    /// </summary>
    private const int MaximumPort = 65535;

    /// <summary>
    /// The host all three parts talk to.
    /// </summary>
    private const string Host = "localhost";

    /// <summary>
    /// The client identifier of the publisher client.
    /// </summary>
    private const string PublisherClientId = "ClientPublisher";

    /// <summary>
    /// The client identifier of the subscriber client.
    /// </summary>
    private const string SubscriberClientId = "ClientSubscriber";

    /// <summary>
    /// The publisher client.
    /// </summary>
    private IMqttClient? mqttClientPublisher;

    /// <summary>
    /// The subscriber client.
    /// </summary>
    private IMqttClient? mqttClientSubscriber;

    /// <summary>
    /// The MQTT server.
    /// </summary>
    private MqttServer? mqttServer;

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Occurs when the subscriber client has received an application message.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Occurs when the publisher client has connected.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public event EventHandler? PublisherConnected;

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Occurs when the publisher client has disconnected.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public event EventHandler? PublisherDisconnected;

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Gets a value indicating whether the server is started.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public bool IsServerStarted => this.mqttServer is not null;

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Gets a value indicating whether the publisher client is started.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public bool IsPublisherStarted => this.mqttClientPublisher is not null;

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Gets a value indicating whether the subscriber client is started.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public bool IsSubscriberStarted => this.mqttClientSubscriber is not null;

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Starts the server on the given port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is not a valid port number.</exception>
    /// <seealso cref="IMqttService"/>
    public async Task StartServerAsync(int port)
    {
        CheckPort(port);

        if (this.mqttServer is not null)
        {
            return;
        }

        // The default endpoint has to be switched on explicitly. A server that is built without it starts
        // without a complaint and listens nowhere, so every client that tries to connect gets its connection
        // refused.
        var options = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .WithPersistentSessions(true)
            .Build();

        var server = new MqttServerFactory().CreateMqttServer(options);

        try
        {
            await server.StartAsync();
        }
        catch
        {
            // The server keeps the port of a failed start until it is disposed, so it is thrown away here
            // instead of being kept in the field. That way the next start attempt begins from scratch.
            await server.StopAsync();
            server.Dispose();
            throw;
        }

        this.mqttServer = server;
    }

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Stops the server.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public async Task StopServerAsync()
    {
        if (this.mqttServer is null)
        {
            return;
        }

        var server = this.mqttServer;
        this.mqttServer = null;
        await server.StopAsync();
        server.Dispose();
    }

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Starts the publisher client and connects it to the server on the given port.
    /// </summary>
    /// <param name="port">The port to connect to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is not a valid port number.</exception>
    /// <seealso cref="IMqttService"/>
    public async Task StartPublisherAsync(int port)
    {
        CheckPort(port);

        if (this.mqttClientPublisher is not null)
        {
            return;
        }

        var client = new MqttClientFactory().CreateMqttClient();
        client.ConnectedAsync += this.OnPublisherConnected;
        client.DisconnectedAsync += this.OnPublisherDisconnected;

        try
        {
            await client.ConnectAsync(BuildClientOptions(PublisherClientId, port, true));
        }
        catch
        {
            client.Dispose();
            throw;
        }

        this.mqttClientPublisher = client;
    }

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Stops the publisher client.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public async Task StopPublisherAsync()
    {
        if (this.mqttClientPublisher is null)
        {
            return;
        }

        var client = this.mqttClientPublisher;
        this.mqttClientPublisher = null;
        await client.DisconnectAsync();
        client.Dispose();
    }

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Publishes the given payload to the given topic.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The payload to publish.</param>
    /// <exception cref="ArgumentException">Thrown if the topic is null, empty or white space.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the publisher client is not started.</exception>
    /// <seealso cref="IMqttService"/>
    public async Task PublishAsync(string topic, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        if (this.mqttClientPublisher is null)
        {
            throw new InvalidOperationException("The publisher is not started.");
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag()
            .Build();

        await this.mqttClientPublisher.PublishAsync(message);
    }

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Starts the subscriber client, connects it to the server on the given port and subscribes it to the
    /// given topic.
    /// </summary>
    /// <param name="port">The port to connect to.</param>
    /// <param name="topic">The topic to subscribe to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is not a valid port number.</exception>
    /// <exception cref="ArgumentException">Thrown if the topic is null, empty or white space.</exception>
    /// <seealso cref="IMqttService"/>
    public async Task StartSubscriberAsync(int port, string topic)
    {
        CheckPort(port);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        if (this.mqttClientSubscriber is not null)
        {
            return;
        }

        var client = new MqttClientFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += this.OnApplicationMessageReceived;

        try
        {
            // The connect has to happen before the subscribe, a client that is not connected refuses to
            // subscribe.
            await client.ConnectAsync(BuildClientOptions(SubscriberClientId, port, false));
            var topicFilter = new MqttTopicFilterBuilder().WithTopic(topic).Build();
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topicFilter).Build();
            await client.SubscribeAsync(subscribeOptions);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        this.mqttClientSubscriber = client;
    }

    /// <inheritdoc cref="IMqttService"/>
    /// <summary>
    /// Stops the subscriber client.
    /// </summary>
    /// <seealso cref="IMqttService"/>
    public async Task StopSubscriberAsync()
    {
        if (this.mqttClientSubscriber is null)
        {
            return;
        }

        var client = this.mqttClientSubscriber;
        this.mqttClientSubscriber = null;
        await client.DisconnectAsync();
        client.Dispose();
    }

    /// <inheritdoc cref="IDisposable"/>
    /// <summary>
    /// Disposes the object. The clients and the server are dropped without a clean disconnect, that is what
    /// their own <see cref="IDisposable"/> implementation is for.
    /// </summary>
    /// <seealso cref="IDisposable"/>
    public void Dispose()
    {
        this.mqttClientPublisher?.Dispose();
        this.mqttClientPublisher = null;
        this.mqttClientSubscriber?.Dispose();
        this.mqttClientSubscriber = null;
        this.mqttServer?.Dispose();
        this.mqttServer = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Builds the client options for a client of the test bench.
    /// </summary>
    /// <param name="clientId">The client identifier to use.</param>
    /// <param name="port">The port to connect to.</param>
    /// <param name="withCredentials">A value indicating whether the fixed credentials are sent or not.</param>
    /// <returns>The <see cref="MqttClientOptions"/> to connect with.</returns>
    private static MqttClientOptions BuildClientOptions(string clientId, int port, bool withCredentials)
    {
        // The ignore flags only matter once TLS is on, which it never is in this test bench. They are kept
        // because they show what a client that talks to a real broker with a self signed certificate needs.
        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = false,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithTcpServer(Host, port)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithTlsOptions(tlsOptions)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(5));

        if (withCredentials)
        {
            // The server of this test bench has no validator, so these are accepted the way anything else
            // would be. They are here to show where credentials belong.
            optionsBuilder = optionsBuilder.WithCredentials("username", "password");
        }

        return optionsBuilder.Build();
    }

    /// <summary>
    /// Checks whether the given port is a valid port number.
    /// </summary>
    /// <param name="port">The port to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the port is not a valid port number.</exception>
    private static void CheckPort(int port)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(port, MinimumPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, MaximumPort);
    }

    /// <summary>
    /// Handles the received application message of the subscriber client.
    /// </summary>
    /// <param name="eventArgs">The event args.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    private Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        this.MessageReceived?.Invoke(this, eventArgs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the connected event of the publisher client.
    /// </summary>
    /// <param name="eventArgs">The event args.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    private Task OnPublisherConnected(MqttClientConnectedEventArgs eventArgs)
    {
        this.PublisherConnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the disconnected event of the publisher client.
    /// </summary>
    /// <param name="eventArgs">The event args.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    private Task OnPublisherDisconnected(MqttClientDisconnectedEventArgs eventArgs)
    {
        this.PublisherDisconnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
