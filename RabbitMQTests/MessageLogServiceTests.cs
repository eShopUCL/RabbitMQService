/// 
/// This file contains the tests for the MessageLogService.
/// 
using System.Text;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using FluentAssertions;
using RabbitMQ.Client.Events;

public class MessageLogServiceTests : IAsyncLifetime
{
    // Create a RabbitMQ testcontainers to isolate the test environment.
    private readonly RabbitMqContainer _rabbitMqContainer;

    // Constants for the dead letter exchange, queue, and routing key
    private const string DeadLetterExchange = "deadLetterExchange";
    private const string DeadLetterQueue = "deadLetterQueue";
    private const string DeadLetterRoutingKey = "dead-letter";
    private const string InvalidMessageExchange = "invalidMessageExchange";
    private const string InvalidMessageRoutingKey = "invalid-letter";
    private const string InvalidMessageQueue = "invalidMessageQueue";
    private const string TestQueue = "testQueue";

    // Constructor to initialize the RabbitMQ container
    public MessageLogServiceTests()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    // Start and stop the RabbitMQ container before and after the tests
    public async Task InitializeAsync() => await _rabbitMqContainer.StartAsync();
    public async Task DisposeAsync() => await _rabbitMqContainer.StopAsync();
    
    // This test verifies that the MessageLogService can consume messages from the dead letter queue.
    // This is an integration test that requires a running RabbitMQ server.
    [Fact]
    public async Task StartDeadLetterConsumerShouldConsumeMessages()
    {
        // Create a connection factory using the RabbitMQ container's connection string
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMqContainer.GetConnectionString()) };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        DeclareExchangeAndQueue(channel);

        // Store the received message
        string receivedMessage = string.Empty;

        // Define the message handler to simulate ForwardMessages() in the MessageLogService
        async Task MessageHandler(string message)
        {
            receivedMessage = message;
            await Task.CompletedTask;
        }

        // Use the StartDeadLetterConsumer method from the MessageLogService to consume messages
        MessageLogService.Program.StartDeadLetterConsumer(channel, MessageHandler);
        
        // Publish a test message to the dead letter exchange, then wait for the message to be consumed
        PublishTestMessage(channel);
        await Task.Delay(100);

        // Assert that the received message body matches the expected message
        receivedMessage.Should().Be("Test dead letter message");
    }

    // Test that messages rejected by a mocked consumer are moved to the dead letter queue
    [Fact]
    public async Task StartDeadLetterConsumer_ShouldMoveRejectedMessagesToDeadLetterQueue()
    {
        // Create a connection factory using the RabbitMQ container's connection string
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMqContainer.GetConnectionString()) };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        DeclareExchangeAndQueue(channel);

        // Variable to hold the received message from the dead letter queue
        string receivedMessage = string.Empty;

        // Define the message handler to simulate ForwardMessages() in the MessageLogService
        async Task MessageHandler(string message)
        {
            receivedMessage = message; // Capture the message in the outer scope variable
            await Task.CompletedTask;
        }

        // Start consuming from the dead letter queue
        MessageLogService.Program.StartInvalidMessageConsumer(channel, MessageHandler);

        // Publish a test message to the "testQueue" and simulate rejection
        await WaitForMessage(async () =>
        {
            PublishTestMessageToTestQueue(channel);

            // Reject the message to simulate an invalid message
            RejectMessage(channel);

            // Allow some time for the message to reach the dead letter queue
            await Task.Delay(1000);
        });

        // Assert that the message in the dead letter queue matches the expected value
        receivedMessage.Should().Be("Test message", because: "the rejected message should be routed to the dead letter queue");

    }
    

    /// 
    /// Helper methods
    /// 

    // Declare the dead letter exchange and queue
    private static void DeclareExchangeAndQueue(IModel channel)
    {
        channel.ExchangeDeclare(DeadLetterExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(DeadLetterQueue, DeadLetterExchange, DeadLetterRoutingKey);


        channel.ExchangeDeclare(InvalidMessageExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(InvalidMessageQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(InvalidMessageQueue, InvalidMessageExchange, InvalidMessageRoutingKey);

        // Declare the test queue with invalid message routing
        channel.QueueDeclare(TestQueue, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", InvalidMessageExchange },
            { "x-dead-letter-routing-key", InvalidMessageRoutingKey }
        });
    }

    // Publish a test message to the dead letter exchange from the channel created on the mock RabbitMQ server
    private static void PublishTestMessage(IModel channel)
    {
        var body = Encoding.UTF8.GetBytes("Test dead letter message");
        channel.BasicPublish(DeadLetterExchange, DeadLetterRoutingKey, null, body);
    }

    // Publish a test message to be rejected by the consumer
    private static void PublishTestMessageToTestQueue(IModel channel)
    {
        var body = Encoding.UTF8.GetBytes("Test message");
        channel.BasicPublish(string.Empty, TestQueue, null, body);
    }

    // Reject the message to simulate an invalid message
    private static void RejectMessage(IModel channel)
    {
        var consumer = new EventingBasicConsumer(channel);
        channel.BasicConsume(TestQueue, autoAck: false, consumer);

        consumer.Received += (_, ea) =>
        {
            channel.BasicReject(ea.DeliveryTag, requeue: false);
        };
    }

    // Helper method to wait for a message to be received
    public async Task<string> WaitForMessage(Func<Task> action, int timeoutMs = 5000)
    {
        string receivedMessage = string.Empty;
        var start = DateTime.UtcNow;

        // Run the action that will produce the message
        await action();

        // Poll for the message
        while (string.IsNullOrEmpty(receivedMessage) && (DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            await Task.Delay(100);
        }

        return receivedMessage;
    }
}
