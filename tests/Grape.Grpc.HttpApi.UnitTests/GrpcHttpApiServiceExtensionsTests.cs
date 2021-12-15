using Google.Protobuf;
using Grape.Grpc.HttpApi.Implements;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Grape.Grpc.HttpApi.UnitTests
{
    public class GrpcHttpApiServiceExtensionsTests
    {

        [Fact]
        public void AddGrpcHttpApi_DefaultOptions_PopulatedProperties()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            services.AddGrpcHttpApi();

            // assert
            var serviceProvider = services.BuildServiceProvider();
            var options1 = serviceProvider.GetRequiredService<IOptions<GrpcHttpApiOptions>>().Value;

            Assert.NotNull(options1.JsonFormatter);
            Assert.NotNull(options1.JsonParser);

            var options2 = serviceProvider.GetRequiredService<IOptions<GrpcHttpApiOptions>>().Value;

            Assert.Equal(options1, options2);
        }

        [Fact]
        public void AddGrpcHttpApi_OverrideOptions_OptionsApplied()
        {
            // arrange
            var jsonFormatter = new JsonFormatter(new JsonFormatter.Settings(formatDefaultValues: false));
            var jsonParser = new DefaultJsonParser(jsonFormatter);

            var services = new ServiceCollection();

            // act
            services.AddGrpcHttpApi(o =>
            {
                o.JsonFormatter = jsonFormatter;
                o.JsonParser = jsonParser;
            });

            // assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<GrpcHttpApiOptions>>().Value;

            Assert.Equal(jsonFormatter, options.JsonFormatter);
            Assert.Equal(jsonParser, options.JsonParser);
        }
    }
}
