-- Moltaro.Work schema version 1, PostgreSQL 15 or later.
-- The meaning of every table and column is in docs/storage.md. The tables are created in the connection's default
-- schema; the application applies this script once, with its own migration tool.

CREATE TABLE moltaro_work_schema (
    version integer NOT NULL,
    CONSTRAINT moltaro_work_schema_pk PRIMARY KEY (version)
);

INSERT INTO moltaro_work_schema (version) VALUES (1);

-- state: 1 waiting, 2 held, 3 finished, 4 dead.
CREATE TABLE moltaro_work_item (
    id           bigint       GENERATED ALWAYS AS IDENTITY,
    queue        varchar(100) COLLATE "C" NOT NULL,
    state        smallint     NOT NULL,
    item_key     varchar(200) COLLATE "C",
    active_key   varchar(200) COLLATE "C",
    scope        varchar(200) COLLATE "C",
    payload      text,
    due_at       timestamptz  NOT NULL,
    again_due_at timestamptz,
    lease_until  timestamptz,
    fence        bigint       NOT NULL DEFAULT 0,
    attempts     integer      NOT NULL DEFAULT 0,
    enqueued_at  timestamptz  NOT NULL,
    claimed_at   timestamptz,
    finished_at  timestamptz,
    reason       text,
    CONSTRAINT moltaro_work_item_pk PRIMARY KEY (id),
    CONSTRAINT moltaro_work_item_state CHECK (state BETWEEN 1 AND 4),
    CONSTRAINT moltaro_work_item_lease CHECK ((state = 2) = (lease_until IS NOT NULL)),
    CONSTRAINT moltaro_work_item_finished CHECK ((state >= 3) = (finished_at IS NOT NULL))
);

-- One item per active key: the one mechanism of coalescing and of the keys of logs and runs.
CREATE UNIQUE INDEX moltaro_work_item_active_key ON moltaro_work_item (queue, active_key)
    WHERE active_key IS NOT NULL;
CREATE INDEX moltaro_work_item_waiting ON moltaro_work_item (queue, due_at, id) WHERE state = 1;
CREATE INDEX moltaro_work_item_held ON moltaro_work_item (queue, lease_until) WHERE state = 2;
CREATE INDEX moltaro_work_item_ended ON moltaro_work_item (queue, finished_at) WHERE state IN (3, 4);

-- A row exists exactly while an item of its scope is held: its primary key is the scope rule.
CREATE TABLE moltaro_work_scope (
    queue   varchar(100) COLLATE "C" NOT NULL,
    scope   varchar(200) COLLATE "C" NOT NULL,
    item_id bigint       NOT NULL,
    CONSTRAINT moltaro_work_scope_pk PRIMARY KEY (queue, scope)
);
