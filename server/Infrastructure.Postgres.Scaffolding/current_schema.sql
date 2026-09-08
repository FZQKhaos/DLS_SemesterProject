-- This schema is generated based on the current DBContext. Please check the class Seeder to see.
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'chat') THEN
        CREATE SCHEMA chat;
    END IF;
END $EF$;


CREATE TABLE chat."group" (
    id text NOT NULL,
    CONSTRAINT group_pkay PRIMARY KEY (id)
);


CREATE TABLE chat."user" (
    id text NOT NULL,
    email text NOT NULL,
    hash text NOT NULL,
    salt text NOT NULL,
    role text NOT NULL,
    CONSTRAINT user_pkey PRIMARY KEY (id)
);


CREATE TABLE chat.groupmember (
    groupid text NOT NULL,
    userid text NOT NULL,
    CONSTRAINT groupmember_pk PRIMARY KEY (groupid, userid),
    CONSTRAINT groupmember_group_fk FOREIGN KEY (groupid) REFERENCES chat."group" (id),
    CONSTRAINT groupmember_user_fk FOREIGN KEY (userid) REFERENCES chat."user" (id)
);


CREATE TABLE chat.message (
    messagetext text NOT NULL,
    id text,
    userid text NOT NULL,
    groupid text NOT NULL,
    timestamp timestamp with time zone NOT NULL,
    CONSTRAINT message_group_id_fk FOREIGN KEY (groupid) REFERENCES chat."group" (id),
    CONSTRAINT message_user_id_fk FOREIGN KEY (userid) REFERENCES chat."user" (id)
);


CREATE INDEX "IX_groupmember_userid" ON chat.groupmember (userid);


CREATE INDEX "IX_message_groupid" ON chat.message (groupid);


CREATE INDEX "IX_message_userid" ON chat.message (userid);


