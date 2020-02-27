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
    public abstract class TransformMediaBlock : IMediaBlock
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
        }

        public int Id { get; set; }
        public Task Init(IMediaBlockContext context)
        {
            throw new NotImplementedException();
        }

        public Task HandlePadEvent(IPad pad, IEvent @event)
        {
            throw new NotImplementedException();
        }

        public Task HandleSinkPadEvent(IPad sinkPad, IEvent @event)
        {
            throw new NotImplementedException();
        }

        public Task Run(IMediaBlockContext context)
        {
            throw new NotImplementedException();
        }

        // public async Task Run(IMediaBlockContext context)
        // {
        //     while (!_isDisposed)
        //     {
        //         var cts = new CancellationTokenSource();
        //         var downstreamRead = context.ReadDownstreamEvent(cts.Token);
        //         var upstreamRead = context.ReadUpstreamEvent(cts.Token);
        //
        //         var task = await Task.WhenAny(downstreamRead, upstreamRead);
        //         if (task == downstreamRead)
        //         {
        //             var resultEvent = await HandleDownstreamEvent(await downstreamRead);
        //             await context.SendDownstreamEvent(resultEvent);
        //         }
        //         else
        //         {
        //             var resultEvent = await HandleUpstreamEvent(await upstreamRead);
        //             await context.SendUpstreamEvent(resultEvent);
        //         }
        //         cts.Cancel();
        //     }
        // }

        public abstract Task<IEvent> HandleUpstreamEvent(IEvent @event);
        public abstract Task<IEvent> HandleDownstreamEvent(IEvent @event);
    }
}