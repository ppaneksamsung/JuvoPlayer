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
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JuvoPlayer2_0.Impl.Framework;
using NSubstitute;
using NUnit.Framework;
using static JuvoPlayer2_0.Tests.Impl.Framework.TestUtils;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    [TestFixture]
    public class PadTests
    {
        [TestCase(PadDirection.Sink, EventFlags.Upstream)]
        [TestCase(PadDirection.Source, EventFlags.Downstream)]
        public async Task SendEvent_AllowedNormalEvent_SendsOnNormalWriter(PadDirection direction, EventFlags flags)
        {
            var @event = StubEvent(flags);
            var writer = StubWriter();
            var pad = CreatePad(new CreateArgs
            {
                Direction = direction,
                Writer = writer
            });

            await pad.SendEvent(@event);

            await writer.Received().WriteAsync(Arg.Is(@event));
        }

        [TestCase(PadDirection.Sink, EventFlags.Upstream)]
        [TestCase(PadDirection.Source, EventFlags.Downstream)]
        public async Task SendEvent_AllowedPriorityEvent_SendsOnPriorityWriter(PadDirection direction, EventFlags flags)
        {
            var @event = StubEvent(flags | EventFlags.IsPrioritized);
            var writer = StubWriter();
            var pad = CreatePad(new CreateArgs
            {
                Direction = direction,
                PriorityWriter = writer
            });

            await pad.SendEvent(@event);

            await writer.Received().WriteAsync(Arg.Is(@event));
        }

        [TestCase(PadDirection.Sink, EventFlags.Downstream)]
        [TestCase(PadDirection.Source, EventFlags.Upstream)]
        public void SendEvent_ProhibitedEvent_Rejects(PadDirection direction, EventFlags flags)
        {
            var @event = StubEvent(EventFlags.Upstream);
            var pad = CreatePad();

            Assert.ThrowsAsync<InvalidOperationException>(async () => { await pad.SendEvent(@event); });
        }

        [Test]
        public void SendEvent_SendingWhileFlushing_Rejects()
        {
            var pad = CreatePad();
            pad.IsFlushing = true;
            var @event = StubEvent();

            Assert.ThrowsAsync<InvalidOperationException>(async () => { await pad.SendEvent(@event); });
        }

        [Test]
        public void SendEvent_SendingWhileFlushing_DoesntRejectFlushStart()
        {
            var pad = CreatePad();
            pad.IsFlushing = true;
            var @event = new FlushStartEvent();

            Assert.DoesNotThrowAsync(async () => { await pad.SendEvent(@event); });
        }

        [Test]
        public void SendEvent_SendingWhileFlushing_DoesntRejectFlushStop()
        {
            var pad = CreatePad();
            pad.IsFlushing = true;
            var @event = new FlushStopEvent();

            Assert.DoesNotThrowAsync(async () => { await pad.SendEvent(@event); });
        }

        private class CreateArgs
        {
            public MediaType MediaType { get; set; }
            public PadDirection Direction { get; set; }
            public ChannelWriter<IEvent> Writer { get; set; }
            public ChannelReader<IEvent> Reader { get; set; }
            public ChannelWriter<IEvent> PriorityWriter { get; set; }
            public ChannelReader<IEvent> PriorityReader { get; set; }
        }

        private static Pad CreatePad(CreateArgs args = default)
        {
            if (args == default)
                args = new CreateArgs();
            if (args.Writer == null)
                args.Writer = StubWriter();
            if (args.Reader == null)
                args.Reader = StubReader();
            if (args.PriorityWriter == null)
                args.PriorityWriter = StubWriter();
            if (args.PriorityReader == null)
                args.PriorityReader = StubReader();
            return new Pad(args.MediaType, args.Direction, args.Writer, args.Reader, args.PriorityWriter,
                args.PriorityReader);
        }

        private static ChannelWriter<IEvent> StubWriter()
        {
            var writer = Substitute.ForPartsOf<ChannelWriter<IEvent>>();
            writer.WhenForAnyArgs(x => x.WriteAsync(Arg.Any<IEvent>(), Arg.Any<CancellationToken>())).DoNotCallBase();
            return writer;
        }

        private static ChannelReader<IEvent> StubReader()
        {
            var reader = Substitute.ForPartsOf<ChannelReader<IEvent>>();
            return reader;
        }
    }
}