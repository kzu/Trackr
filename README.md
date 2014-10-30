Trackr
======

A Google Analytics tracking forwarder

## Problem

Say you have a simple static site in Amazon S3, a GitHub wiki, a static podcast feed, etc., and you just want to 
track page views / downloads of specific assets. It may be that you don't control the client where the requests 
are made (i.e. a feed reader or some external app that consumes those assets). In those cases, you need a server-side 
solution that can call Google Analytics with the tracking information, prior to letting users download (typically 
via a redirect). 

Now you have to read about how to do it (not that it's too complicated, as you can see in this implementation ;)).

## Solution

You just fork this repo, publish it to Azure (or whatever app platform that runs bare-bones ASP.NET 4.0 or later) and 
optionally provide any of the supported [Google Analytics Measurement Protocol parameters](https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters) 
via app settings. These parameters via settings can be overriden on a per-request/url basis, so they are essentially 
your defaults. 

Make sure you provide one way (app settings) or the other (url query string) all the [required parameters](https://developers.google.com/analytics/devguides/collection/protocol/v1/reference#required).

> Note: [Client ID or cid](https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#cid) 
> is treated specially. If not received as a url query string parameter, a new Guid will be generated on the handler 
> and it will be set as a cookie named "cid" for reuse in future requests.
 
The domain you want to redirect to can be specified one way (app settings) or the other (url query string) with the 
[Document Host Name or dh](https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#dh) 
parameter.

And that's it! Any URL that you send to this new website will push that pageview to Google Analytics with the given 
parameters, passing on the client IP and user agent, and ultimately redirect to same file but on the "dh" domain.

> Note: it only does GET redirects.


### Xamarin Insights Support

[Xamarin Insights](http://xamarin.com/insights) is a new analytics offering by [Xamarin](http://www.xamarin.com) that provides many of the benefits 
of Google Analytics, with a far friendlier and simpler UI.

If you want to report to Xamarin Insights, just add the following app settings to your deployement: 
* Insights.ApiKey: the API key for your Insights app 
* Insights.AppName: the name of the application as created in Insights.

From this point on, every URL request (or also via the app settings) can specify the Application Name, ID, Version 
and Installer ID as documented in [App Tracking](https://developers.google.com/analytics/devguides/collection/protocol/v1/parameters#apptracking) 
and that information will be used to identify the client.