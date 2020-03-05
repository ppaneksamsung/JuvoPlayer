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
using System.Threading.Tasks;
using JuvoPlayer2_0.Impl.Framework;
using NUnit.Framework;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    [TestFixture]
    public class QueryEventTests
    {
        [Test]
        public async Task EventHandling_ResultSet_TaskCompletes()
        {
            var queryEvent = new QueryEvent<int>();
            queryEvent.SetResult(1);

            var result = await queryEvent.Completion;

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void EventHandling_Cancelled_ThrowsTaskCancelledException()
        {
            var queryEvent = new QueryEvent<int>();
            queryEvent.SetCancelled();

            Assert.ThrowsAsync<TaskCanceledException>(async () => await queryEvent.Completion);
        }

        [Test]
        public void EventHandling_Failed_ThrowsException()
        {
            var queryEvent = new QueryEvent<int>();
            queryEvent.SetException(new InvalidOperationException());

            Assert.ThrowsAsync<InvalidOperationException>(async () => await queryEvent.Completion);
        }
    }
}