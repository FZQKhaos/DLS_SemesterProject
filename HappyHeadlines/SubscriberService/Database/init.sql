CREATE TABLE IF NOT EXISTS subscribers (
    id                TEXT PRIMARY KEY,
    email             VARCHAR(320) NOT NULL UNIQUE,
    subscribed_at_utc TIMESTAMPTZ  NOT NULL
);
