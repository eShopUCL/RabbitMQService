# RabbitMQService

## Overview

This project sets up a microservice with a RabbitMQ cluster on Kubernetes, with a single-node deployment and Traefik ingress for external access to the RabbitMQ management interface. The setup uses a custom Ingress to route traffic through Traefik and includes essential configurations for RabbitMQ authentication and resource management. 

- **`rabbitmq-management.yml`**: Defines the RabbitMQ cluster, service, and resource limits.
- **`rabbitmq-ingress.yml`**: Configures Traefik ingress to expose the RabbitMQ management interface.
- **`rabbitmq-cluster-operator.yml`**: Deploys the RabbitMQ Cluster Operator in Kubernetes.
- **`MessageLogService`**: Deploys a RabbitMQ log application, see additional info in the readme file, within the subfolder.

## Features

1. **RabbitMQ Cluster Operator**: 
  The `rabbitmq-cluster-operator.yml` file deploys the RabbitMQ Cluster Operator to manage RabbitMQ clusters in Kubernetes.
2. **RabbitMQ Cluster**: 
  Deploys a RabbitMQ instance using `RabbitmqCluster`, with a single replica and management access on port 15672.
3. **Ingress (Traefik)**: 
  Configures Traefik as the ingress controller for external access to RabbitMQ management.
4. **Service Configuration**: 
  Defines a `ClusterIP` service for internal communication and external access via Traefik.

### Cluster Operator

The `RabbitMQService` can be managed using a cluster operator for Kubernetes. The cluster operator ensures high availability and automates the management of RabbitMQ clusters.

### Ingress Configuration

To expose the `RabbitMQService` outside the Kubernetes cluster, you need to configure an ingress resource.

### Management

The `RabbitMQService` can be managed using the RabbitMQ Management Plugin, which provides a web-based UI for monitoring and managing RabbitMQ.

## Deployment

### Prerequisites

- **Kubernetes** cluster with Traefik installed as the ingress controller.

### Configuration

The service can be configured using additional configurations and metadata in the `rabbitmq-management.yml`. The following variables are available:

- `name`: The hostname of the RabbitMQ server.
- `management.tcp.port`: The port of the RabbitMQ server.
- `default_user`: The username for RabbitMQ authentication.
- `default_pass`: The password for RabbitMQ authentication.

To install and deploy the `RabbitMQService`, follow these steps:

1. Clone the repository:
  ```bash
  git clone https://github.com/bast38900/eShopProject.git
  ```
2. Navigate to the RabbitMQService directory:
  ```bash
  cd eShopProject/RabbitMQService
  ```
3. Install RabbitMQ Cluster Operator:
  ```bash
  kubectl apply -f rabbitmq-cluster-operator.yml
  ```
4. Expose Management Interface:
  ```bash
  kubectl apply -f rabbitmq-ingress.yml
  ```
5. Deploy RabbitMQ Cluster:
  ```bash
  kubectl apply -f rabbitmq-management.yml
  ```

## Usage

To access the management ui, go to:
```markdown
http://<EXTERNAL_IP>/
```
=> Will be changed in the future, when prefix issues are fixed.

## License

It's a public schoolproject, feel free to use

## Contact

For any questions or issues, please open an issue on GitHub or contact the maintainers.