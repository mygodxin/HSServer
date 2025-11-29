using System.Net.Http;
using System.Threading.Tasks;

namespace Core.Util
{
    public class HttpUtil
    {
        /// <summary>
        /// 下载byte[]
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static Task<byte[]> DownloadBinary(string url)
        {
            var client = new HttpClient();
            return client.GetByteArrayAsync(url);
        }
    }
}
