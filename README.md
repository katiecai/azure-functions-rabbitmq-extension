|Branch|Status|
|---|---|
|master|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/azure-functions-rabbitmq-extension-ci?branchName=master)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=34&branchName=master)|
|dev|[![Build Status](https://azfunc.visualstudio.com/Azure%20Functions/_apis/build/status/azure-functions-rabbitmq-extension-ci?branchName=dev)](https://azfunc.visualstudio.com/Azure%20Functions/_build/latest?definitionId=34&branchName=dev)|

# RabbitMQ Binding Support for Azure Functions

The Azure Functions RabbitMQ Binding extension encapsulates the functionality of the RabbitMQ API within Azure Functions syntax. This simplifies the use of RabbitMQ in Functions, allowing one to avoid hardcoding API events and sending/receiving logic. Instead, the developer uses Functions to configure their trigger, which receives RabbitMQ messsages from a specified queue, or their output binding, which sends out messages to a specified queue.

[RabbitMQ Documentation for the .NET Client](https://www.rabbitmq.com/dotnet-api-guide.html)

# Samples

See the repository [wiki](https://github.com/katiecai/azure-functions-rabbitmq-extension/wiki) for more detailed samples of bindings to different types.

## Output Binding

```C#
using Microsoft.Azure.WebJobs;
using RabbitMQ.Client;

public static void TimerTrigger_StringOutput(
    [TimerTrigger("00:01")] TimerInfo timer,
    [RabbitMQ(
        Hostname = "localhost",
        QueueName = "queue")] out string outputMessage)
{
    outputMessage = "hello"
}
```

The above example waits on a timer trigger to fire (every second) before sending a message to the queue named "queue" connected to the localhost port. The message we want to send is then bound to the variable outputMessage.

If you'd like to see the messages being sent in action, you first must set up a RabbitMQ listener with the same configuration as that of the output binding. One way you can do this is by writing the receiving logic part of a RabbitMQ C# console app. 

To set this up, navigate to your terminal. Make sure you have dotnet installed first. Then, run the commands:

```
// Create a new console app
dotnet new console --name Receive
mv Receive/Program.cs Receive/Receive.cs

// Install the RabbitMQ dependency
dotnet add package RabbitMQ.Client
dotnet restore
```

Then, open up Receive.cs and set up the following code:

```C#
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

class Receive
{
    public static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            QueueDeclareOk queue = channel.QueueDeclare(queue: "queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) => 
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);
                Console.WriteLine("Number of messages in queue: {0}", queue.MessageCount);

            };
            channel.BasicConsume(queue: "queue", autoAck: false, consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
```

This code listens for messages from the queue we send to in the sample. Once it's in place, we can:

1) On your terminal in the Receive directory, run:
```
dotnet run
```
2) Run the sample
3) On the terminal, you should see logs for the message sent from the function trigger

## Trigger

```C#
public static void RabbitMQTrigger_String(
    [RabbitMQTrigger("localhost", "queue")] string message,
    string consumerTag,
    ILogger logger
)
{
    logger.LogInformation($"RabbitMQ queue trigger function processed message consumer tag: {consumerTag}");
    logger.LogInformation($"RabbitMQ queue trigger function processed message: {message}");
}
```
This above sample is waiting on a message from the queue named "queue" connected to the localhost port. Once the function receives the message from the trigger, it binds it to the variable **message**. The function body logs the value of this received message.

This example also shows how you can extract certain **attributes** from your trigger binding. In this case, we use the variable **consumerTag** to extract the consumer tag from the RabbitMQ message that causes the trigger to fire. All exposed attributes for this binding are members of the **BasicDeliverEventArgs** object in RabbitMQ. The table below details these attributes according to the RabbitMQ documentation.

| Name              | Type             | Description                                                         |
| ----------------- | ---------------- | ------------------------------------------------------------------- |
| ConsumerTag       | string           | The consumer tag of the consumer that the message was delivered to. |
| DeliveryTag       | string           | The delivery tag for this delivery. See [IModel.BasicAck](https://www.rabbitmq.com/releases/rabbitmq-dotnet-client/v3.4.2/rabbitmq-dotnet-client-3.4.2-client-htmldoc/html/type-RabbitMQ.Client.IModel.html#method-M:RabbitMQ.Client.IModel.BasicAck(System.UInt64,System.Boolean)).            |
| Redelivered       | bool             | The AMQP "redelivered" flag.                                        |
| Exchange          | string           | The exchange the message was originally published to.               |
| RoutingKey        | string           | The routing key used when the message was originally published.     |
| BasicProperties   | [IBasicProperties](https://www.rabbitmq.com/releases/rabbitmq-dotnet-client/v3.2.2/rabbitmq-dotnet-client-3.2.2-client-htmldoc/html/type-RabbitMQ.Client.IBasicProperties.html) | The content header of the message.                                  |


To run the sample, again some setup is required. The same prerequisites as those needed for the output binding sample are required.

In your terminal:
```
// Create a new console app
dotnet new console --name Send
mv Receive/Program.cs Send/Send.cs

// Install the RabbitMQ dependency
dotnet add package RabbitMQ.Client
dotnet restore
```

This time we are writing code that *sends* messages to the queue, so that the trigger from the sample is fired when we run this code.

Open up the Send.cs file and add the following code:
```C#
class Send 
{
    public static void Main() 
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            QueueDeclareOk queue = channel.QueueDeclare(queue: "queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
           
            var body = Encoding.UTF8.GetBytes("hello world");
            channel.BasicPublish(exchange: "", routingKey: "queue", basicProperties: null, body: body);
            Console.WriteLine(" [x] Sent {0}", testObjString);
        }

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}
```

Finally, we can run the sample:
1) Run the sample
2) On your terminal in the Send directory, run:
```
dotnet run
```
3) Once the message is sent on your terminal, you should see the trigger being fired and logs in your debug console.

## Trigger and Output Binding
```C#
public static void RabbitMQTrigger_RabbitMQOutput(
    [RabbitMQTrigger("RabbitMQConnection", "queue")] string inputMessage,
    [RabbitMQ(
        ConnectionStringSetting = "RabbitMQConnection",
        QueueName = "hello")] out string outputMessage,
    ILogger logger)
{
    outputMessage = inputMessage;
    logger.LogInformation($"RabittMQ output binding function sent message: {outputMessage}");
}
```

The above sample waits on a trigger from the queue named "queue" connected to the connection string value of key "RabbitMQConnection." The output binding takes the messages from the trigger queue and outputs them to queue "hello" connected to the connection configured by the key "RabibtMQConnection". When running locally, add the connection string setting to appsettings.json file. When running in Azure, add this setting as [ConnectionString ](https://azure.microsoft.com/en-us/blog/windows-azure-web-sites-how-application-strings-and-connection-strings-work/) for your app.

The above sample uses the RabbitMQ extension both to bind to an output queue and to wait on a trigger. In this example, we wait on a message from the queue "queue" connected to localhost. Once we receive a new message in that queue, the trigger fires. We then set the output binding to the variable **outputMessage**, which we then configure to be sent to the queue named "hello".

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.