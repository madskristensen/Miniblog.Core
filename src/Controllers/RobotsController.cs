using System.Globalization;
using System.Text;
using System.Xml;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;

using Miniblog.Core.Services;

using WebEssentials.AspNetCore.Pwa;

namespace Miniblog.Core.Controllers;

public class RobotsController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings, WebManifest manifest) : Controller
{
    [Route("/robots.txt")]
    [OutputCache(PolicyName = "default")]
    public string RobotsTxt()
    {
        StringBuilder sb = new();
        _ = sb
            .AppendLine("User-agent: *")
            .AppendLine("Disallow:")
            .Append("sitemap: ")
            .Append(Request.Scheme)
            .Append("://")
            .Append(Request.Host)
            .AppendLine("/sitemap.xml");

        return sb.ToString();
    }

    [Route("/rsd.xml")]
    public void RsdXml()
    {
        EnableHttpBodySyncIO();

        string host = $"{Request.Scheme}://{Request.Host}";

        Response.ContentType = "application/xml";
        Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";

        using XmlWriter xml = XmlWriter.Create(Response.Body, new XmlWriterSettings { Indent = true });
        xml.WriteStartDocument();
        xml.WriteStartElement("rsd");
        xml.WriteAttributeString("version", "1.0");

        xml.WriteStartElement("service");

        xml.WriteElementString("enginename", "Miniblog.Core");
        xml.WriteElementString("enginelink", "https://github.com/madskristensen/Miniblog.Core/");
        xml.WriteElementString("homepagelink", host);

        xml.WriteStartElement("apis");
        xml.WriteStartElement("api");
        xml.WriteAttributeString("name", "MetaWeblog");
        xml.WriteAttributeString("preferred", "true");
        xml.WriteAttributeString("apilink", $"{host}/metaweblog");
        xml.WriteAttributeString("blogid", "1");

        xml.WriteEndElement(); // api
        xml.WriteEndElement(); // apis
        xml.WriteEndElement(); // service
        xml.WriteEndElement(); // rsd
    }

    [Route("/feed/{type}")]
    public async Task Rss(string type)
    {
        EnableHttpBodySyncIO();

        Response.ContentType = "application/xml";
        string host = $"{Request.Scheme}://{Request.Host}";

        using XmlWriter xmlWriter = XmlWriter.Create(
            Response.Body,
            new XmlWriterSettings() { Async = true, Indent = true, Encoding = new UTF8Encoding(false) });
        var posts = blog.GetPosts(10);
        var writer = await GetWriter(
            type,
            xmlWriter,
            (await posts.MaxByAsync(p => p.PubDate).ConfigureAwait(false))?.PubDate ?? DateTime.UtcNow);

        await foreach (var post in posts)
        {
            AtomEntry item = new()
            {
                Title = post.Title,
                Description = post.Content,
                Id = host + post.GetLink(),
                Published = post.PubDate,
                LastUpdated = post.LastModified,
                ContentType = "html",
            };

            foreach (string category in post.Categories)
            {
                item.AddCategory(new SyndicationCategory(category));
            }

            foreach (string tag in post.Tags)
            {
                item.AddCategory(new SyndicationCategory(tag));
            }

            item.AddContributor(new SyndicationPerson("test@example.com", settings.Value.Owner));
            item.AddLink(new SyndicationLink(new Uri(item.Id)));

            await writer.Write(item).ConfigureAwait(false);
        }
    }

    [Route("/sitemap.xml")]
    public async Task SitemapXml()
    {
        EnableHttpBodySyncIO();

        string host = $"{Request.Scheme}://{Request.Host}";

        Response.ContentType = "application/xml";

        using XmlWriter xml = XmlWriter.Create(Response.Body, new XmlWriterSettings { Indent = true });
        xml.WriteStartDocument();
        xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        var posts = blog.GetPosts(int.MaxValue);

        await foreach (var post in posts)
        {
            var lastMod = new[] { post.PubDate, post.LastModified };

            xml.WriteStartElement("url");
            xml.WriteElementString("loc", host + post.GetLink());
            xml.WriteElementString("lastmod", lastMod.Max().ToString("yyyy-MM-ddThh:mmzzz", CultureInfo.InvariantCulture));
            xml.WriteEndElement();
        }

        xml.WriteEndElement();
    }

    private void EnableHttpBodySyncIO()
    {
        var body = HttpContext.Features.Get<IHttpBodyControlFeature>();
        body!.AllowSynchronousIO = true;
    }

    private async Task<ISyndicationFeedWriter> GetWriter(string? type, XmlWriter xmlWriter, DateTime updated)
    {
        string host = $"{Request.Scheme}://{Request.Host}/";

        if (type?.Equals("rss", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            RssFeedWriter rss = new(xmlWriter);
            await rss.WriteTitle(manifest.Name).ConfigureAwait(false);
            await rss.WriteDescription(manifest.Description).ConfigureAwait(false);
            await rss.WriteGenerator("Miniblog.Core").ConfigureAwait(false);
            await rss.WriteValue("link", host).ConfigureAwait(false);
            return rss;
        }

        AtomFeedWriter atom = new(xmlWriter);
        await atom.WriteTitle(manifest.Name).ConfigureAwait(false);
        await atom.WriteId(host).ConfigureAwait(false);
        await atom.WriteSubtitle(manifest.Description).ConfigureAwait(false);
        await atom.WriteGenerator("Miniblog.Core", "https://github.com/madskristensen/Miniblog.Core", "1.0").ConfigureAwait(false);
        await atom.WriteValue("updated", updated.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)).ConfigureAwait(false);
        return atom;
    }
}
