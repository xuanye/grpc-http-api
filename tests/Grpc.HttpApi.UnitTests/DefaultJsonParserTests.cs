using Grpc.HttpApi.Implements;
using System;
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
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            Assert.Equal("{ \"message\": \"hello\" }", json);
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
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            Assert.Equal("{ }", json);
           
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
            Assert.Null(json); 
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
            reply.ListMessage.AddRange(lorem.Sentences(10).Split("\n"));
            var orginJson = parser.ToJson(reply);

            
            object testObj = parser.FromJson(orginJson, typeof(HelloReply)) ;

            //act 

            var actJson = parser.ToJson(testObj);

            //assert
            Assert.Equal(orginJson, actJson);
           
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
            Assert.NotNull(actualMessage1);
            Assert.Equal("Test", actualMessage1.SubField);
            Assert.Equal(3, actualMessage1.SubFields.Count);
            Assert.Equal("message1", actualMessage1.SubFields[0]);
            Assert.Equal("message2", actualMessage1.SubFields[1]);
            Assert.Equal("message3", actualMessage1.SubFields[2]);


            Assert.NotNull(actualMessage2);
            Assert.Equal("Test", actualMessage2.SubField);
            Assert.Equal(3, actualMessage2.SubFields.Count);
            Assert.Equal("message1", actualMessage2.SubFields[0]);
            Assert.Equal("message2", actualMessage2.SubFields[1]);
            Assert.Equal("message3", actualMessage2.SubFields[2]);

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
            var ex = Assert.Throws<InvalidCastException>(action);
            Assert.Equal("Message is not a Protobuf Message", ex.Message);
           
        }



    }
    internal class TestJsonObject 
    {
        public string Name { get; set; }
    
    }

}
