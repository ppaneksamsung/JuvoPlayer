/*!
 * https://github.com/SamsungDForum/JuvoPlayer
 * Copyright 2018, Samsung Electronics Co., Ltd
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
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JuvoLogger;
using JuvoPlayer.Common;
using JuvoPlayer.Player.EsPlayer;

namespace JuvoPlayer.Tests.Utils
{
    [Serializable]
    public class SeekOperation : TestOperation
    {
        private readonly ILogger _logger = LoggerManager.GetInstance().GetLogger("UT");

        public TimeSpan SeekPosition { get; set; }

        private bool Equals(SeekOperation other)
        {
            return SeekPosition.Equals(other.SeekPosition);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SeekOperation)obj);
        }

        public override int GetHashCode()
        {
            return SeekPosition.GetHashCode();
        }

        public void Prepare(TestContext context)
        {
            var service = context.Service;
            var newSeekPos = context.SeekTime ?? RandomSeekTime(service);
            SeekPosition = newSeekPos - TimeSpan.FromMilliseconds(newSeekPos.Milliseconds);
        }

        public async Task Execute(TestContext context)
        {
            List<(TimeSpan clock, double diff)> clocks = new List<(TimeSpan clock, double diff)>();

            var service = context.Service;

            var tcs = new TaskCompletionSource<bool>();
            var seekDuringPause = service.State == PlayerState.Paused;

            _logger.Info($"Seeking to {SeekPosition}");

            try
            {
                using (PlayerClockProvider.GetInstance().PlayerClockObservable().Subscribe(clk =>
                {
                    // Remove ms component.
                    clk = clk - TimeSpan.FromMilliseconds(clk.Milliseconds);
                    var diffMs = Math.Abs((clk - SeekPosition).TotalMilliseconds);

                    if (diffMs <= 500)
                    {
                        tcs.TrySetResult(true);
                        return;
                    }

                    clocks.Add((clk, diffMs));
                }, SynchronizationContext.Current))
                using (var timeoutCts = new CancellationTokenSource())
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, context.Token))
                {
                    timeoutCts.CancelAfter(context.Timeout);

                    var seekTask = service.SeekTo(SeekPosition);

                    // Seek in paused state requires resume.
                    if (seekDuringPause)
                        service.Start();

                    await seekTask.WithCancellation(linkedCts.Token);
                    await tcs.Task.WithCancellation(linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // If cancellation was not coused by timeout, don't report it. External
                // cancellation are not reported.
                if (!context.Token.IsCancellationRequested)
                    throw;
            }
            catch (Exception)
            {
                _logger.Error($"Timeout: {context.Timeout}");
                _logger.Error($"Seek To: {SeekPosition}");
                foreach (var clock in clocks)
                {
                    _logger.Error($"Clock: {clock.clock} Diff: {clock.diff}");
                }
                throw;
            }
        }

        private static TimeSpan RandomSeekTime(IPlayerService service)
        {
            var rand = new Random();
            return TimeSpan.FromSeconds(rand.Next((int)service.Duration.TotalSeconds - 10));
        }
    }
}