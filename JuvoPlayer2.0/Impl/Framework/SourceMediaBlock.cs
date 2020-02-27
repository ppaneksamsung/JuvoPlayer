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

using System.Linq;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace JuvoPlayer2_0.Impl.Framework
{
    public class SourceMediaBlock : IMediaBlock
    {
        private readonly AsyncCollection<IEvent> _outgoingEvents = new AsyncCollection<IEvent>();
        private readonly AsyncCollection<IEvent> _incomingEvents = new AsyncCollection<IEvent>();
        private bool _isDisposed;
        private IMediaBlockContext _context;

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
        }

        public Task Init(IMediaBlockContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }

        public Task HandlePadEvent(IPad pad, IEvent @event)
        {
            return Task.CompletedTask;
        }

        public Task HandleSinkPadEvent(IPad sinkPad, IEvent @event)
        {
            var tasks = _context.SourcePads.Select(pad => pad.SendEvent(@event).AsTask());
            return Task.WhenAll(tasks);
        }

        public void Add(IEvent @event)
        {
            _outgoingEvents.Add(@event);
        }

        public Task<IEvent> TakeAsync()
        {
            return _incomingEvents.TakeAsync();
        }
    }
}