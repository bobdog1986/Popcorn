using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using EdgeJs;
using Popcorn.Chromecast.Models;

namespace Popcorn.Chromecast.Services
{
    public class ChromecastService : IChromecastService
    {
        public async Task<ChromecastSession> StartCastAsync(ChromecastSession session)
        {
            var server = Edge.Func(@"
                var Client                = require('castv2-client').Client;
                var DefaultMediaReceiver  = require('castv2-client').DefaultMediaReceiver;
                var client = new Client();
                return function (options, cb) {
                    client.connect(options.host, function() {                       
                        client.launch(DefaultMediaReceiver, function(err, player) {
                            cb(err, function (data, cb) {
                                if(data === 'pause'){
                                    player.pause(function() {
                                    });
                                }
                                else if(data === 'play'){
                                    player.play(function() {
                                    });
                                }
                                else if(data === 'stop'){
                                    player.stop(function() {
                                        client.close();
                                    });
                                }
                                else if(data.includes('seek')){
                                    var seek = data.split(':')[1];
                                    player.seek(parseFloat(seek.replace(',', '.')), function(){
                                    });
                                }
                                else if(data.includes('volume')){
                                    var volume = data.split(':')[1];
                                    client.setVolume({ level: parseFloat(volume.replace(',', '.')) }, function(vol) {
                                    });
                                }
                                cb();
                            });
                            var media = {};

                            player.on('status', function(status)
                            {
                                options.onStatusChanged(status, function (error, result) {
                                });
                            });                                                  

                            var opts = {};
                            if(options.anySubtitle){
                                    media = {
                                    contentId: options.mediaPath,
                                    contentType: options.contentType,
                                    streamType: options.streamType,
                                    tracks: [{
                                              trackId: 1,
                                              type: 'TEXT',
                                              trackContentId: options.subtitlePath,
                                              trackContentType: 'text/vtt',
                                              name: 'English',
                                              language: 'en-US',
                                              subtype: 'SUBTITLES'
                                            }],
                                    metadata: {
                                        type: 0,
                                        metadataType: 0,
                                        title: options.mediaTitle
                                    }
                                };

                                opts = {
                                    autoplay: true,
                                    activeTrackIds: [1]
                                };

                            }
                            else{
                                media = {
                                    contentId: options.mediaPath,
                                    contentType: options.contentType,
                                    streamType: options.streamType,
                                    metadata: {
                                        type: 0,
                                        metadataType: 0,
                                        title: options.mediaTitle
                                    }
                                };
                                opts = {
                                    autoplay: true
                                };
                            }

                            player.load(media, opts, function(err, status) {
                                client.setVolume({ level: 0.5 }, function(vol) {
                                });
                                options.onStarted(function(err, cb) {
                                });
                            });
                        });
                    });

                    client.on('error', function(err)
                    {
                        options.onError(err, function(error, result) {
                        });
                        client.close();
                    });
                }
            ");

            var videoPath = session.MediaPath.Split(new[] {"Popcorn\\"}, StringSplitOptions.RemoveEmptyEntries)[1]
                .Replace("\\", "/");
            var mediaPath = session.SourceType == SourceType.Torrent
                ? $"http://{GetLocalIpAddress()}:9900/{videoPath}"
                : session.MediaPath;
            var contentType = "video/mp4";
            var subtitlePath = string.IsNullOrEmpty(session.SubtitlePath) ? string.Empty : session.SubtitlePath.Split(new[] {"Popcorn\\"}, StringSplitOptions.RemoveEmptyEntries)[1]
                .Replace("\\", "/");
            var castServer = (Func<object, Task<object>>) await server(new
            {
                host = session.Host,
                mediaPath = mediaPath,
                mediaTitle = session.MediaTitle,
                onStatusChanged = session.OnStatusChanged,
                onError = session.OnCastFailed,
                onStarted = session.OnCastSarted,
                contentType = contentType,
                streamType = "BUFFERED",
                subtitlePath = $"http://{GetLocalIpAddress()}:9900/{subtitlePath}",
                anySubtitle = !string.IsNullOrEmpty(session.SubtitlePath)
            });

            session.CastServer = castServer;
            return session;
        }

        private string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }
}