USE ExpertTools;

--Tables

CREATE TABLE QueriesAnalyzeTlQueries
(
	id INT IDENTITY NOT NULL,
	_Period DATETIME2 not null,
	_user NVARCHAR(200) NOT NULL,
	connectId NVARCHAR(20) NOT NULL,
	clientId NVARCHAR(20) NOT NULL,
	sql NVARCHAR(MAX) NOT NULL,
	normalized_sql NVARCHAR(MAX) NOT NULL,
	context_first_line NVARCHAR(400) NOT NULL,
	context_last_line NVARCHAR(400) NOT NULL,
	context_exists BIT NOT NULL,
	_hash NVARCHAR(32) NOT NULL,
	CONSTRAINT PK_QueriesAnalyzeTlQueries PRIMARY KEY(id)
);

CREATE TABLE QueriesAnalyzeTlContexts
(
	id INT IDENTITY NOT NULL,
	_Period DATETIME2 NOT NULL,
	_user NVARCHAR(200) NOT NULL,
	connectId NVARCHAR(20) NOT NULL,
	clientId NVARCHAR(20) NOT NULL,
	context_first_line NVARCHAR(400) NOT NULL,
	context_last_line NVARCHAR(400) NOT NULL,
	CONSTRAINT PK_QueriesAnalyzeTlContexts PRIMARY KEY(id)
);

CREATE TABLE QueriesAnalyzeSqlQueries
(
	id INT IDENTITY(1,1) NOT NULL,
	sql NVARCHAR(MAX) NOT NULL,
	normalized_sql NVARCHAR(MAX) NOT NULL,
	duration BIGINT NOT NULL,
	physical_reads BIGINT NOT NULL,
	logical_reads BIGINT NOT NULL,
	writes BIGINT NOT NULL,
	cpu_time BIGINT NOT NULL,
	plan_handle VARBINARY(64) NOT NULL,
	_hash NVARCHAR(32) NOT NULL,
	CONSTRAINT PK_QueriesAnalyzeSqlQueries PRIMARY KEY(id)
);

CREATE TABLE QueriesAnalyzeAvgSqlQueries
(
	id INT IDENTITY(1,1) NOT NULL,
	duration BIGINT NOT NULL,
	physical_reads BIGINT NOT NULL,
	logical_reads BIGINT NOT NULL,
	writes BIGINT NOT NULL,
	cpu_time BIGINT NOT NULL,
	_hash NVARCHAR(32) NOT NULL,
	CONSTRAINT PK_QueriesAnalyzeAvgSqlQueries PRIMARY KEY(id)
);

--Indexes
CREATE NONCLUSTERED INDEX by_hash 
	ON QueriesAnalyzeTlQueries(_hash) 
		INCLUDE(context_last_line, context_first_line, sql);

CREATE NONCLUSTERED INDEX for_join_context 
	ON QueriesAnalyzeTlQueries(context_exists, connectId, clientId, _Period) 
		INCLUDE(id);

CREATE NONCLUSTERED INDEX for_join_context
	ON QueriesAnalyzeTlContexts(connectId, clientId, _Period)
		INCLUDE(context_first_line, context_last_line, id);

CREATE NONCLUSTERED INDEX by_hash 
	ON QueriesAnalyzeSqlQueries(_hash)
		INCLUDE(sql, normalized_sql, duration, physical_reads, logical_reads, writes, cpu_time);

CREATE NONCLUSTERED INDEX by_hash 
	ON QueriesAnalyzeAvgSqlQueries(_hash)
		INCLUDE(duration, physical_reads, logical_reads, writes, cpu_time);