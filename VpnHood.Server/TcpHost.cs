﻿using VpnHood.Server.Factory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Logging;
using VpnHood.Tunneling;
using VpnHood.Tunneling.Messages;

namespace VpnHood.Server
{
    class TcpHost : IDisposable
    {
        private readonly TcpListener _tcpListener;
        private readonly SessionManager _sessionManager;
        private readonly TcpClientFactory _tcpClientFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly SslCertificateManager _sslCertificateManager;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private ILogger _logger => VhLogger.Current;


        public TcpHost(IPEndPoint endPoint, SessionManager sessionManager, SslCertificateManager sslCertificateManager, TcpClientFactory tcpClientFactory)
        {
            _tcpListener = endPoint != null ? new TcpListener(endPoint) : throw new ArgumentNullException(nameof(endPoint));
            _sslCertificateManager = sslCertificateManager ?? throw new ArgumentNullException(nameof(sslCertificateManager));
            _sessionManager = sessionManager;
            _tcpClientFactory = tcpClientFactory;
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint)_tcpListener.LocalEndpoint;

        public void Start()
        {
            using var _ = _logger.BeginScope($"{VhLogger.FormatTypeName<TcpHost>()}");
            var tasks = new ConcurrentDictionary<Task, int>();

            var maxRetry = 5;
            for (var i = 0; ; i++)
            {
                try
                {
                    _logger.LogInformation($"Start listening on {VhLogger.Format(_tcpListener.LocalEndpoint)}...");
                    _tcpListener.Start();
                    break;
                }
                catch (SocketException ex) when (i < maxRetry)
                {
                    Console.WriteLine(ex.GetType());
                    _logger.LogError(ex.Message);
                    _logger.LogWarning($"retry: {i + 1} From {maxRetry}");
                    Thread.Sleep(5000);
                }
            }

            var task = ListenThread();
        }

        private async Task ListenThread()
        {

            try
            {
                // Listening for new connection
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    tcpClient.NoDelay = true;
                    //tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    var _ = ProcessClient(tcpClient);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is ObjectDisposedException))
                    _logger.LogError($"{ex.Message}");
            }
            finally
            {
                _logger.LogInformation($"{VhLogger.FormatTypeName(this)} Listener has been closed.");
            }

        }

        private async Task ProcessClient(TcpClient tcpClient)
        {
            using var _ = _logger.BeginScope($"RemoteEp: {tcpClient.Client.RemoteEndPoint}");

            try
            {
                // find certificate by ip 

                // establish SSL
                var certificate = await _sslCertificateManager.GetCertificate((IPEndPoint)tcpClient.Client.LocalEndPoint);
                _logger.LogInformation($"TLS Authenticating. CertSubject: {certificate.Subject}...");
                var sslStream = new SslStream(tcpClient.GetStream(), true);
                await sslStream.AuthenticateAsServerAsync(certificate, false, true);

                var tcpClientStream = new TcpClientStream(tcpClient, sslStream);
                if (!await ProcessRequestTask(tcpClientStream))
                {
                    _logger.LogTrace($"Disposing the connection...");
                    tcpClientStream.Dispose();
                }
            }
            catch (Exception ex)
            {
                tcpClient.Dispose();

                // logging
                if (!(ex is ObjectDisposedException))
                    _logger.LogError($"{ex}");
                else
                    _logger.LogTrace($"Connection has been closed.");
            }
        }

        private Task<bool> ProcessRequestTask(TcpClientStream tcpClientStream)
        {
            return Task.Run(async () =>
           {
               return await ProcessRequest(tcpClientStream);
           });
        }


        private Task<bool> ProcessRequest(TcpClientStream tcpClientStream, bool afterHello = false)
        {
            // read version
            _logger.LogTrace($"Waiting for request...");
            var version = tcpClientStream.Stream.ReadByte();
            if (version == -1)
            {
                if (!afterHello)
                    _logger.LogWarning("Connection closed without any request!");
                return Task.FromResult(false);
            }
            if (version != 1 && version != 2) throw new NotSupportedException($"Unsupported version in TCP stream. Version: {version}");

            // read request code
            var requestCode = tcpClientStream.Stream.ReadByte();
            if (version == -1) throw new Exception("Could not read client request code!");

            switch ((RequestCode)requestCode)
            {
                case RequestCode.Hello:
                    if (afterHello)
                        throw new Exception("Hello request has already been processed!");
                    return ProcessHello(tcpClientStream);

                case RequestCode.TcpDatagramChannel:
                    if (version == 1) ProcessTcpDatagramChannel1(tcpClientStream); //todo: remove old version
                    else ProcessTcpDatagramChannel(tcpClientStream);
                    return Task.FromResult(true);

                case RequestCode.TcpProxyChannel:
                    ProcessTcpProxyChannel(tcpClientStream);
                    return Task.FromResult(true);

                default:
                    throw new NotSupportedException("Unknown requestCode!");
            }
        }

        private void ProcessTcpDatagramChannel1(TcpClientStream tcpClientStream)
        {
            using var _ = _logger.BeginScope($"{VhLogger.FormatTypeName<TcpDatagramChannel>()}");

            // read SessionId
            _logger.LogTrace($"Reading SessionId...");
            var buffer = TunnelUtil.Stream_ReadWaitForFill(tcpClientStream.Stream, 8);
            if (buffer == null) throw new Exception("Could not read Session Id!");

            // finding session
            var sessionId = BitConverter.ToUInt64(buffer);
            using var _scope2 = _logger.BeginScope($"SessionId: {VhLogger.FormatId(sessionId)}");
            _logger.LogTrace($"SessionId has been readed.");

            try
            {
                var session = _sessionManager.GetSessionById(sessionId);

                // send OK reply
                TunnelUtil.Stream_WriteJson(tcpClientStream.Stream, new ChannelResponse() { ResponseCode = ResponseCode.Ok });

                _logger.LogTrace($"Creating a channel. ClientId: { VhLogger.FormatId(session.ClientId)}");
                var channel = new TcpDatagramChannel(tcpClientStream);
                session.Tunnel.AddChannel(channel);
            }
            catch (SessionException ex)
            {
                WriteChannelResponseException(ex, tcpClientStream.Stream);
                throw;
            }
        }

        private void ProcessTcpDatagramChannel(TcpClientStream tcpClientStream)
        {
            using var _ = _logger.BeginScope($"{VhLogger.FormatTypeName<TcpDatagramChannel>()}");

            // read SessionId
            _logger.LogInformation($"Reading the request...");
            var request = TunnelUtil.Stream_ReadJson<TcpDatagramChannelRequest>(tcpClientStream.Stream);

            // finding session
            using var _scope2 = _logger.BeginScope($"SessionId: {VhLogger.FormatId(request.SessionId)}");
            _logger.LogTrace($"SessionId has been readed.");

            try
            {
                var session = _sessionManager.GetSessionById(request.SessionId);

                // send OK reply
                TunnelUtil.Stream_WriteJson(tcpClientStream.Stream, new ChannelResponse() { ResponseCode = ResponseCode.Ok });

                _logger.LogTrace($"Creating a channel. ClientId: { VhLogger.FormatId(session.ClientId)}");
                var channel = new TcpDatagramChannel(tcpClientStream);
                session.Tunnel.AddChannel(channel);
            }
            catch (Exception ex)
            {
                if (request.ServerId == _sessionManager.ServerId || ex is SessionException) //todo: remove is SessionException for version 2
                    WriteChannelResponseException(ex, tcpClientStream.Stream);
                throw;
            }
        }


        private void ProcessTcpProxyChannel(TcpClientStream tcpClientStream)
        {
            using var _ = _logger.BeginScope($"{VhLogger.FormatTypeName<TcpProxyChannel>()}");

            _logger.LogInformation($"Reading the request...");
            var request = TunnelUtil.Stream_ReadJson<TcpProxyChannelRequest>(tcpClientStream.Stream);

            // find session
            using var _scope2 = _logger.BeginScope($"SessionId: {VhLogger.FormatId(request.SessionId)}");

            try
            {
                var session = _sessionManager.GetSessionById(request.SessionId);

                // connect to requested site
                _logger.LogTrace($"Connecting to the requested endpoint. RequestedEP: {VhLogger.FormatDns(request.DestinationAddress)}:{request.DestinationPort}");
                var requestedEndPoint = new IPEndPoint(IPAddress.Parse(request.DestinationAddress), request.DestinationPort);
                var tcpClient2 = _tcpClientFactory.CreateAndConnect(requestedEndPoint);

                // send response
                var response = new ChannelResponse()
                {
                    ResponseCode = ResponseCode.Ok,
                };
                TunnelUtil.Stream_WriteJson(tcpClientStream.Stream, response);

                // Dispose ssl strean and repalce it with a HeadCryptor
                tcpClientStream.Stream.Dispose();
                tcpClientStream.Stream = StreamHeadCryptor.CreateAesCryptor(tcpClientStream.TcpClient.GetStream(),
                    request.CipherKey, null, request.CipherLength);

                // add the connection
                _logger.LogTrace($"Adding the connection. ClientId: { VhLogger.FormatId(session.ClientId)}, CipherLength: {request.CipherLength}");
                var channel = new TcpProxyChannel(new TcpClientStream(tcpClient2, tcpClient2.GetStream()), tcpClientStream);
                session.Tunnel.AddChannel(channel);
            }
            catch (Exception ex)
            {
                if (request.ServerId == _sessionManager.ServerId || ex is SessionException) //todo: remove is SessionException for version 2
                    WriteChannelResponseException(ex, tcpClientStream.Stream);
                throw;
            }
        }

        private void WriteChannelResponseException(Exception ex, Stream stream)
        {
            if (ex is SessionException sessionException)
            {
                TunnelUtil.Stream_WriteJson(stream, new ChannelResponse()
                {
                    AccessUsage = sessionException.AccessUsage,
                    ResponseCode = sessionException.ResponseCode,
                    SuppressedBy = sessionException.SuppressedBy,
                    ErrorMessage = sessionException.Message
                });
            }
            else
            {
                TunnelUtil.Stream_WriteJson(stream, new ChannelResponse()
                {
                    ResponseCode = ResponseCode.GeneralError,
                    ErrorMessage = ex.Message
                });
            }
        }

        private async Task<bool> ProcessHello(TcpClientStream tcpClientStream)
        {
            _logger.LogInformation($"Processing hello request...");
            var helloRequest = TunnelUtil.Stream_ReadJson<HelloRequest>(tcpClientStream.Stream);

            // creating a session
            _logger.LogInformation($"Creating Session... TokenId: {VhLogger.FormatId(helloRequest.TokenId)}, ClientId: {VhLogger.FormatId(helloRequest.ClientId)}");
            var clientEp = (IPEndPoint)tcpClientStream.TcpClient.Client.RemoteEndPoint;

            try
            {
                var session = await _sessionManager.CreateSession(helloRequest, clientEp.Address);

                // reply hello session
                _logger.LogTrace($"Replying Hello response. SessionId: {VhLogger.FormatId(session.SessionId)}");
                var helloResponse = new HelloResponse()
                {
                    SessionId = session.SessionId,
                    ServerId = _sessionManager.ServerId,
                    SuppressedTo = session.SuppressedTo,
                    AccessUsage = session.AccessController.AccessUsage,
                    ResponseCode = ResponseCode.Ok
                };
                TunnelUtil.Stream_WriteJson(tcpClientStream.Stream, helloResponse);

                _logger.LogTrace($"Reusing Hello stream...");
                return await ProcessRequest(tcpClientStream, true);
            }
            catch (SessionException ex)
            {
                // reply error
                TunnelUtil.Stream_WriteJson(tcpClientStream.Stream, new HelloResponse()
                {
                    AccessUsage = ex.AccessUsage,
                    ResponseCode = ex.ResponseCode,
                    ErrorMessage = ex.Message
                });
                throw;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _tcpListener.Stop();
        }

    }
}