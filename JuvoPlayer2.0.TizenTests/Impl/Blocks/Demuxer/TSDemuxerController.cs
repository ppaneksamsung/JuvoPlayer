/*!
 *
 * [https://github.com/SamsungDForum/JuvoPlayer])
 * Copyright 2020, Samsung Electronics Co., Ltd
 * Licensed under the MIT license
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 */

using System;
using System.Threading.Tasks;
using JuvoPlayer.Common;
using JuvoPlayer.Demuxers.FFmpeg;
using JuvoPlayer.TizenTests.Utils;
using JuvoPlayer2_0.Impl.Blocks.Demuxer;
using JuvoPlayer2_0.Impl.Common;
using JuvoPlayer2_0.Impl.Framework;
using Nito.AsyncEx;
using NUnit.Framework;

namespace JuvoPlayer2_0.TizenTests.Impl.Blocks.Demuxer
{
    [TestFixture]
    public class TSDemuxerController
    {
        private DashContent _content;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var provider = new DashContentProvider();
            _content = provider.GetGoogleCar();
            Assert.That(_content.IsInitialized, Is.True);
        }

        [Test]
        public void NormalFlow_ChunksPushed_DemuxesStreamConfig()
        {
            RunDemuxerTest(async pipeline =>
            {
                while (await pipeline.WaitForSinkReadAsync())
                {
                    while (pipeline.TrySinkRead(out var @event))
                    {
                        if (@event.GetType() != typeof(StreamConfigEvent))
                            continue;
                        var streamConfigEvent = @event as StreamConfigEvent;
                        Assert.That(streamConfigEvent.Payload.StreamType(), Is.EqualTo(StreamType.Video));
                        return;
                    }
                }
            });
        }

        [Test]
        public void NormalFlow_ChunksPushed_DemuxesDuration()
        {
            RunDemuxerTest(async pipeline =>
            {
                while (await pipeline.WaitForSinkReadAsync())
                {
                    while (pipeline.TrySinkRead(out var @event))
                    {
                        if (@event.GetType() != typeof(ClipDurationEvent))
                            continue;
                        var clipDurationEvent = @event as ClipDurationEvent;
                        Assert.That(clipDurationEvent.Payload, Is.GreaterThan(TimeSpan.Zero));
                        return;
                    }
                }
            });
        }

        [Test]
        public void NormalFlow_ChunksPushed_DemuxesPackets()
        {
            RunDemuxerTest(async pipeline =>
            {
                while (await pipeline.WaitForSinkReadAsync())
                {
                    while (pipeline.TrySinkRead(out var @event))
                    {
                        if (@event.GetType() != typeof(PacketEvent))
                            continue;
                        var packetEvent = @event as PacketEvent;
                        var packet = packetEvent.Payload;
                        Assert.That(packet.StreamType, Is.EqualTo(StreamType.Video));
                        return;
                    }
                }
            });
        }

        private void RunDemuxerTest(Func<IMediaPipeline, Task> testImpl)
        {
            AsyncContext.Run(async () =>
            {
                var pipeline = CreateDemuxerPipeline();
                try
                {
                    pipeline.Init();
                    pipeline.Start();

                    await pipeline.SendSrc(new ContentChangedEvent());
                    await pipeline.SendSrc(new ChunkEvent(_content.InitSegment));
                    foreach (var segment in _content.Segments)
                        await pipeline.SendSrc(new ChunkEvent(segment));

                    await testImpl(pipeline);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Assert.Fail();
                }
                finally
                {
                    pipeline.Stop();
                }
            });
        }

        private static DemuxerMediaBlock CreateDemuxerMediaBlock()
        {
            var demuxer = new FFmpegDemuxer(new FFmpegGlue());
            var controller = new DemuxerMediaBlock(demuxer);
            return controller;
        }

        private static IMediaPipeline CreateDemuxerPipeline()
        {
            var builder = new MediaPipelineBuilder();
            var block = CreateDemuxerMediaBlock();
            builder
                .SetSrc(block)
                .SetSink(block, MediaType.Video);
            return builder.CreatePipeline();
        }
    }
}