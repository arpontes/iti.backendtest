# iti.backendtest
Teste para posição de back-end

Para executar via linha de comando, estando na pasta da solution, executar:
#### dotnet run --project iti.backendtest/iti.backendtest.csproj "[path para o arquivo de log]" "[0 ou 1]"
O segundo parâmetro (0 ou 1) refere-se ao armazenamento das movimentações para posterior ordenação e exibição. Zero indica que o sistema utilizará memória para o armazenamento. 1 indica que o sistema utilizará o sistema de arquivos. Detalhes da implementação estão comentados no código.

Para executar os testes via linha de comando, estando na pasta da solution, executar:
#### dotnet test iti.backendtest.Tests/iti.backendtest.Tests.csproj
Os testes estão cobrindo 100% do código não relacionado ao I/O do próprio sistema (ou seja, o conteúdo da classe Program, que trata da entrada dos dados que serão utilizados no processamento e saída dos resultados não possui testes unitários. As regras de negócio propriamente ditas estão cobertas).

# Premissas
Como não havia como fazer questões prévias, realizei o desenvolvimento partindo de algumas premissas, a saber:
- A primeira linha do arquivo poderá ser descartada, contendo apenas os nomes das colunas;
- As colunas são separadas por tabs;
- Todas as linhas (exceto a primeira) terão valores corretos, separados por 3 tabs, com os seguintes formatos:
  - Primeira columa: DD-MMM (o mês está abreviado em inglês)
  - Terceira coluna: Valor decimal formatado no padrão brasileiro (vírgula como separador de casas decimais e ponto como separador de milhar)
- Os caminhos para a obtenção dos JSONs estão fixos no código.
- Caso alguma das chamadas ao JSON apresente erro ou formação incorreta, ou caso o log também não esteja no formato correto, o sistema irá parar com uma exceção.
- O resultado do JSON deverá ser um array de elementos onde cada elemento deve ter os seguintes atributos: data, descricao, valor, categoria. Outros atributos serão ignorados. Há dois formatos observados para a data ("DD/MMM" e "DD / MMM"), portanto, a regra será: o dia será obtido pelos dois primeiros caracteres; o mês, pelos 3 últimos. O valor está formatado no padrão brasileiro (vírgula como separador de casas decimais e ponto como separador de milhar). O mês está abreviado em português.
