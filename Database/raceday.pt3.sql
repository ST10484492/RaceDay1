-- =====================================================================
-- PART 3 DATABASE CHANGES
-- =====================================================================
USE RaceDayDB;
GO

-- 1. Events need a type, a distance and a banner image URL (Azure Blob).
ALTER TABLE dbo.Events ADD
    EventType  NVARCHAR(50)  NOT NULL CONSTRAINT DF_Events_EventType DEFAULT 'Run',
    DistanceKm DECIMAL(6,2)  NOT NULL CONSTRAINT DF_Events_DistanceKm DEFAULT 0,
    BannerUrl  NVARCHAR(500) NULL;
GO

-- 2. Users get a profile picture URL (Azure Blob).
ALTER TABLE dbo.Users ADD ProfilePictureUrl NVARCHAR(500) NULL;
GO


-- 3. HOW TO CREATE AN ORGANISER LOGIN
--    a) Register a normal account in the web app (e.g. organiser@raceday.com).
--    b) Run the two statements below with that email.
--    The API links a login to its Organisers row by matching Users.Email = Organisers.ContactEmail.
UPDATE dbo.Users SET Role = 'Organiser' WHERE Email = 'organiser@raceday.com';

INSERT INTO dbo.Organisers (OrganisationName, ContactEmail, ContactPhone)
SELECT 'My Race Company', 'organiser@raceday.com', '0123456789'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Organisers WHERE ContactEmail = 'organiser@raceday.com');
GO