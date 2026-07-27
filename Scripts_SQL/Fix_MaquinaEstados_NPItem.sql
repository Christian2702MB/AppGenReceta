-- =========================================================================================
-- SCRIPT DE CORRECCIÓN: OPCIÓN A (SOPORTE PARA NP COMPUESTA "NP-ITEM" EN MÁQUINA DE ESTADOS)
-- =========================================================================================

-- 1. SP: AVANZAR ESTADO NP
IF OBJECT_ID('dbo.LIQ_SP_AvanzarEstadoNP', 'P') IS NULL 
    EXEC('CREATE PROCEDURE dbo.LIQ_SP_AvanzarEstadoNP AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE [dbo].[LIQ_SP_AvanzarEstadoNP]
    @NP VARCHAR(100),
    @NuevoEstado VARCHAR(20),
    @Usuario VARCHAR(100),
    @Resultado INT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EstadoActual VARCHAR(20);
    DECLARE @IdFormula INT;

    -- Obtener estado actual soportando NP pura o NP-Item
    SELECT TOP 1 @EstadoActual = Estado, @IdFormula = IdFormula
    FROM LIQ_Formulas
    WHERE (NP = @NP OR (NP + '-' + ISNULL(Item, '')) = @NP) AND Eliminado = 0;

    IF @IdFormula IS NULL
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'No se encontro la NP especificada.';
        RETURN;
    END

    -- Validar transicion permitida
    IF NOT (
        (@EstadoActual = 'Activa' AND @NuevoEstado = 'En Proceso') OR
        (@EstadoActual = 'En Proceso' AND @NuevoEstado = 'Pendiente') OR
        (@EstadoActual = 'Pendiente' AND @NuevoEstado = 'Liquidado')
    )
    BEGIN
        SET @Resultado = 0;
        SET @Mensaje = 'Transicion no permitida: de "' + @EstadoActual + '" a "' + @NuevoEstado + '".';
        RETURN;
    END

    -- VALIDACIONES SEGUN TRANSICION

    -- Activa -> En Proceso: Todos los insumos deben tener al menos un consumo
    IF @EstadoActual = 'Activa' AND @NuevoEstado = 'En Proceso'
    BEGIN
        DECLARE @InsumosSinConsumo INT;
        SELECT @InsumosSinConsumo = COUNT(*)
        FROM LIQ_FormulaInsumos FI
        INNER JOIN LIQ_FormulaColores FC ON FI.IdFormulaColor = FC.IdFormulaColor
        INNER JOIN LIQ_Formulas F ON FC.IdFormula = F.IdFormula
        WHERE (F.NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = @NP) AND F.Eliminado = 0
          AND NOT EXISTS (
              SELECT 1 FROM LIQ_OperacionesDetalle OD
              WHERE (OD.NP = @NP OR OD.NP = F.NP OR (OD.NP + '-' + ISNULL(OD.Item, '')) = @NP) AND OD.CodInsumo = FI.CodigoInsumo AND OD.TipoOperacion = 'Consumo'
          );

        IF @InsumosSinConsumo > 0
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Faltan ' + CAST(@InsumosSinConsumo AS VARCHAR) + ' insumo(s) sin consumo registrado. Registre todos los consumos antes de avanzar.';
            RETURN;
        END
    END

    -- En Proceso -> Pendiente: Todos los colores deben tener merma registrada
    IF @EstadoActual = 'En Proceso' AND @NuevoEstado = 'Pendiente'
    BEGIN
        DECLARE @ColoresSinMerma INT;
        SELECT @ColoresSinMerma = COUNT(*)
        FROM LIQ_FormulaColores FC
        INNER JOIN LIQ_Formulas F ON FC.IdFormula = F.IdFormula
        WHERE (F.NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = @NP) AND F.Eliminado = 0
          AND NOT EXISTS (
              SELECT 1 FROM LIQ_MER_MermasColor MC
              WHERE (MC.NP = @NP OR MC.NP = F.NP) AND MC.NombreColor = FC.NombreColor
          );

        IF @ColoresSinMerma > 0
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Faltan ' + CAST(@ColoresSinMerma AS VARCHAR) + ' color(es) sin merma registrada. Registre mermas o use "No existe merma global".';
            RETURN;
        END
    END

    -- Pendiente -> Liquidado: Todos los insumos deben tener ajuste registrado
    IF @EstadoActual = 'Pendiente' AND @NuevoEstado = 'Liquidado'
    BEGIN
        DECLARE @InsumosSinAjuste INT;
        SELECT @InsumosSinAjuste = COUNT(*)
        FROM LIQ_FormulaInsumos FI
        INNER JOIN LIQ_FormulaColores FC ON FI.IdFormulaColor = FC.IdFormulaColor
        INNER JOIN LIQ_Formulas F ON FC.IdFormula = F.IdFormula
        WHERE (F.NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = @NP) AND F.Eliminado = 0
          AND NOT EXISTS (
              SELECT 1 FROM LIQ_OperacionesDetalle OD
              WHERE (OD.NP = @NP OR OD.NP = F.NP OR (OD.NP + '-' + ISNULL(OD.Item, '')) = @NP) AND OD.CodInsumo = FI.CodigoInsumo AND OD.TipoOperacion = 'Ajuste'
          );

        IF @InsumosSinAjuste > 0
        BEGIN
            SET @Resultado = 0;
            SET @Mensaje = 'Faltan ' + CAST(@InsumosSinAjuste AS VARCHAR) + ' insumo(s) sin ajuste registrado. Registre ajustes o use "No existen ajustes globales".';
            RETURN;
        END
    END

    -- EJECUTAR TRANSICION
    UPDATE LIQ_Formulas
    SET Estado = @NuevoEstado,
        UsuarioModificacion = @Usuario,
        FechaModificacion = GETDATE()
    WHERE IdFormula = @IdFormula;

    -- Si el nuevo estado es Liquidado, registrar cierre
    IF @NuevoEstado = 'Liquidado'
    BEGIN
        UPDATE LIQ_Formulas
        SET UsuarioCierre = @Usuario,
            FechaCierre = CONVERT(VARCHAR(10), GETDATE(), 103),
            ComentarioCerrar = 'Liquidacion completada via maquina de estados'
        WHERE IdFormula = @IdFormula;
    END

    SET @Resultado = 1;
    SET @Mensaje = 'Estado actualizado exitosamente a "' + @NuevoEstado + '".';
END
GO

-- 2. SP: REGISTRAR NO MERMA GLOBAL
IF OBJECT_ID('dbo.LIQ_SP_RegistrarNoMermaGlobal', 'P') IS NULL 
    EXEC('CREATE PROCEDURE dbo.LIQ_SP_RegistrarNoMermaGlobal AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE [dbo].[LIQ_SP_RegistrarNoMermaGlobal]
    @NP VARCHAR(100),
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO LIQ_MER_MermasColor (IdFormulaColor, NP, NombreColor, Gramos, FechaVencimiento, CodigoMerma, FechaRegistro, UsuarioRegistro)
    SELECT 
        FC.IdFormulaColor,
        F.NP,
        FC.NombreColor,
        0,
        NULL,
        'NO_MERMA',
        GETDATE(),
        @Usuario
    FROM LIQ_FormulaColores FC
    INNER JOIN LIQ_Formulas F ON FC.IdFormula = F.IdFormula
    WHERE (F.NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = @NP) AND F.Eliminado = 0
      AND NOT EXISTS (
          SELECT 1 FROM LIQ_MER_MermasColor MC
          WHERE (MC.NP = @NP OR MC.NP = F.NP) AND MC.NombreColor = FC.NombreColor
      );
END
GO

-- 3. SP: REGISTRAR NO AJUSTE GLOBAL
IF OBJECT_ID('dbo.LIQ_SP_RegistrarNoAjusteGlobal', 'P') IS NULL 
    EXEC('CREATE PROCEDURE dbo.LIQ_SP_RegistrarNoAjusteGlobal AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE [dbo].[LIQ_SP_RegistrarNoAjusteGlobal]
    @NP VARCHAR(100),
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO LIQ_OperacionesDetalle (NP, Item, CodInsumo, TipoOperacion, Cantidad, Motivo, MermaReutilizada, LoteVencimiento, FechaRegistro, UsuarioRegistro, FuenteConsumo, NombreColor)
    SELECT 
        F.NP,
        ISNULL(F.Item, '0000'),
        FI.CodigoInsumo,
        'Ajuste',
        0,
        'No existe ajuste',
        '',
        '',
        GETDATE(),
        @Usuario,
        NULL,
        FC.NombreColor
    FROM LIQ_FormulaInsumos FI
    INNER JOIN LIQ_FormulaColores FC ON FI.IdFormulaColor = FC.IdFormulaColor
    INNER JOIN LIQ_Formulas F ON FC.IdFormula = F.IdFormula
    WHERE (F.NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = @NP) AND F.Eliminado = 0
      AND NOT EXISTS (
          SELECT 1 FROM LIQ_OperacionesDetalle OD
          WHERE (OD.NP = @NP OR OD.NP = F.NP OR (OD.NP + '-' + ISNULL(OD.Item, '')) = @NP) AND OD.CodInsumo = FI.CodigoInsumo AND OD.TipoOperacion = 'Ajuste'
      );
END
GO

-- 4. SP: REGISTRAR OPERACION
IF OBJECT_ID('dbo.LIQ_SP_RegistrarOperacion', 'P') IS NULL 
    EXEC('CREATE PROCEDURE dbo.LIQ_SP_RegistrarOperacion AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE [dbo].[LIQ_SP_RegistrarOperacion]
    @NP VARCHAR(100),
    @CodInsumo VARCHAR(50),
    @TipoOperacion VARCHAR(20),
    @Cantidad DECIMAL(18,4),
    @Motivo VARCHAR(250),
    @MermaReutilizada VARCHAR(50),
    @Usuario VARCHAR(100),
    @FuenteConsumo VARCHAR(50),
    @NombreColor VARCHAR(100) = NULL,
    @IdVisita INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @RealNP VARCHAR(100) = @NP;
        DECLARE @RealItem VARCHAR(100) = '0000';

        IF @NP <> 'STOCK-DIR' AND CHARINDEX('-', @NP) > 0
        BEGIN
            -- Buscar primero por coincidencia exacta en LIQ_Formulas
            SELECT TOP 1 @RealNP = F.NP, @RealItem = ISNULL(F.Item, '0000')
            FROM LIQ_Formulas F
            WHERE (F.NP + '-' + ISNULL(F.Item, '')) = @NP AND F.Eliminado = 0;

            -- Si no se encontró en formulas (ej. ajuste o consumo en NP externa/manual), hacer split por guión
            IF @@ROWCOUNT = 0
            BEGIN
                SET @RealNP = LEFT(@NP, CHARINDEX('-', @NP) - 1);
                SET @RealItem = SUBSTRING(@NP, CHARINDEX('-', @NP) + 1, LEN(@NP));
            END
        END
        ELSE IF @NP <> 'STOCK-DIR'
        BEGIN
            SELECT TOP 1 @RealItem = ISNULL(Item, '0000') FROM LIQ_Formulas WHERE NP = @NP AND Eliminado = 0;
        END

        -- Insertar en tabla de operaciones
        INSERT INTO LIQ_OperacionesDetalle (NP, Item, CodInsumo, TipoOperacion, Cantidad, Motivo, MermaReutilizada, FechaRegistro, UsuarioRegistro, FuenteConsumo, NombreColor, IdVisita)
        VALUES (@RealNP, @RealItem, @CodInsumo, @TipoOperacion, @Cantidad, @Motivo, @MermaReutilizada, GETDATE(), @Usuario, @FuenteConsumo, @NombreColor, @IdVisita);

        -- Lógica Específica por Tipo
        IF @TipoOperacion = 'Merma'
        BEGIN
            -- Generar Código de Merma (Ej. MER-2405-01)
            DECLARE @CodMerma VARCHAR(50) = 'MER-' + RIGHT(CONVERT(VARCHAR(4), YEAR(GETDATE())), 2) + RIGHT('0' + CONVERT(VARCHAR(2), MONTH(GETDATE())), 2) + '-' + RIGHT('000' + CONVERT(VARCHAR(4), SCOPE_IDENTITY()), 4);
            
            DECLARE @DescInsumo VARCHAR(250) = '';
            DECLARE @Tecnica VARCHAR(100) = '';
            
            SELECT TOP 1 @DescInsumo = Descripcion FROM LIQ_FormulaInsumos WHERE CodigoInsumo = @CodInsumo;
            SELECT TOP 1 @Tecnica = Tecnica FROM LIQ_Formulas WHERE (NP = @NP OR (NP + '-' + ISNULL(Item, '')) = @NP) AND Eliminado = 0;

            INSERT INTO LIQ_MermasStock (CodigoMerma, NPOrigen, Item, CodInsumo, Descripcion, Tecnica, CantidadOriginal, CantidadDisponible, FechaVencimiento)
            VALUES (@CodMerma, @RealNP, @RealItem, @CodInsumo, 'Merma de ' + ISNULL(@DescInsumo, @CodInsumo), @Tecnica, @Cantidad, @Cantidad, DATEADD(MONTH, 3, GETDATE()));
        END
        ELSE IF @TipoOperacion = 'Ingreso' AND ISNULL(@MermaReutilizada, '') <> ''
        BEGIN
            -- Descontar de stock de mermas
            UPDATE LIQ_MermasStock
            SET CantidadDisponible = CantidadDisponible - @Cantidad
            WHERE CodigoMerma = @MermaReutilizada;

            -- Si llega a cero o menos, marcar como agotado
            UPDATE LIQ_MermasStock
            SET Estado = 'Agotado'
            WHERE CodigoMerma = @MermaReutilizada AND CantidadDisponible <= 0;
        END

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Operación registrada correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO

-- 5. SP: TERMINAR NP (FASE 5)
IF OBJECT_ID('dbo.LIQ_SP_TerminarNP_Fase5', 'P') IS NULL 
    EXEC('CREATE PROCEDURE dbo.LIQ_SP_TerminarNP_Fase5 AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE [dbo].[LIQ_SP_TerminarNP_Fase5]
    @NP VARCHAR(100),
    @DestinoGlobal VARCHAR(100),
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Insertar devoluciones masivas para cada insumo con Saldo > 0
        IF ISNULL(@DestinoGlobal, '') <> ''
        BEGIN
            INSERT INTO LIQ_OperacionesDetalle (NP, Item, CodInsumo, NombreColor, TipoOperacion, Cantidad, Motivo, FechaRegistro, UsuarioRegistro)
            SELECT 
                F.NP, 
                ISNULL(F.Item, '0000'),
                I.CodigoInsumo, 
                (SELECT TOP 1 NombreColor FROM LIQ_FormulaColores WHERE IdFormula = F.IdFormula),
                'Devolucion', 
                (
                    -- Stock Recibido
                    ISNULL((SELECT SUM(D.CantidadRecibida * 1000.00) FROM LIQ_REQ_Recepciones R INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento WHERE (R.CodOrdPro = F.NP OR R.CodOrdPro = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = R.CodOrdPro) AND D.CodInsumo = I.CodigoInsumo), 0)
                    -
                    -- Consumo
                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE (NP = F.NP OR NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = NP) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                    -
                    -- Ajuste
                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE (NP = F.NP OR NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = NP) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                    -
                    -- Devoluciones Previas
                    ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE (NP = F.NP OR NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = NP) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0)
                ) AS SaldoPendiente,
                @DestinoGlobal,
                GETDATE(),
                @Usuario
            FROM LIQ_Formulas F
            INNER JOIN LIQ_FormulaColores C ON F.IdFormula = C.IdFormula
            INNER JOIN LIQ_FormulaInsumos I ON C.IdFormulaColor = I.IdFormulaColor
            WHERE (F.NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = @NP)
              AND F.Eliminado = 0
            GROUP BY F.NP, F.Item, I.CodigoInsumo, F.IdFormula
            HAVING (
                ISNULL((SELECT SUM(D.CantidadRecibida * 1000.00) FROM LIQ_REQ_Recepciones R INNER JOIN LIQ_REQ_RecepcionesDetalle D ON R.NumRequerimiento = D.NumRequerimiento WHERE (R.CodOrdPro = F.NP OR R.CodOrdPro = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = R.CodOrdPro) AND D.CodInsumo = I.CodigoInsumo), 0)
                - ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE (NP = F.NP OR NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = NP) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Consumo' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                - ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE (NP = F.NP OR NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = NP) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Ajuste' AND FuenteConsumo = 'Solicitud Realizada'), 0)
                - ISNULL((SELECT SUM(Cantidad) FROM LIQ_OperacionesDetalle WHERE (NP = F.NP OR NP = @NP OR (F.NP + '-' + ISNULL(F.Item, '')) = NP) AND CodInsumo = I.CodigoInsumo AND TipoOperacion = 'Devolucion'), 0)
            ) > 0;
        END

        -- 2. Cambiar Estado a Terminado (Fase 5)
        UPDATE LIQ_Formulas
        SET Estado = 'Terminado'
        WHERE (NP = @NP OR (NP + '-' + ISNULL(Item, '')) = @NP) AND Eliminado = 0;

        COMMIT TRANSACTION;
        SELECT 1 AS Exito, 'Liquidación terminada y saldos devueltos a ' + @DestinoGlobal AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SELECT 0 AS Exito, ERROR_MESSAGE() AS Mensaje;
    END CATCH
END
GO
