﻿using System;
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
		// Generate query string from parameters
		var queryString = string.Join ("&", parameters.AllKeys
			.Select (key => key + "=" + HttpUtility.UrlEncode (parameters[key])));

		var url = "http://www.google-analytics.com/collect?" + queryString;

		Trace.WriteLine("Google Analytics: " + url);

		using (var client = new WebClient()) {
			client.DownloadData ("http://www.google-analytics.com/collect?" + queryString);
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

		// Always use the client's host address and user agent rather than the server's, unless overriden
		// by url
		if (!parameters.AllKeys.Contains("ua") && !string.IsNullOrEmpty(context.Request.UserAgent))
			parameters["ua"] = context.Request.UserAgent;
		if (!parameters.AllKeys.Contains("uip"))
			parameters["uip"] = context.Request.UserHostAddress;
		
		// Provide also user language
		if (!parameters.AllKeys.Contains("ul") && context.Request.UserLanguages != null && context.Request.UserLanguages.Length != 0)
			parameters["ul"] = context.Request.UserLanguages[0];

		// NOTE: without the sc=start, tracking doesn't work, but we do let the client send sc=end.
		if (!parameters.AllKeys.Contains ("sc"))
			parameters.Add ("sc", "start");

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
}
