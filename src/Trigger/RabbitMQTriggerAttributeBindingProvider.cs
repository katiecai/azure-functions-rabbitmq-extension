﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ
{
    internal class RabbitMQTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly RabbitMQExtensionConfigProvider _provider;
        private readonly ILogger _logger;
        private readonly IOptions<RabbitMQOptions> _options;
        private readonly IConfiguration _configuration;

        public RabbitMQTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            RabbitMQExtensionConfigProvider provider,
            ILogger logger,
            IOptions<RabbitMQOptions> options,
            IConfiguration configuration)
        {
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options;
            _configuration = configuration;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            RabbitMQTriggerAttribute attribute = parameter.GetCustomAttribute<RabbitMQTriggerAttribute>(inherit: false);

            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string connectionString = Utility.ResolveConnectionString(attribute.ConnectionStringSetting, _options.Value.ConnectionString, _configuration);

            string queueName = Resolve(attribute.QueueName) ?? throw new InvalidOperationException("RabbitMQ queue name is missing");

            string hostName = Resolve(attribute.HostName) ?? Constants.LocalHost;

            string userName = Resolve(attribute.UserNameSetting);

            string password = Resolve(attribute.PasswordSetting);

            string deadLetterExchangeName = Resolve(attribute.DeadLetterExchangeName) ?? string.Empty;

            int port = attribute.Port;

            ushort batchNumber = attribute.BatchNumber;

            if (string.IsNullOrEmpty(connectionString) && !Utility.ValidateUserNamePassword(userName, password, hostName))
            {
                throw new InvalidOperationException("RabbitMQ username and password required if not connecting to localhost");
            }

            // If there's no specified batch number, default to 1
            if (batchNumber == 0)
            {
                batchNumber = 1;
            }

            IRabbitMQService service = _provider.GetService(connectionString, hostName, queueName, userName, password, port, deadLetterExchangeName);

            return Task.FromResult<ITriggerBinding>(new RabbitMQTriggerBinding(service, hostName, queueName, batchNumber, _logger));
        }

        private string Resolve(string name)
        {
            return _nameResolver.ResolveWholeString(name) ?? name;
        }
    }
}
