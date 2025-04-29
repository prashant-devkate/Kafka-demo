**What is Kafka?**  
A distributed **event streaming platform** used to build real-time data pipelines and applications.

**Why Kafka?**  
- **High Throughput**: Handles millions of events/sec.  
- **Scalable**: Easily scales horizontally.  
- **Durable**: Data is written to disk and replicated.  
- **Real-Time**: Enables low-latency data processing.  
- **Decouples Systems**: Acts as a buffer between producers and consumers.

**🔑 Key Difference**:
- **Kafka**: To send and receive data.
- **Zookeeper**: To manage the Kafka servers and make sure they work properly.


---

# 🔧 1. Setup Kafka Locally with Docker

1. Install Docker Desktop
2. File Explorer > Create a folder `KafkaSetup` > Create `docker-compose.yml`

### Create `docker-compose.yml`:

```yaml
version: '3'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181

  kafka:
    image: confluentinc/cp-kafka:7.5.0
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
```

### Start Kafka:
Open a terminal, go to the folder where `docker-compose.yml` is, and run:

```bash
docker-compose up -d
```
### Check if Kafka is Running
Run:
```sh
docker ps
```

You should see something like:
```
CONTAINER ID   IMAGE                         ...   PORTS
xyz123         confluentinc/cp-kafka:7.5.0   ...   0.0.0.0:9092->9092/tcp
abc456         confluentinc/cp-zookeeper:7.5.0 ...
```

That means Kafka is running on localhost:9092. ✅

---

# 🚀 2. ASP.NET Core Web API – Kafka Producer

## ✅ File Structure Overview

```
YourProject/
├── Controllers/
│   └── KafkaController.cs     👈 sends messages to Kafka
├── Services/
│   └── KafkaProducer.cs       👈 Kafka client & producer logic
├── Program.cs                 👈 registers KafkaProducer in DI
├── appsettings.json           👈 optional: add Kafka settings here
```

---

## 1. **KafkaProducer.cs** → `Services/`

> Handles actual Kafka message production.

**Create file**: `Services/KafkaProducer.cs`

```csharp
using Confluent.Kafka;
using System;
using System.Threading.Tasks;

namespace YourNamespace.Services
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
```

---

## 2. **KafkaController.cs** → `Controllers/`

> API endpoint to trigger sending a message.

**Create file**: `Controllers/KafkaController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using YourNamespace.Services;
using System.Threading.Tasks;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KafkaController : ControllerBase
    {
        private readonly KafkaProducer _producer;

        public KafkaController(KafkaProducer producer)
        {
            _producer = producer;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] string message)
        {
            await _producer.SendMessageAsync("test-topic", message);
            return Ok("Message sent to Kafka");
        }
    }
}
```

---

## 3. **Program.cs**

> Register the producer as a singleton and start the Web API.

Modify `Program.cs`:

```csharp
using YourNamespace.Services;

var builder = WebApplication.CreateBuilder(args);

// Register KafkaProducer
builder.Services.AddSingleton(new KafkaProducer("localhost:9092"));

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

## ✅ Test the API

1. Make sure Kafka & Zookeeper are running on `localhost:9092`.
2. Run your Web API.
3. Send a POST request:

```http
POST http://localhost:5000/api/kafka/send
Content-Type: application/json

"Hello Kafka!"
```
---

# 🚀 3. ASP.NET Core Web API – Kafka Producer

### **Create Kafka Consumer Class**

1. Create a new file named `KafkaConsumer.cs` in your `Services` folder.
2. Add the following code to consume messages from the `test-topic`:

```csharp
using Confluent.Kafka;
using System;
using System.Threading;

namespace YourNamespace.Services
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
```

### **Start the Consumer in Program.cs**

#### Modify `Program.cs`:

```csharp
using YourNamespace.Services;

var builder = WebApplication.CreateBuilder(args);

// Register KafkaProducer and KafkaConsumer
builder.Services.AddSingleton(new KafkaProducer("localhost:9092"));
builder.Services.AddSingleton(new KafkaConsumer("localhost:9092"));

builder.Services.AddControllers();

var app = builder.Build();

// Start Kafka consumer in a background thread
Task.Run(() =>
{
    var consumer = app.Services.GetRequiredService<KafkaConsumer>();
    consumer.ConsumeMessages("test-topic");  // Start consuming messages from the "test-topic"
});

app.MapControllers();
app.Run();
```

---

### **Run the Web API**

Build and run your Web API again:

```bash
dotnet run
```

---

### 🔹 5. **Test it**

To test this:

1. Open another terminal or Postman, and **send a POST request** to the Web API to send a message to Kafka:

```http
POST http://localhost:5000/api/kafka/send
Content-Type: application/json

"Hello from Kafka producer!"
```

2. Your consumer will display the message:

```bash
Consumed message: Hello from Kafka producer! from test-topic [0] @1
```

