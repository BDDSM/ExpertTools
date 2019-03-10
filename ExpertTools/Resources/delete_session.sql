IF EXISTS(SELECT * FROM sys.server_event_sessions WHERE name = '[SESSION_NAME]')
	DROP EVENT session [SESSION_NAME] ON SERVER;