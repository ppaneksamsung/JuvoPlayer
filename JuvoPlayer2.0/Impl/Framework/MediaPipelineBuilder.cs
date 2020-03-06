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
using System.Threading.Channels;

namespace JuvoPlayer2_0.Impl.Framework
{
    public class MediaPipelineBuilder
    {
        private class MediaBlockContextArgs
        {
            public MediaBlockContextArgs()
            {
                SinkPads = new List<IInputPad>();
                SourcePads = new List<IInputPad>();
            }

            public IList<IInputPad> SinkPads { get; }
            public IList<IInputPad> SourcePads { get; }
        }

        private readonly IDictionary<IMediaBlock, MediaBlockContextArgs> _unlinkedMediaPipeline =
            new Dictionary<IMediaBlock, MediaBlockContextArgs>();

        private IMediaBlock _root;

        public MediaPipelineBuilder SetRoot(IMediaBlock block)
        {
            _root = block;
            return this;
        }

        public MediaPipelineBuilder LinkMediaBlocks(IMediaBlock from, IMediaBlock to)
        {
            if (!_unlinkedMediaPipeline.ContainsKey(from))
                _unlinkedMediaPipeline[from] = new MediaBlockContextArgs();
            if (!_unlinkedMediaPipeline.ContainsKey(to))
                _unlinkedMediaPipeline[to] = new MediaBlockContextArgs();

            var fromArgs = _unlinkedMediaPipeline[from];
            var toArgs = _unlinkedMediaPipeline[to];

            var (srcPad, sinkPad) = CreateLinkedPads();

            fromArgs.SourcePads.Add(srcPad);
            toArgs.SinkPads.Add(sinkPad);
            return this;
        }

        public IMediaPipeline CreatePipeline()
        {
            return CreatePipelineImpl();
        }

        internal MediaPipeline CreatePipelineImpl()
        {
            if (_unlinkedMediaPipeline.Count == 0 || _root == null)
                return null;

            IList<IMediaBlockContext> contexts = new List<IMediaBlockContext>();

            var rootArgs = _unlinkedMediaPipeline[_root];
            var (srcPad, sinkPad) = CreateLinkedPads();
            rootArgs.SinkPads.Add(sinkPad);
            contexts.Add(new MediaBlockContext(_root, rootArgs.SinkPads, rootArgs.SourcePads));
            _unlinkedMediaPipeline.Remove(_root);

            foreach (var keyValue in _unlinkedMediaPipeline)
                contexts.Add(new MediaBlockContext(keyValue.Key, keyValue.Value.SinkPads, keyValue.Value.SourcePads));

            _unlinkedMediaPipeline.Clear();
            _root = null;
            return new MediaPipeline(contexts, srcPad);
        }

        private (IInputPad, IInputPad) CreateLinkedPads()
        {
            var downChannel = Channel.CreateBounded<IEvent>(5);
            var downPriorityChannel = Channel.CreateBounded<IEvent>(2);
            var upChannel = Channel.CreateBounded<IEvent>(5);
            var upPriorityChannel = Channel.CreateBounded<IEvent>(2);

            var srcPad = new Pad(MediaType.Unknown, PadDirection.Source, downChannel.Writer,
                upChannel.Reader, downPriorityChannel.Writer, upPriorityChannel.Reader);
            var sinkPad = new Pad(MediaType.Unknown, PadDirection.Sink, upChannel.Writer,
                downChannel.Reader, upPriorityChannel.Writer, downPriorityChannel.Reader);
            return (srcPad, sinkPad);
        }
    }
}