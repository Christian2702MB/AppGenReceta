-- =====================================================
-- MÓDULO: LIQUIDACIÓN DE INSUMOS DE ESTAMPADO
-- Fecha: 20/05/2026
-- Autor: CMendez / Antigravity
-- Base de Datos: HIALPESA (Servidor HIALPESA103)
-- =====================================================

USE [HIALPESA]
GO

-- =====================================================
-- SECCIÓN 1: TABLAS
-- =====================================================

-- 1.1 Cabecera de Fórmula
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIQ_Formulas]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LIQ_Formulas] (
        IdFormula           INT IDENTITY(1,1) PRIMARY KEY,
        IdRecetaOrigen      INT NOT NULL,           -- FK a AGR_Recetas.IdRecetas
        NP                  VARCHAR(20)  NULL,
        Cliente             VARCHAR(100) NULL,
        Temporada           VARCHAR(100) NULL,
        Estilo              VARCHAR(100) NULL,
        EstiloPropio        VARCHAR(100) NULL,
        Item                VARCHAR(100) NULL,
        Combo               VARCHAR(100) NULL,
        Ubicacion           VARCHAR(50)  NULL,
        Tecnica             VARCHAR(100) NULL,
        OperarioUDP         VARCHAR(100) NULL,
        FechaUDP            VARCHAR(100) NULL,
        Prendas             VARCHAR(100) NULL,
        Arte                VARCHAR(100) NULL,
        -- Auditoría
        UsuarioCreacion     VARCHAR(100) NULL,
        FechaCreacion       DATETIME     DEFAULT GETDATE(),
        UsuarioModificacion VARCHAR(100) NULL,
        FechaModificacion   DATETIME     NULL,
        -- Estado
        Estado              VARCHAR(20)  DEFAULT 'Activa', -- Activa / Cerrada
        UsuarioCierre       VARCHAR(100) NULL,
        FechaCierre         VARCHAR(50)  NULL,
        UsuarioApertura     VARCHAR(100) NULL,
        FechaApertura       VARCHAR(50)  NULL,
        ComentarioCerrar    VARCHAR(500) NULL,
        ComentarioAbrir     VARCHAR(500) NULL,
        -- Eliminación lógica
        Eliminado           BIT          DEFAULT 0,
        UsuarioElimina      VARCHAR(100) NULL,
        FechaElimina        DATETIME     NULL,
        ComentarioElimina   VARCHAR(500) NULL
    )
    PRINT 'Tabla LIQ_Formulas creada exitosamente.'
END
ELSE
    PRINT 'Tabla LIQ_Formulas ya existe.'
GO

-- 1.2 Colores de la Fórmula
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIQ_FormulaColores]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LIQ_FormulaColores] (
        IdFormulaColor  INT IDENTITY(1,1) PRIMARY KEY,
        IdFormula       INT NOT NULL,   -- FK a LIQ_Formulas.IdFormula
        NombreColor     VARCHAR(100) NULL,
        Combo           VARCHAR(50)  NULL
    )
    PRINT 'Tabla LIQ_FormulaColores creada exitosamente.'
END
ELSE
    PRINT 'Tabla LIQ_FormulaColores ya existe.'
GO

-- 1.3 Insumos por Color
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIQ_FormulaInsumos]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LIQ_FormulaInsumos] (
        IdFormulaInsumo INT IDENTITY(1,1) PRIMARY KEY,
        IdFormulaColor  INT NOT NULL,   -- FK a LIQ_FormulaColores.IdFormulaColor
        CodigoInsumo    VARCHAR(50)  NULL,
        Descripcion     VARCHAR(200) NULL,
        Cantidad        DECIMAL(18,2) NULL
    )
    PRINT 'Tabla LIQ_FormulaInsumos creada exitosamente.'
END
ELSE
    PRINT 'Tabla LIQ_FormulaInsumos ya existe.'
GO

-- 1.4 Pruebas UDP de la Fórmula
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LIQ_FormulaInsumosPrueba]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[LIQ_FormulaInsumosPrueba] (
        IdPrueba        INT IDENTITY(1,1) PRIMARY KEY,
        IdFormula       INT NOT NULL,   -- FK a LIQ_Formulas.IdFormula
        NombreColor     VARCHAR(50) NULL,
        CodigoInsumo    VARCHAR(50) NULL,
        NombrePrueba    VARCHAR(100) NULL,
        GramosUDP       DECIMAL(18,4) NULL,
        EsPrincipal     BIT DEFAULT 0
    )
    PRINT 'Tabla LIQ_FormulaInsumosPrueba creada exitosamente.'
END
ELSE
    PRINT 'Tabla LIQ_FormulaInsumosPrueba ya existe.'
GO

-- =====================================================
-- SECCIÓN 2: STORED PROCEDURES
-- =====================================================

-- 2.1 Listar Fórmulas (Mantenimiento)
IF OBJECT_ID('dbo.LIQ_SP_ListarFormulas', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ListarFormulas;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ListarFormulas]
    @FechaInicio VARCHAR(10) = NULL, -- Formato 'DD/MM/YYYY'
    @FechaFin    VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FInicio DATETIME = NULL
    DECLARE @FFin DATETIME = NULL

    IF ISNULL(@FechaInicio, '') <> ''
        SET @FInicio = CONVERT(DATETIME, @FechaInicio, 103)

    IF ISNULL(@FechaFin, '') <> ''
        SET @FFin = DATEADD(DAY, 1, CONVERT(DATETIME, @FechaFin, 103))

    SELECT
        F.IdFormula,
        F.IdRecetaOrigen,
        F.NP,
        F.Cliente,
        F.Temporada,
        F.Estilo,
        F.EstiloPropio,
        F.Item,
        F.Combo,
        F.Ubicacion,
        F.Tecnica,
        F.OperarioUDP,
        F.Prendas,
        F.Estado,
        CONVERT(VARCHAR, F.FechaCreacion, 103) AS FechaRegistro,
        CONVERT(VARCHAR, F.FechaCreacion, 108) AS HoraRegistro,
        ISNULL(F.UsuarioCierre, '') AS UsuarioCierre,
        ISNULL(F.FechaCierre, '') AS FechaCierre,
        ISNULL(F.UsuarioApertura, '') AS UsuarioApertura,
        ISNULL(F.FechaApertura, '') AS FechaApertura
    FROM LIQ_Formulas F (NOLOCK)
    WHERE F.Eliminado = 0
      AND (@FInicio IS NULL OR F.FechaCreacion >= @FInicio)
      AND (@FFin IS NULL OR F.FechaCreacion < @FFin)
    ORDER BY F.IdFormula DESC
END
GO

-- 2.2 Obtener Fórmula Completa (3 result sets)
IF OBJECT_ID('dbo.LIQ_SP_ObtenerFormulaCompleta', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ObtenerFormulaCompleta;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ObtenerFormulaCompleta]
    @IdFormula INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Tabla 1: Cabecera
    SELECT
        F.IdFormula,
        F.IdRecetaOrigen,
        F.NP,
        F.OperarioUDP AS Operario,
        F.Tecnica,
        F.Ubicacion,
        F.Arte,
        F.FechaUDP,
        F.Cliente,
        RTRIM(F.Estilo) AS Estilo,
        RTRIM(F.EstiloPropio) AS EstiloPropio,
        RTRIM(F.Combo) AS ComboCabecera,
        F.Prendas AS PrendasReq,
        RTRIM(F.Temporada) AS Temporada,
        RTRIM(F.Item) AS Item,
        ISNULL(F.UsuarioCierre, '') AS UsuarioCierre,
        ISNULL(F.FechaCierre, '') AS FechaCierre,
        ISNULL(F.UsuarioApertura, '') AS UsuarioApertura,
        ISNULL(F.FechaApertura, '') AS FechaApertura,
        CONVERT(VARCHAR, F.FechaCreacion, 103) AS FechaRegistro,
        CONVERT(VARCHAR, F.FechaCreacion, 108) AS HoraRegistro,
        F.Estado
    FROM LIQ_Formulas F
    WHERE F.IdFormula = @IdFormula AND F.Eliminado = 0;

    -- Tabla 2: Colores + Insumos
    SELECT
        C.IdFormulaColor AS IdColor,
        I.IdFormulaInsumo AS IdInsumo,
        C.NombreColor,
        C.Combo AS ComboColor,
        I.CodigoInsumo,
        I.Descripcion AS InsumoDescripcion,
        I.Cantidad AS CantidadInsumo
    FROM LIQ_FormulaColores C
    INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
    WHERE C.IdFormula = @IdFormula;

    -- Tabla 3: Pruebas UDP
    SELECT
        NombreColor,
        CodigoInsumo,
        NombrePrueba,
        ISNULL(GramosUDP, 0) AS GramosUDP,
        ISNULL(EsPrincipal, 0) AS EsPrincipal,
        IdPrueba
    FROM LIQ_FormulaInsumosPrueba
    WHERE IdFormula = @IdFormula;
END
GO

-- 2.3 Crear Fórmula desde Receta (Copia completa)
IF OBJECT_ID('dbo.LIQ_SP_CrearFormulaDesdeReceta', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_CrearFormulaDesdeReceta;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_CrearFormulaDesdeReceta]
    @IdRecetaOrigen INT,
    @UsuarioCreacion VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION

        -- 1. Copiar cabecera desde AGR_Recetas
        INSERT INTO LIQ_Formulas (
            IdRecetaOrigen, NP, Cliente, Temporada, Estilo, EstiloPropio,
            Item, Combo, Ubicacion, Tecnica, OperarioUDP, FechaUDP,
            Prendas, Arte, UsuarioCreacion, FechaCreacion, Estado
        )
        SELECT
            IdRecetas, NP, Cliente, Temporada, Estilo, EstiloPropio,
            Item, Combo, Ubicacion, Tecnica, OperarioUDP,
            CONVERT(VARCHAR, FechaUDP, 103),
            Prendas, Arte, @UsuarioCreacion, GETDATE(), 'Creada'
        FROM AGR_Recetas
        WHERE IdRecetas = @IdRecetaOrigen;

        DECLARE @IdFormula INT = SCOPE_IDENTITY();

        -- 2. Copiar colores
        DECLARE @MapColores TABLE (IdColorOriginal INT, IdFormulaColorNuevo INT, NombreColor VARCHAR(100));

        INSERT INTO LIQ_FormulaColores (IdFormula, NombreColor, Combo)
        OUTPUT inserted.IdFormulaColor, inserted.NombreColor INTO @MapColores(IdFormulaColorNuevo, NombreColor)
        SELECT
            @IdFormula, C.NombreColor, C.Combo
        FROM AGR_Colores C
        WHERE C.IdRecetas = @IdRecetaOrigen;

        -- Mapear IDs originales a nuevos
        UPDATE M SET M.IdColorOriginal = C.IdColor
        FROM @MapColores M
        INNER JOIN AGR_Colores C ON C.NombreColor = M.NombreColor AND C.IdRecetas = @IdRecetaOrigen;

        -- 3. Copiar insumos cruzando con el mapeo de colores
        -- IMPORTANTE: Solo tomamos la prueba principal (EsPrincipal = 1) para obtener la Cantidad correcta
        INSERT INTO LIQ_FormulaInsumos (IdFormulaColor, CodigoInsumo, Descripcion, Cantidad)
        SELECT
            MC.IdFormulaColorNuevo,
            I.CodigoInsumo,
            I.Descripcion,
            ISNULL(P.GramosUDP, 0) -- Usamos los gramos de la prueba principal, si no tiene, es 0
        FROM AGR_Insumos I
        INNER JOIN @MapColores MC ON MC.IdColorOriginal = I.IdColor
        LEFT JOIN AGR_Insumos_Prueba P 
               ON P.IdRecetas = @IdRecetaOrigen 
              AND P.NombreColor = MC.NombreColor 
              AND P.CodigoInsumo = I.CodigoInsumo 
              AND P.EsPrincipal = 1;

        /* 
        -- 4. Copiar pruebas UDP (A petición del usuario, las pruebas no se arrastran, solo la cantidad a la base)
        INSERT INTO LIQ_FormulaInsumosPrueba (IdFormula, NombreColor, CodigoInsumo, NombrePrueba, GramosUDP, EsPrincipal)
        SELECT
            @IdFormula, P.NombreColor, P.CodigoInsumo, P.NombrePrueba, P.GramosUDP, P.EsPrincipal
        FROM AGR_Insumos_Prueba P
        WHERE P.IdRecetas = @IdRecetaOrigen
          AND P.EsPrincipal = 1;
        */

        COMMIT TRANSACTION;

        -- Retornar el ID de la nueva fórmula
        SELECT @IdFormula AS IdFormula, 1 AS Resultado;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMsg, 16, 1);
    END CATCH
END
GO

-- 2.4 Actualizar Fórmula (Recibe XML)
IF OBJECT_ID('dbo.LIQ_SP_ActualizarFormula', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ActualizarFormula;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ActualizarFormula]
    @XML_DATA XML
AS
BEGIN
    SET ARITHABORT ON;
    SET ANSI_WARNINGS ON;
    SET ANSI_PADDING ON;
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    SET CONCAT_NULL_YIELDS_NULL ON;
    SET NUMERIC_ROUNDABORT OFF;
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION

            DECLARE @IdFormula INT = @XML_DATA.value('(FormulaBE/IdFormula)[1]', 'INT');

            -- Actualizar cabecera (protegiendo campos nulos o vacíos)
            UPDATE LIQ_Formulas SET
                OperarioUDP = ISNULL(NULLIF(@XML_DATA.value('(FormulaBE/Operario)[1]', 'VARCHAR(100)'), ''), OperarioUDP),
                Tecnica     = ISNULL(NULLIF(@XML_DATA.value('(FormulaBE/Tecnica)[1]', 'VARCHAR(100)'), ''), Tecnica),
                FechaUDP    = ISNULL(NULLIF(@XML_DATA.value('(FormulaBE/FechaUDP)[1]', 'VARCHAR(100)'), ''), FechaUDP),
                Prendas     = ISNULL(NULLIF(@XML_DATA.value('(FormulaBE/PrendasReq)[1]', 'VARCHAR(100)'), ''), Prendas),
                FechaModificacion = GETDATE(),
                UsuarioModificacion = @XML_DATA.value('(FormulaBE/UsuarioModificacion)[1]', 'VARCHAR(100)')
            WHERE IdFormula = @IdFormula;

            -- Limpiar detalle anterior
            DELETE FROM LIQ_FormulaInsumos WHERE IdFormulaColor IN (SELECT IdFormulaColor FROM LIQ_FormulaColores WHERE IdFormula = @IdFormula);
            DELETE FROM LIQ_FormulaColores WHERE IdFormula = @IdFormula;

            -- Reinsertar colores
            DECLARE @TabColores TABLE (IdGenerado INT, NombreColor VARCHAR(100));

            INSERT INTO LIQ_FormulaColores (IdFormula, NombreColor, Combo)
            OUTPUT inserted.IdFormulaColor, inserted.NombreColor INTO @TabColores
            SELECT
                @IdFormula,
                T.c.value('(Nombre)[1]', 'VARCHAR(100)'),
                T.c.value('(Combo)[1]', 'VARCHAR(50)')
            FROM @XML_DATA.nodes('/FormulaBE/Colores/ColorBE') AS T(c);

            -- Reinsertar insumos
            INSERT INTO LIQ_FormulaInsumos (IdFormulaColor, CodigoInsumo, Descripcion, Cantidad)
            SELECT
                tc.IdGenerado,
                I.c.value('(CodigoInsumo)[1]', 'VARCHAR(50)'),
                I.c.value('(Descripcion)[1]', 'VARCHAR(200)'),
                I.c.value('(Cantidad)[1]', 'DECIMAL(18,2)')
            FROM @XML_DATA.nodes('/FormulaBE/Colores/ColorBE') AS T(c)
            CROSS APPLY T.c.nodes('Insumos/InsumoBE') AS I(c)
            INNER JOIN @TabColores tc ON tc.NombreColor = T.c.value('(Nombre)[1]', 'VARCHAR(100)');

            -- Reinsertar pruebas
            DELETE FROM LIQ_FormulaInsumosPrueba WHERE IdFormula = @IdFormula;

            INSERT INTO LIQ_FormulaInsumosPrueba (IdFormula, NombreColor, CodigoInsumo, NombrePrueba, GramosUDP, EsPrincipal)
            SELECT
                @IdFormula,
                Color.value('(Nombre)[1]', 'VARCHAR(50)'),
                Insumo.value('(CodigoInsumo)[1]', 'VARCHAR(50)'),
                Prueba.value('(NombrePrueba)[1]', 'VARCHAR(100)'),
                Prueba.value('(GramosUDP)[1]', 'DECIMAL(18,4)'),
                Prueba.value('(EsPrincipal)[1]', 'BIT')
            FROM @XML_DATA.nodes('//FormulaBE/Colores/ColorBE') AS C(Color)
            CROSS APPLY Color.nodes('Insumos/InsumoBE') AS I(Insumo)
            CROSS APPLY Insumo.nodes('PruebasUDP/PruebaUDPBE') AS P(Prueba);

            -- Si se agregaron pruebas (fórmulas), pasa a estado 'Activa'
            -- Si se agregaron pruebas (fórmulas), pasa a estado 'Activa'
            IF EXISTS (SELECT 1 FROM LIQ_FormulaInsumosPrueba WHERE IdFormula = @IdFormula)
            BEGIN
                UPDATE LIQ_Formulas SET Estado = 'Activa' WHERE IdFormula = @IdFormula AND Estado = 'Creada';
            END
            ELSE
            BEGIN
                UPDATE LIQ_Formulas SET Estado = 'Creada' WHERE IdFormula = @IdFormula AND Estado = 'Activa';
            END

            SELECT 1 AS Resultado;
        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT ERROR_MESSAGE() AS Resultado;
    END CATCH
END
GO

-- 2.5 Eliminar Fórmula (Eliminación lógica)
IF OBJECT_ID('dbo.LIQ_SP_EliminarFormula', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_EliminarFormula;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_EliminarFormula]
    @IdFormula      INT,
    @UsuarioElimina VARCHAR(100),
    @Comentario     VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE LIQ_Formulas
        SET Eliminado = 1,
            UsuarioElimina = @UsuarioElimina,
            FechaElimina = GETDATE(),
            ComentarioElimina = @Comentario
        WHERE IdFormula = @IdFormula;

        SELECT 1 AS Resultado, 'Fórmula eliminada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO

-- 2.6 Listar Recetas disponibles para crear Fórmula
-- Condiciones:
--   1. Solo recetas con NP de exactamente 5 dígitos
--   2. Excluir recetas que ya tengan fórmula (NP+Combo+Item ya existe en LIQ_Formulas activa)
IF OBJECT_ID('dbo.LIQ_SP_ListarRecetasParaFormula', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_ListarRecetasParaFormula;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_ListarRecetasParaFormula]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        R.IdRecetas,
        R.NP,
        R.Cliente,
        R.Temporada,
        RTRIM(R.Estilo) AS Estilo,
        RTRIM(R.EstiloPropio) AS EstiloPropio,
        RTRIM(R.Item) AS Item,
        RTRIM(R.Combo) AS Combo,
        R.Ubicacion,
        R.Tecnica,
        R.OperarioUDP,
        CONVERT(VARCHAR, R.FechaRegistro, 103) AS FechaRegistro
    FROM AGR_Recetas R (NOLOCK)
    WHERE ISNULL(R.NP, '') <> ''
      -- Condición 1: NP debe tener exactamente 5 caracteres (dígitos)
      AND LEN(RTRIM(LTRIM(R.NP))) = 5
      -- Condición 2: No debe existir ya una fórmula activa (no eliminada) con la misma combinación NP+Combo+Item
      AND NOT EXISTS (
          SELECT 1 FROM LIQ_Formulas F (NOLOCK)
          WHERE F.NP = R.NP
            AND ISNULL(F.Combo, '') = ISNULL(RTRIM(R.Combo), '')
            AND ISNULL(F.Item, '') = ISNULL(RTRIM(R.Item), '')
            AND F.Eliminado = 0
      )
    ORDER BY R.IdRecetas DESC
END
GO

-- 2.7 Cambiar Estado de Fórmula (Cerrar/Abrir)
IF OBJECT_ID('dbo.LIQ_SP_CambiarEstadoFormula', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_CambiarEstadoFormula;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_CambiarEstadoFormula]
    @IdFormula  INT,
    @Usuario    VARCHAR(100),
    @Accion     INT, -- 1 = Cerrar, 0 = Abrir
    @Comentario VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @Accion = 1
        BEGIN
            UPDATE LIQ_Formulas SET
                Estado = 'Cerrada',
                UsuarioCierre = @Usuario,
                FechaCierre = CONVERT(VARCHAR(10), GETDATE(), 103) + ' ' + CONVERT(VARCHAR(8), GETDATE(), 108),
                UsuarioApertura = NULL,
                FechaApertura = NULL,
                ComentarioAbrir = NULL,
                ComentarioCerrar = @Comentario
            WHERE IdFormula = @IdFormula;
        END
        ELSE
        BEGIN
            UPDATE LIQ_Formulas SET
                Estado = 'Activa',
                UsuarioApertura = @Usuario,
                FechaApertura = CONVERT(VARCHAR(10), GETDATE(), 103) + ' ' + CONVERT(VARCHAR(8), GETDATE(), 108),
                UsuarioCierre = NULL,
                FechaCierre = NULL,
                ComentarioAbrir = @Comentario,
                ComentarioCerrar = NULL
            WHERE IdFormula = @IdFormula;
        END

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Estado actualizado correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO

PRINT '====================================================='
PRINT 'Script LIQ completado exitosamente.'
PRINT '====================================================='
GO
