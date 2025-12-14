CREATE TABLE [ConfiguracoesSistema] (
    [Id] int NOT NULL IDENTITY,
    [DominioCloudflare] nvarchar(max) NOT NULL,
    [DominioNlp] nvarchar(max) NOT NULL,
    [UltimaAtualizacao] datetime2 NOT NULL,
    CONSTRAINT [PK_ConfiguracoesSistema] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ConfiguracoesStarkNlp] (
    [Id] uniqueidentifier NOT NULL,
    [StarkNlpUrl] nvarchar(500) NOT NULL,
    [DataAtualizacao] datetime2 NOT NULL,
    CONSTRAINT [PK_ConfiguracoesStarkNlp] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ErrorCodeDescriptions] (
    [CodigoDeErro] nvarchar(50) NOT NULL,
    [Descricao] nvarchar(max) NOT NULL,
    [Contexto] nvarchar(max) NOT NULL,
    [CamposRelevantes] nvarchar(max) NOT NULL,
    [Origem] nvarchar(20) NOT NULL,
    [Solucoes] nvarchar(max) NULL,
    CONSTRAINT [PK_ErrorCodeDescriptions] PRIMARY KEY ([CodigoDeErro])
);
GO


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
GO


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
GO


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
GO


CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [ApiKey] nvarchar(100) NOT NULL,
    [StarkCoinBalance] int NOT NULL DEFAULT 0,
    [PlanType] int NOT NULL DEFAULT 0,
    [TokensConsumidosSemana] int NOT NULL DEFAULT 0,
    [CreatedAt] datetimeoffset NOT NULL,
    [IsActive] bit NOT NULL,
    [Role] nvarchar(50) NOT NULL,
    [RemovalAds] nvarchar(50) NOT NULL,
    [PreapprovalId] nvarchar(100) NULL,
    [Estado] nvarchar(100) NULL,
    [Cidade] nvarchar(100) NULL,
    [Bairro] nvarchar(100) NULL,
    [LastUpdatedAt] datetimeoffset NULL,
    [UltimoPagamentoConfirmadoEm] datetimeoffset NULL,
    [SpotifyAccessToken] nvarchar(500) NULL,
    [SpotifyRefreshToken] nvarchar(500) NULL,
    [SpotifyTokenExpiresAt] datetimeoffset NULL,
    [MinutosReconhecidos] float NOT NULL DEFAULT 0.0E0,
    [WhatsAppSessionData] nvarchar(max) NULL,
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
    [StripePriceId] nvarchar(100) NULL,
    [TipoPlano] nvarchar(max) NULL,
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
    [RespostasAleatorias] nvarchar(max) NULL,
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
    [Comando] nvarchar(200) NULL,
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


CREATE TABLE [DispositivosEsp] (
    [Id] uniqueidentifier NOT NULL,
    [Nome] nvarchar(150) NOT NULL,
    [Ip] nvarchar(45) NOT NULL,
    [Porta] int NOT NULL,
    [Comando] nvarchar(200) NULL,
    [ComandToEsp] nvarchar(200) NULL,
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


CREATE TABLE [FirebaseTokens] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [DataCadastro] datetimeoffset NOT NULL,
    CONSTRAINT [PK_FirebaseTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FirebaseTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
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
    [Origem] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
GO


CREATE TABLE [StarkCoinPurchases] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [PackageType] int NOT NULL,
    [StarkCoinsAmount] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_StarkCoinPurchases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StarkCoinPurchases_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
GO


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


CREATE TABLE [UserSessions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [SessionName] nvarchar(100) NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [Origem] nvarchar(50) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastActivityAt] datetime2 NULL,
    CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
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


CREATE TABLE [Agendamentos] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [DeviceId] uniqueidentifier NULL,
    [DispositivoEspId] uniqueidentifier NULL,
    [EwelinkDeviceId] nvarchar(max) NULL,
    [TipoAgendamento] int NOT NULL,
    [AgendadoPara] datetimeoffset NOT NULL,
    [Comando] nvarchar(max) NOT NULL,
    [Executado] bit NOT NULL,
    [Recorrencia] nvarchar(max) NULL,
    CONSTRAINT [PK_Agendamentos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Agendamentos_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Agendamentos_DispositivosEsp_DispositivoEspId] FOREIGN KEY ([DispositivoEspId]) REFERENCES [DispositivosEsp] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Agendamentos_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
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


CREATE INDEX [IX_Agendamentos_DeviceId] ON [Agendamentos] ([DeviceId]);
GO


CREATE INDEX [IX_Agendamentos_DispositivoEspId] ON [Agendamentos] ([DispositivoEspId]);
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


CREATE INDEX [IX_DispositivosEsp_UserId] ON [DispositivosEsp] ([UserId]);
GO


CREATE INDEX [IX_ErrorLogsApp_UserId] ON [ErrorLogsApp] ([UserId]);
GO


CREATE INDEX [IX_ErrorLogsSoft_UserId] ON [ErrorLogsSoft] ([UserId]);
GO


CREATE INDEX [IX_EwelinkAccounts_UserId] ON [EwelinkAccounts] ([UserId]);
GO


CREATE INDEX [IX_EwelinkDevices_UserId] ON [EwelinkDevices] ([UserId]);
GO


CREATE INDEX [IX_FirebaseTokens_UserId] ON [FirebaseTokens] ([UserId]);
GO


CREATE INDEX [IX_IaHistoricos_UserId] ON [IaHistoricos] ([UserId]);
GO


CREATE INDEX [IX_LicenseActivations_LicenseId] ON [LicenseActivations] ([LicenseId]);
GO


CREATE INDEX [IX_Licenses_UserId] ON [Licenses] ([UserId]);
GO


CREATE INDEX [IX_LogsFalhasSoft_UserId] ON [LogsFalhasSoft] ([UserId]);
GO


CREATE INDEX [IX_PagamentosAvulsos_UserId] ON [PagamentosAvulsos] ([UserId]);
GO


CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
GO


CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO


CREATE INDEX [IX_ResolvendoSuportes_UserId] ON [ResolvendoSuportes] ([UserId]);
GO


CREATE INDEX [IX_StarkCoinPurchases_UserId] ON [StarkCoinPurchases] ([UserId]);
GO


CREATE INDEX [IX_SuporteAcoes_UserId] ON [SuporteAcoes] ([UserId]);
GO


CREATE INDEX [IX_SuporteConversas_UserId] ON [SuporteConversas] ([UserId]);
GO


CREATE INDEX [IX_UserActivities_UserId] ON [UserActivities] ([UserId]);
GO


CREATE INDEX [IX_UserSessions_UserId] ON [UserSessions] ([UserId]);
GO


