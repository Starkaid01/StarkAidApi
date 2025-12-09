using Microsoft.Data.Sqlite;
using StarkAid.WindowsForms.Models;
using System.Data;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StarkAid.WindowsForms.Database;

public class LocalDatabase
{
    private readonly string _connectionString;

    public LocalDatabase()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StarkAid", "local.db");
        var directory = Path.GetDirectoryName(dbPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Tabela de Comandos Sociais
        var createComandosSociais = @"
            CREATE TABLE IF NOT EXISTS ComandosSociais (
                Id TEXT PRIMARY KEY,
                Comando TEXT NOT NULL,
                Resposta TEXT NOT NULL,
                RespostasAleatorias TEXT
            )";

        // Tabela de Dispositivos ESP
        var createDispositivosEsp = @"
            CREATE TABLE IF NOT EXISTS DispositivosEsp (
                Id TEXT PRIMARY KEY,
                Nome TEXT NOT NULL,
                Ip TEXT NOT NULL,
                Porta INTEGER NOT NULL,
                Comando TEXT,
                ComandToEsp TEXT,
                Status TEXT,
                LigadoDesligado INTEGER
            )";

        // Tabela de Último Comando
        var createUltimoComando = @"
            CREATE TABLE IF NOT EXISTS UltimoComando (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Comando TEXT NOT NULL,
                Resposta TEXT,
                Timestamp TEXT NOT NULL
            )";

        // Tabela de Configurações do Assistente
        var createConfigAssistente = @"
            CREATE TABLE IF NOT EXISTS ConfigAssistente (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NomeAssistente TEXT,
                RespostaPadrao TEXT,
                MicrofoneId INTEGER,
                VozName TEXT
            )";

        // Tabela de Agendamentos de Arquivos
        var createAgendamentosArquivos = @"
            CREATE TABLE IF NOT EXISTS AgendamentosArquivos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CaminhoArquivo TEXT NOT NULL,
                DataHora TEXT NOT NULL,
                Frequencia INTEGER NOT NULL,
                Ativo INTEGER NOT NULL DEFAULT 1,
                UltimaExecucao TEXT
            )";

        // Tabela de Settings
        var createSettings = @"
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            )";

        // Tabela de Credenciais de Login
        var createLoginCredentials = @"
            CREATE TABLE IF NOT EXISTS LoginCredentials (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                Token TEXT,
                LastLogin TEXT
            )";

        // Tabela de Lembretes
        var createLembretes = @"
            CREATE TABLE IF NOT EXISTS Lembretes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Lembrar TEXT NOT NULL,
                Dia INTEGER,
                Mes INTEGER,
                Hora INTEGER,
                Minuto INTEGER,
                Concluido INTEGER NOT NULL DEFAULT 0,
                DataCriacao TEXT NOT NULL,
                UltimaNotificacao TEXT
            )";

        // Tabela de Aprendizado
        var createAprendizado = @"
            CREATE TABLE IF NOT EXISTS Aprendizado (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ComandoUser TEXT NOT NULL,
                RespostaIa TEXT NOT NULL,
                DataCriacao TEXT NOT NULL
            )";

        // Tabela de Logs para Suporte
        var createLogsToSuporte = @"
            CREATE TABLE IF NOT EXISTS logsToSuporte (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ultimoComando TEXT,
                ultimaResposta TEXT,
                ultimoDispositivoAcionado TEXT,
                erroCompleto TEXT,
                codigoDeErro TEXT,
                dataErro TEXT NOT NULL,
                horaErro TEXT NOT NULL,
                acaoErro TEXT NOT NULL
            )";

        // Tabela de Comandos Shell
        var createComandosShell = @"
            CREATE TABLE IF NOT EXISTS ComandosShell (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ComandoInput TEXT NOT NULL,
                Resposta TEXT NOT NULL,
                ComandoCMD TEXT NOT NULL
            )";

        // Tabela de Usuário
        var createUser = @"
            CREATE TABLE IF NOT EXISTS User (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL,
                ApiKey TEXT,
                StarkCoins REAL NOT NULL DEFAULT 0,
                Role TEXT,
                Estado TEXT,
                Cidade TEXT,
                Bairro TEXT,
                LastUpdated TEXT NOT NULL
            )";

        // Tabela de Dispositivos Ewelink
        var createEwelinkDevices = @"
            CREATE TABLE IF NOT EXISTS EwelinkDevices (
                Id INTEGER PRIMARY KEY,
                DeviceId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Type INTEGER NOT NULL,
                Uiid INTEGER NOT NULL,
                Params TEXT,
                Online INTEGER NOT NULL DEFAULT 0,
                FamilyId TEXT,
                RoomId TEXT,
                IsOn INTEGER NOT NULL DEFAULT 0
            )";

        // Tabela de Dispositivos Starkswitch
        var createDevices = @"
            CREATE TABLE IF NOT EXISTS Devices (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Comando TEXT
            )";

        // Tabela de Dados UI
        var createDadosUI = @"
            CREATE TABLE IF NOT EXISTS DadosUI (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StarkCoins REAL NOT NULL DEFAULT 0,
                LastUpdated TEXT NOT NULL
            )";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = createComandosSociais;
            command.ExecuteNonQuery();

            command.CommandText = createDispositivosEsp;
            command.ExecuteNonQuery();

            // Migração: adicionar coluna ComandToEsp se não existir
            try
            {
                command.CommandText = "ALTER TABLE DispositivosEsp ADD COLUMN ComandToEsp TEXT";
                command.ExecuteNonQuery();
            }
            catch
            {
                // Coluna já existe, ignorar erro
            }

            command.CommandText = createUltimoComando;
            command.ExecuteNonQuery();

            command.CommandText = createConfigAssistente;
            command.ExecuteNonQuery();

            // Migração: adicionar coluna VozName se não existir
            try
            {
                command.CommandText = "ALTER TABLE ConfigAssistente ADD COLUMN VozName TEXT";
                command.ExecuteNonQuery();
            }
            catch
            {
                // Coluna já existe, ignorar erro
            }

            command.CommandText = createSettings;
            command.ExecuteNonQuery();

            command.CommandText = createLoginCredentials;
            command.ExecuteNonQuery();

            command.CommandText = createAgendamentosArquivos;
            command.ExecuteNonQuery();

            command.CommandText = createLembretes;
            command.ExecuteNonQuery();

            command.CommandText = createAprendizado;
            command.ExecuteNonQuery();

            command.CommandText = createLogsToSuporte;
            command.ExecuteNonQuery();

            command.CommandText = createComandosShell;
            command.ExecuteNonQuery();

            command.CommandText = createUser;
            command.ExecuteNonQuery();

            command.CommandText = createEwelinkDevices;
            command.ExecuteNonQuery();

            command.CommandText = createDevices;
            command.ExecuteNonQuery();

            command.CommandText = createDadosUI;
            command.ExecuteNonQuery();
        }
    }

    // Comandos Sociais
    public void ClearComandosSociais()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ComandosSociais";
        command.ExecuteNonQuery();
    }

    public void SaveComandosSociais(List<ComandoSocial> comandos)
    {
        ClearComandosSociais();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (var comando in comandos)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ComandosSociais (Id, Comando, Resposta, RespostasAleatorias)
                VALUES (@Id, @Comando, @Resposta, @RespostasAleatorias)";
            command.Parameters.AddWithValue("@Id", comando.Id.ToString());
            command.Parameters.AddWithValue("@Comando", comando.Comando);
            command.Parameters.AddWithValue("@Resposta", comando.Resposta);
            command.Parameters.AddWithValue("@RespostasAleatorias", comando.RespostasAleatorias ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    public List<ComandoSocial> GetComandosSociais()
    {
        var comandos = new List<ComandoSocial>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Comando, Resposta, RespostasAleatorias FROM ComandosSociais";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            comandos.Add(new ComandoSocial
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Comando = reader.GetString("Comando"),
                Resposta = reader.GetString("Resposta"),
                RespostasAleatorias = reader.IsDBNull("RespostasAleatorias") ? null : reader.GetString("RespostasAleatorias")
            });
        }

        return comandos;
    }

    // Dispositivos ESP
    public void ClearDispositivosEsp()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM DispositivosEsp";
        command.ExecuteNonQuery();
    }

    public void SaveDispositivosEsp(List<DispositivoEsp> dispositivos)
    {
        ClearDispositivosEsp();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (var dispositivo in dispositivos)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO DispositivosEsp (Id, Nome, Ip, Porta, Comando, ComandToEsp, Status, LigadoDesligado)
                VALUES (@Id, @Nome, @Ip, @Porta, @Comando, @ComandToEsp, @Status, @LigadoDesligado)";
            command.Parameters.AddWithValue("@Id", dispositivo.Id.ToString());
            command.Parameters.AddWithValue("@Nome", dispositivo.Nome);
            command.Parameters.AddWithValue("@Ip", dispositivo.Ip);
            command.Parameters.AddWithValue("@Porta", dispositivo.Porta);
            command.Parameters.AddWithValue("@Comando", dispositivo.Comando ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ComandToEsp", dispositivo.ComandToEsp ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", dispositivo.Status);
            command.Parameters.AddWithValue("@LigadoDesligado", dispositivo.LigadoDesligado ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    public List<DispositivoEsp> GetDispositivosEsp()
    {
        var dispositivos = new List<DispositivoEsp>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Nome, Ip, Porta, Comando, ComandToEsp, Status, LigadoDesligado FROM DispositivosEsp";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            dispositivos.Add(new DispositivoEsp
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Nome = reader.GetString("Nome"),
                Ip = reader.GetString("Ip"),
                Porta = reader.GetInt32("Porta"),
                Comando = reader.IsDBNull("Comando") ? null : reader.GetString("Comando"),
                ComandToEsp = reader.IsDBNull("ComandToEsp") ? null : reader.GetString("ComandToEsp"),
                Status = reader.GetString("Status"),
                LigadoDesligado = reader.GetInt32("LigadoDesligado") == 1
            });
        }

        return dispositivos;
    }

    // Último Comando
    public void ClearUltimoComando()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM UltimoComando";
        command.ExecuteNonQuery();
    }

    public void SaveUltimoComando(string comando, string? resposta = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO UltimoComando (Comando, Resposta, Timestamp)
            VALUES (@Comando, @Resposta, @Timestamp)";
        command.Parameters.AddWithValue("@Comando", comando);
        command.Parameters.AddWithValue("@Resposta", resposta ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Timestamp", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    // Configurações do Assistente
    public void SaveConfigAssistente(string nomeAssistente, string respostaPadrao, int microfoneId = 0, string? vozName = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Limpar configurações anteriores
        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.CommandText = "DELETE FROM ConfigAssistente";
            deleteCommand.ExecuteNonQuery();
        }

        // Inserir nova configuração
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ConfigAssistente (NomeAssistente, RespostaPadrao, MicrofoneId, VozName)
            VALUES (@NomeAssistente, @RespostaPadrao, @MicrofoneId, @VozName)";
        command.Parameters.AddWithValue("@NomeAssistente", nomeAssistente.ToLowerInvariant());
        command.Parameters.AddWithValue("@RespostaPadrao", respostaPadrao);
        command.Parameters.AddWithValue("@MicrofoneId", microfoneId);
        command.Parameters.AddWithValue("@VozName", vozName ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public (string? NomeAssistente, string? RespostaPadrao, int? MicrofoneId, string? VozName) GetConfigAssistente()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT NomeAssistente, RespostaPadrao, MicrofoneId, VozName FROM ConfigAssistente LIMIT 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var nome = reader.IsDBNull("NomeAssistente") ? null : reader.GetString("NomeAssistente");
            var resposta = reader.IsDBNull("RespostaPadrao") ? null : reader.GetString("RespostaPadrao");
            var microfone = reader.IsDBNull("MicrofoneId") ? (int?)null : reader.GetInt32("MicrofoneId");
            var vozName = reader.IsDBNull("VozName") ? null : reader.GetString("VozName");
            return (nome, resposta, microfone, vozName);
        }

        return (null, null, null, null);
    }

    // Settings
    public void SaveSetting(string key, string value)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Settings (Key, Value)
            VALUES (@Key, @Value)";
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", value);
        command.ExecuteNonQuery();
    }

    public string? GetSetting(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Settings WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return reader.IsDBNull("Value") ? null : reader.GetString("Value");
        }

        return null;
    }

    // Credenciais de Login
    public void SaveLoginCredentials(string email, string password, string? token = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Limpar credenciais anteriores
        using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.CommandText = "DELETE FROM LoginCredentials";
            deleteCommand.ExecuteNonQuery();
        }

        // Criptografar senha (usando Base64 como proteção básica - em produção, usar criptografia adequada)
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        var passwordHash = Convert.ToBase64String(passwordBytes);

        // Inserir nova credencial
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO LoginCredentials (Email, PasswordHash, Token, LastLogin)
            VALUES (@Email, @PasswordHash, @Token, @LastLogin)";
        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@Token", token ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LastLogin", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    public (string? Email, string? PasswordHash, string? Token) GetLoginCredentials()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Email, PasswordHash, Token FROM LoginCredentials ORDER BY LastLogin DESC LIMIT 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var email = reader.IsDBNull("Email") ? null : reader.GetString("Email");
            var passwordHash = reader.IsDBNull("PasswordHash") ? null : reader.GetString("PasswordHash");
            var token = reader.IsDBNull("Token") ? null : reader.GetString("Token");
            return (email, passwordHash, token);
        }

        return (null, null, null);
    }

    public void ClearLoginCredentials()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM LoginCredentials";
        command.ExecuteNonQuery();
    }

    public void UpdateLoginToken(string token)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE LoginCredentials 
            SET Token = @Token, LastLogin = @LastLogin 
            WHERE Id = (SELECT Id FROM LoginCredentials ORDER BY LastLogin DESC LIMIT 1)";
        command.Parameters.AddWithValue("@Token", token);
        command.Parameters.AddWithValue("@LastLogin", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    // Agendamentos de Arquivos
    public void SaveAgendamentoArquivo(AgendamentoArquivo agendamento)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        if (agendamento.Id > 0)
        {
            // Atualizar
            command.CommandText = @"
                UPDATE AgendamentosArquivos 
                SET CaminhoArquivo = @CaminhoArquivo, 
                    DataHora = @DataHora, 
                    Frequencia = @Frequencia, 
                    Ativo = @Ativo,
                    UltimaExecucao = @UltimaExecucao
                WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", agendamento.Id);
        }
        else
        {
            // Inserir
            command.CommandText = @"
                INSERT INTO AgendamentosArquivos (CaminhoArquivo, DataHora, Frequencia, Ativo, UltimaExecucao)
                VALUES (@CaminhoArquivo, @DataHora, @Frequencia, @Ativo, @UltimaExecucao)";
        }
        
        command.Parameters.AddWithValue("@CaminhoArquivo", agendamento.CaminhoArquivo);
        command.Parameters.AddWithValue("@DataHora", agendamento.DataHora.ToString("O"));
        command.Parameters.AddWithValue("@Frequencia", (int)agendamento.Frequencia);
        command.Parameters.AddWithValue("@Ativo", agendamento.Ativo ? 1 : 0);
        command.Parameters.AddWithValue("@UltimaExecucao", agendamento.UltimaExecucao?.ToString("O") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public List<AgendamentoArquivo> GetAgendamentosArquivos()
    {
        var agendamentos = new List<AgendamentoArquivo>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, CaminhoArquivo, DataHora, Frequencia, Ativo, UltimaExecucao FROM AgendamentosArquivos ORDER BY DataHora";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            agendamentos.Add(new AgendamentoArquivo
            {
                Id = reader.GetInt32("Id"),
                CaminhoArquivo = reader.GetString("CaminhoArquivo"),
                DataHora = DateTime.Parse(reader.GetString("DataHora")),
                Frequencia = (FrequenciaAgendamento)reader.GetInt32("Frequencia"),
                Ativo = reader.GetInt32("Ativo") == 1,
                UltimaExecucao = reader.IsDBNull("UltimaExecucao") ? null : DateTime.Parse(reader.GetString("UltimaExecucao"))
            });
        }

        return agendamentos;
    }

    public void DeleteAgendamentoArquivo(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM AgendamentosArquivos WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    // Lembretes
    public void SaveLembrete(Lembrete lembrete)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        if (lembrete.Id > 0)
        {
            command.CommandText = @"
                UPDATE Lembretes 
                SET Lembrar = @Lembrar, Dia = @Dia, Mes = @Mes, Hora = @Hora, Minuto = @Minuto, 
                    Concluido = @Concluido, UltimaNotificacao = @UltimaNotificacao
                WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", lembrete.Id);
        }
        else
        {
            command.CommandText = @"
                INSERT INTO Lembretes (Lembrar, Dia, Mes, Hora, Minuto, Concluido, DataCriacao, UltimaNotificacao)
                VALUES (@Lembrar, @Dia, @Mes, @Hora, @Minuto, @Concluido, @DataCriacao, @UltimaNotificacao)";
            command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("O"));
        }
        
        command.Parameters.AddWithValue("@Lembrar", lembrete.Lembrar);
        command.Parameters.AddWithValue("@Dia", lembrete.Dia ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Mes", lembrete.Mes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Hora", lembrete.Hora ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Minuto", lembrete.Minuto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Concluido", lembrete.Concluido ? 1 : 0);
        command.Parameters.AddWithValue("@UltimaNotificacao", lembrete.UltimaNotificacao?.ToString("O") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public List<Lembrete> GetLembretes(bool apenasPendentes = false)
    {
        var lembretes = new List<Lembrete>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        var query = "SELECT Id, Lembrar, Dia, Mes, Hora, Minuto, Concluido, DataCriacao, UltimaNotificacao FROM Lembretes";
        if (apenasPendentes)
        {
            query += " WHERE Concluido = 0";
        }
        query += " ORDER BY DataCriacao DESC";
        
        command.CommandText = query;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lembretes.Add(new Lembrete
            {
                Id = reader.GetInt32("Id"),
                Lembrar = reader.GetString("Lembrar"),
                Dia = reader.IsDBNull("Dia") ? null : reader.GetInt32("Dia"),
                Mes = reader.IsDBNull("Mes") ? null : reader.GetInt32("Mes"),
                Hora = reader.IsDBNull("Hora") ? null : reader.GetInt32("Hora"),
                Minuto = reader.IsDBNull("Minuto") ? null : reader.GetInt32("Minuto"),
                Concluido = reader.GetInt32("Concluido") == 1,
                DataCriacao = DateTime.Parse(reader.GetString("DataCriacao")),
                UltimaNotificacao = reader.IsDBNull("UltimaNotificacao") ? null : DateTime.Parse(reader.GetString("UltimaNotificacao"))
            });
        }

        return lembretes;
    }

    public void MarcarLembreteConcluido(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Lembretes SET Concluido = 1 WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public List<Lembrete> GetLembretesDisparados()
    {
        var lembretes = new List<Lembrete>();
        var agora = DateTime.Now;
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        // Buscar lembretes pendentes que foram notificados
        command.CommandText = @"
            SELECT Id, Lembrar, Dia, Mes, Hora, Minuto, Concluido, DataCriacao, UltimaNotificacao 
            FROM Lembretes 
            WHERE Concluido = 0 AND UltimaNotificacao IS NOT NULL
            ORDER BY UltimaNotificacao DESC";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var lembrete = new Lembrete
            {
                Id = reader.GetInt32("Id"),
                Lembrar = reader.GetString("Lembrar"),
                Dia = reader.IsDBNull("Dia") ? null : reader.GetInt32("Dia"),
                Mes = reader.IsDBNull("Mes") ? null : reader.GetInt32("Mes"),
                Hora = reader.IsDBNull("Hora") ? null : reader.GetInt32("Hora"),
                Minuto = reader.IsDBNull("Minuto") ? null : reader.GetInt32("Minuto"),
                Concluido = reader.GetInt32("Concluido") == 1,
                DataCriacao = DateTime.Parse(reader.GetString("DataCriacao")),
                UltimaNotificacao = reader.IsDBNull("UltimaNotificacao") ? null : DateTime.Parse(reader.GetString("UltimaNotificacao"))
            };
            
            // Verificar se o lembrete está realmente disparado (dentro do horário)
            bool estaDisparado = false;
            
            if (lembrete.Dia.HasValue && lembrete.Mes.HasValue)
            {
                var dataLembrete = new DateTime(agora.Year, lembrete.Mes.Value, lembrete.Dia.Value);
                if (dataLembrete < agora.Date)
                    dataLembrete = dataLembrete.AddYears(1);
                
                if (agora.Date == dataLembrete.Date || (agora.Day == lembrete.Dia.Value && agora.Month == lembrete.Mes.Value))
                {
                    if (lembrete.Hora.HasValue && lembrete.Minuto.HasValue)
                    {
                        var horaMinutoLembrete = new DateTime(agora.Year, agora.Month, agora.Day, 
                            lembrete.Hora.Value, lembrete.Minuto.Value, 0);
                        if (agora >= horaMinutoLembrete && agora.Hour < 22)
                            estaDisparado = true;
                    }
                    else if (lembrete.Hora.HasValue)
                    {
                        if (agora.Hour >= lembrete.Hora.Value && agora.Hour < 22)
                            estaDisparado = true;
                    }
                    else
                    {
                        if (agora.Hour >= 7 && agora.Hour < 22)
                            estaDisparado = true;
                    }
                }
            }
            else
            {
                // Sem data específica
                if (agora.Hour >= 7 && agora.Hour < 22)
                    estaDisparado = true;
            }
            
            if (estaDisparado)
            {
                lembretes.Add(lembrete);
            }
        }
        
        return lembretes;
    }

    public void DeleteLembrete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Lembretes WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    // Aprendizado
    public void SaveAprendizado(string comandoUser, string respostaIa)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Aprendizado (ComandoUser, RespostaIa, DataCriacao)
            VALUES (@ComandoUser, @RespostaIa, @DataCriacao)";
        command.Parameters.AddWithValue("@ComandoUser", comandoUser);
        command.Parameters.AddWithValue("@RespostaIa", respostaIa);
        command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }

    public List<Aprendizado> GetAprendizados()
    {
        var aprendizados = new List<Aprendizado>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ComandoUser, RespostaIa, DataCriacao FROM Aprendizado ORDER BY DataCriacao DESC";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            aprendizados.Add(new Aprendizado
            {
                Id = reader.GetInt32("Id"),
                ComandoUser = reader.GetString("ComandoUser"),
                RespostaIa = reader.GetString("RespostaIa"),
                DataCriacao = DateTime.Parse(reader.GetString("DataCriacao"))
            });
        }

        return aprendizados;
    }

    public void UpdateAprendizado(Aprendizado aprendizado)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Aprendizado 
            SET ComandoUser = @ComandoUser, RespostaIa = @RespostaIa
            WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", aprendizado.Id);
        command.Parameters.AddWithValue("@ComandoUser", aprendizado.ComandoUser);
        command.Parameters.AddWithValue("@RespostaIa", aprendizado.RespostaIa);
        command.ExecuteNonQuery();
    }

    public void DeleteAprendizado(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Aprendizado WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public void ClearAprendizados()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Aprendizado";
        command.ExecuteNonQuery();
    }

    // Logs para Suporte
    public void SaveLogToSuporte(LogToSuporte log)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO logsToSuporte (
                    ultimoComando, ultimaResposta, ultimoDispositivoAcionado,
                    erroCompleto, codigoDeErro, dataErro, horaErro, acaoErro
                )
                VALUES (
                    @UltimoComando, @UltimaResposta, @UltimoDispositivoAcionado,
                    @ErroCompleto, @CodigoDeErro, @DataErro, @HoraErro, @AcaoErro
                )";
            command.Parameters.AddWithValue("@UltimoComando", log.UltimoComando ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UltimaResposta", log.UltimaResposta ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UltimoDispositivoAcionado", log.UltimoDispositivoAcionado ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ErroCompleto", log.ErroCompleto ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CodigoDeErro", log.CodigoDeErro ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DataErro", log.DataErro);
            command.Parameters.AddWithValue("@HoraErro", log.HoraErro);
            command.Parameters.AddWithValue("@AcaoErro", log.AcaoErro);
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            // Se falhar ao salvar log, escrever no debug apenas (não podemos entrar em loop)
            System.Diagnostics.Debug.WriteLine($"Erro ao salvar log para suporte: {ex.Message}");
        }
    }

    public List<LogToSuporte> GetAllLogsToSuporte()
    {
        var logs = new List<LogToSuporte>();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, ultimoComando, ultimaResposta, ultimoDispositivoAcionado,
                       erroCompleto, codigoDeErro, dataErro, horaErro, acaoErro
                FROM logsToSuporte
                ORDER BY dataErro DESC, horaErro DESC";
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new LogToSuporte
                {
                    Id = reader.GetInt32(0),
                    UltimoComando = reader.IsDBNull(1) ? null : reader.GetString(1),
                    UltimaResposta = reader.IsDBNull(2) ? null : reader.GetString(2),
                    UltimoDispositivoAcionado = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ErroCompleto = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CodigoDeErro = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DataErro = reader.GetString(6),
                    HoraErro = reader.GetString(7),
                    AcaoErro = reader.GetString(8)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao buscar logs: {ex.Message}");
        }
        return logs;
    }

    // Helper estático para facilitar o log de erros
    public static void LogError(LocalDatabase database, Exception ex, string codigoErro, string acaoErro, 
        string? ultimoComando = null, string? ultimaResposta = null, string? ultimoDispositivoAcionado = null)
    {
        try
        {
            var agora = DateTime.Now;
            var log = new LogToSuporte
            {
                UltimoComando = ultimoComando,
                UltimaResposta = ultimaResposta,
                UltimoDispositivoAcionado = ultimoDispositivoAcionado,
                ErroCompleto = $"{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                CodigoDeErro = codigoErro,
                DataErro = agora.ToString("yyyy-MM-dd"),
                HoraErro = agora.ToString("HH:mm:ss"),
                AcaoErro = acaoErro
            };

            database.SaveLogToSuporte(log);
        }
        catch
        {
            // Se falhar ao salvar log, não fazer nada para evitar loops
            System.Diagnostics.Debug.WriteLine("Erro ao tentar salvar log de erro");
        }
    }

    // Comandos Shell
    public void SaveComandoShell(ComandoShell comandoShell)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        
        if (comandoShell.Id == 0)
        {
            // Inserir novo
            command.CommandText = @"
                INSERT INTO ComandosShell (ComandoInput, Resposta, ComandoCMD)
                VALUES (@ComandoInput, @Resposta, @ComandoCMD)";
        }
        else
        {
            // Atualizar existente
            command.CommandText = @"
                UPDATE ComandosShell 
                SET ComandoInput = @ComandoInput, Resposta = @Resposta, ComandoCMD = @ComandoCMD
                WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", comandoShell.Id);
        }
        
        command.Parameters.AddWithValue("@ComandoInput", comandoShell.ComandoInput);
        command.Parameters.AddWithValue("@Resposta", comandoShell.Resposta);
        command.Parameters.AddWithValue("@ComandoCMD", comandoShell.ComandoCMD);
        command.ExecuteNonQuery();
    }

    public List<ComandoShell> GetComandosShell()
    {
        var comandos = new List<ComandoShell>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ComandoInput, Resposta, ComandoCMD FROM ComandosShell ORDER BY Id";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            comandos.Add(new ComandoShell
            {
                Id = reader.GetInt32("Id"),
                ComandoInput = reader.GetString("ComandoInput"),
                Resposta = reader.GetString("Resposta"),
                ComandoCMD = reader.GetString("ComandoCMD")
            });
        }

        return comandos;
    }

    public void DeleteComandoShell(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ComandosShell WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    // User
    public void SaveUser(User user)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO User (Id, Name, Email, ApiKey, StarkCoins, Role, Estado, Cidade, Bairro, LastUpdated)
            VALUES (@Id, @Name, @Email, @ApiKey, @StarkCoins, @Role, @Estado, @Cidade, @Bairro, @LastUpdated)";
        command.Parameters.AddWithValue("@Id", user.Id.ToString());
        command.Parameters.AddWithValue("@Name", user.Name);
        command.Parameters.AddWithValue("@Email", user.Email);
        command.Parameters.AddWithValue("@ApiKey", user.ApiKey ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StarkCoins", user.StarkCoins);
        command.Parameters.AddWithValue("@Role", user.Role ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Estado", user.Estado ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Cidade", user.Cidade ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Bairro", user.Bairro ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LastUpdated", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    public User? GetUser()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email, ApiKey, StarkCoins, Role, Estado, Cidade, Bairro FROM User LIMIT 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Name = reader.GetString("Name"),
                Email = reader.GetString("Email"),
                ApiKey = reader.IsDBNull("ApiKey") ? string.Empty : reader.GetString("ApiKey"),
                StarkCoins = reader.GetDecimal("StarkCoins"),
                Role = reader.IsDBNull("Role") ? string.Empty : reader.GetString("Role"),
                Estado = reader.IsDBNull("Estado") ? null : reader.GetString("Estado"),
                Cidade = reader.IsDBNull("Cidade") ? null : reader.GetString("Cidade"),
                Bairro = reader.IsDBNull("Bairro") ? null : reader.GetString("Bairro")
            };
        }

        return null;
    }

    // Ewelink Devices
    public void ClearEwelinkDevices()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM EwelinkDevices";
        command.ExecuteNonQuery();
    }

    public void SaveEwelinkDevices(List<EwelinkDevice> devices)
    {
        ClearEwelinkDevices();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (var device in devices)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO EwelinkDevices (Id, DeviceId, Name, Type, Uiid, Params, Online, FamilyId, RoomId, IsOn)
                VALUES (@Id, @DeviceId, @Name, @Type, @Uiid, @Params, @Online, @FamilyId, @RoomId, @IsOn)";
            command.Parameters.AddWithValue("@Id", device.Id);
            command.Parameters.AddWithValue("@DeviceId", device.DeviceId);
            command.Parameters.AddWithValue("@Name", device.Name);
            command.Parameters.AddWithValue("@Type", device.Type);
            command.Parameters.AddWithValue("@Uiid", device.Uiid);
            command.Parameters.AddWithValue("@Params", device.Params != null ? JsonConvert.SerializeObject(device.Params) : (object)DBNull.Value);
            command.Parameters.AddWithValue("@Online", device.Online ? 1 : 0);
            command.Parameters.AddWithValue("@FamilyId", device.FamilyId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RoomId", device.RoomId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsOn", device.IsOn ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    public List<EwelinkDevice> GetEwelinkDevices()
    {
        var devices = new List<EwelinkDevice>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, DeviceId, Name, Type, Uiid, Params, Online, FamilyId, RoomId, IsOn FROM EwelinkDevices";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var device = new EwelinkDevice
            {
                Id = reader.GetInt32("Id"),
                DeviceId = reader.GetString("DeviceId"),
                Name = reader.GetString("Name"),
                Type = reader.GetInt32("Type"),
                Uiid = reader.GetInt32("Uiid"),
                Online = reader.GetInt32("Online") == 1,
                IsOn = reader.GetInt32("IsOn") == 1
            };

            if (!reader.IsDBNull("Params"))
            {
                try
                {
                    device.Params = JsonConvert.DeserializeObject(reader.GetString("Params"));
                }
                catch
                {
                    device.Params = null;
                }
            }

            device.FamilyId = reader.IsDBNull("FamilyId") ? null : reader.GetString("FamilyId");
            device.RoomId = reader.IsDBNull("RoomId") ? null : reader.GetString("RoomId");

            devices.Add(device);
        }

        return devices;
    }

    // Devices (Starkswitch)
    public void ClearDevices()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Devices";
        command.ExecuteNonQuery();
    }

    public void SaveDevices(List<Device> devices)
    {
        ClearDevices();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        foreach (var device in devices)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Devices (Id, Name, Comando)
                VALUES (@Id, @Name, @Comando)";
            command.Parameters.AddWithValue("@Id", device.Id.ToString());
            command.Parameters.AddWithValue("@Name", device.Name);
            command.Parameters.AddWithValue("@Comando", device.Comando ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    public List<Device> GetDevices()
    {
        var devices = new List<Device>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Comando FROM Devices";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            devices.Add(new Device
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Name = reader.GetString("Name"),
                Comando = reader.IsDBNull("Comando") ? null : reader.GetString("Comando")
            });
        }

        return devices;
    }

    // DadosUI
    public void SaveDadosUI(decimal starkCoins)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO DadosUI (StarkCoins, LastUpdated)
            VALUES (@StarkCoins, @LastUpdated)";
        command.Parameters.AddWithValue("@StarkCoins", starkCoins);
        command.Parameters.AddWithValue("@LastUpdated", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    public decimal? GetLastStarkCoins()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT StarkCoins FROM DadosUI ORDER BY LastUpdated DESC LIMIT 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetDecimal("StarkCoins");
        }

        return null;
    }
}

