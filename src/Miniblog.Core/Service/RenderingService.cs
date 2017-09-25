using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;

namespace Miniblog.Core
{
    public class RenderingService
    {
        private MarkdownPipeline _pipeline;

        public RenderingService(IOptions<BlogSettings> options)
        {
            BuildPipeline(options.Value.AllowHtml);
        }

        public HtmlString RenderMarkdown(Post post)
        {
            string html = Markdown.ToHtml(post.Content, _pipeline);

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

            _pipeline = builder.Build();
        }
    }
}
