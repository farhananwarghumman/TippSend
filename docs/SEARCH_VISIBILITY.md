# TippSend search visibility

Implemented 6 October 2026. Preferred public address: https://www.tippsend.ie/.

## Website foundation

- Six public pages have unique titles, descriptions, canonical URLs and social sharing metadata: home, Send an Item, For Businesses, Support, Privacy and Terms.
- `/sitemap.xml` lists only these pages on the preferred HTTPS hostname. No fabricated modification dates, prices, reviews or ratings.
- `/robots.txt` allows public crawling, including search crawlers such as Googlebot, Bingbot, OAI-SearchBot and Claude-SearchBot through the wildcard rule. Private operational, tracking and payment routes and query strings are excluded.
- Unknown/non-public routes, query-string responses and POST responses carry an X-Robots-Tag noindex header. Private layouts also emit noindex. These are indexing controls, not replacements for authentication or token checks.
- Organization JSON-LD contains the actual brand, public contact address, service area and request-and-confirm description. No invented street address, telephone or credentials. This does not claim eligibility for Google's LocalBusiness rich results.
- Public content is rendered by Razor on the server and uses crawlable HTML links. No AI-only content or hidden keyword pages are added. Crawl access does not guarantee inclusion or recommendation.

## Owner account steps still required

1. Sign in to Google Search Console with the Google account that should own TippSend's search reporting. Add a Domain property `tippsend.ie`, publish its exact verification TXT record in Cloudflare, verify and submit `https://www.tippsend.ie/sitemap.xml`.
2. Sign in to Bing Webmaster Tools. Add/verify TippSend (or import from the verified Google property when appropriate), submit the sitemap and inspect the homepage.
3. Create a Google Business Profile only with confirmed real business details. A business delivering to customers can use a service-area profile and hide its home address. Owner must supply a genuine business address privately for verification, a public telephone if desired, correct operating hours and any required verification evidence.
4. Request honest reviews after actual completed deliveries. Publish useful service information and local examples when there is real experience to report. Do not create fake reviews, backlinks or duplicated town doorway pages.

No paid SEO subscription, advertising or analytics tracker is required. Search registration and a Business Profile are not completed merely by deploying these files. Search indexing and rankings remain decisions of each provider.

## References

- Google Search Essentials: https://developers.google.com/search/docs/essentials
- Google AI search guidance: https://developers.google.com/search/docs/appearance/ai-features
- OpenAI search crawler: https://developers.openai.com/api/docs/bots
- Google service-area business guidelines: https://support.google.com/business/answer/3038177
