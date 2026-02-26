-- Script simplificado para actualizar la base de datos

-- 1. Crear tabla MedidasCorporales si no existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MedidasCorporales')
BEGIN
    CREATE TABLE [MedidasCorporales] (
        [Id] int NOT NULL IDENTITY PRIMARY KEY,
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
        CONSTRAINT [FK_MedidasCorporales_ClientesPerfil] FOREIGN KEY ([ClienteId]) REFERENCES [ClientesPerfil] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_MedidasCorporales_ClienteId] ON [MedidasCorporales] ([ClienteId]);
    PRINT 'Tabla MedidasCorporales creada.'
END
ELSE
    PRINT 'Tabla MedidasCorporales ya existe.'
GO

-- 2. Agregar columnas nuevas a SeguimientoDiario
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
    ALTER TABLE SeguimientoDiario ADD DiaEntreno nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'RendimientoSesion')
    ALTER TABLE SeguimientoDiario ADD RendimientoSesion nvarchar(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'CalidadSueno')
    ALTER TABLE SeguimientoDiario ADD CalidadSueno nvarchar(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'Apetito')
    ALTER TABLE SeguimientoDiario ADD Apetito nvarchar(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'DuracionCardio')
    ALTER TABLE SeguimientoDiario ADD DuracionCardio nvarchar(50) NULL;

PRINT 'Columnas nuevas agregadas a SeguimientoDiario.'
GO

-- 3. Manejar la conversión de NivelEstres (de int a nvarchar)
DECLARE @columnType nvarchar(50);
SELECT @columnType = TYPE_NAME(system_type_id)
FROM sys.columns 
WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'NivelEstres';

IF @columnType = 'int'
BEGIN
    PRINT 'Convirtiendo NivelEstres de int a nvarchar...'
    
    -- Paso 1: Crear columna temporal
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'NivelEstresStr')
        ALTER TABLE SeguimientoDiario ADD NivelEstresStr nvarchar(50) NULL;
    
    -- Paso 2: Copiar y convertir datos
    UPDATE SeguimientoDiario 
    SET NivelEstresStr = CASE 
        WHEN NivelEstres = 1 THEN 'Bajo'
        WHEN NivelEstres = 2 THEN 'Medio'
        WHEN NivelEstres = 3 THEN 'Alto'
        WHEN NivelEstres IS NOT NULL THEN CAST(NivelEstres AS nvarchar(50))
        ELSE NULL
    END;
    
    -- Paso 3: Eliminar columna vieja
    ALTER TABLE SeguimientoDiario DROP COLUMN NivelEstres;
    
    -- Paso 4: Renombrar la nueva
    EXEC sp_rename 'dbo.SeguimientoDiario.NivelEstresStr', 'NivelEstres', 'COLUMN';
    
    PRINT 'NivelEstres convertido exitosamente.'
END
ELSE IF @columnType IS NULL
BEGIN
    -- La columna no existe, créala
    ALTER TABLE SeguimientoDiario ADD NivelEstres nvarchar(50) NULL;
    PRINT 'Columna NivelEstres creada como nvarchar.'
END
ELSE
BEGIN
    PRINT 'NivelEstres ya es de tipo ' + @columnType + '.'
END
GO

-- 4. Migrar NivelHambre a Apetito si existe
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SeguimientoDiario') AND name = 'NivelHambre')
BEGIN
    PRINT 'Migrando NivelHambre a Apetito...'
    
    UPDATE SeguimientoDiario 
    SET Apetito = CASE 
        WHEN NivelHambre = 1 THEN 'Bajo'
        WHEN NivelHambre = 2 THEN 'Medio'
        WHEN NivelHambre = 3 THEN 'Alto'
        WHEN NivelHambre IS NOT NULL THEN CAST(NivelHambre AS nvarchar(50))
        ELSE Apetito
    END
    WHERE NivelHambre IS NOT NULL;
    
    PRINT 'Datos migrados de NivelHambre a Apetito.'
END
GO

PRINT '=== Script completado exitosamente ==='
