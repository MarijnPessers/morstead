﻿using Flee.PublicTypes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Xml.Linq;
using Vs.Morstead.Bpm.Core;
using Vs.Morstead.Bpm.Model.Gateways;
using Vs.Morstead.Bpm.TestData;
using Xunit;

namespace Vs.Morstead.Bpm.Model.Tests
{
    public class PrimitiveTests
    {
        [Fact]
        public void ExclusiveGatewayCanRead()
        {
            var doc = XDocument.Parse(TestFileLoader.Load(@"Bpmn20/simple-exclusive-gateway.bpmn"));
            string json = JsonConvert.SerializeXNode(doc);
            JObject bpmn = JObject.Parse(json);
            var process = bpmn["bpmn:definitions"]["bpmn:process"];
            var parameters = new BpmParametersCollection();
            BpmExclusiveGateway gateway = new BpmExclusiveGateway(parameters, process, "Gateway_1d2x0fd");
            Assert.Equal("Gateway_1d2x0fd", gateway.Id);
            Assert.Single(gateway.Inputs);
            Assert.Equal("Flow_0hzr3el", gateway.Inputs[0]);
            Assert.Equal(2, gateway.Outputs.Count);
            Assert.Equal("Flow_0gzbcgc", gateway.Outputs[0]);
            Assert.Equal("Flow_1fg2zon", gateway.Outputs[1]);
        }

        [Fact]
        public void ConditionalFlowCanReadAndEvaluateTFormalExpression()
        {
            var doc = XDocument.Parse(TestFileLoader.Load(@"Bpmn20/simple-exclusive-gateway.bpmn"));
            string json = JsonConvert.SerializeXNode(doc);
            JObject bpmn = JObject.Parse(json);
            var process = bpmn["bpmn:definitions"]["bpmn:process"];
            var flow = process["bpmn:sequenceFlow"].Where(p => p["@id"].Value<string>() == "Flow_0gzbcgc").Single()["bpmn:conditionExpression"]["#text"];
            ExpressionContext context = new ExpressionContext();
            context.Variables["A"] = 1;
            IGenericExpression<bool> eDynamic = context.CompileGeneric<bool>((string)flow);
            var result = eDynamic.Evaluate();
            Assert.True(result);
            flow = process["bpmn:sequenceFlow"].Where(p => p["@id"].Value<string>() == "Flow_1fg2zon").Single()["bpmn:conditionExpression"]["#text"];
            eDynamic = context.CompileGeneric<bool>((string)flow);
            result = eDynamic.Evaluate();
            Assert.False(result);
        }
    }
}
