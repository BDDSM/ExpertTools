[English version of this document](README_EN.md)

# ExpertTools
Главной целью инструмента является помощь экспертам в обнаружении проблемных мест приложений 1С. Проект находится в стадии разработки.

<h2>Queries analyze:</h5>
Приложение собирает данные технологического журнала и данные расширенный событий MSSQL, создает MD5 хэш для каждого запроса и после обработки помещает результат в свою собственную базу данных. Таблица с данными технологического журнала содержит первые и последние строки контекста для каждого запроса. Данные расширенных событий содержат такие показатели как cpu_time, duration, logical_reads, physical_reads, и writes. Вы можете получить отсортированный список по любому из этих показателей и для каждого запроса получить первую и последнюю строки контекста выполнения, план запроса можно получить по значению поля plan_handle. 

Например, получение топа запросов по длительности выполнения:
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
Получение строк контекста по хэшу запроса:
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
Получение плана запроса по значению поля plan_handle:
```sql
SELECT 
	* 
FROM sys.dm_exec_query_plan(0x060007001DA57E04307D99330700000001000000000000000000000000000000000000000000000000000000)
```
