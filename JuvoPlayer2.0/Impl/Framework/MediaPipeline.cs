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

namespace JuvoPlayer2_0.Impl.Framework
{
    public class MediaPipeline : IMediaPipeline
    {
        private readonly IInputPad _srcPad;
        public IList<IMediaBlockContext> Blocks { get; }
        public PropertyRegistry PropertyRegistry { get; private set; }

        public MediaPipeline(IList<IMediaBlockContext> blocks, IInputPad srcPad)
        {
            _srcPad = srcPad;
            Blocks = blocks;
        }

        public void Init()
        {
            var allProperties = new List<IProperty>();

            foreach (var block in Blocks)
            {
                var blockProperties = block.Init();
                allProperties.AddRange(blockProperties);
            }

            PropertyRegistry = new PropertyRegistry(allProperties);
        }

        public void Start()
        {
            foreach (var block in Blocks)
                block.Start();
        }

        public void Stop()
        {
            foreach (var block in Blocks)
                block.Stop();
        }

        public ValueTask Send(IEvent @event)
        {
            return _srcPad.SendEvent(@event);
        }

        public async ValueTask<bool> WaitForReadAsync()
        {
            var cts = new CancellationTokenSource();
            var readAsync = _srcPad.WaitToReadAsync(cts.Token).AsTask();
            var readPriorityAsync = _srcPad.WaitToPriorityReadAsync(cts.Token).AsTask();
            await Task.WhenAny(readAsync, readPriorityAsync);
            cts.Cancel();
            return true;
        }

        public bool TryRead(out IEvent @event)
        {
            @event = default;
            return _srcPad.TryPriorityRead(out @event) || _srcPad.TryRead(out @event);
        }
    }
}