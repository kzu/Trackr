using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

public class TrackrHandler : IHttpHandler
{
	public bool IsReusable { get { return true; } }

	public void ProcessRequest (HttpContext context)
	{
		ProcessRequest (context, true);
	}

	void ProcessRequest (HttpContext context, bool redirect)
	{
		// Use whatever was configured as a default.
		var parameters = new NameValueCollection (ConfigurationManager.AppSettings);
		foreach (var key in context.Request.QueryString.AllKeys) {
			// Override with query string parameters.
			parameters[key] = context.Request.QueryString[key];
		}

		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#cid
		if (string.IsNullOrEmpty (parameters["cid"])) {
			var cookie = context.Request.Cookies.Get ("cid");
			if (cookie == null) {
				cookie = new HttpCookie ("cid", Guid.NewGuid ().ToString ());
				context.Response.Cookies.Set (cookie);
			}
			parameters["cid"] = cookie.Value;
		}

		parameters["dp"] = context.Request.Path;

		// Generate the body from parameters
		var content = string.Join (Environment.NewLine + "&",
			parameters.AllKeys.Select (key => key + "=" + HttpUtility.UrlEncode(parameters[key])));

		Trace.WriteLine (content);

		var client = new HttpClient ();
		var response = client.PostAsync ("http://www.google-analytics.com/collect", new StringContent (content)).Result;
		if (response.StatusCode != HttpStatusCode.OK) {
			context.Response.StatusCode = (int)response.StatusCode;
			context.Response.Status = context.Response.StatusDescription = response.ReasonPhrase;
			context.Response.End ();
			return;
		}

		if (!redirect)
			return;

		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#dl
		var dl = parameters["dl"];
		var dh = parameters["dh"];
		if (!string.IsNullOrEmpty (dl)) {
			context.Response.Redirect (dl, true);
		} else if (!string.IsNullOrEmpty (dh)) {
			context.Response.Redirect (new Uri ("http://" + dh + context.Request.Url.PathAndQuery).ToString (), true);
		}
	}

	public void Track ()
	{
		var output = new StringWriter();
		var context = new HttpContext(new HttpRequest("bar.png", "http://vsgallery.azurewebsites.net/feed.atom", "v=1&tid=UA-56247715-1&t=pageview&an=Visual Studio&dh=gallery.mobileessentials.org"), new HttpResponse(output));

		ProcessRequest (context, false);

		Console.WriteLine (((HttpStatusCode)context.Response.StatusCode).ToString());
		Console.WriteLine (output.ToString());
	}
}