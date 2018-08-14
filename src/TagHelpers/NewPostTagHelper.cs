using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core.TagHelpers
{
    [HtmlTargetElement("a", Attributes="new-post")]
    public class NewPostTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.SetAttribute("asp-route-id", "0");
            base.Process(context, output);
        }
    }
}
