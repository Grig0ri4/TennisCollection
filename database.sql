-- 1. Таблица пользователей
create table users (
    id serial primary key,
    login varchar(50) unique, 
    password_hash varchar(255) not null 
);

-- 2. Таблица профилей пользователей
create table user_profiles (
	id serial primary key,
	userid integer references users(id),
	firstname varchar(50),
	lastname varchar(50)
);

-- 3. Таблица профилей пользователей
create table photo_profiles (
	id serial primary key,
    userid integer references users(id), 
    photo bytea NOT NULL 
);

-- 4. Таблица мужских туров
create table male_tours (
    id serial primary key,
    name varchar(10) 
);

-- 5. Таблица женских туров
create table female_tours (
    id serial primary key,
    name varchar(10) 
);

-- 6. Таблица типов турниров (общая)
create table tournament_types (
    id serial primary key,
    name varchar(50) 
);

-- 7. Таблица мужских турниров
create table male_tournaments (
    id serial primary key,
    name varchar(100), 
    type_id integer references tournament_types(id) 
);

-- 8. Таблица женских турниров
create table female_tournaments (
    id serial primary key,
    name varchar(100), 
    type_id integer references tournament_types(id) 
);

-- 9. Таблица мужских игроков
create table male_players (
    id serial primary key,
    full_name varchar(100), 
    country varchar(50) 
);

-- 10. Таблица женских игроков
create table female_players (
    id serial primary key,
    full_name varchar(100), 
    country varchar(50) 
);

-- 11. Таблица мужских финалов
create table male_finals (
    id serial primary key,
    tour_id integer references male_tours(id), 
    year integer,
    tournament_id integer references male_tournaments(id), 
    player1_id integer references male_players(id), 
    player2_id integer references male_players(id),
    winner_id integer references male_players(id), 
    score varchar(50) 
);

-- 12. Таблица женских финалов
create table female_finals (
    id serial primary key,
    tour_id integer references female_tours(id), 
    year integer, 
    tournament_id integer references female_tournaments(id), 
    player1_id integer references female_players(id), 
    player2_id integer references female_players(id), 
    winner_id integer references female_players(id), 
    score varchar(50)
);

-- 13 журнал просмотров
create table user_diary (
    id serial primary key,
    user_id integer references users(id) on delete cascade,
    match_id integer not null, 
    tour_type varchar(3) not null, 
    view_date timestamp default current_timestamp,
    notes text, 
    user_rating integer check (user_rating >= 1 and user_rating <= 10) 
);

-- 14 таблица для "энциклопедии" и внешних ресурсов
create table encyclopedia_articles (
    id serial primary key,
    title varchar(255) not null,
    content text not null,
    category varchar(50), 
    external_url varchar(255) 
);


-- ОБЪЕКТЫ БД

-- Функция получения tour_id по названию
create or replace function get_tour_id(tour_name varchar)
returns integer as $$
begin
    return (
        select id from male_tours where name = tour_name
        union all
        select id from female_tours where name = tour_name
        limit 1
    );
end;
$$ language plpgsql;

-- функция для получения общего количества финалов (ATP/WTA)
create or replace function get_total_finals()
returns integer as $$
    select count(*)::integer from (
        select 1 from male_finals
        union all
        select 1 from female_finals
    ) as subquery;
$$ language sql;

-- процедура добавления финала
create or replace procedure add_final(
    p_tour varchar,
    p_year integer,
    p_tournament_id integer,
    p_player1_id integer,
    p_player2_id integer,
    p_winner_id integer,
    p_score varchar
)
language plpgsql as $$
declare
    tour_id_val integer;
    finals_table text := case when p_tour = 'atp' then 'male_finals' else 'female_finals' end;
begin
    tour_id_val := get_tour_id(p_tour);
   
    execute format('
        insert into %i
        (tour_id, year, tournament_id, player1_id, player2_id, winner_id, score)
        values ($1, $2, $3, $4, $5, $6, $7)', finals_table)
    using tour_id_val, p_year, p_tournament_id, p_player1_id, p_player2_id, p_winner_id, p_score;
end;
$$;

-- процедура удаления финала
create or replace procedure delete_final(p_tour varchar, p_final_id integer)
language plpgsql as $$
declare finals_table text := case when p_tour = 'atp' then 'male_finals' else 'female_finals' end;
begin
    execute format('delete from %i where id = $1', finals_table) using p_final_id;
end;
$$;

-- процедура добавления игрока
create or replace procedure add_player(
    p_tour varchar,
    p_full_name varchar,
    p_country varchar
)
language plpgsql as $$
declare players_table text := case when p_tour = 'atp' then 'male_players' else 'female_players' end;
begin
    execute format('insert into %i (full_name, country) values ($1, $2)', players_table)
    using p_full_name, p_country;
end;
$$;

-- процедура удаления игрока
create or replace procedure delete_player(p_tour varchar, p_player_id integer)
language plpgsql as $$
declare players_table text := case when p_tour = 'atp' then 'male_players' else 'female_players' end;
begin
    execute format('delete from .%i where id = $1', players_table) using p_player_id;
end;
$$;