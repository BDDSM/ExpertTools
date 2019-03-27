INSERT INTO QueriesAnalyzeAvgSqlQueries
SELECT
	SUM(duration) / COUNT(_hash),
	SUM(physical_reads) / COUNT(_hash),
	SUM(logical_reads) / COUNT(_hash),
	SUM(writes) / COUNT(_hash),
	SUM(cpu_time) / COUNT(_hash),
	_hash
FROM QueriesAnalyzeSqlQueries
GROUP BY
	_hash