IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'AgentEvents'
)
BEGIN
    CREATE TABLE AgentEvents (
        Id                     INT PRIMARY KEY IDENTITY(1,1),
        AgentName              NVARCHAR(100) NOT NULL,
        SessionId              NVARCHAR(200) NOT NULL,
        Model                  NVARCHAR(100),
        InputTokens            INT NOT NULL DEFAULT 0,
        OutputTokens           INT NOT NULL DEFAULT 0,
        CacheReadTokens        INT NOT NULL DEFAULT 0,
        CacheWriteTokens       INT NOT NULL DEFAULT 0,
        ReasoningTokens        INT NOT NULL DEFAULT 0,
        TotalCostUsd           DECIMAL(10,6) NOT NULL DEFAULT 0,
        ActivityType           NVARCHAR(50),
        ApiCalls               INT NOT NULL DEFAULT 1,
        HasAgentSpawn          BIT NOT NULL DEFAULT 0,
        SessionDurationMinutes INT NOT NULL DEFAULT 0,
        EventTimestamp         DATETIME2 NOT NULL,
        CollectedAt            DATETIME2 NOT NULL,
        CreatedAt              DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_AgentEvents UNIQUE (SessionId, EventTimestamp, AgentName)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AgentEvents_EventTimestamp' AND object_id = OBJECT_ID('AgentEvents')
)
BEGIN
    CREATE INDEX IX_AgentEvents_EventTimestamp
        ON AgentEvents(EventTimestamp DESC);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AgentEvents_AgentName_Timestamp' AND object_id = OBJECT_ID('AgentEvents')
)
BEGIN
    CREATE INDEX IX_AgentEvents_AgentName_Timestamp
        ON AgentEvents(AgentName, EventTimestamp DESC);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AgentEvents_ActivityType' AND object_id = OBJECT_ID('AgentEvents')
)
BEGIN
    CREATE INDEX IX_AgentEvents_ActivityType
        ON AgentEvents(ActivityType, EventTimestamp DESC);
END;
