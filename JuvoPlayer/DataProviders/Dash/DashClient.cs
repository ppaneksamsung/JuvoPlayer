using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JuvoLogger;
using JuvoPlayer.Common;
using JuvoPlayer.SharedBuffers;
using MpdParser.Node;
using MpdParser.Node.Dynamic;
using Representation = MpdParser.Representation;

namespace JuvoPlayer.DataProviders.Dash
{
    internal class DashClient : IDashClient
    {
        private const string Tag = "JuvoPlayer";

        private static readonly ILogger Logger = LoggerManager.GetInstance().GetLogger(Tag);
        private static readonly TimeSpan TimeBufferDepthDefault = TimeSpan.FromSeconds(10);
        private TimeSpan timeBufferDepth = TimeBufferDepthDefault;

        private readonly IThroughputHistory throughputHistory;
        private readonly ISharedBuffer sharedBuffer;
        private readonly StreamType streamType;

        private Representation currentRepresentation;
        private Representation newRepresentation;
        private TimeSpan currentTime = TimeSpan.Zero;
        private TimeSpan bufferTime = TimeSpan.Zero;
        private uint? currentSegmentId;
        private bool isEosSent;

        private IRepresentationStream currentStreams;
        private TimeSpan? currentStreamDuration;

        private byte[] initStreamBytes;

        private Task processDataTask;
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Contains information about timing data for last requested segment
        /// </summary>
        private TimeRange lastDownloadSegmentTimeRange = new TimeRange(TimeSpan.Zero, TimeSpan.Zero);

        /// <summary>
        /// Buffer full accessor.
        /// true - Underlying player received MagicBufferTime ammount of data
        /// false - Underlying player has at least some portion of MagicBufferTime left and can
        /// continue to accept data.
        /// 
        /// Buffer full is an indication of how much data (in units of time) has been pushed to the player.
        /// MagicBufferTime defines how much data (in units of time) can be pushed before Client needs to
        /// hold off further pushes. 
        /// TimeTicks (current time) received from the player are an indication of how much data (in units of time)
        /// player consumed.
        /// A difference between buffer time (data being pushed to player in units of time) and current tick time (currentTime)
        /// defines how much data (in units of time) is in the player and awaits presentation.
        /// </summary>
        private bool BufferFull => (bufferTime - currentTime) > timeBufferDepth;

        /// <summary>
        /// A shorthand for retrieving currently played out document type
        /// True - Content is dynamic
        /// False - Content is static.
        /// </summary>
        private bool IsDynamic => currentStreams.GetDocumentParameters().Document.IsDynamic;


        /// <summary>
        /// Notification event for informing dash pipeline that unrecoverable error
        /// has occoured.
        /// </summary>
        public event Error Error;

        /// <summary>
        /// Storage holders for initial packets PTS/DTS values.
        /// Used in Trimming Packet Handler to truncate down PTS/DTS values.
        /// First packet seen acts as flip switch. Fill initial values or not.
        /// </summary>
        private TimeSpan? trimmOffset;

        public DashClient(IThroughputHistory throughputHistory, ISharedBuffer sharedBuffer,  StreamType streamType)
        {
            this.throughputHistory = throughputHistory ?? throw new ArgumentNullException(nameof(throughputHistory), "throughputHistory cannot be null");
            this.sharedBuffer = sharedBuffer ?? throw new ArgumentNullException(nameof(sharedBuffer), "sharedBuffer cannot be null");
            this.streamType = streamType;
        }

        public TimeSpan Seek(TimeSpan position)
        {
            currentSegmentId = currentStreams.SegmentId(position);
            var newTime = currentStreams.SegmentTimeRange(currentSegmentId)?.Start;

            // We are not expecting NULL segments after seek. 
            // Termination will occour after restarting 
            if (!currentSegmentId.HasValue || !newTime.HasValue)
                LogError($"Seek Pos Req: {position} failed. No segment/TimeRange found");

            currentTime = newTime ?? position;
            LogInfo($"Seek Pos Req: {position} Seek to: ({currentTime}) SegId: {currentSegmentId}");
            return currentTime;
        }

        public void Start()
        {
            if (currentRepresentation == null)
                throw new Exception("currentRepresentation has not been set");

            LogInfo("DashClient start.");
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            // clear garbage before appending new data
            sharedBuffer?.ClearData();

            bufferTime = currentTime;

            if (currentSegmentId.HasValue == false)
                currentSegmentId = currentStreams.StartSegmentId(currentTime, timeBufferDepth);

            var initSegment = currentStreams.InitSegment;
            if (initSegment != null)
                DownloadInitSegment(initSegment);
            else
                ScheduleNextSegDownload();
        }

        private void ScheduleNextSegDownload()
        {
            if (!Monitor.TryEnter(this))
                return;

            try
            {
                if (IsEndOfContent(bufferTime))
                {
                    Stop();
                    return;
                }

                if (!processDataTask.IsCompleted || cancellationTokenSource.IsCancellationRequested)
                    return;

                if (BufferFull)
                {
                    LogInfo($"Full buffer: ({bufferTime}-{currentTime}) {bufferTime - currentTime} > {timeBufferDepth}.");
                    return;
                }

                SwapRepresentation();

                var segment = currentStreams.MediaSegment(currentSegmentId);
                if (segment == null)
                {
                    LogInfo($"Segment: [{currentSegmentId}] NULL stream");
                    if (IsDynamic)
                        return;

                    LogWarn("Stopping player");

                    Stop();
                    return;
                }

                DownloadSegment(segment);
            }
            finally
            {
                Monitor.Exit(this);
            }
        }


        private void DownloadSegment(Segment segment)
        {
            // Grab a copy (its a struct) of cancellation token, so one token is used throughout entire operation
            var cancelToken = cancellationTokenSource.Token;
            var downloadTask = CreateDownloadTask(segment, IsDynamic, currentSegmentId, cancelToken);
            downloadTask.ContinueWith(_ => SegmentDownloadCancelled(), cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnCanceled,
                TaskScheduler.Default);
            downloadTask.ContinueWith(response => HandleFailedDownload(GetErrorMessage(response)),
                cancelToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            processDataTask = downloadTask.ContinueWith(response => HandleSuccessfullDownload(response.Result),
                cancelToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            processDataTask.ContinueWith(_ => ScheduleNextSegDownload(),
                cancelToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
        }

        private void SegmentDownloadCancelled()
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
            }
            else
            {
                // download was cancelled by timeout cancellation token. Reschedule download
                ScheduleNextSegDownload();
            }
        }

        private void DownloadInitSegment(Segment segment)
        {
            if (initStreamBytes == null)
            {

                // Grab a copy (its a struct) of cancellation token so it is not referenced through cancellationTokenSource each time.
                var cancelToken = cancellationTokenSource.Token;
                var downloadTask = CreateDownloadTask(segment, true, null, cancelToken);

                downloadTask.ContinueWith(response => HandleFailedInitDownload(GetErrorMessage(response)),
                    cancelToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                processDataTask = downloadTask.ContinueWith(response => InitDataDownloaded(response.Result),
                    cancelToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                processDataTask.ContinueWith(_ => ScheduleNextSegDownload(),
                    cancelToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
            }
            else
            {
                // Already have init segment. Push it down the pipeline & schedule next download
                var initData = new DownloadResponse
                {
                    Data = initStreamBytes,
                    SegmentId = null
                };

                LogInfo("Skipping INIT segment download");
                InitDataDownloaded(initData);
                ScheduleNextSegDownload();
            }
        }

        private static string GetErrorMessage(Task response)
        {
            return response.Exception?.Flatten().InnerExceptions[0].Message;
        }

        private void HandleSuccessfullDownload(DownloadResponse responseResult)
        {
            sharedBuffer.WriteData(responseResult.Data);
            lastDownloadSegmentTimeRange = responseResult.DownloadSegment.Period.Copy();
            currentSegmentId = currentStreams.NextSegmentId(currentSegmentId);

            if (trimmOffset.HasValue == false)
                trimmOffset = responseResult.DownloadSegment.Period.Start;

            bufferTime = responseResult.DownloadSegment.Period.Start + responseResult.DownloadSegment.Period.Duration - trimmOffset.Value;

            var timeInfo = responseResult.DownloadSegment.Period.ToString();

            LogInfo($"Segment: {responseResult.SegmentId} received {timeInfo}");

        }

        private void InitDataDownloaded(DownloadResponse responseResult)
        {
            if (responseResult.Data != null)
                sharedBuffer.WriteData(responseResult.Data);

            // Assign initStreamBytes AFTER it has been pushed down the shared buffer.
            // When issuing EOS, initStreamBytes will be checked for NULLnes.
            // We do not want to send EOS before init data - will kill demuxer.
            initStreamBytes = responseResult.Data;
            LogInfo("INIT segment downloaded.");
        }

        private void HandleFailedDownload(string message)
        {
            LogError(message);

            if (IsDynamic)
                return;

            // Stop Client and signal error.
            //
            Stop();

            var exception = response.Exception?.Flatten().InnerExceptions[0];
            if (exception is DashDownloaderException downloaderException)
            {
                var segmentTime = downloaderException.DownloadRequest.DownloadSegment.Period.Start;
                var segmentDuration = downloaderException.DownloadRequest.DownloadSegment.Period.Duration;

                var segmentEndTime = segmentTime + segmentDuration - (trimmOffset ?? TimeSpan.Zero);
                if (IsEndOfContent(segmentEndTime))
                    return false;
            }

            Error?.Invoke(errorMessage);
        }

        private void HandleFailedInitDownload(string message)
        {
            LogError(message);

            Stop();

            Error?.Invoke(message);
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            SendEOSEvent();

            trimmOffset = null;

            // Temporary prevention caused by out of order download processing.
            // Wait for download task to complete. Stale cancellations
            // may happen during FF/REW operations. 
            // If received after client start may result in lack of further download requests 
            // being issued. Once download handler are serialized, should be safe to remove.
            try
            {
                if (processDataTask?.Status > TaskStatus.Created)
                    processDataTask.Wait();
            }
            catch (AggregateException) { }


            LogInfo("Data downloader stopped");
        }

        public void SetRepresentation(Representation representation)
        {
            // representation has changed, so reset initstreambytes
            if (currentRepresentation != null)
                initStreamBytes = null;

            currentRepresentation = representation;
            currentStreams = currentRepresentation.Segments;

            // Prepare Stream for playback.
            if (!currentStreams.PrepeareStream())
            {
                LogError("Failed to prepare stream. No playable segments");

                // Static content - treat as EOS.
                if (!IsDynamic)
                    Stop();

                return;
            }

            currentStreamDuration = IsDynamic
                ? currentStreams.GetDocumentParameters().Document.MediaPresentationDuration
                : currentStreams.Duration;

            UpdateTimeBufferDepth();
        }

        /// <summary>
        /// Updates representation based on Manifest Update
        /// </summary>
        /// <param name="representation"></param>
        public void UpdateRepresentation(Representation representation)
        {
            if (!IsDynamic)
                return;

            Interlocked.Exchange(ref newRepresentation, representation);
            LogInfo("newRepresentation set");

            ScheduleNextSegDownload();
        }

        /// <summary>
        /// Swaps updated representation based on Manifest Reload.
        /// Updates segment information and base segment ID for the stream.
        /// </summary>
        /// <returns>bool. True. Representations were swapped. False otherwise</returns>
        private void SwapRepresentation()
        {
            // Exchange updated representation with "null". On subsequent calls, this will be an indication
            // that there is no new representations.
            var newRep = Interlocked.Exchange(ref newRepresentation, null);

            // Update internals with new representation if exists.
            if (newRep == null)
                return;

            currentRepresentation = newRep;
            currentStreams = currentRepresentation.Segments;

            // Sanity check on streams (needed if stream fails its own IO - index download, etc.)
            if (!currentStreams.PrepeareStream())
            {
                LogError("Failed to prepare stream. No playable segments");

                // Static content - treat as EOS.
                if (!IsDynamic)
                    Stop();

                currentSegmentId = null;
                return;
            }

            currentStreamDuration = IsDynamic 
                ? currentStreams.GetDocumentParameters().Document.MediaPresentationDuration
                : currentStreams.Duration;

            UpdateTimeBufferDepth();

            if (lastDownloadSegmentTimeRange == null)
            {
                currentSegmentId = currentStreams.StartSegmentId(currentTime, timeBufferDepth);
                LogInfo($"Rep. Swap. Start Seg: [{currentSegmentId}]");
                return;
            }

            var newSeg = currentStreams.NextSegmentId(lastDownloadSegmentTimeRange.Start);
            string message;

            if (newSeg.HasValue)
            {
                var segmentTimeRange = currentStreams.SegmentTimeRange(newSeg);
                message = $"Updated Seg: [{newSeg}]/({segmentTimeRange?.Start}-{segmentTimeRange?.Duration})";
            }
            else
            {
                message = "Not Found. Setting segment to null";
            }

            LogInfo($"Rep. Swap. Last Seg: {currentSegmentId}/{lastDownloadSegmentTimeRange.Start}-{lastDownloadSegmentTimeRange.Duration} {message}");

            currentSegmentId = newSeg;

            LogInfo("Representations swapped.");
        }

        public void OnTimeUpdated(TimeSpan time)
        {
            // Ignore time updated events when EOS is already sent
            if (isEosSent)
                return;

            currentTime = time;

            ScheduleNextSegDownload();
        }

        private void SendEOSEvent()
        {
            // Send EOS only when init data has been processed.
            // Stops demuxer being blown to high heavens.
            if (initStreamBytes == null)
                return;

            sharedBuffer.WriteData(null, true);

            isEosSent = true;
        }

        private bool IsEndOfContent(TimeSpan time)
        {
            var endTime = !currentStreamDuration.HasValue || currentStreamDuration.Value == TimeSpan.Zero
                ? TimeSpan.MaxValue 
                : currentStreamDuration.Value;

            return time >= endTime;
        }

        private Task<DownloadResponse> CreateDownloadTask(Segment segment, bool ignoreError, uint? segmentId, CancellationToken cancelToken)
        {
            var requestData = new DownloadRequest
            {
                DownloadSegment = segment,
                IgnoreError = ignoreError,
                SegmentId = segmentId,
                StreamType = streamType
            };

            var timeout = CalculateDownloadTimeout(segment);

            var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
            var downloadCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationTokenSource.Token,
                timeoutCancellationTokenSource.Token);

            return DashDownloader.DownloadDataAsync(requestData, downloadCancellationTokenSource.Token, throughputHistory);
        }

        private TimeSpan CalculateDownloadTimeout(Segment segment)
        {
            var timeout = TimeBufferDepthDefault;
            var avarageThroughput = throughputHistory.GetAverageThroughput();
            if (avarageThroughput > 0 && currentRepresentation.Bandwidth.HasValue && segment.Period != null)
            {
                var bandwith = currentRepresentation.Bandwidth.Value;
                var duration = segment.Period.Duration.TotalSeconds;
                var segmentSize = bandwith * duration;
                var calculatedTimeNeeded = TimeSpan.FromSeconds(segmentSize / avarageThroughput * 1.5);
                var manifestMinBufferDepth = currentStreams.GetDocumentParameters().Document.MinBufferTime ?? TimeSpan.Zero;
                timeout = calculatedTimeNeeded > manifestMinBufferDepth ? calculatedTimeNeeded : manifestMinBufferDepth;
            }

            return timeout;
        }

        private void TimeBufferDepthDynamic()
        {
            // For dynamic content, use Manifest buffer time 
            timeBufferDepth = currentStreams.GetDocumentParameters().Document.MinBufferTime ?? TimeBufferDepthDefault;
        }

        private void TimeBufferDepthStatic()
        {
            // Buffer depth is calculated as so:
            // TimeBufferDepth = 
            // 1 avg. seg. duration (one being played out) +
            // bufferTime in multiples of avgSegment Duration with roundup
            // i.e:
            // minbuftime = 5 sek.
            // avgseg = 3 sek.
            // bufferTime = 6 sec.
            // timeBufferDepth = 3 sek + 6 sec.
            //
            var duration = currentStreams.Duration;
            var segments = currentStreams.Count;
            var manifestMinBufferDepth = currentStreams.GetDocumentParameters().Document.MinBufferTime ?? TimeSpan.Zero;

            //Get average segment duration = Total Duration / number of segments.
            var avgSegmentDuration = TimeSpan.FromSeconds((duration.Value.TotalSeconds / segments));

            // Compute multiples of manifest MinBufferTime in units of average segment duration
            // with round up
            var multiples = (uint)((manifestMinBufferDepth.TotalSeconds + avgSegmentDuration.TotalSeconds - 1) / avgSegmentDuration.TotalSeconds);
            var bufferTime = TimeSpan.FromSeconds(multiples * avgSegmentDuration.TotalSeconds);

            // Compose final timeBufferDepth.
            timeBufferDepth = avgSegmentDuration + bufferTime;

            LogInfo($"Average Segment Duration: {avgSegmentDuration} Manifest Min. Buffer Time: {manifestMinBufferDepth}");
        }

        private void UpdateTimeBufferDepth()
        {
            if (IsDynamic)
                TimeBufferDepthDynamic();
            else
                TimeBufferDepthStatic();

            LogInfo($"TimeBufferDepth: {timeBufferDepth}");
        }

        #region Logging Functions

        private void LogInfo(string logMessage, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Logger.Info(streamType + ": " + logMessage, file, method, line);
        }
        private void LogDebug(string logMessage, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Logger.Debug(streamType + ": " + logMessage, file, method, line);
        }
        private void LogWarn(string logMessage, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Logger.Warn(streamType + ": " + logMessage, file, method, line);
        }
        private void LogFatal(string logMessage, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Logger.Fatal(streamType + ": " + logMessage, file, method, line);
        }
        private void LogError(string logMessage, [CallerFilePath] string file = "", [CallerMemberName] string method = "", [CallerLineNumber] int line = 0)
        {
            Logger.Error(streamType + ": " + logMessage, file, method, line);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        public void Dispose()
        {
            if (disposedValue)
                return;

            cancellationTokenSource?.Dispose();

            disposedValue = true;
        }

        #endregion
    }
}

