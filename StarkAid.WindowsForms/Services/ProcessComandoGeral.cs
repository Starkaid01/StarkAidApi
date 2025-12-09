using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarkAid.WindowsForms.Services
{
    public class ProcessComandoGeral
    {
        public string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Converter para minúsculas
            text = text.ToLowerInvariant();

            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                // Manter apenas letras, números e espaços (remover acentos e pontuação)
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    // Remover pontuação: .,?!;:()[]{}\"'- etc
                    // Manter apenas letras, números e espaços
                    if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                    {
                        stringBuilder.Append(c);
                    }
                }
            }

            // Normalizar espaços múltiplos em um único espaço
            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();

            return result;
        }

        public string CorrectingWordVariationsToAutomation(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Converter para minúsculas
            text = text.ToLowerInvariant();

            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            //LIGAR
            if (text.Contains("liga"))
            {
                text = text.Replace("liga", "ligar");
            }
            if (text.Contains("ligue"))
            {
                text = text.Replace("ligue", "ligar");
            }
            if (text.Contains("acende"))
            {
                text = text.Replace("acende", "ligar");
            }
            if (text.Contains("acenda"))
            {
                text = text.Replace("acenda", "ligar");
            }
            if (text.Contains("acender"))
            {
                text = text.Replace("acender", "ligar");
            }
            //DESLIGAR
            if (text.Contains("desliga"))
            {
                text = text.Replace("desliga", "desligar");
            }
            if (text.Contains("desligue"))
            {
                text = text.Replace("desligue", "desligar");
            }
            if (text.Contains("apaga"))
            {
                text = text.Replace("apaga", "desligar");
            }
            if (text.Contains("apague"))
            {
                text = text.Replace("apague", "desligar");
            }
            if (text.Contains("apagar"))
            {
                text = text.Replace("apagar", "desligar");
            }
            //ABRIR
            if (text.Contains("abre"))
            {
                text = text.Replace("abre", "abrir");
            }
            if (text.Contains("abra"))
            {
                text = text.Replace("abra", "abrir");
            }
            if (text.Contains("abrir"))
            {
                text = text.Replace("abrir", "abrir");
            }
            //FECHAR
            if (text.Contains("fecha"))
            {
                text = text.Replace("fecha", "fechar");
            }
            if (text.Contains("feche"))
            {
                text = text.Replace("feche", "fechar");
            }
            if (text.Contains("fechar"))
            {
                text = text.Replace("fechar", "fechar");
            }
            //ATIVAR
            if (text.Contains("ativa"))
            {
                text = text.Replace("ativa", "ativar");
            }
            if (text.Contains("ative"))
            {
                text = text.Replace("ative", "ativar");
            }
            //DESATIVAR
            if (text.Contains("desativa"))
            {
                text = text.Replace("desativa", "desativar");
            }
            if (text.Contains("desative"))
            {
                text = text.Replace("desative", "desativar");
            }
            //ENVIAR
            if (text.Contains("manda"))
            {
                text = text.Replace("manda", "enviar");
            }
            if (text.Contains("mande"))
            {
                text = text.Replace("mande", "enviar");
            }
            if (text.Contains("envia"))
            {
                text = text.Replace("envia", "enviar");
            }
            if (text.Contains("envie"))
            {
                text = text.Replace("envie", "enviar");
            }
            //PESQUISAR
            if (text.Contains("busca"))
            {
                text = text.Replace("busca", "pesquisar");
            }
            if (text.Contains("busque"))
            {
                text = text.Replace("busque", "pesquisar");
            }
            if (text.Contains("pesquisa"))
            {
                text = text.Replace("pesquisa", "pesquisar");
            }
            if (text.Contains("pesquise"))
            {
                text = text.Replace("pesquise", "pesquisar");
            }
            //SALVAR
            if (text.Contains("guarda"))
            {
                text = text.Replace("guarda", "salvar");
            }
            if (text.Contains("guarde"))
            {
                text = text.Replace("guarde", "salvar");
            }
            if (text.Contains("salva"))
            {
                text = text.Replace("salva", "salvar");
            }
            if (text.Contains("salve"))
            {
                text = text.Replace("salve", "salvar");
            }
            return text;
        }

        //IsCommand
        public bool IsCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = CorrectingWordVariationsToAutomation(text.Normalize(NormalizationForm.FormD));
            if (
                //comandos comuns
                text.Contains("ligar ")
                || text.Contains("desligar ")
                || text.Contains("abrir ")
                || text.Contains("fechar ")
                || text.Contains("ativar ")
                || text.Contains("desativar ")
                || text.Contains("enviar ")
                || text.Contains("pesquisar ")
                || text.Contains("salvar ")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsQuestion
        public bool IsQuestion(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();

            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                //o que
                text.Contains("o que ")
                || text.Contains("qual ")
                || text.Contains("quais ")
                || text.Contains("quem ")
                || text.Contains("onde ")
                || text.Contains("quando ")
                || text.Contains("por que ")
                || text.Contains("como ")
                || text.Contains("quanto ")
                || text.Contains("quantos ")
                || text.Contains("quantas ")
                || text.Contains("voce pode ")
                || text.Contains("voce poderia ")
                || text.Contains("voce gosta ")
                || text.Contains("voce conhece ")
                || text.Contains("voce sabe ")
                || text.Contains("me diga ")
                || text.Contains("me conte ")
                || text.Contains("me fale ")
                || text.Contains("explique ")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsQehoras
        public bool IsAskingTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                //o que
                normalizedString.Contains("que horas e")
                || normalizedString.Contains("que horas sao")
                || normalizedString.Contains("quantas horas")
                || normalizedString.Contains("agora e que horas")
                || normalizedString.Contains("agora e quantas horas")
                || normalizedString.Contains("me diga as horas")
                || normalizedString.Contains("me fale as horas")
                || normalizedString.Contains("me diga que horas sao")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsPrevisdoTempo
        public bool IsAskingWeather(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                    normalizedString.Contains("como esta o tempo")
                    || normalizedString.Contains("previsao do tempo")
                    || normalizedString.Contains("qual a temperatura")
                    || normalizedString.Contains("como esta o clima hoje")
                    || normalizedString.Contains("vai chover hoje")
                    || normalizedString.Contains("vai fazer sol hoje")
                    || normalizedString.Contains("preciso levar sombrinha ")
                    || normalizedString.Contains("preciso levar guarda chuva ")
                    || normalizedString.Contains("preciso levar capa de chuvas ")
                    || normalizedString.Contains("preciso de sombrinha hoje")
                    || normalizedString.Contains("preciso de guarda chuva hoje")
                    || normalizedString.Contains("preciso de guarda capa de chuvas ")
                    || normalizedString.Contains("preciso levar agazalho ")
                    || normalizedString.Contains("preciso levar blusa de frio ")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsLinkInternet
        public bool IsAskingInternetLink(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                    normalizedString.Contains("abra o site de ")
                    || normalizedString.Contains("abra o facebook ")
                    || normalizedString.Contains("abra o instagram ")
                    || normalizedString.Contains("abra o youtube ")
                    || normalizedString.Contains("abra o google ")
                    || normalizedString.Contains("abra o whatsapp ")
                    || normalizedString.Contains("abra o gmail ")

                    || normalizedString.Contains("abre o facebook ")
                    || normalizedString.Contains("abre o instagram ")
                    || normalizedString.Contains("abre o youtube ")
                    || normalizedString.Contains("abre o google ")
                    || normalizedString.Contains("abre o whatsapp ")
                    || normalizedString.Contains("abre o gmail ")

                    || normalizedString.Contains("abrir o facebook ")
                    || normalizedString.Contains("abrir o instagram ")
                    || normalizedString.Contains("abrir o youtube ")
                    || normalizedString.Contains("abrir o google ")
                    || normalizedString.Contains("abrir o whatsapp ")
                    || normalizedString.Contains("abrir o gmail ")


                    || normalizedString.Contains("abra facebook ")
                    || normalizedString.Contains("abra instagram ")
                    || normalizedString.Contains("abra youtube ")
                    || normalizedString.Contains("abra google ")
                    || normalizedString.Contains("abra whatsapp ")
                    || normalizedString.Contains("abra gmail ")

                    || normalizedString.Contains("abre facebook ")
                    || normalizedString.Contains("abre instagram ")
                    || normalizedString.Contains("abre youtube ")
                    || normalizedString.Contains("abre google ")
                    || normalizedString.Contains("abre whatsapp ")
                    || normalizedString.Contains("abre gmail ")

                    || normalizedString.Contains("abrir facebook ")
                    || normalizedString.Contains("abrir instagram ")
                    || normalizedString.Contains("abrir youtube ")
                    || normalizedString.Contains("abrir google ")
                    || normalizedString.Contains("abrir whatsapp ")
                    || normalizedString.Contains("abrir gmail ")

                    || normalizedString.Contains("abra a pagina de ")
                    || normalizedString.Contains("abra a pagina web de ")
                    || normalizedString.Contains("abra o link de ")
                    || normalizedString.Contains("me leve para o site de ")
                    || normalizedString.Contains("me leve para a pagina de ")
                    || normalizedString.Contains("me leve para a pagina web de ")
                    || normalizedString.Contains("me leve para o link de ")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsCloseLinkInternet
        public bool IsCloseInternetLink(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                    normalizedString.Contains("feche a pagina")
                    || normalizedString.Contains("feche o facebook ")
                    || normalizedString.Contains("feche o instagram ")
                    || normalizedString.Contains("feche o youtube ")
                    || normalizedString.Contains("feche o google ")
                    || normalizedString.Contains("feche o whatsapp ")
                    || normalizedString.Contains("feche o gmail ")

                    || normalizedString.Contains("fecha o facebook ")
                    || normalizedString.Contains("fecha o instagram ")
                    || normalizedString.Contains("fecha o youtube ")
                    || normalizedString.Contains("fecha o google ")
                    || normalizedString.Contains("fecha o whatsapp ")
                    || normalizedString.Contains("fecha o gmail ")

                    || normalizedString.Contains("fechar o facebook ")
                    || normalizedString.Contains("fechar o instagram ")
                    || normalizedString.Contains("fechar o youtube ")
                    || normalizedString.Contains("fechar o google ")
                    || normalizedString.Contains("fechar o whatsapp ")
                    || normalizedString.Contains("fechar o gmail ")



                    || normalizedString.Contains("feche a aba")
                    || normalizedString.Contains("feche o site")
                    || normalizedString.Contains("fechar a pagina")
                    || normalizedString.Contains("fechar a aba")
                    || normalizedString.Contains("fechar o site")
                    || normalizedString.Contains("fecha a pagina")
                    || normalizedString.Contains("fecha a aba")
                    || normalizedString.Contains("fecha o site")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsAskingDate
        public bool IsAskingDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                    normalizedString.Contains("que dia e hoje")
                    || normalizedString.Contains("hoje e que dia")
                    || normalizedString.Contains("qual a data de hoje")
                    || normalizedString.Contains("me diga a data de hoje")
                    || normalizedString.Contains("me fale a data de hoje ")
                    || normalizedString.Contains("me diga que dia e hoje")
                    || normalizedString.Contains("me fale que dia e hoje ")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsGreeting
        public bool IsGreeting(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                    normalizedString.Contains("ola ")
                    || normalizedString.Contains("oi ")
                    || normalizedString.Contains("ei ")
                    || normalizedString.Contains("bom dia")
                    || normalizedString.Contains("boa tarde")
                    || normalizedString.Contains("boa noite")
                    || normalizedString.Contains("tudo bem com voce")
                    || normalizedString.Contains("como voce esta")
                    || normalizedString.Contains("voce esta bem")
                    || normalizedString.Contains("fala ai cara beleza")
                    || normalizedString.Contains("e ai tudo certo")
                    || normalizedString.Contains("como vai voce")
                    || normalizedString.Contains("prazer em te conhecer")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //IsFarewell
        public bool IsFarewell(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Converter para minúsculas
            text = text.ToLowerInvariant();
            // Remover acentos
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            if (
                    normalizedString.Contains("tchau ")
                    || normalizedString.Contains("ate mais")
                    || normalizedString.Contains("ate logo")
                    || normalizedString.Contains("ate breve")
                    || normalizedString.Contains("fique bem")
                    || normalizedString.Contains("cuide se")
                    || normalizedString.Contains("nos vemos mais tarde")
                    || normalizedString.Contains("nos vemos em breve")
                    || normalizedString.Contains("foi bom falar com voce")
                    || normalizedString.Contains("falou ai cara")
                    || normalizedString.Contains("falou ai irmao")
                    || normalizedString.Contains("falou ai mano")
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
