-- Migration manual para criar tabelas de suporte
-- Execute este script no banco de dados se a migration não foi aplicada automaticamente

-- Criar tabela ResolvendoSuportes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ResolvendoSuportes')
BEGIN
    CREATE TABLE [ResolvendoSuportes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [Origem] nvarchar(20) NOT NULL,
        [Ativo] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ResolvidoEm] datetimeoffset NULL,
        CONSTRAINT [PK_ResolvendoSuportes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ResolvendoSuportes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_ResolvendoSuportes_UserId] ON [ResolvendoSuportes] ([UserId]);
END

-- Criar tabela SuporteAcoes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SuporteAcoes')
BEGIN
    CREATE TABLE [SuporteAcoes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [Origem] nvarchar(20) NOT NULL,
        [Acao] nvarchar(100) NOT NULL,
        [Resposta] nvarchar(500) NULL,
        [Sucesso] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_SuporteAcoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SuporteAcoes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_SuporteAcoes_UserId] ON [SuporteAcoes] ([UserId]);
END

-- Criar tabela SuporteAprendizados
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SuporteAprendizados')
BEGIN
    CREATE TABLE [SuporteAprendizados] (
        [Id] int NOT NULL IDENTITY,
        [Problema] nvarchar(500) NOT NULL,
        [Solucoes] nvarchar(max) NOT NULL,
        [Origem] nvarchar(20) NOT NULL,
        [ContadorSucesso] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [LastUsedAt] datetimeoffset NULL,
        CONSTRAINT [PK_SuporteAprendizados] PRIMARY KEY ([Id])
    );
END

-- Criar tabela SuporteConversas
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SuporteConversas')
BEGIN
    CREATE TABLE [SuporteConversas] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Origem] nvarchar(20) NOT NULL,
        [ProblemaInicial] nvarchar(1000) NULL,
        [Mensagens] nvarchar(max) NULL,
        [ContadorMensagens] int NOT NULL,
        [ChatConcluido] bit NOT NULL,
        [Resolvido] bit NOT NULL,
        [LimiteAtingido] bit NOT NULL,
        [TransferidoParaHumano] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ConcluidoEm] datetimeoffset NULL,
        CONSTRAINT [PK_SuporteConversas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SuporteConversas_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_SuporteConversas_UserId] ON [SuporteConversas] ([UserId]);
END

-- Criar tabela SuportePerguntasFrequentes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SuportePerguntasFrequentes')
BEGIN
    CREATE TABLE [SuportePerguntasFrequentes] (
        [Id] int NOT NULL IDENTITY,
        [Pergunta] nvarchar(500) NOT NULL,
        [Resposta] nvarchar(max) NOT NULL,
        [SuporteToSoft] nvarchar(200) NULL,
        [SuporteToApp] nvarchar(200) NULL,
        [RequerAcao] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [LastUpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_SuportePerguntasFrequentes] PRIMARY KEY ([Id])
    );
END

-- Registrar a migration na tabela __EFMigrationsHistory
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250115000000_AddSuporteEntities')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20250115000000_AddSuporteEntities', '8.0.5');
END
