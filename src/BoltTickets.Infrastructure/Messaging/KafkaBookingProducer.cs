using System;
using System.Text.Json;
using System.Threading.Tasks;
using BoltTickets.Application.Common.Interfaces;
using BoltTickets.Domain.Entities;
using Confluent.Kafka;

namespace BoltTickets.Infrastructure.Messaging;

public class KafkaBookingProducer : IBookingProducer, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private const string Topic = "booking-intents";

    public KafkaBookingProducer(string bootstrapServers)
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishBookingIntentAsync(Booking booking)
    {
        var message = new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(booking)
        };

        await _producer.ProduceAsync(Topic, message);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
