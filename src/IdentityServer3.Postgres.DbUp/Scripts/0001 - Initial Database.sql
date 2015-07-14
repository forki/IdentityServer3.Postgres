-- Clients
CREATE TABLE clients
(
  client_id character varying(255) NOT NULL,
  model jsonb NOT NULL,
  CONSTRAINT pk_clients_clientid PRIMARY KEY (client_id)
)
WITH (
  OIDS=FALSE
);

-- Consents
CREATE TABLE consents
(
  subject character varying(255) NOT NULL,
  client_id character varying(255) NOT NULL,
  scopes character varying(2000) NOT NULL,
  CONSTRAINT pk_consents_subject_client PRIMARY KEY (subject, client_id)
)
WITH (
  OIDS=FALSE
);

-- Scopes
CREATE TABLE scopes
(
  name text NOT NULL,
  is_public boolean NOT NULL,
  model jsonb NOT NULL,
  CONSTRAINT pk_scopes_name PRIMARY KEY (name)
)
WITH (
  OIDS=FALSE
);

-- Tokens
CREATE TABLE tokens
(
  key text NOT NULL,
  token_type smallint NOT NULL,
  subject character varying(255) NOT NULL,
  client character varying(255) NOT NULL,
  expiry timestamp with time zone NOT NULL,
  model jsonb NOT NULL,
  CONSTRAINT pk_tokens_key_tokentype PRIMARY KEY (token_type, key)
)
WITH (
  OIDS=FALSE
);

CREATE INDEX ix_tokens_subject_client_tokentype
  ON tokens
  USING btree
  (subject COLLATE pg_catalog."default", client COLLATE pg_catalog."default", token_type);

