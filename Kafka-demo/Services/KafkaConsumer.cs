using Confluent.Kafka;
using System;
using System.Threading;

namespace Kafka_demo.Services
{
    public class KafkaConsumer
    {
        private readonly IConsumer<Ignore, string> _consumer;

        public KafkaConsumer(string bootstrapServers)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = "test-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest // Start from the earliest message if no offset
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        }

        public void ConsumeMessages(string topic)
        {
            _consumer.Subscribe(topic);

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var consumeResult = _consumer.Consume(cts.Token);
                    Console.WriteLine($"Consumed message: {consumeResult.Message.Value} from {consumeResult.TopicPartitionOffset}");
                }
            }
            catch (ConsumeException e)
            {
                Console.WriteLine($"Error occurred: {e.Error.Reason}");
            }
            finally
            {
                _consumer.Close();
            }
        }
    }
}
