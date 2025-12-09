-- Migration manual para criar tabela de notificações
-- Execute este script no banco de dados

-- Criar tabela Notifications
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [Tipo] nvarchar(100) NOT NULL,
        [Titulo] nvarchar(500) NOT NULL,
        [Mensagem] nvarchar(max) NOT NULL,
        [UserId] uniqueidentifier NULL,
        [UserEmail] nvarchar(200) NULL,
        [UserName] nvarchar(200) NULL,
        [Valor] decimal(18,2) NULL,
        [ReferenciaId] nvarchar(100) NULL,
        [Lida] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [LidaEm] datetimeoffset NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
    
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
    CREATE INDEX [IX_Notifications_Lida] ON [Notifications] ([Lida]);
    CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]);
END
