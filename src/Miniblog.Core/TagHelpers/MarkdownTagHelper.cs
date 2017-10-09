using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Miniblog.Core.TagHelpers
{
    [HtmlTargetElement("markdown", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class MarkdownTagHelper : TagHelper
    {
        private static MarkdownPipeline _pipelineHtml = BuildPipelines(true);
        private static MarkdownPipeline _pipelineNoHtml = BuildPipelines(false);
        private IOptions<BlogSettings> _options;

        public MarkdownTagHelper(IOptionsSnapshot<BlogSettings> options)
        {
            _options = options;
        }

        public bool? AllowHtml { get; set; }

        public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync(NullHtmlEncoder.Default);
            var markdown = content.GetContent();

            var pipeline = !AllowHtml.HasValue || _options.Value.AllowHtml ? _pipelineHtml : _pipelineNoHtml;
            var htmlContent = new MarkdownHtmlContent(markdown, pipeline);

            output.Content.SetHtmlContent(htmlContent);
            output.TagName = null;
        }

        private static MarkdownPipeline BuildPipelines(bool allowHtml)
        {
            var builder = new MarkdownPipelineBuilder()
            .UseDiagrams()
            .UseAdvancedExtensions();

            if (!allowHtml)
            {
                builder.DisableHtml();
            }

            return builder.Build();
        }

        private class MarkdownHtmlContent : IHtmlContent
        {
            private readonly string _markdown;
            private readonly MarkdownPipeline _pipeline;

            public MarkdownHtmlContent(string markdown, MarkdownPipeline pipeline)
            {
                _markdown = markdown;
                _pipeline = pipeline;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                Markdown.ToHtml(_markdown, writer, _pipeline);
            }
        }
    }
}
