// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MqttServiceTests.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A class to test the <see cref="MqttService" /> class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MQTTnet.TestApp.SimpleServer.Tests;

/// <summary>
/// A class to test the <see cref="MqttService"/> class. The tests run a real server, a real publisher and a
/// real subscriber against the loopback interface, which is what the application does as well.
/// </summary>
[TestClass]
public class MqttServiceTests
{
    /// <summary>
    /// The service under test.
    /// </summary>
    private readonly IMqttService mqttService = new MqttService();

    /// <summary>
    /// The port of this test. Every test gets its own, MSTest creates one instance of this class per test.
    /// </summary>
    private readonly int port = TestDataProvider.GetFreePort();

    /// <summary>
    /// Disposes the service after every test, so that no test leaves a listening port behind.
    /// </summary>
    [TestCleanup]
    public void CleanUp()
    {
        this.mqttService.Dispose();
    }

    /// <summary>
    /// Checks whether a new service has neither a server nor a client, and whether stopping something that
    /// was never started stays quiet.
    /// </summary>
    [TestMethod]
    public async Task NothingIsStartedOnANewService()
    {
        Assert.IsFalse(this.mqttService.IsServerStarted);
        Assert.IsFalse(this.mqttService.IsPublisherStarted);
        Assert.IsFalse(this.mqttService.IsSubscriberStarted);

        await this.mqttService.StopServerAsync();
        await this.mqttService.StopPublisherAsync();
        await this.mqttService.StopSubscriberAsync();

        Assert.IsFalse(this.mqttService.IsServerStarted);
        Assert.IsFalse(this.mqttService.IsPublisherStarted);
        Assert.IsFalse(this.mqttService.IsSubscriberStarted);
    }

    /// <summary>
    /// Checks whether the server starts and stops, and whether starting it twice is the no-op it claims to be
    /// instead of a second server on the same port.
    /// </summary>
    [TestMethod]
    public async Task TheServerStartsStartsAgainAndStops()
    {
        await this.mqttService.StartServerAsync(this.port);
        Assert.IsTrue(this.mqttService.IsServerStarted);

        await this.mqttService.StartServerAsync(this.port);
        Assert.IsTrue(this.mqttService.IsServerStarted);

        await this.mqttService.StopServerAsync();
        Assert.IsFalse(this.mqttService.IsServerStarted);
    }

    /// <summary>
    /// Checks whether a port outside of the valid range is refused before anything is created. The port text
    /// box of the form guards the same range.
    /// </summary>
    [TestMethod]
    public async Task StartServerAsyncWithAPortOutOfRangeThrowsAnArgumentOutOfRangeException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => this.mqttService.StartServerAsync(0));
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => this.mqttService.StartServerAsync(65536));
        Assert.IsFalse(this.mqttService.IsServerStarted);
    }

    /// <summary>
    /// Checks whether a subscriber that cannot reach a server reports that and leaves nothing half started
    /// behind, so that the next attempt starts from scratch.
    /// </summary>
    [TestMethod]
    public async Task StartSubscriberAsyncWithoutAServerLeavesTheSubscriberStopped()
    {
        await Assert.ThrowsExactlyAsync<MqttCommunicationException>(
            () => this.mqttService.StartSubscriberAsync(this.port, TestDataProvider.Topic));

        Assert.IsFalse(this.mqttService.IsSubscriberStarted);
    }

    /// <summary>
    /// Checks whether publishing without a publisher is reported instead of being swallowed. The form keeps
    /// the Publish button disabled until the publisher runs, so this is the second line of defence.
    /// </summary>
    [TestMethod]
    public async Task PublishAsyncWithoutAPublisherThrowsAnInvalidOperationException()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => this.mqttService.PublishAsync(TestDataProvider.Topic, TestDataProvider.Payload));
    }

    /// <summary>
    /// Checks whether an empty topic is refused. A topic of nothing but white space is not a topic.
    /// </summary>
    [TestMethod]
    public async Task PublishAsyncWithoutATopicThrowsAnArgumentException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => this.mqttService.PublishAsync("   ", TestDataProvider.Payload));
    }

    /// <summary>
    /// Checks the whole point of the application: a message that the publisher sends arrives at the
    /// subscriber. The quality of service level of the arriving message is the lower one of the two, the
    /// publisher sends at least once, the topic filter of the subscriber asks for at most once.
    /// </summary>
    [TestMethod]
    public async Task APublishedMessageArrivesAtTheSubscriber()
    {
        var received = new TaskCompletionSource<MqttApplicationMessageReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.mqttService.MessageReceived += (_, eventArgs) => received.TrySetResult(eventArgs);

        await this.mqttService.StartServerAsync(this.port);
        await this.mqttService.StartSubscriberAsync(this.port, TestDataProvider.Topic);
        await this.mqttService.StartPublisherAsync(this.port);
        await this.mqttService.PublishAsync(TestDataProvider.Topic, TestDataProvider.Payload);

        var message = (await received.Task.WaitAsync(TestDataProvider.MessageTimeout)).ApplicationMessage;

        Assert.AreEqual(TestDataProvider.Topic, message.Topic);
        Assert.AreEqual(TestDataProvider.Payload, message.ConvertPayloadToString());
        Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
    }

    /// <summary>
    /// Checks whether a subscriber that starts after the message was published still gets it. Every message
    /// is published with the retain flag, so the server hands the last message of the topic to a subscriber
    /// that arrives late. That is not a bug in the subscriber, it is the reason why the output box is never
    /// empty on a second start.
    /// </summary>
    [TestMethod]
    public async Task ASubscriberThatStartsLateReceivesTheRetainedMessage()
    {
        var received = new TaskCompletionSource<MqttApplicationMessageReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.mqttService.MessageReceived += (_, eventArgs) => received.TrySetResult(eventArgs);

        await this.mqttService.StartServerAsync(this.port);
        await this.mqttService.StartPublisherAsync(this.port);
        await this.mqttService.PublishAsync(TestDataProvider.Topic, TestDataProvider.Payload);
        await this.mqttService.StartSubscriberAsync(this.port, TestDataProvider.Topic);

        var message = (await received.Task.WaitAsync(TestDataProvider.MessageTimeout)).ApplicationMessage;

        Assert.AreEqual(TestDataProvider.Topic, message.Topic);
        Assert.AreEqual(TestDataProvider.Payload, message.ConvertPayloadToString());
        Assert.IsTrue(message.Retain);
    }

    /// <summary>
    /// Checks whether a stopped subscriber really stops receiving. Before the Stop button got its handler
    /// there was no way to find out.
    /// </summary>
    [TestMethod]
    public async Task AStoppedSubscriberReceivesNothing()
    {
        var firstMessage = new TaskCompletionSource<MqttApplicationMessageReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.mqttService.MessageReceived += (_, eventArgs) => firstMessage.TrySetResult(eventArgs);

        await this.mqttService.StartServerAsync(this.port);
        await this.mqttService.StartSubscriberAsync(this.port, TestDataProvider.Topic);
        await this.mqttService.StartPublisherAsync(this.port);
        await this.mqttService.PublishAsync(TestDataProvider.Topic, TestDataProvider.Payload);
        await firstMessage.Task.WaitAsync(TestDataProvider.MessageTimeout);

        await this.mqttService.StopSubscriberAsync();
        Assert.IsFalse(this.mqttService.IsSubscriberStarted);

        var secondMessage = new TaskCompletionSource<MqttApplicationMessageReceivedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.mqttService.MessageReceived += (_, eventArgs) => secondMessage.TrySetResult(eventArgs);
        await this.mqttService.PublishAsync(TestDataProvider.Topic, "{\"dt\":\"after the stop\"}");

        var winner = await Task.WhenAny(secondMessage.Task, Task.Delay(TestDataProvider.SilenceTimeout));
        Assert.AreNotSame(secondMessage.Task, winner, "The stopped subscriber still received a message.");
    }

    /// <summary>
    /// Checks whether the publisher reports its connect and its disconnect. The form shows a message box for
    /// both of them.
    /// </summary>
    [TestMethod]
    public async Task ThePublisherReportsItsConnectAndItsDisconnect()
    {
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        this.mqttService.PublisherConnected += (_, _) => connected.TrySetResult();
        this.mqttService.PublisherDisconnected += (_, _) => disconnected.TrySetResult();

        await this.mqttService.StartServerAsync(this.port);
        await this.mqttService.StartPublisherAsync(this.port);
        await connected.Task.WaitAsync(TestDataProvider.MessageTimeout);
        Assert.IsTrue(this.mqttService.IsPublisherStarted);

        await this.mqttService.StopPublisherAsync();
        await disconnected.Task.WaitAsync(TestDataProvider.MessageTimeout);
        Assert.IsFalse(this.mqttService.IsPublisherStarted);
    }

    /// <summary>
    /// Checks whether disposing the service takes the server and both clients down with it. The form disposes
    /// the service when it is closed.
    /// </summary>
    [TestMethod]
    public async Task DisposeStopsTheServerAndBothClients()
    {
        await this.mqttService.StartServerAsync(this.port);
        await this.mqttService.StartSubscriberAsync(this.port, TestDataProvider.Topic);
        await this.mqttService.StartPublisherAsync(this.port);

        this.mqttService.Dispose();

        Assert.IsFalse(this.mqttService.IsServerStarted);
        Assert.IsFalse(this.mqttService.IsPublisherStarted);
        Assert.IsFalse(this.mqttService.IsSubscriberStarted);
    }
}
