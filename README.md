# consistent-hashing-redis

Implementação de um consistent hashing para distribuicao de acesso aos servidores redis.

Minha intenção é simular o redirecionamento de um recurso (redis ou qualquer outro) entre os usuarios, esse redirecionamento pode ser feito tendo como chave qualquer objeto representado por uma string, por simplificação usarei o parametro "name" passado na chamada do endpoint.

> [!IMPORTANT]  
> Use a branch release/v1, as branchs main e develop podem ser alteradas enquanto faço melhorias o projeto.

## Como executar

Baixe o repositorio

```sh
git clone git@github.com:AdsonFS/consistent-hashing-redis.git
```

Entre na pasta docker/ e execute o docker compose para subir os container redis e o redisinsight caso queria visualizar os dados do redis.

```sh
cd docker
docker compose up --build -d
```

Para rodar a API volte para a raiz do projeto e execute:

```sh
cd ..
dotnet restore
dotnet run --project src/ConsistentHashingRedis.API
```

## Explicação e Teste

Para adicionar e remover os 5 servidores redis, use os endpoints passando name e host (localhost:XXXX). A controller chama os seguintes metodos:

### Add/Delete

```cs
public uint AddServer(string name, string host)
{
    _semaphoreSlim.Wait();
    uint hashKey = GetHash(host);
    System.Console.WriteLine(hashKey);
    if (_map.ContainsKey(hashKey))
    {
        System.Console.WriteLine("Houve conflito no hash");
        _semaphoreSlim.Release();
        return 0;
    }

    System.Console.WriteLine($"Servidor: {name}\tKey: {hashKey}");
    _map.Add(hashKey, new(name, host));
    UpdateSegTree(1, 1, _hashRange, hashKey, hashKey);
    _semaphoreSlim.Release();
    return hashKey;
}

public void RemoveServer(string name, string host)
{
    _semaphoreSlim.Wait();
    uint hashKey = GetHash(host);
    System.Console.WriteLine(hashKey);
    if (_map.ContainsKey(hashKey) && _map[hashKey].Host == host)
    {
        _map[hashKey].Connection.Dispose();
        _map.Remove(hashKey);
        UpdateSegTree(1, 1, _hashRange, hashKey, 0);
        System.Console.WriteLine($"Servidor: {name}\tKey: {hashKey}");
        _semaphoreSlim.Release();

        return;
    }
    _semaphoreSlim.Release();
}
```

O semaphoreSlim serve para bloquear a modificação no \_map, evitando problemas com Threads.

Estou considerando que esteja familiarizado com a ideia do algoritmo de Consistent Hash, o maior segredo na minha implementação está em utilizar uma estrutua de dados chamada SegTree, com isso, posso encontrar o primeiro elemento à direita de qualquer índice em tempo O(log N), onde N é o tamanho do meu range.

A implementação e conceito da SegTree, deixarei para outro material, por hora, entenda apenas que ela resolve de forma eficiente a busca pelo "próximo" servidor.

> [!NOTE]  
> Note que optei por não duplicar os servidores, pois assim será mais facil testar manualmente.

### GetInstance

```cs
public RedisServer? GetInstance(string name)
{
    if (_map.Count == 0) return null;
    uint hashKey = GetHash(name);
    System.Console.WriteLine(hashKey);

    uint index = QuerySegTree(1, 1, _hashRange, hashKey, _hashRange);
    if (index == 0) index = QuerySegTree(1, 1, _hashRange, 1, hashKey - 1);
    System.Console.WriteLine($"Servidor Selecionado: {_map[index].Name}");
    return _map[index];
}
```

Aqui a busca próximo nos intervalos [hashKey, hashRange] e [0, hashKey-1] serve para manter a característica circular do Consistent Hash. Outra opção conveniente é dobrar o tamanho do vetor, e usar apenas a primeira metade como início da busca, essa técnica tem o mesmo efeito de circular o vetor na busca.

### Tabela de mapeamento

Segue o mapeamento de cada um dos servidores (considerando hosts: localhost:6379, localhost:6380, ...)

| redis-server | host           | key |
| ------------ | -------------- | --- |
| redis1       | localhost:6379 | 21  |
| redis2       | localhost:6380 | 93  |
| redis3       | localhost:6381 | 72  |
| redis4       | localhost:6382 | 95  |
| redis5       | localhost:6383 | 59  |

Usando o Endpoint /api/Redis/{name}, gerei a tabela abaixo (para ver a key gerada por cada name, confira o log da aplicação rodando)

| name | redis-server | key |
| ---- | ------------ | --- |
| a1   | redis2       | 91  |
| a2   | redis1       | 14  |
| a3   | redis5       | 58  |

É fácil notar que o algoritmo achou sempre o servidor com a chave mais próxima (pela direita) à chave gerada a partir do campo **name**.

Para validar melhor, exclua o redis2 e chame /api/Redis/a1, agora o servidor que fomos direcionados foi o **redis4**, isso porque sua chave é 95, a próxima após 91 do redis2 que foi excluido.

Seguindo o teste na mesma linha, exclua o redis4 e chame novamente chame /api/Redis/a1, o servidor que recebemos é o **redis1** de chave 21, mostrando a busca circular da implementação.

## Teste e Divirta-se

:)
