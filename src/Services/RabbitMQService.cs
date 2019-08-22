// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal sealed class RabbitMQService : IRabbitMQService
    {
        private IModel _model;
        private IBasicPublishBatch _batch;
        private string _connectionString;
        private string _hostName;
        private string _queueName;
        private string _userName;
        private string _password;
        private string _dlxName;
        private int _port;

        public IModel Model => _model;

        public IBasicPublishBatch BasicPublishBatch => _batch;

        public RabbitMQService(string connectionString, string hostName, string queueName, string userName, string password, int port, string dlxName)
        {
            _connectionString = connectionString;
            _hostName = hostName;
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _userName = userName;
            _password = password;
            _dlxName = dlxName;
            _port = port;

            ConnectionFactory connectionFactory = GetConnectionFactory(_connectionString, _hostName, _userName, _password, _port);

            _model = connectionFactory.CreateConnection().CreateModel();

            Dictionary<string, object> args = null;

            if (!string.IsNullOrEmpty(dlxName))
            {
                _model.ExchangeDeclare(dlxName, "direct");
                args = new Dictionary<string, object>();
                args["x-dead-letter-exchange"] = dlxName;
            }

            _model.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: args);
            _batch = _model.CreateBasicPublishBatch();
        }

        internal static ConnectionFactory GetConnectionFactory(string connectionString, string hostName, string userName, string password, int port)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory();

            // Only set these if specified by user. Otherwise, API will use default parameters.
            if (!string.IsNullOrEmpty(connectionString))
            {
                connectionFactory.Uri = new Uri(connectionString);
            }

            if (!string.IsNullOrEmpty(hostName))
            {
                connectionFactory.HostName = hostName;
            }

            if (!string.IsNullOrEmpty(userName))
            {
                connectionFactory.UserName = userName;
            }

            if (!string.IsNullOrEmpty(password))
            {
                connectionFactory.Password = password;
            }

            if (port != 0)
            {
                connectionFactory.Port = port;
            }

            return connectionFactory;
        }
    }
}
