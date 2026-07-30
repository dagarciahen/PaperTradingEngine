CREATE DATABASE dbmain;
\c dbmain

CREATE ROLE admin;
CREATE ROLE daniel WITH LOGIN PASSWORD 'password';

GRANT admin TO daniel;


CREATE SCHEMA pte AUTHORIZATION daniel;



CREATE TABLE pte.orders(
    OrderId SERIAL PRIMARY KEY,
    UserId INTEGER, 
    Symbol varchar(10),
    Type SMALLINT,
    Quantity numeric(18,4),
    LimitPrice numeric(18,4),
    Status smallint not null default 0,
    CreatedAtUtc timestamptz,
    ExecutedAtUtc timestamptz
);

CREATE TABLE pte.executions(
    ExecutionId SERIAL PRIMARY KEY,
    OrderId INTEGER NOT NULL,
    ExecutedPrice numeric(18,4) NOT NULL,
    Quantity INTEGER NOT NULL,
    ExecutedAtUtc timestamptz NOT NULL,

    CONSTRAINT fk_executions_orders
    FOREIGN KEY (OrderId)
    REFERENCES pte.orders (OrderId)
);

CREATE TABLE pte.processed_messages(
    MessageId TEXT PRIMARY KEY,
    ProcessedAtUtc timestamptz NOT nULL
    
);

create index idx_orders_status ON pte.orders(Status);
create index idx_orders_symbol ON pte.orders(Symbol);
create index idx_orders_created ON pte.orders(CreatedAtUtc);