using Kafka_demo.Services;

var builder = WebApplication.CreateBuilder(args);


// Register KafkaProducer and KafkaConsumer
builder.Services.AddSingleton(new KafkaProducer("localhost:9092"));
builder.Services.AddSingleton(new KafkaConsumer("localhost:9092"));



// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Start Kafka consumer in a background task
Task.Run(() =>
{
    var consumer = app.Services.GetRequiredService<KafkaConsumer>();
    consumer.ConsumeMessages("test-topic");  // Start consuming messages from the "test-topic"
});

app.MapControllers();

app.Run();
