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
-- Name: _heroku; Type: SCHEMA; Schema: -; Owner: heroku_admin
--

CREATE SCHEMA _heroku;


ALTER SCHEMA _heroku OWNER TO heroku_admin;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: u34fcet9a011tp
--

-- *not* creating schema, since initdb creates it


ALTER SCHEMA public OWNER TO u34fcet9a011tp;

--
-- Name: pg_stat_statements; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pg_stat_statements WITH SCHEMA public;


--
-- Name: EXTENSION pg_stat_statements; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pg_stat_statements IS 'track planning and execution statistics of all SQL statements executed';


--
-- Name: create_ext(); Type: FUNCTION; Schema: _heroku; Owner: heroku_admin
--

CREATE FUNCTION _heroku.create_ext() RETURNS event_trigger
    LANGUAGE plpgsql SECURITY DEFINER
    AS $$

DECLARE

  schemaname TEXT;
  databaseowner TEXT;

  r RECORD;

BEGIN

  IF tg_tag = 'CREATE EXTENSION' and current_user != 'rds_superuser' THEN
    FOR r IN SELECT * FROM pg_event_trigger_ddl_commands()
    LOOP
        CONTINUE WHEN r.command_tag != 'CREATE EXTENSION' OR r.object_type != 'extension';

        schemaname = (
            SELECT n.nspname
            FROM pg_catalog.pg_extension AS e
            INNER JOIN pg_catalog.pg_namespace AS n
            ON e.extnamespace = n.oid
            WHERE e.oid = r.objid
        );

        databaseowner = (
            SELECT pg_catalog.pg_get_userbyid(d.datdba)
            FROM pg_catalog.pg_database d
            WHERE d.datname = current_database()
        );
        --RAISE NOTICE 'Record for event trigger %, objid: %,tag: %, current_user: %, schema: %, database_owenr: %', r.object_identity, r.objid, tg_tag, current_user, schemaname, databaseowner;
        IF r.object_identity = 'address_standardizer_data_us' THEN
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT, UPDATE, INSERT, DELETE', databaseowner, 'us_gaz');
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT, UPDATE, INSERT, DELETE', databaseowner, 'us_lex');
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT, UPDATE, INSERT, DELETE', databaseowner, 'us_rules');
        ELSIF r.object_identity = 'amcheck' THEN
            EXECUTE format('GRANT EXECUTE ON FUNCTION %I.bt_index_check TO %I;', schemaname, databaseowner);
            EXECUTE format('GRANT EXECUTE ON FUNCTION %I.bt_index_parent_check TO %I;', schemaname, databaseowner);
        ELSIF r.object_identity = 'dict_int' THEN
            EXECUTE format('ALTER TEXT SEARCH DICTIONARY %I.intdict OWNER TO %I;', schemaname, databaseowner);
        ELSIF r.object_identity = 'pg_partman' THEN
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT, UPDATE, INSERT, DELETE', databaseowner, 'part_config');
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT, UPDATE, INSERT, DELETE', databaseowner, 'part_config_sub');
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT, UPDATE, INSERT, DELETE', databaseowner, 'custom_time_partitions');
        ELSIF r.object_identity = 'pg_stat_statements' THEN
            EXECUTE format('GRANT EXECUTE ON FUNCTION %I.pg_stat_statements_reset TO %I;', schemaname, databaseowner);
        ELSIF r.object_identity = 'postgis' THEN
            PERFORM _heroku.postgis_after_create();
        ELSIF r.object_identity = 'postgis_raster' THEN
            PERFORM _heroku.postgis_after_create();
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT', databaseowner, 'raster_columns');
            PERFORM _heroku.grant_table_if_exists(schemaname, 'SELECT', databaseowner, 'raster_overviews');
        ELSIF r.object_identity = 'postgis_topology' THEN
            PERFORM _heroku.postgis_after_create();
            EXECUTE format('GRANT USAGE ON SCHEMA topology TO %I;', databaseowner);
            EXECUTE format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA topology TO %I;', databaseowner);
            PERFORM _heroku.grant_table_if_exists('topology', 'SELECT, UPDATE, INSERT, DELETE', databaseowner);
            EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA topology TO %I;', databaseowner);
        ELSIF r.object_identity = 'postgis_tiger_geocoder' THEN
            PERFORM _heroku.postgis_after_create();
            EXECUTE format('GRANT USAGE ON SCHEMA tiger TO %I;', databaseowner);
            EXECUTE format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA tiger TO %I;', databaseowner);
            PERFORM _heroku.grant_table_if_exists('tiger', 'SELECT, UPDATE, INSERT, DELETE', databaseowner);

            EXECUTE format('GRANT USAGE ON SCHEMA tiger_data TO %I;', databaseowner);
            EXECUTE format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA tiger_data TO %I;', databaseowner);
            PERFORM _heroku.grant_table_if_exists('tiger_data', 'SELECT, UPDATE, INSERT, DELETE', databaseowner);
        END IF;
    END LOOP;
  END IF;
END;
$$;


ALTER FUNCTION _heroku.create_ext() OWNER TO heroku_admin;

--
-- Name: drop_ext(); Type: FUNCTION; Schema: _heroku; Owner: heroku_admin
--

CREATE FUNCTION _heroku.drop_ext() RETURNS event_trigger
    LANGUAGE plpgsql SECURITY DEFINER
    AS $$

DECLARE

  schemaname TEXT;
  databaseowner TEXT;

  r RECORD;

BEGIN

  IF tg_tag = 'DROP EXTENSION' and current_user != 'rds_superuser' THEN
    FOR r IN SELECT * FROM pg_event_trigger_dropped_objects()
    LOOP
      CONTINUE WHEN r.object_type != 'extension';

      databaseowner = (
            SELECT pg_catalog.pg_get_userbyid(d.datdba)
            FROM pg_catalog.pg_database d
            WHERE d.datname = current_database()
      );

      --RAISE NOTICE 'Record for event trigger %, objid: %,tag: %, current_user: %, database_owner: %, schemaname: %', r.object_identity, r.objid, tg_tag, current_user, databaseowner, r.schema_name;

      IF r.object_identity = 'postgis_topology' THEN
          EXECUTE format('DROP SCHEMA IF EXISTS topology');
      END IF;
    END LOOP;

  END IF;
END;
$$;


ALTER FUNCTION _heroku.drop_ext() OWNER TO heroku_admin;

--
-- Name: extension_before_drop(); Type: FUNCTION; Schema: _heroku; Owner: heroku_admin
--

CREATE FUNCTION _heroku.extension_before_drop() RETURNS event_trigger
    LANGUAGE plpgsql SECURITY DEFINER
    AS $$

DECLARE

  query TEXT;

BEGIN
  query = (SELECT current_query());

  -- RAISE NOTICE 'executing extension_before_drop: tg_event: %, tg_tag: %, current_user: %, session_user: %, query: %', tg_event, tg_tag, current_user, session_user, query;
  IF tg_tag = 'DROP EXTENSION' and not pg_has_role(session_user, 'rds_superuser', 'MEMBER') THEN
    -- DROP EXTENSION [ IF EXISTS ] name [, ...] [ CASCADE | RESTRICT ]
    IF (regexp_match(query, 'DROP\s+EXTENSION\s+(IF\s+EXISTS)?.*(plpgsql)', 'i') IS NOT NULL) THEN
      RAISE EXCEPTION 'The plpgsql extension is required for database management and cannot be dropped.';
    END IF;
  END IF;
END;
$$;


ALTER FUNCTION _heroku.extension_before_drop() OWNER TO heroku_admin;

--
-- Name: grant_table_if_exists(text, text, text, text); Type: FUNCTION; Schema: _heroku; Owner: heroku_admin
--

CREATE FUNCTION _heroku.grant_table_if_exists(alias_schemaname text, grants text, databaseowner text, alias_tablename text DEFAULT NULL::text) RETURNS void
    LANGUAGE plpgsql SECURITY DEFINER
    AS $$

BEGIN

  IF alias_tablename IS NULL THEN
    EXECUTE format('GRANT %s ON ALL TABLES IN SCHEMA %I TO %I;', grants, alias_schemaname, databaseowner);
  ELSE
    IF EXISTS (SELECT 1 FROM pg_tables WHERE pg_tables.schemaname = alias_schemaname AND pg_tables.tablename = alias_tablename) THEN
      EXECUTE format('GRANT %s ON TABLE %I.%I TO %I;', grants, alias_schemaname, alias_tablename, databaseowner);
    END IF;
  END IF;
END;
$$;


ALTER FUNCTION _heroku.grant_table_if_exists(alias_schemaname text, grants text, databaseowner text, alias_tablename text) OWNER TO heroku_admin;

--
-- Name: postgis_after_create(); Type: FUNCTION; Schema: _heroku; Owner: heroku_admin
--

CREATE FUNCTION _heroku.postgis_after_create() RETURNS void
    LANGUAGE plpgsql SECURITY DEFINER
    AS $$
DECLARE
    schemaname TEXT;
    databaseowner TEXT;
BEGIN
    schemaname = (
        SELECT n.nspname
        FROM pg_catalog.pg_extension AS e
        INNER JOIN pg_catalog.pg_namespace AS n ON e.extnamespace = n.oid
        WHERE e.extname = 'postgis'
    );
    databaseowner = (
        SELECT pg_catalog.pg_get_userbyid(d.datdba)
        FROM pg_catalog.pg_database d
        WHERE d.datname = current_database()
    );

    EXECUTE format('GRANT EXECUTE ON FUNCTION %I.st_tileenvelope TO %I;', schemaname, databaseowner);
    EXECUTE format('GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE %I.spatial_ref_sys TO %I;', schemaname, databaseowner);
END;
$$;


ALTER FUNCTION _heroku.postgis_after_create() OWNER TO heroku_admin;

--
-- Name: validate_extension(); Type: FUNCTION; Schema: _heroku; Owner: heroku_admin
--

CREATE FUNCTION _heroku.validate_extension() RETURNS event_trigger
    LANGUAGE plpgsql SECURITY DEFINER
    AS $$

DECLARE

  schemaname TEXT;
  r RECORD;

BEGIN

  IF tg_tag = 'CREATE EXTENSION' and current_user != 'rds_superuser' THEN
    FOR r IN SELECT * FROM pg_event_trigger_ddl_commands()
    LOOP
      CONTINUE WHEN r.command_tag != 'CREATE EXTENSION' OR r.object_type != 'extension';

      schemaname = (
        SELECT n.nspname
        FROM pg_catalog.pg_extension AS e
        INNER JOIN pg_catalog.pg_namespace AS n
        ON e.extnamespace = n.oid
        WHERE e.oid = r.objid
      );

      IF schemaname = '_heroku' THEN
        RAISE EXCEPTION 'Creating extensions in the _heroku schema is not allowed';
      END IF;
    END LOOP;
  END IF;
END;
$$;


ALTER FUNCTION _heroku.validate_extension() OWNER TO heroku_admin;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: PostTag; Type: TABLE; Schema: public; Owner: u34fcet9a011tp
--

CREATE TABLE public."PostTag" (
    "PostsId" text NOT NULL,
    "TagsId" text NOT NULL
);


ALTER TABLE public."PostTag" OWNER TO u34fcet9a011tp;

--
-- Name: Posts; Type: TABLE; Schema: public; Owner: u34fcet9a011tp
--

CREATE TABLE public."Posts" (
    "Id" text NOT NULL,
    "Title" text NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "Link" text,
    "LinkTitle" text,
    "Contents" text DEFAULT ''::text NOT NULL,
    "ShortTitle" text DEFAULT ''::text NOT NULL
);


ALTER TABLE public."Posts" OWNER TO u34fcet9a011tp;

--
-- Name: Tags; Type: TABLE; Schema: public; Owner: u34fcet9a011tp
--

CREATE TABLE public."Tags" (
    "Id" text NOT NULL,
    "Name" text NOT NULL
);


ALTER TABLE public."Tags" OWNER TO u34fcet9a011tp;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: u34fcet9a011tp
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE public."__EFMigrationsHistory" OWNER TO u34fcet9a011tp;

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
\.


--
-- Data for Name: Posts; Type: TABLE DATA; Schema: public; Owner: u34fcet9a011tp
--

COPY public."Posts" ("Id", "Title", "Date", "Link", "LinkTitle", "Contents", "ShortTitle") FROM stdin;
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
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: u34fcet9a011tp
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20250215004115_InitialCreate	9.0.2
20250220042255_AddShortTitle	9.0.2
\.


--
-- Name: PostTag PK_PostTag; Type: CONSTRAINT; Schema: public; Owner: u34fcet9a011tp
--

ALTER TABLE ONLY public."PostTag"
    ADD CONSTRAINT "PK_PostTag" PRIMARY KEY ("PostsId", "TagsId");


--
-- Name: Posts PK_Posts; Type: CONSTRAINT; Schema: public; Owner: u34fcet9a011tp
--

ALTER TABLE ONLY public."Posts"
    ADD CONSTRAINT "PK_Posts" PRIMARY KEY ("Id");


--
-- Name: Tags PK_Tags; Type: CONSTRAINT; Schema: public; Owner: u34fcet9a011tp
--

ALTER TABLE ONLY public."Tags"
    ADD CONSTRAINT "PK_Tags" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: u34fcet9a011tp
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_PostTag_TagsId; Type: INDEX; Schema: public; Owner: u34fcet9a011tp
--

CREATE INDEX "IX_PostTag_TagsId" ON public."PostTag" USING btree ("TagsId");


--
-- Name: PostTag FK_PostTag_Posts_PostsId; Type: FK CONSTRAINT; Schema: public; Owner: u34fcet9a011tp
--

ALTER TABLE ONLY public."PostTag"
    ADD CONSTRAINT "FK_PostTag_Posts_PostsId" FOREIGN KEY ("PostsId") REFERENCES public."Posts"("Id") ON DELETE CASCADE;


--
-- Name: PostTag FK_PostTag_Tags_TagsId; Type: FK CONSTRAINT; Schema: public; Owner: u34fcet9a011tp
--

ALTER TABLE ONLY public."PostTag"
    ADD CONSTRAINT "FK_PostTag_Tags_TagsId" FOREIGN KEY ("TagsId") REFERENCES public."Tags"("Id") ON DELETE CASCADE;


--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: u34fcet9a011tp
--

REVOKE USAGE ON SCHEMA public FROM PUBLIC;


--
-- Name: FUNCTION pg_stat_statements_reset(userid oid, dbid oid, queryid bigint); Type: ACL; Schema: public; Owner: rdsadmin
--

GRANT ALL ON FUNCTION public.pg_stat_statements_reset(userid oid, dbid oid, queryid bigint) TO u34fcet9a011tp;


--
-- Name: extension_before_drop; Type: EVENT TRIGGER; Schema: -; Owner: heroku_admin
--

CREATE EVENT TRIGGER extension_before_drop ON ddl_command_start
   EXECUTE FUNCTION _heroku.extension_before_drop();


ALTER EVENT TRIGGER extension_before_drop OWNER TO heroku_admin;

--
-- Name: log_create_ext; Type: EVENT TRIGGER; Schema: -; Owner: heroku_admin
--

CREATE EVENT TRIGGER log_create_ext ON ddl_command_end
   EXECUTE FUNCTION _heroku.create_ext();


ALTER EVENT TRIGGER log_create_ext OWNER TO heroku_admin;

--
-- Name: log_drop_ext; Type: EVENT TRIGGER; Schema: -; Owner: heroku_admin
--

CREATE EVENT TRIGGER log_drop_ext ON sql_drop
   EXECUTE FUNCTION _heroku.drop_ext();


ALTER EVENT TRIGGER log_drop_ext OWNER TO heroku_admin;

--
-- Name: validate_extension; Type: EVENT TRIGGER; Schema: -; Owner: heroku_admin
--

CREATE EVENT TRIGGER validate_extension ON ddl_command_end
   EXECUTE FUNCTION _heroku.validate_extension();


ALTER EVENT TRIGGER validate_extension OWNER TO heroku_admin;

--
-- PostgreSQL database dump complete
--

