-- Script para agregar las nuevas tablas y columnas de Excel Import/Export

-- Crear tabla MedidasCorporales si no existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MedidasCorporales')
BEGIN
    CREATE TABLE [MedidasCorporales] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [NumeroSemana] int NOT NULL,
        [Fecha] datetime2 NULL,
        [Pecho] decimal(5, 2) NULL,
        [Brazo] decimal(5, 2) NULL,
        [CinturaSobreOmbligo] decimal(5, 2) NULL,
        [CinturaOmbligo] decimal(5, 2) NULL,
        [CinturaBajoOmbligo] decimal(5, 2) NULL,
        [Cadera] decimal(5, 2) NULL,
        [Muslos] decimal(5, 2) NULL,
        CONSTRAINT [PK_MedidasCorporales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MedidasCorporales_ClientesPerfil_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [ClientesPerfil] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_MedidasCorporales_ClienteId] ON [MedidasCorporales] ([ClienteId]);
END

-- Agregar nuevas columnas a SeguimientoDiario si no existen
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'NumeroSemana')
    ALTER TABLE SeguimientoDiario ADD NumeroSemana int NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'HoraPesaje')
    ALTER TABLE SeguimientoDiario ADD HoraPesaje time NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'Proteina')
    ALTER TABLE SeguimientoDiario ADD Proteina decimal(5, 2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'Grasa')
    ALTER TABLE SeguimientoDiario ADD Grasa decimal(5, 2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'Carbohidratos')
    ALTER TABLE SeguimientoDiario ADD Carbohidratos decimal(5, 2) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'TotalCalorias')
    ALTER TABLE SeguimientoDiario ADD TotalCalorias int NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'DiaEntreno')
    ALTER TABLE SeguimientoDiario ADD DiaEntreno nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'RendimientoSesion')
    ALTER TABLE SeguimientoDiario ADD RendimientoSesion nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'CalidadSueno')
    ALTER TABLE SeguimientoDiario ADD CalidadSueno nvarchar(max) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'Apetito')
    ALTER TABLE SeguimientoDiario ADD Apetito nvarchar(max) NULL;

-- Modificar la columna NivelEstres si es necesario (de int a nvarchar)
-- Primero verificar si la columna existe y es de tipo int
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('SeguimientoDiario') 
    AND name = 'NivelEstres' 
    AND system_type_id = 56 -- 56 is int
)
BEGIN
    PRINT 'Convirtiendo NivelEstres de int a nvarchar...'

    -- Crear una columna temporal
    ALTER TABLE SeguimientoDiario ADD NivelEstresTmp nvarchar(50) NULL;

    -- Copiar datos convirtiendo int a string
    UPDATE SeguimientoDiario 
    SET NivelEstresTmp = CASE 
        WHEN NivelEstres = 1 THEN 'Bajo'
        WHEN NivelEstres = 2 THEN 'Medio'
        WHEN NivelEstres = 3 THEN 'Alto'
        ELSE CAST(NivelEstres AS nvarchar(50))
    END
    WHERE NivelEstres IS NOT NULL;

    -- Eliminar la columna vieja
    ALTER TABLE SeguimientoDiario DROP COLUMN NivelEstres;

    -- Renombrar la temporal
    EXEC sp_rename 'dbo.SeguimientoDiario.NivelEstresTmp', 'NivelEstres', 'COLUMN';

    PRINT 'Columna NivelEstres convertida exitosamente.'
END
ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'NivelEstres')
BEGIN
    ALTER TABLE SeguimientoDiario ADD NivelEstres nvarchar(50) NULL;
    PRINT 'Columna NivelEstres creada.'
END
ELSE
BEGIN
    PRINT 'Columna NivelEstres ya es de tipo nvarchar.'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'DuracionCardio')
    ALTER TABLE SeguimientoDiario ADD DuracionCardio nvarchar(max) NULL;

-- Convertir NivelHambre a Apetito si existe como int
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('SeguimientoDiario') 
    AND name = 'NivelHambre' 
    AND system_type_id = 56 -- 56 is int
)
BEGIN
    PRINT 'Convirtiendo NivelHambre a Apetito...'

    -- Si Apetito no existe, créala
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'Apetito')
    BEGIN
        ALTER TABLE SeguimientoDiario ADD Apetito nvarchar(50) NULL;
    END

    -- Copiar y convertir datos
    UPDATE SeguimientoDiario 
    SET Apetito = CASE 
        WHEN NivelHambre = 1 THEN 'Bajo'
        WHEN NivelHambre = 2 THEN 'Medio'
        WHEN NivelHambre = 3 THEN 'Alto'
        ELSE CAST(NivelHambre AS nvarchar(50))
    END
    WHERE NivelHambre IS NOT NULL AND Apetito IS NULL;

    PRINT 'Datos de NivelHambre migrados a Apetito.'
END

PRINT 'Script ejecutado correctamente. Tablas y columnas creadas/actualizadas.'
