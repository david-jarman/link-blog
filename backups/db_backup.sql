--
-- PostgreSQL database dump
--

-- Dumped from database version 16.4
-- Dumped by pg_dump version 16.6 (Ubuntu 16.6-1.pgdg24.04+1)

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

COPY public."Posts" ("Id", "Title", "Date", "Link", "LinkTitle", "Contents", "ShortTitle") FROM stdin;
59d63a46-4048-494b-b12e-907c89423d86	My week in music	2025-02-22 07:24:30.673298+00	https://open.spotify.com/album/3cusZESjkIDnDXyQwbpSsT	Moanin’ The Blues	<p>I reached a major life milestone this week: I fell in love with Hank Williams. This is how I know I’m getting old. The music is timeless though. </p>\n\n<p>Other notable artists this week: Ethel Cain and Kendrick Lamar. I need to find some new music asap. </p>	music-of-the-week
7942ca49-82ef-411c-8f5a-4c0a65855116	A Place to Call Home	2025-02-19 05:02:31.748579+00			<p>Hey, my name is David Jarman.</p>\n\n<h3>// Intent</h3>\n<p>I'm starting this blog to create a space of my own on the web. I have a wide array of interests and do not plan to focus this blog on any one particular topic. We'll see where this leads. The goal is not to build a following, be an "influencer" or "thought leader". I'd almost prefer if nobody reads this to be honest. <i>The goal is only to practice and improve my writing skills</i>. I will not use AI to write these posts, as that would go against the goal. I have no qualms with using AI and in fact use it all the time, but I will not improve my writing by having an LLM produce these posts for me.</p>\n\n<h3>// Interests</h3>\n<p>I have a wide variety of interests, and have been known to say that hobbies are my hobby. I <b>love</b> to try new things. There is nothing better than trying something new and seeing huge personal improvements. This blog is a good example, I've had a lot of fun writing the code for this website. The problem is that I also can lose interest quickly, but the following have been lifelong hobbies of mine:</p>\n\n<p>Riding bikes (all types of bikes), coding (wrote my first program in BASIC on a TI-89 in 2005), and music (playing and listening).</p>\n\n<p>Some hobbies have come and gone, such as beer brewing, running, cooking, and game dev, but each one has brought me valuable knowledge that I'm grateful for. If you ask anyone in my family, the apple doesn't fall from the tree. We once counted how many hobbies my dad has had over the years, I forget the exact number but it was over 20. Hobbies <b>are</b> my hobbies.</p>\n\n<h3>// Where you can find me</h3>\n<p>I don't post much anywhere else, but maybe that will change with this blog.</p>\n\n<a href="https://github.com/david-jarman">GitHub</a><br>\n<a href="https://www.threads.net/@david_jarman">Threads</a><br>\n<a href="https://www.linkedin.com/in/david-jarman-31387131/">LinkedIn</a><br>	hello-world
c8f0e743-50f9-4bc1-8689-66ec6076155c	Short-circuiting to get back on track	2025-02-19 20:05:40.623183+00			<p><b>Problem:</b> Sometimes I start out my day at work not feeling it. Sometimes that feeling lasts all day and my productivity suffers.</p>\n\n<p><b>Solution:</b>Find ways to short-circuit those feelings. This requires introspection, which can be very difficult to do when you aren't feeling it. But sometimes, something happens in your day that you didn't expect, and it helps you short-circuit and get back on track.</p>\n\n<p><b><i>The number one thing for me that helps me get back on track is talking to people.</b></i> Find someone to have a conversation with, ideally someone who gives you energy and can make you laugh. Laughter and lively discussion can turn almost any bad day around for me. Find the people that give you joy, and give them a call. It's a better use of time than just sitting there, and when you are done, you may just have a good day.</p>\n\n<p>Be that person that gives others joy and energy on the days you are feeling it. Call your friends and family on good days, not just the bad ones.</p>	short-circuit
fa95bade-60c0-42ff-9859-f0cbb4a9fff2	Show me the source	2025-02-21 06:24:15.744546+00	https://github.com/david-jarman/link-blog	Website source code	I made the repo for this website public. I’ll share more details about the build process once the site is further along, but dropping the link here as I didn’t have anything else to post for the day 🤦‍♂️	website-source-code
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
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: u34fcet9a011tp
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20250215004115_InitialCreate	9.0.2
20250220042255_AddShortTitle	9.0.2
\.


--
-- PostgreSQL database dump complete
--

