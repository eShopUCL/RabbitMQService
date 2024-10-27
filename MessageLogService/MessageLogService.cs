// This service listens to the dead-letter and invalid-message queues, 
// then forwards the messages to Logstash for further processing.

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MessageLogService
{
  class Program
  {
    // Connection configuration
    private static readonly string RabbitMqHostName = "rabbitmq.rabbitmq.svc.cluster.local"; // Must be replaced with rabbitmq service address
    private static readonly string RabbitMqUserName = "secretUser";
    private static readonly string RabbitMqPassword = "secretPassword";
    private static readonly string LogstashUrl = "http://logstash-service:5044"; // Must be replaced with logstash service address

    static async Task Main(string[] args)
    {
      var factory = new ConnectionFactory
      {
        HostName = RabbitMqHostName,
        UserName = RabbitMqUserName,
        Password = RabbitMqPassword
      };

      using var connection = factory.CreateConnection();
      using var channel = connection.CreateModel();

      // Declare exchanges
      channel.ExchangeDeclare(exchange: "deadLetterExchange", type: ExchangeType.Direct);
      channel.ExchangeDeclare(exchange: "invalidMessageExchange", type: ExchangeType.Direct);

      // Start consumers
      StartDeadLetterConsumer(channel);
      StartInvalidMessageConsumer(channel);

      Console.WriteLine("Press [enter] to exit and shutdown the application.");
      Console.ReadLine();
    }

    // Dead letters are messages that were not delivered to any consumer after several attempts. 
    private static void StartDeadLetterConsumer(IModel channel)
    {
        const string deadLetterQueue = "deadLetterQueue";
        
        // Declare the dead letter queue
        channel.QueueDeclare(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        // Bind the queue to the exchange
        channel.QueueBind
        (
          queue: deadLetterQueue, 
          exchange: "deadLetterExchange", 
          routingKey: "dead-letter"
        );

        // Create a consumer
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            // Get the death reason from the message headers
            var deathReason = ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-death-reason")
          ? Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["x-death-reason"])
          : "Unknown reason";

            Console.WriteLine($"Received dead letter message: {message}");
            Console.WriteLine($"Death reason: {deathReason}");

            await ForwardMessages(message);

            // Acknowledge the message, and remove it from the queue
            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume
        (
          queue: deadLetterQueue,
          // Failed message won’t be removed from the queue and can be retried. 
          autoAck: false, 
          consumer: consumer
        );
    }

    // Invalid messages are messages that were rejected by the consumer.
    private static void StartInvalidMessageConsumer(IModel channel)
    {
        const string invalidMessageQueue = "invalidMessageQueue";
        
        // Declare the invalid message queue
        channel.QueueDeclare(
            queue: invalidMessageQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        // Bind the queue to the exchange
        channel.QueueBind
        (
          queue: invalidMessageQueue, 
          exchange: "invalidMessageExchange", 
          routingKey: "invalid-letter"
          );

        // Create a consumer
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"Received invalid message: {message}");

            await ForwardMessages(message);
            
            // Acknowledge the message, and remove it from the queue
            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume
        (
          queue: invalidMessageQueue,
          // Failed message won’t be removed from the queue and can be retried. 
          autoAck: false, 
          consumer: consumer
        );
    }

    // Forward the message to Logstash
    private static async Task ForwardMessages(string message)
    {
      using var client = new HttpClient();
      var content = new StringContent(message, Encoding.UTF8, "application/json");

      try
      {
        var response = await client.PostAsync(LogstashUrl, content);
        response.EnsureSuccessStatusCode(); // Throw if not a success code.
      }
      catch (Exception ex)
      {
          Console.WriteLine($"Failed to forward message to Logstash: {ex.Message}");
      }
    }
  }
}