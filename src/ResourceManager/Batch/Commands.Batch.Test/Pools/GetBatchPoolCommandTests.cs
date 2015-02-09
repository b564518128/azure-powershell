﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Protocol;
using Microsoft.Azure.Batch.Protocol.Entities;
using Microsoft.Azure.Commands.Batch.Models;
using Microsoft.WindowsAzure.Commands.ScenarioTest;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.Commands.Batch.Test.Pools
{
    public class GetBatchPoolCommandTests
    {
        private GetBatchPoolCommand cmdlet;
        private Mock<BatchClient> batchClientMock;
        private Mock<ICommandRuntime> commandRuntimeMock;

        public GetBatchPoolCommandTests()
        {
            batchClientMock = new Mock<BatchClient>();
            commandRuntimeMock = new Mock<ICommandRuntime>();
            cmdlet = new GetBatchPoolCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                BatchClient = batchClientMock.Object,
            };
        }

        [Fact]
        [Trait(Category.AcceptanceType, Category.CheckIn)]
        public void GetBatchPoolTest()
        {
            // Setup cmdlet to get a WorkItem by name
            BatchAccountContext context = BatchTestHelpers.CreateBatchContextWithKeys();
            cmdlet.BatchContext = context;
            cmdlet.Name = "testPool";
            cmdlet.Filter = null;

            // Build a Pool instead of querying the service on a GetPool call
            YieldInjectionInterceptor interceptor = new YieldInjectionInterceptor((opContext, request) =>
            {
                if (request is GetPoolRequest)
                {
                    GetPoolResponse response = BatchTestHelpers.CreateGetPoolResponse(cmdlet.Name);
                    Task<object> task = Task<object>.Factory.StartNew(() => { return response; });
                    return task;
                }
                return null;
            });
            cmdlet.AdditionalBehaviors = new List<BatchClientBehavior>() { interceptor };

            // Setup the cmdlet to write pipeline output to a list that can be examined later
            List<PSCloudPool> pipeline = new List<PSCloudPool>();
            commandRuntimeMock.Setup(r => r.WriteObject(It.IsAny<PSCloudPool>())).Callback<object>(p => pipeline.Add((PSCloudPool)p));

            cmdlet.ExecuteCmdlet();

            // Verify that the cmdlet wrote the Pool returned from the OM to the pipeline
            Assert.Equal(1, pipeline.Count);
            Assert.Equal(cmdlet.Name, pipeline[0].Name);
        }

        [Fact]
        [Trait(Category.AcceptanceType, Category.CheckIn)]
        public void ListBatchPoolByODataFilterTest()
        {
            // Setup cmdlet to list WorkItems using an OData filter
            BatchAccountContext context = BatchTestHelpers.CreateBatchContextWithKeys();
            cmdlet.BatchContext = context;
            cmdlet.Name = null;
            cmdlet.Filter = "startswith(name,'test')";

            string[] namesOfConstructedPools = new[] { "test1", "test2" };

            // Build some Pools instead of querying the service on a ListPools call
            YieldInjectionInterceptor interceptor = new YieldInjectionInterceptor((opContext, request) =>
            {
                if (request is ListPoolsRequest)
                {
                    ListPoolsResponse response = BatchTestHelpers.CreateListPoolsResponse(namesOfConstructedPools);
                    Task<object> task = Task<object>.Factory.StartNew(() => { return response; });
                    return task;
                }
                return null;
            });
            cmdlet.AdditionalBehaviors = new List<BatchClientBehavior>() { interceptor };

            // Setup the cmdlet to write pipeline output to a list that can be examined later
            List<PSAsyncEnumerable<PSCloudPool, ICloudPool>> pipeline = new List<PSAsyncEnumerable<PSCloudPool, ICloudPool>>();
            commandRuntimeMock.Setup(r =>
                r.WriteObject(It.IsAny<PSAsyncEnumerable<PSCloudPool, ICloudPool>>()))
                .Callback<object>(p => pipeline.Add((PSAsyncEnumerable<PSCloudPool, ICloudPool>)p));

            cmdlet.ExecuteCmdlet();

            // Verify that the cmdlet wrote the enumerator to the pipeline and that it contains the constructed Pools
            Assert.Equal(1, pipeline.Count);
            int poolCount = 0;
            foreach (PSCloudPool p in pipeline[0])
            {
                Assert.True(namesOfConstructedPools.Contains(p.Name));
                poolCount++;
            }
            Assert.Equal(namesOfConstructedPools.Length, poolCount);
        }

        [Fact]
        [Trait(Category.AcceptanceType, Category.CheckIn)]
        public void ListBatchPoolWithoutFiltersTest()
        {
            // Setup cmdlet to list Pools without filters
            BatchAccountContext context = BatchTestHelpers.CreateBatchContextWithKeys();
            cmdlet.BatchContext = context;
            cmdlet.Name = null;
            cmdlet.Filter = null;

            string[] namesOfConstructedPools = new[] { "name1", "name2", "name3" };

            // Build some WorkItems instead of querying the service on a ListWorkItems call
            YieldInjectionInterceptor interceptor = new YieldInjectionInterceptor((opContext, request) =>
            {
                if (request is ListPoolsRequest)
                {
                    ListPoolsResponse response = BatchTestHelpers.CreateListPoolsResponse(namesOfConstructedPools);
                    Task<object> task = Task<object>.Factory.StartNew(() => { return response; });
                    return task;
                }
                return null;
            });
            cmdlet.AdditionalBehaviors = new List<BatchClientBehavior>() { interceptor };

            // Setup the cmdlet to write pipeline output to a list that can be examined later
            List<PSAsyncEnumerable<PSCloudPool, ICloudPool>> pipeline = new List<PSAsyncEnumerable<PSCloudPool, ICloudPool>>();
            commandRuntimeMock.Setup(r =>
                r.WriteObject(It.IsAny<PSAsyncEnumerable<PSCloudPool, ICloudPool>>()))
                .Callback<object>(p => pipeline.Add((PSAsyncEnumerable<PSCloudPool, ICloudPool>)p));

            cmdlet.ExecuteCmdlet();

            // Verify that the cmdlet wrote the enumerator to the pipeline and that it contains the constructed Pools
            Assert.Equal(1, pipeline.Count);
            int poolCount = 0;
            foreach (PSCloudPool p in pipeline[0])
            {
                Assert.True(namesOfConstructedPools.Contains(p.Name));
                poolCount++;
            }
            Assert.Equal(namesOfConstructedPools.Length, poolCount);
        }
    }
}
