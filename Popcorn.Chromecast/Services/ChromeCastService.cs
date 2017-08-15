using System;
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
            if (session.SourceType == SourceType.Torrent)
            {
                var mediaMimeType = string.IsNullOrEmpty(session.MediaMimeType)
                    ? "video/mp4"
                    : session.MediaMimeType;
                var streamServer = await StartStreamFileServer(session.MediaPath,
                    mediaMimeType,
                    9000, session.OnCastFailed);

                session.StreamServer = streamServer;
                if (!string.IsNullOrEmpty(session.SubtitlePath))
                {
                    var subtitleMimeType = string.IsNullOrEmpty(session.SubtitleMimeType)
                        ? "text/plain"
                        : session.SubtitleMimeType;
                    var subtitleServer = await StartStaticFileServer(session.SubtitlePath,
                        subtitleMimeType, 9001, session.OnCastFailed);
                    session.SubtitleServer = subtitleServer;
                }
            }

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
                            var media = {
                                contentId: options.mediaPath,
                                contentType: options.contentType,
                                streamType: options.streamType,
                                metadata: {
                                    type: 0,
                                    metadataType: 0,
                                    title: options.mediaTitle
                                }
                            };

                            player.on('status', function(status)
                            {
                                options.onStatusChanged(status, function (error, result) {
                                });
                            });                                                  

                            player.load(media, { autoplay: true }, function(err, status) {
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

            await Task.Delay(1000);
            var mediaPath = session.SourceType == SourceType.Torrent
                ? $"http://{GetLocalIPAddress()}:9000"
                : session.MediaPath;
            var contentType = "video/mp4";
            var streamType = session.SourceType == SourceType.Torrent ? "LIVE" : "BUFFERED";
            var castServer = (Func<object, Task<object>>)await server(new
            {
                host = session.Host,
                mediaPath = mediaPath,
                mediaTitle = session.MediaTitle,
                onStatusChanged = session.OnStatusChanged,
                onError = session.OnCastFailed,
                onStarted = session.OnCastSarted,
                contentType = contentType,
                streamType = streamType
            });

            session.CastServer = castServer;
            return session;
        }

        public static string GetLocalIPAddress()
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

        private async Task<Func<object, Task<object>>> StartStaticFileServer(string filePath, string contentType, int port, Func<object, Task<object>> onError)
        {
            var server = Edge.Func(@"
                return function (options, cb) {
                    const http = require('http');
                    const fs = require('fs');
                    const port = options.port;
                    var mediaPath = options.path;
                    var server = http.createServer(function (req, res) {
                      fs.exists(mediaPath, function (exist) {
                        if(!exist) {
                          res.statusCode = 404;
                          var err = 'File ${mediaPath} not found!';
                          res.end(err);
                          options.onError(err, function (error, result) {});
                          return;
                        }
                        fs.readFile(pathname, function(err, data){
                          if(err){
                            res.statusCode = 500;
                            res.end('Error getting the file: ${err}.');
                          } else {
                            res.setHeader('Content-type', options.contentType );
                            res.end(data);
                          }
                        });
                      });
                    }).listen(parseInt(port), function (error) {
                        cb(error, function (data, cb) {
                            server.close();
                            cb();
                        });
                    });
                }
            ");

            var result = (Func<object, Task<object>>)await server(new
            {
                path = filePath,
                port = port,
                contentType = contentType,
                onError = onError
            });

            return result;
        }

        private async Task<Func<object, Task<object>>> StartStreamFileServer(string filePath, string contentType, int port, Func<object, Task<object>> onError)
        {
            var server = Edge.Func(@"
                return function (options, cb) {
                    const http = require('http');
                    const fs = require('fs');
                    const port = options.port;
                    var mediaPath = options.path;
                    var server = http.createServer(function (req, res) {
                      fs.exists(mediaPath, function (exist) {
                        if(!exist) {
                          res.statusCode = 404;
                          var err = 'File ${mediaPath} not found!';
                          res.end(err);
                          options.onError(err, function (error, result) {});
                          return;
                        }
                        var stream = fs.createReadStream(mediaPath, { bufferSize: 64 * 1024 });
                        stream.on('error', function(err) {
                            options.onError(err, function (error, result) {});
                            res.end(err);
                        });
                        stream.on('open', function () {
                            res.setHeader('Content-type', options.contentType );
                            stream.pipe(res);
                        });
                      });
                    }).listen(parseInt(port), function (error) {
                        cb(error, function (data, cb) {
                            server.close();
                            cb();
                        });
                    });
                }
            ");

            var result = (Func<object, Task<object>>)await server(new
            {
                path = filePath,
                port = port,
                contentType = contentType,
                onError = onError
            });

            return result;
        }
    }
}
