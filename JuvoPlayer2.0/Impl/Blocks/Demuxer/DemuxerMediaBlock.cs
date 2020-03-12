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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JuvoPlayer.Common;
using JuvoPlayer.Demuxers;
using JuvoPlayer2_0.Impl.Common;
using JuvoPlayer2_0.Impl.Framework;

namespace JuvoPlayer2_0.Impl.Blocks.Demuxer
{
    public class DemuxerMediaBlock : IMediaBlock
    {
        private IMediaBlockContext _context;
        private readonly IDemuxer _demuxer;
        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private IPad _audioSrcPad;
        private IPad _videoSrcPad;

        public DemuxerMediaBlock(IDemuxer demuxer)
        {
            _demuxer = demuxer;
        }

        public void Dispose()
        {
            _demuxer.Dispose();
            _cancelTokenSource.Dispose();
        }

        public IList<IProperty> Init(IMediaBlockContext context)
        {
            _context = context;
            _audioSrcPad = _context.SourcePads.SingleOrDefault(pad => pad.Type == MediaType.Audio);
            _videoSrcPad = _context.SourcePads.SingleOrDefault(pad => pad.Type == MediaType.Video);
            if (_audioSrcPad == null && _videoSrcPad == null)
                throw new InvalidOperationException();
            return new List<IProperty>();
        }

        public async Task HandlePadEvent(IPad pad, IEvent @event)
        {
            switch (@event)
            {
                case ContentChangedEvent _:
                    await CompleteDemuxer();
                    _demuxer.Reset();
                    InitDemuxer();
                    break;
                case ChunkEvent chunkEvent:
                    _demuxer.PushChunk(chunkEvent.Payload);
                    return;
                case FlushStartEvent _:
                    FlushDemuxer();
                    break;
                case EosEvent _:
                    await CompleteDemuxer();
                    break;
            }

            await _context.ForwardEvent(@event);
        }

        private void InitDemuxer()
        {
            _demuxer.InitForEs()
                .ContinueWith(OnDemuxerInitialized,
                    _cancelTokenSource.Token,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task OnDemuxerInitialized(Task<ClipConfiguration> initTask)
        {
            var token = _cancelTokenSource.Token;
            if (initTask.Status == TaskStatus.RanToCompletion)
            {
                await PublishClipConfig(initTask.Result, token);
                if (!token.IsCancellationRequested)
                    ScheduleNextPacketToDemux();
                return;
            }

            await MaybePublishError(initTask, token);
        }

        private async Task MaybePublishError(Task task, CancellationToken token)
        {
            if (task.IsFaulted)
                await _context.ForwardEvent(new ErrorEvent(task.Exception), token);
        }

        private async Task PublishClipConfig(ClipConfiguration configuration, CancellationToken token)
        {
            await PublishClipDuration(configuration.Duration, token);
            await PublishStreamConfigs(configuration.StreamConfigs, token);
            await PublishDrmInitData(configuration.DrmInitDatas, token);
        }

        private void ScheduleNextPacketToDemux()
        {
            if (_demuxer.IsInitialized())
                _demuxer.NextPacket()
                    .ContinueWith(OnPacketReady,
                        _cancelTokenSource.Token,
                        TaskContinuationOptions.None,
                        TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task OnPacketReady(Task<Packet> packetTask)
        {
            var token = _cancelTokenSource.Token;
            if (packetTask.Status == TaskStatus.RanToCompletion && packetTask.Result != null)
            {
                await DispatchPacket(packetTask.Result, token);
                if (!token.IsCancellationRequested)
                    ScheduleNextPacketToDemux();
                return;
            }

            await MaybePublishError(packetTask, token);
        }

        private ValueTask DispatchPacket(Packet packet, CancellationToken token)
        {
            return SelectPad(packet.StreamType).SendEvent(new PacketEvent(packet), token);
        }

        private IPad SelectPad(StreamType streamType)
        {
            switch (streamType)
            {
                case StreamType.Audio:
                    return _audioSrcPad;
                case StreamType.Video:
                    return _videoSrcPad;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Task PublishClipDuration(TimeSpan duration, CancellationToken token)
        {
            if (duration == TimeSpan.Zero)
                return Task.CompletedTask;
            return _context.ForwardEvent(new ClipDurationEvent(duration), token);
        }

        private async Task PublishStreamConfigs(IList<StreamConfig> streamConfigs, CancellationToken token)
        {
            if (streamConfigs == null) return;

            foreach (var streamConfig in streamConfigs)
                await SelectPad(streamConfig.StreamType()).SendEvent(new StreamConfigEvent(streamConfig), token);
        }

        private async Task PublishDrmInitData(IList<DRMInitData> drmInitDatas, CancellationToken token)
        {
            if (drmInitDatas == null) return;

            foreach (var drmInitData in drmInitDatas)
                await SelectPad(drmInitData.StreamType).SendEvent(new DRMInitDataEvent(drmInitData), token);
        }

        private void FlushDemuxer()
        {
            _demuxer.Reset();
            CancelContinuations();
        }

        private void CancelContinuations()
        {
            _cancelTokenSource.Cancel();
            _cancelTokenSource = new CancellationTokenSource();
        }

        private async Task CompleteDemuxer()
        {
            if (!_demuxer.IsInitialized())
                return;

            _demuxer.Complete();
            await _demuxer.Completion;
        }

        public Task HandlePropertyChanged(IProperty property)
        {
            return Task.CompletedTask;
        }
    }
}