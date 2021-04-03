create table if not exists channel
(
    id          text primary key,
    game        text      not null,
    viewers     int       not null,
    chatters    int       not null,
    shared      int       not null,
    last_update timestamp not null
);

create table if not exists overlap
(
    id        text references channel (id) on update cascade,
    timestamp timestamp not null,
    data      jsonb     not null,
    primary key (id, timestamp)
);

create unique index if not exists overlap_id_timestamp_uindex
	on overlap (id asc, timestamp desc);