using Confluent.Kafka;
using System;
using System.Threading.Tasks;

namespace Kafka_demo.Services
{
    public class KafkaProducer
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducer(string bootstrapServers)
        {
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task SendMessageAsync(string topic, string message)
        {
            var msg = new Message<Null, string> { Value = message };
            var result = await _producer.ProduceAsync(topic, msg);
            Console.WriteLine($"Delivered to: {result.TopicPartitionOffset}");
        }
    }
}
