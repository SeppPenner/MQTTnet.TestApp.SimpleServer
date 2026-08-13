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
    /// The MQTT service that runs the server, the publisher and the subscriber.
    /// </summary>
    private readonly IMqttService mqttService = new MqttService();

    /// <summary>
    /// The timer that keeps the button states in sync with the state of the MQTT service.
    /// </summary>
    private readonly Timer timer;

    /// <summary>
    /// The last port that parsed as a valid port number.
    /// </summary>
    private string port = "1883";

    /// <summary>
    /// Initializes a new instance of the <see cref="Form1"/> class.
    /// </summary>
    public Form1()
    {
        this.InitializeComponent();

        this.mqttService.MessageReceived += this.MqttServiceMessageReceived;
        this.mqttService.PublisherConnected += this.MqttServicePublisherConnected;
        this.mqttService.PublisherDisconnected += this.MqttServicePublisherDisconnected;

        this.timer = new Timer
        {
            AutoReset = true,
            Enabled = true,
            Interval = 1000
        };

        this.timer.Elapsed += this.TimerElapsed!;
        this.FormClosed += this.FormClosedHandler!;
    }

    /// <summary>
    /// The method that handles the button click to generate a message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void ButtonGeneratePublishedMessage_Click(object sender, EventArgs e)
    {
        this.TextBoxPublish.Text = $"{{\"dt\":\"{DateTimeOffset.Now:G}\"}}";
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
            await this.mqttService.PublishAsync(this.TextBoxTopic.Text.Trim(), this.TextBoxPublish.Text);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the button click to start the publisher.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublisherStart_Click(object sender, EventArgs e)
    {
        try
        {
            await this.mqttService.StartPublisherAsync(this.GetPort());
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the button click to stop the publisher.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonPublisherStop_Click(object sender, EventArgs e)
    {
        try
        {
            await this.mqttService.StopPublisherAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the button click to start the server.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonServerStart_Click(object sender, EventArgs e)
    {
        try
        {
            await this.mqttService.StartServerAsync(this.GetPort());
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the button click to stop the server.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonServerStop_Click(object sender, EventArgs e)
    {
        try
        {
            await this.mqttService.StopServerAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the button click to start the subscriber.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonSubscriberStart_Click(object sender, EventArgs e)
    {
        try
        {
            await this.mqttService.StartSubscriberAsync(this.GetPort(), this.TextBoxTopic.Text.Trim());
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the button click to stop the subscriber.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private async void ButtonSubscriberStop_Click(object sender, EventArgs e)
    {
        try
        {
            await this.mqttService.StopSubscriberAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    /// <summary>
    /// The method that handles the text changes in the port text box.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void TextBoxPort_TextChanged(object sender, EventArgs e)
    {
        if (int.TryParse(this.TextBoxPort.Text.Trim(), out var parsedPort) && parsedPort is >= 1 and <= 65535)
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
    /// Shows an exception to the user.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to show.</param>
    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "Error Occurs", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Gets the port to use from the port text box.
    /// </summary>
    /// <returns>The port as an <see cref="int"/>.</returns>
    private int GetPort()
    {
        return int.Parse(this.port);
    }

    /// <summary>
    /// Runs the given action on the thread of the user interface. Events of the MQTT service arrive on a
    /// background thread, and the timer runs on one as well.
    /// </summary>
    /// <param name="action">The <see cref="Action"/> to run.</param>
    private void RunOnUserInterfaceThread(Action action)
    {
        if (this.IsDisposed || !this.IsHandleCreated)
        {
            return;
        }

        this.BeginInvoke(action);
    }

    /// <summary>
    /// The method that handles a received application message of the MQTT service.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void MqttServiceMessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs e)
    {
        var item = $"Timestamp: {DateTimeOffset.Now:O} | Topic: {e.ApplicationMessage.Topic} | Payload: {e.ApplicationMessage.ConvertPayloadToString()} | QoS: {e.ApplicationMessage.QualityOfServiceLevel}";
        this.RunOnUserInterfaceThread(() => this.TextBoxSubscriber.Text = item + Environment.NewLine + this.TextBoxSubscriber.Text);
    }

    /// <summary>
    /// The method that handles the connected publisher of the MQTT service.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void MqttServicePublisherConnected(object? sender, EventArgs e)
    {
        this.RunOnUserInterfaceThread(() => MessageBox.Show("Connected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information));
    }

    /// <summary>
    /// The method that handles the disconnected publisher of the MQTT service.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void MqttServicePublisherDisconnected(object? sender, EventArgs e)
    {
        this.RunOnUserInterfaceThread(() => MessageBox.Show("Disconnected", "ConnectHandler", MessageBoxButtons.OK, MessageBoxIcon.Information));
    }

    /// <summary>
    /// The method that handles the timer events.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void TimerElapsed(object sender, ElapsedEventArgs e)
    {
        this.RunOnUserInterfaceThread(
            () =>
            {
                // Server
                this.TextBoxPort.Enabled = !this.mqttService.IsServerStarted;
                this.ButtonServerStart.Enabled = !this.mqttService.IsServerStarted;
                this.ButtonServerStop.Enabled = this.mqttService.IsServerStarted;

                // Publisher
                this.ButtonPublisherStart.Enabled = !this.mqttService.IsPublisherStarted;
                this.ButtonPublisherStop.Enabled = this.mqttService.IsPublisherStarted;
                this.ButtonPublish.Enabled = this.mqttService.IsPublisherStarted;

                // Subscriber
                this.ButtonSubscriberStart.Enabled = !this.mqttService.IsSubscriberStarted;
                this.ButtonSubscriberStop.Enabled = this.mqttService.IsSubscriberStarted;
            });
    }

    /// <summary>
    /// The method that handles the closed form. The timer is stopped first, so that it cannot reach a form
    /// that is on its way out.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void FormClosedHandler(object sender, FormClosedEventArgs e)
    {
        this.timer.Stop();
        this.timer.Dispose();
        this.mqttService.Dispose();
    }
}
