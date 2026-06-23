-- Seed data: ~45 rows spanning the last 7 days (relative to 2026-06-23 16:00 UTC+3 = 13:00 UTC)
-- Agents: Cursor, Claude Code, GitHub Copilot
-- Models: "Cursor (auto)", "Opus 4.8", "Sonnet 4.6", "Copilot (OpenAI)"
-- ActivityTypes: coding, exploration, feature, conversation, debugging, brainstorming

INSERT INTO AgentEvents (AgentName, SessionId, Model, InputTokens, OutputTokens, CacheReadTokens, CacheWriteTokens, ReasoningTokens, TotalCostUsd, ActivityType, ApiCalls, HasAgentSpawn, SessionDurationMinutes, EventTimestamp, CollectedAt)
VALUES
-- 7 days ago: 2026-06-16
('Cursor',          'sess-cur-001', 'Cursor (auto)',    4200,  1100,  800,  300,    0, 0.031200, 'coding',         2, 0, 18, '2026-06-16T08:14:00', '2026-06-16T08:15:00'),
('Claude Code',     'sess-cla-001', 'Opus 4.8',        8900,  2400, 1200,  600,  900, 0.182000, 'feature',        3, 1, 42, '2026-06-16T09:33:00', '2026-06-16T09:34:00'),
('GitHub Copilot',  'sess-cop-001', 'Copilot (OpenAI)',2100,   780,    0,    0,    0, 0.014300, 'coding',         1, 0, 11, '2026-06-16T10:05:00', '2026-06-16T10:06:00'),
('Cursor',          'sess-cur-002', 'Cursor (auto)',    3800,   950,  500,  200,    0, 0.027400, 'exploration',    2, 0, 22, '2026-06-16T13:47:00', '2026-06-16T13:48:00'),
('Claude Code',     'sess-cla-002', 'Sonnet 4.6',      6100,  1800,  900,  400,  300, 0.098500, 'debugging',      4, 0, 35, '2026-06-16T15:22:00', '2026-06-16T15:23:00'),
('GitHub Copilot',  'sess-cop-002', 'Copilot (OpenAI)',1800,   620,    0,    0,    0, 0.011200, 'brainstorming',  1, 0,  8, '2026-06-16T17:10:00', '2026-06-16T17:11:00'),

-- 6 days ago: 2026-06-17
('Cursor',          'sess-cur-003', 'Cursor (auto)',    5500,  1400,  700,  350,    0, 0.039800, 'feature',        3, 1, 29, '2026-06-17T07:55:00', '2026-06-17T07:56:00'),
('Claude Code',     'sess-cla-003', 'Opus 4.8',       11200,  3100, 2100, 1000, 1400, 0.238000, 'feature',        5, 1, 58, '2026-06-17T10:18:00', '2026-06-17T10:19:00'),
('GitHub Copilot',  'sess-cop-003', 'Copilot (OpenAI)',3400,  1020,    0,    0,    0, 0.022800, 'coding',         2, 0, 16, '2026-06-17T11:44:00', '2026-06-17T11:45:00'),
('Cursor',          'sess-cur-004', 'Cursor (auto)',    2900,   820,  400,  180,    0, 0.021300, 'conversation',   1, 0, 14, '2026-06-17T14:30:00', '2026-06-17T14:31:00'),
('Claude Code',     'sess-cla-004', 'Sonnet 4.6',      7400,  2100, 1100,  500,  450, 0.121000, 'brainstorming',  3, 0, 31, '2026-06-17T16:05:00', '2026-06-17T16:06:00'),

-- 5 days ago: 2026-06-18
('Cursor',          'sess-cur-005', 'Cursor (auto)',    6200,  1700,  900,  420,    0, 0.044600, 'coding',         3, 0, 33, '2026-06-18T08:40:00', '2026-06-18T08:41:00'),
('GitHub Copilot',  'sess-cop-004', 'Copilot (OpenAI)',4100,  1280,    0,    0,    0, 0.029400, 'feature',        2, 0, 24, '2026-06-18T09:15:00', '2026-06-18T09:16:00'),
('Claude Code',     'sess-cla-005', 'Opus 4.8',       14500,  4200, 2800, 1300, 1800, 0.312000, 'feature',        6, 1, 71, '2026-06-18T10:50:00', '2026-06-18T10:51:00'),
('Cursor',          'sess-cur-006', 'Cursor (auto)',    3300,   880,  450,  210,    0, 0.024100, 'debugging',      2, 0, 19, '2026-06-18T13:20:00', '2026-06-18T13:21:00'),
('GitHub Copilot',  'sess-cop-005', 'Copilot (OpenAI)',2600,   890,    0,    0,    0, 0.018700, 'exploration',    1, 0, 13, '2026-06-18T15:48:00', '2026-06-18T15:49:00'),
('Claude Code',     'sess-cla-006', 'Sonnet 4.6',      5800,  1650,  850,  390,  280, 0.094200, 'debugging',      3, 0, 27, '2026-06-18T17:33:00', '2026-06-18T17:34:00'),

-- 4 days ago: 2026-06-19
('Cursor',          'sess-cur-007', 'Cursor (auto)',    7100,  1950,  1050, 490,    0, 0.051100, 'feature',        4, 1, 38, '2026-06-19T09:02:00', '2026-06-19T09:03:00'),
('Claude Code',     'sess-cla-007', 'Opus 4.8',        9800,  2700, 1800,  850, 1200, 0.207000, 'exploration',    4, 0, 45, '2026-06-19T11:25:00', '2026-06-19T11:26:00'),
('GitHub Copilot',  'sess-cop-006', 'Copilot (OpenAI)',1500,   510,    0,    0,    0, 0.009800, 'conversation',   1, 0,  7, '2026-06-19T12:10:00', '2026-06-19T12:11:00'),
('Cursor',          'sess-cur-008', 'Cursor (auto)',    4600,  1230,  630,  290,    0, 0.033500, 'brainstorming',  2, 0, 21, '2026-06-19T14:45:00', '2026-06-19T14:46:00'),
('Claude Code',     'sess-cla-008', 'Sonnet 4.6',      8200,  2350, 1400,  650,  520, 0.137000, 'coding',         4, 0, 39, '2026-06-19T16:38:00', '2026-06-19T16:39:00'),
('GitHub Copilot',  'sess-cop-007', 'Copilot (OpenAI)',3700,  1150,    0,    0,    0, 0.026400, 'coding',         2, 0, 20, '2026-06-19T18:12:00', '2026-06-19T18:13:00'),

-- 3 days ago: 2026-06-20
('Cursor',          'sess-cur-009', 'Cursor (auto)',    5100,  1380,  710,  330,    0, 0.036800, 'exploration',    3, 0, 26, '2026-06-20T08:28:00', '2026-06-20T08:29:00'),
('Claude Code',     'sess-cla-009', 'Opus 4.8',       13100,  3700, 2500, 1150, 1600, 0.278000, 'feature',        5, 1, 63, '2026-06-20T10:14:00', '2026-06-20T10:15:00'),
('GitHub Copilot',  'sess-cop-008', 'Copilot (OpenAI)',2300,   820,    0,    0,    0, 0.016600, 'debugging',      1, 0, 12, '2026-06-20T12:55:00', '2026-06-20T12:56:00'),
('Cursor',          'sess-cur-010', 'Cursor (auto)',    3900,  1050,  540,  250,    0, 0.028300, 'debugging',      2, 0, 17, '2026-06-20T15:07:00', '2026-06-20T15:08:00'),
('Claude Code',     'sess-cla-010', 'Sonnet 4.6',      6700,  1920,  1000, 460,  380, 0.109000, 'brainstorming',  3, 0, 30, '2026-06-20T17:44:00', '2026-06-20T17:45:00'),

-- 2 days ago: 2026-06-21
('Cursor',          'sess-cur-011', 'Cursor (auto)',    8400,  2280, 1200,  560,    0, 0.060400, 'coding',         4, 1, 46, '2026-06-21T09:35:00', '2026-06-21T09:36:00'),
('Claude Code',     'sess-cla-011', 'Opus 4.8',       10600,  2950, 1950,  900, 1350, 0.224000, 'conversation',   4, 0, 50, '2026-06-21T11:00:00', '2026-06-21T11:01:00'),
('GitHub Copilot',  'sess-cop-009', 'Copilot (OpenAI)',4800,  1510,    0,    0,    0, 0.034600, 'feature',        3, 0, 28, '2026-06-21T13:22:00', '2026-06-21T13:23:00'),
('Cursor',          'sess-cur-012', 'Cursor (auto)',    2700,   740,  380,  170,    0, 0.019700, 'conversation',   1, 0, 10, '2026-06-21T16:18:00', '2026-06-21T16:19:00'),
('Claude Code',     'sess-cla-012', 'Sonnet 4.6',      9100,  2600, 1550,  710,  600, 0.152000, 'coding',         5, 1, 44, '2026-06-21T18:30:00', '2026-06-21T18:31:00'),
('GitHub Copilot',  'sess-cop-010', 'Copilot (OpenAI)',1900,   680,    0,    0,    0, 0.013700, 'exploration',    1, 0,  9, '2026-06-21T20:05:00', '2026-06-21T20:06:00'),

-- 1 day ago: 2026-06-22
('Cursor',          'sess-cur-013', 'Cursor (auto)',    6800,  1860,  980,  450,    0, 0.048900, 'feature',        3, 0, 34, '2026-06-22T07:48:00', '2026-06-22T07:49:00'),
('Claude Code',     'sess-cla-013', 'Opus 4.8',       12300,  3450, 2300, 1050, 1550, 0.261000, 'debugging',      5, 1, 55, '2026-06-22T10:30:00', '2026-06-22T10:31:00'),
('GitHub Copilot',  'sess-cop-011', 'Copilot (OpenAI)',3200,   980,    0,    0,    0, 0.022900, 'coding',         2, 0, 18, '2026-06-22T12:15:00', '2026-06-22T12:16:00'),
('Cursor',          'sess-cur-014', 'Cursor (auto)',    4400,  1190,  610,  280,    0, 0.031900, 'brainstorming',  2, 0, 23, '2026-06-22T15:40:00', '2026-06-22T15:41:00'),
('Claude Code',     'sess-cla-014', 'Sonnet 4.6',      7600,  2180, 1150,  530,  440, 0.124000, 'exploration',    3, 0, 36, '2026-06-22T19:55:00', '2026-06-22T19:56:00'),

-- Today (last 24 h and last 1 h): 2026-06-23
('GitHub Copilot',  'sess-cop-012', 'Copilot (OpenAI)',2800,   960,    0,    0,    0, 0.020100, 'debugging',      2, 0, 15, '2026-06-23T06:30:00', '2026-06-23T06:31:00'),
('Cursor',          'sess-cur-015', 'Cursor (auto)',    5900,  1610,  830,  380,    0, 0.042500, 'coding',         3, 0, 31, '2026-06-23T08:00:00', '2026-06-23T08:01:00'),
('Claude Code',     'sess-cla-015', 'Opus 4.8',       15800,  4500, 3100, 1400, 2100, 0.341000, 'feature',        7, 1, 78, '2026-06-23T09:45:00', '2026-06-23T09:46:00'),
-- Within the last ~4 hours (dashboard 1h range)
('Cursor',          'sess-cur-016', 'Cursor (auto)',    3600,   990,  510,  230,    0, 0.026200, 'debugging',      2, 0, 20, '2026-06-23T11:50:00', '2026-06-23T11:51:00'),
('GitHub Copilot',  'sess-cop-013', 'Copilot (OpenAI)',4500,  1400,    0,    0,    0, 0.032300, 'feature',        3, 0, 25, '2026-06-23T12:10:00', '2026-06-23T12:11:00'),
('Claude Code',     'sess-cla-016', 'Sonnet 4.6',      6400,  1840,  960,  440,  370, 0.104000, 'coding',         3, 0, 28, '2026-06-23T12:30:00', '2026-06-23T12:31:00');
