# TURTLE

**Turtle** is a collaborative social bookmarking platform inspired by community-driven sites like Reddit. The application allows users to save, organize, and discuss diverse web content: text, images, and documents in a structured, engaging environment.

---

### Project Idea
Turtle builds a knowledge-sharing and content-curation ecosystem where users can:
* Discover the most popular or recent bookmarks shared by the community.
* Support and interact with community content through a dynamic voting and commenting system.

The platform emphasizes community-driven curation, meaning the visibility of content is directly influenced by user engagement (likes) rather than passive consumption.

---

### Dynamic Content Discovery & Search
Finding the right content is a core focus of Turtle. The platform features an advanced search engine that allows users to:
* Search bookmarks by title, description, or category.
* Use partial word matching, ensuring no content is lost due to strict exact-match limitations.
* View results paginated and seamlessly sorted by relevance, such as the highest number of votes or the creation date.

---

### AI Companion – Smart Tagging & Categorization
To simplify content organization, Turtle integrates a virtual AI Companion designed to assist users when creating new bookmarks. 
* When adding a bookmark, users can click the "Sugerează cu AI" (Suggest with AI) button.
* The AI analyzes the provided title and description to automatically propose suitable categories.
* Users maintain full creative control, having the option to accept, modify, or ignore the AI's suggestions before saving.

---

### Bookmarking & Social Interaction
Turtle is built around user profiles and content interaction. Each user can:
* Customize their personal profile with a name, bio (About section), and profile picture.
* Create bookmarks.
* Vote (like) on bookmarks to influence the global "Popular" feed.
* Comment on posts to start discussions, with full control to edit or delete their own input.

---

### Users & Roles
Turtle supports three main levels of access to keep the platform secure and moderated:
* **Unregistered Visitor** – Can browse public bookmarks, view the latest/popular feeds, and use the search engine.
* **Registered User** – Can create bookmarks, vote, comment, customize their profile, and manage private/public categories.
* **Administrator** – Has full oversight of the platform's content, with the ability to delete inappropriate bookmarks, comments, or categories to maintain a safe environment.

---

### Technical Overview
* **ASP.NET Core MVC 9.0** – Backend framework and architectural pattern.
* **Entity Framework Core** – ORM used for data persistence and database management.
* **ASP.NET Core Identity** – Manages secure user authentication, registration, and role-based access control.
* **AI Integration** – External AI API utilization for the smart tagging and categorization features.
