# Guia de Troubleshooting - Stripe Price IDs

## Erro: "No such price: 'price_XXXXX'"

Este erro ocorre quando o Price ID não existe na conta do Stripe que está sendo usada pelas suas chaves de API.

### Verificação Passo a Passo

#### 1. Verificar Ambiente (Test vs Live)

**Seu appsettings.Development.json mostra:**
- SecretKey: `sk_live_...` (modo PRODUÇÃO/LIVE)
- PublishableKey: `pk_live_...` (modo PRODUÇÃO/LIVE)

**IMPORTANTE**: Suas chaves são de PRODUÇÃO, então você DEVE criar os Price IDs no modo **LIVE** do Stripe.

#### 2. Como Verificar/Criar no Stripe Dashboard

1. Acesse: https://dashboard.stripe.com
2. **Verifique o ambiente** no canto superior direito:
   - Se estiver em "Test mode" (modo de teste), você precisa alternar para "Live mode" (modo de produção)
   - Clique no toggle no topo da página para alternar
3. Vá em **Products** → Verifique se seus produtos estão lá
4. **Para cada produto**:
   - Clique no produto
   - Verifique o **Price ID** (começa com `price_`)
   - **COPIE O PRICE ID EXATO** - é importante copiar exatamente como aparece
   - Verifique se o status está "Active"

#### 3. Verificar se o Price ID Está Correto

No seu `appsettings.Development.json`, você tem:
```json
"PriceIdNivel2": "price_1SaSHk1XR8K6iMsaSE9IISHI"
```

**Verifique no Stripe Dashboard**:
- O Price ID começa com `price_1`? (produção) ou `price_` (teste)
- O Price ID está exatamente igual ao copiado?
- O produto e o preço estão **Active**?

#### 4. Comandos Úteis no Stripe CLI (opcional)

Se você tem o Stripe CLI instalado:
```bash
# Verificar se o Price ID existe
stripe prices retrieve price_1SaSHk1XR8K6iMsaSE9IISHI

# Listar todos os produtos
stripe products list

# Listar todos os preços
stripe prices list
```

#### 5. Problemas Comuns

**Problema**: Price ID criado em Test mode mas usando chaves Live
- **Solução**: Crie os produtos/preços no Live mode OU use chaves de teste (`sk_test_...`)

**Problema**: Price ID deletado acidentalmente
- **Solução**: Recrie o produto/preço no Stripe Dashboard

**Problema**: Price ID copiado incorretamente (espaços, caracteres extras)
- **Solução**: Copie novamente diretamente do Stripe Dashboard

#### 6. Criar Novo Price ID (se necessário)

Se precisar recriar:

1. Acesse Stripe Dashboard em **Live mode**
2. Products → **Add product**
3. Preencha:
   - **Name**: "StarkAid Nível 2 - Remove Ads"
   - **Description**: "Remove anúncios - não adiciona StarkCoins"
   - **Pricing**: 
     - Modo: **Recurring**
     - Preço: **R$ 10,00**
     - Recorrência: **Monthly**
4. Clique em **Save product**
5. **Copie o Price ID** (ex: `price_1XXXXX`)
6. **Ative o produto** se necessário
7. Atualize no `appsettings.Development.json`

### Checklist Final

- [ ] Está no ambiente correto no Stripe (Live se usando `sk_live_`, Test se usando `sk_test_`)
- [ ] Produto criado e está **Active**
- [ ] Price ID copiado exatamente (sem espaços, sem caracteres extras)
- [ ] Price ID atualizado no `appsettings.Development.json`
- [ ] API reiniciada após alterar o appsettings.json

