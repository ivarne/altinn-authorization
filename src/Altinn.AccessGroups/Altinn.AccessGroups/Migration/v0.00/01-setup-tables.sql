-- Schema: accessgroup
CREATE SCHEMA IF NOT EXISTS accessgroup
    AUTHORIZATION postgres;

-- Enum: AccessGroup.AccessGroupType
DO $$ BEGIN
    CREATE TYPE accessgroup.AccessGroupType AS ENUM ('Altinn');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Table: accessgroup.AccessGroup
CREATE TABLE IF NOT EXISTS accessgroup.AccessGroup
(
    AccessGroupCode character varying PRIMARY KEY,
    AccessGroupType accessgroup.AccessGroupType NOT NULL,
    Hidden boolean,
    Created timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Modified timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
)
TABLESPACE pg_default;

-- Enum: AccessGroup.ExternalSource
DO $$ BEGIN
    CREATE TYPE accessgroup.ExternalSource AS ENUM ('Enhetsregisteret');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Table: accessgroup.ExternalRelationship
CREATE TABLE IF NOT EXISTS accessgroup.ExternalRelationship
(
    ExternalSource accessgroup.ExternalSource NOT NULL,
    ExternalId character varying NOT NULL,
    AccessGroupCode character varying NOT NULL,
    UnitTypeFilter character varying DEFAULT NULL,
    PRIMARY KEY (ExternalSource, ExternalId, AccessGroupCode)
)
TABLESPACE pg_default;

-- Table: accessgroup.Category
CREATE TABLE IF NOT EXISTS accessgroup.Category
(
    CategoryCode character varying PRIMARY KEY
)
TABLESPACE pg_default;

-- Table: accessgroup.AccessGroupCategory
CREATE TABLE IF NOT EXISTS accessgroup.AccessGroupCategory
(
    AccessGroupCode character varying,
    CategoryCode character varying,
    PRIMARY KEY (AccessGroupCode, CategoryCode)
)
TABLESPACE pg_default;

-- Table: accessgroup.AccessGroupMembership
CREATE TABLE IF NOT EXISTS accessgroup.AccessGroupMembership
(
    MembershipId bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    OfferedByParty bigint,
    UserId bigint,
    PartyId bigint,
    DelegationId bigint,
    ValidTo timestamp with time zone
)
TABLESPACE pg_default;

-- Enum: AccessGroup.DelegationType
DO $$ BEGIN
    CREATE TYPE accessgroup.DelegationType AS ENUM ('Brukerdelegering', 'Klientdelegering', 'Tjenestedelegering');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Table: accessgroup.MemberShipDelegation
CREATE TABLE IF NOT EXISTS accessgroup.MemberShipDelegation
(
    DelegationId bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    DelegatedByUserId bigint,
    DelegatedByPartyId bigint,
    DelegationTime date,
    DelegationType accessgroup.DelegationType NOT NULL
)
TABLESPACE pg_default;

-- Table: accessgroup.MembershipHistory
CREATE TABLE IF NOT EXISTS accessgroup.MembershipHistory
(
    HistoryId bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    MembershipId bigint,
    OfferedByParty bigint,
    UserId bigint,
    PartyId bigint,
    DelegationId bigint,
    ValidTo timestamp with time zone
)
TABLESPACE pg_default;

-- Table: accessgroup.ResourceRight
CREATE TABLE IF NOT EXISTS accessgroup.ResourceRight
(
    RightId bigint,
    GroupId bigint,
    ResourceId bigint
)
TABLESPACE pg_default;

-- Enum: AccessGroup.TextResourceType
DO $$ BEGIN
    CREATE TYPE accessgroup.TextResourceType AS ENUM ('AccessGroup', 'Category');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Table: accessgroup.TextResources
CREATE TABLE IF NOT EXISTS accessgroup.TextResource
(
    TextResourceId bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    TextType accessgroup.TextResourceType NOT NULL,
    Key character varying NOT NULL,
    Content character varying NOT NULL,
    Language character varying NOT NULL,
    AccessGroupCode character varying,
    CategoryCode character varying
)
TABLESPACE pg_default;

