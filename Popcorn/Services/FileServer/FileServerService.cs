using System;
using System.Threading.Tasks;
using EdgeJs;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using Popcorn.Helpers;
using Popcorn.Messaging;
using Popcorn.Utils.Exceptions;

namespace Popcorn.Services.FileServer
{
    public class FileServerService : IFileServerService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private async Task<object> OnFileServerError(object message)
        {
            Logger.Error(message.ToString);
            Messenger.Default.Send(
                new UnhandledExceptionMessage(
                    new PopcornException(LocalizationProviderHelper.GetLocalizedValue<string>("CastFailed"))));
            return await Task.FromResult<object>(null);
        }

        public async Task<Func<object, Task<object>>> StartStaticFileServer(string filePath, string contentType, int port)
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

            var onError = (Func<object, Task<object>>)OnFileServerError;
            var result = (Func<object, Task<object>>)await server(new
            {
                path = filePath,
                port = port,
                contentType = contentType,
                onError = onError
            });

            return result;
        }

        public async Task<Func<object, Task<object>>> StartStreamFileServer(string filePath, string contentType, int port)
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

            var onError = (Func<object, Task<object>>) OnFileServerError;
            var result = (Func<object, Task<object>>) await server(new
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