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

using System.Threading.Tasks;
using JuvoPlayer2_0.Impl.Framework;
using NUnit.Framework;
using static JuvoPlayer2_0.Tests.Impl.Framework.TestUtils;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    [TestFixture]
    public class MediaPipelineBuilderTests
    {
        [Test]
        public void CreatePipeline_SrcSet_ReturnsNull()
        {
            var builder = new MediaPipelineBuilder();
            var dummyRoot = StubMediaBlock();

            builder.SetSrc(dummyRoot);
            var pipeline = builder.CreatePipeline();

            Assert.That(pipeline, Is.Null);
        }

        [Test]
        public void CreatePipeline_SinkSet_ReturnsNull()
        {
            var builder = new MediaPipelineBuilder();
            var sinkBlockStub = StubMediaBlock();

            builder
                .SetSink(sinkBlockStub);
            var pipeline = builder.CreatePipeline();

            Assert.That(pipeline, Is.Null);
        }

        [Test]
        public void CreatePipeline_LinkSet_ReturnsNull()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();
            var sinkBlockStub = StubMediaBlock();

            builder
                .LinkMediaBlocks(srcBlockStub, sinkBlockStub);
            var pipeline = builder.CreatePipeline();

            Assert.That(pipeline, Is.Null);
        }

        [Test]
        public void CreatePipeline_LinkAndSinkSet_ReturnsNull()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();
            var sinkBlockStub = StubMediaBlock();

            builder
                .LinkMediaBlocks(srcBlockStub, sinkBlockStub)
                .SetSink(sinkBlockStub);
            var pipeline = builder.CreatePipeline();

            Assert.That(pipeline, Is.Null);
        }

        [Test]
        public void CreatePipeline_SrcAndLinkSet_ReturnsNull()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();
            var sinkBlockStub = StubMediaBlock();

            builder
                .SetSrc(srcBlockStub)
                .LinkMediaBlocks(srcBlockStub, sinkBlockStub);
            var pipeline = builder.CreatePipeline();

            Assert.That(pipeline, Is.Null);
        }

        [Test]
        public void CreatePipeline_SameSrcAndSinkSet_CreatesSuccessfully()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();

            builder
                .SetSrc(srcBlockStub)
                .SetSink(srcBlockStub);
            var pipeline = builder.CreatePipeline();

            Assert.That(pipeline, Is.Not.Null);
        }

        [Test]
        public void CreatePipeline_SingleSrcAndSink_CreatesSuccessfully()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();
            var sinkBlockStub = StubMediaBlock();

            builder
                .SetSrc(srcBlockStub)
                .LinkMediaBlocks(srcBlockStub, sinkBlockStub)
                .SetSink(sinkBlockStub);
            var pipeline = builder.CreatePipeline();

            Assert.That(pipeline, Is.Not.Null);
        }

        [Test]
        public void CreatePipeline_SingleSrcAndSink_PipelineContains2Blocks()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();
            var sinkBlockStub = StubMediaBlock();

            builder
                .SetSrc(srcBlockStub)
                .LinkMediaBlocks(srcBlockStub, sinkBlockStub)
                .SetSink(sinkBlockStub);
            var pipeline = builder.CreatePipelineImpl();
            var blocksCount = pipeline.Blocks.Count;

            Assert.That(blocksCount, Is.EqualTo(2));
        }

        [Test]
        public void CreatePipeline_SingleSrcAndSink_SrcBlockContainsSinkPad()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();
            var sinkBlockStub = StubMediaBlock();

            builder
                .SetSrc(srcBlockStub)
                .LinkMediaBlocks(srcBlockStub, sinkBlockStub)
                .SetSink(sinkBlockStub);
            var pipeline = builder.CreatePipelineImpl();
            var sinkPadsCount = pipeline.Blocks[0].SinkPads.Count;

            Assert.That(sinkPadsCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CreatePipeline_SingleSrcAndSink_SrcAndSinkAreLinked()
        {
            var builder = new MediaPipelineBuilder();
            var srcBlockStub = StubMediaBlock();
            var sinkBlockStub = StubMediaBlock();
            var eventStub = StubEvent();

            builder
                .SetSrc(srcBlockStub)
                .LinkMediaBlocks(srcBlockStub, sinkBlockStub)
                .SetSink(sinkBlockStub);
            var pipeline = builder.CreatePipelineImpl();

            var srcPad = pipeline.Blocks[0].SourcePads[0];
            var sinkPad = pipeline.Blocks[1].SinkPads[0] as IInputPad;

            await srcPad.SendEvent(eventStub);
            var success = sinkPad.TryRead(out var receivedEvent);

            Assert.That(success, Is.True);
            Assert.That(receivedEvent, Is.EqualTo(eventStub));
        }
    }
}