using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    public class HttpDownloader : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly int _maxRetryCount;
        private readonly int _timeoutSeconds;

        /// <summary>
        /// 初始化HTTP下载器
        /// </summary>
        /// <param name="maxRetryCount">最大重试次数(默认3次)</param>
        /// <param name="timeoutSeconds">超时时间(秒，默认30秒)</param>
        public HttpDownloader(int maxRetryCount = 3, int timeoutSeconds = 30)
        {
            _maxRetryCount = maxRetryCount;
            _timeoutSeconds = timeoutSeconds;

            _httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            {
                Timeout = TimeSpan.FromSeconds(_timeoutSeconds)
            };
        }

        /// <summary>
        /// 异步下载数据到内存(byte[])
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <param name="progress">进度回调(已接收字节数, 总字节数)</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>下载的数据</returns>
        public async Task<byte[]> DownloadDataAsync(
            string url,
            IProgress<(long bytesReceived, long totalBytes)> progress = null,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < _maxRetryCount)
            {
                try
                {
                    using (var response = await _httpClient.GetAsync(
                        url,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();

                        var contentLength = response.Content.Headers.ContentLength;
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var memoryStream = new MemoryStream())
                        {
                            var buffer = new byte[8192];
                            int bytesRead;
                            long totalBytesRead = 0;

                            while ((bytesRead = await contentStream.ReadAsync(
                                buffer, 0, buffer.Length, cancellationToken)) > 0)
                            {
                                await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                totalBytesRead += bytesRead;

                                // 报告进度
                                progress?.Report((totalBytesRead, contentLength ?? -1));

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    throw new TaskCanceledException("Download was canceled.");
                                }
                            }

                            return memoryStream.ToArray();
                        }
                    }
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    throw; // 用户取消，直接抛出
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;

                    if (retryCount >= _maxRetryCount)
                    {
                        throw new WebException(
                            $"Download failed after {_maxRetryCount} attempts.",
                            lastException);
                    }

                    // 等待一段时间后重试
                    await Task.Delay(1000 * retryCount, cancellationToken);
                }
            }

            return Array.Empty<byte>(); // 永远不会执行此行
        }

        /// <summary>
        /// 获取数据大小(不下载内容)
        /// </summary>
        public async Task<long> GetDataSizeAsync(string url)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Head, url))
            using (var response = await _httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                return response.Content.Headers.ContentLength ?? -1;
            }
        }

        /// <summary>
        /// 取消所有正在进行的下载
        /// </summary>
        public void CancelAll()
        {
            _httpClient.CancelPendingRequests();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}