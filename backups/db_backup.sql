--
-- PostgreSQL database dump
--

-- Dumped from database version 16.4
-- Dumped by pg_dump version 16.8 (Ubuntu 16.8-1.pgdg24.04+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: Posts; Type: TABLE DATA; Schema: public; Owner: u34fcet9a011tp
--

COPY public."Posts" ("Id", "Title", "Date", "Link", "LinkTitle", "Contents", "ShortTitle", "UpdatedDate") FROM stdin;
59d63a46-4048-494b-b12e-907c89423d86	My week in music	2025-02-22 07:24:30.673298+00	https://open.spotify.com/album/3cusZESjkIDnDXyQwbpSsT	Moanin’ The Blues	<p>I reached a major life milestone this week: I fell in love with Hank Williams. This is how I know I’m getting old. The music is timeless though. </p>\n\n<p>Other notable artists this week: Ethel Cain and Kendrick Lamar. I need to find some new music asap. </p>	music-of-the-week	2025-02-22 07:24:30.673298+00
fa95bade-60c0-42ff-9859-f0cbb4a9fff2	Show me the source	2025-02-21 06:24:15.744546+00	https://github.com/david-jarman/link-blog	Website source code	I made the repo for this website public. I’ll share more details about the build process once the site is further along, but dropping the link here as I didn’t have anything else to post for the day 🤦‍♂️	website-source-code	2025-02-21 06:24:15.744546+00
7942ca49-82ef-411c-8f5a-4c0a65855116	A Place to Call Home	2025-02-19 05:02:31.748579+00			<p>Hey, my name is David Jarman.</p>\n\n<h3>// Intent</h3>\n<p>I'm starting this blog to create a space of my own on the web. I have a wide array of interests and do not plan to focus this blog on any one particular topic. We'll see where this leads. The goal is not to build a following, be an "influencer" or "thought leader". I'd almost prefer if nobody reads this to be honest. <i>The goal is only to practice and improve my writing skills</i>. I will not use AI to write these posts, as that would go against the goal. I have no qualms with using AI and in fact use it all the time, but I will not improve my writing by having an LLM produce these posts for me.</p>\n\n<h3>// Interests</h3>\n<p>I have a wide variety of interests, and have been known to say that hobbies are my hobby. I <b>love</b> to try new things. There is nothing better than trying something new and seeing huge personal improvements. This blog is a good example, I've had a lot of fun writing the code for this website. The problem is that I also can lose interest quickly, but the following have been lifelong hobbies of mine:</p>\n\n<p>Riding bikes (all types of bikes), coding (wrote my first program in BASIC on a TI-89 in 2005), and music (playing and listening).</p>\n\n<p>Some hobbies have come and gone, such as beer brewing, running, cooking, and game dev, but each one has brought me valuable knowledge that I'm grateful for. If you ask anyone in my family, the apple doesn't fall from the tree. We once counted how many hobbies my dad has had over the years, I forget the exact number but it was over 20. Hobbies <b>are</b> my hobbies.</p>\n\n<h3>// Where you can find me</h3>\n<p>I don't post much anywhere else, but maybe that will change with this blog.</p>\n\n<a href="https://github.com/david-jarman">GitHub</a><br>\n<a href="https://www.threads.net/@david_jarman">Threads</a><br>\n<a href="https://www.linkedin.com/in/david-jarman-31387131/">LinkedIn</a><br>	hello-world	2025-02-19 05:02:31.748579+00
c8f0e743-50f9-4bc1-8689-66ec6076155c	Short-circuiting to get back on track	2025-02-19 20:05:40.623183+00			<p><b>Problem:</b> Sometimes I start out my day at work not feeling it. Sometimes that feeling lasts all day and my productivity suffers.</p>\n\n<p><b>Solution:</b>Find ways to short-circuit those feelings. This requires introspection, which can be very difficult to do when you aren't feeling it. But sometimes, something happens in your day that you didn't expect, and it helps you short-circuit and get back on track.</p>\n\n<p><b><i>The number one thing for me that helps me get back on track is talking to people.</b></i> Find someone to have a conversation with, ideally someone who gives you energy and can make you laugh. Laughter and lively discussion can turn almost any bad day around for me. Find the people that give you joy, and give them a call. It's a better use of time than just sitting there, and when you are done, you may just have a good day.</p>\n\n<p>Be that person that gives others joy and energy on the days you are feeling it. Call your friends and family on good days, not just the bad ones.</p>	short-circuit	2025-02-19 20:05:40.623183+00
f25dd03e-2770-41db-a9e6-cd904f6ea935	Adding the Trix editor to my Blazor SSR site	2025-02-22 23:20:47.629586+00	https://trix-editor.org	Trix: Rich Editor	<div><strong><em>Reinventing a blog website is so fun<br><br></em></strong>I've been raw-dogging these posts thus far, manually typing HTML elements into a raw textarea in my admin page so I get something that looks halfway decent.<br><br>I decided it was time to upgrade my post editing experience. I was completely unaware of the term WYSIWYG (What You See Is What You Get), but figured that embedding a rich-text editor into a website must be a <em>very&nbsp;</em>solved problem at this point. So I asked ChatGPT (first 4o, then o3-mini, then o1-pro) for advice on what I should do for this blog. I looked at a few options first, but so many projects are licensed under GPL2 or have weird pricing models. I needed the most bare-bones simple text editor that's totally free to use. That's when I found Trix.<br><br>I'm really liking is so far. I was able to integrate it into my Blazor admin page in about 5 minutes, including time figuring out how to make sure the binding of the content gets set up correctly. <a href="https://github.com/david-jarman/link-blog/commit/d747aa58bc3e7c6ad95052d8d27e270a710bd631">Here is the commit that shows the integration</a>.<br><br>In my testing, it's covered 90% of my use cases for styling in my writing. I think my only grip so far is that you can only insert an h1 heading. Probably not a huge deal, and I may be able to play around with Trix a bit more and figure out how to add a custom toolbar action to insert an h2 heading instead. Or maybe I can just style the h1 in the posts class to be a bit smaller. We'll see.<br><br>I have lots of other fun things I want to do with this blog, so it may fall by the way side. I also added an Atom feed to the site today, served at <a href="https://davidjarman.net/atom/all">https://davidjarman.net/atom/all</a>.</div>	trix-is-for-adults	2025-02-22 23:20:47.629586+00
3c17de3b-5afe-4b71-955b-06ddd6442ff1	Brown Rice	2025-02-23 06:53:42.549141+00			<div>Fell in love with brown rice today.. It tastes so good, is much healthier.. Why have I avoided it my whole life? Going forward I think it will become my main driver.<br><br>Only con I can think of is it does take a while longer to cook.&nbsp;</div>	brown-rice	2025-02-23 06:53:42.549141+00
604ba90e-3f17-4fa1-8851-89a7746f0255	Mastodon Comments on Blog?	2025-02-24 00:41:52.254658+00	https://beej.us/blog/data/mastodon-comments/	Mastodon Comments	<div>I don't actually use Mastodon anymore, but am tempted to try this out. It would be cool to have comments on the blog, even if just for the process of going through the motions and gaining a bit deeper understanding of <a href="https://en.wikipedia.org/wiki/ActivityPub">ActivityPub</a>.&nbsp;<br><br>This has the potential to become a rabbit hole for me though, as I'm sure I'll have to set up my own server and all that entails. I've been enjoying Heroku as a hosting platform so I bet it won't be&nbsp;<em>too&nbsp;</em>bad to set up (famous last words).</div>	mastodon-comments-on-blog	2025-02-24 00:41:52.254658+00
4f4ee22a-ebd8-412d-b38f-4a9513c8d649	Now on Bluesky @davidjarman.net	2025-02-24 05:34:25.51439+00	https://bsky.app/profile/davidjarman.net	David Jarman (@davidjarman.net) - Bluesky	<div>You can now find me on Bluesky with an updated handle. My interest in social media has been pretty low in the last few months, but I do find it fun linking myself all over the internet.&nbsp;</div>	now-on-bsky	2025-02-24 05:34:25.51439+00
56dbc42f-7aeb-4f29-988b-30c61d1ac485	"Vendoring"	2025-02-24 20:11:38.50574+00	https://htmx.org/essays/vendoring/	Vendoring	<blockquote>“Vendoring” software is a technique where you copy the source of another project directly into your own project.</blockquote><div>I love this idea, so many times you need to debug into your dependencies to figure out an issue or why things aren't working as you expected.<br><br>I mainly develop in C# and .NET and am fortunate to work at Microsoft, so I've done this many times in the past where I grab the source for a dependency, copy it into my code, update the references, and start debugging. This is always a temporary step. I don't actually check in the dependent code. I don't do it that often anymore, as you can enable debug options to disable "just my code" and if your dependencies publish symbols (most do in my experience), you can just F11 and step into library code from Nuget packages.<br><br>In the future, I may take up the vendoring approach for web frontends, where licenses allow. If nothing else, it gives me peace of mind that the dependency won't just disappear from the CDN.</div>	vendoring-essay	2025-02-24 20:11:38.50574+00
69b16f23-94d6-486d-b2d4-3f4c6696ccf1	Claude Code Initial Impressions	2025-02-24 21:36:53.288455+00	https://www.anthropic.com/news/claude-3-7-sonnet	Claude Code Announcement	<div>Claude announced a new hybrid reasoning model today. That's a great idea to have a singular model for both reasoning and quick responses. <br><br>What I'm more interested in is their new Claude Code tool. It's an interactive CLI that is similar to GitHub Copilot or Cursor, but only runs in your terminal as of now. Here is the link for setting it up: <a href="https://docs.anthropic.com/en/docs/agents-and-tools/claude-code/overview">https://docs.anthropic.com/en/docs/agents-and-tools/claude-code/overview</a><br><br></div><div>I was hoping that this tool would just use my existing Claude plan, but no, of course you actually pay for the tokens it uses. I'm sure this was a very conscious decision, as this tool uses A LOT of tokens right now. I mean, it's breathtaking. The first thing I did was load it up on my link blog codebase, and ran the /init command to generate a readme file for the codebase. I immediately ran the /cost command to see how much that operation costed. Thirty cents. That may not sound like much, but for as small as my codebase is, I was expecting that to only be a few cents. I then gave it a very specific task to add validation to my admin post authoring form. I gave it a fair bit of instruction, as the docs recommends treating the tool like a software engineer that you would delegate a task to. So I gave it hints as to how to find validation rules and all that. I then sent it off. It ran for something like 2 minutes making the change. It prompted me for permission to perform tool actions (e.g. run some bash commands, run build, etc). After a total of 10 minutes of use, I was up to $1.50 in spend, the code did not build, and I realized that the <a href="https://github.com/anthropics/claude-code/issues/26">tool call to build the code was broken</a>. <strong><em>Edit: It turns out powershell is not officially supported yet. You must use bash or zsh to launch claude.</em></strong><br><br>I'm still excited about this tool and will keep playing around with it. I'll probably have to reload my anthropic wallet with more credits soon as it is expensive, but so far it seems like a really cool concept, and I hope they keep improving it and driving down the cost.</div>	claude-code-initial-impressions	2025-02-24 22:23:53.411161+00
cac8f7b9-a92c-4de7-8da9-09675d052531	Download your Kindle books	2025-02-26 07:05:40.469807+00	https://github.com/Make-Fun-Stuff/kindle-library-downloader/blob/main/kindle-library-downloader.user.js	kindle-library-downloader.user.js	<div>I guess Amazon is removing the ability to download your ebooks, so my wife asked me to help her download all hers. She has 1,600(ish) books in her library, so manually going through and clicking download on them was an obvious no-go.<br><br>We found a neat script (<a href="https://www.reddit.com/r/DataHoarder/comments/1ivryul/heres_a_browser_script_to_download_your_whole/">via</a> Reddit) to do the clicking and downloading automatically and have been running it for the last 2 hours. I'm seeing some javascript errors being thrown in the console, but it seems to be working.</div>	download-kindle-books	2025-02-26 07:05:40.469807+00
b2a38a28-d3c8-40a3-ac77-c5b904136e7a	.NET Aspire 9.1 just dropped	2025-02-27 04:40:22.39741+00	https://learn.microsoft.com/en-us/dotnet/aspire/whats-new/dotnet-aspire-9.1	Release Notes	<div>I'm very excited about this release of .NET Aspire. It's an absolute game changer for me, specifically in doing dev work on this blog.<br><br></div><div>This release has a bunch of great stuff, but I'm actually only interested in <a href="https://github.com/dotnet/aspire/pull/7085">the bug fix</a> to <a href="https://github.com/dotnet/aspire/issues/6704">this issue</a>. I really love the `dotnet watch run` command, which will perform a hot reload of your application when you make changes to the source files. Before this bug fix, sockets would not be freed up in a timely manner if the process ends, meaning that on reload, it would try to rebind to the existing ports and fail. This meant my workflow was 1. make a change 2. kill the running process 3. wait 10-15 seconds 4. run dotnet run again. Sometimes, I would have to rinse and repeat 3 and 4, if I hadn't waited long enough.<br><br>Now, I run `dotnet watch run`, make changes to my code (.cs, .css, and .razor files), and all I have to do to see the new changes is refresh my browser page. It's like magic. My dev loop has gone from ~30 seconds to see the changes reflected in the browser to near-instantly (the time it takes me to press F5 in Safari).<br><br>I've been very happy with .NET Aspire so far. But because I deploy to Heroku and not Azure, I can't take advantage of any of the deployment features. A side project I've been thinking about is to build a Heroku deployer based on a <a href="https://learn.microsoft.com/en-us/dotnet/aspire/deployment/overview">.NET aspire deployment manifest</a>.</div>	net-aspire-9-1	2025-02-27 04:40:22.39741+00
48b94f51-4928-4d8a-9043-531fbd59b999	Images!	2025-03-03 07:03:49.02425+00			<div>Spent my free time adding image support to the blog today. I'm using Azure Storage accounts, as it's what I know, and I did not feel like diving into S3 buckets right now, although I probably should at some point.<br><br>Once again, .NET Aspire proved its usefulness. I kicked off the changes by adding the Azure Storage hosting and client integrations via Aspire, then added a Minimal API POST endpoint so I had a place to upload the images to. GitHub Copilot is very useful in these kinds of tasks. I can write a comment block describing what I want the endpoint to do, all the edge cases I think it should handle, then hit tab and get 90% of what I wanted. I go through and tweak the rest, then I'm testing almost immediately. <br><br>The last piece of the puzzle was hooking up my new endpoint to the Trix editor. Fortunately, the Trix dev team provided a nice Javascript file to show how to hook up the event listeners and use an XHR object to post the image back to the server. Considering my lack of experience with Javascript, I was ecstatic to get this working in only a few iterations.<br><br>I edited my last post to add a picture from my bike ride, so I know it's working, but I better add one to this post too :)<br><br><figure data-trix-attachment="{&quot;contentType&quot;:&quot;image/jpeg&quot;,&quot;filename&quot;:&quot;2025012600044699778.jpeg&quot;,&quot;filesize&quot;:185777,&quot;height&quot;:697,&quot;href&quot;:&quot;https://linkblog.blob.core.windows.net/images/2025/03/03/07/01/49/2025012600044699778.jpeg&quot;,&quot;url&quot;:&quot;https://linkblog.blob.core.windows.net/images/2025/03/03/07/01/49/2025012600044699778.jpeg&quot;,&quot;width&quot;:1110}" data-trix-content-type="image/jpeg" data-trix-attributes="{&quot;caption&quot;:&quot;Being a goofball with my Instax camera&quot;,&quot;presentation&quot;:&quot;gallery&quot;}" class="attachment attachment--preview attachment--jpeg"><a href="https://linkblog.blob.core.windows.net/images/2025/03/03/07/01/49/2025012600044699778.jpeg"><img src="https://linkblog.blob.core.windows.net/images/2025/03/03/07/01/49/2025012600044699778.jpeg" width="1110" height="697"><figcaption class="attachment__caption attachment__caption--edited">Being a goofball with my Instax camera</figcaption></a></figure></div>	introducing-images	2025-03-03 07:03:49.02425+00
16d39b46-91b0-44d2-9485-24da8fd1fe64	Back on the bike	2025-03-02 06:06:00.974705+00	https://www.strava.com/activities/13763450458	Strava Ride	<div>I broke my wrist back in January when I fell skiing down a run in the early season. Snow conditions were pretty bad and the moguls on the run had really built up. The resort really hadn't been able to groom the runs yet. I did the dumbest move possible and on my first run of the day decided to go down a black diamond. Classic move on my part.<br><br>I did a turn to go around a mogul and didn't realize how big a drop off there was. I lost balance and fell down it onto hard packed snow. I broke the fall with my left hand and immediately knew something was wrong, but did not consider that I broke anything... I got back up and continued to ski down. I did fall one other time but it was a typical lose-your-balance type fall. The only issue was getting back up, when I realized I couldn't put <em>any</em> weight onto my left hand. That's when I knew I was cooked. I managed to ski the rest of the way down to the bottom and found the nearest ski patrol. They put my hand in a splint and that's when I saw the massive swelling. I knew my day, if not season, was over.<br><br>The last two months have been tough, I had been really looking forward to skiing and getting better this season. I also haven't been able to bike much. But I'm now close to fully recovered, the bone is healed and I got the all-clear to start biking again. Today was my first day back on the bike in two months, my biggest gap in between bike rides in years. What a day it was, a beautiful "fake spring" kind of day.<br><br>Here's to looking forward to many more rides soon.<br><br><figure data-trix-attachment="{&quot;contentType&quot;:&quot;image/jpeg&quot;,&quot;filename&quot;:&quot;IMG_4915.jpeg&quot;,&quot;filesize&quot;:4844615,&quot;height&quot;:3024,&quot;href&quot;:&quot;https://linkblog.blob.core.windows.net/images/2025/03/03/06/39/56/IMG_4915.jpeg&quot;,&quot;url&quot;:&quot;https://linkblog.blob.core.windows.net/images/2025/03/03/06/39/56/IMG_4915.jpeg&quot;,&quot;width&quot;:4032}" data-trix-content-type="image/jpeg" data-trix-attributes="{&quot;caption&quot;:&quot;The Snoqualmie Valley is beautiful and reminds me of home&quot;,&quot;presentation&quot;:&quot;gallery&quot;}" class="attachment attachment--preview attachment--jpeg"><a href="https://linkblog.blob.core.windows.net/images/2025/03/03/06/39/56/IMG_4915.jpeg"><img src="https://linkblog.blob.core.windows.net/images/2025/03/03/06/39/56/IMG_4915.jpeg" width="4032" height="3024"><figcaption class="attachment__caption attachment__caption--edited">The Snoqualmie Valley is beautiful and reminds me of home</figcaption></a></figure></div>	back-on-the-bike	2025-03-03 06:40:37.185823+00
ae9d36f3-3c82-44d9-91c6-991cd963422e	Wallbleed	2025-03-05 06:54:55.688571+00	https://www.ndss-symposium.org/ndss-paper/wallbleed-a-memory-disclosure-vulnerability-in-the-great-firewall-of-china/	Wallbleed: A Memory Disclosure Vulnerability in the Great Firewall of China	<div>A friend sent me this paper regarding a security vulnerability in Chinas Great Firewall. The hack is quite interesting and worth reading about, but this quote is what stuck with me&nbsp;<br><br></div><blockquote>Wallbleed exemplifies that the harm censorship middleboxes impose on Internet users is even beyond their obvious infringement of freedom of expression. When implemented poorly, it also imposes severe privacy and confidentiality risks to Internet users.</blockquote>	wallbleed	2025-03-05 06:54:55.688571+00
d011c5b0-2c8f-4ab6-8ad4-97e415748d31	Creating a markdown file from Microsoft Learn docs	2025-03-10 19:30:31.307049+00	https://github.com/microsoft/markitdown	MarkItDown - GitHub	<div>I just learned about a new open-source tool from Microsoft called MarkItDown.&nbsp;<br><br></div><blockquote>MarkItDown is a lightweight Python utility for converting various files to Markdown for use with LLMs and related text analysis pipelines.</blockquote><div>This seems similar to <a href="https://pandoc.org/">pandoc</a>, but instead of any being able to take any formatted document type and convert it to any other type, it only outputs to markdown. It can be used as a standalone CLI tool or as a python library. <br><br>I'm particularly interested in converting HTML to markdown, so that I can take public documentation online and convert it into a markdown file, which can be more effectively consumed by LLMs. I was playing around with this idea last week during a hackathon, where I wanted to take the <a href="https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops">query language specification for WIQL</a> that is online and turn it into a compact prompt, so the LLM can more reliably create WIQL queries for me.<br><br></div><div>To get the HTML for the web page, I use Simon Willison's tool <a href="https://github.com/simonw/shot-scraper">shot-scraper</a> to dump the HTML of the webpage, then pipe it into markitdown<br><br></div><pre>shot-scraper html https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax | markitdown &gt; wiql.md</pre><div>This produces a file called wiql.md (<a href="https://gist.github.com/david-jarman/256bb4f7c6b02fa982dba8d44cfbede2">link to gist with unmodified output</a>). It's certainly not perfect, the first 300 lines (out of around 1000), are not related to the documentation, and is just extra HTML that isn't needed. This could probably be mitigated by passing an element selector to shot-scraper, so it doesn't dump the unrelated HTML of the page. But it's not hard to delete those lines manually, and then the final result is pretty good. It looks fairly similar to the original web page.<br><br><em>edit: Here is the one-liner to only dump the relevant part of the page.. You have to wrap the output of shot-scraper in a &lt;html&gt; so markitdown can infer the input type.<br></em><br></div><pre>echo "&lt;html&gt;$(shot-scraper html https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax -s .content)&lt;/html&gt;" | markitdown -o wiql.md</pre><div><figure data-trix-attachment="{&quot;contentType&quot;:&quot;image/png&quot;,&quot;filename&quot;:&quot;image.png&quot;,&quot;filesize&quot;:196885,&quot;height&quot;:937,&quot;href&quot;:&quot;https://linkblog.blob.core.windows.net/images/2025/03/10/19/27/54/image.png&quot;,&quot;url&quot;:&quot;https://linkblog.blob.core.windows.net/images/2025/03/10/19/27/54/image.png&quot;,&quot;width&quot;:1826}" data-trix-content-type="image/png" data-trix-attributes="{&quot;caption&quot;:&quot;Side by side comparison&quot;,&quot;presentation&quot;:&quot;gallery&quot;}" class="attachment attachment--preview attachment--png"><a href="https://linkblog.blob.core.windows.net/images/2025/03/10/19/27/54/image.png"><img src="https://linkblog.blob.core.windows.net/images/2025/03/10/19/27/54/image.png" width="1826" height="937"><figcaption class="attachment__caption attachment__caption--edited">Side by side comparison</figcaption></a></figure>MarkItDown also supports plugins, so you can extend it to support other file formats. I've only played around with this a little bit, but I think it will be handy to have a quick and easy way to convert more documents to markdown. I'm particularly interested in the pdf and docx input types as well.</div>	markdown-for-microsoft-learn-docs	2025-03-10 20:33:29.889917+00
\.


--
-- Data for Name: Tags; Type: TABLE DATA; Schema: public; Owner: u34fcet9a011tp
--

COPY public."Tags" ("Id", "Name") FROM stdin;
9e1b9cf7-7767-4f4a-9ce5-87e9517a6557	intro
a419c685-a7e5-492a-b3ac-66cbee50333b	hobbies
56e75ed3-a4c2-4640-97ef-4992ddedc04d	dopamine
fcc4655a-4313-413b-a8fb-c338a417f1a5	advice-to-myself
b0b2cba4-82b6-44c0-896c-f68a264831e4	webdev
d8dc9813-ca5e-4eca-8140-74965280365e	meta
a5cb93f6-d4e6-436b-9130-6b874f7fc0b1	music
bdb4ced0-2a92-4a01-a77c-c16c47f45b64	trix
51560bdf-0d58-46c6-9539-dce9364fc642	colophon
421fb9fc-68c9-408f-801d-559be071836a	pontificating
947e7358-7a30-451b-a0c8-71019a7f6441	food
339fda86-1858-4d77-b54d-0a471a3d5a88	mastodon
d7bae5f1-f76f-434b-904c-e30765dea1ee	bluesky
42c6d80f-5cae-4c09-99f3-bf1867ac556d	programming
b65176f2-4961-494c-83ad-81bfbf60c78f	dependencies
1bf414eb-395b-4c63-ba62-34e7c95e2018	llms
a0fc6cfc-7636-4eb7-b816-5798e2682f19	claude
aa217264-5d70-4208-b4b2-adbeb9a8c890	ai
ee6822e1-4263-4a53-a6de-0f2da13adf3b	kindle
f410ddf4-bd59-48f7-aabc-35322e217448	data-hoarding
739afc03-a0a4-4b50-9aa6-0b00b36dd48d	aspire
b8244f53-df27-42d4-8728-fc1411b55c54	dotnet
cd50f142-234a-4a44-9f2b-363fd82ad77f	skiing
cfc50bc0-3c43-4e18-a685-8ed4a5ea206f	biking
ce57c7a9-5ac0-48ef-b722-e63051efd4b2	dev-notes
de2a5084-6ce4-4e86-888c-ca330cf306db	images
188566b6-ed4c-4123-8d70-a726ba3bf3b6	security
25d1859f-430e-4666-b34e-f1ac97704e6c	markdown
2ef98fe4-1f95-409b-afd8-e98153a032e1	shot-scraper
62cd5d58-679d-4f0b-b8f8-d4835c1e9f3f	tools
e3153f6e-d0c8-497e-9905-41f545a6d752	microsoft
ec9d8692-1571-4f7e-a517-003389631cce	markitdown
\.


--
-- Data for Name: PostTag; Type: TABLE DATA; Schema: public; Owner: u34fcet9a011tp
--

COPY public."PostTag" ("PostsId", "TagsId") FROM stdin;
7942ca49-82ef-411c-8f5a-4c0a65855116	9e1b9cf7-7767-4f4a-9ce5-87e9517a6557
7942ca49-82ef-411c-8f5a-4c0a65855116	a419c685-a7e5-492a-b3ac-66cbee50333b
c8f0e743-50f9-4bc1-8689-66ec6076155c	56e75ed3-a4c2-4640-97ef-4992ddedc04d
c8f0e743-50f9-4bc1-8689-66ec6076155c	fcc4655a-4313-413b-a8fb-c338a417f1a5
fa95bade-60c0-42ff-9859-f0cbb4a9fff2	b0b2cba4-82b6-44c0-896c-f68a264831e4
fa95bade-60c0-42ff-9859-f0cbb4a9fff2	d8dc9813-ca5e-4eca-8140-74965280365e
59d63a46-4048-494b-b12e-907c89423d86	a5cb93f6-d4e6-436b-9130-6b874f7fc0b1
f25dd03e-2770-41db-a9e6-cd904f6ea935	51560bdf-0d58-46c6-9539-dce9364fc642
f25dd03e-2770-41db-a9e6-cd904f6ea935	bdb4ced0-2a92-4a01-a77c-c16c47f45b64
3c17de3b-5afe-4b71-955b-06ddd6442ff1	421fb9fc-68c9-408f-801d-559be071836a
3c17de3b-5afe-4b71-955b-06ddd6442ff1	947e7358-7a30-451b-a0c8-71019a7f6441
604ba90e-3f17-4fa1-8851-89a7746f0255	339fda86-1858-4d77-b54d-0a471a3d5a88
4f4ee22a-ebd8-412d-b38f-4a9513c8d649	d7bae5f1-f76f-434b-904c-e30765dea1ee
56dbc42f-7aeb-4f29-988b-30c61d1ac485	42c6d80f-5cae-4c09-99f3-bf1867ac556d
56dbc42f-7aeb-4f29-988b-30c61d1ac485	b65176f2-4961-494c-83ad-81bfbf60c78f
69b16f23-94d6-486d-b2d4-3f4c6696ccf1	1bf414eb-395b-4c63-ba62-34e7c95e2018
69b16f23-94d6-486d-b2d4-3f4c6696ccf1	a0fc6cfc-7636-4eb7-b816-5798e2682f19
69b16f23-94d6-486d-b2d4-3f4c6696ccf1	aa217264-5d70-4208-b4b2-adbeb9a8c890
cac8f7b9-a92c-4de7-8da9-09675d052531	ee6822e1-4263-4a53-a6de-0f2da13adf3b
cac8f7b9-a92c-4de7-8da9-09675d052531	f410ddf4-bd59-48f7-aabc-35322e217448
b2a38a28-d3c8-40a3-ac77-c5b904136e7a	739afc03-a0a4-4b50-9aa6-0b00b36dd48d
b2a38a28-d3c8-40a3-ac77-c5b904136e7a	b8244f53-df27-42d4-8728-fc1411b55c54
16d39b46-91b0-44d2-9485-24da8fd1fe64	cd50f142-234a-4a44-9f2b-363fd82ad77f
16d39b46-91b0-44d2-9485-24da8fd1fe64	cfc50bc0-3c43-4e18-a685-8ed4a5ea206f
48b94f51-4928-4d8a-9043-531fbd59b999	51560bdf-0d58-46c6-9539-dce9364fc642
48b94f51-4928-4d8a-9043-531fbd59b999	739afc03-a0a4-4b50-9aa6-0b00b36dd48d
48b94f51-4928-4d8a-9043-531fbd59b999	bdb4ced0-2a92-4a01-a77c-c16c47f45b64
48b94f51-4928-4d8a-9043-531fbd59b999	ce57c7a9-5ac0-48ef-b722-e63051efd4b2
48b94f51-4928-4d8a-9043-531fbd59b999	de2a5084-6ce4-4e86-888c-ca330cf306db
ae9d36f3-3c82-44d9-91c6-991cd963422e	188566b6-ed4c-4123-8d70-a726ba3bf3b6
d011c5b0-2c8f-4ab6-8ad4-97e415748d31	25d1859f-430e-4666-b34e-f1ac97704e6c
d011c5b0-2c8f-4ab6-8ad4-97e415748d31	2ef98fe4-1f95-409b-afd8-e98153a032e1
d011c5b0-2c8f-4ab6-8ad4-97e415748d31	62cd5d58-679d-4f0b-b8f8-d4835c1e9f3f
d011c5b0-2c8f-4ab6-8ad4-97e415748d31	e3153f6e-d0c8-497e-9905-41f545a6d752
d011c5b0-2c8f-4ab6-8ad4-97e415748d31	ec9d8692-1571-4f7e-a517-003389631cce
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: u34fcet9a011tp
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20250215004115_InitialCreate	9.0.2
20250220042255_AddShortTitle	9.0.2
20250224000450_AddPostIndexesAndTagNameIndex	9.0.2
20250224051317_AddUpdatedDate	9.0.2
\.


--
-- PostgreSQL database dump complete
--

