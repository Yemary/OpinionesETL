IF DB_ID(N'StagingDB') IS NULL CREATE DATABASE StagingDB;
GO
USE StagingDB;
GO
IF SCHEMA_ID(N'stg') IS NULL EXEC(N'CREATE SCHEMA stg');
GO

DROP TABLE IF EXISTS stg.EncuestaRaw;
DROP TABLE IF EXISTS stg.ResenaRaw;
DROP TABLE IF EXISTS stg.ComentarioRaw;
GO

CREATE TABLE stg.EncuestaRaw
(
    IdStg               INT IDENTITY(1,1) NOT NULL,
    IdOpinion           NVARCHAR(50)   NULL,
    IdCliente           NVARCHAR(50)   NULL,
    IdProducto          NVARCHAR(50)   NULL,
    Fecha               NVARCHAR(50)   NULL,
    Comentario          NVARCHAR(1000) NULL,
    Clasificacion       NVARCHAR(50)   NULL,
    PuntajeSatisfaccion NVARCHAR(50)   NULL,
    Fuente              NVARCHAR(100)  NULL,
    FechaExtraccion     DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_stg_EncuestaRaw PRIMARY KEY (IdStg)
);

CREATE TABLE stg.ResenaRaw
(
    IdStg           INT IDENTITY(1,1) NOT NULL,
    IdReview        NVARCHAR(50)   NULL,
    IdCliente       NVARCHAR(50)   NULL,
    IdProducto      NVARCHAR(50)   NULL,
    Fecha           NVARCHAR(50)   NULL,
    Comentario      NVARCHAR(1000) NULL,
    Rating          NVARCHAR(50)   NULL,
    FechaExtraccion DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_stg_ResenaRaw PRIMARY KEY (IdStg)
);

CREATE TABLE stg.ComentarioRaw
(
    IdStg           INT IDENTITY(1,1) NOT NULL,
    IdComment       NVARCHAR(50)   NULL,
    IdCliente       NVARCHAR(50)   NULL,
    IdProducto      NVARCHAR(50)   NULL,
    Fuente          NVARCHAR(100)  NULL,
    Fecha           NVARCHAR(50)   NULL,
    Comentario      NVARCHAR(1000) NULL,
    FechaExtraccion DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_stg_ComentarioRaw PRIMARY KEY (IdStg)
);
GO

SELECT name AS TablaCreada FROM sys.tables WHERE schema_id = SCHEMA_ID(N'stg');