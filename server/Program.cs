using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

var publisher = new Publisher();

int[] counts = new[] { 1 };

Console.WriteLine("Start!");
foreach (var count in counts)
{
    Console.WriteLine($"Count = {count}");
    await Test_sns(count);
    // await publisher.AddFilterPolicy();
    // await publisher.ListAllFilterPolicies();
    // await publisher.SubscribeEndpointToTopic(new List<string>() { "mohapatra.milind@gmail.com" });
    // await publisher.UnsubscribeEndpointFromTopic(new List<string> { "arn:aws:sns:ap-south-1:268360517502:SNSTestTopic:e60a4cbb-3049-43cb-9766-5207eef4f5bc" });
    // await publisher.ListAllSubscriptions();
    // await publisher.ListAllTopics();
    // await publisher.CreateTopic("tempTopic");
    // await publisher.DeleteTopic("arn:aws:sns:ap-south-1:268360517502:tempTopic");
    // var res = await publisher.ListAllTopics_tmp();
    // var res = await publisher.ListAllSubscriptions_tmp();
    // Console.WriteLine(res);
    Console.WriteLine("***********");
}
Console.WriteLine("End!");

async Task Test_sns(int count)
{
    var responses = new List<HttpStatusCode>();
    Stopwatch stopwatch = new();
    stopwatch.Start();
    for (int i = 1; i <= count; i++)
    {
        var message = string.Format("SNS test message - {0}", count);
        var responseCode = await publisher.PublishAsync(message);
        responses.Add(responseCode);
    }
    stopwatch.Stop();
    TimeSpan elapsedTime = stopwatch.Elapsed;
    string formattedElapsedTime = FormatElapsedTime(elapsedTime);
    Console.WriteLine($"Elapsed Time: {formattedElapsedTime}");
    Console.WriteLine("### " + string.Join(", ", responses));
}

static string FormatElapsedTime(TimeSpan elapsedTime)
{
    return $"{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}.{elapsedTime.Milliseconds:D3}";
}

