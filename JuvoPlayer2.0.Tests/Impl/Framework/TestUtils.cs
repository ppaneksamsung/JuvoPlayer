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

using System.Threading;
using System.Threading.Tasks;
using JuvoPlayer2_0.Impl.Framework;
using NSubstitute;

namespace JuvoPlayer2_0.Tests.Impl.Framework
{
    public static class TestUtils
    {
        public static IInputPad StubPad(PadDirection direction)
        {
            var pad = Substitute.For<IInputPad>();
            pad.Direction.Returns(direction);
            return pad;
        }

        public static IInputPad WithEvent(this IInputPad pad, IEvent @event = null)
        {
            if (@event.Flags.HasFlag(EventFlags.IsPrioritized))
            {
                pad.WaitToPriorityReadAsync(default).ReturnsForAnyArgs(new ValueTask<bool>(true));
                var anyEvent = Arg.Any<IEvent>();
                pad.TryPriorityRead(out anyEvent).Returns(x =>
                {
                    x[0] = @event;
                    return true;
                });
            }
            else
            {
                pad.WaitToReadAsync(default).ReturnsForAnyArgs(new ValueTask<bool>(true));
                var anyEvent = Arg.Any<IEvent>();
                pad.TryRead(out anyEvent).Returns(x =>
                {
                    x[0] = @event;
                    return true;
                });
            }

            return pad;
        }

        public static IInputPad WithoutEvents(this IInputPad pad)
        {
            var anyEvent = Arg.Any<IEvent>();
            pad.WaitToPriorityReadAsync(default).ReturnsForAnyArgs(new ValueTask<bool>(false));
            pad.TryPriorityRead(out anyEvent).Returns(x => false);
            pad.WaitToReadAsync(default).ReturnsForAnyArgs(new ValueTask<bool>(false));
            pad.TryRead(out anyEvent).Returns(x => false);
            return pad;
        }

        public static IEvent StubEvent(EventFlags flags = EventFlags.Downstream)
        {
            var @event = Substitute.For<IEvent>();
            @event.Flags.Returns(flags);
            return @event;
        }

        public static IMediaBlock StubMediaBlock()
        {
            return Substitute.For<IMediaBlock>();
        }
    }
}