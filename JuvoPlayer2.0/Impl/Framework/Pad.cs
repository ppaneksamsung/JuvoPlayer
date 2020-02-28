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

namespace JuvoPlayer2_0.Impl.Framework
{
    public class Pad : IInputPad
    {
        private readonly ChannelWriter<IEvent> _writer;
        private readonly ChannelReader<IEvent> _reader;
        private readonly ChannelWriter<IEvent> _priorityWriter;
        private readonly ChannelReader<IEvent> _priorityReader;

        public MediaType Type { get; }
        public PadDirection Direction { get; }
        public bool IsFlushing { get; set; }

        public Pad(MediaType type, PadDirection direction, ChannelWriter<IEvent> writer,
            ChannelReader<IEvent> reader, ChannelWriter<IEvent> priorityWriter, ChannelReader<IEvent> priorityReader)
        {
            Type = type;
            Direction = direction;
            _writer = writer;
            _reader = reader;
            _priorityWriter = priorityWriter;
            _priorityReader = priorityReader;
        }

        public ValueTask SendEvent(IEvent @event, CancellationToken token = default)
        {
            ValidateEvent(@event);
            ValidateState();
            var writer = @event.Flags.HasFlag(EventFlags.IsPrioritized) ? _priorityWriter : _writer;
            return writer.WriteAsync(@event, token);
        }

        private void ValidateState()
        {
            if (IsFlushing)
                throw new InvalidOperationException("Cannot send event while pad flushes");
        }

        private void ValidateEvent(IEvent @event)
        {
            var flags = @event.Flags;
            switch (Direction)
            {
                case PadDirection.Source:
                    if (!flags.HasFlag(EventFlags.Downstream))
                        throw new InvalidOperationException("Cannot send non-downstream event on source pad");
                    break;
                case PadDirection.Sink:
                    if (!flags.HasFlag(EventFlags.Upstream))
                        throw new InvalidOperationException("Cannot send non-upstream event on sink pad");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken token)
        {
            return _reader.WaitToReadAsync(token);
        }

        public bool TryRead(out IEvent @event)
        {
            return _reader.TryRead(out @event);
        }

        public ValueTask<bool> WaitToPriorityReadAsync(CancellationToken token)
        {
            return _priorityReader.WaitToReadAsync(token);
        }

        public bool TryPriorityRead(out IEvent @event)
        {
            return _priorityReader.TryRead(out @event);
        }
    }
}