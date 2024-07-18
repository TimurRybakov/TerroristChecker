-- Table: public.terrorists
CREATE TABLE IF NOT EXISTS public.terrorists
(
    id integer NOT NULL,
    full_name character varying(255) COLLATE pg_catalog."default" NOT NULL,
    birthday date,
    passport character varying(254) COLLATE pg_catalog."default",
    CONSTRAINT pk_terrorists_id PRIMARY KEY (id)
)

TABLESPACE pg_default;

COPY public.terrorists FROM '/docker-entrypoint-initdb.d/terrorists.csv' CSV;