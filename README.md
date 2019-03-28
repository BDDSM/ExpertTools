# ExpertTools
The main purpose of this toolkit is to help 1C experts in detection of the 1C applications problem places.
Project is in the development stage but need expand it, I wait your review. 

<h2>Queries analyze:</h5>
Application collects the tech log data and extended events data, creates MD5 hash for each request statement and inserts theese records into it`s own database. Table with the tech log data contains first and last code lines for each request statement. Extended events data contains performance indicators for each reques statement (such as cpu_time, duration, logical_reads, phisycal_reads and writes). You can get ordered request list by any performance indicator and for each request to get first and last context lines by statement hash, get query plan by plan_handle value.

 For example, to get top statements by duration:
```sql
SELECT 
    [id]
    ,[sql]
    ,[normalized_sql]
    ,[duration]
    ,[physical_reads]
    ,[logical_reads]
    ,[writes]
    ,[cpu_time]
    ,[plan_handle]
    ,[_hash]
FROM [ExpertTools].[dbo].[QueriesAnalyzeSqlQueries]
ORDER BY duration DESC
```
to get context lines by statement hash:
```sql
SELECT 
	[id]
    ,[_Period]
    ,[_user]
    ,[connectId]
    ,[clientId]
    ,[sql]
    ,[normalized_sql]
    ,[context_first_line]
    ,[context_last_line]
    ,[context_exists]
    ,[_hash]
FROM [ExpertTools].[dbo].[QueriesAnalyzeTlQueries]
WHERE _hash = '7f912471b75499e7134c48fb348ebd13'
```
to get query plan by plan_handle:
```sql
SELECT 
	* 
FROM sys.dm_exec_query_plan(0x060007001DA57E04307D99330700000001000000000000000000000000000000000000000000000000000000)
```
