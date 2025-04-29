using Kafka_demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kafka_demo.Controllers
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
