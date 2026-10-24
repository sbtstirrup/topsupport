/*
    00_CreateDatabase.sql
    Creates the TopSupport database if it doesn't already exist.
    Run this first, against LocalDB (or any SQL Server instance) with sqlcmd or SSMS.
*/
IF DB_ID(N'TopSupport') IS NULL
BEGIN
    CREATE DATABASE TopSupport;
END
GO
