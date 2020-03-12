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

namespace JuvoPlayer2_0.Impl.Framework
{
    public abstract class EventBase : IEvent
    {
        protected EventBase(EventFlags flags)
        {
            Flags = flags;
        }

        public EventFlags Flags { get; }

        public override string ToString()
        {
            return $"{GetType()} [Flags:{Flags}]";
        }
    }

    public abstract class EventBase<TPayload> : EventBase
    {
        protected EventBase(TPayload payload, EventFlags flags)
            : base(flags)
        {
            Payload = payload;
        }

        public TPayload Payload { get; }

        public override string ToString()
        {
            return $"{GetType()} [Flags:{Flags}, Payload:{Payload}]";
        }
    }
}