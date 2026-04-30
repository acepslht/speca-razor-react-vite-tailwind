using Microsoft.AspNetCore.Razor.TagHelpers;
using Speca.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speca.Core.Helpers
{
    [HtmlTargetElement("vite-entry")]
    public class ViteEntryTagHelper(ViteService vite) : TagHelper
    {
        private readonly ViteService _vite = vite;

        [HtmlAttributeName("src")]
        public string Src { get; set; } = "";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(Src)) return;

            output.TagName = null;
            var sb = new StringBuilder();
            var finalUrl = _vite.GetAssetUrl(Src);

            if (_vite.IsDevelopment)
            {
                if (!context.Items.ContainsKey("ViteScriptsInjected"))
                {
                    sb.AppendLine("<script type=\"module\">");
                    sb.AppendLine("  import RefreshRuntime from '/dist/@react-refresh'");
                    sb.AppendLine("  RefreshRuntime.injectIntoGlobalHook(window)");
                    sb.AppendLine("  window.$RefreshReg$ = () => {}");
                    sb.AppendLine("  window.$RefreshSig$ = () => (type) => type");
                    sb.AppendLine("  window.__vite_plugin_react_preamble_installed__ = true");
                    sb.AppendLine("</script>");
                    sb.AppendLine("<script type=\"module\" src=\"/dist/@vite/client\"></script>");
                    context.Items["ViteScriptsInjected"] = true;
                }
            }

            if (Src.EndsWith(".css"))
                sb.AppendLine($"<link rel=\"stylesheet\" href=\"/dist{finalUrl}\" />");
            else
                sb.AppendLine($"<script type=\"module\" src=\"/dist{finalUrl}\"></script>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }


    [HtmlTargetElement("vite-asset")]
    public class ViteAssetTagHelper(ViteService vite) : TagHelper
    {
        private readonly ViteService _vite = vite;

        [HtmlAttributeName("src")]
        public string Src { get; set; } = "";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(Src)) return;

            output.TagName = null;
            var sb = new StringBuilder();
            var finalUrl = _vite.GetAssetUrl(Src);

            if (Src.EndsWith(".css"))
                sb.AppendLine($"<link rel=\"stylesheet\" href=\"/dist{finalUrl}\" />");
            else
                sb.AppendLine($"<script type=\"module\" src=\"/dist{finalUrl}\"></script>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }
}
