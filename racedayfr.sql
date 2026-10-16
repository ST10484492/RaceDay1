

EXEC sp_help 'dbo.Users';
EXEC sp_help 'dbo.Participants';
EXEC sp_help 'dbo.Organisers';
EXEC sp_help 'dbo.Events';
EXEC sp_help 'dbo.Categories';
EXEC sp_help 'dbo.Enrolments';
EXEC sp_help 'dbo.Results';

-- Check which tables exist in the current database.
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Check the columns, data types and whether NULL is allowed.
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN (
    'Users',
    'Participants',
    'Organisers',
    'Events',
    'Categories',
    'Enrolments',
    'Results'
)
ORDER BY TABLE_NAME, ORDINAL_POSITION;


-- Check identity columns and primary keys.
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    COLUMNPROPERTY(
        OBJECT_ID(QUOTENAME(TABLE_SCHEMA) + '.' +
                  QUOTENAME(TABLE_NAME)),
        COLUMN_NAME,
        'IsIdentity'
    ) AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN (
    'Users', 'Participants', 'Organisers',
    'Events', 'Categories', 'Enrolments', 'Results'
)
AND COLUMN_NAME IN (
    'UserId', 'ParticipantID', 'OrganiserID',
    'EventID', 'CategoryID', 'EnrolmentID', 'ResultId'
)
ORDER BY TABLE_NAME;

-- Check existing foreign-key relationships.
SELECT
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)
        AS ReferencedColumn
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc
    ON fk.object_id = fkc.constraint_object_id
ORDER BY TableName, ColumnName;


-- Link participant records to login accounts.
-- Existing participant records remain intact.
ALTER TABLE dbo.Participants
ADD UserId INT NULL;
GO

-- Each login account can link to at most one participant.
CREATE UNIQUE INDEX UX_Participants_UserId
ON dbo.Participants(UserId)
WHERE UserId IS NOT NULL;
GO

-- Ensure linked login accounts exist in Users.
ALTER TABLE dbo.Participants
ADD CONSTRAINT FK_Participants_Users_UserId
FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId);
GO


SELECT
    UserId,
    FirstName,
    Email,
    Role
FROM dbo.Users
WHERE Email = 'thandi.test@example.com';

SELECT
    ParticipantID,
    UserId,
    FirstName,
    LastName,
    Email,
    Gender
FROM dbo.Participants
WHERE Email = 'thandi.test@example.com';


SELECT UserId, FirstName, Email, Role
FROM dbo.Users;

SELECT OrganiserID, OrganisationName, ContactEmail
FROM dbo.Organisers;


SELECT UserId, FirstName, Email, Role
FROM dbo.Users
WHERE Role = 'Organiser';


SELECT UserId, FirstName, Email, PasswordHash, Role
FROM dbo.Users
WHERE Role = 'Organiser';