USE [DATABASE_NAME];

--Таблицы базы данных

CREATE TABLE Tldbmssql
(
	id INT IDENTITY NOT NULL,
	sql NVARCHAR(MAX) NOT NULL,
	normalized_sql NVARCHAR(MAX) NOT NULL,
	_user NVARCHAR(200) NOT NULL,
	context_first_line NVARCHAR(MAX) NOT NULL,
	context_last_line NVARCHAR(MAX) NOT NULL,
	_hash NVARCHAR(32) NOT NULL,
	CONSTRAINT PK_Tldbmssql PRIMARY KEY(id)
);

CREATE TABLE Sqlqueries
(
	id INT IDENTITY(1,1) NOT NULL,
	sql NVARCHAR(MAX) NOT NULL,
	normalized_sql NVARCHAR(MAX) NOT NULL,
	duration BIGINT NOT NULL,
	physical_reads BIGINT NOT NULL,
	logical_reads BIGINT NOT NULL,
	writes BIGINT NOT NULL,
	cpu_time BIGINT NOT NULL,
	_hash NVARCHAR(32) NOT NULL,
	CONSTRAINT PK_Sqlqueries PRIMARY KEY(id)
);

--Индексы таблиц базы данных
CREATE NONCLUSTERED INDEX by_hash 
	ON Tldbmssql(_hash) 
		INCLUDE(context_last_line, context_first_line);

CREATE NONCLUSTERED INDEX by_hash 
	ON Sqlqueries(_hash);