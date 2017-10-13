using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Miniblog.Core.TagHelpers
{
    [HtmlTargetElement(Attributes = attrName)]
    public class IfTagHelper : TagHelper
    {
        private const string attrName = "asp-if";

        [HtmlAttributeName(attrName)]
        public bool Condition { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!Condition)
            {
                output.SuppressOutput();
            }
        }
    }
}
