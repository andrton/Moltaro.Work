-- Moltaro.Work schema version 1, MySQL 8.4 or later, InnoDB. Times are UTC.
-- The meaning of every table and column is in docs/storage.md. The tables are created in the connection's database,
-- its default schema; the application applies this script once, with its own migration tool.

CREATE TABLE moltaro_work_schema (
    version int NOT NULL
) ENGINE = InnoDB;

INSERT INTO moltaro_work_schema (version) VALUES (1);

-- state: 1 waiting, 2 held, 3 finished, 4 dead.
-- MySQL has no partial index. The unique index over the nullable active_key admits any number of NULL, so it is one
-- item per active key, the one mechanism of coalescing and of the keys of logs and runs. The waiting and held indexes
-- lead with state; the ended one needs none, because the checks set finished_at exactly for ended items.
CREATE TABLE moltaro_work_item (
    id           bigint       NOT NULL AUTO_INCREMENT,
    queue        varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    state        tinyint      NOT NULL,
    item_key     varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    active_key   varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    scope        varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    payload      longtext     CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    due_at       datetime(6)  NOT NULL,
    again_due_at datetime(6)  NULL,
    lease_until  datetime(6)  NULL,
    fence        bigint       NOT NULL DEFAULT 0,
    attempts     int          NOT NULL DEFAULT 0,
    enqueued_at  datetime(6)  NOT NULL,
    claimed_at   datetime(6)  NULL,
    finished_at  datetime(6)  NULL,
    reason       text         CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NULL,
    CONSTRAINT moltaro_work_item_pk PRIMARY KEY (id),
    CONSTRAINT moltaro_work_item_state CHECK (state BETWEEN 1 AND 4),
    CONSTRAINT moltaro_work_item_lease CHECK ((state = 2) = (lease_until IS NOT NULL)),
    CONSTRAINT moltaro_work_item_finished CHECK ((state >= 3) = (finished_at IS NOT NULL)),
    UNIQUE INDEX moltaro_work_item_active_key (queue, active_key),
    INDEX moltaro_work_item_waiting (queue, state, due_at, id),
    INDEX moltaro_work_item_held (queue, state, lease_until),
    INDEX moltaro_work_item_ended (queue, finished_at)
) ENGINE = InnoDB;

-- A row exists exactly while an item of its scope is held: its primary key is the scope rule.
CREATE TABLE moltaro_work_scope (
    queue   varchar(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    scope   varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NOT NULL,
    item_id bigint       NOT NULL,
    CONSTRAINT moltaro_work_scope_pk PRIMARY KEY (queue, scope)
) ENGINE = InnoDB;
