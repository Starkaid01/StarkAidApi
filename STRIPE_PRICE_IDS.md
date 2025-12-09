# Lista de Price IDs Necessários no Stripe

## Planos de Assinatura Mensal

Você precisa criar **6 produtos/price IDs** no Stripe para os planos de assinatura:

### Nível 2 - Remove Ads (R$ 10,00/mês)
- **Price ID**: Deve ser configurado em `StripeSettings:PriceIdNivel2`
- **Valor**: R$ 10,00 (mensal)
- **Descrição**: Remove anúncios - não adiciona StarkCoins
- **Modo**: Subscription (recorrente)
- **Recorrência**: Mensal

### Nível 3 - Plano de StarkCoins (R$ 5,00/mês)
- **Price ID**: Deve ser configurado em `StripeSettings:PriceIdNivel3`
- **Valor**: R$ 5,00 (mensal)
- **Descrição**: Adiciona 5 StarkCoins por mês
- **Modo**: Subscription (recorrente)
- **Recorrência**: Mensal

### Nível 4 - Plano de StarkCoins (R$ 15,00/mês)
- **Price ID**: Deve ser configurado em `StripeSettings:PriceIdNivel4`
- **Valor**: R$ 15,00 (mensal)
- **Descrição**: Adiciona 15 StarkCoins por mês
- **Modo**: Subscription (recorrente)
- **Recorrência**: Mensal

### Nível 5 - Plano de StarkCoins (R$ 25,00/mês)
- **Price ID**: Deve ser configurado em `StripeSettings:PriceIdNivel5`
- **Valor**: R$ 25,00 (mensal)
- **Descrição**: Adiciona 25 StarkCoins por mês
- **Modo**: Subscription (recorrente)
- **Recorrência**: Mensal

### Nível 6 - Plano de StarkCoins (R$ 50,00/mês)
- **Price ID**: Deve ser configurado em `StripeSettings:PriceIdNivel6`
- **Valor**: R$ 50,00 (mensal)
- **Descrição**: Adiciona 50 StarkCoins por mês
- **Modo**: Subscription (recorrente)
- **Recorrência**: Mensal

### Nível 7 - Plano de StarkCoins (R$ 100,00/mês)
- **Price ID**: Deve ser configurado em `StripeSettings:PriceIdNivel7`
- **Valor**: R$ 100,00 (mensal)
- **Descrição**: Adiciona 100 StarkCoins por mês
- **Modo**: Subscription (recorrente)
- **Recorrência**: Mensal

## Configuração no appsettings.json

Após criar os produtos no Stripe, configure os Price IDs em:

```json
"StripeSettings": {
  "PriceIdNivel2": "price_XXXXX",  // R$ 10,00 - Remove Ads
  "PriceIdNivel3": "price_XXXXX",  // R$ 5,00 - 5 StarkCoins/mês
  "PriceIdNivel4": "price_XXXXX",  // R$ 15,00 - 15 StarkCoins/mês
  "PriceIdNivel5": "price_XXXXX",  // R$ 25,00 - 25 StarkCoins/mês
  "PriceIdNivel6": "price_XXXXX",  // R$ 50,00 - 50 StarkCoins/mês
  "PriceIdNivel7": "price_XXXXX"   // R$ 100,00 - 100 StarkCoins/mês
}
```

## Nota Importante

- Todos os planos devem ser configurados como **Subscription** (assinatura recorrente)
- Todos devem ter recorrência **mensal**
- Os Price IDs no Stripe começam com `price_` (modo de teste) ou `price_` (modo de produção)
- Configure Price IDs diferentes para ambiente de desenvolvimento e produção

## ⚠️ IMPORTANTE: Ativar Produtos no Stripe

**CRÍTICO**: Após criar os produtos/price IDs no Stripe, você DEVE ativá-los:

1. Acesse o Dashboard do Stripe
2. Vá em **Products**
3. Para cada produto criado:
   - Clique no produto
   - Verifique se o status está como **"Active"** (Ativo)
   - Se estiver como **"Inactive"** (Inativo), clique no botão **"Activate"** ou **"Make active"**
4. **Verifique também os Price IDs**:
   - Dentro de cada produto, verifique os preços
   - Certifique-se de que o Price ID que você vai usar está ativo
   - Se houver preços inativos, ative-os

**Erro comum**: `Price 'price_XXXXX' is not available to be purchased because its product is not active.`

**Solução**: Ative o produto no Stripe Dashboard. O produto e o preço devem estar ativos para que o checkout funcione.

