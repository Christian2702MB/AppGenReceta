-- =======================================================================
-- MODULO: LIQUIDACION DE INSUMOS DE ESTAMPADO
-- MAQUINA DE ESTADOS: SPs DE VALIDACION Y TRANSICION
-- =======================================================================

USE [HIALPESA]
GO

-- 1. SP PRINCIPAL: AVANZAR ESTADO DE UNA NP CON VALIDACION
IF OBJECT_ID('dbo.LIQ_SP_AvanzarEstadoNP', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_AvanzarEstadoNP;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_AvanzarEstadoNP]
    @NP VARCHAR(20),
    @NuevoEstado VARCHAR(20),
    @Usuario VARCHAR(100),
    @Resultado INT OUTPUT,
    @Mensaje VARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EstadoActual VARCHAR(20);
    DECLARE @IdFormula INT;

    -- Obtener estado actual
    SELECT TOP 1 @EstadoActual = Estado, @IdFormula = IdFormula
    FROM LIQ_Formulas
    WHERE NP = @NP AND Eliminado = 0;

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
        WHERE F.NP = @NP AND F.Eliminado = 0
          AND NOT EXISTS (
              SELECT 1 FROM LIQ_OperacionesDetalle OD
              WHERE OD.NP = @NP AND OD.CodInsumo = FI.CodigoInsumo AND OD.TipoOperacion = 'Consumo'
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
        WHERE F.NP = @NP AND F.Eliminado = 0
          AND NOT EXISTS (
              SELECT 1 FROM LIQ_MER_MermasColor MC
              WHERE MC.NP = @NP AND MC.NombreColor = FC.NombreColor
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
        WHERE F.NP = @NP AND F.Eliminado = 0
          AND NOT EXISTS (
              SELECT 1 FROM LIQ_OperacionesDetalle OD
              WHERE OD.NP = @NP AND OD.CodInsumo = FI.CodigoInsumo AND OD.TipoOperacion = 'Ajuste'
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

-- 2. SP: REGISTRAR NO MERMA GLOBAL (para todos los colores sin merma)
IF OBJECT_ID('dbo.LIQ_SP_RegistrarNoMermaGlobal', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_RegistrarNoMermaGlobal;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_RegistrarNoMermaGlobal]
    @NP VARCHAR(20),
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO LIQ_MER_MermasColor (IdFormulaColor, NP, NombreColor, Gramos, FechaVencimiento, CodigoMerma, FechaRegistro, UsuarioRegistro)
    SELECT 
        FC.IdFormulaColor,
        @NP,
        FC.NombreColor,
        0,
        NULL,
        'NO_MERMA',
        GETDATE(),
        @Usuario
    FROM LIQ_FormulaColores FC
    INNER JOIN LIQ_Formulas F ON FC.IdFormula = F.IdFormula
    WHERE F.NP = @NP AND F.Eliminado = 0
      AND NOT EXISTS (
          SELECT 1 FROM LIQ_MER_MermasColor MC
          WHERE MC.NP = @NP AND MC.NombreColor = FC.NombreColor
      );
END
GO

-- 3. SP: REGISTRAR NO AJUSTE GLOBAL (para todos los insumos sin ajuste)
IF OBJECT_ID('dbo.LIQ_SP_RegistrarNoAjusteGlobal', 'P') IS NOT NULL DROP PROCEDURE dbo.LIQ_SP_RegistrarNoAjusteGlobal;
GO
CREATE PROCEDURE [dbo].[LIQ_SP_RegistrarNoAjusteGlobal]
    @NP VARCHAR(20),
    @Usuario VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO LIQ_OperacionesDetalle (NP, CodInsumo, TipoOperacion, Cantidad, Motivo, MermaReutilizada, LoteVencimiento, FechaRegistro, UsuarioRegistro, FuenteConsumo, NombreColor)
    SELECT 
        @NP,
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
    WHERE F.NP = @NP AND F.Eliminado = 0
      AND NOT EXISTS (
          SELECT 1 FROM LIQ_OperacionesDetalle OD
          WHERE OD.NP = @NP AND OD.CodInsumo = FI.CodigoInsumo AND OD.TipoOperacion = 'Ajuste'
      );
END
GO
