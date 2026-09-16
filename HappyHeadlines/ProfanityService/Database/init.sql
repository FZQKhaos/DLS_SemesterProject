CREATE TABLE IF NOT EXISTS profanity_words (
    id SERIAL PRIMARY KEY,
    word VARCHAR(255) NOT NULL UNIQUE
);

INSERT INTO profanity_words (word) VALUES
    ('badword'),
    ('verybadword'),
    ('damn'),
    ('crap')
ON CONFLICT (word) DO NOTHING;
