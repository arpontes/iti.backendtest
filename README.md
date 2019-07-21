# iti.backendtest
Teste para posição de back-end

Para executar via linha de comando, estando na pasta da solution, executar:
#### dotnet run --project iti.backendtest/iti.backendtest.csproj "[path para o arquivo de log]"

Para executar os testes via linha de comando, estando na pasta da solution, executar:
#### dotnet test iti.backendtest.Tests/iti.backendtest.Tests.csproj

# Premissas
Como não havia como fazer questões prévias, realizei o desenvolvimento partindo de algumas premissas, a saber:
- A primeira linha do arquivo poderá ser descartada, contendo apenas os nomes das colunas;
- As colunas são separadas por tabs;
- Todas as linhas (exceto a primeira) terão valores corretos, separados por 3 tabs, com os seguintes formatos:
  - Primeira columa: DD-MMM
  - Terceira coluna: Valor decimal formatado no padrão brasileiro (vírgula como separador de casas decimais e ponto como separador de milhar)
- O resultado não está sendo arredondado, porém, os cálculos foram feitos utilizando o tipo decimal, para evitar possíveis erros de conversão inerentes ao tipo double.
