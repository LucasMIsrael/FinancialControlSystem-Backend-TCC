<img width="1200" height="129" alt="finvisionlogo" src="https://github.com/user-attachments/assets/37df6d7d-7d1c-4c20-a045-3c2e52c5c26b" />

# FinancialControlSystem-Backend-TCC  
> Backend do Sistema de Controle Financeiro Multi-Tenancy - Desenvolvido por Lucas Mendes Israel

[![Status do Projeto](https://img.shields.io/static/v1?label=STATUS&message=CONCLUÍDO&color=GREEN&style=for-the-badge)]()

### Documentação RFC: [Sistema de Controle Financeiro MultiTenancy - RFC.pdf](https://github.com/user-attachments/files/23757507/Sistema.de.Controle.Financeiro.MultiTenancy.-.RFC.pdf)

### Aplicação: https://finvision-financialctrl.vercel.app/

<br>

## 🔎 Visão Geral  
Este repositório contém o backend do **FinVision**, um sistema de controle financeiro com suporte a **Multi-Tenancy**, desenvolvido como trabalho de conclusão de curso (TCC/Portfólio) para o curso de Engenharia de Software na Universidade Católica de SC - Joinville. O sistema permite a gestão de finanças, oferecendo isolamento entre diferentes cenários financeiros do usuário - tenants - e persistência segura dos dados.

<br>

## 📦 Tecnologias utilizadas  
- Linguagem principal: **C# (.NET 9)**  
- ORM / Acesso a dados: **Entity Framework**  
- Banco de dados: **PostgreSQL**  
- Autenticação: **JWT / Bearer Token**
- Arquitetura: **MVC**

<br>

## 🛠️ Como rodar localmente  

```bash
# 1. Ter instalado em sua máquina o Visual Studio 2022 (mínimo) e o SDK .NET 9, além do banco PostgreSQL
# disponível em: https://visualstudio.microsoft.com/pt-br/vs/
#                https://www.postgresql.org/

# 2. Clone o repositório
git clone https://github.com/LucasMIsrael/FinancialControlSystem-Backend-TCC.git

# 3. Acesse o diretório do backend
cd FinancialControlSystem-Backend-TCC/FinancialControlSystem-Backend

# 4. Restaurar dependências e compilar
dotnet restore
dotnet build

# 4. Configurar variável de ambiente do banco no appsettings.json, para rodar em seu banco localmente

# 5. no Console Gerenciador de Pacotes do Visual Studio, atualizar banco para fixar as migrações
update-database

# 6. Executar a aplicação
# botão FinancialSystem.Web - Development

# 7. Endpoints devem estar disponíveis em: https://localhost:5243/swagger/index.html
```
<br>

## 🚀: Funcionalidades do projeto
- `Multi-Tenancy`: suporte a múltiplos tenants/ambientes com isolamento de dados;
- `CRUD de entidades`: ambientes, transações planejadas, inesperadas (para despesas e/ou recebimentos que ocorreram sem conhecimento prévio) e metas (pontuais ou não);
- `Metas`: metas pontuais ou recorrentes para serem alcançadas com base nas transações e saldo total;
- `Dashboard`: conjunto de gráficos para melhor controle das finanças e análise feita por IA com dicas;
- `Ranking`: lista dos 10 ambientes que mais alcançaram metas.
