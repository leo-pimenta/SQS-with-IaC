# SQS-with-IaC

Writes and reads messages from a SQS Queue (AWS).

# Infrastructure as Code

The project handles Infrastructure as Code, all SQS handling is done by an YAML file and a SH script to deploy the queues and it's updates to AWS automatically.

# Queues

Two queues are used:

1. Chat queue;
2. Dead letter queue;

# Applications

The application is, actually, two different applications (microservices): a Writer and a Reader. The reasons for this are:

1. Scale: We can scale both services separately;
2. Failures: If an app fails, the error does not spread to the other app;

[See the Writer documentation here.](https://github.com/leo-pimenta/SQS-with-IaC/tree/main/Writer)

See the Reade documentation here. (TODO)
