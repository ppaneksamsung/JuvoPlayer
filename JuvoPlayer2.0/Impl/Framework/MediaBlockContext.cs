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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace JuvoPlayer2_0.Impl.Framework
{
    public class MediaBlockContext : IMediaBlockContext
    {
        private readonly AsyncContextThread _streamingThread;
        private bool _isFlushing;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public IList<IPad> SinkPads { get; }
        public IList<IPad> SourcePads { get; }
        public SynchronizationContext SynchronizationContext => _streamingThread.Context.SynchronizationContext;

        private class PadReader
        {
            private readonly bool _isPriority;

            public Pad Pad { get; }

            public PadReader(Pad pad, bool isPriority)
            {
                Pad = pad;
                _isPriority = isPriority;
            }

            public ValueTask<bool> WaitToReadAsync(CancellationToken token)
            {
                return _isPriority ? Pad.WaitToPriorityReadAsync(token) : Pad.WaitToReadAsync(token);
            }

            public bool TryRead(out IEvent @event)
            {
                return _isPriority ? Pad.TryPriorityRead(out @event) : Pad.TryRead(out @event);
            }
        }

        public IMediaBlock MediaBlock { get; }

        public MediaBlockContext(IMediaBlock mediaBlock)
        {
            _streamingThread = new AsyncContextThread();
            SinkPads = new List<IPad>();
            SourcePads = new List<IPad>();
            MediaBlock = mediaBlock;
        }

        public void Start()
        {
            _streamingThread.Factory.StartNew(async () =>
            {
                await MediaBlock.Init(this);
                await HandleEvents(_cancellationTokenSource.Token);
            });
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private IEnumerable<PadReader> AllPadReaders(bool isPriority)
        {
            var allPads = SinkPads.Concat(SourcePads).Cast<Pad>();
            return allPads.Select(pad => new PadReader(pad, isPriority));
        }

        private async Task HandleEvents(CancellationToken token)
        {
            var allReaders = AllPadReaders(true)
                .Concat(AllPadReaders(false))
                .ToList();

            while (!token.IsCancellationRequested)
            {
                for (var i = 0; i < allReaders.Count;)
                {
                    if (allReaders[i].TryRead(out var @event))
                    {
                        await HandleEvent(allReaders[i].Pad, @event, token);
                        i = 0;
                        continue;
                    }

                    ++i;
                }

                var waitPool = allReaders
                    .Select(reader => reader.WaitToReadAsync(token).AsTask())
                    .Where(waitingRead => !waitingRead.IsCompleted)
                    .ToList();

                var waitTask = waitPool.Count != allReaders.Count ? Task.CompletedTask : Task.WhenAny(waitPool);
                await waitTask;
            }
        }

        private Task HandleEvent(IPad targetPad, IEvent @event, CancellationToken token)
        {
            if (@event.GetType() == typeof(FlushStartEvent) || @event.GetType() == typeof(FlushStopEvent))
                HandleFlushEvents(@event);

            return _isFlushing ? Task.CompletedTask : MediaBlock.HandlePadEvent(targetPad, @event);
        }

        private void HandleFlushEvents(IEvent @event)
        {
            var allPads = SinkPads.Concat(SourcePads).Cast<Pad>();

            if (@event.GetType() == typeof(FlushStartEvent))
            {
                _isFlushing = true;
                foreach (var pad in allPads)
                    pad.IsFlushing = true;
            }
            else if (@event.GetType() == typeof(FlushStopEvent))
            {
                _isFlushing = false;
                foreach (var pad in allPads)
                    pad.IsFlushing = false;
            }
        }
    }
}