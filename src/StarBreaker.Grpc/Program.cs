using Grpc.Core;
using Grpc.Net.Client;
using Sc.External.Services.Entitygraph.V1;
using PropertyFilter = Sc.External.Services.Entitygraph.V1.PropertyFilter;

const string token = "pranked";
const string url = "https://pub-sc-alpha-323-fw-9324446.test1.cloudimperiumgames.com";
var creds = CallCredentials.FromInterceptor((_, metadata) =>
{
    metadata.Add("Authorization", $"Bearer {token}");

    return Task.CompletedTask;
});


var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions
{
    Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, creds)
});
var client = new EntityGraphService.EntityGraphServiceClient(channel);

Console.WriteLine("Calling EntityQuery for entity with geid XX");
var req = new EntityQueryRequest
{
    //1
    Body = new EntityQueryRequestBody
    {
        //1.1
        Scope = new Scope
        {
            //1.1.1
            Type = ScopeType.Global,
            //1.1.2
            //ShardId = "", 
        },
        //1.2
        Query = new EntityGraphQuery
        {
            //1.2.1
            Filter = new EntityFilter
            {
                //1.2.1.3
                PropertyFilter = new PropertyFilter
                {
                    //1.2.1.3.1
                    Property = "geid",
                    //1.2.1.3.2
                    Operator = ComparisonOperator.In,
                    //1.2.1.3.3
                    Values =
                    {
                        new ScalarValue
                        {
                            //1.2.1.3.3.10, where 10 is oneof uint64
                            UnsignedBigintValue = 11111111111
                        }
                    }
                }
            },
            //1.2.2
            //Pagination = new PaginationArguments(),
            //1.2.3
            Projection = new EntityProjection
            {
                //1.2.3.1
                Tree = new EntityTreeProjection(),
                //1.2.3.2
                // Snapshots = false,
                //1.2.3.3
                // EntityClasses = false,
                //1.2.3.4
                OutgoingEdges = true,
            },
            //1.2.4
            //EntityClassFilter = new EntityClassFilter(),
            //1.2.5
            //Sort = new EntitySortingArguments(),
            //1.2.6
            //Language = "",
        },
    },
};
var result = client.EntityQuery(req);

Console.WriteLine("Got response!");
Console.WriteLine(result.ToString());