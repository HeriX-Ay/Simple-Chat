# Simple-Chat
If the code doesn't run on Visual studio try Visual studio Code
Overview
Simple Chat is a social‑media‑style messaging application where users can send and receive messages instantly.
It was built as part of my software development learning journey, focusing on:

Real‑time communication

Backend development

Database handling

Clean project structure

Practical use of C# and ASP.NET Core

This project represents my growing experience in building functional, scalable applications.

Tech Stack
C# / .NET 8

ASP.NET Core

SignalR (real‑time messaging)

Entity Framework Core

SQLite (local database)

Razor Pages

HTML, CSS, JavaScript

Project Structure
Models/ — Data models for messages and users

Data/ — Database context and EF Core configuration

Pages/ — Razor pages for UI

wwwroot/ — Static files (CSS, JS)

ChatHub.cs — SignalR hub handling real‑time messaging

Program.cs — Application startup and configuration

simplechat.db — SQLite database

appsettings.json — App configuration

Features
Real‑time chat using SignalR

Persistent message storage

Clean and simple UI

Fully functional backend

Easy to extend with new features

How to Run the Project
Clone the repository

Open the project in Visual Studio

Restore NuGet packages

Run the project using IIS Express or dotnet run

The app will launch in your browser and the chat will work instantly.

Future Improvements
User accounts & authentication

Private messaging

Message reactions

File/image sharing

Improved UI design

Cloud deployment
