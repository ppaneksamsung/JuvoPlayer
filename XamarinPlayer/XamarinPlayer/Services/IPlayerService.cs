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
using System.Threading.Tasks;

namespace XamarinPlayer.Services
{
    public interface IPlayerService : IDisposable
    {
        IObservable<PlayerState> StateChanged();
        IObservable<string> PlaybackError();
        IObservable<double> BufferingProgress();

        TimeSpan Duration { get; }

        TimeSpan CurrentPosition { get; }

        bool IsSeekingSupported { get; }

        PlayerState State { get; }

        string CurrentCueText { get; }

        void SetSource(object clip);

        void Start();

        void Stop();

        void Pause();

        Task SeekTo(TimeSpan position);

        List<StreamDescription> GetStreamsDescription(StreamDescription.StreamType streamType);
        void ChangeActiveStream(StreamDescription stream);
        void DeactivateStream(StreamDescription.StreamType streamType);
    }
}
