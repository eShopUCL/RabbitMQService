# MessageLogService

## Overview

The `MessageLogService` is a microservice designed to listen to RabbitMQ dead-letter and invalid-message queues, process the messages, and forward them to Logstash for further analysis and logging.

The service is configured to run as a Kubernetes Deployment. It runs one replica with limited resource requests to conserve resources.

## Features

- **Dead-Letter Queue Handling**: Receives undeliverable messages from the `deadLetterQueue`, retrieves the death reason, and forwards them to Logstash.
- **Invalid Message Queue Handling**: Captures messages rejected by the consumer from the `invalidMessageQueue` and forwards them to Logstash.
- **Logstash Integration**: Messages are sent to Logstash for centralized logging and analysis.

## Deployment

### Prerequisites

- Docker image for `MessageLogService` must be available at `eshopregistry.azurecr.io/messagelogservice:latest`.
- Kubernetes cluster must be set up and running.
- RabbitMQ Cluster operator must be set up and running.
- RabbitMQ and Logstash services must be deployed and accessible within the cluster.
- Management node must be configured to manage the Kubernetes cluster.

### Configuration

The service connects to RabbitMQ using the following configuration:
- **Host**: `rabbitmq-management.rabbitmq-system.svc.cluster.local`
- **Port**: `5672`
- **Username**: `<insertUsername>`
- **Password**: `<insertPassword>`

The queues and exchanges it interacts with are:
- **Dead Letter Exchange** (`deadLetterExchange`), bound to **Dead Letter Queue** (`deadLetterQueue`)
- **Invalid Message Exchange** (`invalidMessageExchange`), bound to **Invalid Message Queue** (`invalidMessageQueue`)

The Kubernetes service configuration uses `ClusterIP` to restrict access within the cluster. External access can be configured via Traefik if required.

## Usage

### Logstash

The service forwards all messages to Logstash at the endpoint `http://logstash-service:5044`.

## License

It's a public schoolproject, feel free to use

## Contact

For any questions or issues, please open an issue on GitHub or contact the maintainers.