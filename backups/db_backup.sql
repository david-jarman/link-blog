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

