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
using NSubstitute;
using NUnit.Framework;
using static JuvoPlayer2_0.Tests.Impl.Framework.TestUtils;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    [TestFixture]
    public class MediaPipelineTests
    {
        [Test]
        public void Start_Called_StartsAllBlocks()
        {
            var srcPadStub = StubPad(PadDirection.Source);
            var blocks = new[] {Substitute.For<IMediaBlockContext>(), Substitute.For<IMediaBlockContext>()};

            var pipeline = new MediaPipeline(blocks, srcPadStub);
            pipeline.Start();

            foreach (var block in blocks)
                block.Received().Start();
        }

        [Test]
        public void Stop_Called_StopsAllBlocks()
        {
            var srcPadStub = StubPad(PadDirection.Source);
            var blocks = new[] {Substitute.For<IMediaBlockContext>(), Substitute.For<IMediaBlockContext>()};

            var pipeline = new MediaPipeline(blocks, srcPadStub);
            pipeline.Stop();

            foreach (var block in blocks)
                block.Received().Stop();
        }

        [Test]
        public async Task Send_Called_EventIsSentOnSrcPad()
        {
            var srcPadMock = StubPad(PadDirection.Source);
            var block = Substitute.For<IMediaBlockContext>();
            var eventStub = StubEvent();
            var pipeline = new MediaPipeline(new[] {block}, srcPadMock);

            await pipeline.Send(eventStub);

            await srcPadMock.Received().SendEvent(Arg.Is(eventStub));
        }

        [Test]
        public async Task Reading_EventAvailableOnSrcPad_ReturnsEvent()
        {
            var eventStub = StubEvent();
            var srcPadStub = StubPad(PadDirection.Source)
                .WithEvent(eventStub);
            var block = Substitute.For<IMediaBlockContext>();
            var pipeline = new MediaPipeline(new[] {block}, srcPadStub);

            await pipeline.WaitForReadAsync();
            var read = pipeline.TryRead(out var receivedEvent);

            Assert.That(read, Is.True);
            Assert.That(receivedEvent, Is.EqualTo(eventStub));
        }
    }
}