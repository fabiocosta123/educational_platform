# 📚 Plataforma Educacional - MVP

Este projeto é uma plataforma de estudos a distância, iniciando com uma versão simples (MVP) e evoluindo gradualmente.  
O objetivo é permitir que professores cadastrem cursos, matérias e conteúdos, e que alunos possam assistir aulas e interagir com dúvidas e respostas.

---

## 🏗️ Arquitetura Inicial

### Frontend
- **Framework**: [React](ca://s?q=React_para_plataforma_educacional)
- **Hospedagem inicial**: Vercel/Netlify (plano gratuito)
- **Funcionalidades MVP**:
  - Login e cadastro de usuários
  - Listagem de cursos → matérias → conteúdos
  - Player de vídeo embutido (YouTube)
  - Campo de dúvidas e respostas (assíncrono)

### Backend
- **Framework**: [ASP.NET Core Web API](ca://s?q=ASP_NET_Core_Web_API)
- **IDE**: Visual Studio 2026
- **Hospedagem inicial**: Railway/Render/Azure Free Tier
- **Funcionalidades MVP**:
  - API REST para usuários, cursos, matérias, conteúdos e dúvidas
  - Autenticação básica (JWT)
  - Estrutura modular (Controllers → Services → Repository → Database)
- **Containers**: Docker habilitado (Linux containers)
- **Orquestração**: Aspire para observabilidade e gerenciamento de múltiplos serviços

### Banco de Dados
- **SQL (PostgreSQL ou SQL Server)**  
  - Usuários  
  - Cursos  
  - Matérias  
  - Conteúdos  
  - Pagamentos (futuro)
- **NoSQL (MongoDB Atlas Free)**  
  - Comentários e dúvidas (documentos flexíveis)

---

## 📊 Modelagem Inicial

### SQL
- **Usuarios**
  - Id, Nome, Email, Senha, Perfil (Aluno/Professor/Admin)
- **Cursos**
  - Id, Nome, ProfessorId
- **Materias**
  - Id, Nome, CursoId
- **Conteudos**
  - Id, Nome, MateriaId, LinkYouTube

### NoSQL (MongoDB)
- **Comentarios**
  ```json
  {
    "conteudoId": "123",
    "usuarioId": "456",
    "texto": "Minha dúvida sobre async/await",
    "publico": true,
    "respostas": [
      {
        "usuarioId": "789",
        "texto": "Explicação do professor"
      }
    ]
  }
