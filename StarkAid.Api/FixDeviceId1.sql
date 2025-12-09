-- Script para remover a coluna DeviceId1 da tabela Agendamentos
-- Execute este script diretamente no SQL Server Management Studio ou Azure Data Studio

USE [SeuDatabaseName]; -- Substitua pelo nome do seu banco de dados
GO

-- Remove foreign key se existir
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Agendamentos_Devices_DeviceId1')
BEGIN
    ALTER TABLE [Agendamentos] DROP CONSTRAINT [FK_Agendamentos_Devices_DeviceId1];
    PRINT 'Foreign key FK_Agendamentos_Devices_DeviceId1 removida.';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_Agendamentos_Devices_DeviceId1 não encontrada.';
END
GO

-- Remove index se existir
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Agendamentos_DeviceId1' AND object_id = OBJECT_ID('Agendamentos'))
BEGIN
    DROP INDEX [IX_Agendamentos_DeviceId1] ON [Agendamentos];
    PRINT 'Index IX_Agendamentos_DeviceId1 removido.';
END
ELSE
BEGIN
    PRINT 'Index IX_Agendamentos_DeviceId1 não encontrado.';
END
GO

-- Remove coluna se existir
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Agendamentos') AND name = 'DeviceId1')
BEGIN
    ALTER TABLE [Agendamentos] DROP COLUMN [DeviceId1];
    PRINT 'Coluna DeviceId1 removida.';
END
ELSE
BEGIN
    PRINT 'Coluna DeviceId1 não encontrada.';
END
GO

PRINT 'Script executado com sucesso!';
GO

