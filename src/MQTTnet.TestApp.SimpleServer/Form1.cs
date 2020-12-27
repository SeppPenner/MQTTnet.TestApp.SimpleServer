// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Form1.cs" company="Hämmer Electronics">
//   Copyright (c) 2020 All rights reserved.
// </copyright>
// <summary>
//   The main form.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.SimpleServer
{
    using System;
    using System.Text;
    using System.Timers;
    using System.Windows.Forms;

    using MQTTnet.Client.Connecting;
    using MQTTnet.Client.Disconnecting;
    using MQTTnet.Client.Options;
    using MQTTnet.Extensions.ManagedClient;
    using MQTTnet.Formatter;
    using MQTTnet.Protocol;
    using MQTTnet.Server;

    using Timer = System.Timers.Timer;

    /// <summary>
    /// The main form.
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// The managed publisher client.
        /// </summary>
        private IManagedMqttClient managedMqttClientPublisher;

        /// <summary>
        /// The managed subscriber client.
        /// </summary>
        private IManagedMqttClient managedMqttClientSubscriber;

        /// <summary>
        /// The MQTT server.
        /// </summary>
        private IMqttServer mqttServer;

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
                AutoReset = true, Enabled = true, Interval = 1000
            };

            timer.Elapsed += this.Timer_Elapsed;
        }

        /// <summary>
        /// Handles the publisher connected event.
        /// </summary>
        /// <param name="x">The MQTT client connected event args.</param>
        private static void OnPublisherConnected(MqttClientConnectedEventArgs x)
        {
            // ReSharper disable LocalizableElement
            MessageBox.Show("Connected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handles the publisher disconnected event.
        /// </summary>
        /// <param name="x">The MQTT client disconnected event args.</param>
        private static void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
        {
            // ReSharper disable LocalizableElement
            MessageBox.Show("Disconnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// The method that handles the button click to generate a message.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void ButtonGeneratePublishedMessage_Click(object sender, EventArgs e)
        {
            var message = $"{{\"dt\":\"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}\"}}";
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
                    .WithTopic(this.TextBoxTopic.Text.Trim()).WithPayload(payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag().Build();

                if (this.managedMqttClientPublisher != null)
                {
                    await this.managedMqttClientPublisher.PublishAsync(message);
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
            var mqttFactory = new MqttFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true, AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = "ClientPublisher",
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "localhost", Port = int.Parse(this.TextBoxPort.Text.Trim()), TlsOptions = tlsOptions
                }
            };

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(5);

            this.managedMqttClientPublisher = mqttFactory.CreateManagedMqttClient();
            this.managedMqttClientPublisher.UseApplicationMessageReceivedHandler(this.HandleReceivedApplicationMessage);
            this.managedMqttClientPublisher.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnPublisherConnected);
            this.managedMqttClientPublisher.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnPublisherDisconnected);

            await this.managedMqttClientPublisher.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = options
                });
        }

        /// <summary>
        /// The method that handles the button click to stop the publisher.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonPublisherStop_Click(object sender, EventArgs e)
        {
            if (this.managedMqttClientPublisher == null)
            {
                return;
            }

            await this.managedMqttClientPublisher.StopAsync();
            this.managedMqttClientPublisher = null;
        }

        /// <summary>
        /// The method that handles the button click to start the server.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private async void ButtonServerStart_Click(object sender, EventArgs e)
        {
            if (this.mqttServer != null)
            {
                return;
            }

            var storage = new JsonServerStorage();
            storage.Clear();

            this.mqttServer = new MqttFactory().CreateMqttServer();

            var options = new MqttServerOptions();
            options.DefaultEndpointOptions.Port = int.Parse(this.TextBoxPort.Text);
            options.Storage = storage;
            options.EnablePersistentSessions = true;

            try
            {
                await this.mqttServer.StartAsync(options);
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
            if (this.mqttServer == null)
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
            var options = new ManagedMqttClientOptionsBuilder().WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder().WithClientId("ClientSubscriber").WithTcpServer("localhost", int.Parse(this.TextBoxPort.Text)).WithTls().Build()).Build();

            this.managedMqttClientSubscriber = new MqttFactory().CreateManagedMqttClient();
            this.managedMqttClientSubscriber.UseApplicationMessageReceivedHandler(this.HandleReceivedApplicationMessage);
            await this.managedMqttClientSubscriber.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(this.TextBoxTopic.Text.Trim()).Build());
            await this.managedMqttClientSubscriber.StartAsync(options);
        }

        /// <summary>
        /// Handles the received application message event.
        /// </summary>
        /// <param name="eventArgs">The event args.</param>
        private void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {eventArgs.ApplicationMessage.ConvertPayloadToString()} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";

            this.BeginInvoke((MethodInvoker)delegate { this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text; });
        }

        /// <summary>
        /// The method that handles the text changes in the text box.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void TextBoxPort_TextChanged(object sender, EventArgs e)
        {
            // ReSharper disable once StyleCop.SA1126
            if (int.TryParse(this.TextBoxPort.Text, out _))
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
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.BeginInvoke(
                (MethodInvoker)delegate
                {
                    // Server
                    this.TextBoxPort.Enabled = this.mqttServer == null;
                    this.ButtonServerStart.Enabled = this.mqttServer == null;
                    this.ButtonServerStop.Enabled = this.mqttServer != null;

                    // Publisher
                    this.ButtonPublisherStart.Enabled = this.managedMqttClientPublisher == null;
                    this.ButtonPublisherStop.Enabled = this.managedMqttClientPublisher != null;

                    // Subscriber
                    this.ButtonSubscriberStart.Enabled = this.managedMqttClientSubscriber == null;
                    this.ButtonSubscriberStop.Enabled = this.managedMqttClientSubscriber != null;
                });
        }
    }
}
