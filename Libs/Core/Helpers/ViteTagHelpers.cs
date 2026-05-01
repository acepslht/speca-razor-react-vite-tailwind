using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using Speca.Core.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Speca.Core.Helpers
{
    public static class ViteTagHelpersExtentions
    {
        public static string GetVersionedUrl(this ViteEntryTagHelper entry, IWebHostEnvironment env, string path)
        {
            if (env.IsDevelopment())
            {
                return GetContentRootPathVersionedUrl(path, env);
            }
            else
            {
                return GetWebRootPathVersionedUrl(path, env);
            }
        }

        public static string GetVersionedUrl(this ViteAssetTagHelper asset, IWebHostEnvironment env, string path)
        {
            if(env.IsDevelopment())
            {
                return GetContentRootPathVersionedUrl(path, env);   
            }
            else
            {
                return GetWebRootPathVersionedUrl(path, env);
            }
        }

        private static string GetWebRootPathVersionedUrl(string path, IWebHostEnvironment env)
        {
            if (env?.WebRootPath == null) return path;
            try
            {
                var filePath = Path.Combine(env.WebRootPath, path.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hash = md5.ComputeHash(stream);
                        var hashString = Convert.ToHexString(hash).Substring(0, 8).ToLower();
                        return $"{path}?v={hashString}";
                    }
                }
            }
            catch { }
            return path;
        }

        private static string GetContentRootPathVersionedUrl(string path, IWebHostEnvironment env)
        {
            if (env?.ContentRootPath == null) return path;
            try
            {
                var parent = Directory.GetParent(env.ContentRootPath);
                var solutionPath = parent?.Parent?.FullName;
                if (string.IsNullOrEmpty(solutionPath)) return path;
                var filePath = Path.Combine(solutionPath, path.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hash = md5.ComputeHash(stream);
                        var hashString = Convert.ToHexString(hash)[..8].ToLower();
                        return $"dist/{path}?v={hashString}";
                    }
                }
            }
            catch { }
            return path;
        }
    }


    [HtmlTargetElement("vite-entry")]
    public class ViteEntryTagHelper(IWebHostEnvironment env) : TagHelper
    {
        private bool IsDevelopment => env.IsDevelopment();


        [HtmlAttributeName("srcdev")]
        public string Srcdev { get; set; } = "";


        [HtmlAttributeName("srcpro")]
        public string Srcpro { get; set; } = "";

        [HtmlAttributeName("asp-append-version")]
        public bool AppendVersion { get; set; } = false;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (IsDevelopment && string.IsNullOrEmpty(Srcdev)) return;
            if (!IsDevelopment && string.IsNullOrEmpty(Srcpro)) return;

            output.TagName = null;
            var sb = new StringBuilder();

            if (IsDevelopment)
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
                var src = AppendVersion ? this.GetVersionedUrl(env, $"{Srcdev}") : $"/dist/{Srcdev}";
                sb.AppendLine($"<script type=\"module\" src=\"{src}\"></script>");
            }
            else
            {
                var src = AppendVersion ? this.GetVersionedUrl(env, $"/dist/{Srcpro}") : $"/dist/{Srcpro}";
                sb.AppendLine($"<script type=\"module\" src=\"{src}\" ></script>");
            }
            output.Content.SetHtmlContent(sb.ToString());
        }
    }


    [HtmlTargetElement("vite-asset")]
    public class ViteAssetTagHelper(IWebHostEnvironment env) : TagHelper
    {
        private bool IsDevelopment => env.IsDevelopment();

        [HtmlAttributeName("srcdev")]
        public string Srcdev { get; set; } = "";


        [HtmlAttributeName("srcpro")]
        public string Srcpro { get; set; } = "";

        [HtmlAttributeName("asp-append-version")]
        public bool AppendVersion { get; set; } = false;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (IsDevelopment && string.IsNullOrEmpty(Srcdev)) return;
            if (!IsDevelopment && string.IsNullOrEmpty(Srcpro)) return;

            output.TagName = null;
            var sb = new StringBuilder();

            if (IsDevelopment)
            {
                if (Srcdev.EndsWith(".css"))
                {
                    var href = AppendVersion ? this.GetVersionedUrl(env, $"{Srcdev}") : $"/dist/{Srcdev}";
                    sb.AppendLine($"<link rel=\"stylesheet\" href=\"{href}\" />");
                }
                else
                {
                    var src = AppendVersion ? this.GetVersionedUrl(env, $"{Srcdev}") : $"/dist/{Srcdev}";
                    sb.AppendLine($"<script type=\"module\" src=\"{src}\"></script>");
                }
            }
            else
            {
                if (Srcpro.EndsWith(".css"))
                {
                    var href = AppendVersion ? this.GetVersionedUrl(env, $"/dist/{Srcpro}") : $"/dist/{Srcpro}";
                    sb.AppendLine($"<link rel=\"stylesheet\" href=\"{href}\" />");
                }
                else
                {
                    var src = AppendVersion ? this.GetVersionedUrl(env, $"/dist/{Srcpro}") : $"/dist/{Srcpro}";
                    sb.AppendLine($"<script type=\"module\" src=\"{src}\" ></script>");
                }
            }

            output.Content.SetHtmlContent(sb.ToString());
        }

    }
}
