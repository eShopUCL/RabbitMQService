/// 
/// This file contains the tests for the MessageLogService.
/// 
using System.Text;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using FluentAssertions;

public class MessageLogServiceTests : IAsyncLifetime
{
    // Create a RabbitMQ testcontainers to isolate the test environment.
    private readonly RabbitMqContainer _rabbitMqContainer;

    // Constants for the dead letter exchange, queue, and routing key
    private const string DeadLetterExchange = "deadLetterExchange";
    private const string DeadLetterQueue = "deadLetterQueue";
    private const string RoutingKey = "dead-letter";

    // Constructor to initialize the RabbitMQ container
    public MessageLogServiceTests()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    // Start the RabbitMQ container before running the tests
    public async Task InitializeAsync() => await _rabbitMqContainer.StartAsync();

    // Stop the RabbitMQ container after running the tests
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
        await Task.Delay(1000);

        // Assert that the received message body matches the expected message
        receivedMessage.Should().Be("Test dead letter message");
    }
    /// 
    /// Helper methods
    /// 

    // Declare the dead letter exchange and queue
    private static void DeclareExchangeAndQueue(IModel channel)
    {
        channel.ExchangeDeclare(DeadLetterExchange, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(DeadLetterQueue, DeadLetterExchange, RoutingKey);
    }

    // Publish a test message to the dead letter exchange from the channel created on the mock RabbitMQ server
    private static void PublishTestMessage(IModel channel)
    {
        var body = Encoding.UTF8.GetBytes("Test dead letter message");
        channel.BasicPublish(DeadLetterExchange, RoutingKey, null, body);
    }
}
