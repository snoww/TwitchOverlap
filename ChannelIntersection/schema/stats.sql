create table if not exists channel
(
    id           serial primary key,
    login_name   text not null unique,
    display_name text not null unique,
    avatar       text,
    game         text,
    viewers      int,
    chatters     int,
    shared       int,
    last_update  timestamp
);

create table if not exists overlap
(
    timestamp timestamp not null,
    source    int references channel (id),
    target    int references channel (id),
    overlap   int       not null,
    primary key (timestamp, source, target)
);


create index if not exists channel_timestamp_index on channel (last_update desc);

create index if not exists overlap_timestamp_desc_index on overlap (timestamp desc);
create index if not exists overlap_source_overlap_index on overlap (source, overlap desc);
create index if not exists overlap_target_overlap_index on overlap (target, overlap desc);


---- new 

create table if not exists overlap
(
    timestamp timestamp not null,
    channel   int references channel (id),
    shared    jsonb,
    primary key (timestamp, channel)
);