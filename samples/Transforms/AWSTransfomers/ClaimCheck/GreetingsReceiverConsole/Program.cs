﻿#region Licence
/* The MIT License (MIT)
Copyright © 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Greetings.Ports.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.AWSSQS;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Paramore.Brighter.ServiceActivator.Extensions.Hosting;
using Paramore.Brighter.Tranformers.AWS;
using Paramore.Brighter.Transforms.Storage;
using Serilog;

namespace GreetingsReceiverConsole
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var host = new HostBuilder()
                .ConfigureServices((_, services) =>
                {
                    var subscriptions = new Subscription[]
                    {
                        new SqsSubscription<GreetingEvent>(
                            subscriptionName: new SubscriptionName("paramore.example.greeting"),
                            channelName: new ChannelName(typeof(GreetingEvent).FullName!.ToValidSNSTopicName()),
                            channelType: ChannelType.PubSub,
                            routingKey: new RoutingKey(typeof(GreetingEvent).FullName!.ToValidSNSTopicName()),
                            bufferSize: 10,
                            timeOut: TimeSpan.FromMilliseconds(20), 
                            findTopicBy: TopicFindBy.Convention,
                            queueAttributes: new SqsAttributes(
                                lockTimeout: TimeSpan.FromSeconds(30) 
                            ), makeChannels: OnMissingChannel.Create),
                    };

                    //create the gateway
                    if (new CredentialProfileStoreChain().TryGetAWSCredentials("default", out var credentials))
                    {
                        var awsConnection = new AWSMessagingGatewayConnection(credentials, RegionEndpoint.EUWest1);

                        services.AddServiceActivator(options =>
                        {
                            options.Subscriptions = subscriptions;
                            options.DefaultChannelFactory = new ChannelFactory(awsConnection);
                        })
                        .UseExternalLuggageStore(provider => new S3LuggageStore(new S3LuggageOptions(
                            new AWSS3Connection(credentials, RegionEndpoint.EUWest1),
                            "brightersamplebucketb0561a06-70ec-11ed-a1eb-0242ac120002")
                        {
                            HttpClientFactory = provider.GetService<IHttpClientFactory>(),
                            Strategy = StorageStrategy.Validate
                        }))
                        .AutoFromAssemblies();
                        
                        //We need this for the check as to whether an S3 bucket exists
                        services.AddHttpClient();
                    }

                    services.AddHostedService<ServiceActivatorHostedService>();
                })
                .UseConsoleLifetime()
                .UseSerilog()
                .Build();

            await host.RunAsync();




        }
    }
}

