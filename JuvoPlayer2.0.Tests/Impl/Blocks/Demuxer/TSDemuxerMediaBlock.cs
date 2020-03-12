/*!
 * https://github.com/SamsungDForum/JuvoPlayer
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
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JuvoPlayer.Common;
using JuvoPlayer.Demuxers;
using JuvoPlayer2_0.Impl.Blocks.Demuxer;
using JuvoPlayer2_0.Impl.Common;
using JuvoPlayer2_0.Impl.Framework;
using Nito.AsyncEx;
using NSubstitute;
using NUnit.Framework;

namespace JuvoPlayer2_0.Tests.Impl.Blocks.Demuxer
{
    [TestFixture]
    public class TSDemuxerMediaBlock
    {
        private class BlockWrapper
        {
            public BlockWrapper()
            {
                PadStub = Substitute.For<IPad>();
                DemuxerStub = Substitute.For<IDemuxer>();
                ContextStub = Substitute.For<IMediaBlockContext>();
                ContentChangedEvent = new ContentChangedEvent();
                FlushStartEvent = new FlushStartEvent();
                EosEvent = new EosEvent();
                MediaBlock = new DemuxerMediaBlock(DemuxerStub);
                SrcAudioPad = Substitute.For<IPad>();
                SrcAudioPad.Type.Returns(MediaType.Audio);
                SrcVideoPad = Substitute.For<IPad>();
                SrcVideoPad.Type.Returns(MediaType.Video);
                ContextStub.SourcePads.Returns(new List<IPad> {SrcAudioPad, SrcVideoPad});
            }

            public IList<IProperty> Init()
            {
                return MediaBlock.Init(ContextStub);
            }

            public Task HandlePadEvent(IEvent @event)
            {
                return MediaBlock.HandlePadEvent(PadStub, @event);
            }

            public async Task InitAndHandleContentChangedEvent()
            {
                Init();
                await HandlePadEvent(ContentChangedEvent);
                await Task.Yield();
            }

            public async Task InitAndHandleFlushStartEvent()
            {
                Init();
                await HandlePadEvent(FlushStartEvent);
                await Task.Yield();
            }

            public async Task InitAndHandleEosEvent()
            {
                Init();
                await HandlePadEvent(EosEvent);
                await Task.Yield();
            }

            public IPad PadStub { get; }
            public IDemuxer DemuxerStub { get; }
            public IMediaBlockContext ContextStub { get; }
            public ContentChangedEvent ContentChangedEvent { get; }
            public FlushStartEvent FlushStartEvent { get; }
            public EosEvent EosEvent { get; }
            public DemuxerMediaBlock MediaBlock { get; }
            public IPad SrcAudioPad { get; }
            public IPad SrcVideoPad { get; }
        }

        [Test]
        public void Init_NoSrcPads_ThrowsInvalidOperationException()
        {
            var demuxerStub = Substitute.For<IDemuxer>();
            var contextStub = Substitute.For<IMediaBlockContext>();
            var block = new DemuxerMediaBlock(demuxerStub);

            Assert.Throws<InvalidOperationException>(() => block.Init(contextStub));
        }

        [Test]
        public void Init_SrcPadsWithInvalidMediaType_ThrowsInvalidOperationException()
        {
            var demuxerStub = Substitute.For<IDemuxer>();
            var contextStub = Substitute.For<IMediaBlockContext>();
            var srcPadStub = Substitute.For<IPad>();
            srcPadStub.Type.Returns(MediaType.Unknown);
            contextStub.SourcePads.Returns(new List<IPad> {srcPadStub});
            var block = new DemuxerMediaBlock(demuxerStub);

            Assert.Throws<InvalidOperationException>(() => block.Init(contextStub));
        }

        [Test]
        public void ContentChangedEvent_EventReceived_EventForwarded()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var contextMock = wrapper.ContextStub;

                await wrapper.InitAndHandleContentChangedEvent();

                await contextMock.Received()
                    .ForwardEvent(Arg.Is(wrapper.ContentChangedEvent), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerIsInitialized_CompletesDemuxer()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerMock = wrapper.DemuxerStub;
                demuxerMock.IsInitialized().Returns(true);

                await wrapper.InitAndHandleContentChangedEvent();

                demuxerMock.Received().Complete();
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerIsNotInitialized_CompletesDemuxer()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerMock = wrapper.DemuxerStub;

                await wrapper.InitAndHandleContentChangedEvent();

                demuxerMock.DidNotReceive().Complete();
            });
        }

        [Test]
        public void ContentChangedEvent_EventReceived_ResetsDemuxer()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerMock = wrapper.DemuxerStub;

                await wrapper.InitAndHandleContentChangedEvent();

                demuxerMock.Received().Reset();
            });
        }

        [Test]
        public void ContentChangedEvent_EventReceived_InitsDemuxer()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerMock = wrapper.DemuxerStub;

                await wrapper.InitAndHandleContentChangedEvent();

                await demuxerMock.Received().InitForEs();
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerInitFails_SendsErrorEvent()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                demuxerStub.InitForEs()
                    .Returns(Task.FromException<ClipConfiguration>(new Exception()));
                var contextMock = wrapper.ContextStub;

                await wrapper.InitAndHandleContentChangedEvent();

                await contextMock.Received().ForwardEvent(Arg.Any<ErrorEvent>(), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerReturnsDuration_SendsDuration()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                var duration = TimeSpan.FromSeconds(1);
                demuxerStub.InitForEs()
                    .Returns(Task.FromResult(new ClipConfiguration {Duration = duration}));
                var contextMock = wrapper.ContextStub;

                await wrapper.InitAndHandleContentChangedEvent();

                await contextMock.Received().ForwardEvent(Arg.Any<ClipDurationEvent>(), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerReturnsVideoStreamConfig_SendsStreamConfigOnVideoPad()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                var streamConfig = new VideoStreamConfig();
                demuxerStub.InitForEs().Returns(Task.FromResult(new ClipConfiguration
                    {StreamConfigs = new List<StreamConfig> {streamConfig}}));
                var videoPadMock = wrapper.SrcVideoPad;

                await wrapper.InitAndHandleContentChangedEvent();

                await videoPadMock.Received().SendEvent(Arg.Any<StreamConfigEvent>(), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerReturnsAudioStreamConfig_SendsStreamConfigOnAudioPad()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                var streamConfig = new AudioStreamConfig();
                demuxerStub.InitForEs().Returns(Task.FromResult(new ClipConfiguration
                    {StreamConfigs = new List<StreamConfig> {streamConfig}}));
                var audioPadMock = wrapper.SrcAudioPad;

                await wrapper.InitAndHandleContentChangedEvent();

                await audioPadMock.Received().SendEvent(Arg.Any<StreamConfigEvent>(), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerReturnsVideoDRMInitData_SendsDRMInitDataEventOnVideoPad()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                var drmInitData = new DRMInitData {StreamType = StreamType.Video};
                demuxerStub.InitForEs().Returns(Task.FromResult(new ClipConfiguration
                    {DrmInitDatas = new List<DRMInitData> {drmInitData}}));
                var videoPadMock = wrapper.SrcVideoPad;

                await wrapper.InitAndHandleContentChangedEvent();

                await videoPadMock.Received().SendEvent(Arg.Any<DRMInitDataEvent>(), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void ContentChangedEvent_DemuxerReturnsAudioDRMInitData_SendsDRMInitDataEventOnAudioPad()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                var drmInitData = new DRMInitData {StreamType = StreamType.Audio};
                demuxerStub.InitForEs().Returns(Task.FromResult(new ClipConfiguration
                    {DrmInitDatas = new List<DRMInitData> {drmInitData}}));
                var audioPadMock = wrapper.SrcAudioPad;

                await wrapper.InitAndHandleContentChangedEvent();

                await audioPadMock.Received().SendEvent(Arg.Any<DRMInitDataEvent>(), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void Packets_DemuxerReturnsVideoPacket_SendsPacketEventOnVideoPad()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                demuxerStub.IsInitialized().Returns(true);
                var packet = new Packet {StreamType = StreamType.Video};
                demuxerStub.NextPacket().Returns(Task.FromResult(packet));
                var videoPadMock = wrapper.SrcVideoPad;

                await wrapper.InitAndHandleContentChangedEvent();
                await Task.Yield();

                await videoPadMock.Received().SendEvent(Arg.Any<PacketEvent>(), Arg.Any<CancellationToken>());
                wrapper.MediaBlock.Dispose();
            });
        }

        [Test]
        public void Packets_DemuxerReturnsAudioPacket_SendsPacketEventOnAudioPad()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerStub = wrapper.DemuxerStub;
                demuxerStub.IsInitialized().Returns(true);
                var packet = new Packet {StreamType = StreamType.Audio};
                demuxerStub.NextPacket().Returns(Task.FromResult(packet));
                var audioPadMock = wrapper.SrcAudioPad;

                await wrapper.InitAndHandleContentChangedEvent();
                await Task.Yield();

                await audioPadMock.Received().SendEvent(Arg.Any<PacketEvent>(), Arg.Any<CancellationToken>());
                wrapper.MediaBlock.Dispose();
            });
        }

        [Test]
        public void FlushStartEvent_EventReceived_EventForwarded()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var contextMock = wrapper.ContextStub;

                await wrapper.InitAndHandleFlushStartEvent();

                await contextMock.Received().ForwardEvent(Arg.Is(wrapper.FlushStartEvent));
            });
        }

        [Test]
        public void FlushStartEvent_EventReceived_ResetsDemuxer()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerMock = wrapper.DemuxerStub;

                await wrapper.InitAndHandleFlushStartEvent();

                demuxerMock.Received().Reset();
            });
        }

        [Test]
        public void EosEvent_EventReceived_EventForwarded()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var contextMock = wrapper.ContextStub;

                await wrapper.InitAndHandleEosEvent();

                await contextMock.Received().ForwardEvent(Arg.Is(wrapper.EosEvent), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public void EosEvent_DemuxerInitialized_CompletesDemuxer()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerMock = wrapper.DemuxerStub;
                demuxerMock.IsInitialized().Returns(true);

                await wrapper.InitAndHandleEosEvent();

                demuxerMock.Received().Complete();
            });
        }

        [Test]
        public void EosEvent_DemuxerNotInitialized_DoesNotCompleteDemuxer()
        {
            AsyncContext.Run(async () =>
            {
                var wrapper = new BlockWrapper();
                var demuxerMock = wrapper.DemuxerStub;

                await wrapper.InitAndHandleEosEvent();

                demuxerMock.DidNotReceive().Complete();
            });
        }
    }
}