CREATE TABLE IF NOT EXISTS articles (
    id TEXT PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    body TEXT NOT NULL,
    continent VARCHAR(100) NOT NULL,
    published_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE articles
    ADD COLUMN IF NOT EXISTS published_at TIMESTAMPTZ;

-- Existing rows created by the old schema will have no publication time. Defaulting
-- them to NOW keeps the table valid for later 14-day queries, while preserving the
-- actual publish time for newly published articles.
UPDATE articles
SET published_at = NOW()
WHERE published_at IS NULL;

ALTER TABLE articles
    ALTER COLUMN published_at SET DEFAULT NOW();

ALTER TABLE articles
    ALTER COLUMN published_at SET NOT NULL;