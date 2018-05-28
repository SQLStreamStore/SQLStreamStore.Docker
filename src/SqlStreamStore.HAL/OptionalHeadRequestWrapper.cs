namespace SqlStreamStore.HAL
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    internal class OptionalHeadRequestWrapper : IDisposable
    {
        private readonly IOwinContext _context;
        private readonly Stream _originalBody;

        public OptionalHeadRequestWrapper(IOwinContext context)
        {
            _context = context;
            _originalBody = _context.Response.Body;
            if(context.Request.Method == "HEAD")
            {
                context.Response.Body = new HeadRequestStream();
            }
        }
        
        public void Dispose()
        {
            _context.Response.Body = _originalBody;
        }

        private class HeadRequestStream : Stream
        {
            private long _length;

            public override void Flush() => FlushAsync(CancellationToken.None).Wait();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _length += count;
                Position += count;
                return Task.CompletedTask;
            }

            public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public override bool CanRead { get; } = false;
            public override bool CanSeek { get; } = false;
            public override bool CanWrite { get; } = true;
            public override long Length => _length;
            public override long Position { get; set; }
        } 
    }
}