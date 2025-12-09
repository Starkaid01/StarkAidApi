-- Migration: AddEstadoCidadeBairroToUser
-- Execute este script diretamente no banco de dados SQL Server
-- IMPORTANTE: Execute este script ANTES de usar a aplicação

USE [SeuBancoDeDados]; -- Substitua pelo nome do seu banco de dados
GO

-- Verificar e adicionar coluna Estado
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Users]') 
               AND name = 'Estado')
BEGIN
    ALTER TABLE [Users] ADD [Estado] nvarchar(100) NULL;
    PRINT 'Coluna Estado adicionada com sucesso.';
END
ELSE
BEGIN
    PRINT 'Coluna Estado já existe.';
END
GO

-- Verificar e adicionar coluna Cidade
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Users]') 
               AND name = 'Cidade')
BEGIN
    ALTER TABLE [Users] ADD [Cidade] nvarchar(100) NULL;
    PRINT 'Coluna Cidade adicionada com sucesso.';
END
ELSE
BEGIN
    PRINT 'Coluna Cidade já existe.';
END
GO

-- Verificar e adicionar coluna Bairro
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Users]') 
               AND name = 'Bairro')
BEGIN
    ALTER TABLE [Users] ADD [Bairro] nvarchar(100) NULL;
    PRINT 'Coluna Bairro adicionada com sucesso.';
END
ELSE
BEGIN
    PRINT 'Coluna Bairro já existe.';
END
GO

-- Registrar a migration na tabela __EFMigrationsHistory (se necessário)
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] 
               WHERE [MigrationId] = '20251206000000_AddEstadoCidadeBairroToUser')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251206000000_AddEstadoCidadeBairroToUser', '8.0.5');
    PRINT 'Migration registrada na tabela __EFMigrationsHistory.';
END
ELSE
BEGIN
    PRINT 'Migration já está registrada.';
END
GO

PRINT 'Script executado com sucesso!';
GO
