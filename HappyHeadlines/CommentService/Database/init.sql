CREATE TABLE IF NOT EXISTS comments (
    id TEXT PRIMARY KEY,
    article_id TEXT NOT NULL,
    author VARCHAR(100) NOT NULL,
    original_body TEXT NOT NULL,
    body TEXT NOT NULL,
    moderation_status VARCHAR(32) NOT NULL CHECK (moderation_status IN ('pending', 'published')),
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_comments_article_id ON comments(article_id);
CREATE INDEX IF NOT EXISTS idx_comments_status ON comments(moderation_status);
