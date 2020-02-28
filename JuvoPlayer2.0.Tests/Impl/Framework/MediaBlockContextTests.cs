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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JuvoPlayer2_0.Impl.Framework;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using static JuvoPlayer2_0.Tests.Impl.Framework.TestUtils;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    [TestFixture]
    public class MediaBlockContextTests
    {
        [Test]
        public async Task Start_Called_InitializesBlock()
        {
            var mediaBlockMock = StubMediaBlock();
            var context = Create(new CreateArgs
            {
                MediaBlock = mediaBlockMock
            });

            context.Start();
            context.Stop();
            await context.Completion;

            await mediaBlockMock.Received().Init(Arg.Is(context));
        }

        [TestCase(PadDirection.Sink, EventFlags.IsPrioritized)]
        [TestCase(PadDirection.Sink, EventFlags.None)]
        [TestCase(PadDirection.Source, EventFlags.IsPrioritized)]
        [TestCase(PadDirection.Source, EventFlags.None)]
        public async Task HandlePadEvent_SingleEventSinglePad_DeliveredToBlock(PadDirection direction, EventFlags flags)
        {
            var mediaBlockMock = StubMediaBlock();
            var @event = StubEvent(flags);
            var padStub = StubPad(direction)
                .WithEvent(@event);
            var createArgs = new CreateArgs
            {
                MediaBlock = mediaBlockMock,
            };
            createArgs.AddPad(padStub);
            var context = Create(createArgs);

            await context.HandleEventOrWait(CancellationToken.None);

            await mediaBlockMock.Received().HandlePadEvent(Arg.Is(padStub), Arg.Is(@event));
        }

        [Flags]
        public enum Priority
        {
            None = 0,
            Normal = 1,
            High = 2,
        }

        [TestCase(Priority.High, Priority.None, PadDirection.Sink, Priority.High)]
        [TestCase(Priority.High | Priority.Normal, Priority.None, PadDirection.Sink, Priority.High)]
        [TestCase(Priority.Normal, Priority.None, PadDirection.Sink, Priority.Normal)]
        [TestCase(Priority.None, Priority.High, PadDirection.Source, Priority.High)]
        [TestCase(Priority.None, Priority.High | Priority.Normal, PadDirection.Source, Priority.High)]
        [TestCase(Priority.None, Priority.Normal, PadDirection.Source, Priority.Normal)]
        [TestCase(Priority.High, Priority.Normal, PadDirection.Sink, Priority.High)]
        [TestCase(Priority.High | Priority.Normal, Priority.Normal, PadDirection.Sink, Priority.High)]
        [TestCase(Priority.Normal, Priority.High, PadDirection.Source, Priority.High)]
        [TestCase(Priority.Normal, Priority.High | Priority.Normal, PadDirection.Source, Priority.High)]
        public async Task HandlePadEvent_SingleEventMultiplePads_DeliveredToBlock(Priority sinkPriority,
            Priority sourcePriority, PadDirection expectedPad, Priority expectedEventPriority)
        {
            var mediaBlockMock = StubMediaBlock();

            var events = new Dictionary<Priority, IEvent>
            {
                [Priority.High] = StubEvent(EventFlags.IsPrioritized),
                [Priority.Normal] = StubEvent(EventFlags.None)
            };

            var pads = new Dictionary<PadDirection, IInputPad>
            {
                [PadDirection.Sink] = CreatePad(PadDirection.Sink, sinkPriority,
                    events),
                [PadDirection.Source] = CreatePad(PadDirection.Source, sourcePriority,
                    events),
            };

            var createArgs = new CreateArgs
            {
                MediaBlock = mediaBlockMock,
            };
            createArgs.AddPad(pads[PadDirection.Sink]);
            createArgs.AddPad(pads[PadDirection.Source]);
            var context = Create(createArgs);

            await context.HandleEventOrWait(CancellationToken.None);

            await mediaBlockMock.Received()
                .HandlePadEvent(Arg.Is(pads[expectedPad]), Arg.Is(events[expectedEventPriority]));
        }

        [Test]
        public async Task HandlePadEvent_SinglePadNoEvents_WaitsForNewEvents()
        {
            var padStub = StubPad(PadDirection.Sink)
                .WithoutEvents();
            var createArgs = new CreateArgs();
            createArgs.AddPad(padStub);
            var context = Create(createArgs);

            await context.HandleEventOrWait(CancellationToken.None);

            await padStub.ReceivedWithAnyArgs().WaitToReadAsync(default);
            await padStub.ReceivedWithAnyArgs().WaitToPriorityReadAsync(default);
        }

        [Test]
        public async Task Flushing_FlushStartAndStopEventsSent_AllPendingEventsAreDropped()
        {
            var mediaBlockMock = StubMediaBlock();
            var flushStart = new FlushStartEvent();
            var normalEvent = StubEvent();
            var flushStop = new FlushStopEvent();
            var padStub = StubPad(PadDirection.Sink)
                .WithEvent(flushStart);
            var createArgs = new CreateArgs
            {
                MediaBlock = mediaBlockMock
            };
            createArgs.AddPad(padStub);
            var context = Create(createArgs);

            await context.HandleEventOrWait(CancellationToken.None); // FlushStart event is received
            padStub.WithoutEvents();
            padStub.WithEvent(normalEvent);
            await context.HandleEventOrWait(CancellationToken.None); // normal event is received but dropped
            await context.HandleEventOrWait(CancellationToken.None); // normal event is received but dropped
            padStub.WithEvent(flushStop);
            await context.HandleEventOrWait(CancellationToken.None); // FlushStop event is received

            Assert.That(mediaBlockMock.ReceivedCalls().Count(), Is.EqualTo(2));
            await mediaBlockMock.DidNotReceive().HandlePadEvent(padStub, Arg.Is(normalEvent));
        }

        private IInputPad CreatePad(PadDirection type, Priority priority, Dictionary<Priority, IEvent> events)
        {
            var pad = StubPad(type);
            if (priority == Priority.None)
                pad.WithoutEvents();
            if (priority.HasFlag(Priority.Normal))
                pad.WithEvent(events[Priority.Normal]);
            if (priority.HasFlag(Priority.High))
                pad.WithEvent(events[Priority.High]);
            return pad;
        }

        private static MediaBlockContext Create(CreateArgs args = null)
        {
            if (args == null)
                args = new CreateArgs();

            if (args.MediaBlock == null)
                args.MediaBlock = StubMediaBlock();
            if (args.SinkPads == null)
                args.AddPad(StubPad(PadDirection.Sink));
            if (args.SourcePads == null)
                args.AddPad(StubPad(PadDirection.Source));

            return new MediaBlockContext(args.MediaBlock, args.SinkPads, args.SourcePads);
        }

        private class CreateArgs
        {
            public IMediaBlock MediaBlock { get; set; }
            public IList<IInputPad> SourcePads { get; set; }
            public IList<IInputPad> SinkPads { get; set; }

            public void AddPad(IInputPad pad)
            {
                switch (pad.Direction)
                {
                    case PadDirection.Source:
                        if (SourcePads == null)
                            SourcePads = new List<IInputPad>();
                        SourcePads.Add(pad);
                        break;
                    case PadDirection.Sink:
                        if (SinkPads == null)
                            SinkPads = new List<IInputPad>();
                        SinkPads.Add(pad);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }


        private static IMediaBlock StubMediaBlock()
        {
            return Substitute.For<IMediaBlock>();
        }
    }
}