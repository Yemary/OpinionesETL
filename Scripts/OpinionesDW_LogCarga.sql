USE OpinionesDW;
GO
DROP TABLE IF EXISTS dbo.LogCargaDimension;
GO
CREATE TABLE dbo.LogCargaDimension
(
    IdLog          INT IDENTITY(1,1) NOT NULL,
    Dimension      NVARCHAR(50) NOT NULL,
    Leidos         INT          NOT NULL,
    Insertados     INT          NOT NULL,
    Actualizados   INT          NOT NULL,
    DuracionMs     INT          NOT NULL,
    FechaEjecucion DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_LogCargaDimension PRIMARY KEY (IdLog)
);
GO
SELECT name AS TablaCreada FROM sys.tables WHERE name = 'LogCargaDimension';