// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Form1.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main form.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.SimpleServer;

/// <summary>
/// The main form.
/// </summary>
public partial class Form1 : Form
{
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

    /// <summary>
    /// The port.
    /// </summary>
    private string port = "1883";

    /// <summary>
    /// Initializes a new instance of the <see cref="Form1"/> class.
    /// </summary>
    public Form1()
    {
        this.InitializeComponent();

        var timer = new Timer
        {
            AutoReset = true,
            Enabled = true,
            Interval = 1000
        };

        timer.Elapsed += this.TimerElapsed!;
    }

    /// <summary>
    /// Handles the publisher connected event.
    /// </summary>
    private static Task OnPublisherConnected(MqttClientConnectedEventArgs _)
    {
        MessageBox.Show("Connected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the publisher disconnected event.
    /// </summary>
    private static Task OnPublisherDisconnected(MqttClientDisconnectedEventArgs _)
    {
        MessageBox.Show("Disconnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return Task.CompletedTask;
    }

    /// <summary>
    /// The method that handles the button click to generate a message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void ButtonGeneratePublishedMessage_Click(object sender, EventArgs e)
    {
        var message = $"{{\"dt\":\"{DateTimeOffset.Now:G} {DateTimeOffset.Now:G}\"}}";
        this.TextBoxPublish.Text = message;
    }

    /// <summary>
    /// The method that handles the button click to publish a message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublish_Click(object sender, EventArgs e)
    {
        try
        {
            var payload = Encoding.UTF8.GetBytes(this.TextBoxPublish.Text);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(this.TextBoxTopic.Text.Trim())
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag()
                .Build();

            if (this.mqttClientPublisher is not null)
            {
                await this.mqttClientPublisher.PublishAsync(message);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// The method that handles the button click to start the publisher.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublisherStart_Click(object sender, EventArgs e)
    {
        var mqttFactory = new MqttClientFactory();

        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = false,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };

        var options = new MqttClientOptionsBuilder()
           .WithClientId("ClientPublisher")
           .WithTcpServer("localhost", int.Parse(this.TextBoxPort.Text.Trim()))
           .WithProtocolVersion(MqttProtocolVersion.V311)
           .WithTlsOptions(tlsOptions)
           .WithCleanSession()
           .WithKeepAlivePeriod(TimeSpan.FromSeconds(5))
           .WithCredentials("username", "password")
           .Build();

        if (options.ChannelOptions is null)
        {
            throw new InvalidOperationException();
        }

        this.mqttClientSubscriber = new MqttClientFactory().CreateMqttClient();
        this.mqttClientSubscriber.ApplicationMessageReceivedAsync += this.HandleReceivedApplicationMessage;
        var mqttFilter = new MqttTopicFilterBuilder().WithTopic(this.TextBoxTopic.Text.Trim()).Build();
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(mqttFilter)
            .Build();
        await this.mqttClientSubscriber.SubscribeAsync(subscribeOptions);
        await this.mqttClientSubscriber.ConnectAsync(options);

        this.mqttClientPublisher = mqttFactory.CreateMqttClient();
        this.mqttClientPublisher.ApplicationMessageReceivedAsync += this.HandleReceivedApplicationMessage;
        this.mqttClientPublisher.ConnectedAsync += OnPublisherConnected;
        this.mqttClientPublisher.DisconnectedAsync += OnPublisherDisconnected;
        await this.mqttClientPublisher.ConnectAsync(options);
    }

    /// <summary>
    /// The method that handles the button click to stop the publisher.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublisherStop_Click(object sender, EventArgs e)
    {
        if (this.mqttClientPublisher is null)
        {
            return;
        }

        await this.mqttClientPublisher.DisconnectAsync();
        this.mqttClientPublisher.Dispose();
        this.mqttClientPublisher = null;
    }

    /// <summary>
    /// The method that handles the button click to start the server.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonServerStart_Click(object sender, EventArgs e)
    {
        if (this.mqttServer is not null)
        {
            return;
        }

        var options = new MqttServerOptions();
        options.DefaultEndpointOptions.Port = int.Parse(this.TextBoxPort.Text.Trim());
        options.EnablePersistentSessions = true;
        this.mqttServer = new MqttServerFactory().CreateMqttServer(options);

        try
        {
            await this.mqttServer.StartAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
            await this.mqttServer.StopAsync();
            this.mqttServer = null;
        }
    }

    /// <summary>
    /// The method that handles the button click to stop the server.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonServerStop_Click(object sender, EventArgs e)
    {
        if (this.mqttServer is null)
        {
            return;
        }

        await this.mqttServer.StopAsync();
        this.mqttServer = null;
    }

    /// <summary>
    /// The method that handles the button click to start the subscriber.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonSubscriberStart_Click(object sender, EventArgs e)
    {
        var tlsOptions = new MqttClientTlsOptions
        {
            UseTls = false,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };

        var options = new MqttClientOptionsBuilder()
            .WithClientId("ClientSubscriber")
            .WithTcpServer("localhost", int.Parse(this.TextBoxPort.Text.Trim()))
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithTlsOptions(tlsOptions)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(5))
            .Build();

        this.mqttClientSubscriber = new MqttClientFactory().CreateMqttClient();
        this.mqttClientSubscriber.ApplicationMessageReceivedAsync += this.HandleReceivedApplicationMessage;
        var mqttFilter = new MqttTopicFilterBuilder().WithTopic(this.TextBoxTopic.Text.Trim()).Build();
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(mqttFilter)
            .Build();
        await this.mqttClientSubscriber.SubscribeAsync(subscribeOptions);
        await this.mqttClientSubscriber.ConnectAsync(options);
    }

    /// <summary>
    /// Handles the received application message event.
    /// </summary>
    /// <param name="eventArgs">The event args.</param>
    private Task HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        var item = $"Timestamp: {DateTimeOffset.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {eventArgs.ApplicationMessage.ConvertPayloadToString()} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";
        this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text; });
        return Task.CompletedTask;
    }

    /// <summary>
    /// The method that handles the text changes in the text box.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void TextBoxPort_TextChanged(object sender, EventArgs e)
    {
        if (int.TryParse(this.TextBoxPort.Text.Trim(), out _))
        {
            this.port = this.TextBoxPort.Text.Trim();
        }
        else
        {
            this.TextBoxPort.Text = this.port;
            this.TextBoxPort.SelectionStart = this.TextBoxPort.Text.Length;
            this.TextBoxPort.SelectionLength = 0;
        }
    }

    /// <summary>
    /// The method that handles the timer events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void TimerElapsed(object sender, ElapsedEventArgs e)
    {
        this.BeginInvoke(
            (MethodInvoker)delegate
            {
                // Server
                this.TextBoxPort.Enabled = this.mqttServer is null;
                this.ButtonServerStart.Enabled = this.mqttServer is null;
                this.ButtonServerStop.Enabled = this.mqttServer is not null;

                // Publisher
                this.ButtonPublisherStart.Enabled = this.mqttClientPublisher is null;
                this.ButtonPublisherStop.Enabled = this.mqttClientPublisher is not null;

                // Subscriber
                this.ButtonSubscriberStart.Enabled = this.mqttClientSubscriber is null;
                this.ButtonSubscriberStop.Enabled = this.mqttClientSubscriber is not null;
            });
    }
}
