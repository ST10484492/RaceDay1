CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- Create Organisers Table
CREATE TABLE Organisers (
    OrganiserID INT IDENTITY(1,1) NOT NULL,
    OrganisationName VARCHAR(100) NOT NULL,
    ContactEmail VARCHAR(100) NOT NULL,
    ContactPhone VARCHAR(20) NULL,
    CONSTRAINT PK_Organisers PRIMARY KEY (OrganiserID),
    CONSTRAINT UQ_Organiser_Email UNIQUE (ContactEmail)
);

-- Create Participants Table
CREATE TABLE Participants (
    ParticipantID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Gender VARCHAR(10) NULL,
    RegistrationDate DATETIME DEFAULT GETDATE() NOT NULL,
    CONSTRAINT PK_Participants PRIMARY KEY (ParticipantID),
    CONSTRAINT UQ_Participant_Email UNIQUE (Email)
);

-- Create Events Table
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) NOT NULL,
    OrganiserID INT NOT NULL,
    EventName VARCHAR(100) NOT NULL,
    EventDate DATETIME NOT NULL,
    [Location] VARCHAR(150) NOT NULL,
    Description TEXT NULL,
    CONSTRAINT PK_Events PRIMARY KEY (EventID),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID) 
        REFERENCES Organisers(OrganiserID) ON DELETE CASCADE
);

-- Create Categories Table
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) NOT NULL,
    EventID INT NOT NULL,
    CategoryName VARCHAR(50) NOT NULL, -- e.g., "10km Run", "Marathon", "5km Fun Walk"
    DistanceKM DECIMAL(5,2) NOT NULL,
    EntryFee DECIMAL(10,2) DEFAULT 0.00 NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) 
        REFERENCES Events(EventID) ON DELETE CASCADE,
    CONSTRAINT CHK_EntryFee CHECK (EntryFee >= 0)
);

-- Create Enrolments Table
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) NOT NULL,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME DEFAULT GETDATE() NOT NULL,
    PaymentStatus VARCHAR(20) DEFAULT 'Pending' NOT NULL, -- e.g., Pending, Paid, Cancelled
    RaceNumber INT NULL,
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentID),
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantID) 
        REFERENCES Participants(ParticipantID) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) 
        REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_Enrolment_Category_RaceNumber UNIQUE (CategoryID, RaceNumber),
    CONSTRAINT CHK_PaymentStatus CHECK (PaymentStatus IN ('Pending', 'Paid', 'Cancelled'))
);
GO

-- ============================================================================
-- 3. SAMPLE DATA SEEDING (DML)
-- ============================================================================

-- Seed Organisers (Minimum 2)
INSERT INTO Organisers (OrganisationName, ContactEmail, ContactPhone) VALUES
('Joinville Athletic Club', 'info@joinvilleclub.co.za', '+27115550123'),
('Eco Trails South Africa', 'events@ecotrails.co.za', '+27215554567'),
('Kaizer Chiefs Club', 'events@kfc.co.za', '+27123456789');

-- Seed Participants (Minimum 2)
INSERT INTO Participants (FirstName, LastName, Email, DateOfBirth, Gender) VALUES
('Thabo', 'Molefe', 'thabomolefe12@gmail.com', '1992-05-14', 'Male'),
('Sarah', 'Ramaphakela', 'saraRama11@gmail.com', '1995-11-23', 'Female'),
('Siseko', 'Zwane', 'sisekozwane17@gmail.com', '2001-05-12', 'Male');

-- Seed Events (Minimum 3)
INSERT INTO Events (OrganiserID, EventName, EventDate, [Location], Description) VALUES
(1, 'City Center Summer Classic', '2026-10-15 07:00:00', 'Johannesburg CBD', 'Annual city road running event.'),
(1, 'Night Neon Dash', '2026-11-05 19:00:00', 'Pretoria Botanical Gardens', 'Fun night run with neon lights.'),
(2, 'Magaliesberg Trail Challenge', '2026-12-12 06:00:00', 'Magaliesberg', 'Scenic and rugged mountain trail running experience.');

-- Seed Categories for each event
INSERT INTO Categories (EventID, CategoryName, DistanceKM, EntryFee) VALUES
(1, 'Marathon', 42.20, 350.00),
(1, 'Half Marathon', 21.10, 220.00),
(2, '5km Neon Fun Run', 5.00, 120.00),
(3, 'Extreme Trail 30k', 30.00, 450.00),
(3, 'Lite Trail 12k', 12.00, 250.00);

-- Seed Sample Enrolments
INSERT INTO Enrolments (ParticipantID, CategoryID, PaymentStatus, RaceNumber) VALUES
(1, 1, 'Paid', 1001), -- Thabo entering the City Center Marathon
(1, 3, 'Paid', 5001), -- Thabo entering the Night Neon Dash
(2, 2, 'Paid', 2001), -- Sarah entering the City Center Half Marathon
(2, 5, 'Pending', 1201); -- Sarah entering the Magaliesberg Lite Trail
GO

-- ============================================================================
-- 4. VERIFICATION SELECTION
-- ============================================================================
SELECT 'Organisers Seeded' AS TableName, COUNT(*) AS TotalRows FROM Organisers
UNION ALL
SELECT 'Participants', COUNT(*) FROM Participants
UNION ALL
SELECT 'Events', COUNT(*) FROM Events
UNION ALL
SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL
SELECT 'Enrolments', COUNT(*) FROM Enrolments;
GO