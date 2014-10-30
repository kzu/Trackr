using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;

public class TrackHandler : IHttpHandler
{
	private static string TrackingID = ConfigurationManager.AppSettings["TrackingID"];
	private static string BaseUrl = ConfigurationManager.AppSettings["RedirectTo"];

	public bool IsReusable { get { return true; } }

	public void ProcessRequest (HttpContext context)
	{
		var client = new HttpClient ();
		var redirectUrl = new Uri (new Uri (BaseUrl), context.Request.Url.PathAndQuery);

		var content = @"v=1
&tid=" + TrackingID + @"
&cid=555
&t=pageview
&dh=" + redirectUrl.Host + @"
&dp=" + context.Request.Path + @"
&dt=" + context.Request.QueryString["t"] ?? context.Request.QueryString["title"] ?? Path.GetFileName(context.Request.Url.AbsolutePath);

		var clientIp = IPAddress.Parse (context.Request.UserHostAddress);
		if (!IPAddress.IsLoopback (clientIp))
			content += @"
&uip=" + context.Request.UserHostAddress;

		if (!string.IsNullOrEmpty (context.Request.UserAgent))
			content += @"
&ua=" + context.Request.UserAgent;

		client.PostAsync ("http://www.google-analytics.com/collect", new StringContent (content));
		context.Response.Redirect (redirectUrl.ToString ());
	}
}