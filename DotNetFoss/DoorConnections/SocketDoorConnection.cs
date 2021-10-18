using Microsoft.Extensions.Logging;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;

namespace DotNetFoss.DoorConnections;

public class SocketDoorConnection : IDoorConnection
{
    private Pipe _inboundPipe = new Pipe(PipeOptions.Default);
    private Pipe _outboundPipe = new Pipe(PipeOptions.Default);
    private Queue<char> _inboundChars = new();
    private bool _flushOutput;
    private readonly ILogger _logger;

    public Encoding Encoding { get; } = Encoding.ASCII;

    public Socket Socket { get; }

    public CancellationTokenSource Cancel { get; }

    public CancellationToken CancellationToken => Cancel.Token;

    public SocketDoorConnection(Socket socket, ILogger<SocketDoorConnection> logger)
    {
        Socket = socket;
        _logger = logger;
        Cancel = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        var outbound = OutboundThread();
        var inbound = InboundThread();

        await Task.WhenAll(inbound, outbound);
    }

    /// <summary>
    /// Reads and stores data received from the client until we handle it
    /// </summary>
    public async Task InboundThread()
    {
        var writer = _inboundPipe.Writer;

        _logger.LogInformation("Inbound pipeline started");

        // TODO -- detect excessive inbound traffic and disconnect

        while (Socket.Available > 0 || !CancellationToken.IsCancellationRequested)
        {
            await Task.Yield();

            var bytesAvailable = Socket.Available;
            if (bytesAvailable == 0)
                continue;

            var memory = writer.GetMemory(bytesAvailable);
            var bytesRead = Socket.Receive(memory.Span, SocketFlags.None);
            writer.Advance(bytesRead);
            await writer.FlushAsync();

            if (!Socket.Connected)
                Cancel.Cancel();
        }

        _logger.LogInformation("Inbound pipeline stopped");
    }

    /// <summary>
    /// Sends data out to the client
    /// </summary>
    public async Task OutboundThread()
    {
        var reader = _outboundPipe.Reader;

        _logger.LogInformation("Outbound pipeline started");

        while (!CancellationToken.IsCancellationRequested)
        {
            await Task.Yield();

            if (_flushOutput)
            {
                _flushOutput = false;
                await _outboundPipe.Writer.FlushAsync();
            }

            if (reader.TryRead(out var readResult))
            {
                if (readResult.Buffer.IsSingleSegment)
                {
                    Socket.Send(readResult.Buffer.FirstSpan, SocketFlags.None);
                }
                else
                {
                    foreach (var span in readResult.Buffer)
                    {
                        Socket.Send(span.Span, SocketFlags.None);
                    }
                }

                reader.AdvanceTo(readResult.Buffer.End);
            }

            if (readResult.IsCompleted || readResult.IsCanceled)
                break;
        }

        _logger.LogInformation("Outbound pipeline stopped");
    }

    public char? ReadChar()
    {
        if (_inboundChars.TryDequeue(out var _ch))
            return _ch;

        var result = Read();
        if (result == null || result.Length == 0)
            return null;

        _ch = result[0];

        foreach (var ch in result.Skip(1))
            _inboundChars.Enqueue(ch);

        return _ch;
    }

    public unsafe string? Read()
    {
        if (_inboundChars.Count > 0)
        {
            var cached = new string(_inboundChars.ToArray());
            _inboundChars.Clear();
            return cached;
        }

        var reader = _inboundPipe.Reader;
        if (!reader.TryRead(out var result))
            return null;

        var length = (int)result.Buffer.Length;

        string value;
        if (length < 1024)
        {
            Span<byte> bytes = stackalloc byte[length];
            result.Buffer.CopyTo(bytes);
            value = Encoding.GetString(bytes);
        }
        else
        {
            var bytes = ArrayPool<byte>.Shared.Rent(length);
            result.Buffer.CopyTo(bytes);
            value = Encoding.GetString(bytes);
            ArrayPool<byte>.Shared.Return(bytes);
        }
        reader.AdvanceTo(result.Buffer.End);
        return value;
    }

    public unsafe void Write(char ch)
    {
        ReadOnlySpan<char> chars = stackalloc char[1] { ch };
        Span<byte> bytes = stackalloc byte[Encoding.GetMaxByteCount(1)];
        Encoding.GetBytes(chars, bytes);
        var memory = _outboundPipe.Writer.GetMemory(bytes.Length);
        bytes.CopyTo(memory.Span);
        _outboundPipe.Writer.Advance(bytes.Length);
        _flushOutput = true;
    }

    public unsafe void Write(string? characters)
    {
        if (characters == null || characters.Length == 0)
            return;

        if (characters.Length < 1024)
        {
            Span<byte> bytes = stackalloc byte[Encoding.GetByteCount(characters)];
            Encoding.GetBytes(characters.AsSpan(), bytes);
            var memory = _outboundPipe.Writer.GetMemory(bytes.Length);
            bytes.CopyTo(memory.Span);
            _outboundPipe.Writer.Advance(bytes.Length);
        }
        else
        {
            var bytes = ArrayPool<byte>.Shared.Rent(Encoding.GetByteCount(characters));
            Encoding.GetBytes(characters.AsSpan(), bytes);
            var memory = _outboundPipe.Writer.GetMemory(bytes.Length);
            bytes.CopyTo(memory.Span);
            ArrayPool<byte>.Shared.Return(bytes);
            _outboundPipe.Writer.Advance(bytes.Length);
        }
        _flushOutput = true;
    }

    public void Disconnect()
    {
        // We dropped the client, we don't care about their data anymore
        _outboundPipe.Writer.Complete();
        _inboundPipe.Writer.Complete();
        Cancel.Cancel();
    }
}
