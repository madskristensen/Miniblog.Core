# Blog engine for ASP.NET Core 2.0

A simple yet powerful blog engine built on ASP.NET Core 2.0.

**Demo website**: <https://miniblogcore.azurewebsites.net/>

## Features

- RSS and ATOM feeds
- User comments
- Windows/Open Live Writer support
- Search Engine optimized
- All major browsers fully supported (IE 9+)
- Social media integration (Facebook, Twitter, Pinterest)
- Follows best practices for web applications
  - [See DareBoost report](https://www.dareboost.com/en/report/59cd65b60cf235afa2921e9e)

## Technical features
- High performance. Gets 100/100 points on Google PageSpeed Insights 
  - [Run PageSpeed Insights](https://developers.google.com/speed/pagespeed/insights/?url=https%3A%2F%2Fminiblogcore.azurewebsites.net%2F)
- Speed Index < 1000
  - [See WebPageTest](http://www.webpagetest.org/result/170928_1R_cf91bb2d800cc389821c5cfa7e353f0d/) 
- Meets highest accessibility standards 
  - [Run accessibility validator](http://wave.webaim.org/report#/https://miniblogcore.azurewebsites.net/)
- W3C standards compliant 
  - [Run HTML validator](https://html5.validator.nu/?doc=https%3A%2F%2Fminiblogcore.azurewebsites.net%2F)
  - [Run RSS validator](https://validator.w3.org/feed/check.cgi?url=https%3A%2F%2Fminiblogcore.azurewebsites.net%2Ffeed%2Frss%2F)
- Automatic thumbnail generation
- Responsive web design
  - [See mobile emulators](https://www.responsinator.com/?url=https%3A%2F%2Fminiblogcore.azurewebsites.net%2F)
- Mobile friendly
  - [Run Mobile-Friendly Test](https://search.google.com/test/mobile-friendly?id=_wYVtpbWLJvqeerje7NN0A)
- Schema.org support with HTML 5 Microdata 
  - [Run testing tool](https://search.google.com/structured-data/testing-tool#url=https%3A%2F%2Fminiblogcore.azurewebsites.net%2F)
- OpenGraph support for Facebook, Twitter, Pinterest and more
  - [Check the tags](http://opengraphcheck.com/result.php?url=https%3A%2F%2Fminiblogcore.azurewebsites.net%2F#.WdhRDjBlB3g)
- Seach engine optimized
  - [Run SEO Site Checkup](https://seositecheckup.com/seo-audit/miniblogcore.azurewebsites.net)
- Security HTTP headers set
  - [Run security scan](https://securityheaders.io/?q=https%3A%2F%2Fminiblogcore.azurewebsites.net%2F&hide=on&followRedirects=on)
- Uses the [Azure Image Optimizer](https://github.com/madskristensen/ImageOptimizerWebJob) for superb image compression
- Uses a [CDN Tag Helper](https://github.com/madskristensen/WebEssentials.AspNetCore.CdnTagHelpers) to make it easy to serve the images from any CDN.

## How to use

1. Fork and/or clone this repo
2. Change user settings in `\src\appsettings.json`
3. Make any modifications you want
4. Deploy