IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [ApiKey] nvarchar(100) NOT NULL,
    [StarkCoins] decimal(18,2) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    [Role] nvarchar(50) NOT NULL,
    [PreapprovalId] nvarchar(100) NULL,
    [UltimoPagamentoConfirmadoEm] datetimeoffset NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [WebhookLogs] (
    [Id] int NOT NULL IDENTITY,
    [DataRecebida] datetime2 NOT NULL,
    [Tipo] nvarchar(max) NOT NULL,
    [Acao] nvarchar(max) NOT NULL,
    [DataId] nvarchar(max) NOT NULL,
    [JsonDetalhado] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_WebhookLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Assinaturas] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [StripeCustomerId] nvarchar(100) NULL,
    [StripeSubscriptionId] nvarchar(100) NULL,
    [Status] nvarchar(50) NOT NULL,
    [Valor] decimal(18,2) NOT NULL,
    [IniciadaEm] datetimeoffset NULL,
    [CanceladaEm] datetimeoffset NULL,
    [ExpiraEm] datetimeoffset NULL,
    [PagamentoConfirmadoEm] datetimeoffset NULL,
    [DataCriacao] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Assinaturas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assinaturas_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ComandosSociais] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Comando] nvarchar(max) NOT NULL,
    [Resposta] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ComandosSociais] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComandosSociais_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Devices] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [ApiKey] nvarchar(100) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [MqttTopic] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Devices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Devices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [DispositivosDisparo] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Nome] nvarchar(150) NOT NULL,
    [MqttTopic] nvarchar(200) NOT NULL,
    [StatusTopic] nvarchar(200) NOT NULL,
    [DataCadastro] datetimeoffset NOT NULL,
    CONSTRAINT [PK_DispositivosDisparo] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DispositivosDisparo_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [FirebaseTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [DataCadastro] datetimeoffset NOT NULL,
    CONSTRAINT [PK_FirebaseTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FirebaseTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PasswordResetTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [Expiration] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [Expiration] datetimeoffset NOT NULL,
    [IsRevoked] bit NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Agendamentos] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [DeviceId] uniqueidentifier NOT NULL,
    [AgendadoPara] datetimeoffset NOT NULL,
    [Comando] nvarchar(max) NOT NULL,
    [Executado] bit NOT NULL,
    [Recorrencia] nvarchar(max) NULL,
    CONSTRAINT [PK_Agendamentos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Agendamentos_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Agendamentos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Disparos] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [DispositivoId] uniqueidentifier NOT NULL,
    [DisparadoEm] datetimeoffset NOT NULL,
    [Mensagem] nvarchar(max) NOT NULL,
    [Confirmado] bit NOT NULL,
    [ConfirmadoEm] datetimeoffset NULL,
    CONSTRAINT [PK_Disparos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Disparos_DispositivosDisparo_DispositivoId] FOREIGN KEY ([DispositivoId]) REFERENCES [DispositivosDisparo] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Disparos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_Agendamentos_DeviceId] ON [Agendamentos] ([DeviceId]);
GO

CREATE INDEX [IX_Agendamentos_UserId] ON [Agendamentos] ([UserId]);
GO

CREATE INDEX [IX_Assinaturas_UserId] ON [Assinaturas] ([UserId]);
GO

CREATE INDEX [IX_ComandosSociais_UserId] ON [ComandosSociais] ([UserId]);
GO

CREATE INDEX [IX_Devices_UserId] ON [Devices] ([UserId]);
GO

CREATE INDEX [IX_Disparos_DispositivoId] ON [Disparos] ([DispositivoId]);
GO

CREATE INDEX [IX_Disparos_UserId] ON [Disparos] ([UserId]);
GO

CREATE INDEX [IX_DispositivosDisparo_UserId] ON [DispositivosDisparo] ([UserId]);
GO

CREATE INDEX [IX_FirebaseTokens_UserId] ON [FirebaseTokens] ([UserId]);
GO

CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
GO

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250806204023_InitialCreate', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250808000322_AddStripeAssinaturaIntegration', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [SpotifyAccessToken] nvarchar(500) NULL;
GO

ALTER TABLE [Users] ADD [SpotifyRefreshToken] nvarchar(500) NULL;
GO

ALTER TABLE [Users] ADD [SpotifyTokenExpiresAt] datetimeoffset NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250823053544_AddSpotifyFieldsToUser', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [MinutosReconhecidos] float NOT NULL DEFAULT 0.0E0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250918171913_AddMinutosReconhecidosToUser', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [RefreshTokens] ADD [Origem] nvarchar(max) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250930014458_AddOrigemToRefreshTokens', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [IaHistoricos] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TextoUsuario] nvarchar(max) NOT NULL,
    [TextoIa] nvarchar(max) NOT NULL,
    [CriadoEm] datetimeoffset NOT NULL,
    CONSTRAINT [PK_IaHistoricos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IaHistoricos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_IaHistoricos_UserId] ON [IaHistoricos] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250930172831_AddIaHistorico', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ComandosSociais] ADD [RespostasAleatorias] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250930225128_AddRespostasAleatoriasToComandoSocial', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [WhatsAppSessionData] nvarchar(max) NULL;
GO

CREATE TABLE [ConfiguracoesSistema] (
    [Id] int NOT NULL IDENTITY,
    [DominioCloudflare] nvarchar(max) NOT NULL,
    [UltimaAtualizacao] datetime2 NOT NULL,
    CONSTRAINT [PK_ConfiguracoesSistema] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251008021434_ConfiguracoesSistema', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [UserSessions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [SessionName] nvarchar(100) NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_UserSessions_UserId] ON [UserSessions] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251010032935_AddUserSessions', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ConfiguracoesSistema] ADD [DominioNlp] nvarchar(max) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251016133319_AddDominioNlpToConfiguracaoSistema', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Assinaturas] ADD [StripePriceId] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251018190610_AddStripePriceIdToAssinatura', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [PagamentosAvulsos] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Valor] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [StripeSessionId] nvarchar(max) NOT NULL,
    [StripeCustomerId] nvarchar(max) NOT NULL,
    [DataCriacao] datetimeoffset NOT NULL,
    [PagamentoConfirmadoEm] datetimeoffset NULL,
    CONSTRAINT [PK_PagamentosAvulsos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PagamentosAvulsos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_PagamentosAvulsos_UserId] ON [PagamentosAvulsos] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251018193417_PagamentosAvulsos', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [LastUpdatedAt] datetimeoffset NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251018200740_AddLastUpdatedAtToUser', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [RemovalAds] nvarchar(50) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251018232538_RemovalAds', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Assinaturas] ADD [TipoPlano] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251019031150_TipoPlano', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ConfiguracoesStarkNlp] (
    [Id] uniqueidentifier NOT NULL,
    [StarkNlpUrl] nvarchar(500) NOT NULL,
    [DataAtualizacao] datetime2 NOT NULL,
    CONSTRAINT [PK_ConfiguracoesStarkNlp] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251029030626_AddConfiguracaoStarkNlp', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Devices] ADD [Comando] nvarchar(200) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251116011159_AddComandoToDevice', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Agendamentos] DROP CONSTRAINT [FK_Agendamentos_Users_UserId];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Assinaturas]') AND [c].[name] = N'StripePriceId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Assinaturas] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Assinaturas] ALTER COLUMN [StripePriceId] nvarchar(100) NULL;
GO

CREATE TABLE [DispositivosEsp] (
    [Id] uniqueidentifier NOT NULL,
    [Nome] nvarchar(150) NOT NULL,
    [Ip] nvarchar(45) NOT NULL,
    [Porta] int NOT NULL,
    [Comando] nvarchar(200) NULL,
    [Status] nvarchar(50) NOT NULL,
    [LigadoDesligado] bit NOT NULL,
    [UserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [LastPingAt] datetimeoffset NULL,
    [LastUpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_DispositivosEsp] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DispositivosEsp_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_DispositivosEsp_UserId] ON [DispositivosEsp] ([UserId]);
GO

ALTER TABLE [Agendamentos] ADD CONSTRAINT [FK_Agendamentos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251201023330_AddDispositivoEsp', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Licenses] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [LicenseKey] nvarchar(100) NOT NULL,
    [MaxMachines] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [ExpiresAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    [StripeSessionId] nvarchar(100) NULL,
    [StripePaymentIntentId] nvarchar(100) NULL,
    [PaymentConfirmedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Licenses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Licenses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [LicenseActivations] (
    [Id] uniqueidentifier NOT NULL,
    [LicenseId] uniqueidentifier NOT NULL,
    [MachineId] nvarchar(200) NOT NULL,
    [MachineName] nvarchar(200) NULL,
    [ActivatedAt] datetimeoffset NOT NULL,
    [DeactivatedAt] datetimeoffset NULL,
    [IsActive] bit NOT NULL,
    [IpAddress] nvarchar(50) NULL,
    CONSTRAINT [PK_LicenseActivations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LicenseActivations_Licenses_LicenseId] FOREIGN KEY ([LicenseId]) REFERENCES [Licenses] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_LicenseActivations_LicenseId] ON [LicenseActivations] ([LicenseId]);
GO

CREATE INDEX [IX_Licenses_UserId] ON [Licenses] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251201162632_AddLicenseTables', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [DispositivosEsp] ADD [ComandToEsp] nvarchar(200) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251203033826_AddComandToEspToDispositivoEsp', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Agendamentos] DROP CONSTRAINT [FK_Agendamentos_Devices_DeviceId];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agendamentos]') AND [c].[name] = N'DeviceId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Agendamentos] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Agendamentos] ALTER COLUMN [DeviceId] uniqueidentifier NULL;
GO

ALTER TABLE [Agendamentos] ADD CONSTRAINT [FK_Agendamentos_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251203041656_AddTipoAgendamentoAndDispositivoEspIdToAgendamento', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Agendamentos_Devices_DeviceId1')
                BEGIN
                    ALTER TABLE [Agendamentos] DROP CONSTRAINT [FK_Agendamentos_Devices_DeviceId1];
                END
            
GO


                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Agendamentos_DeviceId1' AND object_id = OBJECT_ID('Agendamentos'))
                BEGIN
                    DROP INDEX [IX_Agendamentos_DeviceId1] ON [Agendamentos];
                END
            
GO


                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Agendamentos') AND name = 'DeviceId1')
                BEGIN
                    ALTER TABLE [Agendamentos] DROP COLUMN [DeviceId1];
                END
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251203044544_AddTipoAgendamentoAndDispositivoEspIdToAgendamentoFixed', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251203050132_RemoveDeviceId1ShadowProperty', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [EwelinkAccounts] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [AccessToken] nvarchar(500) NOT NULL,
    [RefreshToken] nvarchar(500) NOT NULL,
    [AccessTokenExpiry] bigint NOT NULL,
    [RefreshTokenExpiry] bigint NOT NULL,
    [Region] nvarchar(50) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [LastUpdatedAt] datetimeoffset NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EwelinkAccounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EwelinkAccounts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [EwelinkDevices] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [DeviceId] nvarchar(100) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Type] int NOT NULL,
    [Uiid] int NOT NULL,
    [Params] nvarchar(max) NULL,
    [Online] bit NOT NULL,
    [FamilyId] nvarchar(100) NULL,
    [RoomId] nvarchar(100) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [LastUpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_EwelinkDevices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EwelinkDevices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_EwelinkAccounts_UserId] ON [EwelinkAccounts] ([UserId]);
GO

CREATE INDEX [IX_EwelinkDevices_UserId] ON [EwelinkDevices] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251205044302_AddEwelinkEntities', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Agendamentos] ADD [EwelinkDeviceId] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251205170000_AddEwelinkDeviceIdToAgendamento', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [UserSessions] ADD [LastActivityAt] datetime2 NULL;
GO

ALTER TABLE [UserSessions] ADD [Origem] nvarchar(50) NOT NULL DEFAULT N'web';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251207150703_AddOrigemAndLastActivityToUserSession', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ErrorLogsSoft] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [UltimoComando] nvarchar(max) NULL,
    [UltimaResposta] nvarchar(max) NULL,
    [UltimoDispositivoAcionado] nvarchar(max) NULL,
    [ErroCompleto] nvarchar(max) NULL,
    [CodigoDeErro] nvarchar(max) NULL,
    [DataErro] nvarchar(50) NOT NULL,
    [HoraErro] nvarchar(50) NOT NULL,
    [AcaoErro] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ErrorLogsSoft] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ErrorLogsSoft_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_ErrorLogsSoft_UserId] ON [ErrorLogsSoft] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251207170413_AddErrorLogsSoft', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ErrorCodeDescriptions] (
    [CodigoDeErro] nvarchar(50) NOT NULL,
    [Descricao] nvarchar(max) NOT NULL,
    [Contexto] nvarchar(max) NOT NULL,
    [CamposRelevantes] nvarchar(max) NOT NULL,
    [Origem] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_ErrorCodeDescriptions] PRIMARY KEY ([CodigoDeErro])
);
GO

CREATE TABLE [ErrorLogsApp] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [UltimoComando] nvarchar(max) NULL,
    [UltimaResposta] nvarchar(max) NULL,
    [UltimoDispositivoAcionado] nvarchar(max) NULL,
    [ErroCompleto] nvarchar(max) NULL,
    [CodigoDeErro] nvarchar(max) NULL,
    [DataErro] nvarchar(50) NOT NULL,
    [HoraErro] nvarchar(50) NOT NULL,
    [AcaoErro] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ErrorLogsApp] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ErrorLogsApp_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_ErrorLogsApp_UserId] ON [ErrorLogsApp] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251207195528_AddErrorLogsAppAndErrorCodeDescriptions', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ErrorCodeDescriptions] ADD [Solucoes] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251207212540_AddSolucoesToErrorCodeDescription', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [UserActivities] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [Origem] nvarchar(20) NOT NULL,
    [UltimoComandoEsp] nvarchar(max) NULL,
    [UltimoComandoEwelink] nvarchar(max) NULL,
    [UltimoComandoStarkSwitch] nvarchar(max) NULL,
    [UltimoComandoSocial] nvarchar(max) NULL,
    [UltimaRespostaSocial] nvarchar(max) NULL,
    [UltimoComandoIA] nvarchar(max) NULL,
    [UltimaRespostaIA] nvarchar(max) NULL,
    [LastUpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UserActivities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserActivities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_UserActivities_UserId] ON [UserActivities] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251210151138_AddUserActivityTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [LogsFalhasSoft] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [TipoFalha] nvarchar(500) NOT NULL,
    [Descricao] nvarchar(1000) NULL,
    [ComandoTentado] nvarchar(500) NULL,
    [DispositivoNome] nvarchar(500) NULL,
    [ErroDetalhado] nvarchar(200) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_LogsFalhasSoft] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LogsFalhasSoft_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_LogsFalhasSoft_UserId] ON [LogsFalhasSoft] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251210153329_AddLogsFalhasSoftTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


IF COL_LENGTH('Users','PlanType') IS NULL
    ALTER TABLE Users ADD PlanType INT NOT NULL CONSTRAINT DF_Users_PlanType DEFAULT(0);
IF COL_LENGTH('Users','StarkCoins') IS NULL
    ALTER TABLE Users ADD StarkCoins INT NOT NULL CONSTRAINT DF_Users_StarkCoins DEFAULT(0);
IF COL_LENGTH('Users','TokensConsumidosSemana') IS NULL
    ALTER TABLE Users ADD TokensConsumidosSemana INT NOT NULL CONSTRAINT DF_Users_TokensConsumidosSemana DEFAULT(0);

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StarkCoinPurchases' AND TABLE_SCHEMA = 'dbo')
BEGIN
    CREATE TABLE [dbo].[StarkCoinPurchases](
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [PackageType] INT NOT NULL,
        [StarkCoinsAmount] INT NOT NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [CreatedAt] DATETIMEOFFSET NOT NULL,
        CONSTRAINT FK_StarkCoinPurchases_Users_UserId FOREIGN KEY([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX IX_StarkCoinPurchases_UserId ON [dbo].[StarkCoinPurchases]([UserId]);
END

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251212210349_EnsureEconomyColumns', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Aprendizados] (
    [Id] uniqueidentifier NOT NULL,
    [Texto] nvarchar(max) NOT NULL,
    [Resposta] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Aprendizados] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251231015147_AddAprendizadoTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] ADD [Contexto] nvarchar(500) NULL;
GO

ALTER TABLE [Aprendizados] ADD [Tipo] nvarchar(50) NOT NULL DEFAULT N'';
GO

CREATE TABLE [UserConversaContexts] (
    [UserId] uniqueidentifier NOT NULL,
    [ContextoAtual] nvarchar(500) NOT NULL,
    [LastUpdatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UserConversaContexts] PRIMARY KEY ([UserId])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251231031941_UpdateAprendizadoWithContext', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] ADD [UserId] uniqueidentifier NULL;
GO

CREATE INDEX [IX_Aprendizados_UserId] ON [Aprendizados] ([UserId]);
GO

ALTER TABLE [Aprendizados] ADD CONSTRAINT [FK_Aprendizados_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260101190953_UpdateAprendizadoWithUserIdAndScope', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260101192206_MakeUserIdRequiredAndAddIndexes', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] ADD [ConfidenceScore] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Aprendizados] ADD [HitCount] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Aprendizados] ADD [LastUsedAt] datetimeoffset NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260101193241_AddConfidenceMetricsToAprendizado', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] ADD [Ativo] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260101193937_AddAtivoToAprendizado', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] ADD [EmQuarentena] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Aprendizados] ADD [QuarentenaDesde] datetimeoffset NULL;
GO

CREATE TABLE [GcExecutionLogs] (
    [Id] uniqueidentifier NOT NULL,
    [DataExecucao] datetimeoffset NOT NULL,
    [ItensInativados] int NOT NULL,
    [ItensEmQuarentena] int NOT NULL,
    [ItensRessuscitados] int NOT NULL,
    [DuracaoMs] bigint NOT NULL,
    [LogDetalhado] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_GcExecutionLogs] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260102002127_AddGcLogAndQuarantineFields', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] ADD [VariantesDistintasUsadas] int NOT NULL DEFAULT 0;
GO

CREATE TABLE [Telemetrias] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Origem] nvarchar(50) NOT NULL,
    [Evento] nvarchar(100) NOT NULL,
    [Categoria] nvarchar(50) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CriadoEm] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Telemetrias] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Telemetrias_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Telemetrias_UserId] ON [Telemetrias] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260103023930_AddVariantesToAprendizado', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AiInteractionEvents] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [UserHash] nvarchar(64) NULL,
    [TextoOriginal] nvarchar(max) NOT NULL,
    [TextoNormalizado] nvarchar(max) NOT NULL,
    [Resultado] nvarchar(50) NOT NULL,
    [SimilarityScore] float NULL,
    [AprendizadoTipo] nvarchar(50) NULL,
    [AprendizadoId] uniqueidentifier NULL,
    [LatenciaMs] int NOT NULL,
    [ChamouIaExterna] bit NOT NULL,
    [TokensEstimadosEvitados] int NOT NULL,
    [EconomiaUSD] decimal(18,2) NOT NULL,
    [Origem] nvarchar(50) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_AiInteractionEvents] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260103025923_AddAiInteractionTelemetry', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] ADD [UltimaRessurreicaoAt] datetimeoffset NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260103041554_AddUltimaRessurreicaoAt', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Aprendizados] DROP CONSTRAINT [FK_Aprendizados_Users_UserId];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Aprendizados]') AND [c].[name] = N'UserId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Aprendizados] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Aprendizados] ALTER COLUMN [UserId] uniqueidentifier NULL;
GO

CREATE TABLE [AprendizadoRespostas] (
    [Id] uniqueidentifier NOT NULL,
    [AprendizadoId] uniqueidentifier NOT NULL,
    [Texto] nvarchar(max) NOT NULL,
    [UsoCount] int NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_AprendizadoRespostas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AprendizadoRespostas_Aprendizados_AprendizadoId] FOREIGN KEY ([AprendizadoId]) REFERENCES [Aprendizados] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AprendizadoRespostas_AprendizadoId] ON [AprendizadoRespostas] ([AprendizadoId]);
GO

ALTER TABLE [Aprendizados] ADD CONSTRAINT [FK_Aprendizados_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260103054512_AddAprendizadoRespostas', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Piadas] (
    [Id] int NOT NULL IDENTITY,
    [Texto] nvarchar(max) NOT NULL,
    [Categoria] nvarchar(max) NOT NULL,
    [Ativa] bit NOT NULL,
    CONSTRAINT [PK_Piadas] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Receitas] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(max) NOT NULL,
    [Categoria] nvarchar(max) NOT NULL,
    [Ingredientes] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Receitas] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserFunStates] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [PiadasContadasIds] nvarchar(max) NOT NULL,
    [ReceitaAtualId] int NULL,
    [PassoAtual] int NOT NULL,
    [IniciouPassoAPasso] bit NOT NULL,
    [ReceitasVistasIds] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_UserFunStates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserFunStates_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ReceitaPassos] (
    [Id] int NOT NULL IDENTITY,
    [ReceitaId] int NOT NULL,
    [Ordem] int NOT NULL,
    [Descricao] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ReceitaPassos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReceitaPassos_Receitas_ReceitaId] FOREIGN KEY ([ReceitaId]) REFERENCES [Receitas] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Ativa', N'Categoria', N'Texto') AND [object_id] = OBJECT_ID(N'[Piadas]'))
    SET IDENTITY_INSERT [Piadas] ON;
INSERT INTO [Piadas] ([Id], [Ativa], [Categoria], [Texto])
VALUES (1, CAST(1 AS bit), N'Tecnologia', N'Por que o computador foi ao médico? Porque estava com vírus.'),
(2, CAST(1 AS bit), N'Geral', N'O que o zero disse para o oito? Que cinto bonito!'),
(3, CAST(1 AS bit), N'Escola', N'Por que o livro de matemática se suicidou? Porque tinha muitos problemas.'),
(4, CAST(1 AS bit), N'Geral', N'Qual é o cúmulo da força? Dobrar a esquina.'),
(5, CAST(1 AS bit), N'Tecnologia', N'O que uma impressora disse para a outra? Essa folha é sua ou é impressão minha?'),
(6, CAST(1 AS bit), N'Natureza', N'Por que a plantinha não foi ao médico? Porque só tinha médico de plantão.'),
(7, CAST(1 AS bit), N'Animais', N'O que o pato disse para a pata? Vem Quá!'),
(8, CAST(1 AS bit), N'Geral', N'Qual o pé que é mais rápido? O pé-ligeiro.'),
(9, CAST(1 AS bit), N'Natureza', N'Por que o pinheiro não se perde na floresta? Porque ele tem uma pinha.'),
(10, CAST(1 AS bit), N'Comida', N'O que o tomate foi fazer no banco? Tirar extrato.'),
(11, CAST(1 AS bit), N'Tecnologia', N'Qual é a tecla preferida do astronauta? A barra de espaço.'),
(12, CAST(1 AS bit), N'Animais', N'Por que o jacaré tirou o filho da escola? Porque ele réptil de ano.'),
(13, CAST(1 AS bit), N'Comida', N'Qual é o rei dos queijos? O Requeijão.'),
(14, CAST(1 AS bit), N'Geral', N'O que é um ponto verde na antártida? Um ping-green.'),
(15, CAST(1 AS bit), N'Profissões', N'Por que o bombeiro não gosta de andar? Porque ele socorre.'),
(16, CAST(1 AS bit), N'Animais', N'Qual é o animal que não vale mais nada? O javali.'),
(17, CAST(1 AS bit), N'Geral', N'O que o pagodeiro foi fazer na igreja? Cantar pá god.'),
(18, CAST(1 AS bit), N'Geral', N'Por que a velhinha não usa relógio? Porque ela é sem hora.'),
(19, CAST(1 AS bit), N'Herois', N'Como o Batman faz para entrar na Bat-caverna? Ele bat-palma.'),
(20, CAST(1 AS bit), N'Ciencia', N'Qual o doce preferido do átomo? Pé-de-moleculas.'),
(21, CAST(1 AS bit), N'Espaço', N'O que a Lua disse ao Sol? Nossa, você é tão grande e não te deixam sair à noite!'),
(22, CAST(1 AS bit), N'Ciencia', N'Por que as estrelas não fazem miau? Porque Astronomia.'),
(23, CAST(1 AS bit), N'Comida', N'O que a banana suicida falou? Macacos me mordam!'),
(24, CAST(1 AS bit), N'Geografia', N'Qual o estado que quer ser carro? Sergipe.'),
(25, CAST(1 AS bit), N'Charada', N'O que é, o que é: cai em pé e corre deitado? A chuva.'),
(26, CAST(1 AS bit), N'Geral', N'Em qual cidade o Thor mora? Valhalla? Não, Pousada.'),
(27, CAST(1 AS bit), N'Ciencia', N'Por que o elétron não foi à festa? Porque precisa ser positivo.'),
(28, CAST(1 AS bit), N'Animais', N'O que o advogado do frango foi fazer? Foi soltar a franga.'),
(29, CAST(1 AS bit), N'Animais', N'Qual a diferença entre o gato e a coca-cola? O gato faz miau e a coca-cola faz tshhh.'),
(30, CAST(1 AS bit), N'Ferramentas', N'O que o martelo foi fazer no culto? Pregador.');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Ativa', N'Categoria', N'Texto') AND [object_id] = OBJECT_ID(N'[Piadas]'))
    SET IDENTITY_INSERT [Piadas] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Categoria', N'Ingredientes', N'Nome') AND [object_id] = OBJECT_ID(N'[Receitas]'))
    SET IDENTITY_INSERT [Receitas] ON;
INSERT INTO [Receitas] ([Id], [Categoria], [Ingredientes], [Nome])
VALUES (1, N'Doces', N'3 cenouras, 4 ovos, 1 xícara de óleo, 2 xícaras de açúcar, 2 xícaras de farinha, 1 colher de fermento.', N'Bolo de Cenoura'),
(2, N'Salgados', N'2 ovos, sal a gosto, queijo, presunto, orégano.', N'Omelete Simples'),
(3, N'Acompanhamentos', N'1 xícara de arroz, 2 xícaras de água, alho, sal, óleo.', N'Arroz Branco'),
(4, N'Doces', N'1 lata de leite condensado, 4 colheres de chocolate em pó, 1 colher de manteiga, granulado.', N'Brigadeiro'),
(5, N'Bebidas', N'3 limões, 1 litro de água, açúcar ou adoçante a gosto, gelo.', N'Suco de Limão');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Categoria', N'Ingredientes', N'Nome') AND [object_id] = OBJECT_ID(N'[Receitas]'))
    SET IDENTITY_INSERT [Receitas] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Descricao', N'Ordem', N'ReceitaId') AND [object_id] = OBJECT_ID(N'[ReceitaPassos]'))
    SET IDENTITY_INSERT [ReceitaPassos] ON;
INSERT INTO [ReceitaPassos] ([Id], [Descricao], [Ordem], [ReceitaId])
VALUES (1, N'Descasque e corte as cenouras em rodelas.', 1, 1),
(2, N'No liquidificador, bata as cenouras, os ovos e o óleo.', 2, 1),
(3, N'Em uma tigela, misture o açúcar, a farinha e o fermento.', 3, 1),
(4, N'Despeje a mistura do liquidificador na tigela e mexa bem.', 4, 1),
(5, N'Unte uma forma e despeje a massa.', 5, 1),
(6, N'Asse em forno pré-aquecido a 180 graus por 40 minutos.', 6, 1),
(7, N'Quebre os ovos em um prato fundo.', 1, 2),
(8, N'Bata os ovos ligeiramente com um garfo.', 2, 2),
(9, N'Tempere com sal e orégano.', 3, 2),
(10, N'Aqueça uma frigideira com um pouco de óleo.', 4, 2),
(11, N'Despeje os ovos e adicione o queijo e presunto.', 5, 2),
(12, N'Dobre ao meio e deixe dourar dos dois lados.', 6, 2),
(13, N'Lave o arroz se desejar.', 1, 3),
(14, N'Aqueça o óleo e refogue o alho picado.', 2, 3),
(15, N'Adicione o arroz e refogue por um minuto.', 3, 3),
(16, N'Adicione a água fervente e o sal.', 4, 3),
(17, N'Cozinhe em fogo baixo com a panela semi-tampada.', 5, 3),
(18, N'Quando a água secar, desligue e deixe descansar.', 6, 3),
(19, N'Em uma panela, coloque o leite condensado.', 1, 4),
(20, N'Adicione o chocolate em pó e a manteiga.', 2, 4),
(21, N'Leve ao fogo baixo, mexendo sempre.', 3, 4),
(22, N'Mexa até desgrudar do fundo da panela.', 4, 4),
(23, N'Despeje em um prato untado e deixe esfriar.', 5, 4),
(24, N'Enrole as bolinhas e passe no granulado.', 6, 4),
(25, N'Lave bem os limões.', 1, 5),
(26, N'Corte os limões ao meio.', 2, 5),
(27, N'Esprema o suco dos limões em uma jarra.', 3, 5),
(28, N'Adicione a água e misture.', 4, 5),
(29, N'Adoce a gosto e mexa bem até dissolver.', 5, 5),
(30, N'Adicione gelo e sirva imediatamente.', 6, 5);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Descricao', N'Ordem', N'ReceitaId') AND [object_id] = OBJECT_ID(N'[ReceitaPassos]'))
    SET IDENTITY_INSERT [ReceitaPassos] OFF;
GO

CREATE INDEX [IX_ReceitaPassos_ReceitaId] ON [ReceitaPassos] ([ReceitaId]);
GO

CREATE UNIQUE INDEX [IX_UserFunStates_UserId] ON [UserFunStates] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260103095518_AddFunModule', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [YouTubeMusicCaches] (
    [Id] int NOT NULL IDENTITY,
    [NormalizedQuery] nvarchar(500) NOT NULL,
    [VideoId] nvarchar(50) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Channel] nvarchar(max) NULL,
    [DurationSeconds] int NOT NULL,
    [IsLive] bit NOT NULL,
    [Source] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [LastUsedAt] datetimeoffset NOT NULL,
    [HitCount] int NOT NULL,
    CONSTRAINT [PK_YouTubeMusicCaches] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_YouTubeMusicCaches_NormalizedQuery] ON [YouTubeMusicCaches] ([NormalizedQuery]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260104023540_AddYouTubeMusicCache', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ConfiguracoesSistema] ADD [DominioAudioResolver] nvarchar(max) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260105012831_AddDominioAudioResolver', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_YouTubeMusicCaches_NormalizedQuery] ON [YouTubeMusicCaches];
GO

ALTER TABLE [YouTubeMusicCaches] ADD [Kind] int NOT NULL DEFAULT 0;
GO

CREATE INDEX [IX_YouTubeMusicCaches_NormalizedQuery] ON [YouTubeMusicCaches] ([NormalizedQuery]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260105050555_AddKindToYouTubeMusicCache', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [MusicArtistAliases] (
    [Id] int NOT NULL IDENTITY,
    [Alias] nvarchar(200) NOT NULL,
    [Canonical] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_MusicArtistAliases] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260105072231_AddMusicArtistAlias', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Comodos] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Nome] nvarchar(150) NOT NULL,
    [CriadoEm] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Comodos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Comodos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ComodoDispositivos] (
    [ComodoId] uniqueidentifier NOT NULL,
    [DispositivoId] nvarchar(100) NOT NULL,
    [Tipo] nvarchar(50) NOT NULL,
    [Papel] nvarchar(50) NULL,
    CONSTRAINT [PK_ComodoDispositivos] PRIMARY KEY ([ComodoId], [DispositivoId]),
    CONSTRAINT [FK_ComodoDispositivos_Comodos_ComodoId] FOREIGN KEY ([ComodoId]) REFERENCES [Comodos] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [EscoposConversacionais] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ComodoId] uniqueidentifier NOT NULL,
    [ExpiraEm] datetimeoffset NOT NULL,
    [CriadoEm] datetimeoffset NOT NULL,
    CONSTRAINT [PK_EscoposConversacionais] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EscoposConversacionais_Comodos_ComodoId] FOREIGN KEY ([ComodoId]) REFERENCES [Comodos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EscoposConversacionais_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO

CREATE INDEX [IX_Comodos_UserId] ON [Comodos] ([UserId]);
GO

CREATE INDEX [IX_EscoposConversacionais_ComodoId] ON [EscoposConversacionais] ([ComodoId]);
GO

CREATE INDEX [IX_EscoposConversacionais_UserId] ON [EscoposConversacionais] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260105094737_AddComodosModule', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Devices] ADD [IsOn] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260105212139_AddIsOnToDevice', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Rotinas] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Nome] nvarchar(150) NOT NULL,
    [Descricao] nvarchar(300) NULL,
    [Ativa] bit NOT NULL,
    [CriadaEm] datetimeoffset NOT NULL,
    [AtualizadaEm] datetimeoffset NOT NULL,
    CONSTRAINT [PK_Rotinas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rotinas_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RotinaAcoes] (
    [Id] uniqueidentifier NOT NULL,
    [RotinaId] uniqueidentifier NOT NULL,
    [OrdemExecucao] int NOT NULL,
    [Tipo] int NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_RotinaAcoes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RotinaAcoes_Rotinas_RotinaId] FOREIGN KEY ([RotinaId]) REFERENCES [Rotinas] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RotinaGatilhos] (
    [Id] uniqueidentifier NOT NULL,
    [RotinaId] uniqueidentifier NOT NULL,
    [Tipo] int NOT NULL,
    [Expressao] nvarchar(300) NOT NULL,
    [DiasSemana] nvarchar(50) NULL,
    CONSTRAINT [PK_RotinaGatilhos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RotinaGatilhos_Rotinas_RotinaId] FOREIGN KEY ([RotinaId]) REFERENCES [Rotinas] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_RotinaAcoes_RotinaId] ON [RotinaAcoes] ([RotinaId]);
GO

CREATE INDEX [IX_RotinaGatilhos_RotinaId] ON [RotinaGatilhos] ([RotinaId]);
GO

CREATE INDEX [IX_Rotinas_UserId] ON [Rotinas] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260106003145_AddAutomationModule', N'8.0.5');
GO

COMMIT;
GO

