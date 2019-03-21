USE [DATABASE_NAME];
INSERT INTO QueriesAnalyzeAvgSqlQueries
SELECT
	SUM(duration) AS duration,
	SUM(physical_reads) AS physical_reads,
	SUM(logical_reads) AS logical_reads,
	SUM(writes) AS writes,
	SUM(cpu_time) AS cpu_time,
	_hash AS _hash
FROM QueriesAnalyzeSqlQueries
GROUP BY
	_hash;