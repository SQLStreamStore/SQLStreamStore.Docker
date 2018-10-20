namespace SqlStreamStore.HAL
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    internal class MarkdownResponse : Response
    {
        private readonly Stream _body;

        public MarkdownResponse(Stream body)
            : base(body == null ? 404 : 200, Constants.MediaTypes.TextMarkdown)
        {
            _body = body;
        }

        public override Task WriteBody(HttpResponse response, CancellationToken cancellationToken)
            => _body?.CopyToAsync(response.Body, 8192, cancellationToken) ?? Task.CompletedTask;
    }
}