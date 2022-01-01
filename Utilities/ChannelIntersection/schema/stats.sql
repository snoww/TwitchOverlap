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

create table if not exists channel_history
(
    timestamp timestamp not null,
    id        int references channel (id),
    viewers   int,
    chatters  int,
    shared    int,
    primary key (timestamp, id)
);

create table if not exists overlap
(
    timestamp timestamp not null,
    channel   int references channel (id),
    shared    jsonb,
    primary key (timestamp, channel)
);

create table if not exists overlap_daily
(
    date                  date not null,
    channel               int references channel (id),
    channel_total_unique  int  not null,
    channel_total_overlap int  not null,
    shared                jsonb,
    primary key (date, channel)
);

create table if not exists overlap_rolling_3_days
(
    date                  date not null,
    channel               int references channel (id),
    channel_total_unique  int  not null,
    channel_total_overlap int  not null,
    shared                jsonb,
    primary key (date, channel)
);

create table if not exists overlap_rolling_7_days
(
    date                  date not null,
    channel               int references channel (id),
    channel_total_unique  int  not null,
    channel_total_overlap int  not null,
    shared                jsonb,
    primary key (date, channel)
);

create index if not exists channel_last_update_index on channel (last_update desc);
create unique index if not exists overlap_timestamp_desc_channel_uindex on overlap (timestamp desc, channel);
create unique index if not exists chatters_daily_date_desc_username_uindex on chatters (date desc, username);
create unique index if not exists overlap_daily_date_desc_channel_uindex on overlap_daily (date desc, channel);
create unique index if not exists overlap_rolling_3_days_date_desc_channel_uindex on overlap_rolling_3_days (date desc, channel);
create unique index if not exists overlap_rolling_7_days_date_desc_channel_uindex on overlap_rolling_7_days (date desc, channel);
