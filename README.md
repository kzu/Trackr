Trackr
======

A Google Analytics tracking forwarder

## Problem

Say you have a simple static site in Amazon S3, a GitHub wiki, a static podcast feed, etc., and you just want to track page views / downloads of specific assets. It may be that you don't control the client where the requests are made (i.e. a feed reader or some external app that consumes those assets). In those cases, you need a server-side solution that can call Google Analytics with the tracking information, prior to letting users download (typically via a redirect). 

Now you have to read about how to do it (not that it's too complicated, as you can see in this implementation ;)).

## Solution

You just fork this repo, publish it to Azure (or whatever app platform that runs bare-bones ASP.NET 4.0 or later) and provide just two bits of configuration: TrackingID and RedirectTo. And presto! Any URL that you send to this new website will push that pageview to Google Analytics with the given tracking ID, passing on the client IP and user agent, and ultimately redirect to same file but on the RedirectTo domain.

In Azure, the two configuration values can be provided via the web site configuration "blade" (panel in a fancy word).

> Note: it only does GET redirects.


