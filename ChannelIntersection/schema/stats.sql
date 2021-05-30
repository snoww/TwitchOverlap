create table if not exists channel
(
    id           text primary key,
    display_name text unique,
    avatar       text unique,
    game         text      not null,
    viewers      int       not null,
    chatters     int       not null,
    shared       int       not null,
    last_update  timestamp not null
);

-- create table if not exists overlap
-- (
--     id        text references channel (id) on update cascade,
--     timestamp timestamp not null,
--     data      jsonb     not null,
--     primary key (id, timestamp)
-- );
--
-- create unique index if not exists overlap_id_timestamp_uindex
--     on overlap (id asc, timestamp desc);

create table if not exists overlap
(
    timestamp timestamp not null,
    source    text references channel (id) on update cascade,
    target    text references channel (id) on update cascade,
    overlap   int       not null,
    primary key (timestamp, source, target)
);

create index if not exists overlap_timestamp_desc_index on overlap (timestamp desc);
create index if not exists overlap_source_overlap_index on overlap (source, overlap desc);
create index if not exists overlap_target_overlap_index on overlap (target, overlap desc);
