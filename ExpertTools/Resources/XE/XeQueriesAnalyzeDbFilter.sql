CREATE EVENT SESSION [SESSION_NAME] ON SERVER
ADD EVENT sqlserver.rpc_completed(
	WHERE([sqlserver.database_name]=N'[DATABASE_NAME_FILTER]')),
ADD EVENT sqlserver.sql_batch_completed(
	WHERE([sqlserver.database_name]=N'[DATABASE_NAME_FILTER]'))
ADD TARGET package0.event_file(SET filename = N'[EVENT_FILE_PATH]',max_file_size=(10240))
WITH (MAX_DISPATCH_LATENCY=4 SECONDS)