using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Services.Server
{
    internal class VideoStream
    {
        private readonly string videoPath;

        public VideoStream(string videoPath)
        {
            this.videoPath = videoPath;
        }

        public async Task WriteToStream(Stream outputStream, HttpContent content, TransportContext context)
        {
            try
            {
                var buffer = new byte[65536];

                using (var video = File.Open(videoPath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite))
                {
                    var length = (int)video.Length;
                    var bytesRead = 1;

                    while (length > 0 && bytesRead > 0)
                    {
                        bytesRead = await video.ReadAsync(buffer, 0, Math.Min(length, buffer.Length));
                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                        length -= bytesRead;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
            finally
            {
                outputStream.Close();
            }
        }
    }
}
