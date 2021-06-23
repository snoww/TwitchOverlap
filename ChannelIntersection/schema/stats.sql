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
) with (fillfactor = 95);


create table if not exists overlap
(
    timestamp timestamp not null,
    channel   int references channel (id),
    shared    jsonb,
    primary key (timestamp, channel)
);

create table if not exists chatters_daily
(
    date     date primary key,
    chatters json not null
);

create table if not exists overlap_daily
(
    date    date not null,
    channel int references channel (id),
    channel_total_unique int not null,
    channel_total_overlap int not null,
    shared  jsonb,
    primary key (date, channel)
);

create table if not exists overlap_rolling_3_days
(
    date    date not null,
    channel int references channel (id),
    channel_total_unique int not null,
    channel_total_overlap int not null,
    shared  jsonb,
    primary key (date, channel)
    );

create table if not exists overlap_rolling_7_days
(
    date    date not null,
    channel int references channel (id),
    channel_total_unique int not null,
    channel_total_overlap int not null,
    shared  jsonb,
    primary key (date, channel)
);

create table if not exists overlap_rolling_14_days
(
    date    date not null,
    channel int references channel (id),
    channel_total_unique int not null,
    channel_total_overlap int not null,
    shared  jsonb,
    primary key (date, channel)
);

create table if not exists overlap_rolling_30_days
(
    date    date not null,
    channel int references channel (id),
    channel_total_unique int not null,
    channel_total_overlap int not null,
    shared  jsonb,
    primary key (date, channel)
);