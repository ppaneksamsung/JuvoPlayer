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

namespace JuvoPlayer2_0.Impl.Framework
{
    public class QueryEvent<TValue> : IEvent
    {
        private readonly TaskCompletionSource<TValue> _completionSource = new TaskCompletionSource<TValue>();

        public EventFlags Flags { get; }

        public Task<TValue> Completion => _completionSource.Task;

        public QueryEvent()
        {
            Flags = EventFlags.Downstream | EventFlags.IsPrioritized;
        }

        public void SetResult(TValue value)
        {
            _completionSource.SetResult(value);
        }

        public void SetCancelled()
        {
            _completionSource.SetCanceled();
        }

        public void SetException(Exception ex)
        {
            _completionSource.SetException(ex);
        }
    }
}