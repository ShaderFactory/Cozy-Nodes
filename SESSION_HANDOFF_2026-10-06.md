# Cozy Nodes — Handoff de 6 de outubro de 2026

Este arquivo resume a sessão de migração e desenvolvimento do Cozy Nodes. Leia-o
antes de continuar o trabalho em uma conversa nova.

## Objetivo do pacote

Cozy Nodes deve ser um framework reutilizável de grafos para jogos Unity. Ele não
deve conter regras específicas do Quiet Falls. O pacote fornece o editor, portas,
importação e execução; cada jogo poderá criar seus próprios nodes para quests,
cutscenes, inventário, conquistas e outros sistemas.

## Estilo de código combinado

- Escrever C# explícito, simples de ler e com nomes claros.
- Explicar o motivo e o fluxo de trechos não óbvios com comentários.
- Evitar atalhos e casos especiais para nodes concretos quando um contrato genérico
  resolver o problema.
- Manter Editor/Graph Toolkit separado do runtime.
- Crescer a arquitetura somente quando uma funcionalidade concreta pedir isso.

As mesmas regras estão em `AGENTS.md` e no README.

## Migração para Unity 6.6.1f1

- `package.json` foi atualizado para Unity `6000.6` / `1f1`.
- O Graph Toolkit experimental separado foi substituído pelo Graph Toolkit integrado
  ao Editor do Unity 6.6.
- APIs antigas em minúsculas foram atualizadas para as APIs atuais em PascalCase.
- Um `.cozygraph` antigo não carregou depois da migração. Ele foi apagado pelo usuário
  e um grafo novo foi criado com sucesso.
- `CozyEditorNode` recebeu `[Serializable]`, corrigindo o aviso de SerializeReference.

## Modelo de execução decidido

O pacote usa dois tipos de node:

1. **Nodes de fluxo**: executam uma ação e escolhem o próximo caminho. Exemplos:
   Start, Print, Branch, End, futuros Wait/Quest/Play Sound.
2. **Nodes de valor**: não executam no fluxo. Eles calculam uma saída somente quando
   outro node pede um input. Exemplos: CombineString, matemática e comparações.

Quando um input de dados está conectado, o runtime segue a conexão para trás e pede
o valor ao node de origem. Isso é recursivo: um `CombineString` pode alimentar outro
`CombineString`, e ambos podem alimentar o `Message` do Print.

Evitar ciclos de dados por enquanto: ainda não existe detecção de ciclo ou cache de
resultados.

## O que foi implementado e testado

### Dados

- `CombineString` é um node de valor real, não um caso especial do importador.
- `CombineStringRuntime` resolve `First` e `Second` pelo avaliador recursivo e retorna
  `Result`.
- `PrintNode` usa `GetInputValue("Message")`, então aceita texto digitado, constantes,
  CombineString encadeado e valores de variáveis importados.
- `PrintNode.Message` usa a área de texto multilinha nativa do Graph Toolkit.
- Nós constantes internos do Graph Toolkit são lidos pelo importador e não viram nodes
  de runtime. Isso corrigiu o valor `World!` ausente no teste inicial.
- Variáveis do Graph Toolkit agora têm seu **valor padrão** importado genericamente,
  usando `TryGetDefaultValue<T>`. O usuário testou após a recompilação e indicou que
  funcionou.

### Fluxo

- O antigo `NextNodeID` único foi substituído por `RuntimeFlowConnection`:
  uma lista de caminhos com o nome da saída e o node de destino.
- Os nós de runtime usam agora os IDs estáveis dos nós do Graph Toolkit, em vez de
  GUIDs gerados de novo a cada importação.
- `CozyNodeExecutionResult` permite `Continue`, `Waiting`, `Finished` e `Failed`.
  `Waiting` sustenta nodes de evento sem implementar uma máquina de estados grande.
- `CozyManager` é agora a ponte pequena do Unity: recebe `Trigger`, inicia o grafo e
  publica `EventInvoked`. `CozyGraphRunner` executa iterativamente, não por recursão,
  e continua por uma saída de fluxo nomeada.
- `PrintNode` continua por `out`.
- Foi criado `BranchNode`/`BranchRuntime`. Ele lê `Condition` e segue `True` ou `False`.
- O teste do usuário com `True` e `False` funcionou.

## Limitações conhecidas

- Uma saída de fluxo sem fio conectado gera um aviso do tipo “has no connection on
  flow output”. Não quebra o Branch; o caminho simplesmente termina. Melhorar isso
  depois para encerrar silenciosamente ou expor uma configuração de aviso.
- Cada `EndNode` aceita uma entrada. Para testes de Branch, use um End por caminho.
  Um futuro `Merge` pode juntar caminhos quando necessário.
- As definições do Blackboard são importadas e cada `CozyManager` cria uma tabela
  isolada em runtime. Conexões para variáveis do Blackboard já leem dessa tabela.
  `Set Variable` recebe um node de variável na entrada `Variable` e grava o novo
  valor da entrada `Value`, validando o tipo antes de continuar.
- Ainda não há cache de nós de valor nem detecção de ciclos.
- `InvokeEventNode` envia nome e payload para `CozyManager.EventInvoked`. O payload
  é runtime-only e preserva o tipo do valor conectado, sem serialização insegura.
- O importador agora copia as definições do Blackboard para `RuntimeCozyGraph`.
  Cada `CozyManager` cria uma `CozyRuntimeVariables` própria no início da execução.
  Conexões de variável agora preservam a referência estável do Blackboard e leem
  a tabela daquele Manager. `Set Variable` usa a mesma referência para escrever
  somente na tabela de runtime.
- O End usa o comportamento genérico `Finished`; é suficiente, mas futuramente pode
  ganhar uma classe de runtime explícita se ela trouxer comportamento útil.

## Próxima ordem recomendada

1. Testar `Set Variable` com uma variável booleana do Blackboard:
   Start → Set → Branch → Print → End.
2. `Add Integer` foi criado como primeiro node numérico. Testar uma variável `int`
   do Blackboard, uma constante e uma cadeia de dois Add Integer ligados ao payload
   de `Invoke Event` (o Print atual recebe apenas texto).
3. Conversões de porta agora permitem `int`, `float` e `bool` para `string`, além
   de `int` para `float`. Assim Add Integer pode ligar direto no Print. O Graph
   valida Set Variable com `GraphLogger` e mostra o erro no próprio node.
4. Criar `Greater Than Integer` para transformar uma comparação numérica em bool e
   alimentar o Branch.
5. Decidir como apresentar o alvo e o valor do `Set Variable` na API amigável
   futura, quando adicionarmos atributos para criação de nodes externos.
3. Ajustar a mensagem de saída de fluxo desconectada para não parecer erro quando o
   término intencional for válido.
   Isso deve vir antes de quest/achievement/save específicos.
4. Implementar a API amigável para jogos externos: uma classe base pública e atributos
   para expor campos como inputs, outputs e parâmetros. A inspiração é XNode, mas sem
   copiar sua arquitetura literalmente.
5. Depois, nodes core pequenos: comparação e Branch usando valores calculados.

## Como testar o estado atual

1. Crie um `BranchNode`.
2. Ligue Start → Branch (`in`).
3. Ligue `True` e `False` a Prints diferentes.
4. Ligue o `out` de cada Print ao seu próprio End.
5. Defina ou conecte o bool `Condition`; dê Play.
6. Para testar dados, ligue `CombineString.Result` ao `Message` de um Print e use
   constantes ou uma variável com valor padrão nas entradas do Combine.

## Arquivos principais alterados nesta sessão

- `Editor/Graph/CozyGraphImporter.cs`
- `Editor/Graph/CozyEditorNode.cs`
- `Editor/Graph/CozyGraph.cs`
- `Editor/Nodes/BranchNode.cs`
- `Editor/Nodes/CombineString.cs`
- `Editor/Nodes/PrintNode.cs`
- `Editor/Nodes/StartNode.cs`
- `Editor/Nodes/EndNode.cs`
- `Runtime/CozyManager.cs`
- `Runtime/RuntimeCozyNode.cs`
- `Runtime/CozyRuntimePort.cs`
- `Runtime/CustomNodes/BranchRuntime.cs`
- `Runtime/CustomNodes/CombineStringRuntime.cs`
- `Runtime/CustomNodes/CustomPrintRuntime.cs`
- `README.md`
- `package.json`
