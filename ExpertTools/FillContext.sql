SELECT
	T1.id,
	MIN(T2._Period) AS context_period
INTO #j1
FROM ExpertTools.dbo.QueriesAnalyzeTlQueries AS T1
	INNER JOIN ExpertTools.dbo.QueriesAnalyzeTlContexts AS T2
		ON T1._Period < T2._Period
		AND T1.clientId = T2.clientId
		AND T1.connectId = T2.connectId
WHERE
	T1.context_exists = 0
GROUP BY
	T1.id;

UPDATE ExpertTools.dbo.QueriesAnalyzeTlQueries
SET
	context_first_line = T1.context_first_line,
	context_last_line = T1.context_last_line,
	context_exists = 1
FROM
	(SELECT
		T1.id,
		T2.context_first_line,
		T2.context_last_line
	FROM #j1 AS T1
		INNER JOIN ExpertTools.dbo.QueriesAnalyzeTlContexts AS T2
			ON T1.context_period = t2._Period) AS T1
WHERE QueriesAnalyzeTlQueries.id = T1.id

DROP TABLE #j1;