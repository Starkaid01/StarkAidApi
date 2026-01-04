-- Script para adicionar variações ao aprendizado "quem descobriu america"
-- Execute este script no seu banco de dados SQL Server

DECLARE @AprendizadoId UNIQUEIDENTIFIER;

-- Encontrar o ID do aprendizado
SELECT @AprendizadoId = Id 
FROM Aprendizados 
WHERE Texto LIKE '%quem descobriu america%' 
  AND Tipo = 'Global';

-- Verificar se encontrou
IF @AprendizadoId IS NOT NULL
BEGIN
    PRINT 'Aprendizado encontrado: ' + CAST(@AprendizadoId AS NVARCHAR(50));
    
    -- Limpar variações antigas se existirem
    DELETE FROM AprendizadoRespostas WHERE AprendizadoId = @AprendizadoId;
    
    -- Adicionar a resposta original como primeira variação
    INSERT INTO AprendizadoRespostas (Id, AprendizadoId, Texto, UsoCount, CreatedAt)
    VALUES (
        NEWID(),
        @AprendizadoId,
        'Cara! Foi Cristóvão Colombo, em 1492! Mas, é importante lembrar que os índios já estavam por aqui, Ele só "redescobriu".',
        0,
        SYSDATETIMEOFFSET()
    );
    
    -- Adicionar variação 1 (mais neutra)
    INSERT INTO AprendizadoRespostas (Id, AprendizadoId, Texto, UsoCount, CreatedAt)
    VALUES (
        NEWID(),
        @AprendizadoId,
        'Foi Cristóvão Colombo em 1492. Vale lembrar que povos indígenas já habitavam o continente há milhares de anos.',
        0,
        SYSDATETIMEOFFSET()
    );
    
    -- Adicionar variação 2
    INSERT INTO AprendizadoRespostas (Id, AprendizadoId, Texto, UsoCount, CreatedAt)
    VALUES (
        NEWID(),
        @AprendizadoId,
        'Cristóvão Colombo chegou às Américas em 1492, embora o continente já fosse habitado por povos nativos.',
        0,
        SYSDATETIMEOFFSET()
    );
    
    -- Adicionar variação 3
    INSERT INTO AprendizadoRespostas (Id, AprendizadoId, Texto, UsoCount, CreatedAt)
    VALUES (
        NEWID(),
        @AprendizadoId,
        'A chegada de Cristóvão Colombo aconteceu em 1492, mas os povos originários já viviam aqui muito antes disso.',
        0,
        SYSDATETIMEOFFSET()
    );
    
    PRINT 'Variações adicionadas com sucesso!';
    
    -- Mostrar resultado
    SELECT 
        ar.Texto,
        ar.UsoCount,
        ar.CreatedAt
    FROM AprendizadoRespostas ar
    WHERE ar.AprendizadoId = @AprendizadoId
    ORDER BY ar.CreatedAt;
END
ELSE
BEGIN
    PRINT 'Aprendizado não encontrado. Verifique se existe um registro com esse texto.';
END
