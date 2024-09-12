using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Serializers;

namespace LobotJR.Shared.Utility
{
    public class NewtonsoftDeserializer : IDeserializer
    {
        public static NewtonsoftDeserializer Default { get; private set; } = new NewtonsoftDeserializer();

        T IDeserializer.Deserialize<T>(RestResponse response)
        {
            var resolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(true, false, true)
            };
            var settings = new JsonSerializerSettings
            {
                ContractResolver = resolver
            };
            return JsonConvert.DeserializeObject<T>(response.Content, settings);
        }
    }
}
