create table nodes(
    id text primary key,
    size int not null
);

create table edges(
    source text not null,
    target text not null,
    weight int not null,
    primary key (source, target)
);