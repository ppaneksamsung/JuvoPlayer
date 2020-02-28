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
        /// <summary>
        /// Streaming thread where the controlled MediaBlock executes.
        /// </summary>
        private readonly AsyncContextThread _streamingThread;

        /// <summary>
        /// Set to true when any of pads is flushing.
        /// </summary>
        private bool _isFlushing;

        /// <summary>
        /// Source used to stop (cancel) MediaBlock's execution.
        /// </summary>
        private readonly CancellationTokenSource _stopCancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Helper list with all pad readers (prioritized and not).
        /// </summary>
        private readonly List<PadReader> _allReaders;

        public IList<IPad> SinkPads { get; }
        public IList<IPad> SourcePads { get; }
        public SynchronizationContext SynchronizationContext => _streamingThread.Context.SynchronizationContext;
        public IMediaBlock MediaBlock { get; }
        public Task Completion { get; private set; }

        private class PadReader
        {
            private readonly bool _isPriority;

            public IInputPad Pad { get; }

            public PadReader(IInputPad pad, bool isPriority)
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

        public MediaBlockContext(IMediaBlock mediaBlock, IEnumerable<IInputPad> sinkPads,
            IEnumerable<IInputPad> sourcePads)
        {
            _streamingThread = new AsyncContextThread();
            MediaBlock = mediaBlock;
            SinkPads = sinkPads.Cast<IPad>().ToList();
            SourcePads = sourcePads.Cast<IPad>().ToList();

            _allReaders = AllPadReaders(true)
                .Concat(AllPadReaders(false))
                .ToList();
        }

        private IEnumerable<PadReader> AllPadReaders(bool isPriority)
        {
            var allPads = SinkPads.Concat(SourcePads).Cast<IInputPad>();
            return allPads.Select(pad => new PadReader(pad, isPriority));
        }

        public void Start()
        {
            Completion = _streamingThread.Factory.StartNew(async () =>
            {
                await MediaBlock.Init(this);
                await HandleEvents(_stopCancellationTokenSource.Token);
            }).Unwrap();
        }

        public void Stop()
        {
            _stopCancellationTokenSource.Cancel();
        }

        private async Task HandleEvents(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await HandleEventOrWait(token);
            }
        }

        internal async Task HandleEventOrWait(CancellationToken token)
        {
            foreach (var reader in _allReaders)
            {
                if (!reader.TryRead(out var @event)) continue;
                await HandleEvent(reader.Pad, @event);
                return;
            }

            var waitPool = _allReaders
                .Select(reader => reader.WaitToReadAsync(token).AsTask())
                .Where(waitingRead => !waitingRead.IsCompleted)
                .ToList();

            var waitTask = waitPool.Count != _allReaders.Count ? Task.CompletedTask : Task.WhenAny(waitPool);
            await waitTask;
        }

        private async Task HandleEvent(IPad targetPad, IEvent @event)
        {
            if (@event.GetType() == typeof(FlushStopEvent))
                HandleFlushEvents(@event);
            if (!_isFlushing)
                await MediaBlock.HandlePadEvent(targetPad, @event);
            if (@event.GetType() == typeof(FlushStartEvent))
                HandleFlushEvents(@event);
        }

        private void HandleFlushEvents(IEvent @event)
        {
            var allPads = SinkPads.Concat(SourcePads).Cast<IInputPad>();

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