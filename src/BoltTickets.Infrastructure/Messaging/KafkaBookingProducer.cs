using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using BoltTickets.Application.Common.Interfaces;
using BoltTickets.Domain.Entities;
using Confluent.Kafka;
using OpenTelemetry.Context.Propagation;

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
            Value = JsonSerializer.Serialize(booking),
            Headers = new Headers()
        };

        // Inject trace context into headers
        var propagationContext = new OpenTelemetry.Context.Propagation.PropagationContext(Activity.Current?.Context ?? default, default);
        OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator.Inject(propagationContext, message.Headers, (headers, key, value) => headers.Add(key, System.Text.Encoding.UTF8.GetBytes(value)));

        await _producer.ProduceAsync(Topic, message);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
