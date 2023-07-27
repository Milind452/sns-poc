using System.Net;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Secrets;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

public class Publisher
{
    AmazonSimpleNotificationServiceClient _snsClient;
    public Publisher()
    {
        _snsClient = new AmazonSimpleNotificationServiceClient(AWS.ACCESS_KEY, AWS.SECRET_KEY, RegionEndpoint.APSouth1);
    }

    public async Task<HttpStatusCode> PublishAsync(string message)
    {
        List<int> userIDs = new() { 4782, 4783, 4784 };
        // await AddFilterPolicy();

        string[] targets = new[] { "all", "sample-1", "sample-2", "sample-3" };

        string only1 = string.Format("[\"{0}\"]", targets[1]);  // sample-1
        string only2 = string.Format("[\"{0}\"]", targets[2]);  // sample-2
        string only3 = string.Format("[\"{0}\"]", targets[3]);  // sample-3
        string only12 = string.Format("[\"{0}\", \"{1}\"]", targets[1], targets[2]);    //sample-1, sample-2
        string only23 = string.Format("[\"{0}\", \"{1}\"]", targets[2], targets[3]);    //sample-2, sample-3
        string only13 = string.Format("[\"{0}\", \"{1}\"]", targets[1], targets[3]);    //sample-1, sample-3
        string only123 = string.Format("[\"{0}\", \"{1}\", \"{2}\"]", targets[1], targets[2], targets[3]);    //sample-1, sample-2, sample-3

        Dictionary<string, MessageAttributeValue> messageAttributes = new() {
            {
                "target",
                new MessageAttributeValue()
                {
                    DataType = "String.Array",
                    StringValue = only123
                    // "[\"sample-1\", \"sample-2\", \"sample-3\"]"
                    // "[\"user-4783\", ]"
                }
            }
        };
        var topicArn = "arn:aws:sns:ap-south-1:268360517502:SNSTestTopic";
        message = JsonConvert.SerializeObject(new msg()
        {
            @default = message,
            Title = "Sample title",
            Message = "Sample message"
        });

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = message,
            MessageStructure = "json",
            MessageAttributes = messageAttributes
        };



        var response = await _snsClient.PublishAsync(request);
        return response.HttpStatusCode;
        // Console.WriteLine(JsonConvert.SerializeObject(request.MessageAttributes));
        // return HttpStatusCode.OK;
    }

    public class msg
    {
        public string @default { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }

    public async Task AddFilterPolicy()
    {
        var topicArn = "arn:aws:sns:ap-south-1:268360517502:SNSTestTopic";

        var listSubscriptionsRequest = new ListSubscriptionsByTopicRequest
        {
            TopicArn = topicArn
        };
        var listSubscriptionsResponse = await _snsClient.ListSubscriptionsByTopicAsync(listSubscriptionsRequest);
        var existingSubscriptions = listSubscriptionsResponse.Subscriptions;
        // Console.WriteLine(JsonConvert.SerializeObject(existingSubscriptions));

        string newFilterPolicy = "{\"test-filter\": [\"test-1\", \"test-all\"]}";
        foreach (var subscription in existingSubscriptions)
        {
            var getSubscriptionAttributesRequest = new GetSubscriptionAttributesRequest
            {
                SubscriptionArn = subscription.SubscriptionArn
            };
            var getSubscriptionAttributesResponse = await _snsClient.GetSubscriptionAttributesAsync(getSubscriptionAttributesRequest);
            var currentFilterPolicy = getSubscriptionAttributesResponse.Attributes.ContainsKey("FilterPolicy")
                ? getSubscriptionAttributesResponse.Attributes["FilterPolicy"]
                : null;

            string updatedFilterPolicy;
            if (string.IsNullOrEmpty(currentFilterPolicy))
            {
                updatedFilterPolicy = newFilterPolicy;
            }
            else
            {
                var existingFilterPolicyJson = JObject.Parse(currentFilterPolicy);
                var newFilterPolicyJson = JObject.Parse(newFilterPolicy);
                existingFilterPolicyJson.Merge(newFilterPolicyJson, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union
                });
                updatedFilterPolicy = existingFilterPolicyJson.ToString();
            }
            var setSubscriptionAttributesRequest = new SetSubscriptionAttributesRequest
            {
                SubscriptionArn = subscription.SubscriptionArn,
                AttributeName = "FilterPolicy",
                AttributeValue = updatedFilterPolicy
            };
            await _snsClient.SetSubscriptionAttributesAsync(setSubscriptionAttributesRequest);
        }
    }

    public async Task ListAllFilterPolicies()
    {
        var topicArn = "arn:aws:sns:ap-south-1:268360517502:SNSTestTopic";

        var listSubscriptionsRequest = new ListSubscriptionsByTopicRequest
        {
            TopicArn = topicArn
        };
        var listSubscriptionsResponse = await _snsClient.ListSubscriptionsByTopicAsync(listSubscriptionsRequest);
        var existingSubscriptions = listSubscriptionsResponse.Subscriptions;

        foreach (var subscription in existingSubscriptions)
        {
            var getSubscriptionAttributesRequest = new GetSubscriptionAttributesRequest
            {
                SubscriptionArn = subscription.SubscriptionArn
            };
            var getSubscriptionAttributesResponse = await _snsClient.GetSubscriptionAttributesAsync(getSubscriptionAttributesRequest);
            var currentFilterPolicy = getSubscriptionAttributesResponse.Attributes.ContainsKey("FilterPolicy")
                ? getSubscriptionAttributesResponse.Attributes["FilterPolicy"]
                : null;

            Console.WriteLine(string.Format("Subscription Endpoint: {0}\nSubscription ARN: {1}\nFilter Policy: {2}\n--------", subscription.Endpoint, subscription.SubscriptionArn, currentFilterPolicy));
        }
    }

    public async Task SubscribeEndpointToTopic(List<string> deviceEndpoints)
    {
        var topicArn = "arn:aws:sns:ap-south-1:268360517502:SNSTestTopic";
        foreach (string endpoint in deviceEndpoints)
        {
            var subscribeRequest = new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = "email", //"application",
                Endpoint = endpoint
            };

            var subscribeResponse = await _snsClient.SubscribeAsync(subscribeRequest);

            Console.WriteLine("Subscribed device endpoint: " + endpoint);
        }
    }

    public async Task UnsubscribeEndpointFromTopic(List<string> deviceEndpoints)
    {
        foreach (string endpoint in deviceEndpoints)
        {
            var unsubscribeRequest = new UnsubscribeRequest
            {
                SubscriptionArn = endpoint
            };

            var unsubscribeResponse = await _snsClient.UnsubscribeAsync(unsubscribeRequest);

            if (unsubscribeResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("Endpoint unsubscribed successfully.");
            }
            else
            {
                Console.WriteLine("Failed to unsubscribe the endpoint.");
            }
        }
    }

    public async Task ListAllSubscriptions()
    {
        string topicArn = "arn:aws:sns:ap-south-1:268360517502:SNSTestTopic";

        var listSubscriptionsRequest = new ListSubscriptionsByTopicRequest
        {
            TopicArn = topicArn
        };

        ListSubscriptionsByTopicResponse listSubscriptionsResponse = await _snsClient.ListSubscriptionsByTopicAsync(listSubscriptionsRequest);

        foreach (Subscription subscription in listSubscriptionsResponse.Subscriptions)
        {
            Console.WriteLine("Subscription ARN: " + subscription.SubscriptionArn);
            Console.WriteLine("Endpoint: " + subscription.Endpoint);
            Console.WriteLine("Protocol: " + subscription.Protocol);
            Console.WriteLine("Owner: " + subscription.Owner);
            Console.WriteLine("Subscription Status: " + subscription.SubscriptionArn);
            Console.WriteLine("----------------------");
        }
    }

    public async Task<JArray> ListAllSubscriptions_tmp()
    {
        string topicArn = "arn:aws:sns:ap-south-1:268360517502:SNSTestTopic";
        var listSubscriptionsRequest = new ListSubscriptionsByTopicRequest
        {
            TopicArn = topicArn
        };

        ListSubscriptionsByTopicResponse listSubscriptionsResponse = await _snsClient.ListSubscriptionsByTopicAsync(listSubscriptionsRequest);

        var subscriptionsInfo = new JArray();

        foreach (Subscription subscription in listSubscriptionsResponse.Subscriptions)
        {
            var getSubscriptionAttributesRequest = new GetSubscriptionAttributesRequest
            {
                SubscriptionArn = subscription.SubscriptionArn
            };
            var getSubscriptionAttributesResponse = await _snsClient.GetSubscriptionAttributesAsync(getSubscriptionAttributesRequest);
            var currentFilterPolicy = getSubscriptionAttributesResponse.Attributes.ContainsKey("FilterPolicy")
                ? getSubscriptionAttributesResponse.Attributes["FilterPolicy"]
                : null;

            var subscriptionInfo = new JObject();
            subscriptionInfo["SubscriptionArn"] = subscription.SubscriptionArn;
            subscriptionInfo["Endpoint"] = subscription.Endpoint;
            subscriptionInfo["Protocol"] = subscription.Protocol;
            subscriptionInfo["Owner"] = subscription.Owner;
            subscriptionInfo["SubscriptionStatus"] = subscription.SubscriptionArn;
            subscriptionInfo["FilterPolicy"] = currentFilterPolicy ?? "/No Filter Policy Added/";

            subscriptionsInfo.Add(subscriptionInfo);
        }

        Console.WriteLine(subscriptionsInfo);
        return subscriptionsInfo;
    }


    public async Task ListAllTopics()
    {
        var listTopicsRequest = new ListTopicsRequest();
        ListTopicsResponse listTopicsResponse;

        do
        {
            listTopicsResponse = await _snsClient.ListTopicsAsync(listTopicsRequest);

            foreach (var topic in listTopicsResponse.Topics)
            {
                var topicArn = topic.TopicArn;

                var topicAttributesRequest = new GetTopicAttributesRequest
                {
                    TopicArn = topicArn
                };

                var topicAttributesResponse = await _snsClient.GetTopicAttributesAsync(topicAttributesRequest);

                Console.WriteLine("Topic ARN: " + topicArn);
                Console.WriteLine("Topic Name: " + topicArn.Split(':').Last());
                foreach (var attribute in topicAttributesResponse.Attributes)
                {
                    Console.WriteLine(string.Format("{0}: {1}", attribute.Key, attribute.Value));
                }
                Console.WriteLine("----------------------");
            }

            listTopicsRequest.NextToken = listTopicsResponse.NextToken;
        } while (!string.IsNullOrEmpty(listTopicsResponse.NextToken));
    }

    public async Task<JArray> ListAllTopics_tmp()
    {
        var listTopicsRequest = new ListTopicsRequest();
        ListTopicsResponse listTopicsResponse;
        var topicsInfo = new JArray();

        do
        {
            listTopicsResponse = await _snsClient.ListTopicsAsync(listTopicsRequest);

            foreach (var topic in listTopicsResponse.Topics)
            {
                var topicArn = topic.TopicArn;

                var topicAttributesRequest = new GetTopicAttributesRequest
                {
                    TopicArn = topicArn
                };

                var topicAttributesResponse = await _snsClient.GetTopicAttributesAsync(topicAttributesRequest);

                var topicInfo = new JObject();
                topicInfo["TopicArn"] = topicArn;
                topicInfo["TopicName"] = topicArn.Split(':').Last();
                topicInfo["Attributes"] = JObject.FromObject(topicAttributesResponse.Attributes);

                topicsInfo.Add(topicInfo);
            }

            listTopicsRequest.NextToken = listTopicsResponse.NextToken;
        } while (!string.IsNullOrEmpty(listTopicsResponse.NextToken));

        return topicsInfo;
    }


    public async Task CreateTopic(string topicName)
    {
        var createTopicRequest = new CreateTopicRequest
        {
            Name = topicName
        };

        CreateTopicResponse createTopicResponse = await _snsClient.CreateTopicAsync(createTopicRequest);

        if (createTopicResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            string topicArn = createTopicResponse.TopicArn;
            Console.WriteLine("New Topic ARN: " + topicArn);
            Console.WriteLine("Topic created successfully.");
        }
        else
        {
            Console.WriteLine("Failed to create the topic.");
        }
    }

    public async Task DeleteTopic(string topicArn)
    {
        var deleteTopicRequest = new DeleteTopicRequest
        {
            TopicArn = topicArn
        };

        DeleteTopicResponse deleteTopicResponse = await _snsClient.DeleteTopicAsync(deleteTopicRequest);

        if (deleteTopicResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Console.WriteLine("Topic deleted successfully.");
        }
        else
        {
            Console.WriteLine("Failed to delete the topic.");
        }
    }
}