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
) with (fillfactor=95);


create table if not exists overlap
(
    timestamp timestamp not null,
    channel   int references channel (id),
    shared    jsonb,
    primary key (timestamp, channel)
);