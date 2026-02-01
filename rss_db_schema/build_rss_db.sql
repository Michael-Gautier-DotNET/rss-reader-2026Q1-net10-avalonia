CREATE TABLE IF NOT EXISTS feeds_articles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
	feed_name TEXT NOT NULL,
	headline_text TEXT NOT NULL,
	article_summary TEXT,
	article_text TEXT,
	article_date TEXT,
	article_url TEXT NOT NULL,
	row_insert_date_time TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS feeds (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
	feed_name TEXT,
	feed_url TEXT,
	last_retrieved TEXT,
	retrieve_limit_hrs TEXT,
	retention_days TEXT,
	row_insert_date_time TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS index_feeds_articles_feed_name ON feeds_articles (
	feed_name ASC
);

CREATE UNIQUE INDEX IF NOT EXISTS index_feeds_articles_feed_name_headline_text ON feeds_articles (
	feed_name ASC,
	headline_text ASC
);

CREATE UNIQUE INDEX IF NOT EXISTS index_feeds_feed_name_feed_url ON feeds (
	feed_name ASC,
	feed_url ASC
);

CREATE UNIQUE INDEX IF NOT EXISTS index_feeds_feed_url ON feeds (
	feed_url ASC
);

CREATE UNIQUE INDEX IF NOT EXISTS index_feeds_feed_name ON feeds (
	feed_name ASC
);