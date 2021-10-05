using FluentAssertions;
using Grpc.HttpApi.Implements;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Grpc.HttpApi.UnitTests
{
    public class DefaultJsonParserTests
    {
        [Fact]
        public void ToJson_WithSimpleMessage_ShouldParseSusscess()
        {
            //arrange
            var parser = new DefaultJsonParser(new GrpcHttpApiOptions().JsonFormatter);

            HelloReply reply = new HelloReply();
            reply.Message = "hello";
            //act
            var json = parser.ToJson(reply);

            //assert
            json.Should().NotBeNullOrEmpty()
                .And.Equals("{ \"message\":\"hello\" }");
        }

        [Fact]
        public void ToJson_WithDefaultValue_ShouldBeEmplyJsonObject()
        {
            //arrange
            var parser = new DefaultJsonParser(new GrpcHttpApiOptions().JsonFormatter);

            HelloReply reply = new HelloReply();        
            
            //act
            var json = parser.ToJson(reply);

            //assert
            json.Should().NotBeNullOrEmpty()
                .And.Equals("{ }");
        }

        [Fact]
        public void ToJson_NotIMessageType_ShouldBeNull()
        {
            //arrange
            var parser = new DefaultJsonParser(new GrpcHttpApiOptions().JsonFormatter);

            TestJsonObject testObject = new TestJsonObject() { Name = "wong" };

            //act
            var json = parser.ToJson(testObject);

            //assert
            json.Should().BeNull();
        }
        [Fact]
        public void ToJson_ObjectType_ShouldBeSuccess()
        {
            //arrange
            var parser = new DefaultJsonParser(new GrpcHttpApiOptions().JsonFormatter);
            var lorem = new Bogus.DataSets.Lorem("en");
            HelloReply reply = new HelloReply()
            {             
                Message = lorem.Sentence(),
                NullableMessage = null
            };
            reply.Values.AddRange(lorem.Sentences(10).Split("\n"));
            var orginJson = parser.ToJson(reply);
            object testObj = System.Text.Json.JsonSerializer.Deserialize(orginJson, typeof(HelloReply));

            //act 

            var actJson = parser.ToJson(testObj);

            //assert
            actJson.Should().Equals(orginJson);
        }


        [Fact]
        public void FromJson_SimpleUse_ShouldBeSucces()
        {
            //arrange
            var parser = new DefaultJsonParser(new GrpcHttpApiOptions().JsonFormatter);

            SubMessage message = new SubMessage() { SubField = "Test" };
            message.SubFields.Add("message1");
            message.SubFields.Add("message2");
            message.SubFields.Add("message3");

            var json = parser.ToJson(message);

            //act 
            var actualMessage1 = parser.FromJson<SubMessage>(json);

            //use cache type
            var actualMessage2 = parser.FromJson<SubMessage>(json);

            //assert
            actualMessage1.Should().NotBeNull();
            actualMessage1.SubField.Should().Equals("Test");
            actualMessage1.SubFields.Should().HaveCount(3);
            actualMessage1.SubFields[0].Should().Equals("message1");
            actualMessage1.SubFields[1].Should().Equals("message2");
            actualMessage1.SubFields[2].Should().Equals("message3");


            actualMessage2.Should().NotBeNull();
            actualMessage2.SubField.Should().Equals("Test");
            actualMessage2.SubFields.Should().HaveCount(3);
            actualMessage2.SubFields[0].Should().Equals("message1");
            actualMessage2.SubFields[1].Should().Equals("message2");
            actualMessage2.SubFields[2].Should().Equals("message3");
        }


        [Fact]
        public void FromJson_NotIMessageType_ShouldThrow()
        {
            //arrange
            var parser = new DefaultJsonParser(new GrpcHttpApiOptions().JsonFormatter);
            var json = "{\"name\":\"wong\"}";


            //act 

            Action action = ()=> parser.FromJson(json,typeof(TestJsonObject));

            //assert
            action.Should().Throw<InvalidCastException>().WithMessage("Message is not a Protobuf Message");
        }



    }
    internal class TestJsonObject 
    {
        public string Name { get; set; }
    
    }

}
