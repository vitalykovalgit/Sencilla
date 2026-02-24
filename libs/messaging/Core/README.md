# Sencilla.Messaging

Provider-agnostic message bus core for the [Sencilla Framework](https://github.com/vitalykovalgit/Sencilla).

## What's Included

- Message dispatcher interfaces and abstractions
- Command and event routing
- Middleware pipeline for message processing
- Provider-agnostic design — swap implementations without changing application code

## Available Providers

| Package | Transport |
|---------|-----------|
| `Sencilla.Messaging.RabbitMQ` | RabbitMQ |
| `Sencilla.Messaging.Kafka` | Apache Kafka |
| `Sencilla.Messaging.ServiceBus` | Azure Service Bus |
| `Sencilla.Messaging.Redis` | Redis Pub/Sub |
| `Sencilla.Messaging.SignalR` | SignalR (WebSocket) |
| `Sencilla.Messaging.InMemoryQueue` | In-process queue (dev/test) |
| `Sencilla.Messaging.Mediator` | MediatR integration |

## Installation

```bash
dotnet add package Sencilla.Messaging
```

## Documentation

- [Messaging](https://github.com/vitalykovalgit/Sencilla/blob/master/docs/messaging/README.md)

## License

MIT
