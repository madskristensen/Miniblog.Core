using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;

namespace Miniblog.Core
{
    public class RenderingService
    {
        private MarkdownPipeline _pipelinePost;
        private MarkdownPipeline _pipelineComment;

        public RenderingService(IOptions<BlogSettings> options)
        {
            BuildPipeline(options.Value.AllowHtml);
        }

        public HtmlString RenderMarkdown(Post post)
        {
            string html = Markdown.ToHtml(post.Content, _pipelinePost);

            return new HtmlString(html);
        }

        public HtmlString RenderComment(Comment comment)
        {
            string html = Markdown.ToHtml(comment.Content, _pipelineComment);

            return new HtmlString(html);
        }

        private void BuildPipeline(bool allowHtml)
        {
            var builder = new MarkdownPipelineBuilder()
            .UseDiagrams()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter();

            if (!allowHtml)
            {
                builder.DisableHtml();
            }

            _pipelinePost = builder.Build();

            builder.DisableHtml();
            _pipelineComment = builder.Build();
        }
    }
}
