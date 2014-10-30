using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Xamarin;

public class TrackrHandler : IHttpHandler
{
	private static readonly string InsightsApiKey = ConfigurationManager.AppSettings["Insights.ApiKey"]; 
	private static readonly string InsightsAppName = ConfigurationManager.AppSettings["Insights.AppName"];
	private static readonly bool insightsEnabled;

	public bool IsReusable { get { return true; } }

	static TrackrHandler()
	{
		if (!string.IsNullOrEmpty (InsightsApiKey) && !string.IsNullOrEmpty (InsightsAppName)) {
			Insights.Initialize (InsightsApiKey, ThisAssembly.Version, InsightsAppName);
			insightsEnabled = true;
		}
	}

	public void ProcessRequest (HttpContext context)
	{
		try {
			ProcessRequest (context, true);
		} catch (Exception e) {
			if (insightsEnabled)
				Insights.Report (e);
			throw;
		}
	}

	void ProcessRequest (HttpContext context, bool redirect)
	{
		var parameters = BuildParameters (context);

		TrackAnalytics (parameters);
		TrackInsights (context, parameters);

		if (!redirect)
			return;

		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#dl
		var dl = parameters["dl"];
		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#dh
		var dh = parameters["dh"];
		if (!string.IsNullOrEmpty (dl)) {
			context.Response.Redirect (dl, true);
		} else if (!string.IsNullOrEmpty (dh)) {
			context.Response.Redirect (new Uri ("http://" + dh + context.Request.Url.PathAndQuery).ToString (), true);
		}
	}

	private static void TrackAnalytics (NameValueCollection parameters)
	{
		// Generate the body from parameters
		var content = string.Join (Environment.NewLine + "&",
			parameters.AllKeys.Select (key => key + "=" + HttpUtility.UrlEncode (parameters[key])));

		Trace.WriteLine (content);

		var client = new HttpClient ();
		var response = client.PostAsync ("http://www.google-analytics.com/collect", new StringContent (content)).Result;
		if (!response.IsSuccessStatusCode && insightsEnabled) {
			Insights.Report (new HttpResponseException (response.Version, response.StatusCode, response.ReasonPhrase));
		}
	}

	private static void TrackInsights (HttpContext context, NameValueCollection parameters)
	{
		if (!insightsEnabled)
			return;

		// If we find app information, track it.
		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#an
		var appName = parameters["an"];
		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#av
		var appVersion = parameters["av"];
		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#aid
		var appId = parameters["aid"];
		// See https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#aiid
		var appInstallerId = parameters["aiid"];

		if (!string.IsNullOrEmpty(appName)) {
			var data = new Dictionary<string, string> {
				{ "Application Name", appName }
			};

			if (!string.IsNullOrEmpty(appVersion))
				data.Add("Application Version", appVersion);
			if (!string.IsNullOrEmpty(appId))
				data.Add("Application ID", appId);
			if (!string.IsNullOrEmpty(appInstallerId))
				data.Add("Application Installer ID", appInstallerId);

			var clientId = parameters["cid"];
			Insights.Identify(clientId, data);
		}

		Insights.Track (context.Request.Path, parameters.AllKeys.ToDictionary (key => key, key => parameters[key]));
	}

	private static NameValueCollection BuildParameters (HttpContext context)
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

		return parameters;
	}

	private class HttpResponseException : Exception
	{
		public HttpResponseException (Version version, HttpStatusCode status, string reasonPhrase)
		{
			this.Version = version;
			this.Status = status;
			this.ReasonPhrase = reasonPhrase;
		}

		public Version Version { get; private set; }
		public HttpStatusCode Status { get; private set; }
		public string ReasonPhrase { get; private set; }

		public override string ToString ()
		{
			return "HTTP/" + Version + " " + ((int)Status).ToString () + " " + Status + Environment.NewLine + ReasonPhrase;
		}
	}
}