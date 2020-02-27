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
using System.Threading.Tasks;

namespace JuvoPlayer2_0.Impl.Framework
{
    public class MediaPipeline : IMediaPipeline
    {
        private readonly IList<MediaBlockContext> _blocks;
        private readonly SourceMediaBlock _source;

        public MediaPipeline(IList<MediaBlockContext> blocks)
        {
            _blocks = blocks;
            _source = _blocks.First().MediaBlock as SourceMediaBlock;
        }

        public void PushEvent(IEvent @event)
        {
            _source.Add(@event);
        }

        public Task<IEvent> TakeEvent()
        {
            return _source.TakeAsync();
        }

        public void Start()
        {
            foreach (var block in _blocks)
                block.Start();
        }

        public void Stop()
        {
            foreach (var block in _blocks)
                block.Stop();
        }
    }
}