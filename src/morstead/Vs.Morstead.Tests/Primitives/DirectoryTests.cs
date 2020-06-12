﻿using EasyCompressor;
using Microsoft.AspNetCore.Connections;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VirtualSociety.VirtualSocietyDid;
using Vs.Morstead.Grains.Interfaces.Content;
using Vs.Morstead.Grains.Interfaces.Primitives.Directory;
using Xunit;

namespace Vs.Morstead.Tests.Primitives
{
    public class DirectoryTests : IClassFixture<ClusterFixture>
    {
        private readonly TestCluster cluster;

        public DirectoryTests(ClusterFixture fixture)
        {
            cluster = fixture.Cluster;
        }

        [Fact]
        public void CanCreateNestedDirectoriesInState()
        {
            var state = new DirectoryState();
            state.Dir = new NestedDictionary<string, DirectoryItem>();
            var dir1 = state.CreateDir("//test1/test2/test3");
            var dir2 = state.CreateDir("test5//test2///test3");
            Assert.StartsWith("did:vsoc:mstd:dir", dir1.ItemsGrainId);
            Assert.StartsWith("did:vsoc:mstd:dir", dir2.ItemsGrainId);
            Assert.True(state.Dir["test1"].ContainsKey("test2"));
            Assert.True(state.Dir["test1"]["test2"].ContainsKey("test3"));
            Assert.True(state.Dir["test5"].ContainsKey("test2"));
            Assert.True(state.Dir["test5"]["test2"].ContainsKey("test3"));
        }

        [Fact]
        public async void CanReadNestedDirectoriesInGrain()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var did = new Did("mstd:dir").ToString();
            var dir = cluster.GrainFactory.GetGrain<IDirectoryGrain>(did);
            Assert.False(await dir.DirectoryExists("//test1/test2/test3"));
            var dir1 = await dir.CreateDirectory("//test1/test2/test3");
            Assert.True(await dir.DirectoryExists("//test1/test2/test3"));
            Assert.StartsWith("did:vsoc:mstd:dir", dir1.ItemsGrainId);
            var dir2 = await dir.GetDirectory("//test1/test2//test3");
            Assert.Equal(dir1.ItemsGrainId, dir2.ItemsGrainId);
            // Add multiple content items to directory (file contents + directory meta information/created/modified)
            var dirContents = cluster.GrainFactory.GetGrain<IDirectoryContentsGrain>(dir2.ItemsGrainId);
            for (int i = 0; i < 5; i++)
            {
                var contentDid = new Did("mstd:pub").ToString();
                var content = cluster.GrainFactory.GetGrain<IContentPersistentGrain>(contentDid);
                // pointer to content for this directory.
                await dirContents.AddItem(new DirectoryContentsItem()
                {
                    MetaData = Newtonsoft.Json.JsonConvert.SerializeObject(new { FileName = $"file-{i}.txt" }),
                    GrainId = contentDid,
                    Interface = typeof(IContentPersistentGrain)
                });
                await content.Save(new System.Net.Mime.ContentType("text/html"), Encoding.UTF8, "Unit Test, Hello World");
            }
            // read the directory again, check if there's equal number of entries and change the file contents
            dirContents = cluster.GrainFactory.GetGrain<IDirectoryContentsGrain>(dir2.ItemsGrainId);
            var list = await dirContents.ListItems();
            Assert.NotEmpty(list.Items);
            Assert.Equal(5, list.Items.Count);
            for (int i = 0; i < 5; i++)
            {
                var content = cluster.GrainFactory.GetGrain<IContentPersistentGrain>(list.Items.ElementAt(i).Value.GrainId);
                var content2state = await content.Load();
                var contentsDocument = content2state.Encoding.GetString(content2state.Content);
                Assert.Equal(Newtonsoft.Json.JsonConvert.SerializeObject(new { FileName = $"file-{i}.txt" }), list.Items.ElementAt(i).Value.MetaData);
                Assert.Equal("Unit Test, Hello World", contentsDocument);
                await content.Save(content2state.ContentType, content2state.Encoding, content2state.Encoding.GetBytes($"{contentsDocument} {i}"));
            }
            // read the directory again, and check if all content items have different file contents and the modified date differs from the created date.
            dirContents = cluster.GrainFactory.GetGrain<IDirectoryContentsGrain>(dir2.ItemsGrainId);
            list = await dirContents.ListItems();
            for (int i = 0; i < 5; i++)
            {
                //TODO: Update directory modified datetime.
                //Assert.NotEqual<long>(list.Items.ElementAt(i).Value.Created.Value.Ticks, list.Items.ElementAt(i).Value.Modified.Value.Ticks);
                var content = cluster.GrainFactory.GetGrain<IContentPersistentGrain>(list.Items.ElementAt(i).Value.GrainId);
                var content2state = await content.Load();
                var contentsDocument = content2state.Encoding.GetString(content2state.Content);
                Assert.Equal(Newtonsoft.Json.JsonConvert.SerializeObject(new { FileName = $"file-{i}.txt" }), list.Items.ElementAt(i).Value.MetaData);
                Assert.Equal($"Unit Test, Hello World {i}", contentsDocument);
            }

            stopwatch.Stop();
        }
    }
}
