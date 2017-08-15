using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdgeJs;
using Popcorn.Chromecast.Models;

namespace Popcorn.Chromecast.Services
{
    public class ChromecastService : IChromecastService
    {
        public async Task<Func<object, Task<object>>> StartCastAsync(ChromecastSession session)
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
                                cb();
                            });
                            var media = {
                                contentId: options.mediaPath,
                                contentType: 'video/mp4',
                                streamType: 'BUFFERED',
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

            var result = (Func<object, Task<object>>)await server(new
            {
                host = session.Host,
                mediaPath = session.MediaPath,
                mediaTitle = session.MediaTitle,
                onStatusChanged = session.OnStatusChanged,
                onError = session.OnCastFailed,
                onStarted = session.OnCastSarted
            });

            return result;
        }

        private async Task<Func<object, Task<object>>> StartStaticFileServer(string filePath, string contentType, int port)
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
                        cb(error, function (data, callback) {
                            server.close();
                            callback();
                        });
                    });
                }
            ");

            var result = (Func<object, Task<object>>)await server(new
            {
                path = filePath,
                port = port,
                contentType = contentType
            });

            return result;
        }

        private async Task<Func<object, Task<object>>> StartStreamFileServer(string filePath, string contentType, int port)
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
                        cb(error, function (data, callback) {
                            server.close();
                            callback();
                        });
                    });
                }
            ");

            var result = (Func<object, Task<object>>)await server(new
            {
                path = filePath,
                port = port,
                contentType = contentType
            });

            return result;
        }
    }
}
